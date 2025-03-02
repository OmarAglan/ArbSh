/**
 * shell_entry.c - Unified entry point for the shell application
 *
 * This file combines the functionality of:
 * - main.c (standard command-line mode)
 * - main_gui.c (Windows GUI mode with WinMain)
 * - win_main.c (Windows-specific implementation)
 * - win_gui_common.c (common GUI functions)
 *
 * The application can run in either:
 * 1. Console mode (standard shell interface)
 * 2. GUI mode (Windows GUI with embedded shell)
 */

#include "shell.h"

#ifdef WINDOWS
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <fcntl.h>
#include <io.h>
#include <shellapi.h>
#include <commctrl.h> /* For common controls */
#include <commdlg.h>
#include <shlwapi.h>
#include <stdarg.h>
#include <string.h>
#include <dwmapi.h> /* For DwmSetWindowAttribute */
#define popen _popen
#define pclose _pclose
#define fileno _fileno
#define isatty _isatty
#undef write

/* Link required libraries */
#pragma comment(lib, "comctl32.lib")
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "gdi32.lib")
/* Removing dwmapi as it's causing link issues */
/* #pragma comment(lib, "dwmapi.lib") */
#endif

/* Resource IDs */
#define IDI_SHELL_ICON 101
#define IDR_MENU_MAIN 102
#define IDC_STATUSBAR 103
#define IDC_TOOLBAR 104
#define IDC_CONSOLE_CONTAINER 105

/* Menu IDs */
#define ID_FILE_EXIT 201
#define ID_EDIT_CLEAR 202
#define ID_VIEW_FONT 203
#define ID_HELP_ABOUT 204

/* Control IDs */
#define IDM_NEW_TAB 2001
#define IDM_CLOSE_TAB 2002
#define IDM_EXIT 2003

/* Define menu IDs */
#define IDM_MENU 1000
#define IDM_FILE_NEW 1001
#define IDM_FILE_CLOSE 1002
#define IDM_FILE_EXIT 1003
#define IDM_HELP_ABOUT 1004

/* Window dimensions */
#define WINDOW_WIDTH 1000
#define WINDOW_HEIGHT 700

/* Global variables for GUI mode */
#ifdef WINDOWS
BOOL g_GuiMode = FALSE;           /* Flag to indicate GUI mode is active */
HWND g_hMainWindow = NULL;        /* Main window handle */
HWND g_hStatusBar = NULL;         /* Status bar window handle */
HWND g_hToolBar = NULL;           /* Toolbar window handle */
HWND g_hConsoleContainer = NULL;  /* Container for console window */
HANDLE g_hConsoleIn = NULL;       /* Console input handle */
HANDLE g_hConsoleOut = NULL;      /* Console output handle */
HFONT g_hFont = NULL;             /* Font handle */
HBRUSH g_hBackgroundBrush = NULL; /* Background brush */
HWND g_hTabControl = NULL;        /* Tab control handle */
int g_ActiveTab = 0;              /* Current active tab */
HWND g_ConsoleHwnd = NULL;        /* Current active console window */
WNDPROC g_OldEditProc = NULL;     /* Original window procedure for edit controls */

/* Structure to hold tab information */
typedef struct
{
    HWND hConsole;   /* Console window handle */
    HANDLE hProcess; /* Shell process handle */
    HANDLE hStdIn;   /* Standard input handle for this tab */
    char title[256]; /* Tab title */
    BOOL isActive;   /* Is this the active tab? */
} TabInfo;

/* Array to store tab information */
#define MAX_TABS 10
TabInfo g_Tabs[MAX_TABS] = {0};
int g_TabCount = 0;

/* Forward declarations */
int shell_main(int argc, char *argv[]);
LRESULT CALLBACK WindowProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
DWORD WINAPI ShellThreadProc(LPVOID lpParameter);
BOOL setup_console_window(BOOL force_visible);
char **convert_args(int argc, LPWSTR *wargv);
void free_args(int argc, char **argv);
HWND create_status_bar(HWND hParent);
HWND create_toolbar(HWND hParent);
BOOL WINAPI ConsoleEventHandler(DWORD dwCtrlType);
int initialize_console_for_gui(void);
BOOL CreateNewTab(HWND hTabControl, const char *title);
BOOL SwitchToTab(HWND hTabControl, int tabIndex);
BOOL CloseTab(HWND hTabControl, int tabIndex);
HWND CreateConsoleInWindow(HWND hParent);
BOOL PositionConsoleInTab(HWND hConsole, HWND hTabControl);
DWORD WINAPI ReadPipeThread(LPVOID param);
HMENU create_menu(HWND hWnd);
DWORD WINAPI ShellThreadForTab(LPVOID param);
LRESULT CALLBACK EditSubclassProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
void update_console_text(const char *text);
#endif

/* The init_locale function is defined in locale.c */

/* Forward declare these low-level functions we will override */
int _putchar(char c);

/* Override _putchar for direct window output */
int gui_putchar(char c)
{
    static int i;
    static char buf[1024];

    if (g_GuiMode && g_ConsoleHwnd)
    {
        /* In GUI mode, collect characters and update the window */
        if (c == BUF_FLUSH || i >= 1023)
        {
            if (i > 0)
            {
                buf[i] = '\0';
                update_console_text(buf);
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
                update_console_text(buf);
                i = 0;
            }
        }
        return 1;
    }
    else
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
 * shell_main - Main shell logic (formerly in main.c)
 * @argc: argument count
 * @argv: argument vector
 *
 * Return: 0 on success, 1 on error
 */
int shell_main(int argc, char *argv[])
{
    info_t info[] = {INFO_INIT};
    int fd = 2;

    // Initialize locale for better internationalization support
    init_locale();

    // Initialize Arabic input support
    init_arabic_input();

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
        : "=r"(fd)
        : "r"(fd));
#endif

    if (argc == 2)
    {
        fd = open(argv[1], O_RDONLY);
        if (fd == -1)
        {
            if (errno == EACCES)
                exit(126);
            if (errno == ENOENT)
            {
                _eputs(argv[0]);
                _eputs(": 0: Can't open ");
                _eputs(argv[1]);
                _eputchar('\n');
                _eputchar(BUF_FLUSH);
                exit(127);
            }
            return (EXIT_FAILURE);
        }
        info->readfd = fd;
    }
    populate_env_list(info);
    read_history(info);
    hsh(info, argv);
    return (EXIT_SUCCESS);
}

