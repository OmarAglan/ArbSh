#include "shell.h"

#ifdef WINDOWS

#include <windows.h>
#include <io.h>
#include <fcntl.h>
#include <shellapi.h>
#include <commctrl.h>  /* For common controls */

/* Forward declaration of the regular main function */
int main(int ac, char **av);

/* Window class name and window title */
#define WINDOW_CLASS_NAME "SimpleShellWindowClass"
#define WINDOW_TITLE "Simple Shell - Modern UI"

/* Window dimensions */
#define WINDOW_WIDTH 1000
#define WINDOW_HEIGHT 700

/* Global variables */
HWND g_hMainWindow = NULL;
HWND g_hConsoleContainer = NULL;
HWND g_hStatusBar = NULL;
HWND g_hToolBar = NULL;
HFONT g_hFont = NULL;
HBRUSH g_hBackgroundBrush = NULL;
HANDLE g_hConsoleOut = NULL;
HANDLE g_hConsoleIn = NULL;

/**
 * ConsoleEventHandler - Handles console control events
 * @dwCtrlType: The type of control event
 *
 * Return: TRUE if handled, FALSE otherwise
 */
BOOL WINAPI ConsoleEventHandler(DWORD dwCtrlType)
{
    switch (dwCtrlType)
    {
    case CTRL_C_EVENT:
    case CTRL_BREAK_EVENT:
    case CTRL_CLOSE_EVENT:
        /* Handle cleanup if needed */
        return TRUE;
    default:
        return FALSE;
    }
}

/**
 * convert_args - Convert wide character arguments to UTF-8
 * @argc: Number of arguments
 * @wargv: Wide character arguments
 *
 * Return: Array of UTF-8 strings
 */
char **convert_args(int argc, LPWSTR *wargv)
{
    char **argv;
    int i;

    argv = (char **)malloc(argc * sizeof(char *));
    if (!argv)
        return NULL;

    for (i = 0; i < argc; i++)
    {
        int size_needed = WideCharToMultiByte(CP_UTF8, 0, wargv[i], -1, NULL, 0, NULL, NULL);
        argv[i] = (char *)malloc(size_needed);
        if (!argv[i])
        {
            /* Free previously allocated memory */
            while (--i >= 0)
                free(argv[i]);
            free(argv);
            return NULL;
        }
        WideCharToMultiByte(CP_UTF8, 0, wargv[i], -1, argv[i], size_needed, NULL, NULL);
    }

    return argv;
}

/**
 * free_args - Free converted arguments
 * @argc: Number of arguments
 * @argv: Arguments to free
 */
void free_args(int argc, char **argv)
{
    int i;

    for (i = 0; i < argc; i++)
        free(argv[i]);
    free(argv);
}

/**
 * setup_console - Set up the console window with proper settings
 */
void setup_console(void)
{
    HANDLE hOut = g_hConsoleOut;
    HANDLE hIn = g_hConsoleIn;
    DWORD mode;
    
    /* Configure console for better appearance */
    if (hOut != INVALID_HANDLE_VALUE)
    {
        /* Set console mode for output */
        if (GetConsoleMode(hOut, &mode))
        {
            mode |= ENABLE_PROCESSED_OUTPUT | ENABLE_VIRTUAL_TERMINAL_PROCESSING;
            SetConsoleMode(hOut, mode);
        }
        
        /* Set console font */
        CONSOLE_FONT_INFOEX cfi;
        cfi.cbSize = sizeof(cfi);
        cfi.nFont = 0;
        cfi.dwFontSize.X = 0;  /* Default width */
        cfi.dwFontSize.Y = 16; /* Modern terminal size */
        cfi.FontFamily = FF_DONTCARE;
        cfi.FontWeight = FW_NORMAL;
        wcscpy(cfi.FaceName, L"Cascadia Mono"); /* Windows Terminal default font */
        SetCurrentConsoleFontEx(hOut, FALSE, &cfi);
        
        /* Set console window size and buffer size */
        COORD bufferSize = {120, 9000}; /* Width: 120, Height: 9000 (large scrollback) */
        SetConsoleScreenBufferSize(hOut, bufferSize);
        
        SMALL_RECT windowRect = {0, 0, 119, 30}; /* Width: 120, Height: 30 */
        SetConsoleWindowInfo(hOut, TRUE, &windowRect);
        
        /* Set console colors - dark background with bright text (Windows Terminal style) */
        SetConsoleTextAttribute(hOut, FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE | FOREGROUND_INTENSITY);
    }
    
    /* Configure console for input */
    if (hIn != INVALID_HANDLE_VALUE)
    {
        if (GetConsoleMode(hIn, &mode))
        {
            mode |= ENABLE_ECHO_INPUT | ENABLE_LINE_INPUT | ENABLE_PROCESSED_INPUT;
            mode |= ENABLE_VIRTUAL_TERMINAL_INPUT; /* Enable VT input processing */
            SetConsoleMode(hIn, mode);
        }
    }
}

