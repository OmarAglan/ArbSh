/**
 * imgui_shell.h - ImGui shell integration
 *
 * This file defines the interface for integrating Dear ImGui with ArbSh
 */

#ifndef _IMGUI_SHELL_H_
#define _IMGUI_SHELL_H_

#ifdef __cplusplus
extern "C" {
#endif

// Forward declarations
struct ImGuiContext;

// Functions that can be called from C code
#ifdef WINDOWS
#include <windows.h>

// Initialize ImGui with a Win32 window
BOOL imgui_init(HINSTANCE hInstance, HWND *hWnd, int width, int height, const char *title);

// Set ImGui as the active GUI mode
void imgui_set_active(int active);

// Update console text from C code
void imgui_update_console_text(const char* text);

// Run the ImGui main loop (should be called from WinMain)
int imgui_main_loop(HWND hWnd);

// Cleanup ImGui resources
void imgui_shutdown();

#endif // WINDOWS

#ifdef __cplusplus
}
#endif

#endif // _IMGUI_SHELL_H_ 