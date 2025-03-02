#include "shell.h"

/**
 * get_utf8_char_length - Determines the length of a UTF-8 character
 * @first_byte: The first byte of the UTF-8 character
 *
 * Return: The length of the UTF-8 character (1-4 bytes)
 */
int get_utf8_char_length(char first_byte)
{
    if ((first_byte & 0x80) == 0)
        return 1; /* ASCII character (0xxxxxxx) */
    if ((first_byte & 0xE0) == 0xC0)
        return 2; /* 2-byte sequence (110xxxxx) */
    if ((first_byte & 0xF0) == 0xE0)
        return 3; /* 3-byte sequence (1110xxxx) */
    if ((first_byte & 0xF8) == 0xF0)
        return 4; /* 4-byte sequence (11110xxx) */
    
    return 1; /* Invalid UTF-8, treat as single byte */
}

/**
 * set_text_direction - Sets the text direction based on the current language
 * @is_rtl: Whether to set RTL (1) or LTR (0) direction
 *
 * Return: 0 on success, -1 on failure
 */
int set_text_direction(int is_rtl)
{
#ifdef _WIN32
    /* Windows console support for RTL using Virtual Terminal Sequences */
    if (is_rtl) {
        /* Set RTL mode using comprehensive escape sequences */
        write(STDOUT_FILENO, "\033[?7l", 5);     /* Disable line wrapping */
        
        /* Use more reliable RTL rendering sequence */
        write(STDOUT_FILENO, "\033[2J", 4);      /* Clear screen for clean RTL rendering */
        write(STDOUT_FILENO, "\033[0m", 4);      /* Reset attributes */
        
        /* Set base direction to RTL with multiple techniques for better compatibility */
        write(STDOUT_FILENO, "\033]8;;bidi=R\a", 12);
        
        /* Force RTL paragraph direction */
        write(STDOUT_FILENO, "\xE2\x80\x8F", 3); /* RTL mark (U+200F) */
        
        /* Enable additional RTL mode flags - these are implementation specific but work on newer Windows Terminal */
        write(STDOUT_FILENO, "\033[=14h", 6);    /* Enable RTL rendering */
        write(STDOUT_FILENO, "\033[=15h", 6);    /* Enable RTL cursor movement */
        
        /* Set console mode through Windows API for better support */
        #ifdef WINDOWS
        HANDLE hOut = GetStdHandle(STD_OUTPUT_HANDLE);
        if (hOut != INVALID_HANDLE_VALUE) {
            DWORD dwMode = 0;
            if (GetConsoleMode(hOut, &dwMode)) {
                /* Set ENABLE_VIRTUAL_TERMINAL_PROCESSING and other relevant flags */
                dwMode |= 0x0004 | 0x0008; /* ENABLE_VIRTUAL_TERMINAL_PROCESSING | ENABLE_PROCESSED_OUTPUT */
                SetConsoleMode(hOut, dwMode);
            }
        }
        #endif
    } else {
        /* Set LTR mode using comprehensive escape sequences */
        write(STDOUT_FILENO, "\033[?7h", 5);     /* Enable line wrapping */
        write(STDOUT_FILENO, "\033[0m", 4);      /* Reset attributes */
        
        /* Set base direction to LTR */
        write(STDOUT_FILENO, "\033]8;;bidi=L\a", 12);
        
        /* Force LTR paragraph direction */
        write(STDOUT_FILENO, "\xE2\x80\x8E", 3); /* LTR mark (U+200E) */
        
        /* Disable RTL mode flags */
        write(STDOUT_FILENO, "\033[=14l", 6);    /* Disable RTL rendering */
        write(STDOUT_FILENO, "\033[=15l", 6);    /* Disable RTL cursor movement */
    }
#else
    /* For Unix/Linux systems with proper terminal support */
    if (is_rtl) {
        /* Set RTL mode with better compatibility */
        write(STDOUT_FILENO, "\033[?7l", 5);    /* Disable line wrapping */
        write(STDOUT_FILENO, "\xE2\x80\x8F", 3); /* RTL mark (U+200F) */
        
        /* Use BiDi console mode if available (some modern terminals) */
        write(STDOUT_FILENO, "\033]8;;bidi=R\a", 12);
    } else {
        /* Set LTR mode */
        write(STDOUT_FILENO, "\033[?7h", 5);    /* Enable line wrapping */
        write(STDOUT_FILENO, "\xE2\x80\x8E", 3); /* LTR mark (U+200E) */
        
        /* Reset BiDi console mode if available */
        write(STDOUT_FILENO, "\033]8;;bidi=L\a", 12);
    }
#endif
    return 0;
}

/**
 * read_utf8_char - Reads a complete UTF-8 character from a buffer
 * @buffer: The buffer containing the UTF-8 character
 * @max_size: The maximum number of bytes to read
 *
 * Return: The length of the UTF-8 character read, or 0 if invalid
 */
int read_utf8_char(char *buffer, int max_size)
{
    if (max_size <= 0)
        return 0;
    
    int char_length = get_utf8_char_length(buffer[0]);
    if (char_length > max_size)
        return 0;
    
    /* Validate continuation bytes */
    for (int i = 1; i < char_length; i++)
    {
        if ((buffer[i] & 0xC0) != 0x80)
            return 0; /* Invalid continuation byte */
    }
    
    return char_length;
}

/**
 * is_rtl_char - Checks if a Unicode codepoint is a right-to-left character
 * @unicode_codepoint: The Unicode codepoint to check
 *
 * Return: 1 if the character is RTL, 0 otherwise
 */
