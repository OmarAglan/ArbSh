/**
 * imgui_shell.cpp - ImGui integration with the shell
 *
 * This file provides the implementation for using ImGui as the GUI framework
 * for the ArbSh terminal. It is the sole GUI implementation, replacing the
 * old Win32 GUI code.
 */

#include "imgui_shell.h"
#include "shell.h"

#ifdef WINDOWS
#include <windows.h>
#include <string>
#include <vector>
#include <sstream>
#include <stdio.h>
#include <filesystem>
#include "terminal_tab.h"

// Define UNUSED macro for parameter suppression
#define UNUSED(x) (void)(x)

// ImGui includes
#include "imgui.h"
#include "imgui_impl_win32.h"
#include "imgui_impl_dx11.h"
#include <d3d11.h>
#include <tchar.h>

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
// These are now implemented in imgui_main.cpp
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
static int g_activeTab = 0;
static bool g_shouldExit = false;
static int g_nextTabId = 1;

// Example helper function (Windows specific)
std::string get_hsh_executable_path()
{
    wchar_t path[MAX_PATH];
    GetModuleFileNameW(NULL, path, MAX_PATH);
    std::filesystem::path guiPath = path; // Requires C++17
    std::filesystem::path hshPath = guiPath.parent_path() / "hsh.exe";
    // Convert wide string path to narrow string for create_shell_process
    std::wstring ws(hshPath.c_str());
    std::string narrow_path(ws.begin(), ws.end());
    return narrow_path;
    // Simpler, less robust: return "../bin/hsh.exe"; // Assumes build dir structure
}

/**
 * imgui_update_console_text - Update the console text displayed in ImGui
 * @text: Text to add to the console
 */
void imgui_update_console_text(const char *text)
{
    if (!text || g_tabs.empty() || g_activeTab < 0 || g_activeTab >= (int)g_tabs.size())
        return;

    g_tabs[g_activeTab].console.history.push_back(text);
    g_tabs[g_activeTab].console.scrollToBottom = true;

    // Trim history if it gets too long
    const size_t MAX_HISTORY = 1000;
    if (g_tabs[g_activeTab].console.history.size() > MAX_HISTORY)
    {
        g_tabs[g_activeTab].console.history.erase(
            g_tabs[g_activeTab].console.history.begin(),
            g_tabs[g_activeTab].console.history.begin() + (g_tabs[g_activeTab].console.history.size() - MAX_HISTORY));
    }
}

/**
 * imgui_main - Main entry point for ImGui-based GUI
 * @hInstance: Instance handle
 * @hPrevInstance: Previous instance handle (unused)
 * @lpCmdLine: Command line arguments
 * @nCmdShow: Show command
 *
 * Return: Exit code
 */
int imgui_main(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
    UNUSED(hPrevInstance); // Parameter is unused
    UNUSED(lpCmdLine);     // Parameter is unused
    UNUSED(nCmdShow);      // Parameter is unused

    // Create main window
    HWND hWnd = NULL;
    if (!imgui_init(hInstance, &hWnd, 1280, 720, "ArbSh Terminal with ImGui"))
        return 1;

    // Add welcome message
    std::string welcomeMsg =
        "\n"
        "╔══════════════════════════════════════════════════════════╗\n"
        "║                                                          ║\n"
        "║                  ArbSh Terminal                          ║\n"
        "║         MODERN SHELL WITH ARABIC SUPPORT                 ║\n"
        "║                                                          ║\n"
        "╚══════════════════════════════════════════════════════════╝\n\n"
        "Type help for available commands.\n\n"
        "=> مرحبًا بكم في ArbSh - واجهة مستخدم حديثة\n\n";
    imgui_update_console_text(welcomeMsg.c_str());

    // Main loop
    MSG msg;
    ZeroMemory(&msg, sizeof(msg));
    while (msg.message != WM_QUIT)
    {
        // Poll and handle messages
        if (PeekMessage(&msg, NULL, 0U, 0U, PM_REMOVE))
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
            continue;
        }

        // Run the ImGui main loop
        if (!imgui_main_loop(hWnd))
            break;
    }

    // Cleanup
    g_tabs.clear(); // This will call the destructor for each TabData element
    imgui_shutdown();
    DestroyWindow(hWnd);
    UnregisterClassW(L"ImGuiShellWindow", GetModuleHandle(NULL));

    return 0;
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
    sd.BufferDesc.Width = 0;
    sd.BufferDesc.Height = 0;
    sd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    sd.BufferDesc.RefreshRate.Numerator = 60;
    sd.BufferDesc.RefreshRate.Denominator = 1;
    sd.Flags = DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH;
    sd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    sd.OutputWindow = hWnd;
    sd.SampleDesc.Count = 1;
    sd.SampleDesc.Quality = 0;
    sd.Windowed = TRUE;
    sd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;

    UINT createDeviceFlags = 0;
    D3D_FEATURE_LEVEL featureLevel;
    const D3D_FEATURE_LEVEL featureLevelArray[2] = {
        D3D_FEATURE_LEVEL_11_0,
        D3D_FEATURE_LEVEL_10_0,
    };
    if (D3D11CreateDeviceAndSwapChain(NULL, D3D_DRIVER_TYPE_HARDWARE, NULL, createDeviceFlags, featureLevelArray, 2, D3D11_SDK_VERSION, &sd, &g_pSwapChain, &g_pd3dDevice, &featureLevel, &g_pd3dDeviceContext) != S_OK)
        return FALSE;

    CreateRenderTarget();
    return TRUE;
}

