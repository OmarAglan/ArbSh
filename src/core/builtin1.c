#include "shell.h"

#ifdef WINDOWS
#include <windows.h>
#else
#include <unistd.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <errno.h>
#include <dirent.h>
#include <time.h>
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
    char *buf, *dir;

    dir = _getenv(info, "HOME=");
    if (!dir)
        return (NULL);
    buf = malloc(sizeof(char) * (_strlen(dir) + 20)); /* Allow space for path + filename */
    if (!buf)
        return (NULL);
    buf[0] = 0;
    _strcpy(buf, dir);
    _strcat(buf, "/");
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
 * _myclear - clears the terminal screen
 * @info: Structure containing potential arguments
 * Return: Always 0
 */
int _myclear(info_t *info)
{
    /* Unused parameter */
    (void)info;
    
#ifdef WINDOWS
    /* Windows-specific clear screen using ANSI escape codes */
    _puts("\033[2J\033[H");
#else
    /* UNIX clear screen using ANSI escape codes */
    _puts("\033[2J\033[H");
#endif
    return (0);
}

/**
 * _mypwd - prints the current working directory
 * @info: Structure containing potential arguments
 * Return: Always 0
 */
int _mypwd(info_t *info)
{
    char cwd[1024];
    char *pwd_str;
    
    /* Unused parameter */
    (void)info;
    
    /* First try getcwd */
    if (getcwd(cwd, sizeof(cwd)) != NULL)
    {
        _puts(cwd);
        _putchar('\n');
        return (0);
    }
    
    /* If getcwd fails, try getting PWD from environment */
    pwd_str = _getenv(info, "PWD=");
    if (pwd_str)
    {
        _puts(pwd_str);
        _putchar('\n');
        return (0);
    }
    
    /* If both methods fail */
    _puts("Error: Could not determine current working directory\n");
    return (1);
}

/**
 * _myls - simple implementation of ls command
 * @info: Structure containing potential arguments
 * Return: 0 on success, 1 on error
 */
int _myls(info_t *info)
{
    char *dir_path = ".";
    int show_hidden = 0;
    int long_format = 0;
    int i;
    
    /* Parse arguments */
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
            /* Non-flag argument is treated as directory path */
            dir_path = info->argv[i];
        }
    }
    
