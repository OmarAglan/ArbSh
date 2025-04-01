/**
 * imgui_shell.cpp - ImGui integration with the shell
 *
 * This file provides the implementation for using ImGui as the GUI framework
 * for the ArbSh terminal. It is the sole GUI implementation, replacing the
 * old Win32 GUI code.
 */

#include "imgui_shell.h"
#include "shell.h" // For shell types if needed elsewhere, maybe remove if unused

#ifdef WINDOWS
#include <windows.h>
#include <string>
#include <vector>
#include <sstream>
#include <stdio.h>
#include <filesystem> // For path manipulation
#include <algorithm>
#include "terminal_tab.h" // For terminal_tab_t and related functions

// Define UNUSED macro for parameter suppression
#define UNUSED(x) (void)(x)

// ImGui includes
#include "imgui.h"
#include "imgui_impl_win32.h"
#include "imgui_impl_dx11.h"
#include <d3d11.h>
#include <tchar.h> // For _T macro if needed, though L"" works too

// Forward declare message handler from imgui_impl_win32.cpp
extern IMGUI_IMPL_API LRESULT ImGuiWndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

// Global variables for DirectX - extern from imgui_main.cpp
extern ID3D11Device *g_pd3dDevice;
extern ID3D11DeviceContext *g_pd3dDeviceContext;
extern IDXGISwapChain *g_pSwapChain;
extern ID3D11RenderTargetView *g_mainRenderTargetView;

// Forward declarations
std::string get_hsh_executable_path();
BOOL CreateDeviceD3D(HWND hWnd);
void CleanupDeviceD3D();
BOOL imgui_init(HINSTANCE hInstance, HWND *hWnd, int width, int height, const char *title);
void imgui_shutdown(void);
void RenderShell();
BOOL imgui_main_loop(HWND hWnd);
// These are implemented in imgui_main.cpp or elsewhere
extern void CreateRenderTarget();
extern void CleanupRenderTarget();

// Tab data structure
struct TabData
{
    std::string name;
    terminal_tab_t *term_tab; // Pointer to the terminal tab structure
    bool isActive;
    char inputBuffer[1024]; // Keep input buffer here for ImGui widget

    TabData(const std::string &tab_name) : name(tab_name), term_tab(nullptr), isActive(false)
    {
        inputBuffer[0] = '\0';
    }

    ~TabData()
    {
        if (term_tab)
        {
            terminal_tab_free(term_tab); // Ensure cleanup
            term_tab = nullptr;
        }
    }
};

// Global state
static std::vector<TabData> g_tabs;
static int g_activeTab = -1; // Initialize to -1 (no active tab initially)
static bool g_shouldExit = false;
static int g_nextTabId = 1;

// --- START OF MOVED/CORRECTED FUNCTION DEFINITIONS ---

// Example helper function (Windows specific)
std::string get_hsh_executable_path()
{
    wchar_t path[MAX_PATH];
    if (GetModuleFileNameW(NULL, path, MAX_PATH) == 0)
    {
        fprintf(stderr, "Error: GetModuleFileNameW failed (%lu)\n", GetLastError());
        return ""; // Return empty on failure
    }

    try
    { // Use try-catch for filesystem operations
        std::filesystem::path guiPath = path;
        std::filesystem::path hshPath = guiPath.parent_path() / "hsh.exe";

        // Check if the file exists
        if (!std::filesystem::exists(hshPath))
        {
            fprintf(stderr, "Error: hsh.exe not found at expected path: %ls\n", hshPath.c_str());
            // Fallback: Maybe try relative path from CWD if needed?
            // std::filesystem::path relativeHshPath = "./hsh.exe";
            // if (std::filesystem::exists(relativeHshPath)) {
            //    hshPath = relativeHshPath;
            // } else {
            return ""; // Return empty if not found
                       // }
        }

        // Convert wide string path to narrow string for create_shell_process
        std::wstring ws(hshPath.c_str());
        // Simple narrow conversion (may fail for non-ASCII paths, consider WideCharToMultiByte for robustness)
        std::string narrow_path(ws.begin(), ws.end());
        return narrow_path;
    }
    catch (const std::filesystem::filesystem_error &e)
    {
        fprintf(stderr, "Filesystem error: %s\n", e.what());
        return "";
    }
    catch (...)
    {
        fprintf(stderr, "Unknown error getting hsh executable path.\n");
        return "";
    }
}

