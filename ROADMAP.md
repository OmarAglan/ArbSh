# ArbSh Development Roadmap

**Current Version:** 0.8.1-alpha (Phase 6 Kickoff)
**Status:** Phase 6 In Progress - File Management & Installer Integration
**Next Phase:** Phase 6 - Baa Language & External Process Integration

This roadmap outlines the development phases for ArbSh - an Arabic-first command-line shell built on C#/.NET with PowerShell-inspired architecture and full Unicode BiDi compliance. 

## 🎯 Project Vision

ArbSh aims to be the premier Arabic-first shell environment and the ultimate companion terminal for the **Baa (لغة باء)** programming language. By building a custom hardware-accelerated GUI terminal, we bypass legacy console limitations to provide a flawless, Right-To-Left environment that:

- **Provides Native Arabic Support:** Commands and parameters in Arabic script, rendered perfectly.
- **Hosts the Baa Compiler:** Serves as the official environment to correctly execute and display the Arabic output of the Baa language compiler.
- **Guarantees Unicode BiDi Compliance:** Full UAX #9 bidirectional text algorithm implementation.
- **Features an Object Pipeline:** PowerShell-inspired object-based command pipeline.
- **Is Cross-Platform:** Avalonia UI / .NET-based compatibility (Windows, macOS, Linux).

## 📋 Development Phases

### ✅ Phase 1: Project Foundation (Completed)
**C# Project Setup, Core Object Pipeline Design, Documentation**

-[✅] C#/.NET solution and console project structure
- [✅] Core pipeline classes (`PipelineObject`, `CmdletBase`)
- [✅] Command discovery framework (`CommandDiscovery.cs`)
- [✅] Project documentation updates
- [✅] Git configuration for C# development

### ✅ Phase 2: Core Shell Framework (Completed)
**Basic Cmdlet Framework & Execution Engine**

- [✅] REPL (Read-Eval-Print Loop) implementation
- [✅] Advanced parser with quote handling and escape sequences
- [✅] Reflection-based parameter binding with type conversion
- [✅] Task-based concurrent pipeline execution
- [✅] Core cmdlets: `اطبع`, `مساعدة`, `الأوامر`
- [✅] File redirection support (`>`, `>>`, `2>`, `2>>`)
- [✅] Stream merging (`2>&1`, `1>&2`)
- [✅] Pipeline input binding (`ValueFromPipeline`)

### ✅ Phase 3: Advanced Parsing & Tokenization (Completed)
**Regex-Based Tokenizer with Arabic Support**

-[✅] Regex-based tokenizer replacing state machine approach
- [✅] Token type system (`TokenType` enum, `Token` struct)
- [✅] Advanced redirection parsing (input `<`, stream merging)
- [✅] Variable expansion with concatenation (`$var`)
- [✅] Subexpression parsing `$(...)` (recursive)
-[✅] Type literal parsing `[TypeName]`
- [✅] UTF-8 encoding resolution for Arabic text
- [✅] Arabic command name support via `[ArabicName]` attributes

### ✅ Phase 4: BiDi Algorithm UAX #9 Compliance & Advanced Execution (Completed)
**Full Unicode BiDi Algorithm Implementation & Advanced Shell Features**

- [✅] **P Rules:** Paragraph embedding level determination (P2-P3)
- [✅] **X Rules:** Complete explicit formatting code handling (X1-X10)
- [✅] **W Rules:** Complete weak type resolution implementation (W1-W7)
- [✅] **N Rules:** Bracket pair processing and neutral type resolution (N0-N2)
- [✅] **I Rules:** Implicit embedding level assignment for strong types (I1-I2)
- [✅] **L Rules:** Complete level-based reordering implementation (L1-L4)
- [✅] **Subexpression Execution:** `$(...)` command substitution
- [✅] **Type Literal Utilization:** `[TypeName]` type casting
- [✅] **Testing:** 70+ Unicode BidiTest.txt compliance tests passing

