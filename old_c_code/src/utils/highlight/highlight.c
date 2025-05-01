#include "shell.h"
#include <string.h>
#include <stdlib.h>
#include <ctype.h> // For isspace
#include <stdio.h> // For snprintf

// Forward declaration moved here
void highlight_line(const char *line, char *output_buffer, size_t output_size, info_t *info);

/**
 * Color codes for terminal output
 */
#define COLOR_RESET   "\033[0m"
#define COLOR_COMMAND "\033[1;32m"  /* Bright green for commands */
#define COLOR_BUILTIN "\033[1;34m"  /* Cyan for builtin commands */
#define COLOR_ARG     "\033[1;37m"  /* White for arguments */
#define COLOR_VAR     "\033[1;33m"  /* Yellow for variables */
#define COLOR_PATH    "\033[1;34m"  /* Blue for paths */
#define COLOR_QUOTE   "\033[1;35m"  /* Magenta for quoted text */
#define COLOR_ERROR   "\033[1;31m"  /* Red for errors */
#define COLOR_ARGUMENT      "\033[0;37m" // White (same as ARG for now)
#define COLOR_OPERATOR      "\033[1;35m" // Bold Magenta
#define COLOR_STRING        "\033[0;33m" // Yellow
#define COLOR_VARIABLE      "\033[0;36m" // Cyan (different from VAR? maybe COLOR_VAR is for $VAR)
#define COLOR_COMMENT       "\033[0;37m" // Gray/White (adjust as needed)

/**
 * is_builtin - check if a command is a shell builtin
 * @cmd: the command to check
 *
 * Return: 1 if builtin, 0 otherwise
 */
int is_builtin(const char *cmd)
{
    static const char *builtins[] = {
        "exit", "env", "help", "history", "setenv",
        "unsetenv", "cd", "alias", "lang", "test",
        "layout", "config", NULL
    };
    
    int i;
    
    if (!cmd)
        return 0;
    
    for (i = 0; builtins[i]; i++)
    {
        if (_strcmp((char *)cmd, (char *)builtins[i]) == 0)
            return 1;
    }
    
    return 0;
}

/**
 * highlight_command - Adds syntax highlighting to a command string.
 * @input: The command string input.
 * @info: Pointer to the shell info struct (for context like is_cmd).
 * Return: A newly allocated string with ANSI color codes, or NULL on error.
 *         The caller is responsible for freeing the returned string.
 */
char *highlight_command(const char *input, info_t *info)
{
    // Estimate buffer size (generous: 2x original + color codes)
    size_t input_len = strlen(input);
    size_t buffer_size = input_len * 2 + 256; // Extra space for colors/reset
    char *highlighted = malloc(buffer_size);
    if (!highlighted)
    {
        return NULL; // Allocation failed
    }

    // Call the core highlighting logic
    highlight_line(input, highlighted, buffer_size, info); // Pass info

    return highlighted;
}

/**
 * print_highlighted_input - Print a command string with syntax highlighting
 * @input: the input string to highlight and print
 *
 * Return: void
 */
void print_highlighted_input(char *input)
{
    char *highlighted = highlight_command(input, NULL);
    
    if (highlighted)
    {
        _puts(highlighted);
        free(highlighted);
    }
    else
    {
        /* Fall back to normal output if highlighting fails */
        _puts(input);
    }
}

/**
 * get_highlighted_prompt - Get a highlighted prompt with status info
 * @info: shell info struct
 *
 * Return: dynamically allocated highlighted prompt string
 */
