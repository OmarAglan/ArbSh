# Roadmap: Console Mode & GUI Integration Improvement

This roadmap focuses on enhancing the standalone console mode (`hsh.exe` run directly) and improving its consistency and interoperability with the ImGui-based GUI mode.

## Goals

1.  **Consistency:** Ensure shared settings (history, aliases, configuration) are respected by both modes.
2.  **Robustness:** Make the console mode more configurable and stable, especially regarding core features like BiDi text.
3.  **Awareness:** Allow the console instance to know if it's running standalone or hosted within the GUI.
4.  **Foundation:** Lay groundwork for potentially deeper integration or future refactoring (`libArbShCore`).

## Non-Goals (in this specific roadmap)

*   Implementing full terminal emulation in the GUI (This is covered in `TERMINAL_EMULATION_ROADMAP.md`).
*   Making the GUI itself cross-platform (Requires separate PAL work).
*   Implementing complex Console -> GUI IPC commands initially.

## Phase 1: Foundational Consistency & Awareness (Est. 1-2 Weeks) - ✅ COMPLETED

**Goal:** Establish shared state management and context awareness.

**Tasks:**

1.  **1.1. Design and Implement Configuration File Loading:** ✅ COMPLETED
    *   Define configuration file location (cross-platform). ✅
    *   Choose a simple format (e.g., INI-like `key=value`). ✅
    *   Implement parsing logic to load settings at startup. ✅
    *   Initial Settings: `language`, `default_layout`, `history_file_path`. ✅
    *   Modify `init_locale`, `get_history_file`, etc., to use loaded settings. ✅
2.  **1.2. Implement Robust History File Sharing:** ✅ COMPLETED 
    *   Add file locking (`fcntl`/`LockFileEx`) to `read_history` and `write_history` in `history.c` to prevent corruption from concurrent access. ✅
3.  **1.3. Implement Alias Loading/Saving:** ✅ COMPLETED
    *   Decide whether to store aliases in the main config or a separate file (e.g., `~/.arbsh_aliases`). ✅
    *   Implement functions to load aliases at startup and save them on exit (or via an `alias -s` command). ✅
    *   Update `_myalias` command. ✅
4.  **1.4. Implement GUI Host Detection:** ✅ COMPLETED
    *   Define an environment variable (e.g., `ARBSH_HOSTED_BY_GUI=1`). ✅
    *   Modify `process_manager.c` (`create_shell_process`) to set this variable for child processes. ✅
    *   Modify shell startup (`shell_entry.c`/`hsh`) to check for this variable. ✅
    *   Adapt behavior when hosted (e.g., potentially disable interactive prompt printing). ✅

## Phase 2: Enhancing Console Experience & Parity (Est. 1-2 Weeks) - 🟡 IN PROGRESS

**Goal:** Improve the standalone console's usability and visual consistency.

**Tasks:**

1.  **2.1. Ensure Consistent BiDi Application:** 🟡 IN PROGRESS
    *   Review *all* console output functions (`_puts`, `print_error`, `print_list`, etc.) and ensure they use `_puts_utf8` or equivalent BiDi-aware logic when `get_language()` indicates Arabic.
    *   Investigate if BiDi processing is needed/feasible for the command line *input* buffer in console mode.
2.  **2.2. Implement Console Theming via Config:** 🟡 PARTIALLY IMPLEMENTED
    *   Define abstract color names (e.g., `color_prompt`, `color_error`, `color_output`).
    *   Add these settings to the configuration file loader.
    *   Modify output functions (`print_prompt_utf8`, `print_error`) to use loaded colors to generate VT escape sequences for the console. Provide sensible defaults.
3.  **2.3. Review Standard Library Usage:** ⚪ NOT STARTED
    *   Evaluate custom functions (`_strlen`, `_strcpy`, `shell_strdup`, `_realloc`) vs. standard library equivalents.
    *   Replace with standard functions where appropriate unless a specific documented reason exists for the custom implementation.

## Phase 3: Deeper Integration & Refactoring (Longer Term / Optional)

**Goal:** Evaluate and potentially implement major architectural changes for tighter integration.

**Tasks:**

1.  **3.1. Feasibility Study: `libArbShCore`:** ⚪ NOT STARTED
    *   Analyze effort required to refactor core C logic into a shared library.
    *   Define a potential C API.
    *   Evaluate pros/cons vs. the current process-per-tab model for the GUI.
2.  **3.2. Implement Console -> GUI Commands (If Needed):** ⚪ NOT STARTED
    *   Design and implement IPC mechanism (named pipes, sockets).
    *   Add command-line flags to `hsh.exe` to send commands to a running GUI instance.
    *   Implement command handling in the GUI process.

## Current Status and Progress Update

### Completed Features:
- Configuration system with INI-like file format
- Directory creation for config files
- Configuration command (`config`) for managing settings
- File locking for history to prevent corruption between GUI and console modes
- Alias loading and saving with persistence across sessions
- GUI host detection through environment variables
- Prompt and output behavior adapting based on GUI/Console mode

### Current Work:
- Enhancing bidirectional text support for improved Arabic text handling
- Implementing theming via configuration

### Next Steps:
1. Complete the review of all output functions to ensure proper BiDi handling
2. Implement color settings in the configuration system
3. Improve Arabic keyboard layout support
4. Enhance error reporting with consistent UTF-8 handling

---