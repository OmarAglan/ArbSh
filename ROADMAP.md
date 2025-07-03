# ArbSh C#/.NET Refactoring Roadmap

This document outlines the planned development phases for refactoring ArbSh into a modern, cross-platform, PowerShell-inspired shell using C#/.NET, with first-class support for Arabic commands and text handling.

## Overall Goal

To create a powerful, extensible shell environment built on .NET that:

- Implements a PowerShell-like object pipeline.
- Allows commands and parameters to be written in Arabic script.
- Correctly handles and renders complex Arabic and mixed-direction text (UTF-8, BiDi/RTL) in the console, by porting proven logic from the original ArbSh C implementation and enhancing it for full UAX #9 compliance.
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

**Phase 4: BiDi Algorithm UAX #9 Compliance Enhancement, Advanced Execution Logic (In Progress)**

- **BiDi Algorithm Foundation & GetCharType Enhancement (Completed for v0.7.8):**
  - [✅] Carefully port the Unicode Bidirectional Algorithm (UAX #9) implementation from `src/i18n/bidi/bidi.c` to C#. *(Initial simplified logic ported to `I18n/BidiAlgorithm.cs`)*.
  - [✅] Integrate `ICU4N` library for Unicode Character Database property access.
  - [✅] Refactor `BidiAlgorithm.GetCharType(int codepoint)` to use `ICU4N` for determining UAX #9 `Bidi_Class`.
  - [✅] Update `BidiCharacterType` enum (add `BN`, align with UAX #9).
  - [✅] Update and expand unit tests for `BidiAlgorithm.GetCharType` to use ICU data; all `GetCharType` tests passing.

- **BiDi Algorithm UAX #9 Compliance - `ProcessRuns` and `ReorderRunsForDisplay` (Next Major BiDi Focus):**
  - The initial port provided foundational BiDi logic. This sub-phase focuses on enhancing it significantly to align more closely with the Unicode Bidirectional Algorithm (UAX #9) standard.
  - *`ProcessRuns` and `ReorderRunsForDisplay` currently use placeholder logic. The following tasks detail their full implementation.*
  - **Sub-Task 2.1 (Study & Design - X Rules):** ✅ COMPLETED
    - [✅] Thoroughly study UAX #9 rules X1-X10 (Explicit Levels and Directions).
    - [✅] Design the data structure for the "directional status stack" (needs to hold embedding level, override status, and isolate status).
    - **RESOLVED:** Implemented `DirectionalStatusStackEntry` struct with `EmbeddingLevel`, `OverrideStatus` (enum: Neutral/LeftToRight/RightToLeft), and `IsolateStatus` (bool) fields.
  - **Sub-Task 2.2 (Implement Basic Explicit Formatting - X1-X4, X6-X8, simplified X10 for LRE/RLE/LRO/RLO/PDF):** ✅ COMPLETED
    - [✅] Implement logic for LRE, RLE, PDF (X1, X2, X3, X7, X8).
    - [✅] Implement logic for LRO, RLO (X4, X5, X7, X8).
    - [✅] Ensure correct level calculation (e.g., `(current_level + 2) & ~1` for next even, `(current_level + 1) | 1` for next odd).
    - [✅] Implement initial handling for stack overflow (X9).
    - [✅] Implement removal of these explicit formatting codes from consideration for level assignment (part of X10, though full BN removal is later).
  - **Sub-Task 2.3 (Implement Isolates - X5a-X5c, X6a, remaining X10):** ✅ COMPLETED
    - [✅] Implement logic for LRI, RLI, FSI, PDI.
    - [✅] Correctly handle pairing of isolates (e.g., finding matching PDI or end of paragraph/higher isolate).
    - [✅] Correctly resolve levels within and of isolated sequences.
    - **RESOLVED:** FSI direction detection implemented via `DetermineFirstStrongDirection` method with nested isolate boundary respect and first-strong character scanning (L=LTR, AL/R=RTL, default=LTR).
  - **Sub-Task 2.4 (Study & Design - W Rules):**
    - [✅] Thoroughly study UAX #9 rules W1-W7 (Resolving Weak Types). *(Completed: Comprehensive design document created at `docs/BIDI_W_RULES_DESIGN.md`)*.
  - **Sub-Task 2.5 (Implement W Rules):**
    - [✅] Implement W1: NSM resolution based on previous character or sos. *(Completed: `ApplyW1_NonspacingMarks`)*.
    - [✅] Implement W2: Change EN to AN if preceded by AL. *(Completed: `ApplyW2_EuropeanNumberContext` with backward strong type search)*.
    - [✅] Implement W3: Change AL to R. *(Completed: `ApplyW3_ArabicLetterSimplification`)*.
    - [✅] Implement W4: Number separator resolution (ES between EN, CS between same types). *(Completed: `ApplyW4_NumberSeparators`)*.
    - [✅] Implement W5: European terminator sequences adjacent to EN. *(Completed: `ApplyW5_EuropeanTerminators`)*.
    - [✅] Implement W6: Remaining separators to Other Neutral. *(Completed: `ApplyW6_RemainingSeparators`)*.
    - [✅] Implement W7: Change EN to L in L context. *(Completed: `ApplyW7_EuropeanNumberFinal`)*.
    - **RESOLVED:** W rules implemented using isolating run sequence processing with proper sos/eos determination and integration with X rules. Enhanced I rules (I1-I2) added for final level assignment including AN support.
  - **Sub-Task 2.6 (Study & Design - N Rules):** ✅ COMPLETED
    - [✅] Thoroughly study UAX #9 rules N0-N2 (Resolving Neutral Types and BN). *(Completed: Comprehensive design document created at `docs/BIDI_N_RULES_DESIGN.md`)*.
  - **Sub-Task 2.7 (Implement N Rules):** ✅ COMPLETED
    - [✅] Implement N0: Bracket pair processing with BD16 algorithm using 63-element stack. *(Completed: `ApplyN0_BracketPairs` with hardcoded bracket mappings)*.
    - [✅] Implement N1: Neutral sequence resolution based on surrounding strong types. *(Completed: `ApplyN1_SurroundingStrongTypes` treating EN/AN as R)*.
    - [✅] Implement N2: Embedding direction fallback for remaining neutrals. *(Completed: `ApplyN2_EmbeddingDirection` with even→L, odd→R)*.
    - **RESOLVED:** N rules implemented with comprehensive bracket pair processing, canonical equivalence support, and proper integration with isolating run sequences. Added 4 new unit tests covering all N rules scenarios.
  - **Sub-Task 2.8 (Study & Design - I Rules):**
    - [ ] Thoroughly study UAX #9 rules I1-I2 (Resolving Implicit Levels).
  - **Sub-Task 2.9 (Implement I Rules):**
    - [ ] Implement I1: Resolve characters to paragraph embedding level if LTR.
    - [ ] Implement I2: Resolve characters to paragraph embedding level if RTL.
  - **Sub-Task 2.10 (Refine Run Segmentation Logic):**
    - [ ] Ensure `List<BidiRun>` is populated correctly based on changes in the *fully resolved* embedding level after X, W, N, and I rules.
    - [ ] Each `BidiRun` should contain the start index, length (in original string), and the final resolved embedding level.

  - **Sub-Task 3.1 (Study L Rules):**
    - [ ] Thoroughly study UAX #9 rules L1-L4 (Reordering Resolved Levels).
  - **Sub-Task 3.2 (Implement/Verify L1 - BN Removal in `ReorderRunsForDisplay`):**
    - [ ] Ensure that all characters that were resolved as BN (Boundary Neutral) by rule N0 (including explicit formatting codes like PDF, LRI, RLI, FSI, PDI and original BN characters) are removed from the string before or during reordering for display.
    - [ ] Also ensure LRE, RLE, LRO, RLO are removed as per X10 if not already handled as BN.
    - **Discussion Point:** Your current `ReorderRunsForDisplay` filters LRE-PDI. Does this cover all BN types correctly as per L1?
  - **Sub-Task 3.3 (Verify L2 - Run Reordering in `ReorderRunsForDisplay`):**
    - [ ] The existing logic (iterate `maxLevel` down to 0, append runs, reverse RTL runs using `StringInfo.ParseCombiningCharacters`) is a good basis for L2.
    - [ ] Verify its correctness against `BidiTest.txt` once `ProcessRuns` provides accurate levels.
  - **Sub-Task 3.4 (Consider L3 - Combining Marks, L4 - Digit Shaping - Optional/Renderer for `ReorderRunsForDisplay`):**
    - [ ] L3 (reordering combining marks) is generally handled by grapheme cluster logic (`StringInfo.ParseCombiningCharacters` helps).
    - [ ] L4 (digit shaping) is typically a rendering engine task.
    - **Discussion Point:** Confirm we are not expected to implement L4.

  - **Sub-Task 4.1 (Optional - Study P Rules):**
    - [ ] Thoroughly study UAX #9 rules P1-P3 (Determining the Paragraph Embedding Level).
  - **Sub-Task 4.2 (Optional - Implement P2-P3 in `ProcessString`):**
    - [ ] Add logic to `ProcessString` (or a helper) to scan the input text for the first strong character (L, R, or AL) to determine the paragraph level as per P2 and P3.
    - [ ] Allow `ProcessString` to use this auto-detected level if an explicit `baseLevel` is not provided (e.g., by passing a sentinel value like -1 for `baseLevel`).

  - **Sub-Task 5.1 (Setup BidiTest.txt Framework for Comprehensive Testing):**
    - [ ] Develop a test runner or utility to parse `BidiTest.txt` (from Unicode.org). This file defines test cases with input, paragraph level, expected levels, and expected reordered output.
  - **Sub-Task 5.2 (GetCharType UCD Testing - Expansion):**
    - [ ] Further expand `GetCharType` tests to cover a wider range of UCD-defined `Bidi_Class` values, ensuring comprehensive coverage.
  - **Sub-Task 5.3 (Rule-Specific Testing for ProcessRuns):**
    - [ ] Create targeted unit tests for *each* X, W, N, and I rule or small groups of related rules, using crafted strings before tackling the full `BidiTest.txt`.
  - **Sub-Task 5.4 (BidiTest.txt Integration for ProcessRuns + ReorderRunsForDisplay):**
    - [ ] Feed test cases from `BidiTest.txt` into `ProcessString`.
    - [ ] Assert that the resolved levels for each character (intermediate step, if exposed for testing) match `BidiTest.txt`.
    - [ ] Assert that the final reordered string matches the expected output from `BidiTest.txt`.
  - **Sub-Task 5.5 (Edge Case Testing for BiDi):**
    - [ ] Add tests for empty strings, strings with only neutrals, strings with only formatting codes, strings exceeding `MaxEmbeddingDepth`.

- **UTF-8 and Encoding:**
  - [✅] BLOCKER RESOLVED: Resolved UTF-8 input/output encoding corruption when running via PowerShell `Start-Process` with redirected streams.
  - [✅] Standard .NET UTF-8 APIs deemed sufficient for general encoding/decoding.
  - [✅] Resolved UTF-8 encoding corruption issues when *capturing* C# process output externally.

- **Execution & Runtime Enhancements:**
  - [✅] Fix erroneous default redirection attempts in Executor (prevent `Value cannot be null` error when no redirection is specified).
  - [✅] Verify Tokenizer handling of Arabic/mixed-script identifiers and special characters (e.g., `:`) (Seems OK from tests).
  - [✅] Implement Executor logic for input redirection (`<`).
  - [✅] Implement Executor logic for stream redirection merging (`2>&1`, `>&2`).
  - [ ] Implement Executor logic for subexpression (`$(...)`) execution.
  - [ ] Implement utilization of parsed type literals `[...]` for parameter type conversion or validation.

**Phase 5: C# Console I/O with Integrated BiDi Rendering (Next Major Phase)**

- [ ] Implement console input reading in C# that handles potential complexities of RTL input (e.g., correct cursor movement during editing).
- [ ] Implement console output writing routines in C# that utilize the *fully implemented* BiDi algorithm (from Phase 4) to correctly shape and render mixed English/Arabic text to the console. This involves:
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
