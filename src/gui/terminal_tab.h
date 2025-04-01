/**
 * terminal_tab.h - Terminal tab component with process management
 *
 * This file defines the interface for the terminal tab component that uses
 * the process manager to create and communicate with shell processes.
 */

#ifndef _TERMINAL_TAB_H_
#define _TERMINAL_TAB_H_

#include "../platform/process_manager.h"
#include <stdbool.h>

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
        bool is_active;

        // Process information
        shell_process_t process;

        // Terminal state
        int width;
        int height;
        char *buffer;
        int buffer_size;
        int buffer_used;

        // Scroll state
        int scroll_position;
        bool scroll_to_bottom;

        // Input state
        char *input_buffer;
        int input_buffer_size;
        int input_buffer_used;
        int cursor_position;

        // Selection state
        bool has_selection;
        int selection_start;
        int selection_end;

        // Command history
        char **command_history;
        int history_count;
        int history_capacity;
        int history_position;

        // Visual state
        bool is_focused;
        bool show_scrollbar;

        // Custom settings
        char *font_name;
        int font_size;
        unsigned int foreground_color;
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
     *
     * @param tab Terminal tab to process
     * @return true if the tab is still active, false if it should be closed
     */
    bool terminal_tab_process(terminal_tab_t *tab);

    /**
     * Send input to the terminal tab
     *
     * @param tab Terminal tab to send input to
     * @param input Input to send
     * @param size Size of the input
     * @return true if input was sent successfully, false otherwise
     */
    bool terminal_tab_send_input(terminal_tab_t *tab, const char *input, int size);

    /**
     * Send a command to the terminal tab
     *
     * @param tab Terminal tab to send command to
     * @param command Command to send
     * @return true if command was sent successfully, false otherwise
     */
    bool terminal_tab_send_command(terminal_tab_t *tab, const char *command);

    /**
     * Resize the terminal tab
     *
     * @param tab Terminal tab to resize
     * @param width New width in characters
     * @param height New height in characters
     * @return true if terminal was resized successfully, false otherwise
     */
    bool terminal_tab_resize(terminal_tab_t *tab, int width, int height);

    /**
     * Close the terminal tab
     *
     * @param tab Terminal tab to close
     * @param force If true, forcefully terminate the process
     * @return true if terminal was closed successfully, false otherwise
     */
    bool terminal_tab_close(terminal_tab_t *tab, bool force);

    /**
     * Get the title of the terminal tab
     *
     * @param tab Terminal tab
     * @return Title of the tab
     */
    const char *terminal_tab_get_title(terminal_tab_t *tab);

    /**
     * Set the title of the terminal tab
     *
     * @param tab Terminal tab
     * @param title New title of the tab
     * @return true if title was set successfully, false otherwise
     */
    bool terminal_tab_set_title(terminal_tab_t *tab, const char *title);

    /**
     * Get the buffer of the terminal tab
     *
     * @param tab Terminal tab
     * @return Buffer of the tab
     */
    const char *terminal_tab_get_buffer(terminal_tab_t *tab);

    /**
     * Clear the buffer of the terminal tab
     *
     * @param tab Terminal tab
     * @return true if buffer was cleared successfully, false otherwise
     */
    bool terminal_tab_clear_buffer(terminal_tab_t *tab);

    /**
     * Append data directly to the terminal buffer (for GUI messages).
     *
     * @param tab Terminal tab
     * @param data Data to append
     * @param size Size of the data
     * @return true if successful, false otherwise
     */
    bool terminal_tab_append_buffer(terminal_tab_t *tab, const char *data, int size);

    /**
     * Free resources associated with the terminal tab
     *
     * @param tab Terminal tab to free
     */
    void terminal_tab_free(terminal_tab_t *tab);

#ifdef __cplusplus
}
#endif

#endif /* _TERMINAL_TAB_H_ */