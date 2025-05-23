#include "shell.h"
#include "platform/filesystem.h"

#ifdef WINDOWS
#include <windows.h>
struct platform_stat_s {
	WIN32_FILE_ATTRIBUTE_DATA file_info;
};
#else
#include <sys/stat.h>
struct platform_stat_s {
	struct stat posix_stat;
};
#endif

/**
 * is_cmd - determines if a file is an executable command using PAL
 * @info: the info struct (unused)
 * @path: path to the file
 *
 * Return: 1 if true, 0 otherwise
 */
int is_cmd(info_t *info, char *path)
{
	platform_stat_t stat_buf;
	(void)info;

	if (!path)
		return (0);

	if (platform_stat(path, &stat_buf) != 0)
	{
		return (0);
	}

	if (!platform_stat_is_regular_file(&stat_buf))
	{
		return (0);
	}

	if (platform_access(path, PLATFORM_X_OK) == 0)
	{
		return (1);
	}

	return (0);
}

/**
 * dup_chars - duplicates characters
 * @pathstr: the PATH string
 * @start: starting index
 * @stop: stopping index
 *
 * Return: pointer to new buffer
 */
char *dup_chars(char *pathstr, int start, int stop)
{
	static char buf[1024];
	int i = 0, k = 0;

	for (k = 0, i = start; i < stop; i++)
		if (pathstr[i] != ':')
			buf[k++] = pathstr[i];
	buf[k] = 0;
	return (buf);
}

/**
 * find_path - finds this cmd in the PATH string
 * @info: the info struct
 * @pathstr: the PATH string
 * @cmd: the cmd to find
 *
 * Return: full path of cmd if found or NULL
 */
char *find_path(info_t *info, char *pathstr, char *cmd)
{
	int i = 0, curr_pos = 0;
	char *path;

	if (!pathstr)
		return (NULL);
	if ((_strlen(cmd) > 2) && starts_with(cmd, "./"))
	{
		if (is_cmd(info, cmd))
			return (cmd);
	}
	while (1)
	{
		if (!pathstr[i] || pathstr[i] == ':')
		{
			path = dup_chars(pathstr, curr_pos, i);
			if (!*path)
				_strcat(path, cmd);
			else
			{
				_strcat(path, "/");
				_strcat(path, cmd);
			}
			if (is_cmd(info, path))
				return (path);
			if (!pathstr[i])
				break;
			curr_pos = i;
		}
		i++;
	}
	return (NULL);
}
