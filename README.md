
# ArbSh Project Status

This document provides a comprehensive overview of the current state of the ArbSh project, its components, and planned development roadmap.

## Project Overview

The ArbSh project is a UNIX command line interpreter implemented in C with cross-platform support for both Unix/Linux and Windows systems. The project is specifically designed to support the Baa language ecosystem by providing comprehensive Arabic language support that most standard terminals lack. Key features include:

1. **Cross-platform compatibility** - Core shell works on both Windows and Unix/Linux systems.
2. **Full Arabic language support** - Including right-to-left text, a full Unicode Bidirectional Algorithm (UAX #9), UTF-8 handling, and localization.
3. **Standalone operation** - Can be installed as a primary shell.
4. **Modern UI options** - Optional ImGui-based GUI mode on Windows with tabbed interface.

This shell addresses a critical gap in the Baa language ecosystem by providing a terminal environment that fully supports Arabic as both input and output, which is essential for the proper functioning of the Baa programming language.

## Current Implementation Status

### Core Shell Functionality

The core shell functionality is implemented and working:

- Basic command execution
- Environment variable handling
- Command history
- Built-in commands (cd, exit, env, help, history, alias, setenv, unsetenv, lang, layout, test)
- Signal handling (Ctrl+C)
- Input/output redirection (basic)
- Command line editing (basic)
- Command chaining (&&, ||, ;)
- Variable replacement ($?, $$)
- Cross-platform compatibility layer (console mode)

### Arabic Language Support

Arabic language support is a primary focus and largely complete:

| Feature                 | Status        | Notes                                                                 |
| :---------------------- | :------------ | :-------------------------------------------------------------------- |
| UTF-8 encoding          | ✅ Complete   | Full implementation (`utf8.c`).                                       |
| Right-to-left text      | ✅ Complete   | Full RTL support via BiDi algorithm and terminal controls.            |
| Localization            | ✅ Complete   | English/Arabic messages (`locale.c`). `lang` command.                 |
| Terminal configuration  | ✅ Complete   | UTF-8 setup, font hints, direction setting (`utf8.c`).                |
| Arabic character handling | ✅ Complete   | Detection and basic handling. Shaping relies on renderer.             |
| Bidirectional text      | ✅ Complete   | Full UAX #9 implementation (`bidi.c`). Handles complex cases.         |
| Arabic keyboard layout  | ✅ Complete   | Support for layout switching (`arabic_input.c`, `layout` command).    |
| Right-to-left rendering | ✅ Complete   | Text rendered RTL in Arabic mode (console/GUI).                       |
| Mixed text handling     | ✅ Complete   | Correct directional handling via BiDi algorithm.                      |
| Input method            | ✅ Complete   | Basic Arabic text input with RTL handling.                            |

### GUI Support (ImGui - Windows Only)

| Feature                     | Status        | Notes                                                                 |
| :-------------------------- | :------------ | :-------------------------------------------------------------------- |
| ImGui integration           | ✅ Complete   | Framework integrated (`imgui_main.cpp`, `imgui_shell.cpp`).           |
| GUI shell interface         | ✅ Complete   | Shell runs in tabs, basic I/O via pipes.                              |
| Modern UI elements          | ✅ Complete   | Custom styling, dark theme, tabs (`imgui_shell.cpp`).                 |
| Theme support               | ✅ Complete   | Basic dark theme applied.                                             |
| Tab Management (Process)    | ✅ Complete   | Each tab runs `hsh.exe` via `process_manager.c`, `terminal_tab.c`.    |
| **Terminal Emulation**      | ❌ **Missing**| **CRITICAL GAP:** GUI shows raw output, no VT100/ANSI interpretation. |

### Windows-Specific Features

| Feature             | Status        | Notes                                                                 |
| :------------------ | :------------ | :-------------------------------------------------------------------- |
| Standalone console  | ✅ Complete   | Custom console setup with UTF-8 support (`shell_entry.c`, `utf8.c`).    |
| Custom appearance   | ✅ Complete   | Font, colors configurable via code (`utf8.c`, `imgui_shell.cpp`).       |
| Windows API integration | ✅ Complete   | Uses Console API, Process API, etc.                                   |
| ImGui GUI mode      | ✅ Complete   | Functional GUI mode available.                                        |

### Build System

- CMake-based build with cross-platform support (Console mode).
- Options for ENABLE_TESTS, BUILD_STATIC.
- Four distinct build configurations possible.
- Handles C and C++ sources, linking dependencies (ImGui, D3D11).

## Code Structure

The codebase is organized into the following key components:

- **`src/core/`**: Core shell logic (loop, builtins, parsing, execution). `shell_entry.c` is the unified entry point.
- **`src/i18n/`**: Internationalization (`locale/locale.c`), Bidirectional algorithm (`bidi/bidi.c`), Arabic input (`arabic_input.c`).
- **`src/platform/`**: Platform-specific code. `windows/` contains implementations. `process_manager.c/h` handles child processes.
- **`src/gui/`**: ImGui GUI components (`imgui_main.cpp`, `imgui_shell.cpp`), Tab logic (`terminal_tab.c/h`).
- **`src/utils/`**: Utility functions (lists, memory, string, UTF-8 helpers, `imgui_compat.c`).
- **`include/`**: Public headers, primarily `shell.h`.
- **`external/imgui/`**: ImGui library source.
- **`tests/`**: Test suites (`arabic/`, `gui/`).
- **`docs/`**: Project documentation.
- **`CMakeLists.txt`**: Main build configuration.

## Roadmap Overview

(See `ROADMAP.md` and `TERMINAL_EMULATION_ROADMAP.md` for details)

- **Phase 1: Arabic Language Support:** ✅ Mostly Complete
- **Phase 2: Baa Language Integration:** 🔶 Planned (Environment ready)
- **Phase 3: Standalone Shell Application:** 🔶 In Progress (GUI exists, needs terminal emulation)
- **Phase 4: Cross-Platform Compatibility:** 🔶 Partial (Core is cross-platform, GUI/Process Manager is Windows-only)
- **Phase 5: Advanced Features & Ecosystem:** 🔶 Planned

## Next Development Priorities

1. **Implement Terminal Emulation in GUI:** (Critical) Integrate a VT100/ANSI emulator to make the GUI usable with interactive applications. See `TERMINAL_EMULATION_ROADMAP.md`.
2. **Strengthen Platform Abstraction:** Implement Unix/Linux equivalents for `process_manager.c` and other platform code to make GUI/Tabs potentially cross-platform.
3. **Enhance GUI/Shell Integration:** Explore ways for richer communication beyond stdin/stdout (control channel or library model).
4. **Baa Language Features:** Begin implementing Baa-specific commands, syntax highlighting, etc.
5. **PowerShell-like Features:** Start designing/implementing the object pipeline.

## Technical Challenges and Solutions

- **Arabic/BiDi Support:** Addressed via custom UTF-8 handling (`utf8.c`) and a full UAX #9 BiDi implementation (`bidi.c`). Terminal configuration (`utf8.c`) sets up the environment.
- **GUI Implementation:** Addressed using ImGui with DirectX on Windows. Tabs use separate processes managed by `process_manager.c` and `terminal_tab.c`.
- **Cross-Platform:** Core C logic is largely portable. GUI and Process Management are currently Windows-specific and need abstraction/implementation for other platforms.

## Integration with Baa Language Ecosystem

ArbSh provides the necessary foundation:

1. **Correct Arabic Environment:** Overcomes limitations of standard terminals.
2. **Consistent Platform:** Aims to provide a uniform base for Baa development.
3. **UTF-8/RTL/BiDi:** Handles the complexities of Arabic text required by Baa.

## Known Issues and Limitations

1. **GUI Lacks Terminal Emulation:** The ImGui GUI currently displays raw stdout/stderr. It doesn't interpret terminal control codes (colors, cursor movement), making it unsuitable for interactive programs like text editors or utilities like `htop`. This is the most significant limitation of the GUI mode.
2. **GUI/Shell Integration:** Communication is limited to stdin/stdout pipes due to the process-per-tab model. Richer interaction (e.g., GUI reflecting shell CWD) is not implemented.
3. **Platform Support:** GUI mode and process management are currently Windows-only. Unix/Linux implementations are needed for cross-platform GUI/tabs.
4. **Performance:** BiDi processing (`bidi.c`) is complex; performance under heavy load or with extremely long lines hasn't been benchmarked. Process-per-tab might have overhead.
5. **Testing Coverage:** Core Arabic/BiDi/UTF-8 logic has tests (`test_utf8`, `test_bidi`, `test_keyboard`). GUI (`test_imgui`) tests are basic. Terminal emulation, process management, and broader integration tests are needed.
6. **Arabic Shaping:** Relies on the underlying rendering engine (Console font rendering or ImGui font rendering) for Arabic letter shaping (joining forms, ligatures). No custom shaping logic is implemented in ArbSh itself.

## Conclusion

ArbSh has successfully implemented robust core shell features and comprehensive Arabic/BiDi support. The addition of the ImGui GUI provides a modern interface foundation on Windows. The immediate critical next step is implementing terminal emulation within the GUI to make it fully functional. The project is well-positioned to serve the Baa ecosystem once GUI usability is addressed.
