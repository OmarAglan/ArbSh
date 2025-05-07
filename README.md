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
-   **Basic Parsing:** The parser (`Parser.cs`) can now:
    -   Tokenize input respecting double quotes (`"..."`).
    -   Handle general escape characters (`\`) both outside quotes and within double quotes (escaping `$`, `"`, `\`). Single quotes treat content literally.
    -   Identify separate statements using semicolons (`;`) respecting quotes/escapes.
    -   Identify pipeline stages using pipes (`|`) respecting quotes/escapes.
    -   **Tokenizer Refactoring:** Replaced the internal state-machine tokenizer with a new Regex-based tokenizer (`RegexTokenizer.cs`) using Unicode properties (`\p{L}`) for better handling of mixed-script identifiers and complex syntax elements like redirection operators.
    -   **Variable Expansion:** Fixed the regression; variables like `$testVar` are now correctly expanded and concatenated within arguments (e.g., `ValueIs:$testVar` works).
    -   **Advanced Redirection:** Detect and parse advanced redirection operators (`>`, `>>`, `2>`, `2>>`, `>&1`, `>&2`, etc.) and **input redirection (`<`)** using the new tokenizer.
    -   **Sub-expressions:** Recognize `$()` syntax, recursively parse the inner command(s), and store the result (a `List<ParsedCommand>`) as an argument object. *(Execution of sub-expressions is not yet implemented)*.
    -   **Type Literals:** Recognize type literals like `[int]`, `[string]`, `[System.ConsoleColor]` and parse them as distinct arguments (currently stored as prefixed strings like `"TypeLiteral:int"`). *(Using these for casting or binding is not yet implemented)*.
    -   **Escape Sequences:** Interpret standard escape sequences (`\n`, `\t`, `\"`, `\\`, `\$`, etc.) within double-quoted strings. Single-quoted strings remain literal.
-   **Parameter Binding:** Added `ParameterAttribute` (with pipeline support flags) and implemented parameter binding in the `Executor` using reflection. Supports named/positional parameters, mandatory parameter checks, boolean switches, basic type conversion (`TypeConverter`/`Convert.ChangeType`), improved error reporting for conversion failures, and binding remaining positional arguments to array parameters. **Update:** Parameter binder now also recognizes the `[ArabicName]` attribute on cmdlet properties, allowing parameters to be specified using assigned Arabic names (e.g., `-الاسم`).
-   **Command Discovery:** Added `CommandDiscovery` class to find available cmdlets via reflection based on class name convention (e.g., `GetHelpCmdlet` -> `Get-Help`). The `Executor` now uses this for dynamic cmdlet instantiation. **Update:** Command discovery now also recognizes the `[ArabicName]` attribute on cmdlet classes, allowing cmdlets to be invoked using assigned Arabic names (e.g., `احصل-مساعدة`).
-   **Concurrent Pipeline:** The `Executor` now uses `Task` and `BlockingCollection` to execute pipeline stages concurrently within each statement. `CmdletBase` includes logic (`BindPipelineParameters`) to handle binding pipeline input (`ValueFromPipeline`, `ValueFromPipelineByPropertyName`).
-   **Basic Cmdlets:** `Write-Output`, `Get-Help`, and `Get-Command` have functional implementations. `Get-Help` displays detailed parameter info (including pipeline acceptance) and now outputs an error object (`PipelineObject` with `IsError=true`) if a command is not found. `Get-Command` outputs structured `CommandInfo` objects.
-   **Executor Redirection:** The `Executor` now handles file redirection for stdout (`>`, `>>`) and stderr (`2>`, `2>>`). **Update:** Execution logic for **input redirection (`<`)** and **stream merging (`2>&1`, `1>&2`)** has been implemented.
-   **BiDi Algorithm Porting:** The core logic for determining character types, resolving embedding levels based on explicit formatting codes, and reordering runs (UAX #9 Rule L2) has been ported from the original C implementation to `I18n/BidiAlgorithm.cs`. *(Note: This is based on the C code's potentially simplified UAX #9 implementation and requires testing and integration with console rendering)*.
-   **Encoding Issues Resolved:** Fixed the persistent UTF-8 input/output encoding corruption when running via PowerShell `Start-Process` with redirected streams by adjusting C# input reading (`StreamReader`) and setting appropriate encodings in the test script (`StandardInput/Output/ErrorEncoding`). Arabic commands/parameters are now received and logged correctly in tests.
-   **Documentation:** Core documentation (`README.md`, `ROADMAP.md`, `PROJECT_ORGANIZATION.md`, `CHANGELOG.md`, etc.) has been updated to reflect the C# refactoring status.
-   **C Code Reference:** The original C source code and build files have been moved to the `old_c_code/` directory for reference during the porting process.

*The shell can now be built and run via `dotnet run`, providing a basic prompt. It uses a new Regex-based tokenizer. It can parse commands with quoted arguments (interpreting escapes in double quotes), pipelines (`|`), statement separators (`;`), variable expansion (`$var`), type literals (`[int]`), sub-expressions (`$(...)`), and standard redirection operators (`>`, `>>`, `2>`, `2>>`, `>&1`, `>&2`, `<`). It can discover cmdlets (including Arabic aliases via `[ArabicName]` attribute), perform parameter binding (including pipeline input, basic arrays, and Arabic parameter aliases via `[ArabicName]` attribute), execute pipeline stages concurrently, and run basic `Get-Help`, `Get-Command`, and `Write-Output` cmdlets. The executor handles stdout/stderr file redirection, input file redirection (`<`), and stream merging (`2>&1`, `1>&2`). The core BiDi algorithm logic is ported. However, **sub-expression execution**, **using type literals for casting/binding**, robust type conversion, comprehensive error handling (beyond basic exceptions and `IsError` flag), and full Arabic text rendering support (integrating the ported BiDi logic) are still needed.*

## New Code Structure

-   **`src_csharp/`**: Contains the new C#/.NET solution and projects.
    -   `ArbSh.Console/`: The main console application executable.
    -   (Future libraries for core logic, cmdlets, i18n ports will reside here).
-   **`old_c_code/`**: Contains the original C/C++ source, headers, tests, and CMake build files, preserved for reference.
-   **`docs/`**: Project documentation (updated for C#).
-   **`.gitignore`**: Updated for C#.

The project now uses the standard .NET build tools (`dotnet build`). The CMake build system in `old_c_code/` is deprecated.

## Creating a Release

A PowerShell script (`create-release.ps1`) is available in the project root to automate the creation of a release build and archive.

1.  Open PowerShell in the project root directory.
2.  Run the script, providing the desired version number:
    ```powershell
    .\create-release.ps1 -Version "x.y.z"
    ```
    (Replace `x.y.z` with the actual semantic version number).

This will update the changelog, create a self-contained `win-x64` build, and package it into a zip file in the `releases/` directory.

## Roadmap Overview

Please refer to the updated `ROADMAP.md` for the detailed phases of the C# refactoring and feature implementation plan.

## Technical Challenges (Porting Focus)

-   **Porting BiDi Algorithm:** Accurately translating the complex UAX #9 logic from `old_c_code/src/i18n/bidi/bidi.c` to C# requires careful attention to detail.
-   **Console Rendering:** Integrating the ported BiDi logic with .NET console output mechanisms (`System.Console` or potentially libraries like `Spectre.Console`) to achieve correct RTL/mixed text rendering.
-   **Arabic Command Parsing:** Designing and implementing a robust parser in C# that correctly handles Arabic script alongside potential English keywords/parameters and object syntax.

## Known Issues and Limitations (Current State - v0.8.0)

-   **Parsing/Tokenization:**
    -   Mixed-script identifiers (e.g., `Commandمرحبا`) need further verification/testing with the tokenizer.
    -   Handling of escapes outside quoted strings (e.g., `Argument\ WithSpace`) may need refinement.
-   **Execution:**
    -   **Sub-expression Execution (`$(...)`)**: Parsing is implemented, but execution is not. The parsed structure is passed as an argument object.
    -   **Type Literal Usage**: Parsed, but not yet used for casting or parameter binding.
    -   No external process execution support yet.
-   **BiDi / Rendering:**
    -   The ported BiDi algorithm logic requires unit testing and potentially refinement (as it's based on the C code's potentially simplified UAX #9 implementation).
    -   Integration with console rendering (Phase 5) is needed to actually display text correctly using the BiDi logic. Currently, output is not visually reordered.
-   **Parameter Binding:**
    -   Type conversion is basic (relies on `TypeConverter` and `Convert.ChangeType`, no complex type handling like script blocks or hashtables).
    -   Does not yet support named array parameters or advanced pipeline binding scenarios (e.g., binding specific properties of complex input objects without `ValueFromPipelineByPropertyName`).
    -   Unknown named parameters are currently ignored instead of causing an error.
-   **Error Handling:** Rudimentary (basic exceptions caught, `PipelineObject.IsError` flag added, but no rich `ErrorRecord` objects like PowerShell).
-   **Features:**
    -   Basic Arabic command and parameter name support is implemented via the `[ArabicName]` attribute. Full Arabic text rendering (BiDi/RTL) is not yet implemented.
    -   No scripting features (variables are hardcoded in Parser, no functions, flow control, etc.).
    -   No tab completion, history, or aliasing.

## Conclusion

ArbSh is embarking on an ambitious refactoring to become a modern, PowerShell-inspired C# shell with unique, first-class support for Arabic commands and text. The robust i18n logic from the original C implementation provides a strong foundation for the porting effort. Development is currently focused on establishing the core C# architecture and beginning the porting process.
