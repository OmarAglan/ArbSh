#include "shell.h"

/**
 * Color codes for terminal output
 */
#define COLOR_RESET   "\033[0m"
#define COLOR_COMMAND "\033[1;32m"  /* Bright green for commands */
#define COLOR_BUILTIN "\033[1;36m"  /* Cyan for builtin commands */
#define COLOR_ARG     "\033[1;37m"  /* White for arguments */
#define COLOR_VAR     "\033[1;33m"  /* Yellow for variables */
#define COLOR_PATH    "\033[1;34m"  /* Blue for paths */
#define COLOR_QUOTE   "\033[1;35m"  /* Magenta for quoted text */
#define COLOR_ERROR   "\033[1;31m"  /* Red for errors */

/**
 * is_builtin - check if a command is a shell builtin
 * @cmd: the command to check
 *
 * Return: 1 if builtin, 0 otherwise
 */
int is_builtin(const char *cmd)
{
    static const char *builtins[] = {
        "exit", "env", "help", "history", "setenv",
        "unsetenv", "cd", "alias", "lang", "test",
        "layout", "config", NULL
    };
    
    int i;
    
    if (!cmd)
        return 0;
    
    for (i = 0; builtins[i]; i++)
    {
        if (_strcmp((char *)cmd, (char *)builtins[i]) == 0)
            return 1;
    }
    
    return 0;
}

/**
 * is_command - check if a string is a valid command in PATH
 * @cmd: the command to check
 *
 * Return: 1 if command exists, 0 otherwise
 */
int is_command(const char *cmd)
{
    struct stat st;
    char *path, *pathcpy, *token, *fullpath;
    const char *pathvar;
    
    if (!cmd || *cmd == '\0')
        return 0;
    
    /* Check if it's an absolute path */
    if (cmd[0] == '/' || cmd[0] == '\\' || 
        (cmd[1] == ':' && (cmd[2] == '/' || cmd[2] == '\\')))
    {
        if (stat((char *)cmd, &st) == 0 && (st.st_mode & S_IXUSR))
            return 1;
        return 0;
    }
    
    /* Get PATH environment variable */
    pathvar = getenv("PATH");
    if (!pathvar)
        return 0;
    
    /* Make a copy of PATH to tokenize */
    pathcpy = _strdup((char *)pathvar);
    if (!pathcpy)
        return 0;
    
    token = strtok(pathcpy, ":");
    while (token)
    {
        /* Build full path to the command */
        fullpath = malloc(_strlen(token) + _strlen((char *)cmd) + 2);
        if (!fullpath)
        {
            free(pathcpy);
            return 0;
        }
        
        _strcpy(fullpath, token);
        _strcat(fullpath, "/");
        _strcat(fullpath, (char *)cmd);
        
        /* Check if command exists and is executable */
        if (stat(fullpath, &st) == 0 && (st.st_mode & S_IXUSR))
        {
            free(fullpath);
            free(pathcpy);
            return 1;
        }
        
        free(fullpath);
        token = strtok(NULL, ":");
    }
    
    free(pathcpy);
    return 0;
}

/**
 * highlight_command - Highlight different parts of a command with color codes
 * @input: the command string to highlight
 *
 * Return: dynamically allocated highlighted string
 */
