# CMake Configuration Guide

This document provides detailed information about the CMake build system used by the ArbSh project, including available build options, dependencies, and configuration instructions.

## Overview

ArbSh uses CMake (version 3.10+) for cross-platform build generation (Windows, Unix/Linux). The configuration supports console and GUI modes, testing, and static linking.

## Prerequisites

- **CMake** (version 3.10 or higher)
- **C Compiler** with C11 support (GCC, Clang, MSVC)
- **C++ Compiler** with C++17 support (Required *only* for GUI mode: GCC, Clang, MSVC)
- **Development Libraries:**
  - Windows: Platform SDK, DirectX SDK components (for GUI mode).
  - Unix/Linux: Standard development tools (`build-essential`, `gcc`, `make`, etc.).

## Build Options

Configure these options using `cmake -DOPTION=VALUE ..`:


- `ENABLE_TESTS` (Default: `OFF`)
  - `ON`: Build the test suite executables (`test_utf8`, `test_bidi`, `test_keyboard`). Enables CTest support.
  - `OFF`: Do not build tests.
- `BUILD_STATIC` (Default: `OFF`)
  - `ON`: Attempt to build a statically linked executable. Links against static runtime libraries and `-static` flags where applicable (behavior varies between MSVC/GCC).
  - `OFF`: Build with default dynamic linking.

## Build Configurations / Modes

By combining build options, you can achieve different configurations:

1. **Console Mode Only (Default):**

    ```bash
    cmake -DENABLE_TESTS=OFF ..
    ```



2. **Console Mode with Tests:**

    ```bash
    cmake -DENABLE_TESTS=ON ..
    ```



## Basic Build Instructions

### Windows (using MinGW GCC)

```bash
mkdir build
cd build
# Example: Build Console + Tests
cmake -G "MinGW Makefiles" -DENABLE_TESTS=ON ..
# Build the application and tests
mingw32-make
# Run tests (if enabled)
mingw32-make run_tests
