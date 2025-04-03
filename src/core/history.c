#include "shell.h"
#include "platform/filesystem.h" // Include Filesystem PAL
#include <string.h>
#include <stdlib.h>
#include <sys/types.h> // For mode_t on some systems
#include <sys/stat.h>  // For S_IRUSR, S_IWUSR etc.

#ifdef WINDOWS
#include <io.h>      // For _open, _close, _read, _write
#include <fcntl.h>   // For _O_RDONLY, _O_WRONLY, _O_CREAT, _O_TRUNC
#else // POSIX
#include <unistd.h>  // For open, close, read, write, stat
#include <fcntl.h>   // For O_RDONLY, O_WRONLY, O_CREAT, O_TRUNC
#include <pwd.h>     // For getpwuid (now handled by PAL)
#endif

/**
 * get_history_file - returns the history file path
 * @info: parameter struct (unused currently, but good practice)
 *
 * Return: allocated string containing history file path, or NULL
 */
char *get_history_file(info_t *info)
{
	char *buf = NULL, *dir = NULL;
	char home_dir[PATH_MAX];

	(void)info; // Mark as unused for now

	if (!platform_get_home_dir(home_dir, sizeof(home_dir)))
	{
		// Cannot determine home directory, maybe return default relative path?
		// For now, return NULL to indicate failure.
		return (NULL);
	}
	dir = home_dir;

	// Allocate space for the full path: dir + '/' + filename + null terminator
	buf = malloc(sizeof(char) * (strlen(dir) + strlen(HIST_FILE) + 2));
	if (!buf)
	{
		free(dir); // Free dir if malloc fails
		return (NULL);
	}

	buf[0] = 0; // Initialize buffer
	strcpy(buf, dir);

#ifdef WINDOWS
	// Use backslash on Windows
	strcat(buf, "\\");
#else
	// Use forward slash on POSIX
	strcat(buf, "/");
#endif

	strcat(buf, HIST_FILE);

	// We only copied the pointer from platform_get_home_dir's buffer, no need to free 'dir' here
	// if platform_get_home_dir allocated memory, it should be freed by the caller
	// or have a separate platform_free_path function.
	// Assuming platform_get_home_dir writes to the provided buffer.

	return (buf);
}

/**
 * write_history - creates a file, or appends to an existing file
 * @info: the parameter struct
 * Return: 1 on success, else -1
 */
int write_history(info_t *info)
{
    ssize_t fd;
    char *filename = get_history_file(info);
    list_t *node = NULL;
#ifdef WINDOWS
    HANDLE hFile;
    OVERLAPPED ol = {0};
    BOOL locked = FALSE;
#endif

    if (!filename)
        return (-1);

    fd = open(filename, O_WRONLY | O_CREAT | O_TRUNC, 0644);
    if (fd == -1)
    {
        free(filename);
        return (-1);
    }

    // Acquire exclusive lock on the file
#ifdef WINDOWS
    // On Windows, use LockFileEx for file locking
    hFile = (HANDLE)_get_osfhandle(fd);
    if (hFile != INVALID_HANDLE_VALUE)
    {
        locked = LockFileEx(hFile, LOCKFILE_EXCLUSIVE_LOCK, 0, MAXDWORD, 0, &ol);
        if (!locked)
        {
            fprintf(stderr, "Warning: Could not lock history file (error %lu)\n", GetLastError());
        }
    }
#else
    // On Unix-like systems, use fcntl for file locking
    struct flock fl;
    fl.l_type = F_WRLCK;  // Write lock
    fl.l_whence = SEEK_SET;
    fl.l_start = 0;
    fl.l_len = 0;         // Lock entire file
    
    if (fcntl(fd, F_SETLKW, &fl) == -1)
    {
        fprintf(stderr, "Warning: Could not lock history file: %s\n", strerror(errno));
    }
#endif

    // Write history data
    for (node = info->history; node; node = node->next)
    {
        _putsfd(node->str, fd);
        _putfd('\n', fd);
    }
    _putfd(BUF_FLUSH, fd);

    // Release lock
#ifdef WINDOWS
    if (locked)
    {
        UnlockFileEx(hFile, 0, MAXDWORD, 0, &ol);
    }
#else
    fl.l_type = F_UNLCK;
    fcntl(fd, F_SETLK, &fl);
#endif

    close(fd);
    free(filename);
    return (1);
}