/**
 * imgui_update_console_text - DO NOT USE for process output.
 *                             Only for GUI-internal messages if really needed.
 *                             Prefer terminal_tab_append_buffer.
 * @text: Text to add to the console
 */
void imgui_update_console_text([[maybe_unused]] const char *text) // Mark text as potentially unused
{
    // THIS FUNCTION IS LARGELY OBSOLETE NOW.
    // Process output should be read and displayed directly from term_tab->buffer.
    // GUI-generated messages should use terminal_tab_append_buffer.

    // If you absolutely need to inject text outside the process stream:
    if (!text || g_tabs.empty() || g_activeTab < 0 || g_activeTab >= (int)g_tabs.size())
        return;

    terminal_tab_t *current_term = g_tabs[g_activeTab].term_tab;
    if (current_term)
    {
        // Append directly to the terminal's buffer
        terminal_tab_append_buffer(current_term, text, strlen(text));
        current_term->scroll_to_bottom = true;
    }
}

/**
 * imgui_main - Main entry point for ImGui-based GUI (now called by shell_entry)
 * @hInstance: Instance handle
 * @hPrevInstance: Previous instance handle (unused)
 * @lpCmdLine: Command line arguments
 * @nCmdShow: Show command
 *
 * Return: Exit code
 */
// FIX: Applied [[maybe_unused]] to nCmdShow here as well for consistency with imgui_main.cpp
int imgui_main(HINSTANCE hInstance, HINSTANCE hPrevInstance, [[maybe_unused]] LPSTR lpCmdLine, [[maybe_unused]] int nCmdShow)
{
    UNUSED(hPrevInstance); // Parameter is unused
    // UNUSED(lpCmdLine); // lpCmdLine might be useful later if needed
    // UNUSED(nCmdShow);  // nCmdShow might be useful later

    // Create main window
    HWND hWnd = NULL;
    if (!imgui_init(hInstance, &hWnd, 1280, 720, "ArbSh Terminal with ImGui"))
        return 1;

    // Add welcome message TO THE INITIAL TAB'S BUFFER
    std::string welcomeMsg =
        "\r\n" // Use CRLF for terminal consistency
        "╔══════════════════════════════════════════════════════════╗\r\n"
        "║                                                          ║\r\n"
        "║                  ArbSh Terminal (ImGui)                  ║\r\n"
        "║         MODERN SHELL WITH ARABIC SUPPORT                 ║\r\n"
        "║                                                          ║\r\n"
        "╚══════════════════════════════════════════════════════════╝\r\n\r\n"
        // "Type help for available commands.\r\n\r\n" // Shell will provide its own help
        "=> مرحبًا بكم في ArbSh - واجهة مستخدم حديثة\r\n\r\n";

    // Append welcome message directly to the first tab's buffer if it exists
    if (!g_tabs.empty() && g_tabs[0].term_tab)
    {
        terminal_tab_append_buffer(g_tabs[0].term_tab, welcomeMsg.c_str(), welcomeMsg.length());
        g_tabs[0].term_tab->scroll_to_bottom = true; // Ensure scroll after adding text
    }
    else
    {
        fprintf(stderr, "Warning: Could not add welcome message, initial tab not ready.\n");
    }

    // Main loop
    MSG msg;
    ZeroMemory(&msg, sizeof(msg));
    while (msg.message != WM_QUIT)
    {
        // Poll and handle messages (blocking)
        // Use PeekMessage for non-blocking alternative if needed for background tasks
        if (PeekMessage(&msg, NULL, 0U, 0U, PM_REMOVE))
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
            continue; // Process next message
        }

        // --- Process Active Tab Output ---
        // Moved inside the main loop, before rendering
        if (g_activeTab >= 0 && g_activeTab < (int)g_tabs.size())
        {
            terminal_tab_t *active_term = g_tabs[g_activeTab].term_tab;
            if (active_term)
            { // Check if term_tab exists
                if (active_term->is_active)
                {
                    // Process non-blockingly (timeout 0)
                    terminal_tab_process(active_term);
                }
                else
                {
                    // Check if it just finished to update display buffer once
                    if (is_shell_process_running(&active_term->process) == false && active_term->process.exit_code != -999) // Pass the process member
                    {                                                                                                       // Use a flag/special code
                        char exitMsg[128];
                        snprintf(exitMsg, sizeof(exitMsg), "\r\n[Process ended with code %d]\r\n", active_term->process.exit_code); // Access process.exit_code
                        terminal_tab_append_buffer(active_term, exitMsg, strlen(exitMsg));
                        active_term->process.exit_code = -999; // Mark on the process struct
                    }
                }
            }
        }
        // --- End Process Active Tab ---

        // Run the ImGui main loop (rendering and UI logic)
        if (!imgui_main_loop(hWnd)) // This renders the frame AND checks g_shouldExit
            break;                  // Exit if main loop returns false
    }

    // Cleanup
    g_tabs.clear(); // This will call the destructor for each TabData element, freeing term_tab
    imgui_shutdown();
    if (hWnd)
        DestroyWindow(hWnd);                          // Check if hWnd is valid before destroying
    UnregisterClassW(L"ImGuiShellWindow", hInstance); // Use hInstance used for registration

    return (int)msg.wParam;
}

