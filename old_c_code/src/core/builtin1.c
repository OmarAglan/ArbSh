#include "shell.h"
#include "platform/console.h" // For output
#include "platform/filesystem.h" // Include Filesystem PAL
#include <limits.h> // For PATH_MAX

#ifdef WINDOWS
#include <windows.h>
// Include the definition directly for Windows
struct platform_stat_s {
    WIN32_FILE_ATTRIBUTE_DATA file_info;
};
#else
#include <unistd.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <errno.h>
#include <dirent.h>
#include <time.h>
// Include the definition directly for POSIX
struct platform_stat_s {
    struct stat posix_stat;
};
#endif

/* _mylayout function is now defined in arabic_input.c */

/**
 * is_executable_file - Checks if a file has an executable extension (Windows)
 * @filename: The filename to check
 * Return: 1 if executable, 0 otherwise
 */
int is_executable_file(const char *filename)
{
    const char *extension = strrchr(filename, '.');
    if (!extension)
        return 0;
    
    /* Convert to lowercase for case-insensitive comparison */
    char ext_lower[10] = {0};
    int i;
    for (i = 0; extension[i] && i < 9; i++)
        ext_lower[i] = (extension[i] >= 'A' && extension[i] <= 'Z') ? 
                       extension[i] + 32 : extension[i];
    
    /* Check for common executable extensions */
    return (strcmp(ext_lower, ".exe") == 0 ||
            strcmp(ext_lower, ".bat") == 0 ||
            strcmp(ext_lower, ".cmd") == 0 ||
            strcmp(ext_lower, ".com") == 0 ||
            strcmp(ext_lower, ".ps1") == 0 ||
            strcmp(ext_lower, ".vbs") == 0 ||
            strcmp(ext_lower, ".msi") == 0);
}

/**
 * get_alias_file - gets the alias file path
 * @info: parameter struct
 * Return: allocated string containing alias file path
 */
char *get_alias_file(info_t *info)
{
    char homedir[PATH_MAX]; // Use POSIX PATH_MAX (limits.h)
    char *buf;

    if (!platform_get_home_dir(homedir, sizeof(homedir)))
        return (NULL);

    buf = malloc(sizeof(char) * (_strlen(homedir) + 20)); // Allow space
    if (!buf)
        return (NULL);
    _strcpy(buf, homedir);
    // TODO: Use platform-specific path separator
    _strcat(buf, "/"); // Assumes POSIX separator for now
    _strcat(buf, ".arbsh_aliases");
    return (buf);
}

/**
 * load_aliases - loads aliases from file at startup
 * @info: the parameter struct
 * Return: 1 on success, 0 on failure
 */
int load_aliases(info_t *info)
{
    char *filename = get_alias_file(info);
    FILE *file;
    char line[512];
    
    if (!filename)
        return (0);

    file = fopen(filename, "r");
    free(filename);

    if (!file)
        return (0); /* File doesn't exist or can't be opened - this is fine */

    while (fgets(line, sizeof(line), file))
    {
        char *newline = strchr(line, '\n');
        if (newline)
            *newline = '\0'; /* Remove trailing newline */

        /* Skip empty lines and comments */
        if (line[0] == '\0' || line[0] == '#')
            continue;

        set_alias(info, line);
    }

    fclose(file);
    return (1);
}

/**
 * save_aliases - saves all aliases to a file
 * @info: the parameter struct
 * Return: 1 on success, 0 on failure
 */
int save_aliases(info_t *info)
{
    char *filename = get_alias_file(info);
    FILE *file;
    list_t *node;

    if (!filename)
        return (0);

    file = fopen(filename, "w");
    free(filename);

    if (!file)
    {
        _puts("Error: Could not save aliases\n");
        return (0);
    }

    fprintf(file, "# ArbSh Aliases\n");
    fprintf(file, "# Format: name=value\n\n");

    node = info->alias;
    while (node)
    {
        fprintf(file, "%s\n", node->str);
        node = node->next;
    }

    fclose(file);
    return (1);
}

/**
 * _myhistory - displays the history list, one command by line, preceded
 *              with line numbers, starting at 0.
 * @info: Structure containing potential arguments. Used to maintain
 *        constant function prototype.
 *  Return: Always 0
 */
int _myhistory(info_t *info)
{
    print_list(info->history);
    return (0);
}

/**
 * unset_alias - sets alias to string
 * @info: parameter struct
 * @str: the string alias
 *
 * Return: Always 0 on success, 1 on error
 */
int unset_alias(info_t *info, char *str)
{
    char *p, c;
    int ret;

    p = _strchr(str, '=');
    if (!p)
        return (1);
    c = *p;
    *p = 0;
    ret = delete_node_at_index(&(info->alias),
                               get_node_index(info->alias, node_starts_with(info->alias, str, -1)));
    *p = c;
    return (ret);
}