#ifdef WINDOWS

/**
 * ConsoleEventHandler - Handles console control events
 * @dwCtrlType: The type of control event
 *
 * Return: TRUE if the event was handled, FALSE otherwise
 */
BOOL WINAPI ConsoleEventHandler(DWORD dwCtrlType)
{
    switch (dwCtrlType)
    {
    case CTRL_C_EVENT:
    case CTRL_BREAK_EVENT:
    case CTRL_CLOSE_EVENT:
        /* Handle console control events */
        if (g_GuiMode)
        {
            /* In GUI mode, don't exit on Ctrl+C */
            return TRUE;
        }
        return FALSE;
    default:
        return FALSE;
    }
}

/**
 * create_status_bar - Creates the status bar
 * @hParent: Parent window handle
 *
 * Return: The status bar window handle
 */
HWND create_status_bar(HWND hParent)
{
    HWND hStatus;
    int statusParts[] = {200, 400, 600, -1};

    /* Create the status bar */
    hStatus = CreateWindowEx(
        0,
        STATUSCLASSNAME,
        NULL,
        WS_CHILD | WS_VISIBLE | SBARS_SIZEGRIP,
        0, 0, 0, 0,
        hParent,
        (HMENU)IDC_STATUSBAR,
        GetModuleHandle(NULL),
        NULL);

    /* Set the status bar parts */
    SendMessage(hStatus, SB_SETPARTS, 4, (LPARAM)statusParts);

    /* Set the text for each part */
    SendMessage(hStatus, SB_SETTEXT, 0, (LPARAM) "Ready");
    SendMessage(hStatus, SB_SETTEXT, 1, (LPARAM) "UTF-8");
    SendMessage(hStatus, SB_SETTEXT, 2, (LPARAM) "Simple Shell");
    SendMessage(hStatus, SB_SETTEXT, 3, (LPARAM) "Arabic Support");

    return hStatus;
}

/**
 * create_toolbar - Creates the application toolbar
 * @hParent: The parent window
 *
 * Return: The toolbar handle
 */
HWND create_toolbar(HWND hParent)
{
    HWND hToolBar;
    TBBUTTON tbb[3];
    TBADDBITMAP tbab;

    /* Create toolbar control */
    hToolBar = CreateWindowEx(
        0,
        TOOLBARCLASSNAME,
        NULL,
        WS_CHILD | WS_VISIBLE | TBSTYLE_FLAT | TBSTYLE_TOOLTIPS,
        0, 0, 0, 0,
        hParent,
        (HMENU)IDC_TOOLBAR,
        GetModuleHandle(NULL),
        NULL);

    if (!hToolBar)
        return NULL;

    /* Set toolbar style */
    SendMessage(hToolBar, TB_BUTTONSTRUCTSIZE, (WPARAM)sizeof(TBBUTTON), 0);

    /* Add standard bitmap */
    tbab.hInst = HINST_COMMCTRL;
    tbab.nID = IDB_STD_SMALL_COLOR;
    SendMessage(hToolBar, TB_ADDBITMAP, 0, (LPARAM)&tbab);

    /* Add buttons */
    ZeroMemory(tbb, sizeof(tbb));

    /* New Tab button */
    tbb[0].iBitmap = STD_FILENEW;
    tbb[0].fsState = TBSTATE_ENABLED;
    tbb[0].fsStyle = TBSTYLE_BUTTON;
    tbb[0].idCommand = IDM_NEW_TAB;
    tbb[0].iString = (INT_PTR) "New Tab";

    /* Close Tab button */
    tbb[1].iBitmap = STD_DELETE;
    tbb[1].fsState = TBSTATE_ENABLED;
    tbb[1].fsStyle = TBSTYLE_BUTTON;
    tbb[1].idCommand = IDM_CLOSE_TAB;
    tbb[1].iString = (INT_PTR) "Close Tab";

    /* Separator */
    tbb[2].fsStyle = TBSTYLE_SEP;

    /* Add the buttons */
    SendMessage(hToolBar, TB_ADDBUTTONS, 3, (LPARAM)&tbb);

    return hToolBar;
}

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

    /* Set console control handler */
    SetConsoleCtrlHandler(ConsoleEventHandler, TRUE);

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
 * update_console_text - Adds text to the console window
 * @text: The text to add
 *
 * Return: None
 */
void update_console_text(const char *text)
{
    if (!g_ConsoleHwnd || !text)
        return;

    int length = GetWindowTextLength(g_ConsoleHwnd);
    SendMessage(g_ConsoleHwnd, EM_SETSEL, (WPARAM)length, (LPARAM)length);
    SendMessage(g_ConsoleHwnd, EM_REPLACESEL, FALSE, (LPARAM)text);
    SendMessage(g_ConsoleHwnd, EM_SCROLLCARET, 0, 0);
}

/**
 * WindowProc - Window procedure for the main window
 * @hWnd: Window handle
 * @uMsg: Message
 * @wParam: Word parameter
 * @lParam: Long parameter
 *
 * Return: Result
 */
