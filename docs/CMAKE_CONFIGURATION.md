# CMake Configuration Guide

This document provides detailed information about the CMake build system used by the ArbSh project, including available build options, dependencies, and configuration instructions.

## Overview

ArbSh uses CMake as its build system, which provides a cross-platform way to generate platform-specific build files. The CMake configuration supports building on both Windows and Unix/Linux platforms, with options for different features such as GUI mode and static linking.

## Prerequisites

To build ArbSh, you need the following tools:

- **CMake** (version 3.10 or higher)
- **C Compiler** with C11 support
  - Windows: Microsoft Visual C++ (MSVC) or MinGW-w64 GCC
  - Unix/Linux: GCC or Clang
- **C++ Compiler** with C++11 support (required for GUI mode)
- **Development Libraries**
  - Windows: Platform SDK (usually included with compilers)
  - Unix/Linux: Standard development packages

## Compiler Configuration

### Using GCC with the Toolchain File

The project includes a toolchain file for GCC that can be used to force CMake to use GCC for compilation:

```bash
# Use GCC with the toolchain file
cmake -DCMAKE_TOOLCHAIN_FILE=../gcc_toolchain.cmake ..
```

This is useful when you want to ensure that GCC is used regardless of the platform or default compiler.

## Build Options

The CMake configuration provides several build options that can be enabled or disabled:

### GUI Mode

The project supports an ImGui-based GUI mode that provides a modern graphical interface for the shell:

```bash
# Enable GUI mode
cmake -DGUI_MODE=ON ..
```

When GUI mode is enabled:
- The executable is built as a Windows GUI application (on Windows)
- ImGui and DirectX dependencies are linked
- GUI-specific source files are included in the build

### Test Suite

You can enable the test suite build with the ENABLE_TESTS option:

```bash
# Enable test suite
cmake -DENABLE_TESTS=ON ..
```

When tests are enabled:
- Test executables are built alongside the main application
- CMake testing infrastructure is configured
- Additional test targets are created

### Static Linking

You can enable static linking to create a standalone executable with minimal external dependencies:

```bash
# Enable static linking
cmake -DBUILD_STATIC=ON ..
```

This is useful for creating portable applications that don't depend on system-specific shared libraries.

## Build Modes

By combining the GUI_MODE and ENABLE_TESTS options, you can create four distinct build configurations:

### 1. Console Mode Only (Default)

This is the standard build with no extra features:

```bash
mkdir build
cd build
cmake ..
# Or explicitly:
# cmake -DGUI_MODE=OFF -DENABLE_TESTS=OFF ..
```

### 2. GUI Mode Only

Build only the GUI version without tests:

```bash
mkdir build
cd build
cmake -DGUI_MODE=ON -DENABLE_TESTS=OFF ..
```

### 3. Console Mode with Tests

Build the console version with the test suite:

```bash
mkdir build
cd build
cmake -DGUI_MODE=OFF -DENABLE_TESTS=ON ..
```

### 4. GUI Mode with Tests

Build the GUI version and include the test suite:

```bash
mkdir build
cd build
cmake -DGUI_MODE=ON -DENABLE_TESTS=ON ..
```

## Basic Build Instructions

### Windows

#### Using Visual Studio

1. Open a Developer Command Prompt for Visual Studio
2. Navigate to the project directory
3. Create a build directory and run CMake:

```bash
mkdir build
cd build
cmake -G "Visual Studio 16 2019" -A x64 ..
```

4. Build the project:

```bash
cmake --build . --config Release
```

Or open the generated `.sln` file in Visual Studio and build from there.

#### Using MinGW (GCC)

1. Install MinGW-w64 with GCC support
2. Add MinGW's bin directory to your PATH
3. Open a command prompt
4. Navigate to the project directory
5. Create a build directory and run CMake with the GCC toolchain file:

```bash
mkdir build
cd build
cmake -G "MinGW Makefiles" -DCMAKE_TOOLCHAIN_FILE=../gcc_toolchain.cmake ..
```

6. Build the project:

```bash
mingw32-make
```

### Unix/Linux

1. Open a terminal
2. Navigate to the project directory
3. Create a build directory and run CMake:

