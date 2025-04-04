/**
 * shell_entry.c - Unified entry point for the shell application
 *
 * This file combines the functionality of:
 * - main.c (standard command-line mode)
 * - ImGui GUI mode with WinMain
 *
 * The application can run in either:
 * 1. Console mode (standard shell interface)
 * 2. GUI mode (ImGui interface with embedded shell)
 */

#include "shell.h"
#include "config.h" // Include config if needed for defines like USE_IMGUI
#include "platform/console.h" // Include PAL console functions
#include "platform/filesystem.h" // Include Filesystem PAL
#include <fcntl.h> // For O_RDONLY (POSIX standard, CRT often maps it)

#ifdef WINDOWS
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <fcntl.h>
#include <io.h>
#include <shellapi.h>
#include <stdarg.h>
#include <string.h>

#ifdef USE_IMGUI
#include "imgui_shell.h"
#endif

#define popen _popen
#define pclose _pclose
#define fileno _fileno
#define isatty _isatty
#undef write
#endif

/* Global variables for GUI mode */
#ifdef WINDOWS
int g_GuiMode = 0;           /* Flag to indicate GUI mode is active */

int g_ImGuiMode = 0;          /* Flag to indicate ImGui mode is active */

/* Forward declarations */
int shell_main(int argc, char *argv[]);
BOOL setup_console_window(BOOL force_visible);
#endif

/* Forward declare these low-level functions we will override */
int _putchar(char c);

/* Override _putchar for direct window output */
int gui_putchar(char c)
{
    static int i;
    static char buf[1024];

#ifdef USE_IMGUI
    if (g_ImGuiMode && g_GuiMode)
    {
        /* In ImGui mode, collect characters and update the GUI */
        if (c == BUF_FLUSH || i >= 1023)
        {
            if (i > 0)
            {
                buf[i] = '\0';
                imgui_update_console_text(buf);
                i = 0;
            }
        }
        else
        {
            buf[i++] = c;
            buf[i] = '\0';

            /* If newline or buffer nearly full, flush immediately */
            if (c == '\n' || i >= 1000)
            {
                imgui_update_console_text(buf);
                i = 0;
            }
        }
        return 1;
    }
    else 
#endif
    {
        /* Original behavior for console mode */
        if (c == BUF_FLUSH || i >= 1023)
        {
            if (i > 0)
            {
                /* Use _write with correct types for MinGW */
                _write(1, buf, (unsigned int)i);
                i = 0;
            }
        }
        if (c != BUF_FLUSH)
            buf[i++] = c;
        return 1;
    }
}

/**
 * is_hosted_by_gui - Check if the shell is running under a GUI
 * Return: 1 if hosted by GUI, 0 otherwise
 */
int is_hosted_by_gui(void)
{
    static int hosted = -1;
    
    // Only check once per process
    if (hosted == -1)
    {
        const char *env_var = getenv("ARBSH_HOSTED_BY_GUI");
        hosted = (env_var && *env_var == '1') ? 1 : 0;
    }
    
    return hosted;
}

/**
 * set_gui_env_for_child - Sets environment variable for child processes
 * This lets child processes know they're hosted by the GUI
 */
void set_gui_env_for_child(void)
{
#ifdef WINDOWS
    if (g_GuiMode)
        _putenv("ARBSH_HOSTED_BY_GUI=1");
    else
        _putenv("ARBSH_HOSTED_BY_GUI=0");
#else
    if (g_GuiMode)
        setenv("ARBSH_HOSTED_BY_GUI", "1", 1);
    else
        setenv("ARBSH_HOSTED_BY_GUI", "0", 1);
#endif
}

/**
 * shell_main - Main shell logic (formerly in main.c)
 * @argc: argument count
 * @argv: argument vector
 *
 * Return: 0 on success, 1 on error
 */