### ✅ Phase 5: The Custom GUI Terminal (COMPLETED)
**Abandoning legacy console limitations to build a standalone, hardware-accelerated GUI terminal using Avalonia UI.**

#### 5.1 GUI Framework Architecture
- [x] **Avalonia UI Setup:** Create the new `ArbSh.Terminal` graphical project.
- [x] **Decouple Executor:** Refactor `Executor.cs` to output to a Stream/Event system instead of `System.Console.WriteLine`.
- [x] **Typography:** Embed a high-quality Arabic coding font (e.g., Cascadia Code Arabic, Kashida) as the default terminal font.

#### 5.2 Native Text Rendering & Shaping
- [x] **HarfBuzz Integration:** Leverage Avalonia's Skia/HarfBuzz backend for pixel-perfect Arabic character shaping and ligatures.
- [x] **Visual vs. Logical Mapping:** Keep shell state in logical order and delegate visual BiDi/shaping to Avalonia text layout at the rendering boundary.
- [x] **Color & Theming Engine:** Implement a modern dark theme with ANSI escape sequence parsing for colored output.

#### 5.3 RTL Input & Cursor Management (The Core Blocker Solved)
- [x] **True RTL Cursor Positioning:** Implement a cursor that logically navigates RTL text correctly (bypassing legacy Windows `conhost` bugs).
- [x] **Input Buffer Management:** Handle keyboard events directly from the OS GUI, completely avoiding console encoding corruptions.
- [x] **RTL Prompt:** Pin the prompt (e.g., `أربش< `) cleanly to the right side of the window.

#### 5.4 Terminal Emulator Features
- [x] **Scrollback Buffer:** Implement UI virtualization to handle thousands of lines of output efficiently.
- [x] **Clipboard Support:** BiDi-aware Copy/Paste (ensuring copied text pastes correctly into external editors in logical order).

### 🧠 Phase 6: Baa Language & External Process Integration
**Ensuring ArbSh is the perfect host environment for the Baa compiler and general external processes.**

#### 6.1 Hosting the Baa Compiler
- [ ] **Compiler Output Rendering:** Ensure the terminal flawlessly displays the Arabic stdout/stderr produced by the Baa compiler.
- [ ] **Script Execution:** Support executing `.baa` script files directly from the ArbSh command line (`ArbSh> شغل برنامج.baa`).
- [ ] **Baa Interactive Mode:** Support dropping the shell into a Baa REPL session with proper state preservation.

#### 6.2 General Process Management (Pseudo-TTY)
- [x] **Filesystem Built-ins:** Added Arabic file/directory commands (`انتقل`, `المسار`, `اعرض`) with session-scoped working directory behavior.
- [x] **Windows Installer Context Menu:** Added installer packaging scripts that register `Open in ArbSh` and pass `--working-dir` from Explorer.
- [ ] **External Commands:** Execute system commands (`git`, `dotnet`, `node`) *inside* the custom GUI terminal.
- [ ] **Process Pipeline:** Integrate external processes with the ArbSh object pipeline.
- [ ] **Stream Handling:** Correctly capture and route `stdin`, `stdout`, and `stderr` for background and foreground processes.
- [ ] **Arabic Path Support:** Handle Arabic file and directory names natively when launching external tools.

### 🔧 Phase 7: Advanced Shell & Developer UX (Future)
**Polishing the developer experience.**

#### 7.1 Interactive UX
- [ ] **IntelliSense & Tab Completion:** Arabic-aware predictive text and auto-completion for commands, paths, and arguments.
- [ ] **Command History:** Persistent history (`سجل`) navigated with Up/Down arrows.
- [ ] **Multiline Input:** Support for writing control blocks or functions over multiple lines before execution.

#### 7.2 Advanced Scripting
- [ ] **User Variables:** Dynamic variable creation and management.
- [ ] **Functions:** User-defined function support with Arabic names.
- [ ] **Flow Control:** if/else, loops, switch statements with Arabic keywords.
- [ ] **Error Handling:** Beautiful, localized Arabic stack traces and error UI inside the terminal.

