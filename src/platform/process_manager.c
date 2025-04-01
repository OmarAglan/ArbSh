/**
 * process_manager.c - Process spawning and communication system implementation
 *
 * This file provides the implementation for the process management system
 * that creates shell instances as child processes and handles communication with them.
 */

#include "process_manager.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

#ifdef WINDOWS
#include <windows.h>
#include <io.h>
#include <fcntl.h>
#else
#include <unistd.h>
#include <sys/wait.h>
#include <errno.h>
#include <sys/ioctl.h>
#include <termios.h>
#include <signal.h>
#endif

#define DEFAULT_BUFFER_SIZE 4096

/**
 * Initialize a shell process structure
 * 
 * @param process Shell process structure to initialize
 */
static void init_shell_process(shell_process_t *process) {
    if (!process)
        return;
        
    memset(process, 0, sizeof(shell_process_t));
    process->pid = -1;
    process->is_running = false;
    process->exit_code = -1;
    
#ifdef WINDOWS
    process->hProcess = INVALID_HANDLE_VALUE;
    process->hThread = INVALID_HANDLE_VALUE;
    
    process->stdin_pipe.read = INVALID_HANDLE_VALUE;
    process->stdin_pipe.write = INVALID_HANDLE_VALUE;
    process->stdout_pipe.read = INVALID_HANDLE_VALUE;
    process->stdout_pipe.write = INVALID_HANDLE_VALUE;
    process->stderr_pipe.read = INVALID_HANDLE_VALUE;
    process->stderr_pipe.write = INVALID_HANDLE_VALUE;
#else
    process->stdin_pipe.read = -1;
    process->stdin_pipe.write = -1;
    process->stdout_pipe.read = -1;
    process->stdout_pipe.write = -1;
    process->stderr_pipe.read = -1;
    process->stderr_pipe.write = -1;
#endif

    process->output_buffer = NULL;
    process->buffer_size = 0;
    process->buffer_used = 0;
}

/**
 * Create a new shell process
 */
