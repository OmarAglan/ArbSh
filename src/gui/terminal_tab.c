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
#define DEFAULT_INPUT_BUFFER_SIZE 1024

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

    // Allocate input buffer
    tab->input_buffer_size = DEFAULT_INPUT_BUFFER_SIZE;
    tab->input_buffer = (char *)malloc(tab->input_buffer_size);
    if (!tab->input_buffer)
    {
        free(tab->buffer);
        free(tab->title);
        return false;
    }
    tab->input_buffer[0] = '\0';
    tab->input_buffer_used = 0;
    tab->cursor_position = 0;

    // Initialize command history
    tab->history_capacity = DEFAULT_HISTORY_CAPACITY;
    tab->command_history = (char **)malloc(sizeof(char *) * tab->history_capacity);
    if (!tab->command_history)
    {
        free(tab->input_buffer);
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
        free(tab->input_buffer);
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
                 "\r\nProcess exited with code %d.\r\nPress any key to close this terminal...",
                 exit_code);

        // Append exit message to buffer
        terminal_tab_append_buffer(tab, exit_message, strlen(exit_message));

        return false;
    }

    // Read output from process
    char read_buffer[1024];
    int bytes_read = read_shell_output(&tab->process, read_buffer, sizeof(read_buffer), 0);

    // If we have data, append it to the buffer
    if (bytes_read > 0)
    {
        terminal_tab_append_buffer(tab, read_buffer, bytes_read);
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

    // Write input to process
    int bytes_written = write_shell_input(&tab->process, input, size);

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

    // Allocate buffer for command + newline
    cmd_with_newline = (char *)malloc(command_len + 2);
    if (!cmd_with_newline)
    {
        return false;
    }

    // Copy command and add newline
    memcpy(cmd_with_newline, command, command_len);
    cmd_with_newline[command_len] = '\n';
    cmd_with_newline[command_len + 1] = '\0';

    // Send command to process
    bool result = terminal_tab_send_input(tab, cmd_with_newline, command_len + 1);

    // Add command to history
    if (result && tab->history_count < tab->history_capacity)
    {
        tab->command_history[tab->history_count] = strdup(command);
        if (tab->command_history[tab->history_count])
        {
            tab->history_count++;
        }
    }

    // Reset history position
    tab->history_position = -1;

    // Clear input buffer
    tab->input_buffer[0] = '\0';
    tab->input_buffer_used = 0;
    tab->cursor_position = 0;

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

    // Update terminal size
    tab->width = width;
    tab->height = height;

    // Resize the process terminal
    if (tab->is_active)
    {
        return resize_shell_terminal(&tab->process, width, height);
    }

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

    // If process is running, terminate it
    if (tab->is_active)
    {
        if (!terminate_shell_process(&tab->process, force))
        {
            return false;
        }
    }

    // Mark as inactive
    tab->is_active = false;

    return true;
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
        return "";
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
    tab->scroll_to_bottom = true;

    return true;
}

/**
 * Append data to the terminal buffer
 */
bool terminal_tab_append_buffer(terminal_tab_t *tab, const char *data, int size)
{
    if (!tab || !tab->buffer || !data || size <= 0)
    {
        return false;
    }

    // Check if we need to resize the buffer
    if (tab->buffer_used + size + 1 > tab->buffer_size)
    {
        // Grow buffer by doubling its size
        int new_size = tab->buffer_size * 2;
        if (new_size < tab->buffer_used + size + 1)
        {
            new_size = tab->buffer_used + size + 1;
        }

        char *new_buffer = (char *)realloc(tab->buffer, new_size);
        if (!new_buffer)
        {
            return false;
        }

        tab->buffer = new_buffer;
        tab->buffer_size = new_size;
    }

    // Append data to buffer
    memcpy(tab->buffer + tab->buffer_used, data, size);
    tab->buffer_used += size;
    tab->buffer[tab->buffer_used] = '\0';

    // Auto-scroll to bottom if enabled
    if (tab->scroll_to_bottom)
    {
        tab->scroll_position = tab->buffer_used;
    }

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

    // Close process if it's still running
    if (tab->is_active)
    {
        terminal_tab_close(tab, true);
    }

    // Cleanup process resources
    cleanup_shell_process(&tab->process);

    // Free buffers
    if (tab->buffer)
    {
        free(tab->buffer);
    }

    if (tab->input_buffer)
    {
        free(tab->input_buffer);
    }

    // Free title
    if (tab->title)
    {
        free(tab->title);
    }

    // Free font name
    if (tab->font_name)
    {
        free(tab->font_name);
    }

    // Free command history
    if (tab->command_history)
    {
        for (int i = 0; i < tab->history_count; i++)
        {
            if (tab->command_history[i])
            {
                free(tab->command_history[i]);
            }
        }
        free(tab->command_history);
    }

    // Free tab structure
    free(tab);
}