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
 #include <io.h>    // For _close if needed, prefer CloseHandle
 #include <fcntl.h> // For file constants if needed
 #else
 // Includes for Unix/Linux process management
 #include <unistd.h>
 #include <sys/types.h>
 #include <sys/wait.h>
 #include <sys/ioctl.h>
 #include <termios.h>
 #include <errno.h>
 #include <signal.h>
 // #include <pty.h> // For pseudo-terminals (more advanced)
 // #include <utmp.h> // For pseudo-terminals
 #endif
 
 #define DEFAULT_BUFFER_SIZE 4096 // Default size for internal buffers if needed
 
 /**
  * Initialize a shell process structure
  *
  * @param process Shell process structure to initialize
  */
 void init_shell_process(shell_process_t *process)
 {
     if (!process)
         return;
 
     memset(process, 0, sizeof(shell_process_t));
     process->pid = -1;
     process->is_running = false;
     process->exit_code = -1; // Use -1 to indicate not exited or error
 
 #ifdef WINDOWS
     process->hProcess = INVALID_HANDLE_VALUE;
     process->hThread = INVALID_HANDLE_VALUE;
     process->stdin_pipe.read = INVALID_HANDLE_VALUE;
     process->stdin_pipe.write = INVALID_HANDLE_VALUE;
     process->stdout_pipe.read = INVALID_HANDLE_VALUE;
     process->stdout_pipe.write = INVALID_HANDLE_VALUE;
     process->stderr_pipe.read = INVALID_HANDLE_VALUE; // Not currently used but good practice
     process->stderr_pipe.write = INVALID_HANDLE_VALUE;
 #else
     // Unix/Linux pipe initialization
     process->stdin_pipe.read = -1;
     process->stdin_pipe.write = -1;
     process->stdout_pipe.read = -1;
     process->stdout_pipe.write = -1;
     process->stderr_pipe.read = -1;
     process->stderr_pipe.write = -1;
 #endif
 
     // We removed the output buffer from shell_process_t, terminal_tab manages it
     // process->output_buffer = NULL;
     // process->buffer_size = 0;
     // process->buffer_used = 0;
 }
 
 /**
  * Create a new shell process
  */
 bool create_shell_process(shell_process_t *process, const char *command, char *const args[], char *const env[])
 {
     (void)env; // Mark env as unused for now in Windows version
 
     if (!process)
     {
         fprintf(stderr, "Error: create_shell_process called with NULL process pointer.\n");
         return false;
     }
 
     // Initialize the process structure
     init_shell_process(process);
 
     // We removed the output buffer from shell_process_t
     // // Allocate output buffer
     // process->buffer_size = DEFAULT_BUFFER_SIZE;
     // process->output_buffer = (char *)malloc(process->buffer_size);
     // if (!process->output_buffer)
     // {
     //     return false;
     // }
     // process->buffer_used = 0;
 
 #ifdef WINDOWS
     SECURITY_ATTRIBUTES sa;
     STARTUPINFOA si; // Use STARTUPINFOA for char* cmdline
     PROCESS_INFORMATION pi;
     char cmdline[32768] = {0}; // Maximum Windows command line length
 
     // Setup security attributes for pipe inheritance
     sa.nLength = sizeof(SECURITY_ATTRIBUTES);
     sa.bInheritHandle = TRUE;
     sa.lpSecurityDescriptor = NULL;
 
     // Create pipes for stdin, stdout
     // Note: We are ignoring stderr pipe creation/handling for simplicity now
     if (!CreatePipe(&process->stdin_pipe.read, &process->stdin_pipe.write, &sa, 0))
     {
         fprintf(stderr, "Error: CreatePipe stdin failed (%lu)\n", GetLastError());
         cleanup_shell_process(process);
         return false;
     }
     if (!CreatePipe(&process->stdout_pipe.read, &process->stdout_pipe.write, &sa, 0))
     {
         fprintf(stderr, "Error: CreatePipe stdout failed (%lu)\n", GetLastError());
         cleanup_shell_process(process); // Cleans up stdin pipe too
         return false;
     }
     // if (!CreatePipe(&process->stderr_pipe.read, &process->stderr_pipe.write, &sa, 0)) { // Ignore stderr pipe
     //    cleanup_shell_process(process);
     //    return false;
     // }
 
     // Ensure the parent process doesn't inherit the handles it shouldn't
     if (!SetHandleInformation(process->stdin_pipe.write, HANDLE_FLAG_INHERIT, 0))
     { // Parent writes to child stdin
         fprintf(stderr, "Error: SetHandleInformation stdin_write failed (%lu)\n", GetLastError());
         cleanup_shell_process(process);
         return false;
     }
     if (!SetHandleInformation(process->stdout_pipe.read, HANDLE_FLAG_INHERIT, 0))
     { // Parent reads from child stdout
         fprintf(stderr, "Error: SetHandleInformation stdout_read failed (%lu)\n", GetLastError());
         cleanup_shell_process(process);
         return false;
     }
     // if (!SetHandleInformation(process->stderr_pipe.read, HANDLE_FLAG_INHERIT, 0)) { // Ignore stderr pipe
     //     cleanup_shell_process(process);
     //     return false;
     // }
 
     // Setup startup info
     ZeroMemory(&si, sizeof(STARTUPINFOA));
     si.cb = sizeof(STARTUPINFOA);
     si.dwFlags = STARTF_USESTDHANDLES | STARTF_USESHOWWINDOW;
     si.hStdInput = process->stdin_pipe.read;    // Child reads from this end
     si.hStdOutput = process->stdout_pipe.write; // Child writes to this end
     si.hStdError = process->stdout_pipe.write;  // Redirect child stderr to stdout pipe for simplicity
     // si.hStdError = process->stderr_pipe.write; // Or use separate stderr pipe if created
     si.wShowWindow = SW_HIDE; // Hide the child console window
 
     // Use default shell (cmd.exe) if command is NULL or empty
     if (!command || command[0] == '\0')
     {
         // Find cmd.exe path reliably
         char sysDir[MAX_PATH];
         if (GetSystemDirectoryA(sysDir, MAX_PATH) > 0)
         {
             snprintf(cmdline, sizeof(cmdline) - 1, "%s\\cmd.exe", sysDir);
             command = cmdline; // Point command to the full path
         }
         else
         {
             command = "cmd.exe"; // Fallback
         }
         strncpy(cmdline, command, sizeof(cmdline) - 1); // Copy path to cmdline
         cmdline[sizeof(cmdline) - 1] = '\0';            // Ensure null termination
     }
     else
     {
         // Copy the provided command
         strncpy(cmdline, command, sizeof(cmdline) - 1);
         cmdline[sizeof(cmdline) - 1] = '\0';
     }
 
     // Build command line arguments string
     // Note: CreateProcess modifies the string buffer passed as lpCommandLine,
     // so we build it carefully. If args is NULL, cmdline just contains the command.
     if (args && args[0])
     {
         // We need mutable storage if args are const char* const[]
         // For now, assume args are char* const[] as per signature
         size_t current_len = strlen(cmdline);
 
         for (int i = 0; args[i] != NULL; i++)
         {
             // Ensure space before argument (if not the first part)
             if (current_len > 0 && current_len < sizeof(cmdline) - 1)
             {
                 cmdline[current_len++] = ' ';
             }
 
             const char *arg = args[i];
             size_t arg_len = strlen(arg);
             bool needs_quotes = strchr(arg, ' ') != NULL || strchr(arg, '\t') != NULL || arg_len == 0;
 
             if (needs_quotes && current_len < sizeof(cmdline) - 1)
             {
                 cmdline[current_len++] = '"';
             }
 
             // Copy argument, handling backslashes and quotes if necessary (simplified)
             for (size_t j = 0; j < arg_len && current_len < sizeof(cmdline) - 1; ++j)
             {
                 // Basic escaping might be needed for complex cases, but often not required
                 // if the whole argument is quoted.
                 cmdline[current_len++] = arg[j];
             }
 
             if (needs_quotes && current_len < sizeof(cmdline) - 1)
             {
                 cmdline[current_len++] = '"';
             }
             cmdline[current_len] = '\0'; // Null terminate after each part
         }
     }
 
     // Environment block handling (simplified: using parent's for now)
     // To pass custom 'env', you'd format it into a null-terminated block
     // LPVOID envBlock = NULL;
     // if (env) { /* Format env into envBlock */ }
 
     // Create the child process
     // CreateProcess takes a non-const lpCommandLine, so we pass cmdline directly.
     // Ensure cmdline is null-terminated.
     if (!CreateProcessA(      // Use CreateProcessA for char* cmdline
             NULL,             // lpApplicationName - NULL if using command line
             cmdline,          // lpCommandLine - Command line (will be modified!)
             NULL,             // lpProcessAttributes
             NULL,             // lpThreadAttributes
             TRUE,             // bInheritHandles - MUST be TRUE for pipes
             CREATE_NO_WINDOW, // dwCreationFlags - Don't create a visible console window for the child
             // CREATE_NEW_CONSOLE, // Use this if you want a separate (hidden) console
             NULL, // lpEnvironment - NULL uses parent's environment
             NULL, // lpCurrentDirectory - NULL uses parent's CWD
             &si,  // lpStartupInfo
             &pi)) // lpProcessInformation
     {
         fprintf(stderr, "Error: CreateProcess failed for command '%s' (%lu)\n", cmdline, GetLastError());
         cleanup_shell_process(process);
         return false;
     }
 
     // Store process information
     process->pid = pi.dwProcessId;
     process->hProcess = pi.hProcess;
     process->hThread = pi.hThread; // Store thread handle too
     process->is_running = true;
     process->exit_code = -1; // Reset exit code
 
     // Close the handles that the child process inherited, but the parent doesn't need
     CloseHandle(process->stdin_pipe.read);
     process->stdin_pipe.read = INVALID_HANDLE_VALUE;
 
     CloseHandle(process->stdout_pipe.write);
     process->stdout_pipe.write = INVALID_HANDLE_VALUE;
 
     // CloseHandle(process->stderr_pipe.write); // Ignore stderr pipe
     // process->stderr_pipe.write = INVALID_HANDLE_VALUE;
 
     printf("Successfully created process PID: %d for command: %s\n", process->pid, cmdline);
     return true;
 
 #else
     // Unix implementation using fork(), exec(), pipe()
     fprintf(stderr, "Error: create_shell_process not implemented for non-Windows platforms.\n");
     return false;
 #endif
 }
 
 /**
  * Read available output from the shell process
  */
 int read_shell_output(shell_process_t *process, char *buffer, int size, int timeout_ms)
 {
     if (!process || !buffer || size <= 0 || !process->is_running)
     {
         return -1; // Invalid arguments or process not running
     }
 
 #ifdef WINDOWS
     if (process->stdout_pipe.read == INVALID_HANDLE_VALUE)
     {
         fprintf(stderr, "Error: read_shell_output called with invalid stdout pipe handle.\n");
         return -1;
     }
 
     DWORD bytes_read = 0;
     DWORD bytes_available = 0;
     DWORD total_bytes_read = 0;
     DWORD start_time = GetTickCount();
     DWORD current_time;
 
     do
     {
         // Check if there's data available to read from stdout
         // Important: PeekNamedPipe might return TRUE with bytes_available=0 if pipe is closed/broken
         if (!PeekNamedPipe(process->stdout_pipe.read, NULL, 0, NULL, &bytes_available, NULL))
         {
             DWORD error = GetLastError();
             if (error == ERROR_BROKEN_PIPE || error == ERROR_PIPE_NOT_CONNECTED)
             {
                 // Pipe closed, likely process exited
                 process->is_running = false;          // Update state
                 get_shell_process_exit_code(process); // Try to get exit code
                 return 0;                             // Indicate end of stream cleanly
             }
             fprintf(stderr, "Error: PeekNamedPipe failed (%lu)\n", error);
             return -1; // Genuine error
         }
 
         if (bytes_available > 0)
         {
             // Limit bytes to read to remaining buffer size
             DWORD bytes_to_read = (DWORD)size - total_bytes_read;
             if (bytes_to_read > bytes_available)
             {
                 bytes_to_read = bytes_available;
             }
             if (bytes_to_read == 0)
                 break; // Buffer full
 
             if (!ReadFile(process->stdout_pipe.read, buffer + total_bytes_read, bytes_to_read, &bytes_read, NULL))
             {
                 DWORD error = GetLastError();
                 if (error == ERROR_BROKEN_PIPE || error == ERROR_PIPE_NOT_CONNECTED)
                 {
                     process->is_running = false;
                     get_shell_process_exit_code(process);
                     return (int)total_bytes_read; // Return data read so far
                 }
                 fprintf(stderr, "Error: ReadFile failed (%lu)\n", error);
                 return -1; // Genuine error
             }
             total_bytes_read += bytes_read;
         }
 
         // If we've read something or timeout is 0, return immediately
         if (total_bytes_read > 0 || timeout_ms == 0)
         {
             break;
         }
 
         // Check timeout if applicable
         current_time = GetTickCount();
         if (timeout_ms > 0 && (current_time - start_time >= (DWORD)timeout_ms))
         {
             break; // Timeout exceeded
         }
 
         // If timeout allows, wait briefly before peeking again to avoid busy-waiting
         if (timeout_ms != 0)
         {
             // Simple non-blocking wait using WaitForSingleObject with small timeout
             WaitForSingleObject(process->stdout_pipe.read, 10); // Wait up to 10ms for data
         }
 
     } while (timeout_ms != 0 && total_bytes_read < (DWORD)size);
 
     // Check if process exited while we were reading/waiting
     if (!is_shell_process_running(process) && total_bytes_read == 0)
     {
         // Ensure we return 0 if process exited and no data was available/read
         return 0;
     }
 
     return (int)total_bytes_read; // Return total bytes read in this call
 #else
     // Unix implementation using select() or poll() and read()
     fprintf(stderr, "Error: read_shell_output not implemented for non-Windows platforms.\n");
     return -1;
 #endif
 }
 
 /**
  * Write input to the shell process
  */
 int write_shell_input(shell_process_t *process, const char *buffer, int size)
 {
     if (!process || !buffer || size <= 0 || !process->is_running)
     {
         fprintf(stderr, "Error: write_shell_input invalid args or process not running.\n");
         return -1;
     }
 
 #ifdef WINDOWS
     if (process->stdin_pipe.write == INVALID_HANDLE_VALUE)
     {
         fprintf(stderr, "Error: write_shell_input called with invalid stdin pipe handle.\n");
         return -1;
     }
 
     DWORD bytes_written = 0;
 
     if (!WriteFile(process->stdin_pipe.write, buffer, (DWORD)size, &bytes_written, NULL))
     {
         DWORD error = GetLastError();
         fprintf(stderr, "Error: WriteFile to stdin pipe failed (%lu)\n", error);
         if (error == ERROR_BROKEN_PIPE || error == ERROR_NO_DATA)
         {                                // ERROR_NO_DATA can indicate pipe closed
             process->is_running = false; // Assume process exited if pipe broken
             get_shell_process_exit_code(process);
         }
         return -1;
     }
 
     return (int)bytes_written;
 #else
     // Unix implementation using write()
     fprintf(stderr, "Error: write_shell_input not implemented for non-Windows platforms.\n");
     return -1;
 #endif
 }
 
 /**
  * Check if the shell process is still running
  */
 bool is_shell_process_running(shell_process_t *process)
 {
     if (!process)
     {
         return false;
     }
     // If we already know it's not running, return false immediately
     if (!process->is_running)
     {
         return false;
     }
 
 #ifdef WINDOWS
     if (process->hProcess == INVALID_HANDLE_VALUE)
     {
         process->is_running = false; // Mark as not running if handle invalid
         return false;
     }
 
     DWORD exit_code;
     // GetExitCodeProcess updates the exit code if the process has terminated
     if (!GetExitCodeProcess(process->hProcess, &exit_code))
     {
         // Error getting exit code, assume it's dead? Or handle error?
         fprintf(stderr, "Error: GetExitCodeProcess failed (%lu)\n", GetLastError());
         // Maybe don't change is_running state on error? Or maybe do?
         // Let's assume it might still be running but we had an error checking.
         // return process->is_running; // Return previous state? Risky.
         // Safer to assume it might have died if we can't check.
         process->is_running = false;
         process->exit_code = -1; // Indicate error/unknown exit code
         return false;
     }
 
     if (exit_code == STILL_ACTIVE)
     {
         process->is_running = true; // Confirm it's still running
         return true;
     }
 
     // Process has exited, update state if not already done
     if (process->is_running)
     { // Update only if state changes
         process->is_running = false;
         process->exit_code = (int)exit_code;
         printf("Process PID %d detected as exited with code %d.\n", process->pid, process->exit_code);
     }
 
     return false;
 #else
     // Unix implementation using waitpid() with WNOHANG
     fprintf(stderr, "Error: is_shell_process_running not implemented for non-Windows platforms.\n");
     return false;
 #endif
 }
 
 /**
  * Get the exit code of the shell process
  */
 int get_shell_process_exit_code(shell_process_t *process)
 {
     if (!process)
     {
         return -1;
     }
 
     // If process is marked as running, check its status first
     if (process->is_running)
     {
         if (is_shell_process_running(process))
         {
             return -1; // Still running
         }
         // If is_shell_process_running returned false, it updated exit_code
     }
 
     // Return the cached exit code (which might be -1 if GetExitCodeProcess failed)
     return process->exit_code;
 }
 
 // --- Define Callback Function Data Structure (Needed by C Callbacks) ---
 #ifdef WINDOWS
 typedef struct
 {
     DWORD targetPid;
     HWND foundHwnd;
 } FindWindowData;
 #endif
 // ----------------------------------------------------------------------
 
 // --- C Callback Function for EnumWindows (Replaces Lambda) ---
 #ifdef WINDOWS
 BOOL CALLBACK FindProcessWindowProc(HWND hwnd, LPARAM lParam)
 {
     FindWindowData *pData = (FindWindowData *)lParam;
     DWORD currentPid = 0; // Initialize to 0
 
     // Get the process ID associated with the window handle
     // FIX: Pass the address of currentPid (&currentPid)
     GetWindowThreadProcessId(hwnd, &currentPid);
 
     if (currentPid == pData->targetPid)
     {
         // Found a window belonging to the target process.
         // Optional: Add checks here to be more certain it's the *main*
         // or *console* window if possible (e.g., check window class, visibility)
         // Simple check: Is it a visible top-level window?
         if (IsWindowVisible(hwnd))
         { // Check basic visibility
             pData->foundHwnd = hwnd;
             return FALSE; // Stop enumeration, window found
         }
     }
     return TRUE; // Continue enumeration
 }
 #endif
 // -------------------------------------------------------------
 
 /**
  * Terminate the shell process
  */
 bool terminate_shell_process(shell_process_t *process, bool force)
 {
     if (!process)
     {
         return false;
     }
 
     // If process is not running according to our state, nothing to do
     if (!process->is_running)
     {
         return true;
     }
 
 #ifdef WINDOWS
     if (process->hProcess == INVALID_HANDLE_VALUE)
     {
         fprintf(stderr, "Warning: terminate_shell_process called with invalid process handle.\n");
         process->is_running = false; // Mark as not running
         return false;                // Indicate error or invalid state
     }
 
     BOOL result = FALSE;
     DWORD current_pid = GetProcessId(process->hProcess); // Get PID for messages
 
     if (force)
     {
         printf("Forcing termination of process PID %d.\n", (int)current_pid);
         // Forcefully terminate process
         result = TerminateProcess(process->hProcess, 1); // Use non-zero exit code to indicate termination
         if (!result)
         {
             fprintf(stderr, "Error: TerminateProcess failed for PID %d (%lu)\n", (int)current_pid, GetLastError());
         }
     }
     else
     {
         // Try to gracefully exit (more complex, might not work for all console apps)
 
         // 1. Attempt using GenerateConsoleCtrlEvent (often more reliable for console apps)
         //    This simulates Ctrl+C or Ctrl+Break. Try Ctrl+C first.
         //    Need to detach/attach console or use specific flags if parent is also console.
         //    Simpler approach for GUI parent: CreateRemoteThread injecting exit code? Risky.
         //    GenerateConsoleCtrlEvent is tricky from a GUI app without careful console handling.
 
         // 2. Alternative: Find window and send WM_CLOSE (as implemented before)
         HWND hwnd = NULL;
         FindWindowData findData;
         findData.targetPid = current_pid;
         findData.foundHwnd = NULL;
 
         EnumWindows(FindProcessWindowProc, (LPARAM)&findData);
         hwnd = findData.foundHwnd;
 
         if (hwnd)
         {
             // FIX: Cast HWND to void* for %p format specifier
             printf("Attempting graceful shutdown via WM_CLOSE for PID %d (HWND %p).\n", (int)current_pid, (void *)hwnd);
             // Use PostMessage to avoid blocking GUI if child is unresponsive
             PostMessage(hwnd, WM_CLOSE, 0, 0);
 
             // Wait a limited time for the process to exit
             DWORD waitResult = WaitForSingleObject(process->hProcess, 2000); // Wait up to 2 seconds
 
             if (waitResult == WAIT_OBJECT_0)
             {
                 // Process exited
                 printf("Process PID %d exited gracefully after WM_CLOSE.\n", (int)current_pid);
                 result = TRUE;
             }
             else if (waitResult == WAIT_TIMEOUT)
             {
                 // Process didn't exit, force kill it
                 fprintf(stderr, "Warning: Process PID %d did not exit gracefully after WM_CLOSE, forcing termination.\n", (int)current_pid);
                 result = TerminateProcess(process->hProcess, 1);
                 if (!result)
                     fprintf(stderr, "Error: TerminateProcess failed for PID %d (%lu)\n", (int)current_pid, GetLastError());
             }
             else
             {
                 // Wait failed for other reason
                 fprintf(stderr, "Error: WaitForSingleObject failed after WM_CLOSE for PID %d (%lu)\n", (int)current_pid, GetLastError());
                 // Attempt termination anyway
                 result = TerminateProcess(process->hProcess, 1);
                 if (!result)
                     fprintf(stderr, "Error: TerminateProcess failed for PID %d (%lu)\n", (int)current_pid, GetLastError());
             }
         }
         else
         {
             // No suitable window found, attempt TerminateProcess directly
             printf("No suitable window found for PID %d, terminating directly.\n", (int)current_pid);
             result = TerminateProcess(process->hProcess, 1);
             if (!result)
                 fprintf(stderr, "Error: TerminateProcess failed for PID %d (%lu)\n", (int)current_pid, GetLastError());
         }
     }
 
     // Regardless of success/failure of termination method, update state
     // Check final exit code after attempting termination
     DWORD final_exit_code;
     if (GetExitCodeProcess(process->hProcess, &final_exit_code))
     {
         if (final_exit_code != STILL_ACTIVE)
         {
             process->exit_code = (int)final_exit_code;
             process->is_running = false;
         }
         else
         {
             // If it's somehow STILL_ACTIVE after TerminateProcess, something is wrong
             process->exit_code = -1; // Unknown state
             // Keep is_running true? Or force false? Force false is safer.
             process->is_running = false;
             fprintf(stderr, "Warning: Process PID %d still active after termination attempt?\n", (int)current_pid);
         }
     }
     else
     {
         // Couldn't get exit code after termination attempt
         process->exit_code = -1;
         process->is_running = false; // Assume terminated if we tried
         fprintf(stderr, "Warning: Could not get final exit code for PID %d after termination attempt (%lu)\n", (int)current_pid, GetLastError());
     }
 
     // Cleanup handles even if termination failed, as the process might be defunct
     if (process->hThread != INVALID_HANDLE_VALUE)
     {
         CloseHandle(process->hThread);
         process->hThread = INVALID_HANDLE_VALUE;
     }
     if (process->hProcess != INVALID_HANDLE_VALUE)
     {
         // Don't close hProcess immediately if WaitForSingleObject might be used later?
         // However, if terminate succeeded or we assume it did, close it.
         CloseHandle(process->hProcess);
         process->hProcess = INVALID_HANDLE_VALUE;
     }
 
     return !process->is_running; // Return true if process is now marked as not running
 
 #else
     // Unix implementation using kill() signal
     fprintf(stderr, "Error: terminate_shell_process not implemented for non-Windows platforms.\n");
     return false;
 #endif
 }
 
 /**
  * Cleanup resources associated with the shell process
  */
 void cleanup_shell_process(shell_process_t *process)
 {
     if (!process)
     {
         return;
     }
 
     // First attempt to terminate the process if it's still marked as running
     if (process->is_running)
     {
         printf("Cleaning up process PID %d (forcing termination).\n", process->pid);
         terminate_shell_process(process, true); // Force termination during cleanup
     }
 
 #ifdef WINDOWS
     // Close remaining handles (terminate_shell_process should have closed hProcess/hThread)
     if (process->hProcess != INVALID_HANDLE_VALUE)
     {
         CloseHandle(process->hProcess); // Close again just in case
         process->hProcess = INVALID_HANDLE_VALUE;
     }
     if (process->hThread != INVALID_HANDLE_VALUE)
     {
         CloseHandle(process->hThread); // Close again just in case
         process->hThread = INVALID_HANDLE_VALUE;
     }
 
     // Close pipe handles (Parent's ends)
     if (process->stdin_pipe.write != INVALID_HANDLE_VALUE)
     {
         CloseHandle(process->stdin_pipe.write);
         process->stdin_pipe.write = INVALID_HANDLE_VALUE;
     }
     if (process->stdout_pipe.read != INVALID_HANDLE_VALUE)
     {
         CloseHandle(process->stdout_pipe.read);
         process->stdout_pipe.read = INVALID_HANDLE_VALUE;
     }
     // Close stderr pipe read handle if used
     if (process->stderr_pipe.read != INVALID_HANDLE_VALUE)
     {
         CloseHandle(process->stderr_pipe.read);
         process->stderr_pipe.read = INVALID_HANDLE_VALUE;
     }
 
     // Ensure child-side handles (which were closed after CreateProcess) are marked invalid
     process->stdin_pipe.read = INVALID_HANDLE_VALUE;
     process->stdout_pipe.write = INVALID_HANDLE_VALUE;
     process->stderr_pipe.write = INVALID_HANDLE_VALUE;
 
 #else
     // Unix cleanup (close file descriptors)
     if (process->stdin_pipe.write != -1)
         close(process->stdin_pipe.write);
     if (process->stdout_pipe.read != -1)
         close(process->stdout_pipe.read);
     if (process->stderr_pipe.read != -1)
         close(process->stderr_pipe.read);
     // Ensure child FDs are also marked invalid
     process->stdin_pipe.read = -1;
     process->stdout_pipe.write = -1;
     process->stderr_pipe.write = -1;
     // Maybe call waitpid one last time with WNOHANG?
 #endif
 
     // We removed the output buffer from shell_process_t
     // // Free output buffer
     // if (process->output_buffer)
     // {
     //     free(process->output_buffer);
     //     process->output_buffer = NULL;
     // }
 
     // Reset state variables
     process->pid = -1;
     process->is_running = false;
     // process->buffer_size = 0;
     // process->buffer_used = 0;
     process->exit_code = -1; // Reset exit code
     printf("Shell process structure cleaned up.\n");
 }
 
 /**
  * Resize the terminal of the shell process (Placeholder)
  */
 // FIX: Removed C++ style [[maybe_unused]] attribute incompatible with C standards before C23
 bool resize_shell_terminal(shell_process_t *process, int width, int height)
 {
     // Mark parameters as unused using standard C cast if function body is empty
     // (Function body already contains code using these, so cast isn't strictly needed,
     // but removing the attribute is necessary for compilation)
     // (void)process;
     // (void)width;
     // (void)height;
 
 #ifdef WINDOWS
     if (!process || !process->is_running || process->hProcess == INVALID_HANDLE_VALUE)
     {
         return false;
     }
 
     // --- Correct implementation is complex and often involves ---
     // 1. Sending a signal/message (like SIGWINCH on Unix) if the child expects it.
     // 2. Using Console API functions targeting the *child's* console buffer/window,
     //    which is difficult if the child was created with CREATE_NO_WINDOW or
     //    if the parent is a GUI app without its own console attached correctly.
     // 3. Using pseudo-terminals (like ConPTY on modern Windows) which handle resizing naturally.
 
     // The previous attempt using FindWindow + SetConsoleScreenBufferSize on CONOUT$ was incorrect.
     // FindWindow might find a window, but CreateFile("CONOUT$") gets the *parent's* console handle.
 
     fprintf(stdout, "Info: resize_shell_terminal for PID %d (W:%d, H:%d) - Not functionally implemented on Windows yet.\n",
             process->pid, width, height);
 
     // Returning true to indicate the call was made, even if ineffective.
     // Return false if you want to signal that resizing isn't supported.
     return true;
 #else
     // Unix implementation using ioctl(TIOCSWINSZ) on the pseudo-terminal master FD
     fprintf(stderr, "Error: resize_shell_terminal not implemented for non-Windows platforms.\n");
     return false;
 #endif
 }