# CMake Configuration Guide

## Overview

This document provides guidelines for organizing and enhancing the CMake build system for the Simple Shell project. A well-structured build system is essential for cross-platform development, especially when supporting both Windows and Unix/Linux platforms with Arabic language features.

## Current Build System

The current CMake configuration provides basic functionality but can be improved for better organization and maintainability:

- Single top-level CMakeLists.txt file
- Basic source file globbing
- Platform-specific definitions and libraries
- Limited testing support
- Basic installation rules

## Proposed Improvements

### 1. Hierarchical CMake Structure

Reorganize the build system with hierarchical CMakeLists.txt files:

```
simple-shell/
├── CMakeLists.txt              # Top-level CMake file
├── cmake/                      # CMake modules directory
│   ├── FindDependencies.cmake  # Find external dependencies
│   ├── CompilerOptions.cmake   # Compiler configuration
│   └── InstallRules.cmake      # Installation rules
├── src/
│   ├── CMakeLists.txt          # Source directory CMake
│   ├── core/
│   │   └── CMakeLists.txt      # Core module CMake
│   ├── platform/
│   │   ├── CMakeLists.txt      # Platform module CMake
│   │   ├── unix/
│   │   │   └── CMakeLists.txt  # Unix-specific CMake
│   │   └── windows/
│   │       └── CMakeLists.txt  # Windows-specific CMake
│   └── i18n/
│       └── CMakeLists.txt      # Internationalization module CMake
└── tests/
    └── CMakeLists.txt          # Tests directory CMake
```

### 2. Modular Component Organization

Organize the build into logical components:

```cmake
# In src/CMakeLists.txt
add_subdirectory(core)
add_subdirectory(platform)
add_subdirectory(i18n)
add_subdirectory(ui)

# Create the main executable
add_executable(hsh ${SHELL_ENTRY_SOURCES})

# Link against our components
target_link_libraries(hsh
    PRIVATE
        shell_core
        shell_platform
        shell_i18n
        shell_ui
)
```

### 3. Component Libraries

Create separate libraries for each component:

```cmake
# In src/core/CMakeLists.txt
add_library(shell_core
    parser.c
    tokenizer.c
    shell_loop.c
    # Other core files
)

target_include_directories(shell_core
    PUBLIC
        ${CMAKE_SOURCE_DIR}/include
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
)
```

### 4. Platform Abstraction

Organize platform-specific code:

```cmake
# In src/platform/CMakeLists.txt
if(WIN32)
    add_subdirectory(windows)
    set(PLATFORM_LIB shell_platform_windows)
else()
    add_subdirectory(unix)
    set(PLATFORM_LIB shell_platform_unix)
endif()

# Create platform abstraction library
add_library(shell_platform INTERFACE)
target_link_libraries(shell_platform INTERFACE ${PLATFORM_LIB})
```

### 5. Improved Testing Support

Enhance testing configuration:

```cmake
# In tests/CMakeLists.txt
enable_testing()

# Find testing framework
find_package(Unity REQUIRED)

# Add test directories
add_subdirectory(unit)
add_subdirectory(integration)
add_subdirectory(arabic)

# Create test runner
add_executable(test_runner test_runner.c)
target_link_libraries(test_runner PRIVATE Unity::Unity)

# Add tests to CTest
add_test(NAME unit_tests COMMAND test_runner unit)
add_test(NAME arabic_tests COMMAND test_runner arabic)
```

### 6. Version Information

Add proper version handling:

```cmake
# In top-level CMakeLists.txt
set(SHELL_VERSION_MAJOR 1)
set(SHELL_VERSION_MINOR 0)
set(SHELL_VERSION_PATCH 0)
set(SHELL_VERSION "${SHELL_VERSION_MAJOR}.${SHELL_VERSION_MINOR}.${SHELL_VERSION_PATCH}")

# Generate version header
configure_file(
    ${CMAKE_SOURCE_DIR}/include/shell_version.h.in
    ${CMAKE_BINARY_DIR}/include/shell_version.h
)
```