int is_rtl_char(int unicode_codepoint)
{
    /* Arabic range (0x0600-0x06FF) */
    if (unicode_codepoint >= 0x0600 && unicode_codepoint <= 0x06FF)
        return 1;
    
    /* Arabic Supplement (0x0750-0x077F) */
    if (unicode_codepoint >= 0x0750 && unicode_codepoint <= 0x077F)
        return 1;
    
    /* Arabic Extended-A (0x08A0-0x08FF) */
    if (unicode_codepoint >= 0x08A0 && unicode_codepoint <= 0x08FF)
        return 1;
    
    /* Hebrew range (0x0590-0x05FF) */
    if (unicode_codepoint >= 0x0590 && unicode_codepoint <= 0x05FF)
        return 1;
    
    return 0;
}
/**
 * utf8_to_codepoint - Converts a UTF-8 character to a Unicode codepoint
 * @utf8_char: The UTF-8 character buffer
 * @codepoint: Pointer to store the resulting codepoint
 *
 * Return: 1 on success, 0 on failure
 */
int utf8_to_codepoint(const char *utf8_char, int *codepoint)
{
    if (!utf8_char || !codepoint)
        return 0;
    
    int length = get_utf8_char_length(utf8_char[0]);
    
    if (length == 1)
    {
        *codepoint = (unsigned char)utf8_char[0];
    }
    else if (length == 2)
    {
        *codepoint = ((utf8_char[0] & 0x1F) << 6) | (utf8_char[1] & 0x3F);
    }
    else if (length == 3)
    {
        *codepoint = ((utf8_char[0] & 0x0F) << 12) | 
                    ((utf8_char[1] & 0x3F) << 6) | 
                    (utf8_char[2] & 0x3F);
    }
    else if (length == 4)
    {
        *codepoint = ((utf8_char[0] & 0x07) << 18) | 
                    ((utf8_char[1] & 0x3F) << 12) | 
                    ((utf8_char[2] & 0x3F) << 6) | 
                    (utf8_char[3] & 0x3F);
    }
    else
    {
        return 0; /* Invalid UTF-8 sequence */
    }
    
    return 1;
}

/**
 * codepoint_to_utf8 - Converts a Unicode codepoint to a UTF-8 character
 * @codepoint: The Unicode codepoint
 * @utf8_char: The buffer to store the UTF-8 character
 *
 * Return: The length of the UTF-8 character
 */
int codepoint_to_utf8(int codepoint, char *utf8_char)
{
    if (codepoint < 0x80)
    {
        /* 1-byte sequence */
        utf8_char[0] = codepoint;
        return 1;
    }
    else if (codepoint < 0x800)
    {
        /* 2-byte sequence */
        utf8_char[0] = 0xC0 | (codepoint >> 6);
        utf8_char[1] = 0x80 | (codepoint & 0x3F);
        return 2;
    }
    else if (codepoint < 0x10000)
    {
        /* 3-byte sequence */
        utf8_char[0] = 0xE0 | (codepoint >> 12);
        utf8_char[1] = 0x80 | ((codepoint >> 6) & 0x3F);
        utf8_char[2] = 0x80 | (codepoint & 0x3F);
        return 3;
    }
    else if (codepoint < 0x110000)
    {
        /* 4-byte sequence */
        utf8_char[0] = 0xF0 | (codepoint >> 18);
        utf8_char[1] = 0x80 | ((codepoint >> 12) & 0x3F);
        utf8_char[2] = 0x80 | ((codepoint >> 6) & 0x3F);
        utf8_char[3] = 0x80 | (codepoint & 0x3F);
        return 4;
    }
    
    /* Invalid codepoint */
    utf8_char[0] = '?';
    return 1;
}

/**
 * configure_terminal_for_utf8 - Configures the terminal for UTF-8 support
 */
void configure_terminal_for_utf8(void)
{
#ifdef _WIN32
    /* Set console code page to UTF-8 */
    SetConsoleOutputCP(65001); /* CP_UTF8 */
    SetConsoleCP(65001);       /* CP_UTF8 */
    
    /* Enable VT processing for ANSI escape sequences */
    #ifdef WINDOWS
    HANDLE hOut = GetStdHandle(STD_OUTPUT_HANDLE);
    if (hOut != INVALID_HANDLE_VALUE)
    {
        DWORD dwMode = 0;
        if (GetConsoleMode(hOut, &dwMode))
        {
            dwMode |= 0x0004; /* ENABLE_VIRTUAL_TERMINAL_PROCESSING */
            SetConsoleMode(hOut, dwMode);
        }
    }
    
    /* Configure console font for better Unicode support */
    CONSOLE_FONT_INFOEX cfi;
    cfi.cbSize = sizeof(cfi);
    cfi.nFont = 0;
    cfi.dwFontSize.X = 0;  /* Default width */
    cfi.dwFontSize.Y = 16; /* Default height */
    cfi.FontFamily = FF_DONTCARE;
    cfi.FontWeight = FW_NORMAL;
    wcscpy(cfi.FaceName, L"Consolas"); /* Use a font with good Unicode support */
    SetCurrentConsoleFontEx(hOut, FALSE, &cfi);
    
    /* Set console window size */
    CONSOLE_SCREEN_BUFFER_INFO csbi;
    if (GetConsoleScreenBufferInfo(hOut, &csbi))
    {
        SMALL_RECT windowRect = {0, 0, 100, 30}; /* Width: 100, Height: 30 */
        SetConsoleWindowInfo(hOut, TRUE, &windowRect);
    }
    #endif
#else
    /* Set locale to use UTF-8 */
    setlocale(LC_ALL, "en_US.UTF-8");
#endif
}