char *get_highlighted_prompt(info_t *info)
{
    char *prompt = malloc(256);
    char cwd[256] = {0};
    char *username = NULL;
    
    if (!prompt)
        return NULL;
    
    /* Get current working directory */
    if (getcwd(cwd, sizeof(cwd)) == NULL)
        _strcpy(cwd, "?");
    
    /* Get username from environment */
    username = _getenv(info, "USER=");
    if (!username)
        username = _getenv(info, "USERNAME=");
    if (!username)
        username = "user";
    
    /* Create a highlighted prompt with user, directory and status */
    _snprintf(prompt, 256, "%s%s%s@%s%s%s:%s%s%s$ ", 
             info->status == 0 ? "\033[1;32m" : "\033[1;31m", 
             username, 
             "\033[0m",
             "\033[1;34m", 
             cwd, 
             "\033[0m",
             info->status == 0 ? "\033[1;32m" : "\033[1;31m",
             info->status == 0 ? "✓" : "✗",
             "\033[0m");
    
    return prompt;
}

/**
 * highlight_line - Applies syntax highlighting to a shell command line.
 * @line: The input command line string.
 * @output_buffer: Buffer to store the highlighted output.
 * @output_size: Size of the output buffer.
 * @info: Shell info struct (needed for is_cmd context)
 */
