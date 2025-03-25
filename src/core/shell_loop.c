#include "shell.h"

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
            print_prompt_utf8(info);
        _eputchar(BUF_FLUSH);
        r = get_input(info);
        if (r != -1)
        {
            set_info(info, av);
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
 * fork_cmd - forks a an exec thread to run cmd
 * @info: the parameter & return info struct
 * Return: void
 */
void fork_cmd(info_t *info)
{
#ifdef WINDOWS
    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    char cmdline[1024];
    int i;

    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));

    // Build command line string
    _snprintf(cmdline, sizeof(cmdline), "%s", info->path);
    for (i = 1; info->argv[i] != NULL; i++)
    {
        _snprintf(cmdline + strlen(cmdline), sizeof(cmdline) - strlen(cmdline),
                  " %s", info->argv[i]);
    }

    if (!CreateProcess(NULL,    // No module name (use command line)
                       cmdline, // Command line
                       NULL,    // Process handle not inheritable
                       NULL,    // Thread handle not inheritable
                       FALSE,   // Set handle inheritance to FALSE
                       0,       // No creation flags
                       NULL,    // Use parent's environment block
                       NULL,    // Use parent's starting directory
                       &si,     // Pointer to STARTUPINFO structure
                       &pi))    // Pointer to PROCESS_INFORMATION structure
    {
        info->status = GetLastError();
        print_error(info, "Command failed\n");
        return;
    }

    // Wait until child process exits
    WaitForSingleObject(pi.hProcess, INFINITE);
    GetExitCodeProcess(pi.hProcess, (LPDWORD) & (info->status));

    // Close process and thread handles
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
#else
    pid_t child_pid;
    int status;

    child_pid = fork();
    if (child_pid == -1)
    {
        perror("Error:");
        return;
    }
    if (child_pid == 0)
    {
        if (execve(info->path, info->argv, get_environ_copy(info)) == -1)
        {
            free_info(info, 1);
            if (errno == EACCES)
                exit(126);
            exit(1);
        }
    }
    else
    {
        wait(&(info->status));
        if (WIFEXITED(info->status))
        {
            info->status = WEXITSTATUS(info->status);
            if (info->status == 126)
                print_error(info, "Permission denied\n");
        }
    }
#endif
}
