#ifndef _PLATFORM_CONSOLE_H_
#define _PLATFORM_CONSOLE_H_

#include <stddef.h> // For size_t
#include <stdio.h>  // For FILE*

// Standard file descriptor numbers (consistent across platforms)
#define PLATFORM_STDIN_FILENO  0
#define PLATFORM_STDOUT_FILENO 1
#define PLATFORM_STDERR_FILENO 2

/**
 * @brief Initializes the console for the application.
 * Sets up UTF-8 support, enables virtual terminal processing (if needed),
 * and performs other platform-specific console setup.
 * Should be called early in application startup.
 */
void platform_console_init(void);

/**
 * @brief Writes data to a console file descriptor.
 *
 * @param fd The file descriptor (PLATFORM_STDOUT_FILENO or PLATFORM_STDERR_FILENO).
 * @param buf The buffer containing data to write.
 * @param count The number of bytes to write.
 * @return The number of bytes written, or -1 on error.
 */
int platform_console_write(int fd, const char *buf, size_t count);

/**
 * @brief Reads data from a console file descriptor.
 *
 * @param fd The file descriptor (usually PLATFORM_STDIN_FILENO).
 * @param buf The buffer to store the read data.
 * @param count The maximum number of bytes to read.
 * @return The number of bytes read, 0 on EOF, or -1 on error.
 */
int platform_console_read(int fd, char *buf, size_t count);

/**
 * @brief Checks if a file descriptor is connected to a terminal (TTY).
 *
 * @param fd The file descriptor (PLATFORM_STDIN_FILENO, PLATFORM_STDOUT_FILENO, etc.).
 * @return 1 if it's a terminal, 0 otherwise.
 */
int platform_console_isatty(int fd);

/**
 * @brief Sets the console text direction (primarily for display hints).
 *
 * @param is_rtl 1 to hint RTL direction, 0 for LTR.
 * @return 0 on success, -1 on failure.
 */
int platform_console_set_text_direction(int is_rtl);

// Optional advanced functions (can be added later if needed)
/*
int platform_console_get_size(int *rows, int *cols);
int platform_console_set_raw_mode(int enable);
*/

#endif /* _PLATFORM_CONSOLE_H_ */ 