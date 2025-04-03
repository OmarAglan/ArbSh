#include "shell.h"

/**
 **_strncpy - copies a string
 *@dest: the destination string to be copied to
 *@src: the source string (read-only)
 *@n: the amount of characters to be copied
 *Return: the concatenated string
 */
char *_strncpy(char *dest, const char *src, size_t n)
{
	size_t i = 0;
	char *s = dest;

	while (src[i] != '\0' && i < n)
	{
		dest[i] = src[i];
		i++;
	}
	// If n is greater than the length of src, pad with null bytes
	while (i < n)
	{
		dest[i] = '\0';
		i++;
	}
	return (s);
}

/**
 **_strncat - concatenates two strings
 *@dest: the first string
 *@src: the second string (read-only)
 *@n: the maximum number of bytes to be used from src
 *Return: the concatenated string
 */
char *_strncat(char *dest, const char *src, size_t n)
{
	size_t i = 0, j = 0;
	char *s = dest;

	while (dest[i] != '\0')
		i++;
	while (src[j] != '\0' && j < n)
	{
		dest[i] = src[j];
		i++;
		j++;
	}
	if (j < n) // Only null-terminate if we didn't reach the limit n
		dest[i] = '\0';
	return (s);
}

/**
 **_strchr - locates a character in a string
 *@s: the string to be parsed (read-only)
 *@c: the character to look for
 *Return: (s) a pointer to the memory area s, or NULL if not found
 */
char *_strchr(const char *s, int c)
{
	do
	{
		if (*s == (char)c)
			return ((char *)s); // Cast away const, standard C behavior
	} while (*s++ != '\0');

	return (NULL);
}