#ifdef WINDOWS
    /* Windows implementation using FindFirstFile/FindNextFile */
    WIN32_FIND_DATA find_data;
    HANDLE find_handle;
    char search_path[1024];
    SYSTEMTIME system_time;
    FILETIME local_file_time;
    
    /* Create search path pattern */
    snprintf(search_path, sizeof(search_path), "%s\\*", dir_path);
    
    /* Start file enumeration */
    find_handle = FindFirstFile(search_path, &find_data);
    if (find_handle == INVALID_HANDLE_VALUE)
    {
        _puts("Cannot access directory: ");
        _puts(dir_path);
        _putchar('\n');
        return (1);
    }
    
    do
    {
        /* Skip hidden files if not showing hidden */
        if (!show_hidden && find_data.cFileName[0] == '.')
            continue;
        
        if (long_format)
        {
            /* Print file type */
            if (find_data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
                _puts("d");
            else
                _puts("-");
            
            /* Print attributes (simplified) */
            _puts((find_data.dwFileAttributes & FILE_ATTRIBUTE_READONLY) ? "r-" : "rw");
            _puts((find_data.dwFileAttributes & FILE_ATTRIBUTE_SYSTEM) ? "s " : "- ");
            
            /* Convert file time to local time and format */
            FileTimeToLocalFileTime(&(find_data.ftLastWriteTime), &local_file_time);
            FileTimeToSystemTime(&local_file_time, &system_time);
            
            /* Print size */
            char size_str[32];
            ULARGE_INTEGER file_size;
            file_size.LowPart = find_data.nFileSizeLow;
            file_size.HighPart = find_data.nFileSizeHigh;
            snprintf(size_str, sizeof(size_str), "%12llu ", file_size.QuadPart);
            _puts(size_str);
            
            /* Print date/time (basic format) */
            char time_str[64];
            snprintf(time_str, sizeof(time_str), "%02d-%02d-%04d %02d:%02d ",
                    system_time.wMonth, system_time.wDay, system_time.wYear,
                    system_time.wHour, system_time.wMinute);
            _puts(time_str);
        }
        
        /* Print file name with color coding */
        if (find_data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
        {
            /* Directory - blue */
            _puts("\033[1;34m");
            _puts(find_data.cFileName);
            _puts("\033[0m");
        }
        else if (is_executable_file(find_data.cFileName))
        {
            /* Executable - green */
            _puts("\033[1;32m");
            _puts(find_data.cFileName);
            _puts("\033[0m");
        }
        else
        {
            /* Regular file - normal color */
            _puts(find_data.cFileName);
        }
        _putchar('\n');
        
    } while (FindNextFile(find_handle, &find_data));
    
    FindClose(find_handle);
#else
    /* UNIX implementation using opendir/readdir */
    DIR *dir;
    struct dirent *entry;
    struct stat file_stat;
    char file_path[1024];
    
    dir = opendir(dir_path);
    if (!dir)
    {
        _puts("Cannot access directory: ");
        _puts(dir_path);
        _putchar('\n');
        return (1);
    }
    
    while ((entry = readdir(dir)) != NULL)
    {
        /* Skip hidden files if not showing hidden */
        if (!show_hidden && entry->d_name[0] == '.')
            continue;
        
        /* Construct full file path for stat */
        snprintf(file_path, sizeof(file_path), "%s/%s", dir_path, entry->d_name);
        
        /* Get file status */
        if (stat(file_path, &file_stat) < 0) 
            continue;  /* Skip files we can't stat */
        
        if (long_format)
        {
            /* Print file type */
            _puts(S_ISDIR(file_stat.st_mode) ? "d" : "-");
            
            /* Print permissions */
            _puts(file_stat.st_mode & S_IRUSR ? "r" : "-");
            _puts(file_stat.st_mode & S_IWUSR ? "w" : "-");
            _puts(file_stat.st_mode & S_IXUSR ? "x" : "-");
            _puts(file_stat.st_mode & S_IRGRP ? "r" : "-");
            _puts(file_stat.st_mode & S_IWGRP ? "w" : "-");
            _puts(file_stat.st_mode & S_IXGRP ? "x" : "-");
            _puts(file_stat.st_mode & S_IROTH ? "r" : "-");
            _puts(file_stat.st_mode & S_IWOTH ? "w" : "-");
            _puts(file_stat.st_mode & S_IXOTH ? "x" : "-");
            _puts(" ");
            
            /* Print size */
            char size_str[32];
            snprintf(size_str, sizeof(size_str), "%8lld ", (long long)file_stat.st_size);
            _puts(size_str);
            
            /* Print date (basic format) */
            char time_str[64];
            struct tm *tm_info = localtime(&file_stat.st_mtime);
            strftime(time_str, sizeof(time_str), "%b %d %H:%M ", tm_info);
            _puts(time_str);
        }
        
        /* Print file name with color coding based on file type from stat() */
        if (S_ISDIR(file_stat.st_mode))
        {
            /* Directory - blue */
            _puts("\033[1;34m");
            _puts(entry->d_name);
            _puts("\033[0m");
        }
        else if (S_ISREG(file_stat.st_mode) && (file_stat.st_mode & S_IXUSR))
        {
            /* Executable - green */
            _puts("\033[1;32m");
            _puts(entry->d_name);
            _puts("\033[0m");
        }
        else
        {
            /* Regular file - normal color */
            _puts(entry->d_name);
        }
        _putchar('\n');
    }
    
    closedir(dir);
#endif
    
    return (0);
}