LRESULT CALLBACK WindowProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    switch (uMsg)
    {
    case WM_CREATE:
    {
        RECT rcClient;

        /* Enable dark mode for title bar commented out due to linking issues
        BOOL darkMode = TRUE;
        DwmSetWindowAttribute(hWnd, 19, &darkMode, sizeof(darkMode));
        */

        /* Create status bar */
        g_hStatusBar = create_status_bar(hWnd);

        /* Create toolbar */
        g_hToolBar = create_toolbar(hWnd);

        /* Create menu */
        create_menu(hWnd);

        /* Get client area size */
        GetClientRect(hWnd, &rcClient);

        /* Create tab control */
        g_hTabControl = CreateWindowEx(
            0,
            WC_TABCONTROL,
            NULL,
            WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | TCS_TABS | TCS_FOCUSONBUTTONDOWN |
                TCS_FLATBUTTONS | TCS_OWNERDRAWFIXED,
            0, 0, rcClient.right, rcClient.bottom,
            hWnd,
            (HMENU)1000,
            GetModuleHandle(NULL),
            NULL);

        /* Set tab control font */
        SendMessage(g_hTabControl, WM_SETFONT, (WPARAM)g_hFont, TRUE);

        /* Create console container */
        g_hConsoleContainer = CreateWindowEx(
            0,
            "STATIC",
            NULL,
            WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | SS_SUNKEN,
            0, 0, rcClient.right, rcClient.bottom,
            hWnd,
            NULL,
            GetModuleHandle(NULL),
            NULL);

        /* Create initial tab */
        CreateNewTab(g_hTabControl, "Shell 1");

        return 0;
    }

    case WM_CHAR:
        /* Forward keyboard input to active console */
        if (g_ConsoleHwnd)
        {
            SendMessage(g_ConsoleHwnd, WM_CHAR, wParam, lParam);
            return 0;
        }
        break;

    case WM_KEYDOWN:
        /* Forward keyboard input to active console */
        if (g_ConsoleHwnd)
        {
            SendMessage(g_ConsoleHwnd, WM_KEYDOWN, wParam, lParam);
            return 0;
        }
        break;

    case WM_SIZE:
    {
        int width = LOWORD(lParam);
        int height = HIWORD(lParam);
        RECT rcStatus, rcToolbar;
        int statusHeight, toolbarHeight;

        /* Resize the status bar */
        SendMessage(g_hStatusBar, WM_SIZE, 0, 0);

        /* Get status bar height */
        GetWindowRect(g_hStatusBar, &rcStatus);
        statusHeight = rcStatus.bottom - rcStatus.top;

        /* Get toolbar height */
        GetWindowRect(g_hToolBar, &rcToolbar);
        toolbarHeight = rcToolbar.bottom - rcToolbar.top;

        /* Position toolbar */
        SetWindowPos(g_hToolBar, NULL, 0, 0, width, toolbarHeight, SWP_NOZORDER);

        /* Position tab control */
        SetWindowPos(g_hTabControl, NULL, 0, toolbarHeight, width, height - statusHeight - toolbarHeight, SWP_NOZORDER);

        /* Position console container */
        SetWindowPos(g_hConsoleContainer, NULL, 0, toolbarHeight, width, height - statusHeight - toolbarHeight, SWP_NOZORDER);

        /* Reposition the active console */
        if (g_TabCount > 0 && g_ActiveTab >= 0 && g_ActiveTab < g_TabCount)
        {
            PositionConsoleInTab(g_Tabs[g_ActiveTab].hConsole, g_hTabControl);
        }

        return 0;
    }

    case WM_NOTIFY:
    {
        LPNMHDR pnmh = (LPNMHDR)lParam;

        /* Handle tab control notifications */
        if (pnmh->hwndFrom == g_hTabControl)
        {
            switch (pnmh->code)
            {
            case TCN_SELCHANGE:
                /* Switch to the selected tab */
                SwitchToTab(g_hTabControl, TabCtrl_GetCurSel(g_hTabControl));
                break;
            }
        }

        break;
    }

    case WM_COMMAND:
    {
        int wmId = LOWORD(wParam);

        /* Handle menu commands */
        switch (wmId)
        {
        case IDM_NEW_TAB:
        case IDM_FILE_NEW:
            /* Create a new tab */
            {
                char tabName[256];
                sprintf(tabName, "Shell %d", g_TabCount + 1);
                CreateNewTab(g_hTabControl, tabName);
            }
            break;

        case IDM_CLOSE_TAB:
        case IDM_FILE_CLOSE:
            /* Close the current tab */
            if (g_TabCount > 0)
            {
                CloseTab(g_hTabControl, g_ActiveTab);
            }
            break;

        case IDM_FILE_EXIT:
            /* Exit application */
            DestroyWindow(hWnd);
            break;

        case IDM_HELP_ABOUT:
            /* Show about dialog */
            MessageBox(hWnd, "Simple Shell Terminal\nWith Tabbed Interface", "About", MB_OK | MB_ICONINFORMATION);
            break;
        }

        break;
    }

    case WM_CLOSE:
        /* Clean up before closing */
        DestroyWindow(hWnd);
        return 0;

    case WM_DESTROY:
        /* Exit the application */
        PostQuitMessage(0);
        return 0;

    case WM_DRAWITEM:
    {
        LPDRAWITEMSTRUCT lpdis = (LPDRAWITEMSTRUCT)lParam;

        /* Check if the draw item is from our tab control */
        if (lpdis->CtlID == 1000 && lpdis->hwndItem == g_hTabControl)
        {
            char label[256];
            RECT rect = lpdis->rcItem;
            COLORREF bgColor = RGB(12, 12, 12);         /* Dark background */
            COLORREF textColor = RGB(240, 240, 240);    /* Light text */
            COLORREF selectedBgColor = RGB(65, 65, 65); /* Slightly lighter when selected */

            /* Get tab text */
            TCITEM tci;
            ZeroMemory(&tci, sizeof(TCITEM));
            tci.mask = TCIF_TEXT;
            tci.pszText = label;
            tci.cchTextMax = sizeof(label);
            TabCtrl_GetItem(g_hTabControl, lpdis->itemID, &tci);

            /* Fill background */
            SetBkMode(lpdis->hDC, TRANSPARENT);
            if (lpdis->itemState & ODS_SELECTED)
            {
                /* Selected tab */
                SetTextColor(lpdis->hDC, textColor);
                HBRUSH hBrush = CreateSolidBrush(selectedBgColor);
                FillRect(lpdis->hDC, &rect, hBrush);
                DeleteObject(hBrush);
            }
            else
            {
                /* Unselected tab */
                SetTextColor(lpdis->hDC, RGB(180, 180, 180)); /* Slightly dimmer text */
                HBRUSH hBrush = CreateSolidBrush(bgColor);
                FillRect(lpdis->hDC, &rect, hBrush);
                DeleteObject(hBrush);
            }

            /* Add some padding */
            rect.left += 10;
            rect.top += 5;

            /* Draw text */
            DrawText(lpdis->hDC, label, -1, &rect, DT_LEFT | DT_VCENTER | DT_SINGLELINE);

            return TRUE;
        }
        break;
    }
    }

    return DefWindowProc(hWnd, uMsg, wParam, lParam);
}

