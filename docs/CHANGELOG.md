# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.8.0] - 2025-05-07

### Added
- **BiDi Algorithm:** Ported core BiDi algorithm logic (determining character types, resolving embedding levels via explicit codes, reordering runs per Rule L2) from the original C implementation to `I18n/BidiAlgorithm.cs`. Includes `GetCharType`, `ProcessRuns`, `ReorderRunsForDisplay`, and `ProcessString` methods. (Note: Based on simplified C logic, requires testing and rendering integration).
- **Executor:** Implemented execution logic for input redirection (`< file.txt`). Reads specified file and provides content as pipeline input to the first command.
- **Executor:** Implemented execution logic for stream redirection merging (`2>&1`, `1>&2`). Output/Error objects are now correctly routed to the target stream's file handle or console stream based on merge directives.

### Fixed
- Fixed build warnings related to unused variables in `Executor.cs`'s output handling logic.
- Fixed build warning related to empty switch block in `I18n/BidiAlgorithm.cs`.

### Changed
- Updated `README.md` and `ROADMAP.md` to reflect Phase 4 progress.

## [0.7.6] - 2025-05-07

### Added
- **Parser:** Added parsing support for type literals (e.g., `[int]`, `[string]`, `[System.ConsoleColor]`). Recognized via `TokenType.TypeLiteral` and stored as a special argument string (`"TypeLiteral:TypeName"`) in `ParsedCommand`.
- **Parser:** Added parsing support for input redirection (`< file.txt`). Recognized via `TokenType.Operator` (`<`) and stored in the new `InputRedirectPath` property of `ParsedCommand`.

### Fixed
- **Parser:** Correctly interpret standard escape sequences (`\n`, `\t`, `\"`, `\\`, `\$`, etc.) within double-quoted strings (`StringLiteralDQ`). Added `ProcessEscapesInString` helper method. Single-quoted strings remain literal.
- **Parser:** Fixed sub-expression (`$(...)`) parsing regression. Nested sub-expressions and pipelines within them are now parsed correctly without causing "unterminated" errors or incorrect token consumption.

### Changed
- Updated `README.md` and `ROADMAP.md` to reflect completion of Phase 3 parsing enhancements.

## [0.7.5] - 2025-05-06

### Fixed
- **Encoding Issues:** Resolved persistent UTF-8 input/output corruption when running via PowerShell `Start-Process` with redirected streams. This involved:
    - Setting `StandardInputEncoding`, `StandardOutputEncoding`, and `StandardErrorEncoding` to UTF-8 in `test_features.ps1`.
    - Modifying `Program.cs` to use `StreamReader` with explicit UTF-8 encoding for `Console.OpenStandardInput()` and ensuring `Console.OutputEncoding` is also UTF-8.
    - Arabic commands/parameters and output are now handled correctly in test scenarios.
- **Variable Expansion Regression:** Fixed the parser logic in `Parser.cs` to correctly handle variable expansion (`$var`) within arguments, ensuring adjacent tokens are concatenated properly (e.g., `ValueIs:$testVar` now works as expected). Implemented using a `StringBuilder` in the argument parsing loop.

### Changed
- Updated `README.md` and `docs/USAGE_EXAMPLES.md` to reflect the encoding and variable expansion fixes.
- Updated `ROADMAP.md` to mark the encoding blocker as resolved and the variable expansion fix as complete.
- **Tokenizer Refactoring:** Replaced the internal state-machine tokenizer with a new Regex-based tokenizer (`Parsing/RegexTokenizer.cs`) using Unicode properties (`\p{L}`) for potentially better handling of mixed-script identifiers and complex syntax elements. Created `Parsing/Token.cs` for token definitions. Integrated the new tokenizer into `Parser.cs`.
- **Parser Logic:** Updated redirection and argument/parameter parsing logic in `Parser.cs` to work with the new `List<Token>` structure.
- **Redirection Parsing:** Refined Regex patterns in `RegexTokenizer.cs` and parsing logic in `Parser.cs` to correctly identify and parse all standard redirection operators (`>`, `>>`, `2>`, `2>>`, `>&1`, `>&2`).
- **Executor Redirection Handling:**
    - Implemented handling for stdout file redirection (`>`, `>>`) in `Executor.cs`.
    - Implemented handling for stderr file redirection (`2>`, `2>>`) in `Executor.cs`.
    - Fixed null path error when no redirection was specified.