char *highlight_command(const char *input)
{
    char *highlighted, *token, *saveptr;
    char *inputcpy;
    size_t len, total_len = 0;
    int is_first_token = 1;
    int in_quote = 0;
    char quote_char = 0;
    
    if (!input || !*input)
        return NULL;
    
    /* Estimate size for highlighted string (4x for color codes) */
    len = _strlen((char *)input);
    highlighted = malloc(len * 4 + 20);
    if (!highlighted)
        return NULL;
    
    *highlighted = '\0';
    
    /* Make a copy to tokenize */
    inputcpy = _strdup((char *)input);
    if (!inputcpy)
    {
        free(highlighted);
        return NULL;
    }
    
    /* First pass: simple tokenization by space to identify command/builtin */
    token = strtok_r(inputcpy, " \t", &saveptr);
    
    while (token)
    {
        if (is_first_token)
        {
            /* First token is the command */
            if (is_builtin(token))
            {
                _strcat(highlighted, COLOR_BUILTIN);
                _strcat(highlighted, token);
                _strcat(highlighted, COLOR_RESET);
            }
            else if (is_command(token))
            {
                _strcat(highlighted, COLOR_COMMAND);
                _strcat(highlighted, token);
                _strcat(highlighted, COLOR_RESET);
            }
            else
            {
                /* Command not found */
                _strcat(highlighted, COLOR_ERROR);
                _strcat(highlighted, token);
                _strcat(highlighted, COLOR_RESET);
            }
            is_first_token = 0;
        }
        else
        {
            /* Arguments */
            if (token[0] == '-')
            {
                /* Option argument */
                _strcat(highlighted, COLOR_ARG);
                _strcat(highlighted, token);
                _strcat(highlighted, COLOR_RESET);
            }
            else if (token[0] == '$')
            {
                /* Variable */
                _strcat(highlighted, COLOR_VAR);
                _strcat(highlighted, token);
                _strcat(highlighted, COLOR_RESET);
            }
            else if (token[0] == '/' || token[0] == '\\' || 
                    (token[1] == ':' && (token[2] == '/' || token[2] == '\\')))
            {
                /* Path */
                _strcat(highlighted, COLOR_PATH);
                _strcat(highlighted, token);
                _strcat(highlighted, COLOR_RESET);
            }
            else if (token[0] == '\'' || token[0] == '"')
            {
                /* Quoted string */
                _strcat(highlighted, COLOR_QUOTE);
                _strcat(highlighted, token);
                _strcat(highlighted, COLOR_RESET);
            }
            else
            {
                /* Regular argument */
                _strcat(highlighted, COLOR_ARG);
                _strcat(highlighted, token);
                _strcat(highlighted, COLOR_RESET);
            }
        }
        
        /* Add space between tokens */
        _strcat(highlighted, " ");
        
        token = strtok_r(NULL, " \t", &saveptr);
    }
    
    free(inputcpy);
    return highlighted;
}

/**
 * print_highlighted_input - Print a command string with syntax highlighting
 * @input: the input string to highlight and print
 *
 * Return: void
 */
void print_highlighted_input(char *input)
{
    char *highlighted = highlight_command(input);
    
    if (highlighted)
    {
        _puts(highlighted);
        free(highlighted);
    }
    else
    {
        /* Fall back to normal output if highlighting fails */
        _puts(input);
    }
}

/**
 * get_highlighted_prompt - Get a highlighted prompt with status info
 * @info: shell info struct
 *
 * Return: dynamically allocated highlighted prompt string
 */
char *get_highlighted_prompt(info_t *info)
{
    char *prompt = malloc(256);
    char cwd[256] = {0};
    char *username = NULL;
    
    if (!prompt)
        return NULL;
    
    /* Get current working directory */
    if (getcwd(cwd, sizeof(cwd)) == NULL)
        _strcpy(cwd, "?");
    
    /* Get username from environment */
    username = _getenv(info, "USER=");
    if (!username)
        username = _getenv(info, "USERNAME=");
    if (!username)
        username = "user";
    
    /* Create a highlighted prompt with user, directory and status */
    _snprintf(prompt, 256, "%s%s%s@%s%s%s:%s%s%s$ ", 
             info->status == 0 ? "\033[1;32m" : "\033[1;31m", 
             username, 
             "\033[0m",
             "\033[1;34m", 
             cwd, 
             "\033[0m",
             info->status == 0 ? "\033[1;32m" : "\033[1;31m",
             info->status == 0 ? "✓" : "✗",
             "\033[0m");
    
    return prompt;
} 