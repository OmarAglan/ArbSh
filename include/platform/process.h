#ifndef _PLATFORM_PROCESS_H_
#define _PLATFORM_PROCESS_H_

#include "shell.h" // Include main shell header for info_t etc.

// Opaque structure to hold platform-specific process details
// The actual definition will be in the platform-specific source files.
typedef struct platform_process_s platform_process_t;

/**
 * @brief Creates a new process to execute a command.
 *
 * This function abstracts the platform-specific details of creating a child
 * process (like fork/exec on POSIX or CreateProcess on Windows).
 *
 * @param info Pointer to the shell info structure, containing argv, env, etc.
 * @param command Full path to the executable command.
 * @param argv Argument vector for the new process.
 * @param envp Environment variables for the new process.
 * @return A pointer to a platform_process_t structure representing the
 *         new process, or NULL on failure.
 */
platform_process_t* platform_create_process(info_t *info, const char *command, char * const argv[], char * const envp[]);

/**
 * @brief Waits for a process to terminate and retrieves its exit status.
 *
 * @param process Pointer to the platform_process_t structure.
 * @return The exit status code of the process.
 */
int platform_wait_process(platform_process_t *process);

/**
 * @brief Cleans up resources associated with a process.
 *
 * This should be called after waiting for the process to release any
 * platform-specific handles or memory.
 *
 * @param process Pointer to the platform_process_t structure.
 */
void platform_cleanup_process(platform_process_t *process);

/**
 * @brief Gets the process ID of the current process.
 *
 * @return The process ID.
 */
long platform_getpid(void); // Return type could be pid_t or DWORD, use long for now

#endif /* _PLATFORM_PROCESS_H_ */ 