### 🌍 Phase 8: Deployment & Ecosystem (Future)
**Releasing the ArbSh/Baa environment to the world.**

- [ ] **Cross-Platform Binaries:** Package as standalone `.exe`/`.app` for Windows, Linux, and macOS (no runtime installation required).
- [ ] **Baa Package Manager (BPM) Interface:** Integrate commands to download and install Baa libraries from a central repository.
- [ ] **IDE Integration:** Visual Studio Code extension compatibility for ArbSh scripts.
- [ ] **Documentation Portal:** Generate an Arabic website documenting the shell features.

## 📊 Current Status Summary

### ✅ Phase 6 Kickoff Status (v0.8.1-alpha)
**Progress:** ArbSh entered Phase 6 with foundational file-management commands and installer integration while preserving the Phase 5 GUI baseline and logical/visual separation architecture.

**Completed This Cycle:**
- Extracted engine code into `src_csharp/ArbSh.Core`.
- Introduced host-output abstractions (`IExecutionSink`, `CoreConsole`, `ShellEngine`) to preserve the logical/visual split and remove hard console coupling.
- Added `src_csharp/ArbSh.Terminal` Avalonia bootstrap (App/MainWindow/ViewModel/custom surface) as the foundation for full GUI terminal rendering.
- Implemented a dedicated terminal rendering pipeline (`TerminalTextPipeline`, `TerminalLayoutEngine`) for visual reordering/shaping of output and prompt lines.
- Added runtime font fallback configuration for mixed Arabic/Latin terminal text.
- Added renderer-focused tests for logical/visual separation and frame layout behavior.
- Implemented `TerminalInputBuffer` with logical caret, selection, grapheme-safe deletion, and insertion-at-caret editing.
- Added visual caret navigation for mixed BiDi input using `TextLine` hit-testing APIs.
- Added input selection with mouse drag and keyboard extension, plus clipboard copy/cut/paste integration.
- Anchored the Arabic prompt to RTL flow with the final marker form `أربش< `.
- Implemented scrollback offset virtualization with mouse-wheel and PageUp/PageDown navigation while keeping prompt pinned.
- Added output-line selection and clipboard copy in logical-order text, alongside existing prompt-line clipboard editing.
- Bundled terminal font assets (`CascadiaMono.ttf`, `arabtype.ttf`) and switched render font chain to packaged-first fallback.
- Added full ANSI SGR parsing pipeline (16-color, 256-color, and truecolor) with span-based foreground/background styling.
- Added ArbSh navy theme/palette abstractions and applied ANSI-aware styling in output rendering without mutating logical text.
- Added tests for ANSI parser, ANSI palette mapping, and ANSI-aware terminal text pipeline behavior.
- Finalized Arabic-only command surface (`الأوامر`, `مساعدة`, `اطبع`, `اختبار-مصفوفة`, `اختبار-نوع`, plus host `اخرج`) and removed legacy command aliases.
- Added session-scoped working directory state and new file commands (`انتقل`, `المسار`, `اعرض`) with Arabic path support.
- Added terminal startup `--working-dir` handling to open ArbSh in a selected Explorer folder.
- Added Windows installer packaging scripts (`Install-ArbSh.ps1`, `Uninstall-ArbSh.ps1`) and release automation support for context-menu registration.

**Next Focus:** Begin Phase 6 by integrating external process execution (`git`, `dotnet`, `node`) into the GUI terminal stream model.

## 🌟 Project Philosophy

**ArbSh is designed as an Arabic-first shell for the Arabic developer community.** Our approach prioritizes:

- **Cultural Authenticity:** Built by Arabic developers for Arabic developers.
- **The Perfect Host for Baa:** Acting as the definitive visual and interactive environment for the Baa programming language.
- **Technical Excellence:** Modern C# architecture with strict Unicode UAX #9 compliance.
- **Innovation:** Bypassing 40-year-old legacy console constraints to pioneer true Arabic-native command-line computing via a modern UI framework.
