#include "shell.h"
#include <signal.h>
#include "bidi.h"  /* Add include for bidi constants */
#include "platform/console.h" // Include PAL

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
 * read_buf - reads a buffer using PAL
 * @info: parameter struct (contains readfd)
 * @buf: buffer to read into
 * @i: size pointer (updated with bytes read)
 * Return: bytes read (ssize_t), 0 on EOF, -1 on error
 */
ssize_t read_buf(info_t *info, char *buf, size_t *i)
{
    ssize_t r = 0;

    if (*i)
        return (0); // Buffer already has data

    // Use platform_console_read, passing the read file descriptor from info struct
    r = platform_console_read(info->readfd, buf, READ_BUF_SIZE);

    if (r >= 0) // platform_console_read returns >= 0 on success (including 0 for EOF)
        *i = r;
    // Return value directly from platform_console_read (0 for EOF, -1 for error)
    return (r);
}

/**
 * _getline - gets the next line of input from STDIN (uses read_buf -> PAL)
 * @info: parameter struct
 * @ptr: address of pointer to buffer, preallocated or NULL
 * @length: size of preallocated ptr buffer if not NULL
 * Return: size of line read (ssize_t), or -1 on EOF/error
 */
ssize_t _getline(info_t *info, char **ptr, size_t *length)
{
    static char read_static_buf[READ_BUF_SIZE]; // Renamed to avoid confusion with buf in input_buf
    static size_t buf_i = 0, buf_len = 0;
    size_t k;
    ssize_t r = 0, s = 0;
    char *p = NULL, *new_p = NULL, *c;

    p = *ptr;
    if (p && length)
        s = *length;

    // Loop to read until newline or EOF/error
    while (1) {
        // If buffer is empty, read more data using PAL via read_buf
        if (buf_i == buf_len)
        {
            buf_i = buf_len = 0; // Reset buffer pointers
            r = read_buf(info, read_static_buf, &buf_len);
            if (r == -1 || (r == 0 && buf_len == 0)) // Error or EOF with no data left
            {
                 // Clean up allocated buffer if nothing was added to it
                if (p && s == 0) free(p);
                *ptr = NULL;
                return (-1);
            }
        }

        // Find newline in the current buffer chunk
        c = _strchr(read_static_buf + buf_i, '\n');
        k = c ? 1 + (unsigned int)(c - (read_static_buf + buf_i)) : buf_len - buf_i;

        // Reallocate buffer to hold the new chunk
        new_p = _realloc(p, s, s ? s + k : k + 1);
        if (!new_p) /* MALLOC FAILURE! */
            return (p ? free(p), -1 : -1);

        // Copy the chunk (up to newline or end of buffer) into the result buffer
        _memcpy(new_p + s, read_static_buf + buf_i, k);

        s += k; // Update total size read into result buffer
        buf_i += k; // Update position in static read buffer
        p = new_p;

        if (length) *length = s;
        *ptr = p;

        // If newline was found, terminate the string and return
        if (c) {
            p[s - 1] = '\0'; // Replace newline with null terminator
            // Note: Cursor width calculation was removed, needs re-evaluation
            // in the context of a proper line editing library/implementation.
            return (s - 1);
        }

        // If we read the whole buffer but didn't find newline, loop to read more
        if (buf_i == buf_len) continue;
    }
    // Should not be reached
    return -1;
}

/**
 * interactive - returns true if shell is interactive mode using PAL
 * @info: struct address
 * Return: 1 if interactive mode, 0 otherwise
 */
int interactive(info_t *info)
{
    // Check if standard input is a TTY using PAL
    // and readfd is less than or equal to 2 (stdin, stdout, stderr)
    return (platform_console_isatty(PLATFORM_STDIN_FILENO) && info->readfd <= 2);
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
