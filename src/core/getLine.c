#include "shell.h"
#include <signal.h>
#include "bidi.h"  /* Add include for bidi constants */

#ifdef WINDOWS
#define SIGINT 2
#endif

/**
 * input_buf - buffers chained commands
 * @info: parameter struct
 * @buf: address of buffer
 * @len: address of len var
 *
 * Return: bytes read
 */
ssize_t input_buf(info_t *info, char **buf, size_t *len)
{
    ssize_t r = 0;
    size_t len_p = 0;

    if (!*len) /* if nothing left in the buffer, fill it */
    {
        /*bfree((void **)buf);*/
        free(*buf);
        *buf = NULL;
        signal(SIGINT, sigintHandler);
#if USE_GETLINE
        r = getline(buf, &len_p, stdin);
#else
        r = _getline(info, buf, &len_p);
#endif
        if (r > 0)
        {
            if ((*buf)[r - 1] == '\n')
            {
                (*buf)[r - 1] = '\0'; /* remove trailing newline */
                r--;
            }
            
            /* Process buffer for RTL if in Arabic mode */
            if (get_language() == LANG_AR || get_keyboard_layout() == 1) /* LANG_AR or Arabic layout */
            {
                /* Process the buffer through bidirectional algorithm */
                char *processed_buf = malloc(r * 4 + 16); /* Extra space for control characters and safety */
                if (processed_buf)
                {
                    /* Set base direction based on first strong character */
                    int base_direction = 1; /* Default to RTL for Arabic mode */
                    int first_strong_found = 0;
                    
                    /* Look for first strong directional character */
                    for (int i = 0; i < r;) {
                        int char_len = get_utf8_char_length((*buf)[i]);
                        if (char_len > 0 && i + char_len <= r) {
                            char utf8_char[5] = {0};
                            memcpy(utf8_char, (*buf) + i, char_len);
                            int codepoint;
                            if (utf8_to_codepoint(utf8_char, &codepoint)) {
                                int char_type = get_char_type(codepoint);
                                if (char_type == BIDI_TYPE_L) {
                                    base_direction = 0; /* LTR */
                                    first_strong_found = 1;
                                    break;
                                } else if (char_type == BIDI_TYPE_R || char_type == BIDI_TYPE_AL) {
                                    base_direction = 1; /* RTL */
                                    first_strong_found = 1;
                                    break;
                                }
                            }
                            i += char_len;
                        } else {
                            i++; /* Skip invalid UTF-8 sequence */
                        }
                    }
                    
                    /* If no strong direction found, use layout setting */
                    if (!first_strong_found) {
                        base_direction = get_keyboard_layout();
                    }
                    
                    int processed_length = process_bidirectional_text(*buf, r, base_direction, processed_buf);
                    if (processed_length > 0)
                    {
                        /* Replace original buffer with processed one */
                        free(*buf);
                        *buf = processed_buf;
                        r = processed_length;
                    }
                    else
                    {
                        /* Processing failed, keep original buffer */
                        free(processed_buf);
                    }
                }
            }
            
            info->linecount_flag = 1;
            remove_comments(*buf);
            build_history_list(info, *buf, info->histcount++);
            /* if (_strchr(*buf, ';')) is this a command chain? */
            {
                *len = r;
                info->cmd_buf = buf;
            }
        }
    }
    return (r);
}

/**
 * get_input - gets a line minus the newline
 * @info: parameter struct
 *
 * Return: bytes read
 */
ssize_t get_input(info_t *info)
{
    static char *buf; /* the ';' command chain buffer */
    static size_t i, j, len;
    ssize_t r = 0;
    char **buf_p = &(info->arg), *p;

    _putchar(BUF_FLUSH);
    r = input_buf(info, &buf, &len);
    if (r == -1) /* EOF */
        return (-1);
    if (len) /* we have commands left in the chain buffer */
    {
        j = i;       /* init new iterator to current buf position */
        p = buf + i; /* get pointer for return */

        check_chain(info, buf, &j, i, len);
        while (j < len) /* iterate to semicolon or end */
        {
            if (is_chain(info, buf, &j))
                break;
            j++;
        }

        i = j + 1;    /* increment past nulled ';'' */
        if (i >= len) /* reached end of buffer? */
        {
            i = len = 0; /* reset position and length */
            info->cmd_buf_type = CMD_NORM;
        }

        *buf_p = p;          /* pass back pointer to current command position */
        return (_strlen(p)); /* return length of current command */
    }

    *buf_p = buf; /* else not a chain, pass back buffer from _getline() */
    return (r);   /* return length of buffer from _getline() */
}