bool create_shell_process(shell_process_t *process, const char *command, char *const args[], char *const env[]) {
    if (!process)
        return false;
        
    // Initialize the process structure
    init_shell_process(process);
    
    // Allocate output buffer
    process->buffer_size = DEFAULT_BUFFER_SIZE;
    process->output_buffer = (char *)malloc(process->buffer_size);
    if (!process->output_buffer) {
        return false;
    }
    process->buffer_used = 0;
    
#ifdef WINDOWS
    SECURITY_ATTRIBUTES sa;
    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    char cmdline[32768] = {0};  // Maximum Windows command line length
    
    // Setup security attributes for pipe inheritance
    sa.nLength = sizeof(SECURITY_ATTRIBUTES);
    sa.bInheritHandle = TRUE;
    sa.lpSecurityDescriptor = NULL;
    
    // Create pipes for stdin, stdout, stderr
    if (!CreatePipe(&process->stdin_pipe.read, &process->stdin_pipe.write, &sa, 0) ||
        !CreatePipe(&process->stdout_pipe.read, &process->stdout_pipe.write, &sa, 0) ||
        !CreatePipe(&process->stderr_pipe.read, &process->stderr_pipe.write, &sa, 0)) {
        cleanup_shell_process(process);
        return false;
    }
    
    // Ensure the parent process doesn't inherit the child sides of the pipes
    if (!SetHandleInformation(process->stdin_pipe.write, HANDLE_FLAG_INHERIT, 0) ||
        !SetHandleInformation(process->stdout_pipe.read, HANDLE_FLAG_INHERIT, 0) ||
        !SetHandleInformation(process->stderr_pipe.read, HANDLE_FLAG_INHERIT, 0)) {
        cleanup_shell_process(process);
        return false;
    }
    
    // Setup startup info
    ZeroMemory(&si, sizeof(STARTUPINFO));
    si.cb = sizeof(STARTUPINFO);
    si.dwFlags = STARTF_USESTDHANDLES | STARTF_USESHOWWINDOW;
    si.hStdInput = process->stdin_pipe.read;
    si.hStdOutput = process->stdout_pipe.write;
    si.hStdError = process->stderr_pipe.write;
    si.wShowWindow = SW_HIDE;  // Hide the console window
    
    // Use default shell if command is NULL
    if (!command) {
        command = "cmd.exe";
    }
    
    // Build command line
    strncpy(cmdline, command, sizeof(cmdline) - 1);
    
    // Add arguments if provided
    if (args && args[0]) {
        int cmdlen = strlen(cmdline);
        cmdline[cmdlen++] = ' ';
        
        for (int i = 0; args[i] != NULL && cmdlen < sizeof(cmdline) - 1; i++) {
            // Add space between arguments
            if (i > 0 && cmdlen < sizeof(cmdline) - 1) {
                cmdline[cmdlen++] = ' ';
            }
            
            // Copy argument with quoting if it contains spaces
            if (strchr(args[i], ' ')) {
                if (cmdlen < sizeof(cmdline) - 1) cmdline[cmdlen++] = '"';
                
                int arglen = strlen(args[i]);
                for (int j = 0; j < arglen && cmdlen < sizeof(cmdline) - 1; j++) {
                    cmdline[cmdlen++] = args[i][j];
                }
                
                if (cmdlen < sizeof(cmdline) - 1) cmdline[cmdlen++] = '"';
            } else {
                // No spaces, copy directly
                int arglen = strlen(args[i]);
                for (int j = 0; j < arglen && cmdlen < sizeof(cmdline) - 1; j++) {
                    cmdline[cmdlen++] = args[i][j];
                }
            }
        }
        
        cmdline[cmdlen] = '\0';
    }
    
    // Create the child process
    if (!CreateProcess(
        NULL,              // No module name (use command line)
        cmdline,           // Command line
        NULL,              // Process handle not inheritable
        NULL,              // Thread handle not inheritable
        TRUE,              // Inherit handles
        CREATE_NEW_CONSOLE,// Creation flags
        NULL,              // Use parent's environment block
        NULL,              // Use parent's starting directory
        &si,               // Pointer to STARTUPINFO structure
        &pi))              // Pointer to PROCESS_INFORMATION structure
    {
        cleanup_shell_process(process);
        return false;
    }
    
    // Store process information
    process->pid = pi.dwProcessId;
    process->hProcess = pi.hProcess;
    process->hThread = pi.hThread;
    process->is_running = true;
    
    // Close child side of pipes
    CloseHandle(process->stdin_pipe.read);
    process->stdin_pipe.read = INVALID_HANDLE_VALUE;
    
    CloseHandle(process->stdout_pipe.write);
    process->stdout_pipe.write = INVALID_HANDLE_VALUE;
    
    CloseHandle(process->stderr_pipe.write);
    process->stderr_pipe.write = INVALID_HANDLE_VALUE;
    
    return true;
#else
    // Unix implementation will go here
    return false;
#endif
}

/**
 * Read available output from the shell process
 */
int read_shell_output(shell_process_t *process, char *buffer, int size, int timeout_ms) {
    if (!process || !buffer || size <= 0) {
        return -1;
    }
    
#ifdef WINDOWS
    DWORD bytes_read = 0;
    DWORD bytes_available = 0;
    
    // Check if there's data available to read from stdout
    if (!PeekNamedPipe(process->stdout_pipe.read, NULL, 0, NULL, &bytes_available, NULL)) {
        return -1;
    }
    
    // If no data and we have a timeout, wait for data
    if (bytes_available == 0 && timeout_ms != 0) {
        DWORD wait_result = WaitForSingleObject(process->stdout_pipe.read, 
                                               timeout_ms == -1 ? INFINITE : (DWORD)timeout_ms);
        if (wait_result != WAIT_OBJECT_0) {
            return 0; // No data available within timeout
        }
        
        // Check again after waiting
        if (!PeekNamedPipe(process->stdout_pipe.read, NULL, 0, NULL, &bytes_available, NULL)) {
            return -1;
        }
    }
    
    // If we have data, read it
    if (bytes_available > 0) {
        // Limit bytes to read to buffer size
        DWORD bytes_to_read = (bytes_available < (DWORD)size) ? bytes_available : (DWORD)size;
        
        if (!ReadFile(process->stdout_pipe.read, buffer, bytes_to_read, &bytes_read, NULL)) {
            return -1;
        }
        
        return (int)bytes_read;
    }
    
    return 0; // No data available
#else
    // Unix implementation will go here
    return -1;
#endif
}

