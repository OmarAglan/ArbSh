#include "platform/console.h"
#include <windows.h>
#include <io.h>     // For _isatty, _read, _write
#include <stdio.h>  // For _fileno

// Helper to get Windows handle from standard fd number
static HANDLE get_std_handle(int fd) {
    if (fd == PLATFORM_STDIN_FILENO) return GetStdHandle(STD_INPUT_HANDLE);
    if (fd == PLATFORM_STDOUT_FILENO) return GetStdHandle(STD_OUTPUT_HANDLE);
    if (fd == PLATFORM_STDERR_FILENO) return GetStdHandle(STD_ERROR_HANDLE);
    return INVALID_HANDLE_VALUE;
}

void platform_console_init(void)
{
    // Set console code page to UTF-8 for input and output
    SetConsoleOutputCP(CP_UTF8);
    SetConsoleCP(CP_UTF8);

    // Enable Virtual Terminal Processing for ANSI escape codes
    HANDLE hOut = GetStdHandle(STD_OUTPUT_HANDLE);
    if (hOut != INVALID_HANDLE_VALUE) {
        DWORD dwMode = 0;
        if (GetConsoleMode(hOut, &dwMode)) {
            dwMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
            SetConsoleMode(hOut, dwMode);
        }
    }
    // Note: Font setting might be better handled externally or via user config
    // Removing the automatic font setting from here.
}

int platform_console_write(int fd, const char *buf, size_t count)
{
    HANDLE h = get_std_handle(fd);
    if (h == INVALID_HANDLE_VALUE) return -1;

    DWORD bytes_written = 0;
    // Use WriteFile for console handles, _write otherwise (for redirection)
    if (platform_console_isatty(fd)) {
        if (!WriteFile(h, buf, (DWORD)count, &bytes_written, NULL)) {
            return -1;
        }
    }
    else {
        // Use standard C _write for non-console handles (redirected files)
        // Note: _write returns int, might truncate large counts on 32-bit?
        int result = _write(_fileno(fd == PLATFORM_STDOUT_FILENO ? stdout : stderr), buf, (unsigned int)count);
        if (result < 0) return -1;
        bytes_written = (DWORD)result;
    }
    return (int)bytes_written;
}

int platform_console_read(int fd, char *buf, size_t count)
{
    HANDLE h = get_std_handle(fd);
    if (h == INVALID_HANDLE_VALUE || fd != PLATFORM_STDIN_FILENO) return -1;

    DWORD bytes_read = 0;
    // Use ReadFile for console handles, _read otherwise
    if (platform_console_isatty(fd)) {
        // Note: ReadFile on console handles might behave differently (e.g., line buffering)
        // Consider ReadConsole for more control if needed.
        if (!ReadFile(h, buf, (DWORD)count, &bytes_read, NULL)) {
             // Check for EOF specifically? ReadFile might return true with 0 bytes read on Ctrl+Z/Ctrl+D
            if (GetLastError() == ERROR_BROKEN_PIPE) { // Often indicates EOF on console
                return 0;
            }
            return -1;
        }
    }
    else {
        int result = _read(_fileno(stdin), buf, (unsigned int)count);
        if (result < 0) return -1;
        bytes_read = (DWORD)result;
    }
    return (int)bytes_read;
}

int platform_console_isatty(int fd)
{
    // Use the MSVC runtime function _isatty
    int CrtFd = -1;
    if (fd == PLATFORM_STDIN_FILENO) CrtFd = _fileno(stdin);
    else if (fd == PLATFORM_STDOUT_FILENO) CrtFd = _fileno(stdout);
    else if (fd == PLATFORM_STDERR_FILENO) CrtFd = _fileno(stderr);
    else return 0;

    return _isatty(CrtFd);
}

int platform_console_set_text_direction(int is_rtl)
{
    // Windows console direction is primarily controlled by font and content.
    // However, we can emit VT sequences as hints, which modern terminals might use.
    // This matches the logic previously in src/utils/utf8.c set_text_direction.
    if (is_rtl) {
        // Sequence for RTL (may include BiDi mode hints, RTL mark)
        platform_console_write(PLATFORM_STDOUT_FILENO, "\033]8;;bidi=R\a", 12);
        platform_console_write(PLATFORM_STDOUT_FILENO, "\xE2\x80\x8F", 3); // RTL Mark
        // Additional sequences like \033[=14h, \033[=15h could be added if needed
    } else {
        // Sequence for LTR
        platform_console_write(PLATFORM_STDOUT_FILENO, "\033]8;;bidi=L\a", 12);
        platform_console_write(PLATFORM_STDOUT_FILENO, "\xE2\x80\x8E", 3); // LTR Mark
        // Additional sequences like \033[=14l, \033[=15l could be added
    }
    return 0;
} 