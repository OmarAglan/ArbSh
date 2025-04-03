#include "shell.h"

#ifdef WINDOWS
#include <windows.h>
#include <io.h>
#else
#include <fcntl.h>
#include <unistd.h>
#include <sys/file.h> /* For file locking constants on some systems */
#include <sys/types.h>
#include <errno.h>
#include <string.h> /* For strerror() */
#endif

/**
 * get_history_file - gets the history file
 * @info: parameter struct
 * Return: allocated string containing history file
 */
char *get_history_file(info_t *info)
{
    char *buf, *dir;

    // If we have a configured history_file_path, use it
    if (info->history_file_path)
        return shell_strdup(info->history_file_path);

    // Fall back to the default behavior
    dir = _getenv(info, "HOME=");
    if (!dir)
        return (NULL);
    buf = malloc(sizeof(char) * (_strlen(dir) + _strlen(HIST_FILE) + 2));
    if (!buf)
        return (NULL);
    buf[0] = 0;
    _strcpy(buf, dir);
    _strcat(buf, "/");
    _strcat(buf, HIST_FILE);
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
