#include "shell.h"
#include <errno.h>

#ifdef WINDOWS
#include <shlobj.h> // For SHGetFolderPathW
#include <wchar.h>
#include <stdlib.h>
#else
#include <unistd.h> // For getuid, getpwuid
#include <sys/types.h>
#include <pwd.h>    // For getpwuid
#endif

// --- Configuration Defaults ---
#define DEFAULT_LANGUAGE LANG_EN
#define DEFAULT_LAYOUT 0 // 0 = EN, 1 = AR
#define DEFAULT_HISTORY_FILE HIST_FILE // From shell.h

// Structure to hold loaded config (can be integrated into info_t later)
// For now, we'll directly modify info_t or global settings.

/**
 * trim_whitespace - Removes leading/trailing whitespace from a string in-place.
 * @str: The string to trim.
 * Return: Pointer to the trimmed string (same as input).
 */
char *trim_whitespace(char *str)
{
    char *end;

    // Trim leading space
    while (isspace((unsigned char)*str)) str++;

    if (*str == 0) // All spaces?
        return str;

    // Trim trailing space
    end = str + strlen(str) - 1;
    while (end > str && isspace((unsigned char)*end)) end--;

    // Write new null terminator
    *(end + 1) = '\0';

    return str;
}

/**
 * parse_config_line - Parses a single "key = value" line.
 * @line: The line string.
 * @key: Pointer to store the allocated key.
 * @value: Pointer to store the allocated value.
 * Return: 0 on success, -1 on failure/comment/empty.
 */
int parse_config_line(char *line, char **key, char **value)
{
    char *trimmed_line;
    char *separator;

    *key = NULL;
    *value = NULL;

    trimmed_line = trim_whitespace(line);

    // Skip empty lines and comments
    if (trimmed_line[0] == '\0' || trimmed_line[0] == '#' || trimmed_line[0] == ';')
    {
        return -1;
    }

    separator = strchr(trimmed_line, '=');
    if (!separator)
    {
        fprintf(stderr, "Warning: Invalid config line (missing '='): %s\n", trimmed_line);
        return -1; // No '=' found
    }

    // Split the line
    *separator = '\0'; // Null-terminate the key part

    *key = shell_strdup(trim_whitespace(trimmed_line));
    *value = shell_strdup(trim_whitespace(separator + 1));

    if (!*key || !*value)
    {
        fprintf(stderr, "Error: Memory allocation failed while parsing config.\n");
        free(*key);
        free(*value);
        *key = NULL;
        *value = NULL;
        return -1;
    }

    return 0;
}

/**
 * ensure_config_dir_exists - Ensures the configuration directory exists
 * @path: Path to the configuration file
 * Return: 1 on success, 0 on failure
 */
int ensure_config_dir_exists(const char *path)
{
    char dir_path[PATH_MAX];
    char *last_slash;
    
    // Copy the path to work with
    snprintf(dir_path, sizeof(dir_path), "%s", path);
    
    // Find the last directory separator
    last_slash = strrchr(dir_path, 
#ifdef WINDOWS
                         '\\'
#else
                         '/'
#endif
                         );
    
    if (!last_slash)
        return 0; // No directory part
    
    // Null-terminate at the slash to get just the directory path
    *last_slash = '\0';
    
#ifdef WINDOWS
    // Create directory if it doesn't exist (Windows)
    struct _stat dir_stat;
    if (_stat(dir_path, &dir_stat) != 0) // Directory doesn't exist
    {
        // Use _mkdir to create the directory
        if (_mkdir(dir_path) != 0)
        {
            fprintf(stderr, "Error creating directory: %s\n", dir_path);
            return 0;
        }
        printf("Created configuration directory: %s\n", dir_path);
    }
#else
    // Create directory if it doesn't exist (Unix)
    struct stat dir_stat;
    if (stat(dir_path, &dir_stat) != 0) // Directory doesn't exist
    {
        // Use mkdir to create the directory with 0755 permissions
        if (mkdir(dir_path, 0755) != 0)
        {
            fprintf(stderr, "Error creating directory: %s\n", dir_path);
            return 0;
        }
        printf("Created configuration directory: %s\n", dir_path);
    }
#endif
    
    return 1;
}

