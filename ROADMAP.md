# ArbSh C#/.NET Refactoring Roadmap

This document outlines the planned development phases for refactoring ArbSh into a modern, cross-platform, PowerShell-inspired shell using C#/.NET, with first-class support for Arabic commands and text handling.

## Overall Goal

To create a powerful, extensible shell environment built on .NET that:
- Implements a PowerShell-like object pipeline.
- Allows commands and parameters to be written in Arabic script.
- Correctly handles and renders complex Arabic and mixed-direction text (UTF-8, BiDi/RTL) in the console, by porting proven logic from the original ArbSh C implementation.
- Is cross-platform (Windows, macOS, Linux).

## Development Phases

**Phase 1: C# Project Setup, Core Object Pipeline Design, Documentation Update (Completed)**

-   [✅] Create new C#/.NET solution (`src_csharp/ArbSh.sln`) and console project (`src_csharp/ArbSh.Console/`).
-   [✅] Update `README.md` to reflect the new project direction.
-   [✅] Update this `ROADMAP.md` file.
-   [✅] Define core C# interfaces/base classes for the object pipeline (`PipelineObject`, `CmdletBase`).
-   [✅] Design the basic structure for cmdlet discovery and loading (`CommandDiscovery.cs`).
-   [✅] Update `.gitignore` for C# artifacts.
-   [✅] Update `docs/PROJECT_ORGANIZATION.md`.

**Phase 2: Basic Cmdlet Framework & Execution Engine (C#) (Current Phase)**

-   [✅] Basic REPL (Read-Eval-Print Loop) implementation in `ArbSh.Console`.
-   [✅] Develop the initial C# parser (`Parser.cs`) for simple syntax, handling quoted args, parameters (`-`), and pipelines (`|`).
-   [✅] Implement basic cmdlet parameter binding (`Executor.BindParameters`) using reflection and `[Parameter]` attribute.
-   [✅] Refined Parameter Binding: Added basic type conversion (`TypeConverter`/`Convert.ChangeType`), mandatory parameter checks, and stricter boolean switch handling.
-   [✅] Implement fundamental built-in cmdlets placeholders (`Write-Output`, `Get-Help`, `Get-Command`) with basic logic.
-   [✅] Implement basic command discovery (`CommandDiscovery.cs`).
-   [✅] Implement basic sequential pipeline execution simulation (`Executor.cs`). (Superseded)
-   [✅] **Refine Parser (Basic):** Handled basic variable expansion (`$var`), statement separators (`;`), pipeline operators (`|`), and escape characters (`\`) respecting quotes.
-   [✅] **Refine Pipeline Execution:** Implemented true concurrent execution using `Task` and `BlockingCollection` in `Executor.cs`.
-   [✅] **Implement Cmdlet Logic:** Added functional logic to `Get-Help` (displaying detailed parameter info including pipeline), `Get-Command` (outputting `CommandInfo` objects), and `Write-Output` (handling pipeline/parameter input).
-   [✅] **Further Refine Parameter Binding:** Improved error reporting for type conversion failures and added support for binding remaining positional arguments to array parameters. Added pipeline binding support (`ValueFromPipeline`, `ValueFromPipelineByPropertyName`) via `CmdletBase.BindPipelineParameters`.

**Phase 3: Arabic Command Parsing & Tokenization (C#)**

-   [ ] Extend the C# parser and tokenizer to correctly identify and handle commands, parameters, and potentially string literals written in Arabic script.
-   [ ] Implement mechanisms to map Arabic command names (e.g., `احصل-محتوى`) to their corresponding C# cmdlet classes.
-   [ ] Handle potential ambiguities or interactions between Arabic and English syntax elements.
-   [ ] **Refine Parser (Advanced):** Implement more robust parsing for complex cases (e.g., advanced redirection `2>&1`, sub-expressions `$(...)`, type literals `[int]`, etc.).

**Phase 4: Porting ArbSh UTF-8 & BiDi Algorithms to C#**

-   [ ] Systematically port the UTF-8 handling logic from `src/utils/utf8.c` to a C# utility class/module.
-   [ ] Carefully port the Unicode Bidirectional Algorithm (UAX #9) implementation from `src/i18n/bidi/bidi.c` to C#. This requires thorough understanding and testing.
-   [ ] Port supporting functions (e.g., character classification, string utilities) as needed from `src/utils/` and `src/i18n/`.
-   [ ] Develop C# unit tests for the ported i18n logic.

**Phase 5: C# Console I/O with Integrated BiDi Rendering**

-   [ ] Implement console input reading in C# that handles potential complexities of RTL input.
-   [ ] Implement console output writing routines in C# that utilize the ported BiDi algorithm (from Phase 4) to correctly shape and render mixed English/Arabic text to the console.
-   [ ] Investigate and potentially integrate with libraries like `System.Console` or more advanced TUI libraries (e.g., `Spectre.Console`) ensuring BiDi compatibility.

**Phase 6: External Process Execution & IO Handling**

-   [ ] Implement functionality to execute external commands and applications using `System.Diagnostics.Process`.
-   [ ] Handle redirection of stdin, stdout, and stderr for external processes.
-   [ ] Ensure correct encoding (likely UTF-8) when interacting with external processes.

**Phase 7: Scripting, Error Handling, Advanced Features**

-   [ ] Design and implement a basic scripting capability.
-   [ ] Implement robust error handling and reporting mechanisms.
-   [ ] Develop features like tab completion, history, aliasing (potentially with Arabic support).
-   [ ] Explore module system for extending the shell with more cmdlets.
-   [ ] Cross-platform testing and refinement.

**Future Considerations:**

-   GUI development (potentially using Avalonia UI or MAUI for cross-platform).
-   Advanced terminal emulation features if a GUI is pursued.
-   Integration with specific Baa language tools (if still relevant).

This roadmap provides a high-level overview. Specific tasks within each phase will be refined as development progresses.
