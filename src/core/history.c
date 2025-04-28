#include "shell.h"
#include "platform/filesystem.h" // Include Filesystem PAL
#include <string.h>
#include <stdlib.h>
#include <sys/types.h> // For mode_t on some systems
#include <sys/stat.h>  // For S_IRUSR, S_IWUSR etc.
#include <limits.h>    // For PATH_MAX

#ifdef WINDOWS
#include <io.h>      // For _open, _close, _read, _write
#include <fcntl.h>   // For _O_RDONLY, _O_WRONLY, _O_CREAT, _O_TRUNC
// Include the definition directly for Windows
struct platform_stat_s {
    WIN32_FILE_ATTRIBUTE_DATA file_info;
};
#else // POSIX
#include <unistd.h>  // For open, close, read, write, stat
#include <fcntl.h>   // For O_RDONLY, O_WRONLY, O_CREAT, O_TRUNC
// Include the definition directly for POSIX
struct platform_stat_s {
    struct stat posix_stat;
};
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
		// Do not free 'dir' here, it points to the stack buffer 'home_dir'
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
 * write_history - writes the current history to file
 * @info: the parameter struct
 *
 * Return: 1 on success, -1 otherwise
 */
int write_history(info_t *info)
{
    ssize_t fd;
    char *filename = get_history_file(info);
    list_t *node = NULL;

    if (!filename)
        return (-1);

    // Use platform_open with appropriate flags
    int open_flags = O_WRONLY | O_CREAT | O_TRUNC;
    fd = platform_open(filename, open_flags);
    free(filename);
    if (fd == -1)
        return (-1);

    // Write history entries
    for (node = info->history; node; node = node->next)
    {
        _putsfd(node->str, fd);
        _putfd('\n', fd);
    }
    _putfd(BUF_FLUSH, fd);

    platform_close(fd);
    return (1);
}

/**
 * read_history - reads history from file
 * @info: the parameter struct
 *
 * Return: histcount on success, 0 otherwise
 */
int read_history(info_t *info)
{
    int i, last = 0, linecount = 0;
    ssize_t fd, rdlen;
    long long fsize = 0; // Use long long for platform_stat_get_size
    char *buf = NULL, *filename = NULL;
    platform_stat_t p_stat; // Use stack allocation now

    filename = get_history_file(info);
    if (!filename)
        return (0);

    // Use platform_open (read-only)
    fd = platform_open(filename, O_RDONLY);
    // Need filename for stat, so don't free yet

    if (fd == -1) {
        free(filename);
        return (0);
    }

    // Get file size using platform_stat
    if (platform_stat(filename, &p_stat) == 0) { 
         fsize = platform_stat_get_size(&p_stat);
    } else {
        // Could not get size, assume 0 or fail?
        fsize = 0;
    }
    free(filename); // Free filename now that stat is done

    if (fsize < 2) 
    {
        platform_close(fd);
        return (0);
    }

    buf = malloc(sizeof(char) * (fsize + 1));
    if (!buf)
    {
        platform_close(fd);
        return (0);
    }

    // Use platform_console_read (or a new platform_file_read)
    // For now, assume read works on the fd from platform_open
    // TODO: Replace with platform_read(fd, buf, fsize)
    rdlen = read(fd, buf, fsize); 
    buf[fsize] = 0; // Null terminate based on expected size

    platform_close(fd);

    if (rdlen <= 0)
        return (free(buf), 0);

    // Parse history buffer
    last = 0;
    for (i = 0; i < fsize; i++)
        if (buf[i] == '\n')
        {
            buf[i] = 0;
            build_history_list(info, buf + last, linecount++);
            last = i + 1;
        }
    if (last != i) // Handle last line if no trailing newline
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
