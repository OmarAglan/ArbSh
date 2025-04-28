#include "shell.h"
#include "platform/filesystem.h" // Include Filesystem PAL
#include <errno.h>
#include <ctype.h> // For isspace

#ifdef WINDOWS
#include <shlobj.h> // For SHGetFolderPathW (now handled by PAL)
#include <wchar.h>
#include <stdlib.h>
#else
#include <unistd.h> // For getuid, getpwuid (now handled by PAL)
#include <sys/types.h>
#include <stdlib.h> // For wcstombs_s
#endif

// Forward declaration
void apply_configuration(info_t *info, const char *key, const char *value, const char *filename, int line_num);

// --- Configuration Defaults ---
#define DEFAULT_LANGUAGE LANG_EN
#define DEFAULT_LAYOUT 0 // 0 = EN, 1 = AR
#define DEFAULT_HISTORY_FILE HIST_FILE // From shell.h
#define DEFAULT_CONFIG_FILENAME_WINDOWS L"\\ArbSh\\config.ini" // Needs conversion
#define DEFAULT_CONFIG_FILENAME_POSIX "/.arbshrc"

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
    // TODO: Implement directory creation using PAL (platform_mkdir)
    // For now, this function is a no-op, assuming the directory exists.
    (void)path; // Suppress unused parameter warning
    return 1; // Assume success for now
}

/**
 * get_config_file_path - Constructs the platform-specific path to the config file.
 * @buf: Buffer to store the resulting path.
 * @size: Size of the buffer.
 * Return: 1 on success, 0 on failure.
 */
int get_config_file_path(char *buf, size_t size)
{
    char home_dir[PATH_MAX];

    if (!platform_get_home_dir(home_dir, sizeof(home_dir)))
    {
        return 0; // Cannot determine home directory
    }

#ifdef WINDOWS
    // On Windows, config is typically in %APPDATA%\ArbSh\config.ini
    // platform_get_home_dir usually gives USERPROFILE, need APPDATA
    wchar_t wpath[MAX_PATH];
    if (SUCCEEDED(SHGetFolderPathW(NULL, CSIDL_APPDATA, NULL, 0, wpath))) {
        // Convert APPDATA path
        size_t converted_chars = 0;
        errno_t err = wcstombs_s(&converted_chars, buf, size, wpath, _TRUNCATE);
        if (err == 0 && converted_chars > 0) {
            // Append our subdirectory and filename
            strncat(buf, "\\ArbSh", size - strlen(buf) - 1);
            // TODO: Ensure ArbSh directory exists
            strncat(buf, "\\config.ini", size - strlen(buf) - 1);
            return 1;
        }
    }
    // Fallback if SHGetFolderPath fails (less ideal)
    snprintf(buf, size, "%s%s", home_dir, "\\AppData\\Roaming\\ArbSh\\config.ini");
    return 1;

#else // POSIX
    // On POSIX, config is typically ~/.arbshrc
    snprintf(buf, size, "%s%s", home_dir, DEFAULT_CONFIG_FILENAME_POSIX);
    return 1;
#endif
}

/**
 * load_configuration - Loads settings from the configuration file.
 * @info: The info struct to populate.
 */
void load_configuration(info_t *info)
{
    char config_path[PATH_MAX];
    FILE *file;
    char line[512];
    char *key = NULL;
    char *value = NULL;
    int line_num = 0;

    // --- Set Defaults First ---
    info->default_layout = DEFAULT_LAYOUT;
    // History file path might also depend on home dir, handle later
    info->history_file_path = NULL; // Set to null initially

    // --- Get Config Path (uses platform_get_home_dir internally) ---
    if (!get_config_file_path(config_path, sizeof(config_path)))
    {
        // Non-fatal: Use defaults if config path fails
        fprintf(stderr, "Warning: Could not determine config file path. Using defaults.\n");
        // Ensure history path has a default based on platform_get_home_dir if possible
        char *hist_path = get_history_file(info); // This now uses PAL
        info->history_file_path = hist_path ? hist_path : shell_strdup(DEFAULT_HISTORY_FILE);
        return;
    }

    // --- Open and Read Config File ---
    // TODO: Use platform_open later?
    file = fopen(config_path, "r");
    if (!file)
    {
        // Non-fatal: Use defaults if file not found
        char *hist_path = get_history_file(info);
        info->history_file_path = hist_path ? hist_path : shell_strdup(DEFAULT_HISTORY_FILE);
        return;
    }

    printf("Loading configuration from: %s\n", config_path); // Debug message

    while (fgets(line, sizeof(line), file))
    {
        line_num++;
        if (parse_config_line(line, &key, &value) == 0)
        {
            // TODO: Re-implement or find apply_configuration function
            // apply_configuration(info, key, value, config_path, line_num);
            free(key); key = NULL;
            free(value); value = NULL;
        }
    }

    fclose(file);

    // If history file wasn't set in config, get default path now
    if (!info->history_file_path) {
        char *hist_path = get_history_file(info); // Uses PAL
        info->history_file_path = hist_path ? hist_path : shell_strdup(DEFAULT_HISTORY_FILE);
    }

    // Apply defaults if specific settings weren't found
    // (e.g., set language based on system default if not in config)
    if (/* language not set by config */ 1) {
         set_language(detect_system_language());
    }
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