/**
 * convert_args - Convert wide character arguments to UTF-8
 * @argc: Argument count
 * @wargv: Wide character arguments
 *
 * Return: UTF-8 arguments
 */
char **convert_args(int argc, LPWSTR *wargv)
{
    int i;
    char **argv;

    /* Allocate memory for arguments */
    argv = (char **)malloc(sizeof(char *) * (argc + 1));
    if (!argv)
    {
        return NULL;
    }

    /* Convert each argument to UTF-8 */
    for (i = 0; i < argc; i++)
    {
        /* Get required buffer size */
        int size = WideCharToMultiByte(CP_UTF8, 0, wargv[i], -1, NULL, 0, NULL, NULL);
        if (size <= 0)
        {
            /* Error converting argument */
            int j;
            for (j = 0; j < i; j++)
            {
                free(argv[j]);
            }
            free(argv);
            return NULL;
        }

        /* Allocate buffer for converted argument */
        argv[i] = (char *)malloc(size);
        if (!argv[i])
        {
            /* Error allocating memory */
            int j;
            for (j = 0; j < i; j++)
            {
                free(argv[j]);
            }
            free(argv);
            return NULL;
        }

        /* Convert argument to UTF-8 */
        WideCharToMultiByte(CP_UTF8, 0, wargv[i], -1, argv[i], size, NULL, NULL);
    }

    /* NULL-terminate argument array */
    argv[argc] = NULL;

    return argv;
}

/**
 * free_args - Free converted arguments
 * @argc: Argument count
 * @argv: Arguments to free
 */
void free_args(int argc, char **argv)
{
    int i;

    if (!argv)
    {
        return;
    }

    /* Free each argument */
    for (i = 0; i < argc; i++)
    {
        free(argv[i]);
    }

    /* Free argument array */
    free(argv);
}

/**
 * ShellThreadProc - Thread procedure for running the shell
 * @param: Console window handle
 *
 * Return: Thread exit code
 */
DWORD WINAPI ShellThreadProc(LPVOID lpParameter)
{
    HWND hConsole = (HWND)lpParameter;
    int result = 0;

    /* Set window as active shell target */
    g_ConsoleHwnd = hConsole;
    g_GuiMode = TRUE;

    /* Run the shell */
    char *argv[] = {"shell", NULL};
    result = shell_main(1, argv);

    /* Handle shell exit */
    char exitMsg[100];
    sprintf(exitMsg, "\r\n[Shell exited with code %d]\r\n", result);
    update_console_text(exitMsg);

    return result;
}

/**
 * initialize_console_for_gui - Initialize console for GUI mode
 *
 * Return: Result code
 */
