# Project Organization

## Overview

This document outlines the organization structure for the **ArbSh project** - an Arabic-first shell designed specifically for the Arabic developer community. ArbSh is a PowerShell-inspired shell with complete Arabic language support, including full Unicode BiDi (Bidirectional) text rendering compliance according to UAX #9 standards.

**Current Version:** 0.7.7.11
**Status:** Phase 4 Complete - Full BiDi Algorithm UAX #9 Compliance
**Next Phase:** Phase 5 - Console I/O with BiDi Rendering

## Current Directory Structure

```
ArbSh/
├── .git/               # Git repository data
├── .github/            # GitHub specific files (workflows, etc.)
├── .vscode/            # VS Code settings (optional)
├── docs/               # Comprehensive documentation
│   ├── CHANGELOG.md                    # Version history and changes
│   ├── DOCUMENTATION_INDEX.md          # Index of all documentation
│   ├── PROJECT_ORGANIZATION.md         # This file - project structure
│   ├── USAGE_EXAMPLES.md              # Usage examples and tutorials
│   ├── BIDI_ALGORITHM_DESIGN.md       # BiDi algorithm implementation design
│   ├── BIDI_P_RULES_DESIGN.md         # P Rules (Paragraph) design document
│   ├── BIDI_X_RULES_DESIGN.md         # X Rules (Explicit) design document
│   ├── BIDI_W_RULES_DESIGN.md         # W Rules (Weak) design document
│   ├── BIDI_N_RULES_DESIGN.md         # N Rules (Neutral) design document
│   ├── BIDI_I_RULES_DESIGN.md         # I Rules (Implicit) design document
│   └── BIDI_L_RULES_DESIGN.md         # L Rules (Levels) design document
├── src_csharp/         # Root for all C#/.NET code
│   ├── ArbSh.sln       # .NET Solution file
│   └── ArbSh.Console/  # Main C# Console Application project
│       ├── ArbSh.Console.csproj        # Project file (v0.7.7.11)
│       ├── Program.cs                  # Main entry point
│       ├── PipelineObject.cs           # Pipeline data structure
│       ├── CmdletBase.cs              # Base class for all cmdlets
│       ├── Parser.cs                   # Command parsing and tokenization
│       ├── Executor.cs                 # Command execution and BiDi processing
│       ├── CommandDiscovery.cs         # Cmdlet discovery and caching
│       ├── ParameterAttribute.cs       # Parameter binding attributes
│       ├── BidiAlgorithm.cs           # Complete UAX #9 BiDi implementation
│       ├── BidiTypes.cs               # BiDi character types and enums
│       ├── BidiRun.cs                 # BiDi run data structure
│       ├── ArabicShaper.cs            # ICU4N-based character shaper
│       ├── BidiTestRunner.cs          # Unicode BidiTest.txt test runner
│       └── Commands/                   # Built-in cmdlets
│           ├── WriteOutputCmdlet.cs    # Write-Output cmdlet
│           ├── GetHelpCmdlet.cs       # Get-Help cmdlet (Arabic alias: احصل-مساعدة)
│           ├── GetCommandCmdlet.cs     # Get-Command cmdlet
│           ├── TestArrayBindingCmdlet.cs # Array binding test cmdlet
│           └── TestTypeLiteralCmdlet.cs  # Type literal test cmdlet
├── test_output.log     # Test execution output log
├── test_features.ps1   # PowerShell test script
├── .editorconfig       # Editor configuration
├── .gitattributes      # Git attributes
├── .gitignore          # Git ignore rules
├── README.md           # Main project README
└── ROADMAP.md          # Project roadmap and phases
```

## Code Organization Principles

The ArbSh C# codebase follows standard .NET conventions and principles with Arabic-first design:

1.  **Namespaces:** Code is organized using logical namespaces:
    *   `ArbSh.Console` - Main console application and core functionality
    *   `ArbSh.Console.Commands` - Built-in cmdlets and command implementations