/**
 * CleanupDeviceD3D - Clean up Direct3D resources
 */
void CleanupDeviceD3D()
{
    CleanupRenderTarget();
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
    UNUSED(hInstance); // Parameter is unused
    UNUSED(title);     // Parameter is unused

    // Create application window
    WNDCLASSEXW wc = {sizeof(wc), CS_CLASSDC, ImGuiWndProc, 0L, 0L, GetModuleHandle(NULL), NULL, NULL, NULL, NULL, L"ImGuiShellWindow", NULL};
    RegisterClassExW(&wc);

    // Get screen dimensions for better default positioning
    int screenWidth = GetSystemMetrics(SM_CXSCREEN);
    int screenHeight = GetSystemMetrics(SM_CYSCREEN);
    int windowX = (screenWidth - width) / 2;
    int windowY = (screenHeight - height) / 2;

    *hWnd = CreateWindowW(wc.lpszClassName, L"ArbSh Terminal", WS_OVERLAPPEDWINDOW,
                          windowX, windowY, width, height,
                          NULL, NULL, wc.hInstance, NULL);

    if (!*hWnd)
        return FALSE;

    // Initialize Direct3D
    if (!CreateDeviceD3D(*hWnd))
    {
        CleanupDeviceD3D();
        UnregisterClassW(wc.lpszClassName, wc.hInstance);
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
    ImGui_ImplWin32_Init(*hWnd);
    ImGui_ImplDX11_Init(g_pd3dDevice, g_pd3dDeviceContext);

    // Create initial tab
    if (g_tabs.empty())
    {
        std::string tabName = "Shell " + std::to_string(g_nextTabId++);
        g_tabs.emplace_back(tabName);
        int initialTabIndex = 0;

        std::string hsh_path = get_hsh_executable_path();
        if (!hsh_path.empty())
        {
            g_tabs[initialTabIndex].term_tab = terminal_tab_create(tabName.c_str(), hsh_path.c_str(), NULL, NULL);
            if (g_tabs[initialTabIndex].term_tab)
            {
                g_activeTab = initialTabIndex;
                g_tabs[initialTabIndex].isActive = true;
            }
            else
            {
                fprintf(stderr, "Error: Failed to create initial terminal tab process!\n");
                g_tabs.pop_back(); // Remove the tab if creation failed
            }
        }
        else
        {
            fprintf(stderr, "Error: Could not find hsh.exe path for initial tab!\n");
            g_tabs.pop_back();
        }
    }

    return TRUE;
}

/**
 * imgui_shutdown - Clean up ImGui and close the window
 */
void imgui_shutdown(void)
{
    // Cleanup
    ImGui_ImplDX11_Shutdown();
    ImGui_ImplWin32_Shutdown();
    ImGui::DestroyContext();

    CleanupDeviceD3D();
}

/**
 * RenderShell - Render the shell interface with ImGui
 */
void RenderShell()
{
    // Main window styling
    ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding, ImVec2(10, 10));
    ImGui::PushStyleVar(ImGuiStyleVar_WindowRounding, 0.0f);

    // Set window size to fill the viewport
    ImGuiIO &io = ImGui::GetIO();
    ImGui::SetNextWindowPos(ImVec2(0, 0));
    ImGui::SetNextWindowSize(io.DisplaySize);

    // Create the main window without standard window decorations
    ImGui::Begin("ArbSh Terminal", NULL,
                 ImGuiWindowFlags_NoCollapse |
                     ImGuiWindowFlags_MenuBar |
                     ImGuiWindowFlags_NoResize |
                     ImGuiWindowFlags_NoMove |
                     ImGuiWindowFlags_NoTitleBar |
                     ImGuiWindowFlags_NoScrollbar);

    // Add some general content padding
    ImGui::PushStyleVar(ImGuiStyleVar_ItemSpacing, ImVec2(10, 10));

    // Style the menu bar
    ImGui::PushStyleColor(ImGuiCol_MenuBarBg, ImVec4(0.12f, 0.15f, 0.25f, 1.00f));

    // Menu bar
    if (ImGui::BeginMenuBar())
    {
        // Add terminal title
        ImGui::Text("ArbSh Terminal");
        ImGui::SameLine(ImGui::GetWindowWidth() - 100); // Right-align

        if (ImGui::BeginMenu("File"))
        {
            if (ImGui::MenuItem("New Tab"))
            {
                std::string tabName = "Shell " + std::to_string(g_nextTabId++);
                g_tabs.emplace_back(tabName); // Use emplace_back for direct construction
                int newTabIndex = g_tabs.size() - 1;

                // --- NEW CODE ---
                std::string hsh_path = get_hsh_executable_path(); // Get path to hsh.exe
                if (hsh_path.empty())
                {
                    // Handle error - maybe show a message box
                    fprintf(stderr, "Error: Could not find hsh.exe path!\n");
                    g_tabs.pop_back(); // Remove the tab we just added
                }
                else
                {
                    g_tabs[newTabIndex].term_tab = terminal_tab_create(tabName.c_str(), hsh_path.c_str(), NULL, NULL);
                    if (!g_tabs[newTabIndex].term_tab)
                    {
                        // Handle error - process creation failed
                        fprintf(stderr, "Error: Failed to create terminal tab process for %s!\n", hsh_path.c_str());
                        g_tabs.pop_back(); // Remove the tab
                    }
                    else
                    {
                        // Select the new tab
                        g_activeTab = newTabIndex;
                        for (size_t i = 0; i < g_tabs.size(); ++i)
                        {
                            g_tabs[i].isActive = (i == (size_t)g_activeTab);
                        }
                        printf("Created new terminal tab: %s\n", tabName.c_str());
                    }
                }
            }
            ImGui::Separator();
            if (ImGui::MenuItem("Exit"))
            {
                g_shouldExit = true;
            }
            ImGui::EndMenu();
        }
        if (ImGui::BeginMenu("Help"))
        {
            if (ImGui::MenuItem("About"))
            {
                // Show about dialog
                // For now, we'll just add text to the console
                std::string aboutText =
                    "\n---------------------------------------\n"
                    "ArbSh Terminal with ImGui\n"
                    "Version 1.1\n"
                    "Modern GPU-accelerated UI with Arabic support\n"
                    "---------------------------------------\n";

                if (g_activeTab >= 0 && g_activeTab < g_tabs.size())
                {
                    terminal_tab_t *current_term = g_tabs[g_activeTab].term_tab;
                    if (current_term)
                    {
                        terminal_tab_append_buffer(current_term, aboutText.c_str(), aboutText.length());
                        // Trigger scroll to bottom in the terminal tab
                        current_term->scroll_to_bottom = true;
                    }
                }
            }
            ImGui::EndMenu();
        }
        ImGui::EndMenuBar();
    }

    ImGui::PopStyleColor(); // MenuBarBg

    // Add spacing after menu bar
    ImGui::Spacing();

    // Style tabs
    ImGui::PushStyleColor(ImGuiCol_Tab, ImVec4(0.12f, 0.15f, 0.25f, 0.8f));
    ImGui::PushStyleColor(ImGuiCol_TabHovered, ImVec4(0.18f, 0.22f, 0.36f, 1.0f));
    ImGui::PushStyleColor(ImGuiCol_TabActive, ImVec4(0.24f, 0.30f, 0.46f, 1.0f));
    ImGui::PushStyleVar(ImGuiStyleVar_FramePadding, ImVec2(12, 8));
    ImGui::PushStyleVar(ImGuiStyleVar_TabRounding, 6.0f);

    // Tab bar
    if (ImGui::BeginTabBar("ShellTabs", ImGuiTabBarFlags_AutoSelectNewTabs | ImGuiTabBarFlags_FittingPolicyResizeDown))
    {
        for (size_t i = 0; i < g_tabs.size();)
        {
            bool open = true;
            ImGuiTabItemFlags flags = 0;
            if (g_tabs[i].isActive)
                flags |= ImGuiTabItemFlags_SetSelected;

            if (ImGui::BeginTabItem(g_tabs[i].name.c_str(), &open, flags))
            {
                g_activeTab = i;
                g_tabs[i].isActive = true;

                // Console window
                const float footer_height_to_reserve = ImGui::GetStyle().ItemSpacing.y + ImGui::GetFrameHeightWithSpacing();

                // Add a darker background for the console text area
                ImGui::PushStyleColor(ImGuiCol_ChildBg, ImVec4(0.05f, 0.05f, 0.10f, 1.0f));
                ImGui::PushStyleVar(ImGuiStyleVar_ChildRounding, 6.0f);
                ImGui::BeginChild("ScrollingRegion", ImVec2(0, -footer_height_to_reserve), true, ImGuiWindowFlags_HorizontalScrollbar);

                // Slight padding for text and better text color
                ImGui::PushStyleVar(ImGuiStyleVar_ItemSpacing, ImVec2(8, 4));
                ImGui::PushStyleColor(ImGuiCol_Text, ImVec4(0.90f, 0.90f, 1.00f, 1.00f));

                // Display console history with proper wrapping
                ImGui::PushTextWrapPos(ImGui::GetWindowWidth() - 20);

                terminal_tab_t *current_term = g_tabs[i].term_tab;
                if (current_term)
                {
                    const char *term_buffer = terminal_tab_get_buffer(current_term);
                    // Ensure buffer is not null before displaying
                    if (term_buffer)
                    {
                        ImGui::PushTextWrapPos(ImGui::GetWindowWidth() - 20);
                        // Display the raw buffer content
                        ImGui::TextUnformatted(term_buffer, term_buffer + current_term->buffer_used);
                        ImGui::PopTextWrapPos();

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

                // Auto-scroll to the bottom
                if (g_tabs[i].console.scrollToBottom)
                {
                    ImGui::SetScrollHereY(1.0f);
                    g_tabs[i].console.scrollToBottom = false;
                }

                ImGui::PopStyleColor(); // Text
                ImGui::PopStyleVar();   // ItemSpacing
                ImGui::EndChild();
                ImGui::PopStyleVar();   // ChildRounding
                ImGui::PopStyleColor(); // ChildBg

                // Command input
                ImGui::Separator();
                bool reclaim_focus = false;

                // Styling for the input area
                ImGui::PushStyleColor(ImGuiCol_FrameBg, ImVec4(0.10f, 0.10f, 0.20f, 1.0f));
                ImGui::PushStyleColor(ImGuiCol_Text, ImVec4(1.00f, 1.00f, 1.00f, 1.0f)); // Brighter text
                ImGui::PushStyleVar(ImGuiStyleVar_FramePadding, ImVec2(10, 8));          // More padding
                ImGui::PushStyleVar(ImGuiStyleVar_FrameRounding, 4.0f);

                // Add a little space before the input
                ImGui::Spacing();

                // Use the default font for the label (safeguard for PushFont)
                ImGui::Text("Command:");
                ImGui::SameLine();

                // Use the tab's specific input buffer
                if (ImGui::InputText("##CommandInput", g_tabs[i].inputBuffer, IM_ARRAYSIZE(g_tabs[i].inputBuffer),
                                     ImGuiInputTextFlags_EnterReturnsTrue, NULL, NULL))
                {
                    terminal_tab_t *current_term = g_tabs[i].term_tab;
                    if (current_term && current_term->is_active)
                    { // Check if process is active
                        std::string command = g_tabs[i].inputBuffer;
                        if (!command.empty())
                        {
                            // --- NEW CODE ---
                            // Send the command (with newline) to the process
                            terminal_tab_send_command(current_term, command.c_str());

                            // Clear the ImGui input buffer
                            g_tabs[i].inputBuffer[0] = '\0';
                            // --- END NEW CODE ---

                            // Keep focus on input
                            reclaim_focus = true;
                        }
                    }
                    else
                    {
                        // Optionally notify user that the terminal is not active
                        // Maybe add text like "[Process Ended]" to the display buffer?
                        if (current_term)
                        { // If term exists but not active
                            terminal_tab_append_buffer(current_term, "\r\n[Process has ended]\r\n", strlen("\r\n[Process has ended]\r\n"));
                        }
                        g_tabs[i].inputBuffer[0] = '\0'; // Clear input anyway
                    }
                }

                ImGui::PopStyleVar(2);   // FramePadding, FrameRounding
                ImGui::PopStyleColor(2); // FrameBg, Text

                // Auto-focus on the input box
                ImGui::SetItemDefaultFocus();
                if (reclaim_focus)
                    ImGui::SetKeyboardFocusHere(-1);

                ImGui::EndTabItem();
            }

            // Handle tab closing
            if (!open)
            {
                // terminal_tab_free takes care of closing/terminating the process
                // The TabData destructor will call terminal_tab_free

                // Close the tab
                g_tabs.erase(g_tabs.begin() + i);
                if (g_tabs.empty())
                {
                    // If no tabs left, create a new one
                    g_tabs.push_back(TabData("Shell 1"));
                    g_activeTab = 0;
                    g_tabs[0].isActive = true;
                }
                else
                {
                    // Select an appropriate tab
                    if (g_activeTab >= (int)g_tabs.size())
                        g_activeTab = g_tabs.size() - 1;
                    g_tabs[g_activeTab].isActive = true;
                }
                continue; // Don't increment i since we removed an element
            }

            // Mark all other tabs as inactive
            if (g_activeTab != (int)i)
            {
                g_tabs[i].isActive = false;
            }

            i++; // Move to the next tab
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
    }

    // Pop the remaining style vars before ending the window
    ImGui::PopStyleVar(); // ItemSpacing

    ImGui::End();

    // Pop window styling
    ImGui::PopStyleVar(2); // WindowPadding, WindowRounding
}

/**
 * imgui_main_loop - Run the main loop for ImGui
 * @hWnd: Window handle
 *
 * Return: TRUE to continue the loop, FALSE to exit
 */
BOOL imgui_main_loop(HWND hWnd)
{
    UNUSED(hWnd); // Parameter is unused

    // Start the Dear ImGui frame
    ImGui_ImplDX11_NewFrame();
    ImGui_ImplWin32_NewFrame();
    ImGui::NewFrame();

    // Render the shell interface
    RenderShell();

    // Rendering
    ImGui::Render();
    // Use a darker blue-tinted background
    const float clear_color_with_alpha[4] = {0.03f, 0.04f, 0.08f, 1.00f};
    g_pd3dDeviceContext->OMSetRenderTargets(1, &g_mainRenderTargetView, NULL);
    g_pd3dDeviceContext->ClearRenderTargetView(g_mainRenderTargetView, clear_color_with_alpha);
    ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());

    // Present
    g_pSwapChain->Present(1, 0);

    // Process messages for the active tab
    if (g_activeTab >= 0 && g_activeTab < g_tabs.size())
    {
        terminal_tab_t *active_term = g_tabs[g_activeTab].term_tab;
        if (active_term && active_term->is_active)
        {
            // Process non-blockingly
            terminal_tab_process(active_term);
        }
        else if (active_term && !active_term->is_active)
        {
            // Optionally: Handle tab showing process exited message
            // Maybe disable input, change title?
        }

        return !g_shouldExit;
    }
    return TRUE;
}

#endif /* WINDOWS */