/**
 * read_history - reads history from file
 * @info: the parameter struct
 * Return: histcount on success, 0 otherwise
 */
int read_history(info_t *info)
{
    int i, last = 0, linecount = 0;
    ssize_t fd, rdlen, fsize = 0;
    char *buf = NULL, *filename = get_history_file(info);
#ifdef WINDOWS
    struct _stat64i32 st;
    HANDLE hFile;
    OVERLAPPED ol = {0};
    BOOL locked = FALSE;
#else
    struct stat st;
    struct flock fl;
#endif

    if (!filename)
        return (0);

    fd = open(filename, O_RDONLY);
    free(filename);
    if (fd == -1)
        return (0);

    // Acquire shared lock (read lock) on the file
#ifdef WINDOWS
    // On Windows, use LockFileEx for file locking
    hFile = (HANDLE)_get_osfhandle(fd);
    if (hFile != INVALID_HANDLE_VALUE)
    {
        locked = LockFileEx(hFile, 0, 0, MAXDWORD, 0, &ol); // 0 flags = shared lock
        if (!locked)
        {
            fprintf(stderr, "Warning: Could not lock history file for reading (error %lu)\n", GetLastError());
        }
    }
#else
    // On Unix-like systems, use fcntl for file locking
    fl.l_type = F_RDLCK;  // Read lock
    fl.l_whence = SEEK_SET;
    fl.l_start = 0;
    fl.l_len = 0;         // Lock entire file
    
    if (fcntl(fd, F_SETLKW, &fl) == -1)
    {
        fprintf(stderr, "Warning: Could not lock history file for reading: %s\n", strerror(errno));
    }
#endif

    if (!fstat(fd, &st))
        fsize = st.st_size;
    if (fsize < 2)
    {
        close(fd);
        return (0);
    }
    buf = malloc(sizeof(char) * (fsize + 1));
    if (!buf)
    {
        close(fd);
        return (0);
    }
    rdlen = read(fd, buf, fsize);
    buf[fsize] = 0;

    // Release lock
#ifdef WINDOWS
    if (locked)
    {
        UnlockFileEx(hFile, 0, MAXDWORD, 0, &ol);
    }
#else
    fl.l_type = F_UNLCK;
    fcntl(fd, F_SETLK, &fl);
#endif

    close(fd);

    if (rdlen <= 0)
        return (free(buf), 0);
        
    for (i = 0; i < fsize; i++)
        if (buf[i] == '\n')
        {
            buf[i] = 0;
            build_history_list(info, buf + last, linecount++);
            last = i + 1;
        }
    if (last != i)
        build_history_list(info, buf + last, linecount++);
    free(buf);
    info->histcount = linecount;
    while (info->histcount-- >= HIST_MAX)
        delete_node_at_index(&(info->history), 0);
    renumber_history(info);
    return (info->histcount);
}

/**
 * build_history_list - adds entry to a history linked list
 * @info: Structure containing potential arguments
 * @buf: buffer
 * @linecount: the history linecount, histcount
 * Return: Always 0
 */
int build_history_list(info_t *info, char *buf, int linecount)
{
    list_t *node = NULL;

    if (info->history)
        node = info->history;
    add_node_end(&node, buf, linecount);

    if (!info->history)
        info->history = node;
    return (0);
}

/**
 * renumber_history - renumbers the history linked list after changes
 * @info: Structure containing potential arguments
 * Return: the new histcount
 */
int renumber_history(info_t *info)
{
    list_t *node = info->history;
    int i = 0;

    while (node)
    {
        node->num = i++;
        node = node->next;
    }
    return (info->histcount = i);
}
