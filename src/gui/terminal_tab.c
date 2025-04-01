/**
 * terminal_tab.c - Terminal tab component with process management
 *
 * This file provides the implementation for the terminal tab component that uses
 * the process manager to create and communicate with shell processes.
 */

#include "terminal_tab.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

#define DEFAULT_BUFFER_SIZE 8192
#define DEFAULT_HISTORY_CAPACITY 100
// #define DEFAULT_INPUT_BUFFER_SIZE 1024 // No longer needed here

/**
 * Create a new terminal tab
 */
terminal_tab_t *terminal_tab_create(const char *title, const char *command, char *const args[], char *const env[])
{
    terminal_tab_t *tab = (terminal_tab_t *)malloc(sizeof(terminal_tab_t));
    if (!tab)
    {
        return NULL;
    }

    if (!terminal_tab_init(tab, title, command, args, env))
    {
        free(tab);
        return NULL;
    }

    return tab;
}

/**
 * Initialize a terminal tab
 */
bool terminal_tab_init(terminal_tab_t *tab, const char *title, const char *command, char *const args[], char *const env[])
{
    if (!tab)
    {
        return false;
    }

    // Initialize tab structure
    memset(tab, 0, sizeof(terminal_tab_t));

    // Set title
    if (title)
    {
        tab->title = strdup(title);
        if (!tab->title)
        {
            return false;
        }
    }
    else
    {
        tab->title = strdup("Terminal");
        if (!tab->title)
        {
            return false;
        }
    }

    // Allocate buffer
    tab->buffer_size = DEFAULT_BUFFER_SIZE;
    tab->buffer = (char *)malloc(tab->buffer_size);
    if (!tab->buffer)
    {
        free(tab->title);
        return false;
    }
    tab->buffer[0] = '\0';
    tab->buffer_used = 0;

    // FIX: Remove allocation and initialization of input_buffer members
    // tab->input_buffer_size = DEFAULT_INPUT_BUFFER_SIZE;
    // tab->input_buffer = (char *)malloc(tab->input_buffer_size);
    // if (!tab->input_buffer)
    // {
    //     free(tab->buffer);
    //     free(tab->title);
    //     return false;
    // }
    // tab->input_buffer[0] = '\0';
    // tab->input_buffer_used = 0;
    tab->cursor_position = 0; // Keep cursor position if relevant for terminal emulation

    // Initialize command history
    tab->history_capacity = DEFAULT_HISTORY_CAPACITY;
    tab->command_history = (char **)malloc(sizeof(char *) * tab->history_capacity);
    if (!tab->command_history)
    {
        // FIX: Remove free(tab->input_buffer);
        free(tab->buffer);
        free(tab->title);
        return false;
    }
    tab->history_count = 0;
    tab->history_position = -1;

    // Initialize scroll state
    tab->scroll_position = 0;
    tab->scroll_to_bottom = true;

    // Initialize selection state
    tab->has_selection = false;
    tab->selection_start = 0;
    tab->selection_end = 0;

    // Initialize visual state
    tab->is_focused = false;
    tab->show_scrollbar = true;

    // Initialize custom settings
    tab->font_name = NULL;
    tab->font_size = 16;
    tab->foreground_color = 0xFFFFFFFF; // White
    tab->background_color = 0xFF000000; // Black
    tab->selection_color = 0xFF3080FF;  // Blue
    tab->cursor_color = 0xFFFFFFFF;     // White

    // Initialize terminal state
    tab->width = 80;
    tab->height = 24;

    // Create shell process
    if (!create_shell_process(&tab->process, command, args, env))
    {
        free(tab->command_history);
        // FIX: Remove free(tab->input_buffer);
        free(tab->buffer);
        free(tab->title);
        return false;
    }

    // Set active flag
    tab->is_active = true;

    return true;
}

/**
 * Process terminal tab events
 */
bool terminal_tab_process(terminal_tab_t *tab)
{
    if (!tab || !tab->is_active)
    {
        return false;
    }

    // Check if the process is still running
    if (!is_shell_process_running(&tab->process))
    {
        // Process has exited
        tab->is_active = false;

        // Add exit message to buffer
        int exit_code = get_shell_process_exit_code(&tab->process);
        char exit_message[128];
        snprintf(exit_message, sizeof(exit_message),
                 "\r\nProcess exited with code %d.\r\n", // Removed "Press any key..." as GUI handles closing
                 exit_code);

        // Append exit message to buffer
        terminal_tab_append_buffer(tab, exit_message, strlen(exit_message));

        return false;
    }

    // Read output from process
    char read_buffer[1024];
    // Use a small timeout (e.g., 0) for non-blocking reads suitable for GUI loops
    int bytes_read = read_shell_output(&tab->process, read_buffer, sizeof(read_buffer) - 1, 0);

    // If we have data, append it to the buffer
    if (bytes_read > 0)
    {
        read_buffer[bytes_read] = '\0'; // Null-terminate for safety if used as string
        terminal_tab_append_buffer(tab, read_buffer, bytes_read);
    }
    else if (bytes_read < 0)
    {
        // Handle read error
        fprintf(stderr, "Error reading from shell process for tab '%s'.\n", tab->title ? tab->title : "");
        // Optionally close the tab or mark as inactive on error
        tab->is_active = false;
        return false;
    }

    return true;
}