/**
 * CreateDeviceD3D - Initialize Direct3D for ImGui
 * @hWnd: Window handle
 *
 * Return: TRUE if successful, FALSE otherwise
 */
BOOL CreateDeviceD3D(HWND hWnd)
{
    // Setup swap chain
    DXGI_SWAP_CHAIN_DESC sd;
    ZeroMemory(&sd, sizeof(sd));
    sd.BufferCount = 2;
    sd.BufferDesc.Width = 0; // Use automatic sizing
    sd.BufferDesc.Height = 0;
    sd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    sd.BufferDesc.RefreshRate.Numerator = 60;
    sd.BufferDesc.RefreshRate.Denominator = 1;
    sd.Flags = DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH; // May not be needed for windowed mode
    sd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    sd.OutputWindow = hWnd;
    sd.SampleDesc.Count = 1; // No multisampling
    sd.SampleDesc.Quality = 0;
    sd.Windowed = TRUE;
    sd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD; // Or DXGI_SWAP_EFFECT_FLIP_DISCARD

    UINT createDeviceFlags = 0;
#ifdef _DEBUG
// createDeviceFlags |= D3D11_CREATE_DEVICE_DEBUG; // Enable debug layer if SDK Layers are installed
#endif

    D3D_FEATURE_LEVEL featureLevel;
    const D3D_FEATURE_LEVEL featureLevelArray[2] = {
        // Try DirectX 11, then 10
        D3D_FEATURE_LEVEL_11_0,
        D3D_FEATURE_LEVEL_10_0,
    };
    HRESULT res = D3D11CreateDeviceAndSwapChain(NULL, D3D_DRIVER_TYPE_HARDWARE, NULL, createDeviceFlags, featureLevelArray, 2, D3D11_SDK_VERSION, &sd, &g_pSwapChain, &g_pd3dDevice, &featureLevel, &g_pd3dDeviceContext);
    if (res != S_OK)
    {
        fprintf(stderr, "Error: D3D11CreateDeviceAndSwapChain failed (HRESULT: 0x%lX)\n", res);
        return FALSE;
    }

    CreateRenderTarget(); // Create the render target view for the swap chain's back buffer
    return TRUE;
}

/**
 * CleanupDeviceD3D - Clean up Direct3D resources
 */
void CleanupDeviceD3D()
{
    CleanupRenderTarget(); // Release render target view first
    if (g_pSwapChain)
    {
        g_pSwapChain->Release();
        g_pSwapChain = NULL;
    }
    if (g_pd3dDeviceContext)
    {
        g_pd3dDeviceContext->Release();
        g_pd3dDeviceContext = NULL;
    }
    if (g_pd3dDevice)
    {
        g_pd3dDevice->Release();
        g_pd3dDevice = NULL;
    }
}

// --- END OF MOVED/CORRECTED FUNCTION DEFINITIONS ---

/**
 * imgui_init - Initialize ImGui and create the main window
 * @hInstance: Application instance handle
 * @hWnd: Pointer to receive the window handle
 * @width: Initial window width
 * @height: Initial window height
 * @title: Window title
 *
 * Return: TRUE if successful, FALSE otherwise
 */