/**
 * read_buf - reads a buffer
 * @info: parameter struct
 * @buf: buffer
 * @i: size
 *
 * Return: r
 */
ssize_t read_buf(info_t *info, char *buf, size_t *i)
{
    ssize_t r = 0;

    if (*i)
        return (0);
    r = read(info->readfd, buf, READ_BUF_SIZE);
    if (r >= 0)
        *i = r;
    return (r);
}

/**
 * _getline - gets the next line of input from STDIN
 * @info: parameter struct
 * @ptr: address of pointer to buffer, preallocated or NULL
 * @length: size of preallocated ptr buffer if not NULL
 *
 * Return: size
 */
int _getline(info_t *info, char **ptr, size_t *length)
{
    static char buf[READ_BUF_SIZE];
    static size_t i, len;
    size_t k;
    ssize_t r = 0, s = 0;
    char *p = NULL, *new_p = NULL, *c;

    p = *ptr;
    if (p && length)
        s = *length;
    if (i == len)
        i = len = 0;

    r = read_buf(info, buf, &len);
    if (r == -1 || (r == 0 && len == 0))
        return (-1);

    c = _strchr(buf + i, '\n');
    k = c ? 1 + (unsigned int)(c - buf) : len;
    new_p = _realloc(p, s, s ? s + k : k + 1);
    if (!new_p) /* MALLOC FAILURE! */
        return (p ? free(p), -1 : -1);

    if (s)
        _strncat(new_p, buf + i, k - i);
    else
        _strncpy(new_p, buf + i, k - i + 1);

    s += k - i;
    i = k;
    p = new_p;

    /* Process character widths for cursor positioning if in interactive mode */
    if (interactive(info))
    {
        /* UTF-8 character width processing for proper cursor movement */
        int char_pos = 0;
        int display_pos = 0;
        int utf8_bytes = 0;
        
        /* Calculate character width mappings for cursor positioning */
        while (char_pos < s)
        {
            /* Get UTF-8 character length at current position */
            utf8_bytes = get_utf8_char_length(p[char_pos]);
            
            if (utf8_bytes > 0 && char_pos + utf8_bytes <= s)
            {
                /* Valid UTF-8 character */
                char utf8_char[5] = {0};
                int codepoint;
                
                /* Extract the character */
                memcpy(utf8_char, p + char_pos, utf8_bytes);
                
                /* Convert to Unicode codepoint */
                if (utf8_to_codepoint(utf8_char, &codepoint))
                {
                    /* Special handling for double-width characters (CJK, etc.) */
                    if ((codepoint >= 0x1100 && codepoint <= 0x11FF) ||   /* Hangul Jamo */
                        (codepoint >= 0x3000 && codepoint <= 0x303F) ||   /* CJK Symbols and Punctuation */
                        (codepoint >= 0x3040 && codepoint <= 0x309F) ||   /* Hiragana */
                        (codepoint >= 0x30A0 && codepoint <= 0x30FF) ||   /* Katakana */
                        (codepoint >= 0x3400 && codepoint <= 0x4DBF) ||   /* CJK Unified Ideographs Extension A */
                        (codepoint >= 0x4E00 && codepoint <= 0x9FFF) ||   /* CJK Unified Ideographs */
                        (codepoint >= 0xF900 && codepoint <= 0xFAFF) ||   /* CJK Compatibility Ideographs */
                        (codepoint >= 0xFF00 && codepoint <= 0xFFEF))     /* Halfwidth and Fullwidth Forms */
                    {
                        display_pos += 2;  /* Double-width character */
                    }
                    else
                    {
                        display_pos += 1;  /* Regular width character */
                    }
                }
                else
                {
                    display_pos += 1;  /* Invalid UTF-8 sequence treated as single character */
                }
                
                char_pos += utf8_bytes;  /* Move to next character */
            }
            else
            {
                /* Invalid UTF-8 sequence, treat as single byte */
                display_pos += 1;
                char_pos += 1;
            }
        }
    }

    if (length)
        *length = s;
    *ptr = p;
    return (s);
}

/**
 * sigintHandler - blocks ctrl-C
 * @sig_num: the signal number
 *
 * Return: void
 */
void sigintHandler(__attribute__((unused))int sig_num)
{
    _puts("\n");
    _puts("> ");
    _putchar(BUF_FLUSH);
}
