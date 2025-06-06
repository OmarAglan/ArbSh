#include "shell.h"

/**
 * _myenv - prints the current environment
 * @info: Structure containing potential arguments. Used to maintain
 *          constant function prototype.
 * Return: Always 0
 */
int _myenv(info_t *info)
{
    print_list_str(info->env);
    return (0);
}

/**
 * _getenv - gets the value of an environment variable
 * @info: Structure containing potential arguments. Used to maintain
 * @name: env var name
 *
 * Return: the value or NULL if not found
 */
char *_getenv(info_t *info, const char *name)
{
    list_t *node = info->env;
    const char *p = NULL;

    while (node)
    {
        p = starts_with(node->str, name);
        if (p && *p)
            return ((char*)p);
        node = node->next;
    }
    return (NULL);
}

/**
 * _mysetenv - Initialize a new environment variable,
 *             or modify an existing one
 * @info: Structure containing potential arguments. Used to maintain
 *        constant function prototype.
 * Return: Always 0
 */
int _mysetenv(info_t *info)
{
    if (info->argc != 3)
    {
        _eputs("Incorrect number of arguments\n");
        return (1);
    }
    if (_setenv(info, info->argv[1], info->argv[2]))
        return (0);
    return (1);
}

/**
 * _myunsetenv - Remove an environment variable
 * @info: Structure containing potential arguments. Used to maintain
 *        constant function prototype.
 * Return: Always 0
 */
int _myunsetenv(info_t *info)
{
    int i;

    if (info->argc == 1)
    {
        _eputs("Too few arguments.\n");
        return (1);
    }
    for (i = 1; i < info->argc; i++)
        _unsetenv(info, info->argv[i]);

    return (0);
}

/**
 * populate_env_list - populates env linked list
 * @info: Structure containing potential arguments. Used to maintain
 *          constant function prototype.
 * Return: Always 0
 */
int populate_env_list(info_t *info)
{
    list_t *node = NULL;
    size_t i;
    char **env = shell_environ;

    for (i = 0; env && env[i]; i++)
        add_node_end(&node, env[i], 0);
    info->env = node;
    return (0);
}
