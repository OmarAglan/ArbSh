/**
 * arabic_input.c - Arabic input support for the shell
 *
 * This file provides support for Arabic input in the shell, including
 * bidirectional text rendering, keyboard input handling, and text output.
 * It has been updated to work with both console and ImGui modes.
 */

#include "shell.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
/* Include bidi from the shell.h common includes instead */

#ifdef WINDOWS
#include <windows.h>
#endif

/* Global variables for Arabic support */
int g_arabic_mode = 0;  /* 0 = disabled, 1 = enabled */
int g_keyboard_layout = 0;  /* 0 = EN, 1 = AR */

/**
 * init_arabic_input - Initialize Arabic input support
 */
void init_arabic_input(void)
{
    /* Set default Arabic mode based on locale */
    g_arabic_mode = 0;  /* Disabled by default */
    g_keyboard_layout = 0;  /* English keyboard by default */
    
    /* Initialize BiDi support - this function is defined in bidi.c */
    init_bidi();
    
#ifdef WINDOWS
    /* Get current keyboard layout */
    HKL hkl = GetKeyboardLayout(0);
    WORD language_id = LOWORD(hkl);
    
    /* Check if Arabic keyboard layout is active */
    if (language_id == 0x0401 || language_id == 0x0801 || language_id == 0x0C01 ||
        language_id == 0x1001 || language_id == 0x1401 || language_id == 0x1801 ||
        language_id == 0x1C01 || language_id == 0x2001 || language_id == 0x2401 ||
        language_id == 0x2801 || language_id == 0x2C01 || language_id == 0x3001 ||
        language_id == 0x3401 || language_id == 0x3801 || language_id == 0x3C01 ||
        language_id == 0x4001)
    {
        g_keyboard_layout = 1;  /* Arabic keyboard */
        g_arabic_mode = 1;      /* Enable Arabic mode */
    }
#endif
}

/**
 * toggle_arabic_mode - Toggle Arabic input mode
 *
 * Return: 1 if Arabic mode is enabled, 0 if disabled
 */
int toggle_arabic_mode(void)
{
    g_arabic_mode = !g_arabic_mode;
    
#ifdef WINDOWS
    /* Update status bar if in GUI mode */
    if (g_arabic_mode)
    {
        printf("Arabic mode enabled\n");
    }
    else
    {
        printf("Arabic mode disabled\n");
    }
#endif
    
    return g_arabic_mode;
}

/**
 * is_arabic_mode - Check if Arabic mode is enabled
 *
 * Return: 1 if Arabic mode is enabled, 0 if disabled
 */
int is_arabic_mode(void)
{
    return g_arabic_mode;
}

/**
 * set_keyboard_layout - Set the keyboard layout
 * @layout: Keyboard layout (0 = EN, 1 = AR)
 *
 * Return: 1 if successful, 0 if failed
 */
int set_keyboard_layout(int layout)
{
    if (layout < 0 || layout > 1)
        return 0;
    
    g_keyboard_layout = layout;
    
#ifdef WINDOWS
    /* Update status bar if in GUI mode */
    if (g_keyboard_layout == 1)
    {
        printf("Arabic keyboard layout\n");
    }
    else
    {
        printf("English keyboard layout\n");
    }
#endif
    
    return 1;
}

/**
 * get_keyboard_layout - Get the current keyboard layout
 *
 * Return: 0 for EN, 1 for AR
 */
int get_keyboard_layout(void)
{
    return g_keyboard_layout;
}

/**
 * _mylayout - Changes keyboard layout
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
            set_keyboard_layout(1); /* Arabic layout */
            toggle_arabic_mode();
            _puts("Keyboard layout set to Arabic\n");
            return 0;
        }
        else if (_strcmp(info->argv[1], "en") == 0 || 
                 _strcmp(info->argv[1], "english") == 0)
        {
            set_keyboard_layout(0); /* English layout */
            if (is_arabic_mode())
                toggle_arabic_mode();
            _puts("Keyboard layout set to English\n");
            return 0;
        }
        else if (_strcmp(info->argv[1], "toggle") == 0)
        {
            toggle_arabic_mode();
            set_keyboard_layout(is_arabic_mode() ? 1 : 0);
            _puts("Keyboard layout toggled to ");
            _puts(is_arabic_mode() ? "Arabic\n" : "English\n");
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
        _puts(is_arabic_mode() ? "Arabic\n" : "English\n");
        _puts("Use 'layout ar' for Arabic, 'layout en' for English, or 'layout toggle' to switch\n");
    }
    
    return 0;
}

/* Add other Arabic-related functions here */