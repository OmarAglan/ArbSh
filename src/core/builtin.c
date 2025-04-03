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
 * _myhelp - displays help information for shell built-in commands
 * @info: Structure containing potential arguments
 * Return: Always 0
 */
int _myhelp(info_t *info)
{
    char **arg_array;
    int i = 0;

    arg_array = info->argv;
    
    /* If no specific help topic, show general help */
    if (!arg_array[1])
    {
        _puts("ArbSh shell - Help\n");
        _puts("Type 'help <command>' for detailed information on a specific command.\n\n");
        
        _puts("Built-in commands:\n");
        _puts("  alias    - Define or display aliases\n");
        _puts("  cd       - Change the current directory\n");
        _puts("  clear    - Clear the terminal screen\n");
        _puts("  config   - Configure shell settings\n");
        _puts("  env      - Display environment variables\n");
        _puts("  exit     - Exit the shell\n");
        _puts("  help     - Display this help information\n");
        _puts("  history  - Display command history\n");
        _puts("  lang     - Change or display the current language (en/ar)\n");
        _puts("  layout   - Change or display the current keyboard layout\n");
        _puts("  ls       - List directory contents\n");
        _puts("  pwd      - Print working directory\n");
        _puts("  setenv   - Set an environment variable\n");
        _puts("  test     - Test command for debugging purposes\n");
        _puts("  unsetenv - Unset an environment variable\n");
        
        _puts("\nArabic support features:\n");
        _puts("  - Arabic text display with proper bidirectional rendering\n");
        _puts("  - Arabic keyboard input support (use 'layout' command)\n");
        _puts("  - Right-to-left text alignment\n");
        _puts("  - Enhanced prompt with colors and symbols\n");
        
        return (0);
    }

    /* Help for specific commands */
    if (_strcmp(arg_array[1], "alias") == 0)
    {
        _puts("alias: alias [name[=value] ...]\n");
        _puts("    Define or display aliases.\n");
        _puts("    Options:\n");
        _puts("      -s    Save aliases to file\n");
        _puts("      -l    Load aliases from file\n");
        _puts("    With no arguments, 'alias' prints the list of aliases.\n");
        _puts("    With name=value arguments, sets each name to the value.\n");
    }
    else if (_strcmp(arg_array[1], "cd") == 0)
    {
        _puts("cd: cd [directory]\n");
        _puts("    Change the current directory to the specified directory.\n");
        _puts("    If no directory is specified, change to the HOME directory.\n");
        _puts("    'cd -' changes to the previous directory.\n");
    }
    else if (_strcmp(arg_array[1], "clear") == 0)
    {
        _puts("clear: clear\n");
        _puts("    Clear the terminal screen.\n");
    }
    else if (_strcmp(arg_array[1], "config") == 0)
    {
        _puts("config: config [option] [value]\n");
        _puts("    Configure shell settings.\n");
        _puts("    Without arguments, displays current configuration.\n");
        _puts("    Options:\n");
        _puts("      history_file=PATH - Set the path to the history file\n");
        _puts("      prompt=STRING    - Set the prompt string\n");
    }
    else if (_strcmp(arg_array[1], "env") == 0)
    {
        _puts("env: env\n");
        _puts("    Display all environment variables.\n");
    }
    else if (_strcmp(arg_array[1], "exit") == 0)
    {
        _puts("exit: exit [status]\n");
        _puts("    Exit the shell with a status of N.\n");
        _puts("    If N is omitted, the exit status is that of the last command.\n");
    }
    else if (_strcmp(arg_array[1], "help") == 0)
    {
        _puts("help: help [command]\n");
        _puts("    Display information about built-in commands.\n");
        _puts("    If COMMAND is specified, gives detailed help on that command.\n");
        _puts("    Otherwise, it lists the available commands.\n");
    }
    else if (_strcmp(arg_array[1], "history") == 0)
    {
        _puts("history: history\n");
        _puts("    Display the command history list with line numbers.\n");
    }
    else if (_strcmp(arg_array[1], "lang") == 0)
    {
        _puts("lang: lang [en|ar]\n");
        _puts("    Change or display the current language.\n");
        _puts("    Without arguments, displays the current language.\n");
        _puts("    Options:\n");
        _puts("      en - Set language to English\n");
        _puts("      ar - Set language to Arabic\n");
    }
    else if (_strcmp(arg_array[1], "layout") == 0)
    {
        _puts("layout: layout [en|ar]\n");
        _puts("    Change or display the current keyboard layout.\n");
        _puts("    Without arguments, displays the current layout.\n");
        _puts("    Options:\n");
        _puts("      en - Set keyboard layout to English\n");
        _puts("      ar - Set keyboard layout to Arabic\n");
    }
    else if (_strcmp(arg_array[1], "ls") == 0)
    {
        _puts("ls: ls [-a] [-l] [directory]\n");
        _puts("    List directory contents.\n");
        _puts("    Options:\n");
        _puts("      -a    Do not hide entries starting with .\n");
        _puts("      -l    Use a long listing format\n");
    }
    else if (_strcmp(arg_array[1], "pwd") == 0)
    {
        _puts("pwd: pwd\n");
        _puts("    Print the current working directory.\n");
    }
    else if (_strcmp(arg_array[1], "setenv") == 0)
    {
        _puts("setenv: setenv VARIABLE VALUE\n");
        _puts("    Set environment variable VARIABLE to VALUE.\n");
        _puts("    If the variable exists, its value is updated.\n");
    }
    else if (_strcmp(arg_array[1], "test") == 0)
    {
        _puts("test: test [option]\n");
        _puts("    Run test commands for debugging purposes.\n");
        _puts("    Options vary depending on the current implementation.\n");
    }
    else if (_strcmp(arg_array[1], "unsetenv") == 0)
    {
        _puts("unsetenv: unsetenv VARIABLE\n");
        _puts("    Remove environment variable VARIABLE.\n");
    }
    else
    {
        _puts("No help available for ");
        _puts(arg_array[1]);
        _puts("\n");
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
