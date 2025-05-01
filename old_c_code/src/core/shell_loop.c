#include "shell.h"
#include "platform/process.h"

/**
 * hsh - main shell loop
 * @info: the parameter & return info struct
 * @av: the argument vector from main()
 * Return: 0 on success, 1 on error, or error code
 */
int hsh(info_t *info, char **av)
{
    ssize_t r = 0;
    int builtin_ret = 0;

    while (r != -1 && builtin_ret != -2)
    {
        clear_info(info);
        if (interactive(info))
        {
            /* Use modern enhanced prompt instead of simple prompt */
            print_prompt_utf8(info);
        }
        _eputchar(BUF_FLUSH);
        r = get_input(info);
        if (r != -1)
        {
            set_info(info, av);
            
            /* Display the command with syntax highlighting for better readability */
            if (interactive(info) && info->arg && *(info->arg))
            {
                write(STDOUT_FILENO, "\r", 1); /* Move cursor to start of line */
                print_highlighted_input(info->arg);
            }
            
            builtin_ret = find_builtin(info);
            if (builtin_ret == -1)
                find_cmd(info);
        }
        else if (interactive(info))
            _putchar('\n');
        free_info(info, 0);
    }
    write_history(info);
    free_info(info, 1);
    if (!interactive(info) && info->status)
        exit(info->status);
    if (builtin_ret == -2)
    {
        if (info->err_num == -1)
            exit(info->status);
        exit(info->err_num);
    }
    return (builtin_ret);
}

/**
 * find_builtin - finds a builtin command
 * @info: the parameter & return info struct
 * Return: -1 if builtin not found,
 * 0 if builtin executed successfully,
 * 1 if builtin found but not successful,
 * 2 if builtin signals exit()
 */
int find_builtin(info_t *info)
{
    int i, built_in_ret = -1;
    builtin_table builtintbl[] = {
        {"exit", _myexit},
        {"env", _myenv},
        {"help", _myhelp},
        {"history", _myhistory},
        {"setenv", _mysetenv},
        {"unsetenv", _myunsetenv},
        {"cd", _mycd},
        {"alias", _myalias},
        {"lang", _mylang},
        {"test", _mytest},
        {"layout", _mylayout},
        {"config", _myconfig},
        {"clear", _myclear},
        {"pwd", _mypwd},
        {"ls", _myls},
        {NULL, NULL}};

    for (i = 0; builtintbl[i].type; i++)
        if (_strcmp(info->argv[0], builtintbl[i].type) == 0)
        {
            info->line_count++;
            built_in_ret = builtintbl[i].func(info);
            break;
        }
    return (built_in_ret);
}

/**
 * find_cmd - finds a command in PATH
 * @info: the parameter & return info struct
 * Return: void
 */
void find_cmd(info_t *info)
{
    char *path = NULL;
    int i, k;

    info->path = info->argv[0];
    if (info->linecount_flag == 1)
    {
        info->line_count++;
        info->linecount_flag = 0;
    }
    for (i = 0, k = 0; info->arg[i]; i++)
        if (!is_delim(info->arg[i], " \t\n"))
            k++;
    if (!k)
        return;

    path = find_path(info, _getenv(info, "PATH="), info->argv[0]);
    if (path)
    {
        info->path = path;
        fork_cmd(info);
    }
    else
    {
        if ((interactive(info) || _getenv(info, "PATH=") || info->argv[0][0] == '/') && is_cmd(info, info->argv[0]))
            fork_cmd(info);
        else if (*(info->arg) != '\n')
        {
            info->status = 127;
            print_error(info, "not found\n");
        }
    }
}

/**
 * fork_cmd - Uses the platform abstraction layer to create and wait for a command.
 * @info: the parameter & return info struct
 * Return: void
 */
void fork_cmd(info_t *info)
{
    platform_process_t *process = NULL;
    int exit_code;
    char **env = get_environ_copy(info);

    // Create the process using the platform abstraction layer
    // Pass the command path (info->path), arguments (info->argv), and environment (env)
    process = platform_create_process(info, info->path, info->argv, env);

    // Free the copied environment strings if they were allocated
    // Note: get_environ() returns info->env_array which is freed by free_info later.
    // Do not free 'env' here.

    if (!process)
    {
        // Error occurred during process creation (already printed by PAL)
        // We might want to set a specific info->status here
        info->status = 127; // Indicate command could not be run
        // Print a generic error? PAL might have already done it.
        // print_error(info, "Failed to start command\n");
        return;
    }

    // Wait for the process to complete and get its exit code
    exit_code = platform_wait_process(process);

    // Store the exit status in the info struct
    info->status = exit_code;

    // Perform platform-specific cleanup (closing handles, etc.)
    platform_cleanup_process(process);

    // Handle specific exit codes (like permission denied)
    if (info->status == 126)
    {
        print_error(info, "Permission denied\n");
    }
    // The POSIX implementation of wait handles WIFSIGNALED, returning 128 + signal
    // No need for specific WIFEXITED checks here as PAL handles it.
}