int initialize_console_for_gui(void)
{
    FILE *fout, *fin, *ferr;
    int result = 0;
    MSG msg;
    LPWSTR *wargv;
    int argc;
    char **argv = NULL;

    /* Allocate a console for the application */
    if (!AllocConsole())
    {
        MessageBox(NULL, "Failed to allocate console!", "Error", MB_ICONEXCLAMATION | MB_OK);
        return 1;
    }

    /* Open console streams */
    fout = freopen("CONOUT$", "w", stdout);
    fin = freopen("CONIN$", "r", stdin);
    ferr = freopen("CONOUT$", "w", stderr);

    if (!fout || !fin || !ferr)
    {
        MessageBox(NULL, "Failed to redirect console streams!", "Error", MB_ICONEXCLAMATION | MB_OK);
        return 1;
    }

    *stdout = *fout;
    *stdin = *fin;
    *stderr = *ferr;

    setvbuf(stdout, NULL, _IONBF, 0);
    setvbuf(stderr, NULL, _IONBF, 0);

    /* Parse command line into argc/argv */
    wargv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (wargv)
    {
        /* Convert wide character arguments to UTF-8 */
        argv = convert_args(argc, wargv);

        /* Create a thread to run the shell */
        HANDLE hThread = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)ShellThreadProc,
                                      argv ? argv : (void *)1, 0, NULL);
        if (!hThread)
        {
            printf("Failed to create shell thread\n");
            result = -1;
        }
        else
        {
            /* Process messages while the shell thread is running */
            DWORD threadStatus = STILL_ACTIVE;

            do
            {
                /* Check if there are messages to process */
                while (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE))
                {
                    /* If it's a quit message, exit the message loop */
                    if (msg.message == WM_QUIT)
                    {
                        /* Signal the shell thread to terminate */
                        TerminateThread(hThread, 0);
                        break;
                    }

                    /* Process the message */
                    TranslateMessage(&msg);
                    DispatchMessage(&msg);
                }

                /* Check if the thread is still running */
                GetExitCodeThread(hThread, &threadStatus);

                /* Give other threads a chance to run */
                Sleep(10);

            } while (threadStatus == STILL_ACTIVE);

            /* Get the thread's exit code */
            GetExitCodeThread(hThread, (LPDWORD)&result);
            CloseHandle(hThread);
        }

        /* Free the converted arguments */
        if (argv)
            free_args(argc, argv);

        /* Free the wide character arguments */
        LocalFree(wargv);
    }
    else
    {
        /* Message loop - simplified version if we can't get command line arguments */
        while (GetMessage(&msg, NULL, 0, 0))
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }

        result = (int)msg.wParam;
    }

    /* Display exit message */
    printf("\n\033[0m");             /* Reset any previous formatting */
    printf("\033[38;2;255;255;50m"); /* RGB color similar to Windows Terminal accent */
    printf("╔════════════════════════════════════════════════════╗\n");
    printf("║                                                    ║\n");
    printf("║              SIMPLE SHELL - MODERN UI              ║\n");
    printf("║         WITH ARABIC AND BAA LANGUAGE SUPPORT       ║\n");
    printf("║                                                    ║\n");
    printf("╚════════════════════════════════════════════════════╝\n");
    printf("\033[0m\n"); /* Reset text formatting */

    /* Test Arabic output */
    printf("\033[38;2;255;200;50m"); /* Gold color for Arabic text */
    printf("مرحبًا بكم في الصدفة البسيطة - واجهة مستخدم حديثة\n");
    printf("\033[0m\n"); /* Reset text formatting */

    /* Set the default console color scheme to match Windows Terminal */
    printf("\033[40m");   /* Black background */
    printf("\033[37;1m"); /* Bright white text */

    /* Display exit message */
    printf("\n\033[38;2;255;255;50mShell exited with code %d\033[0m\n", result);
    printf("\033[38;2;200;200;200mPress Enter to exit...\033[0m");
    fflush(stdout);
    getchar();

    /* Close file streams */
    fclose(fout);
    fclose(fin);
    fclose(ferr);

    /* Free the console */
    FreeConsole();

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
    MSG msg;
    HWND hWnd;
    WNDCLASSEX wc;
    HICON hIcon;
    FILE *debug_file = NULL;

    UNREFERENCED_PARAMETER(hPrevInstance);
    UNREFERENCED_PARAMETER(lpCmdLine);
    UNREFERENCED_PARAMETER(nCmdShow);

    /* Create a debug file */
    debug_file = fopen("gui_debug.log", "w");
    if (debug_file)
    {
        fprintf(debug_file, "WinMain: Starting GUI mode...\n");
        fflush(debug_file);
    }

    /* Display a message box to confirm we're in WinMain */
    MessageBox(NULL, "Starting GUI Mode from WinMain - Shell is working correctly!", "Shell Status", MB_OK | MB_ICONINFORMATION);

    /* Allocate a console for debug output */
    if (AllocConsole())
    {
        fprintf(stdout, "Console allocated successfully\n");
    }
    else
    {
        MessageBox(NULL, "Failed to allocate console", "Error", MB_ICONERROR);
    }

    /* Redirect standard streams */
    FILE *fout = freopen("CONOUT$", "w", stdout);

/* Redirect stderr if needed */
#ifdef DEBUG
    FILE *ferr = freopen("CONOUT$", "w", stderr);
    FILE *fin = freopen("CONIN$", "r", stdin);
    UNREFERENCED_PARAMETER(ferr);
    UNREFERENCED_PARAMETER(fin);
