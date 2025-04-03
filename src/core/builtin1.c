#include "shell.h"

#ifdef WINDOWS
#include <windows.h>
#else
#include <unistd.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <errno.h>
#endif

/* _mylayout function is now defined in arabic_input.c */

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