### 7. Installation Configuration

Improve installation rules:

```cmake
# In cmake/InstallRules.cmake
include(GNUInstallDirs)

install(TARGETS hsh
    RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR}
    LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
    ARCHIVE DESTINATION ${CMAKE_INSTALL_LIBDIR}
)

# Install headers
install(DIRECTORY ${CMAKE_SOURCE_DIR}/include/
    DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}/shell
    FILES_MATCHING PATTERN "*.h"
)

# Install documentation
install(DIRECTORY ${CMAKE_SOURCE_DIR}/docs/
    DESTINATION ${CMAKE_INSTALL_DOCDIR}
    FILES_MATCHING
    PATTERN "*.md"
    PATTERN "*.txt"
)
```

### 8. Package Configuration

Add support for package creation:

```cmake
# In top-level CMakeLists.txt
include(CPack)

set(CPACK_PACKAGE_NAME "simple-shell")
set(CPACK_PACKAGE_VENDOR "Baa Language Team")
set(CPACK_PACKAGE_DESCRIPTION_SUMMARY "Simple Shell with Arabic Support")
set(CPACK_PACKAGE_VERSION ${SHELL_VERSION})
set(CPACK_PACKAGE_VERSION_MAJOR ${SHELL_VERSION_MAJOR})
set(CPACK_PACKAGE_VERSION_MINOR ${SHELL_VERSION_MINOR})
set(CPACK_PACKAGE_VERSION_PATCH ${SHELL_VERSION_PATCH})

# Platform-specific packaging
if(WIN32)
    set(CPACK_GENERATOR "NSIS;ZIP")
    set(CPACK_NSIS_MODIFY_PATH ON)
else()
    set(CPACK_GENERATOR "DEB;TGZ")
    set(CPACK_DEBIAN_PACKAGE_MAINTAINER "Baa Language Team")
endif()
```

## Implementation Steps

### 1. Create Directory Structure

1. Create the `cmake` directory and module files
2. Organize source code into subdirectories
3. Create subdirectory CMakeLists.txt files

### 2. Update Top-Level CMakeLists.txt

1. Include CMake modules
2. Set project properties and version
3. Define global compiler options
4. Add subdirectories

### 3. Create Component Libraries

1. Organize source files by component
2. Create library targets for each component
3. Define dependencies between components

### 4. Set Up Testing Framework

1. Create test directory structure
2. Configure test discovery and execution
3. Add test targets to build system

### 5. Configure Installation

1. Define installation rules for binaries
2. Set up documentation installation
3. Configure package generation

## Best Practices

1. **Avoid Globbing**: Instead of using `file(GLOB ...)`, explicitly list source files
2. **Use Target Properties**: Set properties on targets rather than using global variables
3. **Minimize Cache Variables**: Use local variables where possible
4. **Document CMake Code**: Add comments explaining non-obvious CMake code
5. **Use Modern CMake**: Prefer target-based commands over global variables
6. **Consistent Naming**: Use consistent naming conventions for targets and variables
7. **Version Compatibility**: Support older CMake versions where needed

## Cross-Platform Considerations

1. **Path Handling**: Use CMake path utilities for cross-platform path handling
2. **Compiler Flags**: Set compiler flags conditionally based on compiler ID
3. **Dependencies**: Handle dependencies differently on Windows and Unix
4. **Installation Paths**: Use platform-appropriate installation directories

## Arabic Support Considerations

1. **UTF-8 Configuration**: Ensure proper UTF-8 handling in build system
2. **Resource Files**: Configure proper handling of Arabic resource files
3. **Documentation**: Install documentation in both English and Arabic

## Conclusion

A well-organized CMake build system will significantly improve the maintainability and extensibility of the Simple Shell project. By following these guidelines, the project can achieve better cross-platform compatibility, easier integration of Arabic language features, and a more professional development workflow.