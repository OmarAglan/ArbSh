# Project Organization

## Overview

This document defines the current architecture of **ArbSh** as an Arabic-first shell and terminal platform.

**Current Version:** 0.8.0-alpha
**Status:** Phase 5 In Progress - Native Rendering Pipeline Active
**Next Phase:** Phase 5.3 - RTL Input & Cursor Mapping

ArbSh now uses a host-agnostic core engine with separate presentation hosts:
- `ArbSh.Core` contains parsing, execution, cmdlets, and BiDi/i18n logic.
- `ArbSh.Console` is the legacy/compatibility console host.
- `ArbSh.Terminal` is the new Avalonia GUI terminal host.

## Current Directory Structure

```text
ArbSh/
├── docs/
│   ├── CHANGELOG.md
│   ├── PROJECT_ORGANIZATION.md
│   ├── USAGE_EXAMPLES.md
│   ├── BIDI_P_RULES_DESIGN.md
│   ├── BIDI_X_RULES_DESIGN.md
│   ├── BIDI_W_RULES_DESIGN.md
│   ├── BIDI_N_RULES_DESIGN.md
│   ├── BIDI_I_RULES_DESIGN.md
│   └── BIDI_L_RULES_DESIGN.md
├── src_csharp/
│   ├── ArbSh.sln
│   ├── ArbSh.Core/
│   │   ├── Commands/
│   │   ├── Hosting/
│   │   ├── I18n/
│   │   ├── Models/
│   │   ├── Parsing/
│   │   ├── Executor.cs
│   │   ├── Parser.cs
│   │   └── ShellEngine.cs
│   ├── ArbSh.Console/
│   │   ├── I18n/
│   │   ├── ConsoleExecutionSink.cs
│   │   └── Program.cs
│   ├── ArbSh.Terminal/
│   │   ├── Rendering/
│   │   │   ├── TerminalSurface.cs
│   │   │   ├── TerminalTextPipeline.cs
│   │   │   ├── TerminalLayoutEngine.cs
│   │   │   └── TerminalRenderConfig.cs
│   │   ├── ViewModels/
│   │   ├── Models/
│   │   ├── App.axaml
│   │   ├── MainWindow.axaml
│   │   └── Program.cs
│   └── ArbSh.Test/
│       ├── BidiAlgorithmTests.cs
│       ├── BidiTestConformanceTests.cs
│       └── ArabicShapingTests.cs
├── ROADMAP.md
└── System.md
```

## Architecture Layers

| Layer | Project | Responsibility |
|---|---|---|
| Core Engine | `src_csharp/ArbSh.Core` | Parsing, tokenization, parameter binding, cmdlet execution, pipeline, BiDi logic. |
| Host Abstraction | `src_csharp/ArbSh.Core/Hosting` | `IExecutionSink` and sink-aware output boundary between logic and rendering. |
| Console Host | `src_csharp/ArbSh.Console` | CLI host loop, console-specific I/O, backward-compatible execution sink. |
| GUI Terminal Host | `src_csharp/ArbSh.Terminal` | Avalonia app, view models, rendering surface, RTL-first terminal UX. |
| Test Suite | `src_csharp/ArbSh.Test` | Unit and conformance tests for BiDi and core behavior. |

## Key Design Principles

1. **Logical vs Visual Split**: Logical command text and pipeline state remain in `ArbSh.Core`. Visual ordering/shaping belongs to host/rendering layers only.
2. **Arabic-First UX**: Arabic command aliases and UAX #9 compliance are first-class requirements.
3. **Host Independence**: Core execution must not directly write to `System.Console`; all output routes through execution sinks.
4. **Concurrency Safety**: Pipeline stages use task-based execution and `BlockingCollection<T>` patterns.
5. **MVVM for GUI**: Avalonia host follows view/viewmodel separation for terminal state and rendering.

## Build and Run

```powershell
# Build full solution
dotnet build src_csharp/ArbSh.sln

# Run console host
dotnet run --project src_csharp/ArbSh.Console

# Run Avalonia terminal host
dotnet run --project src_csharp/ArbSh.Terminal

# Run tests
dotnet test src_csharp/ArbSh.Test
```

## Current Phase Snapshot

### Completed in Phase 5.1
- Extracted shared engine into `ArbSh.Core`.
- Moved cmdlets/models/parser/executor/BiDi code into the core project.
- Added sink-based host boundary (`IExecutionSink`, `CoreConsole`, `ShellEngine`).
- Added Avalonia terminal bootstrap project (`ArbSh.Terminal`).

### Completed in Phase 5.2 (Output + Prompt Rendering)
- Added a terminal rendering pipeline that converts logical text to visual display text at the UI boundary only.
- Implemented `TerminalTextPipeline` and `TerminalLayoutEngine` for line shaping/reordering and frame composition.
- Refactored `TerminalSurface` to consume draw instructions instead of performing ad-hoc reordering logic inline.
- Added runtime Arabic/Latin font fallback configuration for terminal text rendering.

### In Progress
- Phase 5.3 logical-to-visual cursor index mapping and RTL input navigation.
- Terminal virtualization for large scrollback.

## Notes on Documentation Locations

- Changelog is maintained in `docs/CHANGELOG.md`.
- Architecture and planning state live in `ROADMAP.md` and this document.
- BiDi algorithm changes must be reflected in the specific `docs/BIDI_*_RULES_DESIGN.md` files.
