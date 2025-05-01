# ArbSh Project - Refactoring Status

**Note:** This project is currently undergoing a major refactoring. The goal is to transition from the original C-based implementation to a new **C#/.NET PowerShell-inspired shell** with first-class support for **Arabic commands and full Arabic text handling (UTF-8, BiDi/RTL)**.

This document provides an overview of the new direction and the status of the refactoring effort. The original C implementation remains in the `src/` directory for reference, particularly its i18n components which are being ported to C#.

## New Project Overview

The refactored ArbSh aims to be a modern, cross-platform command-line shell built on C# and .NET. Inspired by PowerShell's object pipeline architecture, it will offer a powerful and extensible environment.

A primary and distinguishing feature is its **native support for the Arabic language**:

1.  **Arabic Commands:** Users will be able to write and execute commands using Arabic script (e.g., `احصل-محتوى` instead of `Get-Content`).
2.  **Full Arabic Text Handling:** Leveraging ported logic from the original ArbSh, it will correctly handle UTF-8 encoding, complex bidirectional text (UAX #9 BiDi algorithm), and Right-to-Left (RTL) rendering in the console.
3.  **Object Pipeline:** Commands (cmdlets) will operate on rich .NET objects, enabling more powerful scripting and data manipulation than traditional text-based shells.
4.  **Cross-Platform:** Targeting .NET Core / .NET 5+ ensures compatibility across Windows, macOS, and Linux.

This shell aims to provide a seamless and powerful command-line experience for Arabic-speaking users and developers.

## Current Refactoring Status

The project is in the **early stages** of the C# refactoring (Phase 1 complete, starting Phase 2):

-   **C# Project Structure:** A new .NET solution (`src_csharp/ArbSh.sln`) and console application project (`src_csharp/ArbSh.Console/`) have been created.
-   **Core Placeholders:** Initial classes for the object pipeline (`PipelineObject`, `CmdletBase`), parsing (`Parser`, `ParsedCommand`), and execution (`Executor`) are in place.
-   **Basic REPL:** A functional Read-Eval-Print Loop exists in `Program.cs`.
-   **Basic Parsing:** The parser (`Parser.cs`, `TokenizeInput`) can now handle basic double-quoted arguments, differentiate command names/arguments/parameters, and split commands based on the pipeline operator (`|`).
-   **Parameter Binding:** Added `ParameterAttribute` and implemented basic parameter binding in the `Executor` using reflection. Supports named/positional parameters and basic type conversion (e.g., string to bool for switches like `Get-Help -Full`).
-   **Command Discovery:** Added `CommandDiscovery` class to find available cmdlets via reflection based on class name convention (e.g., `GetHelpCmdlet` -> `Get-Help`). The `Executor` now uses this for dynamic cmdlet instantiation.
-   **Basic Pipeline:** `CmdletBase` now collects output internally. The `Executor` simulates sequential pipeline execution by passing output collections between cmdlets (verified with `Get-Command | Write-Output`).
-   **Placeholder Cmdlets:** Basic `Write-Output`, `Get-Help`, and `Get-Command` cmdlets have been added and are discoverable. Cmdlets use the `[Parameter]` attribute.
-   **Documentation:** Core documentation (`README.md`, `ROADMAP.md`, `PROJECT_ORGANIZATION.md`, etc.) has been updated to reflect the C# refactoring.
-   **C Code Reference:** The original C source code (`src/`, `include/`) is preserved as a reference for porting the i18n algorithms.

*The shell can now be built and run via `dotnet run`, providing a basic prompt. It can parse simple commands with quoted arguments and pipelines, discover cmdlets, perform basic parameter binding with simple type conversion, and simulate pipeline flow. However, advanced parsing (escaped quotes, robust pipeline handling), true concurrent pipeline execution, robust type conversion, error handling, and Arabic support are still needed.*

## New Code Structure

-   **`src_csharp/`**: Contains the new C#/.NET solution and projects.
    -   `ArbSh.Console/`: The main console application executable.
    -   (Future libraries for core logic, cmdlets, i18n ports will reside here).
-   **`src/`**: Contains the original C source code (preserved for reference, especially `src/i18n/` and `src/utils/`). Will likely be removed or archived once porting is complete.
-   **`include/`**: Original C headers (preserved for reference).
-   **`docs/`**: Project documentation (being updated).
-   **`tests/`**: Original C tests (preserved for reference, new C# tests needed).

The project will transition away from the CMake build system towards the standard .NET build tools (`dotnet build`).

## Roadmap Overview

Please refer to the updated `ROADMAP.md` for the detailed phases of the C# refactoring and feature implementation plan.

## Technical Challenges (Porting Focus)

-   **Porting BiDi Algorithm:** Accurately translating the complex UAX #9 logic from `src/i18n/bidi/bidi.c` to C# requires careful attention to detail.
-   **Console Rendering:** Integrating the ported BiDi logic with .NET console output mechanisms (`System.Console` or potentially libraries like `Spectre.Console`) to achieve correct RTL/mixed text rendering.
-   **Arabic Command Parsing:** Designing and implementing a robust parser in C# that correctly handles Arabic script alongside potential English keywords/parameters and object syntax.

## Known Issues and Limitations (Current State)

-   The C# shell is currently non-functional and in the very early stages of development.
-   No Arabic language support (commands or text rendering) is implemented in the C# version yet.
-   No object pipeline or cmdlet execution is implemented yet.

## Conclusion

ArbSh is embarking on an ambitious refactoring to become a modern, PowerShell-inspired C# shell with unique, first-class support for Arabic commands and text. The robust i18n logic from the original C implementation provides a strong foundation for the porting effort. Development is currently focused on establishing the core C# architecture and beginning the porting process.