/**
 * create_ui_elements - Create UI elements for the main window
 * @hWnd: Handle to the main window
 */
void create_ui_elements(HWND hWnd)
{
    RECT rcClient;
    int statusBarHeight = 25;
    int toolBarHeight = 40;
    
    /* Initialize common controls */
    INITCOMMONCONTROLSEX icex;
    icex.dwSize = sizeof(INITCOMMONCONTROLSEX);
    icex.dwICC = ICC_BAR_CLASSES | ICC_COOL_CLASSES;
    InitCommonControlsEx(&icex);
    
    /* Get client area dimensions */
    GetClientRect(hWnd, &rcClient);
    
    /* Create toolbar */
    g_hToolBar = CreateWindowEx(
        0, TOOLBARCLASSNAME, NULL,
        WS_CHILD | WS_VISIBLE | TBSTYLE_FLAT | TBSTYLE_TOOLTIPS,
        0, 0, rcClient.right, toolBarHeight,
        hWnd, NULL, GetModuleHandle(NULL), NULL);
    
    /* Create status bar */
    g_hStatusBar = CreateWindowEx(
        0, STATUSCLASSNAME, NULL,
        WS_CHILD | WS_VISIBLE | SBARS_SIZEGRIP,
        0, rcClient.bottom - statusBarHeight, rcClient.right, statusBarHeight,
        hWnd, NULL, GetModuleHandle(NULL), NULL);
    
    /* Set status bar parts */
    int statwidths[] = {rcClient.right / 3, 2 * rcClient.right / 3, -1};
    SendMessage(g_hStatusBar, SB_SETPARTS, 3, (LPARAM)statwidths);
    SendMessageW(g_hStatusBar, SB_SETTEXTW, 0, (LPARAM)L"Ready");
    SendMessageW(g_hStatusBar, SB_SETTEXTW, 1, (LPARAM)L"Language: English");
    SendMessageW(g_hStatusBar, SB_SETTEXTW, 2, (LPARAM)L"UTF-8 Enabled");
    
    /* Create console container */
    g_hConsoleContainer = CreateWindowEx(
        WS_EX_CLIENTEDGE, "STATIC", NULL,
        WS_CHILD | WS_VISIBLE | SS_SUNKEN,
        0, toolBarHeight, rcClient.right, rcClient.bottom - toolBarHeight - statusBarHeight,
        hWnd, NULL, GetModuleHandle(NULL), NULL);
}

/**
 * resize_ui_elements - Resize UI elements when the main window is resized
 * @hWnd: Handle to the main window
 */
void resize_ui_elements(HWND hWnd)
{
    RECT rcClient;
    int statusBarHeight = 25;
    int toolBarHeight = 40;
    
    /* Get client area dimensions */
    GetClientRect(hWnd, &rcClient);
    
    /* Resize toolbar */
    SetWindowPos(g_hToolBar, NULL, 0, 0, rcClient.right, toolBarHeight, SWP_NOZORDER);
    
    /* Resize status bar */
    SendMessage(g_hStatusBar, WM_SIZE, 0, 0);
    
    /* Update status bar parts */
    int statwidths[] = {rcClient.right / 3, 2 * rcClient.right / 3, -1};
    SendMessage(g_hStatusBar, SB_SETPARTS, 3, (LPARAM)statwidths);
    
    /* Resize console container */
    SetWindowPos(g_hConsoleContainer, NULL, 0, toolBarHeight, 
                 rcClient.right, rcClient.bottom - toolBarHeight - statusBarHeight, 
                 SWP_NOZORDER);
    
    /* Resize console window */
    HWND hConsoleWnd = GetConsoleWindow();
    if (hConsoleWnd)
    {
        RECT rcContainer;
        GetClientRect(g_hConsoleContainer, &rcContainer);
        
        /* Get console window position relative to screen */
        RECT rcConsole;
        GetWindowRect(hConsoleWnd, &rcConsole);
        
        /* Convert container client coordinates to screen coordinates */
        POINT ptContainer = {0, 0};
        ClientToScreen(g_hConsoleContainer, &ptContainer);
        
        /* Set console window position and size */
        SetWindowPos(hConsoleWnd, NULL, 
                     ptContainer.x, ptContainer.y,
                     rcContainer.right, rcContainer.bottom,
                     SWP_NOZORDER);
    }
}

