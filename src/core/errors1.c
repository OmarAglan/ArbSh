#include "shell.h"
#include "platform/console.h"

/**
 * _erratoi - converts a string to an integer
 * @s: the string to be converted
 * Return: 0 if no numbers in string, converted number otherwise
 *       -1 on error
 */
int _erratoi(char *s)
{
	int i = 0;
	unsigned long int result = 0;

	if (*s == '+')
		s++;  /* Skip the plus sign without affecting the return value */
	for (i = 0;  s[i] != '\0'; i++)
	{
		if (s[i] >= '0' && s[i] <= '9')
		{
			result *= 10;
			result += (s[i] - '0');
			if (result > INT_MAX)
				return (-1);
		}
		else
			return (-1);
	}
	return (result);
}

/**
 * print_error - prints an error message to stderr using PAL
 * @info: the parameter & return info struct
 * @estr: string containing specified error type
 * Return: void
 */
void print_error(info_t *info, char *estr)
{
	_eputs(info->fname);
	_eputs(": ");
	print_d(info->line_count, PLATFORM_STDERR_FILENO);
	_eputs(": ");
	_eputs(info->argv[0]);
	_eputs(": ");
	_eputs(estr);
}

/**
 * print_d - function prints a decimal (integer) number (base 10) to a given fd
 * @input: the input number
 * @fd: the filedescriptor to write to (use platform constants)
 * Return: number of characters printed
 */
int print_d(int input, int fd)
{
	int (*output_char)(char, int) = _putfd;
	int i, count = 0;
	unsigned int _abs_, current;
	char buffer[20];
	int buf_idx = 0;

	if (input < 0)
	{
		_abs_ = -input;
		output_char('-', fd);
		count++;
	}
	else
		_abs_ = input;

	current = _abs_;
	if (current == 0) {
		buffer[buf_idx++] = '0';
	} else {
		while (current > 0) {
			buffer[buf_idx++] = '0' + (current % 10);
			current /= 10;
		}
	}

	for (i = buf_idx - 1; i >= 0; i--)
	{
		output_char(buffer[i], fd);
		count++;
	}

	output_char(BUF_FLUSH, fd);

	return (count);
}

/**
 * convert_number - converter function, a clone of itoa
 * @num: number
 * @base: base
 * @flags: argument flags
 *
 * Return: string
 */
char *convert_number(long int num, int base, int flags)
{
	static char *array;
	static char buffer[50];
	char sign = 0;
	char *ptr;
	unsigned long n = num;

	if (!(flags & CONVERT_UNSIGNED) && num < 0)
	{
		n = -num;
		sign = '-';

	}
	array = flags & CONVERT_LOWERCASE ? "0123456789abcdef" : "0123456789ABCDEF";
	ptr = &buffer[49];
	*ptr = '\0';

	do	{
		*--ptr = array[n % base];
		n /= base;
	} while (n != 0);

	if (sign)
		*--ptr = sign;
	return (ptr);
}

/**
 * remove_comments - function replaces first instance of '#' with '\0'
 * @buf: address of the string to modify
 *
 * Return: Always 0;
 */
void remove_comments(char *buf)
{
	int i;

	for (i = 0; buf[i] != '\0'; i++)
		if (buf[i] == '#' && (!i || buf[i - 1] == ' '))
		{
			buf[i] = '\0';
			break;
		}
}