/**
 * Write input to the shell process
 */
int write_shell_input(shell_process_t *process, const char *buffer, int size) {
    if (!process || !buffer || size <= 0) {
        return -1;
    }
    
#ifdef WINDOWS
    DWORD bytes_written = 0;
    
    if (!WriteFile(process->stdin_pipe.write, buffer, (DWORD)size, &bytes_written, NULL)) {
        return -1;
    }
    
    return (int)bytes_written;
#else
    // Unix implementation will go here
    return -1;
#endif
}

/**
 * Check if the shell process is still running
 */
bool is_shell_process_running(shell_process_t *process) {
    if (!process) {
        return false;
    }
    
#ifdef WINDOWS
    if (process->hProcess == INVALID_HANDLE_VALUE) {
        return false;
    }
    
    DWORD exit_code;
    if (!GetExitCodeProcess(process->hProcess, &exit_code)) {
        return false;
    }
    
    if (exit_code == STILL_ACTIVE) {
        return true;
    }
    
    // Process has exited, update state
    process->is_running = false;
    process->exit_code = (int)exit_code;
    
    return false;
#else
    // Unix implementation will go here
    return false;
#endif
}

/**
 * Get the exit code of the shell process
 */
int get_shell_process_exit_code(shell_process_t *process) {
    if (!process) {
        return -1;
    }
    
    // If we already know the process has exited, return the cached exit code
    if (!process->is_running && process->exit_code != -1) {
        return process->exit_code;
    }
    
#ifdef WINDOWS
    if (process->hProcess == INVALID_HANDLE_VALUE) {
        return -1;
    }
    
    DWORD exit_code;
    if (!GetExitCodeProcess(process->hProcess, &exit_code)) {
        return -1;
    }
    
    if (exit_code == STILL_ACTIVE) {
        return -1; // Process is still running
    }
    
    // Process has exited, update state
    process->is_running = false;
    process->exit_code = (int)exit_code;
    
    return process->exit_code;
#else
    // Unix implementation will go here
    return -1;
#endif
}

/**
 * Terminate the shell process
 */
bool terminate_shell_process(shell_process_t *process, bool force) {
    if (!process) {
        return false;
    }
    
    // If process is not running, nothing to do
    if (!process->is_running) {
        return true;
    }
    
#ifdef WINDOWS
    if (process->hProcess == INVALID_HANDLE_VALUE) {
        return false;
    }
    
    BOOL result = FALSE;
    
    if (force) {
        // Forcefully terminate process
        result = TerminateProcess(process->hProcess, 1);
    } else {
        // Try to gracefully exit the process by closing its console
        // This will generate a CTRL_CLOSE_EVENT
        HWND hwnd = NULL;
        DWORD processId = GetProcessId(process->hProcess);
        
        // First try to find the console window
        BOOL found = FALSE;
        EnumWindows([](HWND hwnd, LPARAM lParam) -> BOOL {
            DWORD pid = 0;
            GetWindowThreadProcessId(hwnd, &pid);
            if (pid == *(DWORD*)lParam) {
                *(HWND*)(lParam + sizeof(DWORD)) = hwnd;
                return FALSE;  // Stop enumeration
            }
            return TRUE;  // Continue enumeration
        }, (LPARAM)&processId);
        
        if (hwnd) {
            // Send WM_CLOSE to the window
            result = (SendMessage(hwnd, WM_CLOSE, 0, 0) == 0);
            
            // Wait a bit for the process to exit
            if (WaitForSingleObject(process->hProcess, 1000) == WAIT_TIMEOUT) {
                // If process didn't exit, force kill it
                result = TerminateProcess(process->hProcess, 1);
            } else {
                result = TRUE;
            }
        } else {
            // No window found, use TerminateProcess
            result = TerminateProcess(process->hProcess, 1);
        }
    }
    
    if (result) {
        // Get exit code
        DWORD exit_code;
        if (GetExitCodeProcess(process->hProcess, &exit_code)) {
            process->exit_code = (int)exit_code;
        }
        
        process->is_running = false;
        return true;
    }
    
    return false;
#else
    // Unix implementation will go here
    return false;
#endif
}

