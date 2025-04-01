/**
 * terminal_tab.h - Terminal tab component with process management
 *
 * This file defines the interface for the terminal tab component that uses
 * the process manager to create and communicate with shell processes.
 */

#ifndef _TERMINAL_TAB_H_
#define _TERMINAL_TAB_H_

// Include dependencies FIRST
#include "../platform/process_manager.h" // Assumes relative path from src/gui/
#include <stdbool.h>

// Add extern "C" guards for C++ compatibility
#ifdef __cplusplus
extern "C"
{
#endif

    /**
     * Structure representing a terminal tab
     */
    typedef struct _terminal_tab
    {
        // Tab information
        char *title;
        bool is_active; // Note: is_active relates to UI, process.is_running relates to child process

        // Process information
        shell_process_t process; // Embed the process struct

        // Terminal state & Buffer (Managed by tab)
        int width;
        int height;
        char *buffer; // Display buffer for this tab
        int buffer_size;
        int buffer_used;

        // Scroll state
        int scroll_position;   // Tracks current scroll position
        bool scroll_to_bottom; // Flag for auto-scrolling

        // Input state (Managed by UI, maybe move inputBuffer from TabData here later)
        // char *input_buffer; // Maybe manage input buffer here?
        // int input_buffer_size;
        // int input_buffer_used;
        int cursor_position; // Logical cursor position

        // Selection state
        bool has_selection;
        int selection_start; // Indices within the *buffer*
        int selection_end;

        // Command history (UI might manage this better)
        char **command_history;
        int history_count;
        int history_capacity;
        int history_position; // Current position when navigating history

        // Visual state (Managed by UI)
        bool is_focused; // If the ImGui widget has focus
        bool show_scrollbar;

        // Custom settings (Terminal appearance)
        char *font_name;
        int font_size;
        unsigned int foreground_color; // Example: 0xAARRGGBB
        unsigned int background_color;
        unsigned int selection_color;
        unsigned int cursor_color;
    } terminal_tab_t;

    /**
     * Create a new terminal tab
     *
     * @param title Title of the tab
     * @param command Command to run (NULL for default shell)
     * @param args Array of command arguments (NULL terminated)
     * @param env Array of environment variables (NULL terminated)
     * @return Pointer to the created terminal tab, or NULL on error
     */
    terminal_tab_t *terminal_tab_create(const char *title, const char *command, char *const args[], char *const env[]);

    /**
     * Initialize a terminal tab
     *
     * @param tab Terminal tab to initialize
     * @param title Title of the tab
     * @param command Command to run (NULL for default shell)
     * @param args Array of command arguments (NULL terminated)
     * @param env Array of environment variables (NULL terminated)
     * @return true if the tab was initialized successfully, false otherwise
     */
    bool terminal_tab_init(terminal_tab_t *tab, const char *title, const char *command, char *const args[], char *const env[]);

    /**
     * Process terminal tab events (read output, update state)
     * Should be called periodically (e.g., each frame).
     *
     * @param tab Terminal tab to process
     * @return true if the underlying process is still active, false otherwise (or on error)
     */
    bool terminal_tab_process(terminal_tab_t *tab);

    /**
     * Send input string to the terminal tab's process stdin.
     *
     * @param tab Terminal tab to send input to
     * @param input Input string to send
     * @param size Size of the input string
     * @return true if input was sent successfully, false otherwise
     */
    bool terminal_tab_send_input(terminal_tab_t *tab, const char *input, int size);

    /**
     * Send a command (input string + newline) to the terminal tab.
     *
     * @param tab Terminal tab to send command to
     * @param command Command string to send (newline will be added)
     * @return true if command was sent successfully, false otherwise
     */
    bool terminal_tab_send_command(terminal_tab_t *tab, const char *command);

    /**
     * Resize the logical size of the terminal tab and notify the child process.
     * (Child process notification is currently a placeholder).
     *
     * @param tab Terminal tab to resize
     * @param width New width in characters
     * @param height New height in characters
     * @return true if resize notification was attempted successfully, false otherwise
     */
    bool terminal_tab_resize(terminal_tab_t *tab, int width, int height);

    /**
     * Close the terminal tab and terminate its associated process.
     *
     * @param tab Terminal tab to close
     * @param force If true, forcefully terminate the process (e.g., TerminateProcess)
     * @return true if terminal was closed successfully, false otherwise
     */
    bool terminal_tab_close(terminal_tab_t *tab, bool force);

    /**
     * Get the title of the terminal tab
     *
     * @param tab Terminal tab
     * @return Title of the tab (pointer to internal string, do not free)
     */
    const char *terminal_tab_get_title(terminal_tab_t *tab);

    /**
     * Set the title of the terminal tab
     *
     * @param tab Terminal tab
     * @param title New title for the tab (will be copied)
     * @return true if title was set successfully, false otherwise
     */
    bool terminal_tab_set_title(terminal_tab_t *tab, const char *title);

    /**
     * Get the display buffer content of the terminal tab.
     *
     * @param tab Terminal tab
     * @return Pointer to the internal display buffer (do not free). Returns "" if buffer is NULL.
     */
    const char *terminal_tab_get_buffer(terminal_tab_t *tab);

    /**
     * Clear the display buffer of the terminal tab.
     *
     * @param tab Terminal tab
     * @return true if buffer was cleared successfully, false otherwise
     */
    bool terminal_tab_clear_buffer(terminal_tab_t *tab);

    /**
     * Append data directly to the terminal display buffer (for GUI messages).
     * Use with caution, prefer reading process output via terminal_tab_process.
     *
     * @param tab Terminal tab
     * @param data Data to append
     * @param size Size of the data
     * @return true if successful, false otherwise
     */
    bool terminal_tab_append_buffer(terminal_tab_t *tab, const char *data, int size);

    /**
     * Free resources associated with the terminal tab, including terminating the process.
     *
     * @param tab Terminal tab to free
     */
    void terminal_tab_free(terminal_tab_t *tab);

// End extern "C" guards
#ifdef __cplusplus
}
#endif

#endif /* _TERMINAL_TAB_H_ */