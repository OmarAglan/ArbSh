# ArbSh Documentation Index (C# Refactoring)

**Note:** The ArbSh project is currently undergoing a major refactoring to C#/.NET. This index reflects the documentation relevant to this new direction. Some documents listed below describe the original C implementation and are preserved primarily as **reference material** for the porting effort.

## Core Documentation

| Document | Description | Status / Target Audience |
|----------|-------------|-----------------|
| [README.md](../README.md) | **(Updated)** Project overview, C# refactoring status, goals. | All users |
| [ROADMAP.md](ROADMAP.md) | **(Updated)** Detailed C#/.NET development roadmap. | Developers, Project Managers |
| [PROJECT_ORGANIZATION.md](PROJECT_ORGANIZATION.md) | **(Updated)** Describes the new C# project structure and the role of legacy C code. | Developers, Project Managers |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Guidelines for contributing (Needs update for C# focus). | Contributors |
| [CHANGELOG.md](CHANGELOG.md) | History of changes (Needs update for C# refactoring start). | All users |
| [PROJECT_STATUS.md](PROJECT_STATUS.md) | **(Outdated)** Describes status of the *original C* implementation. | Historical Reference |
| [DEVELOPMENT_TASKS.md](DEVELOPMENT_TASKS.md) | **(Outdated)** Tasks related to the *original C* implementation. See [ROADMAP.md](ROADMAP.md) for C# tasks. | Historical Reference |

## Specialized Documentation (Reference for C# Porting)

| Document | Description | Status / Relevance |
|----------|-------------|-----------------|
| [ARABIC_SUPPORT_GUIDE.md](ARABIC_SUPPORT_GUIDE.md) | **(Reference)** Technical details of the *original C* Arabic language support implementation (BiDi, UTF-8). Crucial reference for porting to C#. | C# Developers (Porting) |
| [TESTING_FRAMEWORK.md](TESTING_FRAMEWORK.md) | **(Reference)** Describes the testing strategy for the *original C* implementation. C# tests need a new framework (xUnit/NUnit). | Historical Reference |
| [CMAKE_CONFIGURATION.md](CMAKE_CONFIGURATION.md) | **(Deprecated)** Details the CMake build system used for the *original C* code. C# uses `.NET CLI`. | Historical Reference |
| [BAA_INTEGRATION.md](BAA_INTEGRATION.md) | **(Outdated/Reference)** Describes integration plans for the *original C* shell with Baa. Integration for C# version is a future goal. | Historical Reference |

## Documentation Highlights

### For Understanding the New Direction

1.  **[README.md](../README.md)** - Get the overview of the C# refactoring goals.
2.  **[ROADMAP.md](ROADMAP.md)** - Understand the development plan for the C# shell.
3.  **[PROJECT_ORGANIZATION.md](PROJECT_ORGANIZATION.md)** - See the new C# project structure.

### For C# Developers (Porting Focus)

1.  **[ARABIC_SUPPORT_GUIDE.md](ARABIC_SUPPORT_GUIDE.md)** - **Essential reading** to understand the C i18n logic being ported.
2.  Review the original C code in `src/i18n/` and `src/utils/`.
3.  Review the C# placeholder code in `src_csharp/`.
4.  Follow the tasks outlined in [ROADMAP.md](ROADMAP.md).

## Documentation Maintenance

-   Documentation should reflect the current state of the **C# refactoring effort**.
-   Documents describing the original C implementation should be clearly marked as reference/historical.
-   New C# features require corresponding updates or new documentation files.

## Future C# Documentation (Planned)

-   C# API Reference (Generated from XML comments).
-   C# Cmdlet Developer Guide.
-   C# Porting Notes (Lessons learned from porting i18n logic).
-   C# User Guide (Once basic functionality exists).

## Conclusion

This documentation index guides users and developers through the resources available during the C# refactoring of ArbSh. Priority is given to understanding the new architecture and leveraging the original C implementation's i18n logic as a reference for porting.