/**
 * Send input to the terminal tab
 */
bool terminal_tab_send_input(terminal_tab_t *tab, const char *input, int size)
{
    if (!tab || !tab->is_active || !input || size <= 0)
    {
        return false;
    }

    // Check if process is still running before writing
    if (!is_shell_process_running(&tab->process))
    {
        tab->is_active = false; // Update state if found terminated here
        return false;
    }

    // Write input to process
    int bytes_written = write_shell_input(&tab->process, input, size);

    if (bytes_written < 0)
    {
        fprintf(stderr, "Error writing to shell process for tab '%s'.\n", tab->title ? tab->title : "");
        // Process might have terminated between check and write, or other error
        is_shell_process_running(&tab->process); // Update process state
        tab->is_active = false;
        return false;
    }

    return (bytes_written == size);
}

/**
 * Send a command to the terminal tab
 */
bool terminal_tab_send_command(terminal_tab_t *tab, const char *command)
{
    if (!tab || !tab->is_active || !command)
    {
        return false;
    }

    // Create command with newline
    char *cmd_with_newline = NULL;
    int command_len = strlen(command);

    // Allocate buffer for command + newline (\n for Unix-like shells, \r\n for cmd.exe might be better)
    // Let's stick with \n for now, assuming cmd.exe handles it okay.
    size_t full_len = command_len + 1;               // Just \n
    cmd_with_newline = (char *)malloc(full_len + 1); // +1 for null terminator
    if (!cmd_with_newline)
    {
        return false;
    }

    // Copy command and add newline
    memcpy(cmd_with_newline, command, command_len);
    cmd_with_newline[command_len] = '\n'; // Add newline
    cmd_with_newline[command_len + 1] = '\0';

    // Send command to process
    bool result = terminal_tab_send_input(tab, cmd_with_newline, full_len);

    // Add command to history only if it was successfully sent and not empty
    if (result && command_len > 0 && tab->history_count < tab->history_capacity)
    {
        // Avoid adding duplicates? (Optional)
        bool add = true;
        if (tab->history_count > 0 && strcmp(tab->command_history[tab->history_count - 1], command) == 0)
        {
            add = false; // Don't add if same as last command
        }

        if (add)
        {
            // Shift existing history up if full (simple circular buffer or just discard oldest)
            if (tab->history_count == tab->history_capacity)
            {
                free(tab->command_history[0]);
                // Shift elements down
                memmove(&tab->command_history[0], &tab->command_history[1], (tab->history_capacity - 1) * sizeof(char *));
                tab->command_history[tab->history_capacity - 1] = NULL; // Clear last slot
                tab->history_count--;                                   // Decrement count before adding new one
            }

            tab->command_history[tab->history_count] = strdup(command);
            if (tab->command_history[tab->history_count])
            {
                tab->history_count++;
            }
        }
    }

    // Reset history position for next navigation attempt
    tab->history_position = tab->history_count; // Point *after* the last entry

    // FIX: Clear input buffer is handled by the UI (imgui_shell.cpp)
    // tab->input_buffer[0] = '\0';
    // tab->input_buffer_used = 0;
    // tab->cursor_position = 0;

    // Free temporary buffer
    free(cmd_with_newline);

    return result;
}

/**
 * Resize the terminal tab
 */
bool terminal_tab_resize(terminal_tab_t *tab, int width, int height)
{
    if (!tab || width <= 0 || height <= 0)
    {
        return false;
    }

    // Update terminal size stored in the tab
    tab->width = width;
    tab->height = height;

    // Resize the process terminal (inform the child process)
    if (tab->is_active && is_shell_process_running(&tab->process))
    {
        return resize_shell_terminal(&tab->process, width, height);
    }

    // If process isn't active/running, just update our state
    return true;
}

/**
 * Close the terminal tab
 */
bool terminal_tab_close(terminal_tab_t *tab, bool force)
{
    if (!tab)
    {
        return false;
    }

    bool success = true;
    // If process is running according to our state, try terminating it
    if (tab->is_active && is_shell_process_running(&tab->process))
    {
        if (!terminate_shell_process(&tab->process, force))
        {
            fprintf(stderr, "Warning: Failed to terminate process for tab '%s' during close.\n", tab->title ? tab->title : "");
            success = false; // Indicate termination might have failed
        }
    }

    // Mark as inactive regardless of termination success
    tab->is_active = false;

    return success;
}

/**
 * Get the title of the terminal tab
 */
const char *terminal_tab_get_title(terminal_tab_t *tab)
{
    if (!tab || !tab->title)
    {
        return "Terminal";
    }

    return tab->title;
}

/**
 * Set the title of the terminal tab
 */
