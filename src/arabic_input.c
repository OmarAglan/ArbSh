/**
 * arabic_input.c - Arabic keyboard layout and input method support
 *
 * This file implements Arabic keyboard layout support and input method switching
 * for the Simple Shell project, addressing the high-priority task of enhancing
 * Arabic text input as outlined in the development tasks.
 */

#include "shell.h"

#ifdef WINDOWS
/* External declarations for GUI-related variables defined in shell_entry.c */
extern BOOL g_GuiMode;
extern HWND g_hStatusBar;
#include <commctrl.h>                /* For status bar constants like SB_SETTEXT */
/* Link the common controls library - only used in MSVC, not in MinGW */
#ifdef _MSC_VER
#pragma comment(lib, "comctl32.lib")
#endif
#endif

/* Arabic keyboard layout modes */
#define KEYBOARD_MODE_EN 0 /* English (Latin) keyboard */
#define KEYBOARD_MODE_AR 1 /* Arabic keyboard */

/* Current keyboard layout mode */
static int current_keyboard_mode = KEYBOARD_MODE_EN;

/* Visual indicator states */
static int input_mode_indicator_visible = 0;

/**
 * Arabic character mapping for standard QWERTY keyboard
 * Maps Latin keys to their Arabic equivalents
 */
typedef struct
{
    char latin_key;    /* Key on Latin keyboard */
    char *arabic_char; /* Corresponding Arabic character (UTF-8) */
} KeyMapping;

/* Arabic keyboard mapping table */
static const KeyMapping ar_key_map[] = {
    {'q', "ض"}, {'w', "ص"}, {'e', "ث"}, {'r', "ق"}, {'t', "ف"}, {'y', "غ"}, {'u', "ع"}, {'i', "ه"}, {'o', "خ"}, {'p', "ح"}, {'[', "ج"}, {']', "د"}, {'a', "ش"}, {'s', "س"}, {'d', "ي"}, {'f', "ب"}, {'g', "ل"}, {'h', "ا"}, {'j', "ت"}, {'k', "ن"}, {'l', "م"}, {';', "ك"}, {'\'', "ط"}, {'z', "ئ"}, {'x', "ء"}, {'c', "ؤ"}, {'v', "ر"}, {'b', "لا"}, {'n', "ى"}, {'m', "ة"}, {',', "و"}, {'.', "ز"}, {'/', "ظ"}, {'`', "ذ"}, {'1', "١"}, {'2', "٢"}, {'3', "٣"}, {'4', "٤"}, {'5', "٥"}, {'6', "٦"}, {'7', "٧"}, {'8', "٨"}, {'9', "٩"}, {'0', "٠"}, {'-', "-"}, {'=', "="}};

/* Size of the key mapping table */
static const int ar_key_map_size = sizeof(ar_key_map) / sizeof(KeyMapping);

/**
 * set_keyboard_mode - Sets the current keyboard layout mode
 * @mode: Keyboard mode (0 for English, 1 for Arabic)
 *
 * Return: 0 on success, -1 on failure
 */
int set_keyboard_mode(int mode)
{
    if (mode != KEYBOARD_MODE_EN && mode != KEYBOARD_MODE_AR)
        return -1;

    current_keyboard_mode = mode;

    /* Update UI indicator */
    update_input_mode_indicator();

    return 0;
}

/**
 * get_keyboard_mode - Gets the current keyboard layout mode
 *
 * Return: Current keyboard mode
 */
int get_keyboard_mode(void)
{
    return current_keyboard_mode;
}

/**
 * toggle_keyboard_mode - Toggles between English and Arabic keyboard layouts
 *
 * Return: New keyboard mode
 */
int toggle_keyboard_mode(void)
{
    current_keyboard_mode = (current_keyboard_mode == KEYBOARD_MODE_EN) ? KEYBOARD_MODE_AR : KEYBOARD_MODE_EN;

    /* Update UI indicator */
    update_input_mode_indicator();

    return current_keyboard_mode;
}

/**
 * map_key_to_arabic - Maps a Latin keyboard key to its Arabic equivalent
 * @key: Latin keyboard key
 *
 * Return: Pointer to UTF-8 string containing the Arabic character, or NULL if no mapping
 */
char *map_key_to_arabic(char key)
{
    int i;

    for (i = 0; i < ar_key_map_size; i++)
    {
        if (ar_key_map[i].latin_key == key)
            return ar_key_map[i].arabic_char;
    }

    return NULL; /* No mapping found */
}

/**
 * process_keyboard_input - Processes keyboard input based on current mode
 * @key: Input key character
 *
 * Return: Processed character string (must be freed by caller) or NULL
 */
