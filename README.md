# ArbSh Project - Refactoring Status

**Note:** This project is currently undergoing a major refactoring. The goal is to transition from the original C-based implementation to a new **C#/.NET PowerShell-inspired shell** with first-class support for **Arabic commands and full Arabic text handling (UTF-8, BiDi/RTL)**.

This document provides an overview of the new direction and the status of the refactoring effort. The original C implementation has been moved to the `old_c_code/` directory for reference.

## New Project Overview

The refactored ArbSh aims to be a modern, cross-platform command-line shell built on C# and .NET. Inspired by PowerShell's object pipeline architecture, it will offer a powerful and extensible environment.

A primary and distinguishing feature is its **native support for the Arabic language**:

1. **Arabic Commands:** Users will be able to write and execute commands using Arabic script (e.g., `احصل-محتوى` instead of `Get-Content`).
2. **Full Arabic Text Handling:** Leveraging ported logic from the original ArbSh, it will correctly handle UTF-8 encoding, complex bidirectional text (UAX #9 BiDi algorithm), and Right-to-Left (RTL) rendering in the console.
3. **Object Pipeline:** Commands (cmdlets) will operate on rich .NET objects, enabling more powerful scripting and data manipulation than traditional text-based shells.
4. **Cross-Platform:** Targeting .NET Core / .NET 5+ ensures compatibility across Windows, macOS, and Linux.

This shell aims to provide a seamless and powerful command-line experience for Arabic-speaking users and developers.

## Current Refactoring Status (v0.7.6 - Targeting v0.8.0)

The project is in the **advanced stages of C# refactoring (Phase 4 in progress, focusing on BiDi algorithm testing and refinement; Phase 5 is next):**

- **C# Project Structure:** A .NET solution (`src_csharp/ArbSh.sln`) and console application project (`src_csharp/ArbSh.Console/`) are established.
- **Core Object Pipeline:** Classes for the object pipeline (`PipelineObject`, `CmdletBase`), parsing (`Parser`, `ParsedCommand`), and execution (`Executor`) are functional.
- **REPL:** A functional Read-Eval-Print Loop exists in `Program.cs`.
- **Advanced Parsing (`Parser.cs` with `RegexTokenizer.cs`):**
  - Tokenizes input respecting double quotes (`"..."`) and single quotes (`'...'`).
  - Handles general escape characters (`\`) including standard C-style escapes (`\n`, `\t`, `\"`, `\\`, `\$`) within double-quoted strings. Single-quoted strings remain literal.
  - Identifies separate statements using semicolons (`;`) respecting quotes/escapes.
  - Identifies pipeline stages using pipes (`|`) respecting quotes/escapes.
  - Handles variable expansion (e.g., `$testVar`) within arguments, including concatenation with adjacent literals.
  - Parses redirection operators:
    - Output: `>`, `>>`
    - Error: `2>`, `2>>`
    - Input: `<`
    - Stream Merging: `>&1`, `>&2` (and variants like `2>&1`)
  - Parses sub-expressions `$(...)` recursively, storing the inner command structure.
  - Parses type literals `[...]` (e.g., `[int]`, `[string]`) as distinct argument tokens.
- **Parameter Binding (`Executor.cs`, `ParameterAttribute.cs`):**
  - Dynamic parameter binding using reflection and `[ParameterAttribute]`.
  - Supports named parameters, positional parameters, and boolean switches.
  - Handles mandatory parameter checks and basic type conversion.
  - Supports binding remaining positional arguments to array parameters.
  - Recognizes `[ArabicName]` attribute on cmdlet properties for Arabic parameter aliases (e.g., `-الاسم`).
- **Command Discovery (`CommandDiscovery.cs`):**
  - Finds cmdlets via reflection based on class name (e.g., `GetHelpCmdlet` -> `Get-Help`).
  - Recognizes `[ArabicName]` attribute on cmdlet classes for Arabic command aliases (e.g., `احصل-مساعدة`).
- **Concurrent Pipeline Execution (`Executor.cs`):**
  - Uses `Task` and `BlockingCollection<PipelineObject>` to execute pipeline stages concurrently within each statement.
  - `CmdletBase` handles pipeline input binding via `ValueFromPipeline` and `ValueFromPipelineByPropertyName` attributes.
- **Basic Cmdlets Implemented:**
  - `Write-Output`: Functional with pipeline and parameter input.
  - `Get-Help`: Displays detailed parameter info (including pipeline acceptance and Arabic names) and handles "command not found" errors.
  - `Get-Command`: Outputs structured `CommandInfo` objects.
  - `Test-Array-Binding`: For testing array parameter binding.
- **Redirection Execution (`Executor.cs`):**
  - Handles file redirection for stdout (`>`, `>>`), stderr (`2>`, `2>>`), and stdin (`<`).
  - Implements stream merging (`2>&1`, `1>&2`).
- **BiDi Algorithm Porting (`I18n/BidiAlgorithm.cs`):**
  - The core logic for UAX #9 (determining character types, resolving embedding levels via explicit formatting codes, reordering runs per Rule L2) has been ported from the C implementation. *(Integration with console rendering is Phase 5)*.
- **Unit Testing:** `GetCharType` is now extensively unit-tested. Initial unit tests for `ProcessRuns` (handling explicit formatting codes and run segmentation) have been added; this part is under active debugging and refinement.
  - *(Integration with console rendering is Phase 5)*.
- **Encoding:** UTF-8 input/output encoding issues resolved for console and redirected streams.
- **Documentation:** Core documentation (`README.md`, `ROADMAP.md`, `CHANGELOG.md`, etc.) updated for C# refactoring.
- **C Code Reference:** Original C source code moved to `old_c_code/`.

*The shell can be built and run via `dotnet run`. It parses complex commands with quotes, escapes, variables, pipelines, statements, redirection (input, output, error, merging), sub-expressions, and type literals. It discovers and executes cmdlets (including Arabic aliases) concurrently, with parameter binding (including pipeline input and Arabic aliases). The executor handles all parsed redirection types. The core BiDi algorithm is ported. Key pending items include **sub-expression execution**, **using type literals for casting/binding**, robust type conversion, advanced error handling, and **full Arabic text rendering in the console by integrating the ported BiDi logic (Phase 5)**.*

## New Code Structure

- **`src_csharp/`**: Contains the new C#/.NET solution and projects.
  - `ArbSh.Console/`: The main console application executable.
  - `ArbSh.Console/Commands/`: Directory for cmdlet implementations.
  - `ArbSh.Console/Parsing/`: Directory for tokenizer and token definitions.
  - `ArbSh.Console/I18n/`: Directory for internationalization code, including `BidiAlgorithm.cs`.
  - `ArbSh.Console/Models/`: Directory for data model classes (e.g., `CommandInfo`).
- **`old_c_code/`**: Contains the original C/C++ source, headers, tests, and CMake build files, preserved for reference.
- **`docs/`**: Project documentation (updated for C#).
- **`.gitignore`**: Updated for C#.

The project uses the standard .NET build tools (`dotnet build`).

## Creating a Release

A PowerShell script (`create-release.ps1`) is available in the project root to automate the creation of a release build and archive.

1. Open PowerShell in the project root directory.
2. Run the script, providing the desired version number:

    ```powershell
    .\create-release.ps1 -Version "x.y.z"
    ```

    (Replace `x.y.z` with the actual semantic version number, e.g., "0.8.0").

This will update the changelog, create a self-contained `win-x64` release build for `net9.0` (as per script configuration), and package it into a zip file in the `releases/` directory.

## Roadmap Overview

Please refer to the updated `ROADMAP.md` for the detailed phases of the C# refactoring and feature implementation plan. The project is currently focused on completing Phase 4 (execution enhancements) and moving into Phase 5 (console I/O with BiDi rendering).

## Technical Challenges (Focus)

- **Console Rendering with BiDi:** The next major challenge is integrating the ported BiDi algorithm (`BidiAlgorithm.cs`) with .NET console output mechanisms (`System.Console` or potentially libraries like `Spectre.Console`) to achieve correct visual rendering of RTL and mixed-direction text (Phase 5).
- **Sub-expression Execution:** Implementing the execution logic for parsed `$(...)` sub-expressions.
- **Type Literal Utilization:** Using parsed type literals `[...]` for actual type casting or parameter validation.

## Known Issues and Limitations (Current State - Targeting v0.8.0)

- **Parsing/Tokenization:**
  - While generally robust, edge cases with extremely complex mixed-script identifiers or highly nested structures might require further testing.
  - Handling of escape characters outside quoted strings (e.g., `Argument\ WithSpace`) is basic and might need refinement for more complex shell scripting scenarios.
- **Execution:**
  - **Sub-expression Execution (`$(...)`)**: Parsing is implemented, but the execution logic (running the inner commands and substituting their output) is **not yet implemented**. The parsed structure is passed as an argument object.
  - **Type Literal Usage**: Parsed (e.g., `[int]`), but **not yet used for casting parameter values or for type validation** during binding.
  - No external process execution support yet (e.g., running `git`, `notepad`).
- **BiDi / Rendering:**
  - The ported BiDi algorithm logic (`BidiAlgorithm.cs`) correctly processes levels and reorders runs based on the original C implementation's logic. However, it requires **comprehensive unit testing** and potential refinement against UAX #9 for full compliance if the C version was simplified.
  - Unit tests for `GetCharType` are complete. `ProcessRuns` is currently being unit tested and debugged to ensure correct segmentation and level assignment, especially in complex scenarios with explicit formatting codes.
  - **Integration with console rendering (Phase 5) is needed to actually display text visually reordered according to BiDi rules.** Currently, output is sent to the console as-is without visual reordering.
- **Parameter Binding:**
  - Type conversion relies on `TypeConverter` and `Convert.ChangeType`, which handles common types but not complex types like script blocks or hashtables (as in PowerShell).
  - Does not yet support named array parameters (e.g., `-Names "a","b"`) or advanced pipeline binding scenarios (e.g., binding specific properties of complex input objects to parameters without `ValueFromPipelineByPropertyName`).
  - Unknown named parameters are currently ignored by the binder instead of causing an error.
- **Error Handling:**
  - Basic error handling exists (exceptions are caught, `PipelineObject.IsError` flag is used for some cmdlet errors like "command not found").
  - Lacks rich `ErrorRecord` objects or sophisticated error reporting mechanisms found in shells like PowerShell.
- **Features:**
  - Arabic command and parameter name support is implemented via the `[ArabicName]` attribute.
  - **Full Arabic text rendering (visual BiDi/RTL display in the console) is not yet implemented (pending Phase 5).**
  - No scripting features (variables are hardcoded in Parser for testing, no user-defined variables, functions, flow control, etc.).
  - No tab completion, interactive command history (beyond basic OS terminal history), or shell aliasing features.

## Conclusion

ArbSh is making strong progress in its transformation into a modern, PowerShell-inspired C# shell with a unique emphasis on first-class Arabic language support. The core parsing and concurrent execution engine is robust, and the foundational BiDi logic has been successfully ported. The immediate next steps involve implementing sub-expression execution, followed by the critical Phase 5 task of integrating the BiDi algorithm with console rendering to bring true Arabic display capabilities to the shell.