int shell_main(int argc, char *argv[])
{
    info_t info[] = {INFO_INIT};
    int script_fd = -1;

    // Initialize locale for better internationalization support
    init_locale();

    // Initialize Arabic input support
    init_arabic_input();

    // Load configuration from file
    load_configuration(info);

    // Display welcome message in the current language
    if (get_language() == 1) /* LANG_AR */
        _puts_utf8((char *)get_message(MSG_WELCOME));
    else
        _puts((char *)get_message(MSG_WELCOME));
    _putchar('\n');

#ifdef WINDOWS
    // Windows specific initialization - already handled in configure_terminal_for_utf8
#endif

#ifdef WINDOWS
    // Windows doesn't support inline assembly in MSVC
    // Just skip this part
#else
    asm("mov %1, %0\n\t"
        "add $3, %0"
        : "=r"(script_fd)
        : "r"(script_fd));
#endif

    // Argument parsing for script execution
    if (argc == 2)
    {
        // Use platform-independent file operations
        script_fd = platform_open(argv[1], O_RDONLY); // Use PAL open
        if (script_fd == -1)
        {
            // Error handling (check errno/GetLastError after platform_open)
            // Use platform console write for error messages
            if (errno == EACCES) exit(126); // Assuming errno is set by PAL
            if (errno == ENOENT)
            {
                platform_console_write(PLATFORM_STDERR_FILENO, argv[0], strlen(argv[0]));
                platform_console_write(PLATFORM_STDERR_FILENO, ": 0: Can't open ", 18);
                platform_console_write(PLATFORM_STDERR_FILENO, argv[1], strlen(argv[1]));
                platform_console_write(PLATFORM_STDERR_FILENO, "\n", 1);
                exit(127);
            }
            // Generic open error
            platform_console_write(PLATFORM_STDERR_FILENO, argv[0], strlen(argv[0]));
            platform_console_write(PLATFORM_STDERR_FILENO, ": Can't open script ", 20);
            platform_console_write(PLATFORM_STDERR_FILENO, argv[1], strlen(argv[1]));
            platform_console_write(PLATFORM_STDERR_FILENO, "\n", 1);
            return (EXIT_FAILURE);
        }
        info->readfd = script_fd; // Tell shell loop to read from this fd
    }
    else
    {
        // Default to reading from stdin if no script provided
        info->readfd = PLATFORM_STDIN_FILENO;
    }

    populate_env_list(info);
    read_history(info);
    load_aliases(info);  // Load aliases at startup
    hsh(info, argv);

    // Cleanup
    if (script_fd != -1 && script_fd != PLATFORM_STDIN_FILENO) {
        platform_close(script_fd); // Use PAL close
    }

    return (EXIT_SUCCESS);
}

#ifdef WINDOWS

/**
 * setup_console_window - Set up console window with proper settings
 * @force_visible: Force the console window to be visible
 *
 * Return: TRUE if successful, FALSE otherwise
 */
BOOL setup_console_window(BOOL force_visible)
{
    BOOL result = TRUE;
    HANDLE hConsole;
    CONSOLE_FONT_INFOEX fontInfo;
    int cp = GetConsoleOutputCP();

    /* Get the console output handle */
    hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
    if (hConsole == INVALID_HANDLE_VALUE)
    {
        return FALSE;
    }

    /* Configure console for UTF-8 output */
    if (cp != CP_UTF8)
    {
        SetConsoleOutputCP(CP_UTF8);
        printf("Changed console code page from %d to %d (UTF-8)\n", cp, CP_UTF8);
    }

    /* Set console font to one with good Arabic support */
    fontInfo.cbSize = sizeof(CONSOLE_FONT_INFOEX);
    GetCurrentConsoleFontEx(hConsole, FALSE, &fontInfo);

    /* Try to use a font with good Arabic support */
    wcscpy(fontInfo.FaceName, L"Consolas");
    fontInfo.dwFontSize.X = 0;
    fontInfo.dwFontSize.Y = 16;
    fontInfo.FontFamily = FF_DONTCARE;
    fontInfo.FontWeight = FW_NORMAL;

    /* Set the new font */
    if (!SetCurrentConsoleFontEx(hConsole, FALSE, &fontInfo))
    {
        printf("Failed to set console font. Arabic characters may not display correctly.\n");
        result = FALSE;
    }

    /* Configure console window visibility for GUI mode */
    if (g_GuiMode && !force_visible)
    {
        /* Hide the console window in GUI mode unless forced visible */
        ShowWindow(GetConsoleWindow(), SW_HIDE);
    }
    else
    {
        /* Show the console window */
        ShowWindow(GetConsoleWindow(), SW_SHOW);
    }

    return result;
}

/**
 * WinMain - Windows GUI entry point
 * @hInstance: The instance handle
 * @hPrevInstance: The previous instance handle (always NULL)
 * @lpCmdLine: Command line arguments
 * @nCmdShow: Show command
 *
 * Return: Exit code
 */
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
#ifdef USE_IMGUI
    /* ImGui entry point */
    extern int imgui_main(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow);
    g_ImGuiMode = TRUE;
    g_GuiMode = TRUE;
    return imgui_main(hInstance, hPrevInstance, lpCmdLine, nCmdShow);