BOOL imgui_init(HINSTANCE hInstance, HWND *hWnd, int width, int height, const char *title)
{
    UNUSED(title); // Parameter is unused

    // Create application window
    WNDCLASSEXW wc = {sizeof(wc), CS_CLASSDC, ImGuiWndProc, 0L, 0L, hInstance, NULL, NULL, NULL, NULL, L"ImGuiShellWindow", NULL};
    if (!RegisterClassExW(&wc))
    {
        fprintf(stderr, "Error: RegisterClassExW failed (%lu)\n", GetLastError());
        return FALSE;
    }

    // Get screen dimensions for better default positioning
    int screenWidth = GetSystemMetrics(SM_CXSCREEN);
    int screenHeight = GetSystemMetrics(SM_CYSCREEN);
    int windowX = (screenWidth > width) ? (screenWidth - width) / 2 : 0;
    int windowY = (screenHeight > height) ? (screenHeight - height) / 2 : 0;

    // Convert title to wide string for CreateWindowW
    const wchar_t *windowTitle = L"ArbSh Terminal"; // Use wide string literal

    *hWnd = CreateWindowW(wc.lpszClassName, windowTitle, WS_OVERLAPPEDWINDOW,
                          windowX, windowY, width, height,
                          NULL, NULL, hInstance, NULL);

    if (!*hWnd)
    {
        fprintf(stderr, "Error: CreateWindowW failed (%lu)\n", GetLastError());
        UnregisterClassW(wc.lpszClassName, hInstance);
        return FALSE;
    }

    // Initialize Direct3D
    if (!CreateDeviceD3D(*hWnd))
    {
        CleanupDeviceD3D(); // Cleanup any partial D3D setup
        DestroyWindow(*hWnd);
        UnregisterClassW(wc.lpszClassName, hInstance);
        return FALSE;
    }

    // Show the window
    ShowWindow(*hWnd, SW_SHOWDEFAULT);
    UpdateWindow(*hWnd);

    // Setup Dear ImGui context
    IMGUI_CHECKVERSION();
    ImGui::CreateContext();
    ImGuiIO &io = ImGui::GetIO();
    (void)io;
    io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard; // Enable Keyboard Controls
    // io.ConfigFlags |= ImGuiConfigFlags_DockingEnable;           // Enable Docking
    // io.ConfigFlags |= ImGuiConfigFlags_ViewportsEnable;         // Enable Multi-Viewport

    // Setup Dear ImGui style
    ImGui::StyleColorsDark();

    // Customize ImGui style for a more modern look
    ImGuiStyle &style = ImGui::GetStyle();
    style.WindowRounding = 5.0f;
    style.FrameRounding = 4.0f;
    style.PopupRounding = 4.0f;
    style.ScrollbarRounding = 4.0f;
    style.GrabRounding = 4.0f;

    // Use a more appealing blue color scheme
    ImVec4 *colors = style.Colors;
    colors[ImGuiCol_WindowBg] = ImVec4(0.08f, 0.08f, 0.15f, 1.00f);
    colors[ImGuiCol_TitleBg] = ImVec4(0.10f, 0.18f, 0.26f, 1.00f);
    colors[ImGuiCol_TitleBgActive] = ImVec4(0.15f, 0.26f, 0.38f, 1.00f);
    colors[ImGuiCol_MenuBarBg] = ImVec4(0.12f, 0.20f, 0.28f, 1.00f);
    colors[ImGuiCol_Tab] = ImVec4(0.12f, 0.20f, 0.28f, 0.86f);
    colors[ImGuiCol_TabHovered] = ImVec4(0.20f, 0.32f, 0.44f, 1.00f);
    colors[ImGuiCol_TabActive] = ImVec4(0.24f, 0.40f, 0.55f, 1.00f);
    colors[ImGuiCol_ScrollbarBg] = ImVec4(0.08f, 0.08f, 0.15f, 0.60f);

    // Setup Platform/Renderer backends
    if (!ImGui_ImplWin32_Init(*hWnd))
    {
        fprintf(stderr, "Error: ImGui_ImplWin32_Init failed.\n");
        // Cleanup...
        return FALSE;
    }
    if (!ImGui_ImplDX11_Init(g_pd3dDevice, g_pd3dDeviceContext))
    {
        fprintf(stderr, "Error: ImGui_ImplDX11_Init failed.\n");
        // Cleanup...
        return FALSE;
    }

    // Create initial tab only if g_tabs is currently empty
    if (g_tabs.empty())
    {
        std::string tabName = "Shell " + std::to_string(g_nextTabId++);
        g_tabs.emplace_back(tabName);
        int initialTabIndex = 0; // Index of the newly added tab

        std::string hsh_path = get_hsh_executable_path();
        if (!hsh_path.empty())
        {
            // Attempt to create the terminal tab process
            g_tabs[initialTabIndex].term_tab = terminal_tab_create(tabName.c_str(), hsh_path.c_str(), NULL, NULL);
            if (g_tabs[initialTabIndex].term_tab)
            {
                // Success: Set this tab as active
                g_activeTab = initialTabIndex;
                g_tabs[initialTabIndex].isActive = true;
                printf("Initial terminal tab created successfully.\n");
            }
            else
            {
                // Process creation failed
                fprintf(stderr, "Error: Failed to create initial terminal tab process!\n");
                g_tabs.pop_back(); // Remove the tab we failed to initialize
                g_activeTab = -1;  // No active tab
            }
        }
        else
        {
            // hsh executable path not found
            fprintf(stderr, "Error: Could not find hsh.exe path for initial tab!\n");
            g_tabs.pop_back(); // Remove the tab
            g_activeTab = -1;  // No active tab
        }
    }
    else
    {
        // If tabs already exist (e.g., context recreated), ensure g_activeTab is valid
        if (g_activeTab < 0 || g_activeTab >= (int)g_tabs.size())
        {
            g_activeTab = g_tabs.empty() ? -1 : 0;
        }
        // Make sure the supposedly active tab is marked as active
        if (g_activeTab != -1)
        {
            for (size_t i = 0; i < g_tabs.size(); ++i)
            {
                g_tabs[i].isActive = (i == (size_t)g_activeTab);
            }
        }
    }

    return TRUE;
}