- **Error Handling:** Added `IsError` flag to `PipelineObject.cs` and updated `GetHelpCmdlet.cs` to use it for "command not found" errors.
- **Test Script:** Updated `test_features.ps1` to log temporary file contents line-by-line to avoid `Add-Content` stream errors. Changed default file encoding for `test_output.log` to UTF-8 with BOM.

### Known Issues / Regressions
- **Tokenizer:**
    - Input redirection operator `<` is not yet recognized.
    - Mixed-script identifiers (e.g., `Commandمرحبا`) need verification/improvement.
- **Parser:**
    - Sub-expression `$(...)` parsing is implemented, but execution is not.
    - Type literals `[int]` are not yet parsed.
- **Execution:**
    - Stream redirection merging (`2>&1`, `1>&2`) is parsed but not implemented in the Executor.
    - Sub-expression execution is not implemented.

## [0.7.0] - 2025-05-03

### Added

- **Arabic Command Name Support:**
  - Extended the parser (`Parser.cs`) to recognize Arabic letters as valid characters in command/argument tokens.
  - Added `ArabicNameAttribute.cs` to allow specifying an Arabic alias for a cmdlet class.
  - Updated `CommandDiscovery.cs` to read the `ArabicNameAttribute` and add both English and Arabic names to the command cache.
  - Cmdlets can now be invoked using either their English name (e.g., `Get-Help`) or their assigned Arabic name (e.g., `احصل-مساعدة`).
- **Arabic Parameter Name Support:**
  - Updated `ArabicNameAttribute` to be applicable to properties (parameters).
  - Modified the parameter binding logic in `Executor.cs` to check for `ArabicNameAttribute` on properties and attempt binding using the Arabic name first, falling back to the English name.
  - Cmdlet parameters can now be specified using either their English name (e.g., `-CommandName`) or their assigned Arabic name (e.g., `-الاسم`).
- **Testing:** Added tests to `test_features.ps1` for Arabic command/parameter aliases.

### Fixed
- **Parser Tokenization:** Corrected tokenizer logic (`Parser.cs`) to properly handle hyphens within command names (e.g., `Get-Command`) and dots within arguments/filenames (e.g., `temp_file.txt`), preventing incorrect splitting.
- **Redirection:** Resolved issue where output redirection (`>`, `>>`) failed due to incorrect filename parsing. Files are now created correctly. Added explicit `Flush()` calls in `Executor.cs` for robustness.
- **Test Script Encoding:** Fixed `test_features.ps1` to correctly send UTF-8 encoded input to the ArbSh process by writing bytes directly to the standard input stream, enabling proper testing of Arabic characters. Confirmed successful parsing and execution of Arabic aliases.

## [0.6.0] - 2025-05-02

### Changed

- **Pipeline Execution:** Refactored the `Executor` to use `System.Threading.Tasks.Task` and `System.Collections.Concurrent.BlockingCollection` for true concurrent execution of pipeline stages within a statement, replacing the previous sequential simulation. Each cmdlet now runs in its own task, improving potential parallelism. Includes basic error handling for task failures using `Task.WhenAll` and `AggregateException`.
- **Cmdlet Logic & Pipeline Binding:**
  - Enhanced `ParameterAttribute` to include `ValueFromPipeline` and `ValueFromPipelineByPropertyName` flags.
  - Added `BindPipelineParameters` virtual method to `CmdletBase` to handle binding pipeline input to parameters based on the new flags (called by Executor before `ProcessRecord`).
  - Updated `GetHelpCmdlet` to display pipeline input acceptance details for parameters when using the `-Full` switch.
  - Updated `WriteOutputCmdlet` to declare `ValueFromPipeline=true` for its `InputObject` parameter and handle processing pipeline input vs. direct parameter value.
  - Updated `GetCommandCmdlet` to output structured `Models.CommandInfo` objects instead of just strings.
  - Refined parameter binding in `Executor.cs` to throw specific `ParameterBindingException` on type conversion errors (named and positional) instead of just logging warnings.
  - Added support in `Executor.cs` for binding remaining positional arguments to a parameter with an array type (e.g., `string[]`).

## [0.5.0] - 2025-05-02

### Added