#else
    /* This should never happen, as we've removed Win32 GUI support */
    MessageBox(NULL, "GUI mode requires ImGui support", "Error", MB_ICONERROR);
    return 1;
#endif /* USE_IMGUI */
}

#endif /* WINDOWS */

/**
 * main - Standard entry point for the application
 * @argc: Argument count
 * @argv: Argument array
 *
 * Return: Exit code
 */
int main(int argc, char *argv[])
{
#ifdef WINDOWS
    /* When compiled as a Windows GUI application, ensure we use WinMain */
#ifdef GUI_MODE
    /* Redirect to WinMain for GUI mode */
    extern int WINAPI WinMain(HINSTANCE, HINSTANCE, LPSTR, int);
    HINSTANCE hInstance = GetModuleHandle(NULL);
    return WinMain(hInstance, NULL, GetCommandLine(), SW_SHOWNORMAL);
#endif

    /* Check if we should run in GUI mode */
    g_GuiMode = FALSE;
#ifdef USE_IMGUI
    g_ImGuiMode = FALSE;
#endif

    for (int i = 1; i < argc; i++)
    {
        if (strcmp(argv[i], "--gui") == 0)
        {
#ifdef USE_IMGUI
            /* GUI mode now always uses ImGui */
            g_GuiMode = TRUE;
            g_ImGuiMode = TRUE;
            
            /* Redirect to ImGui WinMain */
            extern int WINAPI WinMain(HINSTANCE, HINSTANCE, LPSTR, int);
            HINSTANCE hInstance = GetModuleHandle(NULL);
            return WinMain(hInstance, NULL, GetCommandLine(), SW_SHOWNORMAL);
#else
            printf("GUI mode requires ImGui support\n");
            return 1;
#endif
        }
    }

    /* Set up console and run in console mode */
    setup_console_window(TRUE);

    /* Display startup message with a distinctive appearance */
    printf("\033[0m");               /* Reset any previous formatting */
    printf("\033[38;2;50;255;255m"); /* RGB color similar to Windows Terminal accent */
    printf("╔════════════════════════════════════════════════════╗\n");
    printf("║                                                    ║\n");
    printf("║                  ArbSh - CONSOLE MODE              ║\n");
    printf("║         WITH ARABIC AND BAA LANGUAGE SUPPORT       ║\n");
    printf("║                                                    ║\n");
    printf("╚════════════════════════════════════════════════════╝\n");
    printf("\033[0m\n"); /* Reset text formatting */

    /* Test Arabic output */
    printf("\033[38;2;255;200;50m"); /* Gold color for Arabic text */
    printf("مرحبًا بكم في ArbSh - واجهة مستخدم حديثة\n");
    printf("\033[0m\n"); /* Reset text formatting */

    /* Parse command line into argc/argv */
    LPWSTR *wargv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (wargv)
    {
        /* Run the shell in console mode */
        int result = shell_main(argc, argv);
        LocalFree(wargv);
        return result;
    }

    return shell_main(argc, argv);
#else
    /* On non-Windows platforms, just run in console mode */
    return shell_main(argc, argv);
#endif
}

#ifdef WINDOWS
/* Override printf only when not already declared */
#ifndef PRINTF_OVERRIDE
#define PRINTF_OVERRIDE
int printf(const char *format, ...)
{
    char buffer[4096];
    va_list args;
    int result;

    /* Format the message */
    va_start(args, format);
    result = vsnprintf(buffer, sizeof(buffer), format, args);
    va_end(args);

#ifdef USE_IMGUI
    /* Update the ImGui console window */
    if (g_GuiMode && g_ImGuiMode)
    {
        imgui_update_console_text(buffer);
    }
    else
#endif
    {
        /* Use the original printf */
        fputs(buffer, stdout);
    }

    return result;
}
#endif

/* Override puts only when not already declared */
#ifndef PUTS_OVERRIDE
#define PUTS_OVERRIDE
int puts(const char *str)
{
    char buffer[4096];
    int result;

    /* Format the message with newline */
    snprintf(buffer, sizeof(buffer), "%s\n", str);

#ifdef USE_IMGUI
    /* Update the ImGui console window */
    if (g_GuiMode && g_ImGuiMode)
    {
        imgui_update_console_text(buffer);
        result = (int)strlen(buffer);
    }
    else
#endif
    {
        /* Use the original puts */
        result = fputs(buffer, stdout);
    }

    return result;
}
#endif
#endif