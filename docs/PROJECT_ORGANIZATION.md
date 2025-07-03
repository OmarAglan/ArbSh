# Project Organization

## Overview

This document outlines the organization structure for the **ArbSh project** - an Arabic-first shell designed specifically for the Arabic developer community. ArbSh is a PowerShell-inspired shell with complete Arabic language support, including full Unicode BiDi (Bidirectional) text rendering compliance according to UAX #9 standards.

**Current Version:** 0.7.7.11
**Status:** Phase 4 Complete - Full BiDi Algorithm UAX #9 Compliance
**Next Phase:** Phase 5 - Console I/O with BiDi Rendering

## Current Directory Structure

```
arbsh/
├── .git/               # Git repository data
├── .github/            # GitHub specific files (workflows, etc.)
├── .vscode/            # VS Code settings (optional)
├── docs/               # Documentation (including this file)
│   ├── *.md            # Markdown documentation files
├── src_csharp/         # NEW: Root for all C#/.NET code
│   ├── ArbSh.sln       # .NET Solution file
│   └── ArbSh.Console/  # Main C# Console Application project
│       ├── ArbSh.Console.csproj
│       ├── Program.cs
│       ├── PipelineObject.cs
│       ├── CmdletBase.cs
│       ├── Parser.cs
│       ├── Executor.cs
│       ├── CommandDiscovery.cs
│       ├── ParameterAttribute.cs
│       └── Commands/
│           ├── WriteOutputCmdlet.cs
│           ├── GetHelpCmdlet.cs
│           └── GetCommandCmdlet.cs
│   # (Future C# library projects for Core, Cmdlets, I18n_Ported, etc. will go here)
├── old_c_code/         # MOVED: Original C/C++ code preserved for reference
│   ├── src/
│   ├── include/
│   ├── tests/
│   ├── cmake/
│   ├── build/          # (Example build output, may vary)
│   └── CMakeLists.txt
├── .editorconfig       # Editor configuration
├── .gitattributes      # Git attributes
├── .gitignore          # Git ignore rules (updated for C#)
├── README.md           # Main project README (updated for C#)
└── ROADMAP.md          # Project roadmap (updated for C#)

# Files removed from root:
# - CMakeLists.txt
# - cmake/
# - build/
# - src/
# - include/
# - tests/
```

## Code Organization Principles (C# Focus)

While the C code is preserved for reference, the new C# codebase will adhere to standard .NET conventions and principles:

1.  **Namespaces:** Organize code logically using namespaces (e.g., `ArbSh.Core`, `ArbSh.Commands`, `ArbSh.I18n`).
2.  **Solution/Projects:** Structure the code into multiple projects within the `ArbSh.sln` solution for better modularity (e.g., a core library, a cmdlet library, the console host).
3.  **Separation of Concerns:**
    *   Isolate pipeline execution logic.
    *   Separate command parsing from execution.
    *   Keep cmdlet implementations distinct.
    *   Encapsulate ported i18n logic.
4.  **Naming Conventions:** Follow standard .NET naming guidelines (PascalCase for classes, methods, properties; camelCase for local variables).
5.  **Asynchronous Programming:** Utilize `async`/`await` where appropriate, especially for I/O operations and external process management.
6.  **Dependency Injection:** Consider using DI for managing dependencies between components, especially as the shell grows more complex.

## Build System

-   The project now uses the standard **.NET CLI build tools** (`dotnet build`, `dotnet run`, etc.) managed via the solution (`.sln`) and project (`.csproj`) files.
-   The CMake build system (`CMakeLists.txt`, `cmake/`) is **deprecated** and only relevant for potentially building the reference C code if needed. It will eventually be removed.

## Testing (C# Focus)

-   A new C# test suite needs to be developed using a .NET testing framework (e.g., xUnit, NUnit, MSTest).
-   Unit tests should cover:
    *   Core pipeline logic.
    *   Parser and command resolution.
    *   Individual cmdlets.
    *   **Crucially: The ported i18n/BiDi logic.**
-   Integration tests will be needed to verify end-to-end scenarios.

## Documentation

-   **Code Documentation:** Use XML documentation comments (`///`) in C# code for IntelliSense and potential automated documentation generation.
-   **Markdown Docs:** Continue using Markdown files in the `docs/` directory for conceptual documentation, design decisions, user guides, etc., ensuring they reflect the C# implementation.

## Role of Original C Code

-   The original C/C++ source (`src`), headers (`include`), tests (`tests`), CMake build files (`cmake`, `CMakeLists.txt`), and build output (`build`) have been moved into the `old_c_code/` directory.
-   This code serves purely as a **reference** during the porting process. Key areas for reference are `old_c_code/src/i18n/`, `old_c_code/src/utils/`, and `old_c_code/tests/arabic/`.
-   This code is **not** part of the active C# build process.
-   The `old_c_code/` directory may be archived or removed once the relevant logic is successfully ported and tested in C#.

## Conclusion

This revised organization reflects the shift to C#/.NET development. It establishes a clean structure for the new codebase while preserving the valuable logic from the original implementation as a reference for the critical porting effort, particularly for the complex Arabic language support features.