/**
 * imgui_shutdown - Clean up ImGui and close the window
 */
void imgui_shutdown(void)
{
    // Cleanup ImGui backends first
    ImGui_ImplDX11_Shutdown();
    ImGui_ImplWin32_Shutdown();
    ImGui::DestroyContext();

    // Cleanup Direct3D resources
    CleanupDeviceD3D();
    // Note: Window destruction and UnregisterClass is handled in the main loop caller (imgui_main)
}

/**
 * RenderShell - Render the shell interface with ImGui
 */
void RenderShell()
{
    // Main window styling
    ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding, ImVec2(0, 0)); // No padding for main window
    ImGui::PushStyleVar(ImGuiStyleVar_WindowRounding, 0.0f);
    ImGui::PushStyleVar(ImGuiStyleVar_WindowBorderSize, 0.0f);

    // Set window size to fill the viewport
    ImGuiIO &io = ImGui::GetIO();
    ImGui::SetNextWindowPos(ImVec2(0, 0));
    ImGui::SetNextWindowSize(io.DisplaySize);

    // Create the main window without standard window decorations
    ImGui::Begin("ArbSh Terminal Host", NULL, // Changed name to avoid conflict if we nest windows
                 ImGuiWindowFlags_NoCollapse |
                     ImGuiWindowFlags_MenuBar |
                     ImGuiWindowFlags_NoResize |
                     ImGuiWindowFlags_NoMove |
                     ImGuiWindowFlags_NoTitleBar |
                     ImGuiWindowFlags_NoScrollbar |
                     ImGuiWindowFlags_NoBringToFrontOnFocus); // Prevent window stealing focus

    // Add some general content padding *after* Begin, affects content area
    // ImGui::PushStyleVar(ImGuiStyleVar_ItemSpacing, ImVec2(10, 10)); // Maybe remove this if causing layout issues

    // Style the menu bar
    ImGui::PushStyleColor(ImGuiCol_MenuBarBg, ImVec4(0.12f, 0.15f, 0.25f, 1.00f));

    // Menu bar
    if (ImGui::BeginMenuBar())
    {
        // Add terminal title
        ImGui::Text("ArbSh Terminal");
        // ImGui::SameLine(ImGui::GetWindowWidth() - 100); // Right-align (might need adjustment)

        if (ImGui::BeginMenu("File"))
        {
            if (ImGui::MenuItem("New Tab", "Ctrl+T")) // Added shortcut hint
            {
                std::string tabName = "Shell " + std::to_string(g_nextTabId++);
                g_tabs.emplace_back(tabName); // Use emplace_back for direct construction
                int newTabIndex = g_tabs.size() - 1;

                std::string hsh_path = get_hsh_executable_path(); // Get path to hsh.exe
                if (hsh_path.empty())
                {
                    fprintf(stderr, "Error: Could not find hsh.exe path for new tab!\n");
                    g_tabs.pop_back(); // Remove the tab we just added
                }
                else
                {
                    g_tabs[newTabIndex].term_tab = terminal_tab_create(tabName.c_str(), hsh_path.c_str(), NULL, NULL);
                    if (!g_tabs[newTabIndex].term_tab)
                    {
                        fprintf(stderr, "Error: Failed to create terminal tab process for %s!\n", hsh_path.c_str());
                        g_tabs.pop_back(); // Remove the tab
                    }
                    else
                    {
                        // Select the new tab
                        g_activeTab = newTabIndex;
                        for (size_t i = 0; i < g_tabs.size(); ++i) // Update all tabs' active state
                        {
                            g_tabs[i].isActive = (i == (size_t)g_activeTab);
                        }
                        printf("Created new terminal tab: %s\n", tabName.c_str());
                    }
                }
            }
            ImGui::Separator();
            if (ImGui::MenuItem("Exit", "Alt+F4")) // Added shortcut hint
            {
                g_shouldExit = true;
            }
            ImGui::EndMenu();
        }
        if (ImGui::BeginMenu("Help"))
        {
            if (ImGui::MenuItem("About"))
            {
                // Append text to the *active* terminal's buffer
                std::string aboutText =
                    "\r\n---------------------------------------\r\n"
                    "ArbSh Terminal with ImGui\r\n"
                    "Version 1.1\r\n"
                    "Modern GPU-accelerated UI with Arabic support\r\n"
                    "---------------------------------------\r\n";

                if (g_activeTab >= 0 && g_activeTab < (int)g_tabs.size())
                {
                    terminal_tab_t *current_term = g_tabs[g_activeTab].term_tab;
                    if (current_term)
                    {
                        terminal_tab_append_buffer(current_term, aboutText.c_str(), (int)aboutText.length());
                        current_term->scroll_to_bottom = true;
                    }
                }
            }
            ImGui::EndMenu();
        }
        ImGui::EndMenuBar();
    }

    ImGui::PopStyleColor(); // MenuBarBg

    // Add spacing after menu bar if needed
    // ImGui::Spacing(); // Maybe remove if using WindowPadding(0,0)

    // Style tabs
    ImGui::PushStyleColor(ImGuiCol_Tab, ImVec4(0.12f, 0.15f, 0.25f, 0.8f));
    ImGui::PushStyleColor(ImGuiCol_TabHovered, ImVec4(0.18f, 0.22f, 0.36f, 1.0f));
    ImGui::PushStyleColor(ImGuiCol_TabActive, ImVec4(0.24f, 0.30f, 0.46f, 1.0f));
    ImGui::PushStyleVar(ImGuiStyleVar_FramePadding, ImVec2(12, 8));
    ImGui::PushStyleVar(ImGuiStyleVar_TabRounding, 6.0f);

    // Tab bar
    if (ImGui::BeginTabBar("ShellTabs", ImGuiTabBarFlags_AutoSelectNewTabs | ImGuiTabBarFlags_Reorderable | ImGuiTabBarFlags_FittingPolicyResizeDown))
    {
        // Use a copy for safe iteration while erasing
        // std::vector<size_t> tabs_to_close; // Alternative: Collect indices to close

        for (size_t i = 0; i < g_tabs.size(); /* Increment managed inside loop */)
        {
            bool open = true; // Control variable for the tab item close button
            // Ensure isActive reflects the actual selected tab
            g_tabs[i].isActive = (i == (size_t)g_activeTab);
            ImGuiTabItemFlags flags = g_tabs[i].isActive ? ImGuiTabItemFlags_SetSelected : 0;

            if (ImGui::BeginTabItem(g_tabs[i].name.c_str(), &open, flags))
            {
                // If this tab was just selected, update g_activeTab
                if (!g_tabs[i].isActive)
                {
                    g_activeTab = i;
                    // Update all other tabs to inactive (redundant due to check above, but safe)
                    for (size_t j = 0; j < g_tabs.size(); ++j)
                    {
                        g_tabs[j].isActive = (j == i);
                    }
                }

                terminal_tab_t *current_term = g_tabs[i].term_tab;

                // Calculate available height for the terminal view
                const float footer_height_to_reserve = ImGui::GetStyle().ItemSpacing.y + ImGui::GetFrameHeightWithSpacing() + 5; // Add a little extra space

                // Terminal Output Area (Child Window)
                ImGui::PushStyleColor(ImGuiCol_ChildBg, ImVec4(0.05f, 0.05f, 0.10f, 1.0f)); // Darker background
                ImGui::PushStyleVar(ImGuiStyleVar_ChildRounding, 0.0f);                     // No rounding for terminal area
                ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding, ImVec2(4, 4));             // Padding inside terminal view

                ImGui::BeginChild("ScrollingRegion", ImVec2(0, -footer_height_to_reserve), false, ImGuiWindowFlags_HorizontalScrollbar | ImGuiWindowFlags_NoMove); // No border, Allow scrollbars

                // Use a monospace font if available/loaded
                // ImGui::PushFont(monospace_font); // Assuming you loaded one

                ImGui::PushStyleVar(ImGuiStyleVar_ItemSpacing, ImVec2(0, 0)); // Tight spacing for terminal text

                if (current_term)
                {
                    const char *term_buffer = terminal_tab_get_buffer(current_term);
                    if (term_buffer)
                    {
                        // Display the raw buffer content - THIS IS WHERE TERMINAL EMULATION WILL GO
                        // For now, just raw text
                        ImGui::TextUnformatted(term_buffer, term_buffer + current_term->buffer_used);

                        // Auto-scroll logic
                        if (current_term->scroll_to_bottom)
                        {
                            ImGui::SetScrollHereY(1.0f);
                            current_term->scroll_to_bottom = false; // Reset flag after scrolling
                        }
                    }
                    else
                    {
                        ImGui::Text("Error: Terminal buffer is NULL.");
                    }
                }
                else
                {
                    ImGui::Text("Terminal not initialized for this tab.");
                }

                ImGui::PopStyleVar(); // ItemSpacing
                                      // ImGui::PopFont(); // Pop monospace font

                ImGui::EndChild();

                ImGui::PopStyleVar(2);  // WindowPadding, ChildRounding
                ImGui::PopStyleColor(); // ChildBg

                // Command Input Area
                ImGui::Separator();
                bool reclaim_focus = false;

                ImGui::PushItemWidth(-1); // Make input take full width

                // Styling for the input area
                ImGui::PushStyleColor(ImGuiCol_FrameBg, ImVec4(0.10f, 0.10f, 0.20f, 1.0f));
                ImGui::PushStyleColor(ImGuiCol_Text, ImVec4(1.00f, 1.00f, 1.00f, 1.0f)); // Brighter text
                ImGui::PushStyleVar(ImGuiStyleVar_FramePadding, ImVec2(10, 8));          // More padding
                ImGui::PushStyleVar(ImGuiStyleVar_FrameRounding, 4.0f);

                // ImGui::Text(">"); ImGui::SameLine(); // Simple prompt indicator
                std::string input_label = "##CommandInput_" + std::to_string(i); // Unique label per tab

                if (ImGui::InputText(input_label.c_str(), g_tabs[i].inputBuffer, IM_ARRAYSIZE(g_tabs[i].inputBuffer),
                                     ImGuiInputTextFlags_EnterReturnsTrue | ImGuiInputTextFlags_CallbackHistory | ImGuiInputTextFlags_CallbackCharFilter,
                                     NULL, NULL)) // Add callbacks later for history/completion
                {
                    if (current_term && current_term->is_active)
                    {
                        std::string command = g_tabs[i].inputBuffer;
                        if (!command.empty())
                        {
                            terminal_tab_send_command(current_term, command.c_str());
                            g_tabs[i].inputBuffer[0] = '\0'; // Clear the ImGui input buffer
                            reclaim_focus = true;            // Refocus after Enter
                        }
                    }
                    else
                    {
                        // Process ended or terminal not active
                        if (current_term)
                        { // If term exists but not active
                          // Avoid repeatedly appending message
                          // We handle this better in the main loop now
                        }
                        g_tabs[i].inputBuffer[0] = '\0'; // Clear input anyway
                    }
                }

                // Auto-focus on the input box when tab becomes active
                if (g_tabs[i].isActive && !ImGui::IsItemActive() && !ImGui::IsAnyItemActive())
                {
                    if (ImGui::IsWindowFocused(ImGuiFocusedFlags_RootAndChildWindows))
                    {
                        ImGui::SetKeyboardFocusHere(-1); // Focus input text
                    }
                }
                if (reclaim_focus)
                {
                    ImGui::SetKeyboardFocusHere(-1); // Refocus after pressing Enter
                }

                ImGui::PopStyleVar(2);   // FramePadding, FrameRounding
                ImGui::PopStyleColor(2); // FrameBg, Text
                ImGui::PopItemWidth();

                ImGui::EndTabItem();
            }

            // Handle tab closing *after* EndTabItem
            if (!open)
            {
                // The TabData destructor handles terminal_tab_free
                g_tabs.erase(g_tabs.begin() + i);

                // Adjust active tab index if necessary
                if (g_tabs.empty())
                {
                    g_activeTab = -1; // No tabs left
                }
                else
                {
                    if (g_activeTab >= (int)i)
                    {                                               // If closed tab was active or before active
                        g_activeTab = std::max(0, g_activeTab - 1); // Select previous or first
                    }
                    // No need to explicitly set isActive here, the next loop iteration handles it
                }
                // Don't increment i, loop continues with the next element at the current index
            }
            else
            {
                i++; // Move to the next tab only if the current one wasn't closed
            }
        }
        ImGui::EndTabBar();

        // Clean up tab styling
        ImGui::PopStyleVar(2);   // FramePadding, TabRounding
        ImGui::PopStyleColor(3); // Tab, TabHovered, TabActive
    }
    else
    {
        // If BeginTabBar failed, we still need to clean up the styles
        ImGui::PopStyleVar(2);   // FramePadding, TabRounding
        ImGui::PopStyleColor(3); // Tab, TabHovered, TabActive
        // Also display a message if no tabs exist?
        if (g_tabs.empty())
        {
            ImGui::Text("No active shells. Use File -> New Tab to start.");
        }
    }

    // ImGui::PopStyleVar(); // Pop ItemSpacing (If uncommented earlier)

    ImGui::End(); // End of "ArbSh Terminal Host" window

    ImGui::PopStyleVar(3); // WindowPadding, WindowRounding, WindowBorderSize
}