/**
 * get_config_file_path - Gets the platform-specific path for the config file.
 * @buffer: Buffer to store the path.
 * @size: Size of the buffer.
 * Return: Pointer to buffer on success, NULL on failure.
 */
char *get_config_file_path(char *buffer, size_t size)
{
#ifdef WINDOWS
    wchar_t path_w[MAX_PATH];
    char path_a[MAX_PATH];
    if (SUCCEEDED(SHGetFolderPathW(NULL, CSIDL_APPDATA, NULL, 0, path_w)))
    {
        // Convert wide char path to multi-byte
        if (WideCharToMultiByte(CP_UTF8, 0, path_w, -1, path_a, MAX_PATH, NULL, NULL) == 0) {
             fprintf(stderr, "Error converting config path to UTF-8\n");
             return NULL;
        }
        snprintf(buffer, size, "%s\\ArbSh\\config.ini", path_a);
        // Ensure the ArbSh directory exists
        ensure_config_dir_exists(buffer);
        return buffer;
    } else {
         fprintf(stderr, "Error getting APPDATA directory\n");
         return NULL;
    }
#else
    const char *home_dir = getenv("HOME");
    struct passwd *pw = NULL;

    if (!home_dir)
    {
        pw = getpwuid(getuid());
        if (pw)
        {
            home_dir = pw->pw_dir;
        }
    }

    if (home_dir)
    {
        // Prefer ~/.arbshrc for simplicity for now
        snprintf(buffer, size, "%s/.arbshrc", home_dir);
        return buffer;
        // // XDG compliant path (more complex to ensure dir exists)
        // const char *xdg_config_home = getenv("XDG_CONFIG_HOME");
        // if (xdg_config_home && xdg_config_home[0]) {
        //     snprintf(buffer, size, "%s/arbsh/config", xdg_config_home);
        // } else {
        //     snprintf(buffer, size, "%s/.config/arbsh/config", home_dir);
        // }
        // // Ensure the config directory exists
        // ensure_config_dir_exists(buffer);
        // return buffer;
    } else {
        fprintf(stderr, "Error getting HOME directory\n");
        return NULL;
    }
#endif
}


/**
 * load_configuration - Loads settings from the config file.
 * @info: Pointer to the shell info struct to update.
 */
void load_configuration(info_t *info)
{
    char config_path[PATH_MAX]; // Use PATH_MAX from limits.h
    FILE *file;
    char line[512];
    char *key = NULL;
    char *value = NULL;

    // --- Set Defaults First ---
    info->default_layout = DEFAULT_LAYOUT;
    info->history_file_path = shell_strdup(DEFAULT_HISTORY_FILE);

    // --- Get Config Path ---
    if (!get_config_file_path(config_path, sizeof(config_path)))
    {
        fprintf(stderr, "Warning: Could not determine configuration file path. Using defaults.\n");
        return;
    }

    // --- Open and Read Config File ---
    file = fopen(config_path, "r");
    if (!file)
    {
        // File doesn't exist or can't be opened - this is fine, use defaults.
        // You might want to log this only if verbose mode is on.
        // fprintf(stderr, "Info: Configuration file '%s' not found or not readable. Using defaults.\n", config_path);
        return;
    }

    printf("Loading configuration from: %s\n", config_path); // Debug message

    while (fgets(line, sizeof(line), file))
    {
        if (parse_config_line(line, &key, &value) == 0)
        {
            // --- Apply Settings ---
            if (_strcmp(key, "language") == 0)
            {
                if (_strcmp(value, "ar") == 0) {
                    set_language(LANG_AR); // Directly set language
                    printf("Config: Language set to Arabic\n"); // Debug
                } else if (_strcmp(value, "en") == 0) {
                    set_language(LANG_EN);
                    printf("Config: Language set to English\n"); // Debug
                } else {
                    fprintf(stderr, "Warning: Invalid language '%s' in config file. Using default.\n", value);
                }
            }
            else if (_strcmp(key, "history_file") == 0)
            {
                // Update the history file path in the info struct
                free(info->history_file_path); // Free default if set
                info->history_file_path = shell_strdup(value);
                printf("Config: History file path set to '%s'\n", value); // Debug
            }
            else if (_strcmp(key, "default_layout") == 0)
            {
                if (_strcmp(value, "ar") == 0) {
                    info->default_layout = 1;
                    set_keyboard_layout(1); // Set initial layout
                    printf("Config: Default layout set to Arabic\n"); // Debug
                } else if (_strcmp(value, "en") == 0) {
                    info->default_layout = 0;
                    set_keyboard_layout(0);
                    printf("Config: Default layout set to English\n"); // Debug
                } else {
                    fprintf(stderr, "Warning: Invalid default_layout '%s' in config file. Using default.\n", value);
                }
            }
            // Add more settings here (e.g., colors)

            // Free allocated key/value for the current line
            free(key);
            free(value);
            key = NULL;
            value = NULL;
        }
    }

    fclose(file);
}

