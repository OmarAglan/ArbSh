#include "shell.h"
#include "platform/console.h" // Include PAL

/**
 * _puts_utf8 - prints a UTF-8 string with proper handling to STDOUT
 * @str: the string to be printed
 * Return: Nothing
 */
void _puts_utf8(char *str)
{
    int is_rtl = (get_language() == 1);
    int length;
    
    if (!str)
        return;
    
    length = _strlen(str);
    
    if (is_rtl) {
        // Simplified RTL handling: Use BiDi processing and PAL write.
        // TODO: Revisit BiDi processing integration if needed.
        char *output = malloc(length * 4 + 10);
        if (!output) {
            // Fallback: Write RTL mark, original string, newline
            platform_console_write(PLATFORM_STDOUT_FILENO, "\xE2\x80\x8F", 3);
            platform_console_write(PLATFORM_STDOUT_FILENO, str, length);
            platform_console_write(PLATFORM_STDOUT_FILENO, "\n", 1);
            return;
        }
        
        memcpy(output, "\xE2\x80\x8F", 3); // Start with RTL mark
        int out_pos = 3;
        int processed_length = process_bidirectional_text(str, length, 1, output + out_pos);
        
        if (processed_length > 0) {
            out_pos += processed_length;
            memcpy(output + out_pos, "\n", 1); // Add newline
            out_pos += 1;
            platform_console_write(PLATFORM_STDOUT_FILENO, output, out_pos);
        } else {
            // Fallback if BiDi fails
            platform_console_write(PLATFORM_STDOUT_FILENO, "\xE2\x80\x8F", 3);
            platform_console_write(PLATFORM_STDOUT_FILENO, str, length);
            platform_console_write(PLATFORM_STDOUT_FILENO, "\n", 1);
        }
        free(output);
    } else {
        // LTR handling: Write string then newline
        platform_console_write(PLATFORM_STDOUT_FILENO, str, length);
        platform_console_write(PLATFORM_STDOUT_FILENO, "\n", 1);
    }
}

/**
 * _eputs_utf8 - prints a UTF-8 string to STDERR
 * @str: the string to be printed
 * Return: Nothing
 */
void _eputs_utf8(char *str)
{
    if (!str)
        return;
    // No BiDi processing for stderr, just write directly
    platform_console_write(PLATFORM_STDERR_FILENO, str, _strlen(str));
}

/**
 * _putsfd_utf8 - prints a UTF-8 string to a specific file descriptor
 * @str: the string to be printed
 * @fd: the file descriptor (using platform constants)
 * Return: the number of bytes written or -1 on error
 */
int _putsfd_utf8(char *str, int fd)
{
    if (!str)
        return 0;
    // This function is tricky because fd might not be a console.
    // platform_console_write handles console vs non-console internally.
    return platform_console_write(fd, str, _strlen(str));
}

/**
 * print_prompt_utf8 - prints the shell prompt with RTL/color support using PAL
 * @info: the parameter struct
 * Return: Nothing
 */
void print_prompt_utf8(info_t *info)
{
    char prompt_buffer[512] = {0}; // Increased size
    char *username = NULL;
    char cwd[1024] = {0}; // Increased size
    char status_indicator[8] = {0};
    const char *prompt_base;
    int is_rtl;

    // Only print prompt in interactive mode
    if (!platform_console_isatty(PLATFORM_STDIN_FILENO))
        return;

    /* Get localized prompt base */
    prompt_base = get_message(MSG_PROMPT);
    is_rtl = (get_language() == LANG_AR);

    /* Check if we're hosted by GUI - if so, use simpler prompt (for now) */
    if (is_hosted_by_gui()) // This check might need refinement
    {
        platform_console_write(PLATFORM_STDOUT_FILENO, prompt_base, _strlen(prompt_base));
        return;
    }

    /* Get current working directory */
    // TODO: Replace getcwd with platform_filesystem_getcwd() when PAL is expanded
    if (getcwd(cwd, sizeof(cwd)) == NULL)
        _strcpy(cwd, "?");

    /* Get username from environment */
    username = _getenv(info, "USER");
    if (!username) username = _getenv(info, "USERNAME");
    if (!username) username = "user";

    /* Create status indicator */
    _strcpy(status_indicator, info->status == 0 ? "\xE2\x9C\x93" : "\xE2\x9C\x97"); // UTF-8 Checkmark / X Mark

    /* Set text direction hint for the terminal */
    platform_console_set_text_direction(is_rtl);

    /* --- Format and Print Prompt with Colors --- */
    // Colors definitions (could be moved to a header)
    #define C_RESET   "\033[0m"
    #define C_BOLD    "\033[1m"
    #define C_USER    "\033[1;32m" // Bold Green
    #define C_DIR     "\033[1;34m" // Bold Blue
    #define C_STATUS_OK "\033[1;32m" // Bold Green
    #define C_STATUS_ERR "\033[1;31m" // Bold Red
    #define C_PROMPT  "\033[1;35m" // Bold Magenta

    // Build the prompt string with ANSI color codes
    char temp_buf[128];
    _strcat(prompt_buffer, "[");
    _strcat(prompt_buffer, C_USER);
    _strcat(prompt_buffer, username);
    _strcat(prompt_buffer, C_RESET);
    _strcat(prompt_buffer, "@"); // Separator
    _strcat(prompt_buffer, C_DIR);
    // Shorten long paths?
    _strcat(prompt_buffer, cwd);
    _strcat(prompt_buffer, C_RESET);
    _strcat(prompt_buffer, "] ");

    // Status
    _strcat(prompt_buffer, info->status == 0 ? C_STATUS_OK : C_STATUS_ERR);
    _strcat(prompt_buffer, status_indicator);
    _strcat(prompt_buffer, C_RESET);
    _strcat(prompt_buffer, " ");

    // Prompt base ($)
    _strcat(prompt_buffer, C_PROMPT);
    _strcat(prompt_buffer, prompt_base);
    _strcat(prompt_buffer, C_RESET);
    _strcat(prompt_buffer, " "); // Space after prompt char

    // Add directionality mark for safety
    if (is_rtl)
        platform_console_write(PLATFORM_STDOUT_FILENO, "\xE2\x80\x8F", 3); // RTL Mark
    else
        platform_console_write(PLATFORM_STDOUT_FILENO, "\xE2\x80\x8E", 3); // LTR Mark

    // Write the fully formatted prompt string
    platform_console_write(PLATFORM_STDOUT_FILENO, prompt_buffer, strlen(prompt_buffer));

    // The BUF_FLUSH for _eputchar in shell_loop should handle flushing stdout
} 