2.  **Separation of Concerns:**
    *   **Parser.cs** - Command parsing, tokenization, and subexpression handling
    *   **Executor.cs** - Pipeline execution, parameter binding, and BiDi processing
    *   **BidiAlgorithm.cs** - Complete UAX #9 BiDi algorithm implementation
    *   **ArabicShaper.cs** - Text shaping logic using ICU4N
    *   **CommandDiscovery.cs** - Cmdlet discovery and caching
    *   **CmdletBase.cs** - Base class for all cmdlets with parameter binding
3.  **Arabic-First Design:**
    *   Commands support Arabic aliases (e.g., `احصل-مساعدة` for `Get-Help`)
    *   Full BiDi text processing for mixed Arabic/English content
    *   Unicode-compliant text rendering and input handling
4.  **Naming Conventions:** Standard .NET naming guidelines (PascalCase for classes, methods, properties)
5.  **Task-Based Concurrency:** Pipeline execution uses `Task` and `BlockingCollection<T>` for concurrent processing
6.  **Comprehensive Testing:** Built-in test cmdlets and Unicode BidiTest.txt compliance testing

## Build System

The project uses standard **.NET CLI build tools**:
- `dotnet build` - Build the solution
- `dotnet run --project src_csharp/ArbSh.Console` - Run ArbSh
- `dotnet test` - Run unit tests (when implemented)

**Dependencies:**
- .NET 9.0 target framework
- ICU4N library (v60.1.0-alpha.437) for Unicode processing

## Current Implementation Status

### ✅ **Completed Features (Phase 4)**
- **Complete BiDi Algorithm (UAX #9):** All rule sets implemented and tested
  - **P Rules (P2-P3):** Paragraph embedding level determination
  - **X Rules (X1-X10):** Explicit formatting codes (LRE, RLE, PDF, LRO, RLO, LRI, RLI, FSI, PDI)
  - **W Rules (W1-W7):** Weak type resolution (ES, ET, EN, AN handling)
  - **N Rules (N0-N2):** Neutral type resolution and boundary neutrals
  - **I Rules (I1-I2):** Implicit embedding levels for strong types
  - **L Rules (L1-L4):** Level-based reordering and combining marks
- **Pipeline Execution:** Task-based concurrent command execution
- **Parameter Binding:** Reflection-based cmdlet parameter binding with type conversion
- **Subexpression Execution:** PowerShell-style `$(...)` command substitution
- **Type Literal Utilization:** PowerShell-style `[TypeName]` type casting
- **Command Discovery:** Automatic cmdlet discovery and caching
- **Arabic Command Aliases:** Support for Arabic command names

### 🔄 **Next Phase (Phase 5)**
- Console I/O with integrated BiDi rendering
- RTL input handling and cursor management
- BiDi-aware output rendering
- TUI library integration evaluation

## Testing Framework

**Current Testing:**
- **BidiTestRunner.cs:** Unicode BidiTest.txt compliance testing (70+ tests passing)
- **Test Cmdlets:** Built-in cmdlets for testing specific features
- **test_features.ps1:** PowerShell script for automated testing
- **Manual Testing:** Interactive shell testing with debug output

**Testing Coverage:**
- ✅ BiDi algorithm compliance (Unicode standard test suite)
- ✅ Parameter binding and type conversion
- ✅ Subexpression execution
- ✅ Type literal processing
- ✅ Pipeline execution and error handling

## Documentation

**Comprehensive Documentation:**
- **Design Documents:** Detailed BiDi algorithm rule implementation designs
- **Usage Examples:** Current working features and command examples
- **Technical Documentation:** Architecture and implementation details
- **Change Log:** Version history with technical details

**Code Documentation:**
- XML documentation comments (`///`) for IntelliSense
- Extensive debug logging for troubleshooting
- Inline comments for complex BiDi algorithm logic

## Arabic-First Philosophy

ArbSh is designed specifically for Arabic developers with:
- **Native Arabic Support:** Full BiDi text processing and rendering
- **Arabic Command Names:** Commands available in Arabic (e.g., `احصل-مساعدة`)
- **Unicode Compliance:** Full UAX #9 BiDi algorithm implementation
- **Cultural Localization:** Designed for Arabic developer workflows and preferences

The project prioritizes Arabic language support as a first-class feature, not an afterthought or translation layer.