/**
 * imgui_main_loop - Run the main loop for ImGui (Rendering and Frame Logic)
 * @hWnd: Window handle (unused in this function body)
 *
 * Return: TRUE to continue the loop, FALSE to exit
 */
BOOL imgui_main_loop([[maybe_unused]] HWND hWnd) // Mark hWnd as potentially unused
{
    // Start the Dear ImGui frame
    ImGui_ImplDX11_NewFrame();
    ImGui_ImplWin32_NewFrame();
    ImGui::NewFrame();

    // Render the shell interface
    RenderShell(); // This function now contains all the UI rendering logic

    // Rendering ImGui Draw Data
    ImGui::Render();
    const float clear_color_with_alpha[4] = {0.03f, 0.04f, 0.08f, 1.00f}; // Dark blue background
    g_pd3dDeviceContext->OMSetRenderTargets(1, &g_mainRenderTargetView, NULL);
    g_pd3dDeviceContext->ClearRenderTargetView(g_mainRenderTargetView, clear_color_with_alpha);
    ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());

    // Present the frame
    // TODO: Add VSync handling? (Present interval)
    g_pSwapChain->Present(1, 0); // Present with vsync interval 1

    // Check if an exit was requested (e.g., by the menu)
    return !g_shouldExit;
}

#endif /* WINDOWS */