/**
 * WindowProc - Window procedure for the main window
 * @hWnd: Handle to the window
 * @uMsg: Message
 * @wParam: Additional message information
 * @lParam: Additional message information
 *
 * Return: Result of message processing
 */
LRESULT CALLBACK WindowProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    switch (uMsg)
    {
    case WM_CREATE:
        /* Create UI elements */
        create_ui_elements(hWnd);
        return 0;
        
    case WM_SIZE:
        /* Resize UI elements */
        resize_ui_elements(hWnd);
        return 0;
        
    case WM_CTLCOLORSTATIC:
        /* Set background color for static controls */
        if ((HWND)lParam == g_hConsoleContainer)
        {
            return (LRESULT)g_hBackgroundBrush;
        }
        break;
        
    case WM_DESTROY:
        /* Clean up resources */
        if (g_hFont)
            DeleteObject(g_hFont);
        if (g_hBackgroundBrush)
            DeleteObject(g_hBackgroundBrush);
        PostQuitMessage(0);
        return 0;
    }
    
    return DefWindowProc(hWnd, uMsg, wParam, lParam);
}

/**
 * register_window_class - Register the window class
 * @hInstance: Instance handle
 *
 * Return: TRUE if successful, FALSE otherwise
 */
BOOL register_window_class(HINSTANCE hInstance)
{
    WNDCLASSEX wcex;
    
    wcex.cbSize = sizeof(WNDCLASSEX);
    wcex.style = CS_HREDRAW | CS_VREDRAW;
    wcex.lpfnWndProc = WindowProc;
    wcex.cbClsExtra = 0;
    wcex.cbWndExtra = 0;
    wcex.hInstance = hInstance;
    wcex.hIcon = LoadIcon(NULL, IDI_APPLICATION);
    wcex.hCursor = LoadCursor(NULL, IDC_ARROW);
    wcex.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
    wcex.lpszMenuName = NULL;
    wcex.lpszClassName = WINDOW_CLASS_NAME;
    wcex.hIconSm = LoadIcon(NULL, IDI_APPLICATION);
    
    return RegisterClassEx(&wcex);
}

/**
 * create_main_window - Create the main window
 * @hInstance: Instance handle
 *
 * Return: Handle to the main window
 */
HWND create_main_window(HINSTANCE hInstance)
{
    /* Create the main window */
    HWND hWnd = CreateWindowEx(
        WS_EX_OVERLAPPEDWINDOW,
        WINDOW_CLASS_NAME,
        WINDOW_TITLE,
        WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT, CW_USEDEFAULT,
        WINDOW_WIDTH, WINDOW_HEIGHT,
        NULL, NULL, hInstance, NULL);
    
    if (hWnd)
    {
        /* Create font for UI elements */
        g_hFont = CreateFont(16, 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE,
                            DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS,
                            CLEARTYPE_QUALITY, DEFAULT_PITCH | FF_DONTCARE, "Segoe UI");
        
        /* Create background brush */
        g_hBackgroundBrush = CreateSolidBrush(RGB(30, 30, 30));
        
        /* Show the window */
        ShowWindow(hWnd, SW_SHOW);
        UpdateWindow(hWnd);
    }
    
    return hWnd;
}