char *process_keyboard_input(char key)
{
    char *result = NULL;

    if (current_keyboard_mode == KEYBOARD_MODE_AR)
    {
        /* Arabic mode - map the key */
        char *arabic_char = map_key_to_arabic(key);
        if (arabic_char)
        {
            result = strdup(arabic_char);
        }
        else
        {
            /* If no mapping exists, use the original key */
            result = malloc(2);
            if (result)
            {
                result[0] = key;
                result[1] = '\0';
            }
        }
    }
    else
    {
        /* English mode - use the key as is */
        result = malloc(2);
        if (result)
        {
            result[0] = key;
            result[1] = '\0';
        }
    }

    return result;
}

/**
 * update_input_mode_indicator - Updates the UI indicator for input mode
 *
 * Return: 0 on success, -1 on failure
 */
int update_input_mode_indicator(void)
{
#ifdef WINDOWS
    if (g_GuiMode && g_hStatusBar)
    {
        /* Update status bar with current input mode */
        const char *mode_text = (current_keyboard_mode == KEYBOARD_MODE_AR) ? "Arabic Input" : "English Input";

        /* Convert to wide string for Windows API */
        int needed = MultiByteToWideChar(CP_UTF8, 0, mode_text, -1, NULL, 0);
        if (needed > 0)
        {
            WCHAR *wide_text = (WCHAR *)malloc(needed * sizeof(WCHAR));
            if (wide_text)
            {
                MultiByteToWideChar(CP_UTF8, 0, mode_text, -1, wide_text, needed);
                SendMessage(g_hStatusBar, SB_SETTEXT, 1, (LPARAM)wide_text);
                free(wide_text);
                return 0;
            }
        }
        return -1;
    }
#endif

    /* For non-GUI mode or non-Windows platforms */
    if (!input_mode_indicator_visible)
    {
        /* Print mode indicator at first toggle */
        _puts("\nKeyboard mode: ");
        _puts((current_keyboard_mode == KEYBOARD_MODE_AR) ? "Arabic" : "English");
        _puts("\n");
        input_mode_indicator_visible = 1;
    }
    else
    {
        /* Update existing indicator */
        _puts("\rKeyboard mode: ");
        _puts((current_keyboard_mode == KEYBOARD_MODE_AR) ? "Arabic" : "English");
        _putchar(BUF_FLUSH);
    }

    return 0;
}

/**
 * handle_keyboard_shortcut - Processes keyboard shortcuts for input mode switching
 * @info: Shell info structure
 * @key: Input key character
 *
 * Return: 1 if shortcut was handled, 0 otherwise
 */
int handle_keyboard_shortcut(info_t *info, char key)
{
    /* Suppress unused parameter warning */
    (void)info;

    /* Ctrl+Shift+A (ASCII 1) for toggling Arabic/English input */
    if (key == 1)
    { /* Ctrl+A */
        toggle_keyboard_mode();
        return 1;
    }

    /* Add more shortcuts as needed */

    return 0; /* Not a recognized shortcut */
}

/**
 * init_arabic_input - Initializes Arabic input support
 *
 * Return: 0 on success, -1 on failure
 */
int init_arabic_input(void)
{
    /* Initialize with system language if it's Arabic */
    if (get_language() == 1)
    { /* LANG_AR from locale.c */
        set_keyboard_mode(KEYBOARD_MODE_AR);
    }
    else
    {
        set_keyboard_mode(KEYBOARD_MODE_EN);
    }

    return 0;
}

/**
 * _mylayout - Shell builtin command to change keyboard layout
 * @info: Shell info structure
 *
 * Return: 0 on success, 1 on error
 */
int _mylayout(info_t *info)
{
    if (info->argv[1])
    {
        /* Command argument provided */
        if (_strcmp(info->argv[1], "ar") == 0 ||
            _strcmp(info->argv[1], "arabic") == 0)
        {
            set_keyboard_mode(KEYBOARD_MODE_AR);
            _puts("Keyboard layout set to Arabic\n");
            return 0;
        }
        else if (_strcmp(info->argv[1], "en") == 0 ||
                 _strcmp(info->argv[1], "english") == 0)
        {
            set_keyboard_mode(KEYBOARD_MODE_EN);
            _puts("Keyboard layout set to English\n");
            return 0;
        }
        else if (_strcmp(info->argv[1], "toggle") == 0)
        {
            toggle_keyboard_mode();
            _puts("Keyboard layout toggled to ");
            _puts((current_keyboard_mode == KEYBOARD_MODE_AR) ? "Arabic\n" : "English\n");
            return 0;
        }
        else
        {
            _puts("Usage: layout [ar|en|toggle]\n");
            return 1;
        }
    }
    else
    {
        /* No argument, show current layout */
        _puts("Current keyboard layout: ");
        _puts((current_keyboard_mode == KEYBOARD_MODE_AR) ? "Arabic\n" : "English\n");
        _puts("Use 'layout ar' for Arabic, 'layout en' for English, or 'layout toggle' to switch\n");
        _puts("Shortcut: Ctrl+A to toggle between layouts\n");
    }

    return 0;
}