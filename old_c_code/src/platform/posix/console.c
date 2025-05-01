#include "platform/console.h"
#include <unistd.h> // For read, write, isatty
#include <stdio.h>  // For setlocale
#include <locale.h> // For setlocale
#include <errno.h>

void platform_console_init(void)
{
    // Set locale to potentially enable UTF-8 if the system supports it.
    // The exact locale string might need adjustment based on target systems.
    setlocale(LC_ALL, "en_US.UTF-8");
    // Other POSIX terminal setup (like termios settings) could go here
    // if more advanced control (e.g., raw mode) is needed later.
}

int platform_console_write(int fd, const char *buf, size_t count)
{
    ssize_t bytes_written = write(fd, buf, count);
    if (bytes_written < 0) {
        // errno is set by write()
        return -1;
    }
    return (int)bytes_written;
}

int platform_console_read(int fd, char *buf, size_t count)
{
    ssize_t bytes_read = read(fd, buf, count);
    if (bytes_read < 0) {
        // errno is set by read()
        return -1;
    }
    // read() returns 0 on EOF.
    return (int)bytes_read;
}

int platform_console_isatty(int fd)
{
    // Use the standard POSIX function isatty
    return isatty(fd);
}

int platform_console_set_text_direction(int is_rtl)
{
    // POSIX terminals generally rely on the content and terminal emulator
    // capabilities for BiDi rendering. We can still emit VT sequences
    // as hints, similar to the Windows implementation.
    if (is_rtl) {
        platform_console_write(PLATFORM_STDOUT_FILENO, "\033]8;;bidi=R\a", 12);
        platform_console_write(PLATFORM_STDOUT_FILENO, "\xE2\x80\x8F", 3); // RTL Mark
    } else {
        platform_console_write(PLATFORM_STDOUT_FILENO, "\033]8;;bidi=L\a", 12);
        platform_console_write(PLATFORM_STDOUT_FILENO, "\xE2\x80\x8E", 3); // LTR Mark
    }
    // We could potentially use fcntl or termios for more advanced settings
    // if needed, but simple VT sequences are often sufficient as hints.
    return 0;
} 