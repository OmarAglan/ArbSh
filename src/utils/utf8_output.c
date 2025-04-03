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
    int length = _strlen(str);
    
    if (!str)
        return;
    
    if (is_rtl) {
        /* Allocate buffers for bidirectional processing */
        char *output = malloc(length * 4 + 10); /* Extra space for control characters */
        
        if (!output) {
            /* Memory allocation failed, use basic output */
            write(STDOUT_FILENO, "\xE2\x80\x8F", 3); /* RTL mark (U+200F) */
            write(STDOUT_FILENO, str, length);
            write(STDOUT_FILENO, "\n", 1);
            return;
        }
        
        /* Add RTL mark at the beginning */
        memcpy(output, "\xE2\x80\x8F", 3); /* RTL mark (U+200F) */
        int out_pos = 3;
        
        /* Process text with bidirectional algorithm */
        int processed_length = process_bidirectional_text(str, length, 1, output + out_pos);
        
        if (processed_length > 0) {
            out_pos += processed_length;
            
            /* Add newline and ending RTL mark */
            memcpy(output + out_pos, "\xE2\x80\x8F\n", 4);
            out_pos += 4;
            
            /* Write the entire processed buffer */
            write(STDOUT_FILENO, output, out_pos);
        } else {
            /* Bidirectional processing failed, fallback to simpler approach */
            write(STDOUT_FILENO, "\xE2\x80\x8F", 3); /* RTL mark (U+200F) */
            
            /* Write character by character in reverse order for RTL */
            int pos = length;
            while (pos > 0) {
                /* Find the start of the previous UTF-8 character */
                int char_start = pos - 1;
                while (char_start > 0 && (str[char_start] & 0xC0) == 0x80) {
                    char_start--;
                }
                
                /* Write the UTF-8 character */
                write(STDOUT_FILENO, str + char_start, pos - char_start);
                pos = char_start;
            }
            
            write(STDOUT_FILENO, "\n", 1);
        }
        
        free(output);
    } else {
        /* Simple LTR handling for non-Arabic text */
        while (str[i] != '\0')
        {
            char_length = get_utf8_char_length(str[i]);
            
            /* Check if we have a complete UTF-8 character */
            if (i + char_length <= length)
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
        
        _putchar('\n');
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
        
        /* Check if we're hosted by GUI - if so, use simpler prompt */
        if (is_hosted_by_gui())
        {
            if (get_language() == 1) /* LANG_AR */
                _puts_utf8((char *)prompt);
            else
                _puts((char *)prompt);
            return;
        }
        
        /* Check if we're using Arabic */
        if (get_language() == 1) /* LANG_AR */
        {
            /* Force text direction to RTL */
            set_text_direction(1);
            
            /* For Arabic, create a robust RTL prompt with multiple control codes */
            /* First, add control characters to ensure RTL rendering */
            write(STDOUT_FILENO, "\xE2\x80\x8F", 3);   /* RTL mark (U+200F) */
            
            /* For Windows terminals, we need to add additional controls */
            #ifdef WINDOWS
            /* Add specialized formatting for Windows terminal */
            write(STDOUT_FILENO, "\033[1;36m", 7);     /* Make prompt visible with cyan color */
            #endif
            
            /* Write the actual prompt content */
            _puts_utf8((char *)prompt);
            
            /* Add finishing RTL controls to maintain direction */
            write(STDOUT_FILENO, "\xE2\x80\x8F", 3);   /* Another RTL mark to reinforce direction */
            
            #ifdef WINDOWS
            /* Reset color formatting */
            write(STDOUT_FILENO, "\033[0m", 4);
            #endif
        }
        else
        {
            /* For English, use regular output with LTR direction */
            set_text_direction(0);
            
            /* Add special LTR marker for better rendering */
            write(STDOUT_FILENO, "\xE2\x80\x8E", 3);   /* LTR mark (U+200E) */
            
            #ifdef WINDOWS
            /* Add highlighting for visibility */
            write(STDOUT_FILENO, "\033[1;32m", 7);     /* Make prompt visible with green color */
            #endif
            
            /* Write the prompt */
            _puts((char *)prompt);
            
            #ifdef WINDOWS
            /* Reset color formatting */
            write(STDOUT_FILENO, "\033[0m", 4);
            #endif
        }
    }
} 