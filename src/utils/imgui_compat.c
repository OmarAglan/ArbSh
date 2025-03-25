/**
 * imgui_compat.c - Compatibility functions for ImGui integration
 *
 * This file implements stub functions that are needed for linking with ImGui
 */

#include "shell.h"

/**
 * interactive - Check if the shell is in interactive mode
 * @info: Pointer to the info structure
 *
 * Return: 1 if interactive mode, 0 otherwise
 */
int interactive(info_t *info)
{
    (void)info;
    return 1;
}

/**
 * is_delim - Check if a character is a delimiter
 * @c: The character to check
 * @delim: The delimiter string
 *
 * Return: 1 if true, 0 if false
 */
int is_delim(char c, char *delim)
{
    while (*delim)
        if (*delim++ == c)
            return 1;
    return 0;
} 