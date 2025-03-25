#include "shell.h"

/**
 * _myexit - exits the shell
 * @info: Structure containing potential arguments. Used to maintain
 *          constant function prototype.
 *  Return: exits with a given exit status
 *         (0) if info.argv[0] != "exit"
 */
int _myexit(info_t *info)
{
    int exitcheck;

    if (info->argv[1]) /* If there is an exit arguement */
    {
        exitcheck = _erratoi(info->argv[1]);
        if (exitcheck == -1)
        {
            info->status = 2;
            print_error(info, "Illegal number: ");
            _eputs(info->argv[1]);
            _eputchar('\n');
            return (1);
        }
        info->err_num = _erratoi(info->argv[1]);
        return (-2);
    }
    info->err_num = -1;
    return (-2);
}

/**
 * _mycd - changes the current directory of the process
 * @info: Structure containing potential arguments. Used to maintain
 *          constant function prototype.
 *  Return: Always 0
 */
int _mycd(info_t *info)
{
    char *s, *dir, buffer[1024];
    int chdir_ret;

    s = _getcwd(buffer, 1024);
    if (!s)
        _puts("Error: Could not get current directory\n");
    if (!info->argv[1])
    {
        dir = _getenv(info, "HOME=");
        if (!dir)
            chdir_ret = chdir((dir = _getenv(info, "PWD=")) ? dir : "/");
        else
            chdir_ret = chdir(dir);
    }
    else if (_strcmp(info->argv[1], "-") == 0)
    {
        if (!_getenv(info, "OLDPWD="))
        {
            _puts(s);
            _putchar('\n');
            return (1);
        }
        _puts(_getenv(info, "OLDPWD=")), _putchar('\n');
        chdir_ret = chdir((dir = _getenv(info, "OLDPWD=")) ? dir : "/");
    }
    else
        chdir_ret = chdir(info->argv[1]);
    if (chdir_ret == -1)
    {
        print_error(info, "can't cd to ");
        _eputs(info->argv[1]), _eputchar('\n');
    }
    else
    {
        _setenv(info, "OLDPWD", _getenv(info, "PWD="));
        _setenv(info, "PWD", _getcwd(buffer, 1024));
    }
    return (0);
}

/**
 * _myhelp - displays help information
 * @info: Structure containing potential arguments. Used to maintain
 *          constant function prototype.
 *  Return: Always 0
 */
int _myhelp(info_t *info)
{
    char **arg_array;

    arg_array = info->argv;
    if (info->argc == 1)
    {
        _puts("Help menu - type 'help' followed by a command for more info.\n");
        _puts("  cd       - Change directory\n");
        _puts("  exit     - Exit the shell\n");
        _puts("  env      - Show environment variables\n");
        _puts("  setenv   - Set environment variable\n");
        _puts("  unsetenv - Remove environment variable\n");
        _puts("  help     - Display help information\n");
        _puts("  history  - Show command history\n");
        _puts("  alias    - Manage command aliases\n");
        _puts("  lang     - Change shell language\n");
        _puts("  test     - Test UTF-8 and Arabic support\n");
        return (0);
    }
    if (_strcmp(arg_array[1], "cd") == 0)
    {
        _puts("cd: cd [DIRECTORY]\n");
        _puts("    Change the current directory to DIRECTORY.\n");
        _puts("    If no argument is given, changes to HOME directory.\n");
        _puts("    'cd -' changes to the previous directory.\n");
    }
    else if (_strcmp(arg_array[1], "exit") == 0)
    {
        _puts("exit: exit [STATUS]\n");
        _puts("    Exit the shell with a status of STATUS.\n");
        _puts("    If no status is given, the exit status is that of the last command.\n");
    }
    else if (_strcmp(arg_array[1], "env") == 0)
    {
        _puts("env: env\n");
        _puts("    Print the current environment variables.\n");
    }
    else if (_strcmp(arg_array[1], "setenv") == 0)
    {
        _puts("setenv: setenv VARIABLE VALUE\n");
        _puts("    Set environment variable VARIABLE to VALUE.\n");
        _puts("    If the variable exists, the VALUE will be updated.\n");
    }
    else if (_strcmp(arg_array[1], "unsetenv") == 0)
    {
        _puts("unsetenv: unsetenv VARIABLE\n");
        _puts("    Remove environment variable VARIABLE.\n");
    }
    else if (_strcmp(arg_array[1], "help") == 0)
    {
        _puts("help: help [COMMAND]\n");
        _puts("    Display information about builtin commands.\n");
        _puts("    If COMMAND is specified, gives detailed help on that command.\n");
        _puts("    Otherwise, a list of commands is displayed.\n");
    }
    else if (_strcmp(arg_array[1], "history") == 0)
    {
        _puts("history: history\n");
        _puts("    Display the command history list with line numbers.\n");
    }
    else if (_strcmp(arg_array[1], "alias") == 0)
    {
        _puts("alias: alias [name[='value'] ...]\n");
        _puts("    Define or display aliases.\n");
        _puts("    Without arguments, prints a list of all aliases.\n");
        _puts("    Otherwise, defines an alias for each name with an assigned value.\n");
        _puts("    Example: alias ls='ls -la'\n");
    }
    else if (_strcmp(arg_array[1], "lang") == 0)
    {
        _puts("lang: lang [LANGUAGE]\n");
        _puts("    Change the shell language or display current language.\n");
        _puts("    Without arguments, displays the current language.\n");
        _puts("    Supported languages: en (English), ar (Arabic)\n");
        _puts("    Example: lang ar\n");
    }
    else if (_strcmp(arg_array[1], "test") == 0)
    {
        _puts("test: test\n");
        _puts("    Test UTF-8 and Arabic support in the shell.\n");
        _puts("    Displays various test patterns including ASCII, UTF-8,\n");
        _puts("    Arabic text, mixed text direction, and Arabic numbers.\n");
    }
    else if (_strcmp(arg_array[1], "layout") == 0)
    {
        _puts("layout: layout [OPTION]\n");
        _puts("    Change keyboard layout or display current layout.\n");
        _puts("    Without arguments, displays the current keyboard layout.\n");
        _puts("    Options:\n");
        _puts("      ar, arabic   - Switch to Arabic keyboard layout\n");
        _puts("      en, english  - Switch to English keyboard layout\n");
        _puts("      toggle       - Toggle between Arabic and English layouts\n");
        _puts("    Shortcut: Ctrl+A to toggle between layouts\n");
    }
    else
    {
        _puts("No help available for this command.\n");
    }
    return (0);
}

