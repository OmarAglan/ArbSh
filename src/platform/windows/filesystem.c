#include "platform/filesystem.h"
#include <windows.h>
#include <direct.h> // For _getcwd, _chdir
#include <io.h>     // For _access
#include <sys/stat.h> // For _stat structure (used indirectly by _access maybe)
#include <shlobj.h> // For SHGetFolderPathW
#include <stdlib.h> // For wcstombs
#include <stdio.h>  // For snprintf

// Define the actual structure for Windows file status
struct platform_stat_s {
    WIN32_FILE_ATTRIBUTE_DATA file_info;
};

int platform_stat(const char *path, platform_stat_t *buf)
{
    if (!path || !buf) return -1;
    // GetFileAttributesEx is generally preferred over GetFileAttributes
    if (!GetFileAttributesEx(path, GetFileExInfoStandard, &buf->file_info)) {
        // Set errno based on GetLastError()? Might be complex.
        // For now, just return -1 on failure.
        // errno = map_windows_error_to_errno(GetLastError()); 
        return -1;
    }
    return 0;
}

void platform_free_stat(platform_stat_t *buf)
{
    // No dynamic allocation within the struct itself for Windows
    (void)buf; // Suppress unused parameter warning
}

int platform_stat_is_directory(const platform_stat_t *buf)
{
    return (buf && (buf->file_info.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY));
}

int platform_stat_is_regular_file(const platform_stat_t *buf)
{
    // Check if it's NOT a directory, device, etc.
    return (buf && !(buf->file_info.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) &&
                   !(buf->file_info.dwFileAttributes & FILE_ATTRIBUTE_DEVICE) &&
                   !(buf->file_info.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT)); // Basic check
}

// Helper function from builtin1.c (consider moving to a shared util if needed)
static int is_executable_file_ext(const char *filename)
{
    const char *extension = strrchr(filename, '.');
    if (!extension)
        return 0;
    char ext_lower[10] = {0};
    int i;
    for (i = 0; extension[i] && i < 9; i++)
        ext_lower[i] = (extension[i] >= 'A' && extension[i] <= 'Z') ? 
                       extension[i] + 32 : extension[i];
    return (strcmp(ext_lower, ".exe") == 0 ||
            strcmp(ext_lower, ".bat") == 0 ||
            strcmp(ext_lower, ".cmd") == 0 ||
            strcmp(ext_lower, ".com") == 0 ||
            strcmp(ext_lower, ".ps1") == 0 ||
            strcmp(ext_lower, ".vbs") == 0 ||
            strcmp(ext_lower, ".msi") == 0);
}

int platform_stat_is_executable(const platform_stat_t *buf)
{
    // Windows doesn't have a direct executable bit in the same way as POSIX.
    // We rely on file extension primarily.
    // NOTE: This requires the original path, which platform_stat doesn't store.
    // This highlights a limitation of this simple stat abstraction.
    // For now, return 0, as we need the path.
    // A better approach might require passing the path to this function
    // or having platform_access handle X_OK.
    (void)buf; // Suppress unused
    // return is_executable_file_ext(path_passed_to_platform_stat); // Need path!
    return 0; // Cannot determine without path
}

long long platform_stat_get_size(const platform_stat_t *buf)
{
    if (!buf) return -1;
    ULARGE_INTEGER size;
    size.LowPart = buf->file_info.nFileSizeLow;
    size.HighPart = buf->file_info.nFileSizeHigh;
    return (long long)size.QuadPart;
}

time_t platform_stat_get_mtime(const platform_stat_t *buf)
{
    if (!buf) return (time_t)-1;
    // Conversion from FILETIME to time_t
    ULARGE_INTEGER ull;
    ull.LowPart = buf->file_info.ftLastWriteTime.dwLowDateTime;
    ull.HighPart = buf->file_info.ftLastWriteTime.dwHighDateTime;
    // Windows FILETIME is 100-nanosecond intervals since Jan 1, 1601.
    // time_t is seconds since Jan 1, 1970.
    // Conversion factor: (1000*1000*10) = 10,000,000 -> number of 100ns intervals per second
    // Epoch difference: 11644473600 seconds between 1601 and 1970
    return (time_t)(ull.QuadPart / 10000000ULL - 11644473600ULL);
}

char* platform_getcwd(char *buf, size_t size)
{
    // Use the CRT function _getcwd
    return _getcwd(buf, (int)size); // Note: size cast to int
}

int platform_chdir(const char *path)
{
    // Use the CRT function _chdir
    return _chdir(path);
}

int platform_access(const char *path, int mode)
{
    // Use the CRT function _access
    // Map platform constants to Windows _access constants
    int win_mode = 0;
    if (mode == PLATFORM_F_OK) win_mode = 0; // Existence
    if (mode & PLATFORM_R_OK) win_mode |= 4; // Read
    if (mode & PLATFORM_W_OK) win_mode |= 2; // Write
    if (mode & PLATFORM_X_OK) {
        // Windows _access doesn't check executable bit directly.
        // Check existence and then maybe check extension?
        // For now, just check existence for X_OK, as true execute check is complex.
        win_mode |= 0; 
    }

    return _access(path, win_mode);
}

char* platform_get_home_dir(char *buf, size_t size)
{
    wchar_t wpath[MAX_PATH];
    if (SUCCEEDED(SHGetFolderPathW(NULL, CSIDL_PROFILE, NULL, 0, wpath))) {
        // Convert wide char path to multibyte (UTF-8)
        size_t converted_chars = 0;
        errno_t err = wcstombs_s(&converted_chars, buf, size, wpath, _TRUNCATE);
        if (err == 0 && converted_chars > 0) {
            return buf;
        }
    }
    // Fallback: Try environment variables
    const char* home_drive = getenv("HOMEDRIVE");
    const char* home_path = getenv("HOMEPATH");
    if (home_drive && home_path) {
        snprintf(buf, size, "%s%s", home_drive, home_path);
        return buf;
    }
     const char* user_profile = getenv("USERPROFILE");
    if (user_profile) {
         strncpy(buf, user_profile, size -1);
         buf[size -1] = '\0';
         return buf;
    }

    return NULL; // Failed to get home directory
}

int platform_open(const char *pathname, int flags)
{
    // TODO: Map POSIX flags (O_RDONLY etc) to Windows flags (_O_RDONLY)
    // Requires including fcntl.h potentially
    // For now, assume flags are already Windows-compatible CRT flags
    return _open(pathname, flags); // Using CRT _open
}

int platform_close(int fd)
{
    return _close(fd); // Using CRT _close
} 