/**
 * Cleanup resources associated with the shell process
 */
void cleanup_shell_process(shell_process_t *process) {
    if (!process) {
        return;
    }
    
    // First terminate the process if it's still running
    if (process->is_running) {
        terminate_shell_process(process, true);
    }
    
#ifdef WINDOWS
    // Close handles
    if (process->hProcess != INVALID_HANDLE_VALUE) {
        CloseHandle(process->hProcess);
        process->hProcess = INVALID_HANDLE_VALUE;
    }
    
    if (process->hThread != INVALID_HANDLE_VALUE) {
        CloseHandle(process->hThread);
        process->hThread = INVALID_HANDLE_VALUE;
    }
    
    // Close pipe handles
    if (process->stdin_pipe.read != INVALID_HANDLE_VALUE) {
        CloseHandle(process->stdin_pipe.read);
        process->stdin_pipe.read = INVALID_HANDLE_VALUE;
    }
    
    if (process->stdin_pipe.write != INVALID_HANDLE_VALUE) {
        CloseHandle(process->stdin_pipe.write);
        process->stdin_pipe.write = INVALID_HANDLE_VALUE;
    }
    
    if (process->stdout_pipe.read != INVALID_HANDLE_VALUE) {
        CloseHandle(process->stdout_pipe.read);
        process->stdout_pipe.read = INVALID_HANDLE_VALUE;
    }
    
    if (process->stdout_pipe.write != INVALID_HANDLE_VALUE) {
        CloseHandle(process->stdout_pipe.write);
        process->stdout_pipe.write = INVALID_HANDLE_VALUE;
    }
    
    if (process->stderr_pipe.read != INVALID_HANDLE_VALUE) {
        CloseHandle(process->stderr_pipe.read);
        process->stderr_pipe.read = INVALID_HANDLE_VALUE;
    }
    
    if (process->stderr_pipe.write != INVALID_HANDLE_VALUE) {
        CloseHandle(process->stderr_pipe.write);
        process->stderr_pipe.write = INVALID_HANDLE_VALUE;
    }
#else
    // Unix cleanup implementation will go here
#endif

    // Free output buffer
    if (process->output_buffer) {
        free(process->output_buffer);
        process->output_buffer = NULL;
    }
    
    // Reset state
    process->pid = -1;
    process->is_running = false;
    process->buffer_size = 0;
    process->buffer_used = 0;
}

/**
 * Resize the terminal of the shell process
 */
bool resize_shell_terminal(shell_process_t *process, int width, int height) {
    if (!process || !process->is_running) {
        return false;
    }
    
#ifdef WINDOWS
    // On Windows, this is done via the console API
    // Find the console window
    HWND hwnd = NULL;
    DWORD processId = GetProcessId(process->hProcess);
    
    // First try to find the console window
    EnumWindows([](HWND hwnd, LPARAM lParam) -> BOOL {
        DWORD pid = 0;
        GetWindowThreadProcessId(hwnd, &pid);
        if (pid == *(DWORD*)lParam) {
            *(HWND*)(lParam + sizeof(DWORD)) = hwnd;
            return FALSE;  // Stop enumeration
        }
        return TRUE;  // Continue enumeration
    }, (LPARAM)&processId);
    
    if (!hwnd) {
        return false;
    }
    
    // Get console handle
    HANDLE hConsole = CreateFile(
        "CONOUT$",
        GENERIC_READ | GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        OPEN_EXISTING,
        0,
        NULL);
    
    if (hConsole == INVALID_HANDLE_VALUE) {
        return false;
    }
    
    // Set console screen buffer size
    COORD size;
    size.X = (SHORT)width;
    size.Y = (SHORT)height;
    
    BOOL result = SetConsoleScreenBufferSize(hConsole, size);
    CloseHandle(hConsole);
    
    return result;
#else
    // Unix implementation will go here
    return false;
#endif
} 