/**
 * _mylang - Implements the 'lang' shell command to change interface language
 * @info: Shell info structure
 *
 * Return: 0 on success, 1 on error
 */
int _mylang(info_t *info)
{
    if (info->argv[1])
    {
        /* Command argument provided */
        if (_strcmp(info->argv[1], "ar") == 0 || 
            _strcmp(info->argv[1], "arabic") == 0)
        {
            set_language(1); /* LANG_AR */
            _puts("Language set to Arabic\n");
            return 0;
        }
        else if (_strcmp(info->argv[1], "en") == 0 || 
                 _strcmp(info->argv[1], "english") == 0)
        {
            set_language(0); /* LANG_EN */
            _puts("Language set to English\n");
            return 0;
        }
        else if (_strcmp(info->argv[1], "toggle") == 0)
        {
            /* Toggle between languages */
            if (get_language() == 0) /* LANG_EN */
            {
                set_language(1); /* LANG_AR */
                _puts("Language set to Arabic\n");
            }
            else
            {
                set_language(0); /* LANG_EN */
                _puts("Language set to English\n");
            }
            
            /* Also toggle keyboard layout since they typically go together */
            toggle_arabic_mode();
            return 0;
        }
        else
        {
            _puts("Usage: lang [ar|en|toggle]\n");
            return 1;
        }
    }
    else
    {
        /* No argument, show current language */
        _puts("Current language: ");
        _puts((get_language() == 1) ? "Arabic\n" : "English\n");
        _puts("Use 'lang ar' for Arabic, 'lang en' for English, or 'lang toggle' to switch\n");
    }
    
    return 0;
}

/**
 * _mytest - tests UTF-8 and Arabic support
 * @info: Structure containing potential arguments. Used to maintain
 *          constant function prototype.
 *  Return: Always 0
 */
int _mytest(info_t *info)
{
    (void)info; /* Unused parameter */

    /* Test ASCII characters */
    _puts("ASCII Test: Hello, World!\n");

    /* Test UTF-8 characters */
    _puts_utf8("UTF-8 Test: こんにちは世界! Привет, мир! 你好，世界！\n");

    /* Test Arabic characters */
    _puts_utf8("Arabic Test: مرحبا بالعالم!\n");

    /* Test mixed text direction */
    _puts_utf8("Mixed Test: Hello مرحبا World العالم!\n");

    /* Test Arabic numbers */
    _puts_utf8("Arabic Numbers: ٠١٢٣٤٥٦٧٨٩\n");

    /* Test Arabic punctuation */
    _puts_utf8("Arabic Punctuation: ؟ ، ؛ « »\n");

    /* Test text direction markers */
    _puts("Text Direction Test:\n");

    /* Force LTR */
    write(STDOUT_FILENO, "\xE2\x80\x8E", 3); /* LTR mark (U+200E) */
    _puts_utf8("LTR: Hello مرحبا بالعالم World!\n");

    /* Force RTL */
    write(STDOUT_FILENO, "\xE2\x80\x8F", 3); /* RTL mark (U+200F) */
    _puts_utf8("RTL: Hello مرحبا بالعالم World!\n");

    return (0);
}