```bash
mkdir build
cd build
cmake ..
```

4. Build the project:

```bash
make
```

## Advanced Configuration

### Specifying Compiler

You can specify which compiler to use by using the toolchain file or by setting the `CC` and `CXX` environment variables before running CMake:

```bash
# Using the toolchain file (recommended for GCC)
cmake -DCMAKE_TOOLCHAIN_FILE=../gcc_toolchain.cmake ..

# Or using environment variables
# GCC
export CC=gcc
export CXX=g++

# Clang
export CC=clang
export CXX=clang++

# Then run CMake
cmake ..
```

### Debug Build

To create a debug build with full debugging information:

```bash
cmake -DCMAKE_BUILD_TYPE=Debug ..
```

### Release Build

To create an optimized release build:

```bash
cmake -DCMAKE_BUILD_TYPE=Release ..
```

## Project Structure

The CMake configuration is organized as follows:

- **Main CMakeLists.txt**: Root configuration file that defines the project, options, and targets
- **gcc_toolchain.cmake**: Toolchain file for building with GCC
- **Source Files**: Automatically discovered using `file(GLOB_RECURSE ...)` patterns
- **Tests**: Each test suite is defined as a separate executable target
- **Output Directories**: Binaries are placed in the `bin` directory, libraries in the `lib` directory

## ImGui Integration

The ImGui-based GUI mode requires additional dependencies and build steps:

1. The ImGui source files are located in `external/imgui`
2. DirectX 11 is used as the rendering backend on Windows
3. The core shell functionality is integrated with the ImGui interface

When GUI mode is enabled:
- ImGui source files are automatically included in the build
- DirectX libraries are linked
- The application entry point switches to a Windows GUI application

## Testing Configuration

The project includes several test suites that can be built and run using CMake when ENABLE_TESTS is ON:

- **test_utf8**: Tests for UTF-8 character handling
- **test_bidi**: Tests for bidirectional text algorithm
- **test_keyboard**: Tests for Arabic keyboard input
- **test_imgui**: Tests for ImGui integration (when GUI_MODE is also ON)

All tests can be built and run with a single command:

```bash
# On Windows with MinGW
mingw32-make run_tests

# On Unix/Linux
make run_tests
```

This will build all test executables and run them in sequence.

## Troubleshooting

### Common Issues

#### "CMake can't find compiler"

Make sure your compiler is installed and in your PATH:
- On Windows: Use the toolchain file to ensure GCC is used: `-DCMAKE_TOOLCHAIN_FILE=../gcc_toolchain.cmake`
- On Linux: Install GCC with `sudo apt install build-essential` (Ubuntu/Debian) or equivalent

#### "Missing libraries"

Ensure that all required development libraries are installed. On Unix/Linux, you may need to install specific development packages.

#### "ImGui build fails"

When building with GUI mode on Windows, make sure you have MinGW-w64 with DirectX support installed. The ImGui dependencies should be in the `external/imgui` directory.

#### "Static build fails"

When building with static linking, some platform-specific libraries might be missing. Make sure you have all required static libraries available.

## Example Configurations

### Windows GUI Build with GCC (MinGW)

```bash
mkdir build
cd build
cmake -G "MinGW Makefiles" -DCMAKE_TOOLCHAIN_FILE=../gcc_toolchain.cmake -DGUI_MODE=ON ..
mingw32-make
```

### Linux Static Build

```bash
mkdir build
cd build
cmake -DBUILD_STATIC=ON -DCMAKE_BUILD_TYPE=Release ..
make
```

### Debug Build with Tests (GCC)

```bash
mkdir build
cd build
cmake -DCMAKE_TOOLCHAIN_FILE=../gcc_toolchain.cmake -DCMAKE_BUILD_TYPE=Debug -DENABLE_TESTS=ON ..
mingw32-make run_tests
```

### GUI Mode with Tests (GCC)

```bash
mkdir build
cd build
cmake -G "MinGW Makefiles" -DCMAKE_TOOLCHAIN_FILE=../gcc_toolchain.cmake -DGUI_MODE=ON -DENABLE_TESTS=ON ..
mingw32-make
mingw32-make run_tests
```