/**
 * create_default_config - Creates a default configuration file
 * @path: Path where to create the config file
 * Return: 1 on success, 0 on failure
 */
int create_default_config(const char *path)
{
    FILE *config_file;
    
    // Make sure the directory exists
    if (!ensure_config_dir_exists(path))
    {
        fprintf(stderr, "Error: Could not create config directory.\n");
        return 0;
    }
    
    // Open the file for writing
    config_file = fopen(path, "w");
    if (!config_file)
    {
        fprintf(stderr, "Error: Could not create config file at %s.\n", path);
        return 0;
    }
    
    // Write default configuration content
    fprintf(config_file, "# ArbSh Configuration File\n\n");
    
    fprintf(config_file, "# Language Settings\n");
    fprintf(config_file, "# Supported values: en, ar\n");
    fprintf(config_file, "language = en\n\n");
    
    fprintf(config_file, "# History File Path\n");
    fprintf(config_file, "# You can customize where the shell history is stored\n");
    fprintf(config_file, "# Default: ~/.simple_shell_history\n");
    fprintf(config_file, "history_file = .simple_shell_history\n\n");
    
    fprintf(config_file, "# Default Keyboard Layout\n");
    fprintf(config_file, "# Supported values: en, ar\n");
    fprintf(config_file, "# This sets the initial keyboard layout when the shell starts\n");
    fprintf(config_file, "default_layout = en\n\n");
    
    fprintf(config_file, "# --- Future Settings (Not Yet Implemented) ---\n");
    fprintf(config_file, "# Console Color Settings\n");
    fprintf(config_file, "# color_prompt = green\n");
    fprintf(config_file, "# color_error = red\n");
    fprintf(config_file, "# color_output = white\n");
    
    fclose(config_file);
    printf("Created default configuration file at: %s\n", path);
    return 1;
}

// Add a command to create the default configuration
/**
 * _myconfig - handles the config command
 * @info: the parameter and return info struct
 * Return: 0 on success, 1 on failure
 */
int _myconfig(info_t *info)
{
    char config_path[PATH_MAX];
    char *command = info->argv[0];
    
    // No arguments, display help
    if (info->argc == 1)
    {
        _puts("config: Manage shell configuration\n");
        _puts("Usage: config [OPTION]\n");
        _puts("Options:\n");
        _puts("  init       Create a default configuration file\n");
        _puts("  path       Show the configuration file path\n");
        return 0;
    }
    
    // Get config file path
    if (!get_config_file_path(config_path, sizeof(config_path)))
    {
        _puts("Error: Could not determine configuration file path.\n");
        return 1;
    }
    
    // Handle subcommands
    if (_strcmp(info->argv[1], "path") == 0)
    {
        // Show config file path
        _puts(config_path);
        _putchar('\n');
        return 0;
    }
    else if (_strcmp(info->argv[1], "init") == 0)
    {
        // Create default config file
        if (create_default_config(config_path))
        {
            _puts("Default configuration created. Restart shell to apply changes.\n");
            return 0;
        }
        else
        {
            _puts("Error creating default configuration.\n");
            return 1;
        }
    }
    
    // Unknown subcommand
    _puts(command);
    _puts(": Unknown subcommand. Use 'config' without arguments for help.\n");
    return 1;
}