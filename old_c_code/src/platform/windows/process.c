#include "platform/process.h"
#include <windows.h>
#include <stdio.h>
#include <string.h>

// Define the actual structure for Windows process details
struct platform_process_s {
    PROCESS_INFORMATION process_info; // Holds process and thread handles
    DWORD exit_code;
};

platform_process_t* platform_create_process(info_t *info, const char *command, char * const argv[], char * const envp[])
{
    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    char cmdline[2048]; // Increased buffer size for command line
    int i;
    platform_process_t *process = NULL;
    char *current_env = NULL;

    // Allocate memory for our platform process structure
    process = (platform_process_t*)malloc(sizeof(platform_process_t));
    if (!process) {
        // Use _eputs for error messages if available, otherwise fprintf
        _eputs("platform_create_process: Malloc failed\n");
        return NULL;
    }

    ZeroMemory(process, sizeof(platform_process_t));
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));

    // Build command line string carefully
    // Ensure proper quoting for arguments with spaces
    snprintf(cmdline, sizeof(cmdline), "\"%s\"", command); // Quote the command itself
    for (i = 1; argv[i] != NULL; i++)
    {
        size_t current_len = strlen(cmdline);
        // Check if argument contains spaces and needs quoting
        if (strchr(argv[i], ' ') != NULL) {
            snprintf(cmdline + current_len, sizeof(cmdline) - current_len,
                     " \"%s\"", argv[i]);
        }
        else {
             snprintf(cmdline + current_len, sizeof(cmdline) - current_len,
                      " %s", argv[i]);
        }
        // Check for potential buffer overflow
        if (strlen(cmdline) >= sizeof(cmdline) - 1) {
            _eputs("platform_create_process: Command line too long\n");
            free(process);
            return NULL;
        }
    }

    // Prepare environment block if envp is provided (Windows expects a specific format)
    // If envp is NULL, CreateProcess uses the parent's environment.
    // Creating the environment block manually is complex; for now, use parent's.
    // TODO: Implement environment block creation from envp if needed.
    if (envp) {
        _eputs("platform_create_process: Custom environment not yet supported on Windows\n");
        // For now, proceed using parent's environment block by passing NULL
    }

    if (!CreateProcess(NULL,        // No module name (use command line)
                       cmdline,     // Command line
                       NULL,        // Process handle not inheritable
                       NULL,        // Thread handle not inheritable
                       FALSE,       // Set handle inheritance to FALSE
                       CREATE_UNICODE_ENVIRONMENT, // Use Unicode environment if possible
                       NULL,        // Environment block (NULL = parent's)
                       NULL,        // Use parent's starting directory
                       &si,         // Pointer to STARTUPINFO structure
                       &pi))        // Pointer to PROCESS_INFORMATION structure
    {
        DWORD error_code = GetLastError();
        char err_msg[100];
        snprintf(err_msg, sizeof(err_msg), "platform_create_process: CreateProcess failed (%lu)\n", error_code);
        _eputs(err_msg);
        free(process);
        return NULL;
    }

    // Store process information in our structure
    process->process_info = pi;
    process->exit_code = STILL_ACTIVE; // Initialize exit code

    return process;
}

int platform_wait_process(platform_process_t *process)
{
    if (!process) return -1;

    // Wait for the process to exit
    WaitForSingleObject(process->process_info.hProcess, INFINITE);

    // Get the exit code
    if (!GetExitCodeProcess(process->process_info.hProcess, &process->exit_code)) {
        char err_msg[100];
        snprintf(err_msg, sizeof(err_msg), "platform_wait_process: GetExitCodeProcess failed (%lu)\n", GetLastError());
        _eputs(err_msg);
        // Assign a default error code if GetExitCodeProcess fails
        process->exit_code = -1; // Or some other indicator of failure
    }

    return (int)process->exit_code;
}

void platform_cleanup_process(platform_process_t *process)
{
    if (!process) return;

    // Close process and thread handles
    if (process->process_info.hProcess) {
        CloseHandle(process->process_info.hProcess);
    }
    if (process->process_info.hThread) {
        CloseHandle(process->process_info.hThread);
    }

    // Free the memory allocated for our structure
    free(process);
}

long platform_getpid(void)
{
    // Windows API function to get current process ID
    return (long)GetCurrentProcessId();
} 