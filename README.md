# ArbSh Project - Refactoring Status

**Note:** This project is currently undergoing a major refactoring. The goal is to transition from the original C-based implementation to a new **C#/.NET PowerShell-inspired shell** with first-class support for **Arabic commands and full Arabic text handling (UTF-8, BiDi/RTL)**.

This document provides an overview of the new direction and the status of the refactoring effort. The original C implementation has been moved to the `old_c_code/` directory for reference.

## New Project Overview

The refactored ArbSh aims to be a modern, cross-platform command-line shell built on C# and .NET. Inspired by PowerShell's object pipeline architecture, it will offer a powerful and extensible environment.

A primary and distinguishing feature is its **native support for the Arabic language**:

1.  **Arabic Commands:** Users will be able to write and execute commands using Arabic script (e.g., `احصل-محتوى` instead of `Get-Content`).
2.  **Full Arabic Text Handling:** Leveraging ported logic from the original ArbSh, it will correctly handle UTF-8 encoding, complex bidirectional text (UAX #9 BiDi algorithm), and Right-to-Left (RTL) rendering in the console.
3.  **Object Pipeline:** Commands (cmdlets) will operate on rich .NET objects, enabling more powerful scripting and data manipulation than traditional text-based shells.
4.  **Cross-Platform:** Targeting .NET Core / .NET 5+ ensures compatibility across Windows, macOS, and Linux.

This shell aims to provide a seamless and powerful command-line experience for Arabic-speaking users and developers.

## Current Refactoring Status

The project is in the **early stages** of the C# refactoring (Phase 1 complete, Phase 2 in progress):

-   **C# Project Structure:** A new .NET solution (`src_csharp/ArbSh.sln`) and console application project (`src_csharp/ArbSh.Console/`) have been created.
-   **Core Placeholders:** Initial classes for the object pipeline (`PipelineObject`, `CmdletBase`), parsing (`Parser`, `ParsedCommand`), and execution (`Executor`) are in place.
-   **Basic REPL:** A functional Read-Eval-Print Loop exists in `Program.cs`.
-   **Basic Parsing:** The parser (`Parser.cs`, `TokenizeInput`) can now handle basic double-quoted arguments with escaped quotes/operators (`\"`, `\|`, etc.), differentiate command names/arguments/parameters, split commands based on the pipeline operator (`|`), recognize the command separator (`;`, only first statement processed), and detect basic output redirection (`>`, `>>`).
-   **Parameter Binding:** Added `ParameterAttribute` and implemented parameter binding in the `Executor` using reflection. Supports named/positional parameters, mandatory parameter checks (throwing `ParameterBindingException`), stricter boolean switch handling, and improved type conversion using `TypeConverter` / `Convert.ChangeType`.
-   **Command Discovery:** Added `CommandDiscovery` class to find available cmdlets via reflection based on class name convention (e.g., `GetHelpCmdlet` -> `Get-Help`). The `Executor` now uses this for dynamic cmdlet instantiation.
-   **Basic Pipeline:** `CmdletBase` now collects output internally. The `Executor` simulates sequential pipeline execution by passing output collections between cmdlets (verified with `Get-Command | Write-Output`).
-   **Basic Cmdlets:** `Write-Output`, `Get-Help`, and `Get-Command` have basic functional implementations. `Get-Help` uses reflection to display syntax/parameters based on `[Parameter]` attributes. `Get-Command` uses `CommandDiscovery`.
-   **Documentation:** Core documentation (`README.md`, `ROADMAP.md`, `PROJECT_ORGANIZATION.md`, etc.) has been updated to reflect the C# refactoring.
-   **C Code Reference:** The original C source code and build files have been moved to the `old_c_code/` directory for reference during the porting process.

*The shell can now be built and run via `dotnet run`, providing a basic prompt. It can parse simple commands with quoted arguments (including basic escapes), pipelines (`|`), command separators (`;`), and output redirection (`>`), discover cmdlets, perform parameter binding (with improved type conversion and mandatory checks), simulate pipeline flow, and execute basic `Get-Help` and `Get-Command`. However, advanced parsing (robust pipeline/separator/redirection handling, variable expansion), true concurrent pipeline execution, robust type conversion, comprehensive error handling, and Arabic support are still needed.*

## New Code Structure

-   **`src_csharp/`**: Contains the new C#/.NET solution and projects.
    -   `ArbSh.Console/`: The main console application executable.
    -   (Future libraries for core logic, cmdlets, i18n ports will reside here).
-   **`old_c_code/`**: Contains the original C/C++ source, headers, tests, and CMake build files, preserved for reference.
-   **`docs/`**: Project documentation (updated for C#).
-   **`.gitignore`**: Updated for C#.

The project now uses the standard .NET build tools (`dotnet build`). The CMake build system in `old_c_code/` is deprecated.

## Roadmap Overview

Please refer to the updated `ROADMAP.md` for the detailed phases of the C# refactoring and feature implementation plan.

## Technical Challenges (Porting Focus)

-   **Porting BiDi Algorithm:** Accurately translating the complex UAX #9 logic from `old_c_code/src/i18n/bidi/bidi.c` to C# requires careful attention to detail.
-   **Console Rendering:** Integrating the ported BiDi logic with .NET console output mechanisms (`System.Console` or potentially libraries like `Spectre.Console`) to achieve correct RTL/mixed text rendering.
-   **Arabic Command Parsing:** Designing and implementing a robust parser in C# that correctly handles Arabic script alongside potential English keywords/parameters and object syntax.

## Known Issues and Limitations (Current State)

-   Parsing logic is still basic (e.g., no robust handling of quoted pipeline operators, no variable expansion).
-   Pipeline execution is sequential, not concurrent.
-   Type conversion in parameter binding is limited.
-   Error handling is rudimentary.
-   No Arabic language support (commands or text rendering) is implemented in the C# version yet.

## Conclusion

ArbSh is embarking on an ambitious refactoring to become a modern, PowerShell-inspired C# shell with unique, first-class support for Arabic commands and text. The robust i18n logic from the original C implementation provides a strong foundation for the porting effort. Development is currently focused on establishing the core C# architecture and beginning the porting process.
