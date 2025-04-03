#ifndef _PLATFORM_FILESYSTEM_H_
#define _PLATFORM_FILESYSTEM_H_

#include <stddef.h> // For size_t
#include <time.h>   // For time_t

// --- Platform-Independent File Status --- 

// Opaque structure for file status details.
// The actual definition is platform-specific.
typedef struct platform_stat_s platform_stat_t;

/**
 * @brief Gets the status information for a file.
 * 
 * @param path The path to the file.
 * @param buf Pointer to a platform_stat_t structure to fill.
 * @return 0 on success, -1 on error (errno/GetLastError should be set).
 */
int platform_stat(const char *path, platform_stat_t *buf);

/**
 * @brief Frees resources associated with a platform_stat_t structure.
 * Necessary if the structure itself allocates memory internally.
 * 
 * @param buf Pointer to the structure to free.
 */
void platform_free_stat(platform_stat_t *buf);

// Accessor functions for platform_stat_t
int platform_stat_is_directory(const platform_stat_t *buf);
int platform_stat_is_regular_file(const platform_stat_t *buf);
int platform_stat_is_executable(const platform_stat_t *buf);
long long platform_stat_get_size(const platform_stat_t *buf);
time_t platform_stat_get_mtime(const platform_stat_t *buf);
// Add more accessors as needed (e.g., permissions)


// --- Filesystem Operations --- 

/**
 * @brief Gets the current working directory.
 * 
 * @param buf Buffer to store the path.
 * @param size Size of the buffer.
 * @return Pointer to buf on success, NULL on error.
 */
char* platform_getcwd(char *buf, size_t size);

/**
 * @brief Changes the current working directory.
 * 
 * @param path The target directory path.
 * @return 0 on success, -1 on error.
 */
int platform_chdir(const char *path);

/**
 * @brief Checks file access permissions.
 * 
 * @param path The path to the file.
 * @param mode Access mode to check (e.g., F_OK, R_OK, W_OK, X_OK - requires including unistd.h or defining constants).
 *             Let's define our own constants for portability for now.
 */
#define PLATFORM_F_OK   0  // Test for existence
#define PLATFORM_X_OK   1  // Test for execute permission
#define PLATFORM_W_OK   2  // Test for write permission
#define PLATFORM_R_OK   4  // Test for read permission
int platform_access(const char *path, int mode);

/**
 * @brief Gets the user's home directory path.
 * 
 * @param buf Buffer to store the path.
 * @param size Size of the buffer.
 * @return Pointer to buf on success, NULL on error.
 */
char* platform_get_home_dir(char *buf, size_t size);

// --- File Operations (Simplified for now) ---
// These might need more abstraction later if advanced features are needed

/**
 * @brief Opens a file.
 * TODO: Abstract open flags (O_RDONLY etc.)
 * 
 * @param pathname The file path.
 * @param flags Platform-specific flags (e.g., O_RDONLY from fcntl.h).
 * @return File descriptor on success, -1 on error.
 */
int platform_open(const char *pathname, int flags);

/**
 * @brief Closes a file descriptor.
 * 
 * @param fd The file descriptor.
 * @return 0 on success, -1 on error.
 */
int platform_close(int fd);


#endif /* _PLATFORM_FILESYSTEM_H_ */ 