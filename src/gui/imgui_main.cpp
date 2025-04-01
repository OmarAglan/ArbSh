/**
 * imgui_main.cpp - Main entry point for ImGui-based GUI
 *
 * This file serves as the entry point for the ImGui-based GUI application.
 * It is the only GUI implementation, replacing the old Win32 GUI code.
 */

#include "imgui_shell.h"
#include "shell.h"
#include <tchar.h>

// ImGui includes
#include "imgui.h"
#include "imgui_impl_win32.h"
#include "imgui_impl_dx11.h"
#include <d3d11.h>

// Define UNUSED macro for parameter suppression
#define UNUSED(x) (void)(x)

#ifdef WINDOWS
#include <windows.h>

// Forward declarations
extern "C" int shell_main(int argc, char *argv[]);

// Global variables for DirectX (defined here since we need them in this file)
ID3D11Device *g_pd3dDevice = NULL;
ID3D11DeviceContext *g_pd3dDeviceContext = NULL;
IDXGISwapChain *g_pSwapChain = NULL;
ID3D11RenderTargetView *g_mainRenderTargetView = NULL;

// Forward declaration of ImGui_ImplWin32_WndProcHandler
extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

// Forward declarations of Direct3D functions
void CleanupRenderTarget()
{
    if (g_mainRenderTargetView)
    {
        g_mainRenderTargetView->Release();
        g_mainRenderTargetView = NULL;
    }
}

void CreateRenderTarget()
{
    ID3D11Texture2D *pBackBuffer;
    g_pSwapChain->GetBuffer(0, __uuidof(ID3D11Texture2D), (LPVOID *)&pBackBuffer);
    g_pd3dDevice->CreateRenderTargetView(pBackBuffer, NULL, &g_mainRenderTargetView);
    pBackBuffer->Release();
}

/**
 * WndProc - Windows procedure for ImGui window
 * @hWnd: Window handle
 * @message: Message
 * @wParam: Word parameter
 * @lParam: Long parameter
 *
 * Return: Result
 */
LRESULT CALLBACK ImGuiWndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    // Forward to ImGui Win32 implementation
    if (ImGui_ImplWin32_WndProcHandler(hWnd, message, wParam, lParam))
        return true;

    switch (message)
    {
    case WM_SIZE:
        if (g_pd3dDevice != NULL && wParam != SIZE_MINIMIZED)
        {
            CleanupRenderTarget();
            g_pSwapChain->ResizeBuffers(0, (UINT)LOWORD(lParam), (UINT)HIWORD(lParam), DXGI_FORMAT_UNKNOWN, 0);
            CreateRenderTarget();
        }
        return 0;
    case WM_SYSCOMMAND:
        if ((wParam & 0xfff0) == SC_KEYMENU) // Disable ALT application menu
            return 0;
        break;
    case WM_DESTROY:
        ::PostQuitMessage(0);
        return 0;
    }
    return ::DefWindowProc(hWnd, message, wParam, lParam);
}

#endif /* WINDOWS */

// Main entry point for ImGui-based GUI
// FIX: Added [[maybe_unused]] to nCmdShow for consistency (C++ style)
extern "C" int imgui_main(HINSTANCE hInstance, HINSTANCE hPrevInstance, [[maybe_unused]] LPSTR lpCmdLine, [[maybe_unused]] int nCmdShow)
{
    UNUSED(hPrevInstance); // Using UNUSED macro for hPrevInstance as before
    // UNUSED(lpCmdLine); // Keeping previous style for lpCmdLine
    // UNUSED(nCmdShow);  // Using [[maybe_unused]] now in declaration

    // Register window class
    WNDCLASSEX wc = {sizeof(WNDCLASSEX), CS_CLASSDC, ImGuiWndProc, 0L, 0L, hInstance, NULL, NULL, NULL, NULL, _T("ArbSh ImGui"), NULL};
    RegisterClassEx(&wc);

    // Create window
    HWND hwnd = CreateWindow(wc.lpszClassName, _T("ArbSh Terminal"), WS_OVERLAPPEDWINDOW, 100, 100, 1280, 720, NULL, NULL, wc.hInstance, NULL);

    // Initialize ImGui
    if (!imgui_init(hInstance, &hwnd, 1280, 720, "ArbSh Terminal"))
    {
        DestroyWindow(hwnd);
        UnregisterClass(wc.lpszClassName, wc.hInstance);
        return 1;
    }

    // Main loop
    ShowWindow(hwnd, SW_SHOWDEFAULT);
    UpdateWindow(hwnd);

    MSG msg;
    ZeroMemory(&msg, sizeof(msg));
    while (msg.message != WM_QUIT)
    {
        if (PeekMessage(&msg, NULL, 0U, 0U, PM_REMOVE))
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
            continue;
        }

        if (!imgui_main_loop(hwnd))
            break;
    }

    // Shutdown ImGui
    imgui_shutdown();

    // Cleanup
    DestroyWindow(hwnd);
    UnregisterClass(wc.lpszClassName, wc.hInstance);

    return (int)msg.wParam;
}