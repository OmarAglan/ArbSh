# Simple Shell

A simple UNIX command line interpreter implemented in C, with cross-platform support for both Unix/Linux and Windows systems.

## Features

- Basic command execution
- Environment variable handling
- Command history
- Built-in commands (cd, exit, env, etc.)
- Signal handling (Ctrl+C)
- Input/output redirection
- Command line editing
- Cross-platform compatibility (Windows and Unix/Linux)

## Building from Source

### Prerequisites

- C compiler (GCC 4.8+ for Unix/Linux, MSVC 2019+ for Windows)
- CMake 3.10 or higher
- Make (Unix/Linux) or Visual Studio (Windows)

### Build Instructions

#### Unix/Linux
```bash
mkdir build && cd build
cmake ..
make
```

#### Windows (Visual Studio)
```powershell
mkdir build
cd build
cmake ..
cmake --build .
```

## Usage

Run the shell:
```bash
./hsh
```

The shell supports both interactive and non-interactive modes.

### Interactive Mode
```bash
$ ./hsh
($) /bin/ls
hsh main.c shell.c
($) exit
$
```

### Non-interactive Mode
```bash
$ echo "/bin/ls" | ./hsh
hsh main.c shell.c test_ls_2
$
```

## Built-in Commands

- `cd [directory]`: Change current directory
- `exit [status]`: Exit the shell
- `env`: Print environment variables
- `setenv VARIABLE VALUE`: Set an environment variable
- `unsetenv VARIABLE`: Remove an environment variable
- `help`: Display help information
- `history`: Display command history

## Platform-Specific Notes

### Windows
- Uses native Windows API calls where necessary
- Handles path separators appropriately
- Proper signal handling for Windows
- Environment variable management adapted for Windows

### Unix/Linux
- Full POSIX compliance
- Native system call usage
- Standard Unix path handling

## Project Structure

```
.
├── AUTHORS
├── CMakeLists.txt
├── README.md
├── CHANGELOG.md
├── docs/
│   └── CONTRIBUTING.md
├── src/
│   ├── builtin.c
│   ├── environ.c
│   ├── errors.c
│   └── ...
└── include/
    └── shell.h
```

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Authors

See the [AUTHORS](AUTHORS) file for the list of contributors.
