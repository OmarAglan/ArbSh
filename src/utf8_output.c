#include "shell.h"

/**
 * _puts_utf8 - prints a UTF-8 string with proper handling
 * @str: the string to be printed
 *
 * Return: Nothing
 */
void _puts_utf8(char *str)
{
    int i = 0;
    int char_length;
    int is_rtl = (get_language() == 1); /* Check if we're in RTL mode */

    if (!str)
        return;
    
    /* If in RTL mode, add RTL mark at the beginning */
    if (is_rtl) {
        write(STDOUT_FILENO, "\xE2\x80\x8F", 3); /* RTL mark (U+200F) */
    }
    
    while (str[i] != '\0')
    {
        char_length = get_utf8_char_length(str[i]);
        
        /* Check if we have a complete UTF-8 character */
        if (i + char_length <= _strlen(str))
        {
            /* Write the complete UTF-8 character */
            write(1, &str[i], char_length);
            i += char_length;
        }
        else
        {
            /* Incomplete UTF-8 character, write a replacement character */
            _putchar('?');
            i++;
        }
    }
    
    /* If in RTL mode, add pop directional formatting at the end */
    if (is_rtl) {
        write(STDOUT_FILENO, "\xE2\x80\x8C", 3); /* Pop Directional Formatting (U+200C) */
    }
}

/**
 * _eputs_utf8 - prints a UTF-8 string to stderr with proper handling
 * @str: the string to be printed
 *
 * Return: Nothing
 */
void _eputs_utf8(char *str)
{
    int i = 0;
    int char_length;

    if (!str)
        return;
    
    while (str[i] != '\0')
    {
        char_length = get_utf8_char_length(str[i]);
        
        /* Check if we have a complete UTF-8 character */
        if (i + char_length <= _strlen(str))
        {
            /* Write the complete UTF-8 character */
            write(2, &str[i], char_length);
            i += char_length;
        }
        else
        {
            /* Incomplete UTF-8 character, write a replacement character */
            _eputchar('?');
            i++;
        }
    }
}

/**
 * _putsfd_utf8 - prints a UTF-8 string to a file descriptor
 * @str: the string to be printed
 * @fd: the file descriptor to write to
 *
 * Return: the number of bytes written
 */
int _putsfd_utf8(char *str, int fd)
{
    int i = 0;
    int bytes_written = 0;
    int char_length;

    if (!str)
        return (0);
    
    while (str[i] != '\0')
    {
        char_length = get_utf8_char_length(str[i]);
        
        /* Check if we have a complete UTF-8 character */
        if (i + char_length <= _strlen(str))
        {
            /* Write the complete UTF-8 character */
            bytes_written += write(fd, &str[i], char_length);
            i += char_length;
        }
        else
        {
            /* Incomplete UTF-8 character, write a replacement character */
            bytes_written += _putfd('?', fd);
            i++;
        }
    }
    
    return bytes_written;
}

/**
 * print_prompt_utf8 - prints the shell prompt with RTL support
 * @info: the parameter struct
 *
 * Return: Nothing
 */
void print_prompt_utf8(info_t *info)
{
    const char *prompt;
    
    /* Check if we're in interactive mode */
    if (interactive(info))
    {
        /* Get localized prompt */
        prompt = get_message(MSG_PROMPT);
        
        /* Check if we're using Arabic */
        if (get_language() == 1) /* LANG_AR */
        {
            /* For Arabic, use UTF-8 aware output with RTL direction */
            /* Add special RTL marker for better rendering */
            write(STDOUT_FILENO, "\xE2\x80\x8F", 3); /* RTL mark (U+200F) */
            _puts_utf8((char *)prompt);
        }
        else
        {
            /* For English, use regular output with LTR direction */
            /* Add special LTR marker for better rendering */
            write(STDOUT_FILENO, "\xE2\x80\x8E", 3); /* LTR mark (U+200E) */
            _puts((char *)prompt);
        }
    }
} 