/**
 * set_alias - sets alias to string
 * @info: parameter struct
 * @str: the string alias
 *
 * Return: Always 0 on success, 1 on error
 */
int set_alias(info_t *info, char *str)
{
    char *p;

    p = _strchr(str, '=');
    if (!p)
        return (1);
    if (!*++p)
        return (unset_alias(info, str));

    unset_alias(info, str);
    return (add_node_end(&(info->alias), str, 0) == NULL);
}

/**
 * print_alias - prints an alias string
 * @node: the alias node
 *
 * Return: Always 0 on success, 1 on error
 */
int print_alias(list_t *node)
{
    char *p = NULL, *a = NULL;

    if (node)
    {
        p = _strchr(node->str, '=');
        for (a = node->str; a <= p; a++)
            _putchar(*a);
        _putchar('\'');
        _puts(p + 1);
        _puts("'\n");
        return (0);
    }
    return (1);
}

/**
 * _myalias - mimics the alias builtin (man alias)
 * @info: Structure containing potential arguments. Used to maintain
 *          constant function prototype.
 *  Return: Always 0
 */
int _myalias(info_t *info)
{
    int i = 0;
    char *p = NULL;
    list_t *node = NULL;

    if (info->argc == 1)
    {
        node = info->alias;
        while (node)
        {
            print_alias(node);
            node = node->next;
        }
        return (0);
    }

    /* Check for save/load commands */
    if (info->argc == 2 && _strcmp(info->argv[1], "-s") == 0)
    {
        if (save_aliases(info))
            _puts("Aliases saved successfully\n");
        else
            _puts("Error saving aliases\n");
        return (0);
    }
    
    if (info->argc == 2 && _strcmp(info->argv[1], "-l") == 0)
    {
        if (load_aliases(info))
            _puts("Aliases loaded successfully\n");
        else
            _puts("No aliases file found or error loading aliases\n");
        return (0);
    }

    for (i = 1; info->argv[i]; i++)
    {
        p = _strchr(info->argv[i], '=');
        if (p)
            set_alias(info, info->argv[i]);
        else
            print_alias(node_starts_with(info->alias, info->argv[i], '='));
    }

    return (0);
}

/**
 * _myclear - clears the terminal screen using PAL
 * @info: Structure containing potential arguments
 * Return: Always 0
 */
int _myclear(info_t *info)
{
    (void)info; // Unused
    // Use platform write with standard ANSI clear codes
    platform_console_write(PLATFORM_STDOUT_FILENO, "\033[2J\033[H", 6);
    return (0);
}

/**
 * _mypwd - prints the current working directory using PAL
 * @info: Structure containing potential arguments
 * Return: Always 0 on success, 1 on error
 */
int _mypwd(info_t *info)
{
    char cwd[1024];
    (void)info; // Unused

    if (platform_getcwd(cwd, sizeof(cwd)) != NULL)
    {
        _puts(cwd); // _puts uses platform_console_write
        _putchar('\n');
        return (0);
    }
    else
    {
        _eputs("pwd: error retrieving current directory\n"); // _eputs uses PAL
        return (1);
    }
}

/**
 * _myls - simple implementation of ls command using PAL stat
 * @info: Structure containing potential arguments
 * Return: 0 on success, 1 on error
 */