#endif

    /* Print debug information */
    printf("=========================================\n");
    printf("Shell Console Window\n");
    printf("=========================================\n");
    printf("Application started in GUI mode\n");
    printf("WinMain function is running\n");
    printf("Console should be visible\n");
    printf("=========================================\n");

    /* Set up console window properties */
    HWND consoleWindow = GetConsoleWindow();
    if (consoleWindow)
    {
        /* Make console window visible */
        ShowWindow(consoleWindow, SW_SHOW);
        printf("Console window should now be visible\n");

        /* Set console window title */
        SetConsoleTitle("Shell Console");

        /* Set console window properties - make it more visible */
        RECT rect;
        GetWindowRect(consoleWindow, &rect);
        int width = 800;
        int height = 500;
        int posX = GetSystemMetrics(SM_CXSCREEN) / 4;
        int posY = GetSystemMetrics(SM_CYSCREEN) / 4;
        SetWindowPos(consoleWindow, 0, posX, posY, width, height, SWP_SHOWWINDOW);

        /* Change console color for better visibility */
        HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
        if (hConsole != INVALID_HANDLE_VALUE)
        {
            SetConsoleTextAttribute(hConsole, FOREGROUND_GREEN | FOREGROUND_INTENSITY);
        }
        printf("Console window position and size adjusted\n");
    }
    else
    {
        MessageBox(NULL, "Failed to get console window handle", "Error", MB_ICONERROR);
    }

    /* Initialize console handles */
    g_hConsoleIn = GetStdHandle(STD_INPUT_HANDLE);
    g_hConsoleOut = GetStdHandle(STD_OUTPUT_HANDLE);

    /* Set GUI mode flag */
    g_GuiMode = TRUE;

    /* Load shell icon */
    printf("WinMain: Loading icon...\n");
    if (debug_file)
        fprintf(debug_file, "WinMain: Loading icon...\n");
    hIcon = LoadIcon(hInstance, MAKEINTRESOURCE(IDI_SHELL_ICON));
    if (!hIcon)
    {
        /* Fallback to default application icon */
        printf("WinMain: Icon not found, using default icon\n");
        if (debug_file)
            fprintf(debug_file, "WinMain: Icon not found, using default icon\n");
        hIcon = LoadIcon(NULL, IDI_APPLICATION);
    }

    /* Create background brush */
    printf("WinMain: Creating background brush...\n");
    g_hBackgroundBrush = CreateSolidBrush(RGB(12, 12, 12)); /* Dark background similar to Windows Terminal */

    /* Create font */
    printf("WinMain: Creating font...\n");
    g_hFont = CreateFont(16, 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE,
                         DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS,
                         CLEARTYPE_QUALITY, FIXED_PITCH | FF_MODERN, "Consolas");

    /* Initialize common controls */
    printf("WinMain: Initializing common controls...\n");
    INITCOMMONCONTROLSEX icc;
    icc.dwSize = sizeof(INITCOMMONCONTROLSEX);
    icc.dwICC = ICC_WIN95_CLASSES | ICC_BAR_CLASSES;
    InitCommonControlsEx(&icc);

    /* Register the window class */
    printf("WinMain: Registering window class...\n");
    wc.cbSize = sizeof(WNDCLASSEX);
    wc.style = CS_HREDRAW | CS_VREDRAW;
    wc.lpfnWndProc = WindowProc;
    wc.cbClsExtra = 0;
    wc.cbWndExtra = 0;
    wc.hInstance = hInstance;
    wc.hIcon = hIcon;
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);
    wc.hbrBackground = g_hBackgroundBrush;
    wc.lpszMenuName = NULL;
    wc.lpszClassName = "SimpleShellWindow";
    wc.hIconSm = hIcon;

    if (!RegisterClassEx(&wc))
    {
        printf("WinMain: Failed to register window class\n");
        MessageBox(NULL, "Window registration failed!", "Error", MB_ICONEXCLAMATION | MB_OK);
        return 1;
    }

    /* Create the main window */
    printf("WinMain: Creating main window...\n");
    hWnd = CreateWindowEx(
        WS_EX_COMPOSITED,
        "SimpleShellWindow",
        "Windows Terminal",
        WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT, CW_USEDEFAULT, 1024, 768,
        NULL, NULL, hInstance, NULL);

    if (!hWnd)
    {
        printf("WinMain: Failed to create window\n");
        MessageBox(NULL, "Window creation failed!", "Error", MB_ICONEXCLAMATION | MB_OK);
        return 1;
    }

    /* Initialize COM for the UI thread */
    printf("WinMain: Initializing COM...\n");
    HRESULT hr = CoInitializeEx(NULL, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);
    if (FAILED(hr))
    {
        printf("WinMain: COM initialization failed\n");
        MessageBox(NULL, "COM initialization failed!", "Error", MB_ICONEXCLAMATION | MB_OK);
        return 1;
    }

    /* Store main window handle */
    g_hMainWindow = hWnd;

    /* Show the window */
    printf("WinMain: Showing window...\n");
    ShowWindow(hWnd, SW_SHOW);
    UpdateWindow(hWnd);

    /* Run the shell in a background thread */
    printf("WinMain: Starting shell thread...\n");
    HANDLE hShellThread = CreateThread(NULL, 0, ShellThreadProc, NULL, 0, NULL);
    if (!hShellThread)
    {
        printf("WinMain: Failed to create shell thread\n");
        MessageBox(NULL, "Failed to start shell thread!", "Error", MB_ICONEXCLAMATION | MB_OK);
    }

    /* Message loop */
    printf("WinMain: Entering message loop...\n");
    while (GetMessage(&msg, NULL, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    printf("WinMain: Message loop exited\n");

    /* Wait for shell thread to finish */
    if (hShellThread)
    {
        printf("WinMain: Waiting for shell thread to finish...\n");
        WaitForSingleObject(hShellThread, INFINITE);
        CloseHandle(hShellThread);
    }

    /* Uninitialize COM */
    printf("WinMain: Uninitializing COM...\n");
    CoUninitialize();

    /* Clean up resources */
    printf("WinMain: Cleaning up resources...\n");
    if (debug_file)
        fprintf(debug_file, "WinMain: Cleaning up resources...\n");
    if (g_hFont)
    {
        DeleteObject(g_hFont);
    }

    if (g_hBackgroundBrush)
    {
        DeleteObject(g_hBackgroundBrush);
    }

    if (fout)
    {
        fclose(fout);
    }

    if (debug_file)
    {
        fprintf(debug_file, "WinMain: Exiting...\n");
        fclose(debug_file);
    }

    return (int)msg.wParam;
}

/**
 * CreateNewTab - Creates a new tab with an embedded console
 * @hTabControl: Handle to the tab control
 * @title: Title for the new tab
 *
 * Return: TRUE if successful, FALSE otherwise
 */
BOOL CreateNewTab(HWND hTabControl, const char *title)
{
    TCITEM tie;
    HWND hConsole;

    /* Don't exceed maximum tabs */
    if (g_TabCount >= MAX_TABS)
        return FALSE;

    /* Create a new console window */
    hConsole = CreateConsoleInWindow(g_hConsoleContainer);
    if (!hConsole)
        return FALSE;

    /* Add the tab */
    ZeroMemory(&tie, sizeof(TCITEM));
    tie.mask = TCIF_TEXT;
    tie.pszText = (LPSTR)title;

    int tabIndex = TabCtrl_InsertItem(hTabControl, g_TabCount, &tie);
    if (tabIndex == -1)
        return FALSE;

    /* Store tab information */
    g_Tabs[g_TabCount].hConsole = hConsole;
    strncpy(g_Tabs[g_TabCount].title, title, sizeof(g_Tabs[g_TabCount].title) - 1);
    g_Tabs[g_TabCount].isActive = FALSE;

    /* Increment tab count */
    g_TabCount++;

    /* Switch to the newly created tab */
    SwitchToTab(hTabControl, tabIndex);

    return TRUE;
}

/**
 * SwitchToTab - Switches to the specified tab
 * @hTabControl: Handle to the tab control
 * @tabIndex: Index of the tab to switch to
 *
 * Return: TRUE if successful, FALSE otherwise
 */
BOOL SwitchToTab(HWND hTabControl, int tabIndex)
{
    /* Validate tab index */
    if (tabIndex < 0 || tabIndex >= g_TabCount)
        return FALSE;

    /* Hide all console windows */
    for (int i = 0; i < g_TabCount; i++)
    {
        ShowWindow(g_Tabs[i].hConsole, SW_HIDE);
        g_Tabs[i].isActive = FALSE;
    }

    /* Show the selected console window */
    ShowWindow(g_Tabs[tabIndex].hConsole, SW_SHOW);
    g_Tabs[tabIndex].isActive = TRUE;
    g_ActiveTab = tabIndex;
    g_ConsoleHwnd = g_Tabs[tabIndex].hConsole;

    /* Select the tab */
    TabCtrl_SetCurSel(hTabControl, tabIndex);

    /* Position the console window */
    PositionConsoleInTab(g_Tabs[tabIndex].hConsole, hTabControl);

    return TRUE;
}

/**
 * CloseTab - Closes the specified tab
 * @hTabControl: Handle to the tab control
 * @tabIndex: Index of the tab to close
 *
 * Return: TRUE if successful, FALSE otherwise
 */
BOOL CloseTab(HWND hTabControl, int tabIndex)
{
    /* Validate tab index */
    if (tabIndex < 0 || tabIndex >= g_TabCount)
        return FALSE;

    /* Close the console window */
    ShowWindow(g_Tabs[tabIndex].hConsole, SW_HIDE);

    /* If there's a process associated with this tab, terminate it */
    if (g_Tabs[tabIndex].hProcess)
    {
        TerminateProcess(g_Tabs[tabIndex].hProcess, 0);
        CloseHandle(g_Tabs[tabIndex].hProcess);
    }

    /* Remove the tab */
    TabCtrl_DeleteItem(hTabControl, tabIndex);

    /* Shift the tab information */
    for (int i = tabIndex; i < g_TabCount - 1; i++)
    {
        g_Tabs[i] = g_Tabs[i + 1];
    }

    /* Decrement tab count */
    g_TabCount--;

    /* Switch to another tab if necessary */
    if (g_TabCount > 0)
    {
        int newTabIndex = (tabIndex < g_TabCount) ? tabIndex : g_TabCount - 1;
        SwitchToTab(hTabControl, newTabIndex);
    }

    return TRUE;
}

/**
 * CreateConsoleInWindow - Creates a console window as a child of the specified parent
 * @hParent: Handle to the parent window
 *
 * Return: Handle to the console window if successful, NULL otherwise
 */
HWND CreateConsoleInWindow(HWND hParent)
{
    HWND hConsole = NULL;
    HANDLE hProcess = NULL;

    /* Create the console window */
    hConsole = CreateWindowEx(
        0,
        "EDIT",
        NULL,
        WS_CHILD | WS_VISIBLE | WS_VSCROLL |
            ES_MULTILINE | ES_AUTOVSCROLL | ES_WANTRETURN,
        0, 0, 100, 100,
        hParent,
        NULL,
        GetModuleHandle(NULL),
        NULL);

    if (!hConsole)
        return NULL;

    /* Set console properties */
    HFONT hConsoleFont = CreateFont(
        18, 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE,
        DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS,
        CLEARTYPE_QUALITY, FIXED_PITCH | FF_MODERN, "Cascadia Mono");

    if (hConsoleFont)
    {
        SendMessage(hConsole, WM_SETFONT, (WPARAM)hConsoleFont, TRUE);
    }
    else
    {
        SendMessage(hConsole, WM_SETFONT, (WPARAM)g_hFont, TRUE);
    }

    /* Set window background and text colors the Windows way */
    SetClassLongPtr(hConsole, GCLP_HBRBACKGROUND, (LONG_PTR)CreateSolidBrush(RGB(12, 12, 12)));

    /* Add welcome message */
    SetWindowText(hConsole, "Windows Terminal\r\n\r\n");

    /* Set this as the active console */
    g_ConsoleHwnd = hConsole;
    g_GuiMode = TRUE;

    /* Store tab information */
    if (g_TabCount < MAX_TABS)
    {
        g_Tabs[g_TabCount].hConsole = hConsole;

        /* Create a thread to run the shell */
        DWORD threadId;
        hProcess = CreateThread(NULL, 0, ShellThreadProc, hConsole, 0, &threadId);

        if (hProcess)
        {
            g_Tabs[g_TabCount].hProcess = hProcess;
        }
    }

    /* Subclass the window to capture keyboard input */
    SetWindowLongPtr(hConsole, GWLP_USERDATA, (LONG_PTR)hConsole);
    g_OldEditProc = (WNDPROC)SetWindowLongPtr(hConsole, GWLP_WNDPROC, (LONG_PTR)EditSubclassProc);

    return hConsole;
}

/**
 * PositionConsoleInTab - Positions the console window inside the tab control
 * @hConsole: Handle to the console window
 * @hTabControl: Handle to the tab control
 *
 * Return: TRUE if successful, FALSE otherwise
 */
BOOL PositionConsoleInTab(HWND hConsole, HWND hTabControl)
{
    RECT tabRect;

    /* Get the tab control's client area */
    GetClientRect(hTabControl, &tabRect);

    /* Adjust for the tab headers */
    TabCtrl_AdjustRect(hTabControl, FALSE, &tabRect);

    /* Position the console window */
    SetWindowPos(
        hConsole,
        NULL,
        tabRect.left,
        tabRect.top,
        tabRect.right - tabRect.left,
        tabRect.bottom - tabRect.top,
        SWP_NOZORDER);

    return TRUE;
}

/**
 * create_menu - Creates the main menu
 * @hWnd: Handle to the main window
 *
 * Return: Menu handle
 */
HMENU create_menu(HWND hWnd)
{
    /* Create main menu */
    HMENU hMenu = CreateMenu();

    /* Create File menu */
    HMENU hFileMenu = CreatePopupMenu();
    AppendMenu(hFileMenu, MF_STRING, IDM_FILE_NEW, "&New Tab");
    AppendMenu(hFileMenu, MF_STRING, IDM_FILE_CLOSE, "&Close Tab");
    AppendMenu(hFileMenu, MF_SEPARATOR, 0, NULL);
    AppendMenu(hFileMenu, MF_STRING, IDM_FILE_EXIT, "E&xit");

    /* Create Help menu */
    HMENU hHelpMenu = CreatePopupMenu();
    AppendMenu(hHelpMenu, MF_STRING, IDM_HELP_ABOUT, "&About");

    /* Add menus to main menu */
    AppendMenu(hMenu, MF_POPUP, (UINT_PTR)hFileMenu, "&File");
    AppendMenu(hMenu, MF_POPUP, (UINT_PTR)hHelpMenu, "&Help");

    /* Set menu to window */
    SetMenu(hWnd, hMenu);

    return hMenu;
}

/**
 * ReadPipeThread - Thread function to read from the pipe and update the console
 * @param: Pointer to the READPARAM structure
 *
 * Return: Thread exit code
 */
DWORD WINAPI ReadPipeThread(LPVOID param)
{
    /* Extract parameters */
    typedef struct
    {
        HWND hConsole;
        HANDLE hReadPipe;
    } READPARAM;

    READPARAM *pParams = (READPARAM *)param;
    HWND hConsole = pParams->hConsole;
    HANDLE hReadPipe = pParams->hReadPipe;

    char buffer[4096];
    DWORD bytesRead;

    while (ReadFile(hReadPipe, buffer, sizeof(buffer) - 1, &bytesRead, NULL) && bytesRead > 0)
    {
        buffer[bytesRead] = '\0';

        /* Get current text length */
        int length = GetWindowTextLength(hConsole);

        /* Select the end of text */
        SendMessage(hConsole, EM_SETSEL, (WPARAM)length, (LPARAM)length);

        /* Add the new text */
        SendMessage(hConsole, EM_REPLACESEL, FALSE, (LPARAM)buffer);

        /* Scroll to the bottom */
        SendMessage(hConsole, EM_SCROLLCARET, 0, 0);
    }

    /* Free the parameter structure */
    free(param);

    return 0;
}

/**
 * EditSubclassProc - Subclass procedure for the edit control
 * @hWnd: Window handle
 * @uMsg: Message
 * @wParam: Word parameter
 * @lParam: Long parameter
 *
 * Return: Message result
 */
LRESULT CALLBACK EditSubclassProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    switch (uMsg)
    {
    case WM_CHAR:
    {
        /* Send character to the shell via stdin */
        if (wParam == VK_RETURN)
        {
            /* Get the current line from the edit control */
            int length = GetWindowTextLength(hWnd);
            char *buffer = (char *)malloc(length + 1);
            if (buffer)
            {
                GetWindowText(hWnd, buffer, length + 1);
                /* Find the start of the current line */
                char *start = strrchr(buffer, '\n');
                start = start ? start + 1 : buffer;
                /* Add the command to the process's stdin */
                /* For now, just append it to the buffer */
                char command[1024];
                sprintf(command, "\n");
                update_console_text(command);
                free(buffer);
            }
        }
        break;
    }
    }

    return CallWindowProc(g_OldEditProc, hWnd, uMsg, wParam, lParam);
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
#ifdef WIN_GUI
    /* Redirect to WinMain for GUI mode instead of showing an error */
    extern int WINAPI WinMain(HINSTANCE, HINSTANCE, LPSTR, int);
    HINSTANCE hInstance = GetModuleHandle(NULL);
    return WinMain(hInstance, NULL, GetCommandLine(), SW_SHOWNORMAL);
#endif

    /* Check if we should run in GUI mode */
    g_GuiMode = FALSE;
    for (int i = 1; i < argc; i++)
    {
        if (strcmp(argv[i], "--gui") == 0)
        {
            g_GuiMode = TRUE;
            continue;
        }
    }

    /* If not in GUI mode, set up console and run in console mode */
    if (!g_GuiMode)
    {
        /* Set up console window with proper settings */
        setup_console_window(TRUE);

        /* Display startup message with a distinctive appearance */
        printf("\033[0m");               /* Reset any previous formatting */
        printf("\033[38;2;50;255;255m"); /* RGB color similar to Windows Terminal accent */
        printf("╔════════════════════════════════════════════════════╗\n");
        printf("║                                                    ║\n");
        printf("║              SIMPLE SHELL - CONSOLE MODE           ║\n");
        printf("║         WITH ARABIC AND BAA LANGUAGE SUPPORT       ║\n");
        printf("║                                                    ║\n");
        printf("╚════════════════════════════════════════════════════╝\n");
        printf("\033[0m\n"); /* Reset text formatting */

        /* Test Arabic output */
        printf("\033[38;2;255;200;50m"); /* Gold color for Arabic text */
        printf("مرحبًا بكم في الصدفة البسيطة - وضع وحدة التحكم\n");
        printf("\033[0m\n"); /* Reset text formatting */

#ifdef WINDOWS
        /* Parse command line into argc/argv */
        LPWSTR *wargv = CommandLineToArgvW(GetCommandLineW(), &argc);
        if (wargv)
        {
            /* Run the shell in console mode */
            int result = shell_main(argc, argv);
            LocalFree(wargv);
            return result;
        }
#endif

        return shell_main(argc, argv);
    }

    /* GUI mode initialization */
    return initialize_console_for_gui();
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

    /* Update the console window */
    if (g_GuiMode && g_ConsoleHwnd)
    {
        update_console_text(buffer);
    }
    else
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

    /* Update the console window */
    if (g_GuiMode && g_ConsoleHwnd)
    {
        update_console_text(buffer);
        result = (int)strlen(buffer);
    }
    else
    {
        /* Use the original puts */
        result = fputs(buffer, stdout);
    }

    return result;
}
#endif
#endif