void highlight_line(const char *line, char *output_buffer, size_t output_size, info_t *info)
{
    size_t line_len = strlen(line);
    size_t out_idx = 0;
    size_t line_idx = 0;
    char current_token[1024];
    size_t token_idx = 0;
    int in_string = 0; // 0 = no, 1 = double quotes, 2 = single quotes
    int potential_command = 1; // Flag to check the first word as a command

    output_buffer[0] = '\0';

    while (line_idx < line_len && out_idx < output_size - 20) { // Leave buffer space
        char current_char = line[line_idx];

        // Handle whitespace: terminates the current token
        if (isspace(current_char) && !in_string) {
            if (token_idx > 0) {
                current_token[token_idx] = '\0';
                const char *color = COLOR_ARGUMENT; // Default color

                // Check if it's the command
                if (potential_command) {
                    if (info != NULL && is_cmd(info, current_token)) {
                        color = COLOR_COMMAND;
                    } else {
                        // Check common builtins (placeholder)
                        if (strcmp(current_token, "cd") == 0 || strcmp(current_token, "exit") == 0 ||
                            strcmp(current_token, "pwd") == 0 || strcmp(current_token, "ls") == 0 ||
                            strcmp(current_token, "echo") == 0 || strcmp(current_token, "export") == 0 ||
                            strcmp(current_token, "unset") == 0 || strcmp(current_token, "alias") == 0 ||
                            strcmp(current_token, "clear") == 0 || strcmp(current_token, "history") == 0 ||
                            strcmp(current_token, "lang") == 0 || strcmp(current_token, "layout") == 0 ||
                            strcmp(current_token, "config") == 0)
                        {
                             color = COLOR_BUILTIN;
                        }
                    }
                    potential_command = 0; // Only the first word can be the command
                } else if (current_token[0] == '-' && strlen(current_token) > 1) { // Treat as argument
                    color = COLOR_ARGUMENT;
                } else if (strchr("|&;<>", current_token[0]) && token_idx == 1) { // Basic operator check
                     color = COLOR_OPERATOR;
                } else if (current_token[0] == '$') {
                    color = COLOR_VARIABLE;
                }

                snprintf(output_buffer + out_idx, output_size - out_idx, "%s%s%s", color, current_token, COLOR_RESET);
                out_idx += strlen(output_buffer + out_idx);
                token_idx = 0;
            }
            // Append the whitespace itself
            if (out_idx < output_size - 1) { // Check buffer space for whitespace
                output_buffer[out_idx++] = current_char;
                output_buffer[out_idx] = '\0';
            }
            potential_command = (out_idx == 1); // Reset potential command only if it was the very start
        }
        // Handle string literals
        else if ((current_char == '"' || current_char == '\'') && (line_idx == 0 || line[line_idx - 1] != '\\')) {
             if (in_string == 0) {
                in_string = (current_char == '"' ? 1 : 2);
                current_token[token_idx++] = current_char;
             } else if ((in_string == 1 && current_char == '"') || (in_string == 2 && current_char == '\'')) {
                current_token[token_idx++] = current_char;
                current_token[token_idx] = '\0';
                snprintf(output_buffer + out_idx, output_size - out_idx, "%s%s%s", COLOR_STRING, current_token, COLOR_RESET);
                out_idx += strlen(output_buffer + out_idx);
                token_idx = 0;
                in_string = 0;
             } else {
                current_token[token_idx++] = current_char; // Character inside string
             }
        }
        // Handle comments
        else if (current_char == '#' && !in_string && (line_idx == 0 || isspace(line[line_idx-1]))) {
            // Flush previous token if any
            if (token_idx > 0) {
                 current_token[token_idx] = '\0';
                 const char *color = COLOR_ARGUMENT;
                 if (potential_command) {
                      if (info != NULL && is_cmd(info, current_token)) { color = COLOR_COMMAND; }
                      else { /* Check builtins? */ color = COLOR_BUILTIN; }
                 } else if (current_token[0] == '$') { color = COLOR_VARIABLE; }
                 snprintf(output_buffer + out_idx, output_size - out_idx, "%s%s%s", color, current_token, COLOR_RESET);
                 out_idx += strlen(output_buffer + out_idx);
                 token_idx = 0;
            }
            // Add comment color and the rest of the line
            snprintf(output_buffer + out_idx, output_size - out_idx, "%s%s%s", COLOR_COMMENT, line + line_idx, COLOR_RESET);
            out_idx += strlen(output_buffer + out_idx);
            break; // End processing after comment start
        }
         // Handle operators (simple cases)
        else if (!in_string && strchr("|&;<>", current_char)) {
            // Flush previous token
            if (token_idx > 0) {
                current_token[token_idx] = '\0';
                const char *color = COLOR_ARGUMENT;
                if (potential_command) { /* Command/Builtin Check */
                    if (info != NULL && is_cmd(info, current_token)) { color = COLOR_COMMAND; }
                    else { color = COLOR_BUILTIN; } // Simple fallback
                    potential_command = 0;
                } else if (current_token[0] == '$') { color = COLOR_VARIABLE; }
                snprintf(output_buffer + out_idx, output_size - out_idx, "%s%s%s", color, current_token, COLOR_RESET);
                out_idx += strlen(output_buffer + out_idx);
                token_idx = 0;
            }
             // Add operator token
            char op_str[2] = {current_char, '\0'};
            snprintf(output_buffer + out_idx, output_size - out_idx, "%s%s%s", COLOR_OPERATOR, op_str, COLOR_RESET);
            out_idx += strlen(output_buffer + out_idx);
            potential_command = 1; // Operator likely followed by a command
        }
        // Accumulate token characters
        else {
             if (token_idx < sizeof(current_token) - 1) {
                current_token[token_idx++] = current_char;
             } else { // Prevent overflow
                 // Flush what we have and restart token
                 current_token[token_idx] = '\0';
                 snprintf(output_buffer + out_idx, output_size - out_idx, "%s", current_token); // No color
                 out_idx += strlen(output_buffer + out_idx);
                 token_idx = 0;
                 current_token[token_idx++] = current_char; // Start new token
             }
        }
        line_idx++;
    }

    // Flush any remaining token at the end of the line
    if (token_idx > 0 && out_idx < output_size - 20) {
        current_token[token_idx] = '\0';
        const char *color = COLOR_ARGUMENT; // Default
         if (potential_command) {
             if (info != NULL && is_cmd(info, current_token)) { color = COLOR_COMMAND; }
             else { /* Check builtins? */ color = COLOR_BUILTIN; }
         } else if (in_string) { // Unclosed string
             color = COLOR_STRING;
         } else if (current_token[0] == '$') { color = COLOR_VARIABLE; }

        snprintf(output_buffer + out_idx, output_size - out_idx, "%s%s%s", color, current_token, COLOR_RESET);
    }

    // Ensure null termination if something was written
    if (out_idx < output_size)
        output_buffer[out_idx] = '\0';
    else if (output_size > 0)
        output_buffer[output_size - 1] = '\0'; // Ensure termination even if truncated
} 