# ArbSh - Arabic-First Shell

ArbSh is a cross-platform shell specially designed for proper Arabic language support. It addresses critical limitations in standard terminals that fail to properly handle Arabic text, providing comprehensive support for right-to-left text rendering and Arabic text input.

## Features

- **Full Arabic language support**
  - Right-to-left text rendering
  - Arabic keyboard layout with toggle
  - Bidirectional text algorithm implementation
  - UTF-8 character handling
  - Mixed Arabic/Latin text support

- **Cross-platform compatibility**
  - Works on both Windows and Unix/Linux
  - Consistent experience across platforms

- **Modern terminal features**
  - Custom appearance
  - Enhanced UI on Windows
  - Command history
  - Environment variable handling
  - ImGui-based GUI mode (Windows only)

## Building

### Prerequisites

- CMake 3.10 or higher
- C compiler with C11 support
- C++ compiler with C++11 support (for GUI mode)
- Windows: MinGW or MSVC
- Linux/Unix: GCC

### Windows Build Instructions

```bash
mkdir build
cd build
cmake ..
cmake --build .
```

#### GUI Mode (Windows only)

To build with ImGui-based GUI support:

```bash
mkdir build
cd build
cmake -DGUI_MODE=ON ..
cmake --build .
```

#### Static Build

For a statically linked executable:

```bash
mkdir build
cd build
cmake -DBUILD_STATIC=ON ..
cmake --build .
```

### Linux/Unix Build Instructions

```bash
mkdir build
cd build
cmake ..
make
```

## Testing

ArbSh includes a comprehensive test suite to verify Arabic language support.

### Running Tests

```bash
# Build and run all tests
mkdir build
cd build
cmake ..
cmake --build . --target run_tests

# Run individual test suites
cd bin
./test_utf8      # Test UTF-8 character handling
./test_bidi      # Test bidirectional text algorithm
./test_keyboard  # Test Arabic keyboard input
```

### Test Coverage

- **UTF-8 Tests**: Verify proper UTF-8 character handling, codepoint conversion, and RTL character detection
- **Bidirectional Tests**: Test the bidirectional text algorithm implementation
- **Keyboard Tests**: Test Arabic keyboard layout and input handling

## Usage

### Basic Commands

```bash
# Start the shell
./bin/hsh

# Switch to Arabic mode
lang ar

# Switch to English mode
lang en

# Toggle keyboard layout
layout toggle
```

## Documentation

For more detailed information about the project, refer to the documentation in the `docs` directory:

- [Project Status](docs/PROJECT_STATUS.md)
- [Development Tasks](docs/DEVELOPMENT_TASKS.md)
- [Roadmap](docs/ROADMAP.md)
- [Arabic Support Guide](docs/ARABIC_SUPPORT_GUIDE.md)
- [Testing Framework](docs/TESTING_FRAMEWORK.md)
- [CMake Configuration](docs/CMAKE_CONFIGURATION.md)
- [BAA Integration](docs/BAA_INTEGRATION.md)

## License

This project is licensed under the MIT License - see the LICENSE file for details. 