bool terminal_tab_set_title(terminal_tab_t *tab, const char *title)
{
    if (!tab || !title)
    {
        return false;
    }

    // Free old title
    if (tab->title)
    {
        free(tab->title);
        tab->title = NULL; // Avoid double free if strdup fails
    }

    // Set new title
    tab->title = strdup(title);

    return (tab->title != NULL);
}

/**
 * Get the buffer of the terminal tab
 */
const char *terminal_tab_get_buffer(terminal_tab_t *tab)
{
    if (!tab || !tab->buffer)
    {
        return ""; // Return empty string literal if no buffer
    }

    return tab->buffer;
}

/**
 * Clear the buffer of the terminal tab
 */
bool terminal_tab_clear_buffer(terminal_tab_t *tab)
{
    if (!tab || !tab->buffer)
    {
        return false;
    }

    // Clear buffer
    tab->buffer[0] = '\0';
    tab->buffer_used = 0;

    // Reset scroll position
    tab->scroll_position = 0;
    tab->scroll_to_bottom = true; // Scroll to bottom after clear might be desired

    return true;
}

/**
 * Append data to the terminal buffer (No longer static)
 */
bool terminal_tab_append_buffer(terminal_tab_t *tab, const char *data, int size) // Ensure no 'static' here
{
    if (!tab || !tab->buffer || !data || size <= 0)
    {
        return false;
    }

    // Check if we need to resize the buffer
    if (tab->buffer_used + size + 1 > tab->buffer_size)
    {
        // Option 1: Grow buffer (double size, check against max)
        int new_size = tab->buffer_size * 2;
        if (new_size < tab->buffer_used + size + 1)
        {
            new_size = tab->buffer_used + size + 1; // Ensure enough space if doubling isn't enough
        }

        // Add a safety limit to prevent excessive allocation
        const int MAX_BUFFER_SIZE = 1024 * 1024 * 8; // Example: 8MB limit
        bool truncated = false;
        if (new_size > MAX_BUFFER_SIZE)
        {
            new_size = MAX_BUFFER_SIZE;
            truncated = true; // Signal that we might truncate
        }

        // If resizing still doesn't provide enough space (even at max size), truncate existing data
        if (truncated && tab->buffer_used + size + 1 > new_size)
        {
            fprintf(stderr, "Warning: Terminal buffer limit reached for tab '%s'. Discarding old data.\n", tab->title ? tab->title : "");
            int needed_space = size + 1;
            int space_to_free = tab->buffer_used - (new_size - needed_space);
            if (space_to_free <= 0)
            {
                // Should not happen if new_size is MAX_BUFFER_SIZE, but handle defensively
                fprintf(stderr, "Error: Cannot free enough space in terminal buffer.\n");
                return false;
            }
            memmove(tab->buffer, tab->buffer + space_to_free, tab->buffer_used - space_to_free);
            tab->buffer_used -= space_to_free;
        }

        // Only reallocate if we're not truncating *and* need more space,
        // or if we truncated and new_size is different from buffer_size
        if ((!truncated && tab->buffer_used + size + 1 > tab->buffer_size) || (new_size != tab->buffer_size))
        {
            char *new_buffer = (char *)realloc(tab->buffer, new_size);
            if (!new_buffer)
            {
                fprintf(stderr, "Error: Failed to reallocate terminal buffer to size %d.\n", new_size);
                // Keep old buffer, data cannot be appended
                return false;
            }
            tab->buffer = new_buffer;
            tab->buffer_size = new_size;
        }
    }

    // Append data to buffer
    memcpy(tab->buffer + tab->buffer_used, data, size);
    tab->buffer_used += size;
    tab->buffer[tab->buffer_used] = '\0'; // Ensure null termination

    // Mark for auto-scroll
    tab->scroll_to_bottom = true; // Always scroll when new data is appended

    return true;
}

/**
 * Free resources associated with the terminal tab
 */
void terminal_tab_free(terminal_tab_t *tab)
{
    if (!tab)
    {
        return;
    }

    // Close process if it's still running (force=true during final free)
    terminal_tab_close(tab, true);

    // Cleanup process resources (pipes, handles, etc.)
    cleanup_shell_process(&tab->process);

    // Free buffers
    if (tab->buffer)
    {
        free(tab->buffer);
        tab->buffer = NULL;
    }

    // FIX: Remove freeing of input_buffer
    // if (tab->input_buffer)
    // {
    //     free(tab->input_buffer);
    //     tab->input_buffer = NULL;
    // }

    // Free title
    if (tab->title)
    {
        free(tab->title);
        tab->title = NULL;
    }

    // Free font name
    if (tab->font_name)
    {
        free(tab->font_name);
        tab->font_name = NULL;
    }

    // Free command history
    if (tab->command_history)
    {
        for (int i = 0; i < tab->history_count; i++)
        {
            if (tab->command_history[i])
            {
                free(tab->command_history[i]);
                tab->command_history[i] = NULL;
            }
        }
        free(tab->command_history);
        tab->command_history = NULL;
    }

    // Free tab structure itself
    free(tab);
}