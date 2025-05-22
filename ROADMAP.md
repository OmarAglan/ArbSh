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

- [✅] Create new C#/.NET solution (`src_csharp/ArbSh.sln`) and console project (`src_csharp/ArbSh.Console/`).
- [✅] Update `README.md` to reflect the new project direction.
- [✅] Update this `ROADMAP.md` file.
- [✅] Define core C# interfaces/base classes for the object pipeline (`PipelineObject`, `CmdletBase`).
- [✅] Design the basic structure for cmdlet discovery and loading (`CommandDiscovery.cs`).
- [✅] Update `.gitignore` for C# artifacts.
- [✅] Update `docs/PROJECT_ORGANIZATION.md`.

**Phase 2: Basic Cmdlet Framework & Execution Engine (C#) (Completed)**

- [✅] Basic REPL (Read-Eval-Print Loop) implementation in `ArbSh.Console`.
- [✅] Develop the initial C# parser (`Parser.cs`) for simple syntax, handling quoted args, parameters (`-`), and pipelines (`|`).
- [✅] Implement basic cmdlet parameter binding (`Executor.BindParameters`) using reflection and `[Parameter]` attribute.
- [✅] Refined Parameter Binding: Added basic type conversion (`TypeConverter`/`Convert.ChangeType`), mandatory parameter checks, and stricter boolean switch handling.
- [✅] Implement fundamental built-in cmdlets placeholders (`Write-Output`, `Get-Help`, `Get-Command`) with basic logic.
- [✅] Implement basic command discovery (`CommandDiscovery.cs`).
- [✅] Implement basic sequential pipeline execution simulation (`Executor.cs`). (Superseded)
- [✅] **Refine Parser (Basic):** Handled basic variable expansion (`$var`), statement separators (`;`), pipeline operators (`|`), and escape characters (`\`) respecting quotes.
- [✅] **Refine Pipeline Execution:** Implemented true concurrent execution using `Task` and `BlockingCollection` in `Executor.cs`.
- [✅] **Implement Cmdlet Logic:** Added functional logic to `Get-Help` (displaying detailed parameter info including pipeline), `Get-Command` (outputting `CommandInfo` objects), and `Write-Output` (handling pipeline/parameter input).
- [✅] **Further Refine Parameter Binding:** Improved error reporting for type conversion failures and added support for binding remaining positional arguments to array parameters. Added pipeline binding support (`ValueFromPipeline`, `ValueFromPipelineByPropertyName`) via `CmdletBase.BindPipelineParameters`.
- [✅] **Basic Error Handling:** Added `IsError` flag to `PipelineObject` and updated `GetHelpCmdlet` to use it.
- [✅] **Basic Executor Redirection:** Implemented handling for stdout (`>`, `>>`) and stderr (`2>`, `2>>`) file redirection in `Executor.cs`.

**Phase 3: Arabic Command Parsing & Tokenization (C#) - Regex Approach (Completed)**

- [✅] **Refactor Tokenizer using Regex:** Replace the state machine tokenizer with a Regex-based approach.
  - [✅] Define Token Types (`TokenType` enum) & `Token` struct in `Parsing/Token.cs`.
  - [✅] Implement `RegexTokenizer` Class/Method in `Parsing/RegexTokenizer.cs` with initial Regex patterns.
  - [✅] Integrate Tokenizer into Parser (`Parser.cs` now calls `RegexTokenizer.Tokenize`).
  - [✅] Refine Tokenizer Regex Patterns: Fixed patterns for stream redirection (`>&1`, `>&2`). Added input redirection (`<`). Added type literals (`[type]`). Added sub-expression `$(...)`.
  - [✅] Refine Redirection & Argument Parsing in `Parser.cs`: Logic updated to use `Token` objects and correctly parse all redirection types (including input `<`).
  - [✅] Testing and Verification: Basic functionality confirmed. **Encoding issues resolved.** **Variable expansion regression fixed.** Sub-expression parsing regression fixed. Escape sequence interpretation fixed.
- [✅] Implement mechanisms to map Arabic command names/parameters (Verified `[ArabicName]` attribute approach works).
- [✅] **Refine Parser (Advanced - Post-Tokenizer Refactor):**
  - [✅] Implement variable expansion logic within the argument parsing loop (using StringBuilder).
  - [✅] Implement parsing logic (using the new token stream) for sub-expressions `$(...)`. *(Parsing structure complete; execution is Phase 4)*.
  - [✅] Implement parsing logic for type literals `[int]`. *(Parsed as special argument; usage is later phase)*.
  - [✅] Re-verify complex escape sequence handling based on the new token stream. *(Interpretation of common escapes in double-quoted strings implemented)*.

**Phase 4: Porting ArbSh UTF-8 & BiDi Algorithms to C#, Advanced Execution Logic (Nearing Completion)**

- [✅] **BLOCKER RESOLVED:** Resolved UTF-8 input/output encoding corruption when running via PowerShell `Start-Process` with redirected streams.
- [✅] Systematically port the UTF-8 handling logic from `src/utils/utf8.c` to a C# utility class/module. *(Decision: Standard .NET UTF-8 APIs deemed sufficient, no direct port needed)*.
- [✅] Carefully port the Unicode Bidirectional Algorithm (UAX #9) implementation from `src/i18n/bidi/bidi.c` to C#. *(Core logic ported to `I18n/BidiAlgorithm.cs`; includes `GetCharType`, `ProcessRuns`, `ReorderRunsForDisplay`, `ProcessString`)*.
- [✅] Port supporting functions (e.g., character classification, string utilities) as needed from `src/utils/` and `src/i18n/`. *(Determined not necessary as .NET APIs cover needs)*.
- [🚧] Develop C# unit tests for the ported i18n logic:
  - [✅] Comprehensive unit tests for `BidiAlgorithm.GetCharType`.
  - [🚧] Unit tests for `BidiAlgorithm.ProcessRuns` (in progress, debugging level assignment with explicit codes).
  - [ ] Unit tests for `BidiAlgorithm.ReorderRunsForDisplay` and `BidiAlgorithm.ProcessString`.
- [ ] **Implement/Refine Phase 3 Execution & Runtime Enhancements:**
  - [✅] Resolved UTF-8 encoding corruption issues when *capturing* C# process output externally.
  - [✅] Fix erroneous default redirection attempts in Executor (prevent `Value cannot be null` error when no redirection is specified).
  - [✅] Verify Tokenizer handling of Arabic/mixed-script identifiers and special characters (e.g., `:`) (Seems OK from tests).
  - [✅] Implement Executor logic for input redirection (`<`).
  - [✅] Implement Executor logic for stream redirection merging (`2>&1`, `>&2`).
  - [ ] Implement Executor logic for subexpression (`$(...)`) execution.
  - [ ] Implement utilization of parsed type literals `[...]` for parameter type conversion or validation.

**Phase 5: C# Console I/O with Integrated BiDi Rendering (Next Major Phase)**

- [ ] Implement console input reading in C# that handles potential complexities of RTL input (e.g., correct cursor movement during editing).
- [ ] Implement console output writing routines in C# that utilize the ported BiDi algorithm (from Phase 4) to correctly shape and render mixed English/Arabic text to the console. This involves:
  - Applying `BidiAlgorithm.ProcessString()` to text before writing.
  - Ensuring the console (e.g., `System.Console` or a TUI library) correctly displays the pre-processed string.
- [ ] Investigate and potentially integrate with libraries like `System.Console` (with careful handling of its limitations) or more advanced TUI libraries (e.g., `Spectre.Console`, `Terminal.Gui`) ensuring BiDi compatibility and control over rendering.

**Phase 6: External Process Execution & IO Handling**

- [ ] Implement functionality to execute external commands and applications using `System.Diagnostics.Process`.
- [ ] Handle redirection of stdin, stdout, and stderr for external processes, respecting ArbSh redirection syntax.
- [ ] Ensure correct encoding (likely UTF-8) when interacting with external processes.
- [ ] Manage process lifetime and exit codes.

**Phase 7: Scripting, Error Handling, Advanced Features**

- [ ] Design and implement a basic scripting capability:
  - [ ] User-defined variables (session state).
  - [ ] Basic flow control (e.g., `if`, `foreach`).
  - [ ] Functions or script blocks.
- [ ] Implement robust error handling and reporting mechanisms (e.g., `ErrorRecord` objects similar to PowerShell).
- [ ] Develop features like tab completion (basic command/parameter completion).
- [ ] Implement command history (interactive history navigation and recall).
- [ ] Implement shell aliasing.
- [ ] Explore a module system for extending the shell with more cmdlets.
- [ ] Comprehensive cross-platform testing and refinement (Windows, Linux, macOS).

**Future Considerations:**

- GUI development (potentially using Avalonia UI or MAUI for cross-platform).
- Advanced terminal emulation features if a GUI is pursued.
- Integration with specific Baa language tools (if still relevant).

This roadmap provides a high-level overview. Specific tasks within each phase will be refined as development progresses.