- **Parser Refinements (2025-05-01):**
  - Implemented basic variable expansion (`$varName`) during tokenization.
  - Implemented statement splitting (`;`) respecting quotes/escapes.
  - Implemented pipeline splitting (`|`) respecting quotes/escapes.
  - Implemented general escape character (`\`) handling (including inside quotes).
  - Updated `Executor` to process multiple statements sequentially.
- **Start of C#/.NET Refactoring:**
  - Created new C# solution (`src_csharp/ArbSh.sln`) and console project (`src_csharp/ArbSh.Console`).
  - Added placeholder classes for core pipeline (`PipelineObject`, `CmdletBase`) and execution (`Parser`, `Executor`).
  - Implemented basic REPL loop in `Program.cs`.
  - Added basic `WriteOutputCmdlet` placeholder.
  - Added `ParsedCommand.cs` to represent parsed command structure, including properties for output redirection.
  - Updated `Parser.cs`'s `TokenizeInput` method to handle escaped quotes (`\"`) and basic escaped operators (`\|`, `\;`, `\>`).
  - Updated `Parser.cs`'s `Parse` method to detect output redirection operators (`>`, `>>`) and store the path/append flag in `ParsedCommand`. Added basic handling for command separator (`;`) - currently only processes the first statement.
  - Added `ParameterAttribute.cs` for marking cmdlet parameters.
  - Updated `GetHelpCmdlet.cs` and `GetCommandCmdlet.cs` to use `[Parameter]` attribute.
  - Implemented parameter binding logic in `Executor.cs` using reflection, including mandatory parameter checks (throwing `ParameterBindingException`), stricter boolean switch handling, and improved type conversion using `TypeConverter` / `Convert.ChangeType`.
  - Added `CommandDiscovery.cs` to find available cmdlets using reflection.
  - Updated `Executor.cs` to use `CommandDiscovery`, handle `ParameterBindingException`, and use the refined binding logic.
  - Updated `CmdletBase.cs` to collect output internally using `BlockingCollection`.
  - Updated `Executor.cs` with basic sequential pipeline logic (passing output of one command to the input of the next via `BlockingCollection`), confirmed working with `Get-Command | Write-Output`.
  - Updated `Parser.cs` to handle the pipeline operator (`|`).
  - Implemented basic logic for `GetHelpCmdlet` using reflection to display command syntax and parameters.
  - Implemented basic logic for `GetCommandCmdlet` using `CommandDiscovery` to list commands.
- Updated documentation (`README.md`, `ROADMAP.md`, `docs/PROJECT_ORGANIZATION.md`, `docs/DOCUMENTATION_INDEX.md`, `CONTRIBUTING.md`, `docs/CHANGELOG.md`) to reflect the new C# direction and refactoring status.
- Updated `.gitignore` for C#/.NET development.

### Changed

- Project direction shifted towards a PowerShell-inspired C# shell with Arabic command support.
- Build system transitioned from CMake to .NET CLI.
- Moved original C/C++ source, include, tests, cmake, build files into `old_c_code/` directory for reference.

### Deprecated

- The existing C implementation (now in `old_c_code/`) is considered legacy and primarily serves as a reference for porting i18n logic.
- CMake build system (now in `old_c_code/`).

## Previous C Implementation [Unreleased] - Before C# Refactor

### Added

- ImGui-based GUI mode for Windows
  - Modern GPU-accelerated user interface
  - Tab-based terminal interface
  - Customizable appearance with dark mode
  - Command history and completion
  - Split testing configuration in CMake build system
  - Four distinct build modes (console, GUI, console+tests, GUI+tests)
  - Arabic text support in the GUI interface
- Unified entry point architecture through `shell_entry.c`
  - Single code path for shell initialization across all platforms
  - Improved maintainability and code organization
  - Simplified build process
- Complete implementation of Unicode Bidirectional Algorithm (UAX #9)
  - Full support for directional formatting characters (LRM, RLM, LRE, RLE, LRO, RLO, LRI, RLI, FSI, PDI)
  - Proper handling of nested bidirectional text
  - Paragraph-level direction control
  - Comprehensive character type classification
- Enhanced Arabic keyboard layout support
  - Complete key mapping for Arabic characters
  - Keyboard layout switching with 'layout' command
  - Ctrl+A shortcut for quick layout toggling
  - Visual indicators for current input mode

### Changed

- Enhanced UI appearance for ImGui shell
  - Modern blue color scheme with rounded corners
  - Improved tab styling and visibility
  - Better console text display with proper wrapping
  - Enhanced command input styling
  - Fullscreen window mode with proper layout
- Improved build system with GCC toolchain support
  - Added toolchain file for GCC compilation
  - Enhanced CMake configuration options
  - Better cross-platform build support
- Merged multiple entry point files (`main.c`, `main_gui.c`, `win_main.c`, `win_gui_common.c`) into a single `shell_entry.c`
- Updated CMakeLists.txt to use the new unified entry point
- Enhanced README.md with architecture documentation
- Improved integration between console and GUI modes
- Consolidated common platform-specific code
- Refactored bidirectional text handling for better performance and compliance with Unicode standards
- Enhanced cursor positioning logic for RTL text input

### Fixed

- ImGui integration issues
  - Fixed style push/pop imbalance causing assertion failures
  - Corrected InputText callback implementation
  - Properly initialized DirectX resources
  - Fixed window sizing and positioning
- Bidirectional text rendering issues with mixed Arabic and Latin text
- Arabic character input and display problems
- Cursor positioning in RTL text mode
- Status bar updates for keyboard layout changes

### Removed

- Redundant initialization code across multiple entry point files
- Duplicate functionality for Windows GUI mode

## [1.1.0] - 2025-03-02

### Added

- Comprehensive Arabic language support
  - Complete UTF-8 implementation
  - Right-to-left text display
  - Bidirectional text algorithm (partial implementation)
  - Arabic character shaping
  - Arabic input method support
- Baa language integration capabilities
  - Compatible terminal environment
  - Support for Arabic programming
  - Consistent cross-platform behavior
- Localization system with English and Arabic support
- `lang` command to switch between languages
- Custom Windows console for enhanced Arabic support
- Enhanced documentation
  - PROJECT_STATUS.md - Current implementation status
  - ARABIC_SUPPORT_GUIDE.md - Technical details of Arabic support
  - BAA_INTEGRATION.md - Integration with Baa language
  - DEVELOPMENT_TASKS.md - Prioritized development tasks
  - DOCUMENTATION_INDEX.md - Overview of all documentation

### Changed

- Refactored text handling to use UTF-8 throughout
- Updated README.md to reflect new focus on Arabic support and Baa integration
- Enhanced CONTRIBUTING.md with Arabic and Baa-specific guidelines
- Improved UI for bidirectional text display
- Restructured code for better modularity of language features

### Fixed

- Unicode rendering issues in Windows console
- Command parsing errors with RTL text
- Input positioning with bidirectional text
- File path handling with Arabic characters

## [1.0.1] - 2025-01-31

### Added

- Windows-specific signal handling with proper SIGINT definition
- CMake build system improvements for MSVC compiler
- Cross-platform compatibility layer in shell.h

### Changed

- Updated header file organization for better Windows/Unix compatibility
- Improved signal handler implementation for Windows
- Refactored environment variable handling for Windows compatibility

### Fixed

- Signal handling issues on Windows platform
- Build errors related to missing POSIX headers on Windows
- Type conversion warnings in getLine.c
- Environment variable access on Windows systems

## [1.0.0] - 2025-01-31

### Added

- Cross-platform support for Windows and Unix-like systems
- CMake build system with proper configuration
- Documentation generation using Doxygen
- Comprehensive .gitignore file
- Environment variable handling for both platforms
- Signal handling for Windows and Unix-like systems
- Built-in command support (cd, exit, env, etc.)
- History functionality
- Command chaining support (&&, ||, ;)
- Alias support
- Variable replacement ($? and $$)

### Changed

- Improved code organization and structure
- Enhanced error handling and reporting
- Updated function documentation
- Optimized environment variable management
- Standardized coding style across the project

### Fixed

- Duplicate function definitions
- Memory leaks in environment variable handling
- Signal handling issues on Windows
- Path resolution bugs
- Command execution errors
- History file handling issues

### Security

- Improved environment variable security
- Better memory management
- Enhanced error checking for system calls

## [0.1.0] - 2025-01-30

### Added

- Initial release
- Basic shell functionality
- Command execution
- Environment variable support
- Basic error handling
