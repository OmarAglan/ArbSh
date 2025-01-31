# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2025-01-31

### Added
- Windows-specific signal handling with proper SIGINT definition
- CMake build system improvements for MSVC compiler
- Cross-platform compatibility layer in shell.h

### Changed
- Updated header file organization for better Windows/Unix compatibility
- Improved signal handler implementation for Windows
- Refactored environment variable handling for Windows compatibility

### Fixed
- Signal handling issues on Windows platform
- Build errors related to missing POSIX headers on Windows
- Type conversion warnings in getLine.c
- Environment variable access on Windows systems

## [1.0.0] - 2025-01-31

### Added
- Cross-platform support for Windows and Unix-like systems
- CMake build system with proper configuration
- Documentation generation using Doxygen
- Comprehensive .gitignore file
- Environment variable handling for both platforms
- Signal handling for Windows and Unix-like systems
- Built-in command support (cd, exit, env, etc.)
- History functionality
- Command chaining support (&&, ||, ;)
- Alias support
- Variable replacement ($? and $$)

### Changed
- Improved code organization and structure
- Enhanced error handling and reporting
- Updated function documentation
- Optimized environment variable management
- Standardized coding style across the project

### Fixed
- Duplicate function definitions
- Memory leaks in environment variable handling
- Signal handling issues on Windows
- Path resolution bugs
- Command execution errors
- History file handling issues

### Security
- Improved environment variable security
- Better memory management
- Enhanced error checking for system calls

## [0.1.0] - 2025-01-30

### Added
- Initial release
- Basic shell functionality
- Command execution
- Environment variable support
- Basic error handling
