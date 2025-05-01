#include "platform/filesystem.h"
#include <sys/stat.h> // For stat, S_ISDIR, S_ISREG, S_IXUSR etc.
#include <unistd.h>   // For getcwd, chdir, access, getuid, close
#include <sys/types.h>// For uid_t, gid_t, pid_t
#include <pwd.h>      // For getpwuid, getpwnam
#include <stdlib.h>   // For getenv, malloc, free
#include <string.h>
#include <fcntl.h>    // For open flags (O_RDONLY etc.)
#include <errno.h>

// Define the actual structure for POSIX file status
// It simply wraps the standard struct stat
struct platform_stat_s {
    struct stat posix_stat;
};

int platform_stat(const char *path, platform_stat_t *buf)
{
    if (!path || !buf) return -1;
    // Use the standard POSIX stat function
    int result = stat(path, &buf->posix_stat);
    // errno is set by stat() on failure
    return result;
}

void platform_free_stat(platform_stat_t *buf)
{
    // No dynamic allocation within the struct itself for POSIX
    (void)buf; // Suppress unused parameter warning
}

int platform_stat_is_directory(const platform_stat_t *buf)
{
    return (buf && S_ISDIR(buf->posix_stat.st_mode));
}

int platform_stat_is_regular_file(const platform_stat_t *buf)
{
    return (buf && S_ISREG(buf->posix_stat.st_mode));
}

int platform_stat_is_executable(const platform_stat_t *buf)
{
    // Check the execute bits. Assumes check for the owner.
    // TODO: Check group/other execute bits based on effective UID/GID if needed.
    return (buf && (buf->posix_stat.st_mode & S_IXUSR));
}

long long platform_stat_get_size(const platform_stat_t *buf)
{
    return (buf ? (long long)buf->posix_stat.st_size : -1);
}

time_t platform_stat_get_mtime(const platform_stat_t *buf)
{
    return (buf ? buf->posix_stat.st_mtime : (time_t)-1);
}

char* platform_getcwd(char *buf, size_t size)
{
    // Use the standard POSIX getcwd function
    return getcwd(buf, size);
}

int platform_chdir(const char *path)
{
    // Use the standard POSIX chdir function
    return chdir(path);
}

int platform_access(const char *path, int mode)
{
    // Map platform constants directly to POSIX access constants
    int posix_mode = 0;
    if (mode == PLATFORM_F_OK) posix_mode = F_OK;
    if (mode & PLATFORM_R_OK) posix_mode |= R_OK;
    if (mode & PLATFORM_W_OK) posix_mode |= W_OK;
    if (mode & PLATFORM_X_OK) posix_mode |= X_OK;

    return access(path, posix_mode);
}

char* platform_get_home_dir(char *buf, size_t size)
{
    const char *home_env = getenv("HOME");
    if (home_env) {
        strncpy(buf, home_env, size - 1);
        buf[size - 1] = '\0'; // Ensure null termination
        return buf;
    }

    // Fallback: Get home directory from user database
    uid_t uid = getuid();
    struct passwd *pw = getpwuid(uid);
    if (pw && pw->pw_dir) {
        strncpy(buf, pw->pw_dir, size - 1);
        buf[size - 1] = '\0';
        return buf;
    }

    return NULL; // Failed to get home directory
}

int platform_open(const char *pathname, int flags)
{
    // Assume standard POSIX flags are passed for now
    // TODO: Define platform-independent flags if needed
    return open(pathname, flags);
}

int platform_close(int fd)
{
    return close(fd);
} 