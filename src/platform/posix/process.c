#include "platform/process.h"
#include <sys/types.h>
#include <sys/wait.h>
#include <unistd.h>
#include <stdlib.h>
#include <stdio.h>
#include <errno.h>

// Define the actual structure for POSIX process details
struct platform_process_s {
    pid_t pid;          // Process ID
    int exit_status;    // Raw status from waitpid
};

platform_process_t* platform_create_process(info_t *info, const char *command, char * const argv[], char * const envp[])
{
    pid_t child_pid;
    platform_process_t *process = NULL;

    // Allocate memory for our platform process structure
    process = (platform_process_t*)malloc(sizeof(platform_process_t));
    if (!process) {
        perror("platform_create_process: Malloc failed");
        return NULL;
    }
    process->exit_status = -1; // Initialize status

    // Set GUI environment variable for child process (if applicable)
    // TODO: Pass GUI mode status explicitly
    set_gui_env_for_child();

    child_pid = fork();
    if (child_pid == -1)
    {
        perror("platform_create_process: fork failed");
        free(process);
        return NULL;
    }

    if (child_pid == 0)
    {
        // Child process
        // Use envp if provided, otherwise use info->env_array or default environ
        char **child_env = envp ? envp : get_environ_copy(info);

        if (execve(command, argv, child_env) == -1)
        {
            // execve only returns on error
            perror("platform_create_process: execve failed");
            // We need to exit the child process forcefully
            // free_info might not be safe to call here depending on state
            if (errno == EACCES)
                _exit(126);
            _exit(1); // General exec error
        }
        // Child process should not reach here
    }
    else
    {
        // Parent process
        process->pid = child_pid;
        return process;
    }
    return NULL; // Should not be reached
}

int platform_wait_process(platform_process_t *process)
{
    int status;
    int exit_code = -1;

    if (!process || process->pid <= 0) return -1;

    if (waitpid(process->pid, &status, 0) == -1) {
        perror("platform_wait_process: waitpid failed");
        return -1; // Indicate wait error
    }

    process->exit_status = status; // Store raw status

    if (WIFEXITED(status))
    {
        exit_code = WEXITSTATUS(status);
    }
    else if (WIFSIGNALED(status))
    {
        // Process terminated by signal
        // We could return a special value, e.g., 128 + signal number
        exit_code = 128 + WTERMSIG(status);
    }
    // Handle other wait status cases if needed (stopped, continued)

    return exit_code;
}

void platform_cleanup_process(platform_process_t *process)
{
    if (!process) return;
    // Nothing specific to clean up for POSIX fork/exec apart from memory
    free(process);
} 