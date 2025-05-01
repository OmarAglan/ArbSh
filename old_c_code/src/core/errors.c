#include "shell.h"
#include "platform/console.h"

/**
 *_eputs - prints an input string to stderr using PAL
 * @str: the string to be printed
 *
 * Return: Nothing
 */
void _eputs(char *str)
{
	if (!str)
		return;
	platform_console_write(PLATFORM_STDERR_FILENO, str, _strlen(str));
}

/**
 * _eputchar - writes the character c to stderr using PAL (buffered)
 * @c: The character to print
 *
 * Return: On success 1.
 * On error, -1 is returned, and errno is set appropriately.
 */
int _eputchar(char c)
{
	static int i;
	static char buf[WRITE_BUF_SIZE];
	int written = 0;

	if (c == BUF_FLUSH || i >= WRITE_BUF_SIZE)
	{
		if (i > 0) {
			written = platform_console_write(PLATFORM_STDERR_FILENO, buf, i);
		}
		i = 0;
		return (written < 0) ? -1 : 1;
	}
	if (c != BUF_FLUSH)
		buf[i++] = c;
	return (1);
}

/**
 * _putfd - writes the character c to given fd using PAL (buffered)
 * @c: The character to print
 * @fd: The filedescriptor to write to (use platform constants)
 *
 * Return: On success 1.
 * On error, -1 is returned, and errno is set appropriately.
 */
int _putfd(char c, int fd)
{
	static int i_map[10];
	static char buf_map[10][WRITE_BUF_SIZE];
	int *i_ptr;
	char *buf_ptr;
	int written = 0;

	if (fd < 0 || fd >= 10) return -1;
	i_ptr = &i_map[fd];
	buf_ptr = buf_map[fd];

	if (c == BUF_FLUSH || *i_ptr >= WRITE_BUF_SIZE)
	{
		if (*i_ptr > 0) {
			written = platform_console_write(fd, buf_ptr, *i_ptr);
		}
		*i_ptr = 0;
		return (written < 0) ? -1 : 1;
	}
	if (c != BUF_FLUSH)
		buf_ptr[(*i_ptr)++] = c;
	return (1);
}

/**
 *_putsfd - prints an input string to a given fd using PAL
 * @str: the string to be printed
 * @fd: the filedescriptor to write to (use platform constants)
 *
 * Return: the number of chars put or -1 on error
 */
int _putsfd(char *str, int fd)
{
	if (!str)
		return (0);
	int written = platform_console_write(fd, str, _strlen(str));
	_putfd(BUF_FLUSH, fd);
	return written;
}
