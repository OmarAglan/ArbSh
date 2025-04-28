/**
 * process_manager.h - Process spawning and communication system
 *
 * This file defines the interface for the process management system that creates
 * shell instances as child processes and handles communication with them.
 */

#ifndef _PROCESS_MANAGER_H_
#define _PROCESS_MANAGER_H_

#include <stdbool.h>

// Include platform specifics needed for definition
#ifdef WINDOWS
#include <windows.h>
#endif

// Add extern "C" guards for C++ compatibility
#ifdef __cplusplus
extern "C"
{
#endif

    /**
     * Structure representing a child shell process
     */
    typedef struct _shell_process
    {
        // Process identification
        int pid;
#ifdef WINDOWS
        HANDLE hProcess;
        HANDLE hThread;
#endif

        // Communication pipes
        struct
        {
#ifdef WINDOWS
            HANDLE read;
            HANDLE write;
#else
        int read;
        int write;
#endif
        } stdin_pipe, stdout_pipe, stderr_pipe;

        // Process state
        bool is_running;
        int exit_code;

        // Buffer management (Maybe remove? Terminal tab manages its own buffer)
        // char *output_buffer;
        // int buffer_size;
        // int buffer_used;
    } shell_process_t;

    /**
     * Create a new shell process
     *
     * @param process Pointer to a shell_process_t structure to be initialized
     * @param command Command to run (NULL for default shell)
     * @param args Array of command arguments (NULL terminated)
     * @param env Array of environment variables (NULL terminated)
     * @return true if process was created successfully, false otherwise
     */
    bool create_shell_process(shell_process_t *process, const char *command, char *const args[], char *const env[]);

    /**
     * Read available output from the shell process
     *
     * @param process Shell process structure
     * @param buffer Buffer to store the output
     * @param size Size of the buffer
     * @param timeout_ms Timeout in milliseconds (0 for non-blocking, -1 for infinite)
     * @return Number of bytes read, 0 if no data/timeout, or -1 on error
     */
    int read_shell_output(shell_process_t *process, char *buffer, int size, int timeout_ms);

    /**
     * Write input to the shell process
     *
     * @param process Shell process structure
     * @param buffer Buffer containing the input
     * @param size Size of the input
     * @return Number of bytes written, or -1 on error
     */
    int write_shell_input(shell_process_t *process, const char *buffer, int size);

    /**
     * Check if the shell process is still running
     *
     * @param process Shell process structure
     * @return true if the process is running, false otherwise
     */
    bool is_shell_process_running(shell_process_t *process);

    /**
     * Get the exit code of the shell process
     *
     * @param process Shell process structure
     * @return Exit code of the process, or -1 if still running or error
     */
    int get_shell_process_exit_code(shell_process_t *process);

    /**
     * Terminate the shell process
     *
     * @param process Shell process structure
     * @param force If true, forcefully terminate the process
     * @return true if process was terminated successfully, false otherwise
     */
    bool terminate_shell_process(shell_process_t *process, bool force);

    /**
     * Send an interrupt signal (like Ctrl+C) to the shell process
     *
     * @param process Shell process structure
     * @return true if signal was sent successfully (or attempted), false on error
     */
    bool send_shell_interrupt(shell_process_t *process);

    /**
     * Cleanup resources associated with the shell process
     *
     * @param process Shell process structure
     */
    void cleanup_shell_process(shell_process_t *process);

    /**
     * Resize the terminal of the shell process (Placeholder - Currently non-functional)
     *
     * @param process Shell process structure
     * @param width New width in characters
     * @param height New height in characters
     * @return true if terminal was resized successfully, false otherwise
     */
    // Note: C++ compilers will understand [[maybe_unused]] here, C compilers prior to C23 might warn.
    // It's generally safer in headers used by both C and C++ to omit the attribute
    // and rely on the implementation (.c file) to handle unused parameters.
    // However, leaving it here as it was before, just be aware of potential C warnings.
    bool resize_shell_terminal([[maybe_unused]] shell_process_t *process, [[maybe_unused]] int width, [[maybe_unused]] int height);

// End extern "C" guards
#ifdef __cplusplus
}
#endif

#endif /* _PROCESS_MANAGER_H_ */