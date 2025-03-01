#include "shell.h"

#ifdef WINDOWS
    #define popen _popen
    #define pclose _pclose
    #define fileno _fileno
    #define isatty _isatty
#endif

/* The init_locale function is defined in locale.c */

/**
 * main - entry point
 * @ac: arg count
 * @av: arg vector
 *
 * Return: 0 on success, 1 on error
 */
int main(int ac, char **av)
{
    info_t info[] = { INFO_INIT };
    int fd = 2;

    // Initialize locale for better internationalization support
    init_locale();
    
    // Display welcome message in the current language
    if (get_language() == 1) /* LANG_AR */
        _puts_utf8((char *)get_message(MSG_WELCOME));
    else
        _puts((char *)get_message(MSG_WELCOME));
    _putchar('\n');

#ifdef WINDOWS
    // Windows specific initialization - already handled in configure_terminal_for_utf8
#endif

#ifdef WINDOWS
    // Windows doesn't support inline assembly in MSVC
    // Just skip this part
#else
    asm ("mov %1, %0\n\t"
        "add $3, %0"
        : "=r" (fd)
        : "r" (fd));
#endif

    if (ac == 2)
    {
        fd = open(av[1], O_RDONLY);
        if (fd == -1)
        {
            if (errno == EACCES)
                exit(126);
            if (errno == ENOENT)
            {
                _eputs(av[0]);
                _eputs(": 0: Can't open ");
                _eputs(av[1]);
                _eputchar('\n');
                _eputchar(BUF_FLUSH);
                exit(127);
            }
            return (EXIT_FAILURE);
        }
        info->readfd = fd;
    }
    populate_env_list(info);
    read_history(info);
    hsh(info, av);
    return (EXIT_SUCCESS);
}