/**
 * WinMain - Windows GUI entry point
 * @hInstance: The instance handle
 * @hPrevInstance: The previous instance handle (always NULL)
 * @lpCmdLine: The command line string
 * @nCmdShow: The show window command
 *
 * Return: The exit code
 */
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
                  LPSTR lpCmdLine, int nCmdShow)
{
    int argc = 0;
    LPWSTR *wargv = NULL;
    char **argv = NULL;
    int result;
    FILE *fout, *fin, *ferr;
    MSG msg;
    
    /* Unused parameters */
    (void)hPrevInstance;
    (void)lpCmdLine;
    (void)nCmdShow;
    
    /* Register window class */
    if (!register_window_class(hInstance))
    {
        MessageBox(NULL, "Window Registration Failed!", "Error", MB_ICONEXCLAMATION | MB_OK);
        return 1;
    }
    
    /* Create main window */
    g_hMainWindow = create_main_window(hInstance);
    if (!g_hMainWindow)
    {
        MessageBox(NULL, "Window Creation Failed!", "Error", MB_ICONEXCLAMATION | MB_OK);
        return 1;
    }
    
    /* Allocate a console for this application */
    if (!AllocConsole())
    {
        MessageBox(NULL, "Failed to allocate console", "Error", MB_ICONERROR);
        return 1;
    }
    
    /* Set the console title */
    SetConsoleTitle(TEXT("Simple Shell Console"));
    
    /* Set up the console event handler */
    SetConsoleCtrlHandler(ConsoleEventHandler, TRUE);
    
    /* Get handles to console input and output */
    g_hConsoleOut = GetStdHandle(STD_OUTPUT_HANDLE);
    g_hConsoleIn = GetStdHandle(STD_INPUT_HANDLE);
    
    if (g_hConsoleOut == INVALID_HANDLE_VALUE || g_hConsoleIn == INVALID_HANDLE_VALUE)
    {
        MessageBox(NULL, "Failed to get console handles", "Error", MB_ICONERROR);
        FreeConsole();
        return 1;
    }
    
    /* Set up console appearance and behavior */
    setup_console();
    
    /* Redirect standard input/output to console */
    int outfd = _open_osfhandle((intptr_t)g_hConsoleOut, _O_TEXT);
    int infd = _open_osfhandle((intptr_t)g_hConsoleIn, _O_TEXT);
    
    if (outfd == -1 || infd == -1)
    {
        MessageBox(NULL, "Failed to create file descriptors", "Error", MB_ICONERROR);
        FreeConsole();
        return 1;
    }
    
    fout = _fdopen(outfd, "w");
    fin = _fdopen(infd, "r");
    ferr = _fdopen(outfd, "w");
    
    if (!fout || !fin || !ferr)
    {
        MessageBox(NULL, "Failed to redirect standard streams", "Error", MB_ICONERROR);
        FreeConsole();
        return 1;
    }
    
    *stdout = *fout;
    *stdin = *fin;
    *stderr = *ferr;
    
    setvbuf(stdout, NULL, _IONBF, 0);
    setvbuf(stderr, NULL, _IONBF, 0);
    
    /* Make the console window visible and bring it to the foreground */
    HWND consoleWindow = GetConsoleWindow();
    if (consoleWindow)
    {
        /* Embed console window in the container */
        RECT rcContainer;
        GetClientRect(g_hConsoleContainer, &rcContainer);
        
        /* Get container position relative to screen */
        POINT ptContainer = {0, 0};
        ClientToScreen(g_hConsoleContainer, &ptContainer);
        
        /* Set console window style */
        LONG style = GetWindowLong(consoleWindow, GWL_STYLE);
        style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZE | WS_MAXIMIZE | WS_SYSMENU);
        SetWindowLong(consoleWindow, GWL_STYLE, style);
        
        /* Set console window position and size */
        SetWindowPos(consoleWindow, NULL, 
                     ptContainer.x, ptContainer.y,
                     rcContainer.right, rcContainer.bottom,
                     SWP_NOZORDER);
        
        /* Show the console window */
        ShowWindow(consoleWindow, SW_SHOW);
    }
    
    /* Display startup message with a distinctive appearance */
    printf("\033[0m"); /* Reset any previous formatting */
    printf("\033[38;2;50;255;255m"); /* RGB color similar to Windows Terminal accent */
    printf("╔════════════════════════════════════════════════════╗\n");
    printf("║                                                    ║\n");
    printf("║              SIMPLE SHELL - MODERN UI              ║\n");
    printf("║                                                    ║\n");
    printf("╚════════════════════════════════════════════════════╝\n");
    printf("\033[0m\n"); /* Reset text formatting */
    
    /* Set the default console color scheme to match Windows Terminal */
    printf("\033[40m"); /* Black background */
    printf("\033[37;1m"); /* Bright white text */
    
    /* Parse command line into argc/argv */
    wargv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (wargv)
    {
        /* Convert wide character arguments to UTF-8 */
        argv = convert_args(argc, wargv);
        
        /* Create a thread to run the shell */
        HANDLE hThread = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)main, 
                                     argv ? argv : (void *)1, 0, NULL);
        if (!hThread)
        {
            printf("Failed to create shell thread\n");
            result = -1;
        }
        else
        {
            /* Message loop */
            while (GetMessage(&msg, NULL, 0, 0))
            {
                TranslateMessage(&msg);
                DispatchMessage(&msg);
            }
            
            /* Wait for the shell thread to exit */
            WaitForSingleObject(hThread, INFINITE);
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
        /* Message loop */
        while (GetMessage(&msg, NULL, 0, 0))
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }
        
        result = (int)msg.wParam;
    }
    
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

#endif /* WINDOWS */ 