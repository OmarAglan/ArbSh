# Development Tasks and Priorities

This document outlines the immediate and medium-term development tasks for the ArbSh project, prioritized by importance for supporting the Baa language ecosystem and improving usability.

## Immediate Priorities (1-3 Months)

### 1. Implement Terminal Emulation in GUI (Critical)

**Status:** Not Started
**Priority:** Critical

**Tasks:**

- [ ] Choose or implement a VT100/ANSI terminal emulation library/state machine (e.g., libtsm, vterm, or custom).
- [ ] Integrate emulator with `terminal_tab.c`: Feed stdout bytes from `read_shell_output` into the emulator.
- [ ] Modify `imgui_shell.cpp` (`RenderShell`): Render the terminal grid state (characters, attributes, cursor) from the emulator using ImGui primitives.
- [ ] Implement input translation: Convert ImGui keyboard/mouse events into appropriate byte sequences (including escape codes) for `terminal_tab_send_input`.
- [ ] Handle terminal resizing events, propagating size changes to the emulator and the child process (`resize_shell_terminal`).
- [ ] Ensure correct rendering of colors, text attributes (bold, underline).

**Rationale:** The GUI is currently unusable for interactive terminal applications without emulation. This is the highest priority for making the GUI functional. See `TERMINAL_EMULATION_ROADMAP.md`.

### 2. Strengthen Platform Abstraction Layer (PAL)

**Status:** Partial (Windows implemented, Unix stubs/missing)
**Priority:** High (for cross-platform GUI/Tabs)

**Tasks:**

- [ ] Create dedicated PAL headers/sources (e.g., `pal_process.h/c`, `pal_pipe.h/c`).
- [ ] Implement Unix/Linux equivalents for `process_manager.c` functions (`create_shell_process`, `read_shell_output`, `write_shell_input`, `is_shell_process_running`, `terminate_shell_process`, `resize_shell_terminal`) using `fork`, `exec`, `pipe`, `waitpid`, `kill`, `ioctl`.
- [ ] Refactor existing platform-specific code (`#ifdef WINDOWS` blocks outside `src/platform`) into the PAL.
- [ ] Ensure consistent error handling across platforms within the PAL.

**Rationale:** Enables the GUI and tab management features to potentially work on Linux/macOS.

### 3. Enhance GUI/Shell Integration

**Status:** Basic (stdin/stdout pipes only)
**Priority:** Medium (After Terminal Emulation)

**Tasks:**

- [ ] **Option A (Control Pipe):**
  - [ ] Design a simple IPC protocol (e.g., text-based commands over a named pipe/socket).
  - [ ] Modify `process_manager.c` and `terminal_tab.c` to create/manage the control pipe.
  - [ ] Modify core shell (`shell_loop.c` or elsewhere) to listen/respond to queries (e.g., "GET CWD", "GET ENV VAR").
  - [ ] Modify GUI (`imgui_shell.cpp`) to send queries and display results (e.g., show CWD in tab title or status bar).
- [ ] **Option B (Library Model - Major Refactor):**
  - [ ] Refactor core C shell logic into a shared library (`libArbShCore`).
  - [ ] Define a stable C API for the library.
  - [ ] Modify GUI to link the library and manage shell state instances directly (potentially per tab thread).

**Rationale:** Allows richer interaction between the GUI and the running shell instances beyond basic terminal I/O. Option A is less invasive.

### 4. GUI Testing Framework Enhancement

**Status:** Basic (`test_imgui` exists)
**Priority:** Medium

**Tasks:**

- [ ] Expand `test_imgui` or create new GUI tests.
- [ ] Develop strategies for testing ImGui interactions (may require mocking or specific test harnesses).
- [ ] Add tests for terminal tab creation, destruction, and communication.
- [ ] Implement tests for the (future) terminal emulation rendering.

**Rationale:** Ensures GUI stability and correctness as features are added.

## Medium-Term Priorities (3-6 Months)

### 5. Begin PowerShell-like Object Pipeline

**Status:** Not started
**Priority:** Medium

**Tasks:** (As previously listed)

- [ ] Design core object data structures
- [ ] Implement basic object creation/manipulation
- [ ] Create pipeline mechanism
- [...]

### 6. Enhanced Command Completion

**Status:** Basic implementation exists (Alias/Var replacement)
**Priority:** Medium

**Tasks:** (As previously listed)

- [ ] Add context-aware command completion (paths, commands)
- [ ] Support completion for Arabic commands/paths
- [...]

### 7. Cross-Platform UI Consistency (Console)

**Status:** Partial
**Priority:** Medium

**Tasks:** (As previously listed)

- [ ] Normalize console UI experience
- [ ] Add theming support with RTL awareness (console)
- [...]

### 8. Continuous Integration Setup

**Status:** Not started
**Priority:** Medium

**Tasks:** (As previously listed)

- [ ] Set up CI/CD pipeline (e.g., GitHub Actions)
- [ ] Implement automatic build/test for multiple platforms/configs
- [...]

## Completed Tasks (Recent)

- ✅ **Complete Bidirectional Text Implementation:** Full UAX #9 algorithm in `bidi.c`.
- ✅ **Enhance Arabic Text Input:** Keyboard layout command (`layout`), internal state tracking.
- ✅ **ImGui GUI Mode Implementation:** Functional ImGui GUI with process-based tabs on Windows.
- ✅ **Unified Entry Point:** `shell_entry.c` handles console/GUI startup.
- ✅ **Process Manager:** `process_manager.c` for creating/managing tab processes (Windows).
- ✅ **Terminal Tab Logic:** `terminal_tab.c` encapsulates tab state and process interaction.
- ✅ **Basic Testing Framework:** CTest setup, tests for UTF-8, BiDi, Keyboard state.

## Long-Term / Baa Integration / Documentation

(Priorities remain largely as previously listed, contingent on completing immediate/medium goals)

- Advanced Scripting Capabilities
- Remote Execution Features
- Plugin/Extension System
- Performance Optimization
- Baa Language Recognition & Tools
- Documentation Updates (Ongoing)

## Assignment and Tracking

Use the project's issue tracker (e.g., GitHub Issues) to create specific, actionable issues for these tasks, assign priorities, and track progress.