int _myls(info_t *info)
{
    char *dir_path = ".";
    int show_hidden = 0;
    int long_format = 0;
    int i;
    platform_stat_t p_stat; // Now declare directly, size should be known

    // Allocate memory for the stat structure - NO longer needed
    // p_stat_ptr = malloc(sizeof(platform_stat_t));
    // if (!p_stat_ptr) { ... }

    // Argument parsing
    for (i = 1; i < info->argc; i++)
    {
        if (info->argv[i][0] == '-')
        {
            int j = 1;
            while (info->argv[i][j] != '\0')
            {
                if (info->argv[i][j] == 'a')
                    show_hidden = 1;
                else if (info->argv[i][j] == 'l')
                    long_format = 1;
                j++;
            }
        }
        else
        {
            dir_path = info->argv[i];
        }
    }

#ifdef WINDOWS
    // TODO: Abstract directory iteration into the PAL
    // Keep platform-specific block for now
    WIN32_FIND_DATA find_data;
    HANDLE find_handle;
    char search_path[1024];
    char full_path[PATH_MAX]; // Use PATH_MAX

    snprintf(search_path, sizeof(search_path), "%s\\*", dir_path);
    find_handle = FindFirstFile(search_path, &find_data);
    if (find_handle == INVALID_HANDLE_VALUE) {
        _eputs("ls: cannot access "); _eputs(dir_path); _eputs(": No such file or directory\n");
        return (1);
    }

    do {
        if (!show_hidden && find_data.cFileName[0] == '.')
            continue;

        snprintf(full_path, sizeof(full_path), "%s\\%s", dir_path, find_data.cFileName);

        // Use the stack-allocated p_stat
        int stat_result = platform_stat(full_path, &p_stat);

        if (long_format) {
             if (stat_result == 0) {
                 // Use p_stat accessors
                 _puts(platform_stat_is_directory(&p_stat) ? "d" : "-");
                 _puts("rwxrwxrwx ");
                 char size_str[32];
                 snprintf(size_str, sizeof(size_str), "%10lld ", platform_stat_get_size(&p_stat));
                 _puts(size_str);
                 time_t mtime = platform_stat_get_mtime(&p_stat);
                 struct tm *tm_info = localtime(&mtime);
                 if (tm_info) { 
                    char time_str[64];
                    strftime(time_str, sizeof(time_str), "%b %d %H:%M ", tm_info);
                    _puts(time_str);
                 } else { _puts("Jan 01 00:00 "); }
             } else {
                 _puts("??????????          ?? ??? ?? ??:?? "); // Stat failed
             }
        }

        // Print file name with color coding using stat info (check stat_result)
        if (stat_result == 0 && platform_stat_is_directory(&p_stat)) {
            _puts("\033[1;34m"); _puts(find_data.cFileName); _puts("\033[0m");
        } else if (is_executable_file(find_data.cFileName)) {
             _puts("\033[1;32m"); _puts(find_data.cFileName); _puts("\033[0m");
        } else {
            _puts(find_data.cFileName);
        }
        _putchar('\n');

    } while (FindNextFile(find_handle, &find_data));
    FindClose(find_handle);

#else // POSIX
    DIR *dir;
    struct dirent *entry;
    char file_path[PATH_MAX]; // Use PATH_MAX

    dir = opendir(dir_path);
    if (!dir) {
        _eputs("ls: cannot access "); _eputs(dir_path); _eputs(": No such file or directory\n");
        return (1);
    }

    while ((entry = readdir(dir)) != NULL) {
        if (!show_hidden && entry->d_name[0] == '.')
            continue;

        snprintf(file_path, sizeof(file_path), "%s/%s", dir_path, entry->d_name);

        // Use the stack-allocated p_stat
        int stat_result = platform_stat(file_path, &p_stat);
        if (stat_result != 0)
             continue; // Skip files we can't stat

        if (long_format) {
            _puts(platform_stat_is_directory(&p_stat) ? "d" : "-");
            // Permissions (use p_stat.posix_stat directly)
            _puts((p_stat.posix_stat.st_mode & S_IRUSR) ? "r" : "-");
            _puts((p_stat.posix_stat.st_mode & S_IWUSR) ? "w" : "-");
            _puts((p_stat.posix_stat.st_mode & S_IXUSR) ? "x" : "-");
            _puts((p_stat.posix_stat.st_mode & S_IRGRP) ? "r" : "-");
            _puts((p_stat.posix_stat.st_mode & S_IWGRP) ? "w" : "-");
            _puts((p_stat.posix_stat.st_mode & S_IXGRP) ? "x" : "-");
            _puts((p_stat.posix_stat.st_mode & S_IROTH) ? "r" : "-");
            _puts((p_stat.posix_stat.st_mode & S_IWOTH) ? "w" : "-");
            _puts((p_stat.posix_stat.st_mode & S_IXOTH) ? "x" : "-");
            _puts(" ");
            // Size
            char size_str[32];
            snprintf(size_str, sizeof(size_str), "%10lld ", platform_stat_get_size(&p_stat));
            _puts(size_str);
            // Time
            time_t mtime = platform_stat_get_mtime(&p_stat);
            struct tm *tm_info = localtime(&mtime);
             if (tm_info) { 
                char time_str[64];
                strftime(time_str, sizeof(time_str), "%b %d %H:%M ", tm_info);
                _puts(time_str);
             } else { _puts("Jan 01 00:00 "); }
        }

        // Print file name with color coding using stat info
        if (platform_stat_is_directory(&p_stat)) {
             _puts("\033[1;34m"); _puts(entry->d_name); _puts("\033[0m");
        } else if (platform_stat_is_executable(&p_stat)) {
             _puts("\033[1;32m"); _puts(entry->d_name); _puts("\033[0m");
        } else {
             _puts(entry->d_name);
        }
        _putchar('\n');
    }
    closedir(dir);
#endif

    // free(p_stat_ptr); // No longer needed
    return (0);
}
