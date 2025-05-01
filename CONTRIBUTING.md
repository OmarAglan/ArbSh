# Contributing to ArbSh (C#/.NET Refactoring)

We love your input! We want to make contributing to ArbSh as easy and transparent as possible, especially during this refactoring phase to C#/.NET. Contributions can include:

- Reporting a bug in the C# code or the porting process.
- Discussing the current state of the C# code and architecture.
- Submitting a fix or improvement to the C# codebase.
- Proposing new features for the C# shell.
- Helping with the porting of i18n logic from C to C#.
- Improving documentation related to the C# version.

## We Develop with Github
We use Github to host code, to track issues and feature requests, as well as accept pull requests.

## We Use [Github Flow](https://guides.github.com/introduction/flow/index.html) (or similar)
Pull requests are the best way to propose changes to the codebase. We actively welcome your pull requests:

1. Fork the repo and create your branch from the `main` branch (or the relevant development branch).
2. If you've added code that should be tested, add corresponding C# unit tests (using xUnit, NUnit, or MSTest - check project setup).
3. If you've changed APIs or behavior, update the relevant documentation (XML comments in code, Markdown files in `/docs`).
4. Ensure the C# test suite passes (`dotnet test`).
5. Make sure your code follows standard C#/.NET coding conventions (see below).
6. Issue that pull request!

## Any contributions you make will be under the MIT Software License
In short, when you submit code changes, your submissions are understood to be under the same [MIT License](http://choosealicense.com/licenses/mit/) that covers the project. Feel free to contact the maintainers if that's a concern.

## Report bugs using Github's [issue tracker](https://github.com/OmarAglan/simple_shell/issues)
We use GitHub issues to track public bugs related to the **C# refactoring**. Report a bug by [opening a new issue](https://github.com/OmarAglan/simple_shell/issues/new); it's that easy! Please specify that the bug relates to the C# version.

## Write bug reports with detail, background, and sample code

**Great Bug Reports** tend to have:

- A quick summary and/or background (mentioning it's the C# version).
- Steps to reproduce
  - Be specific!
  - Give sample code or commands if you can.
- What you expected would happen
- What actually happens
- Notes (possibly including why you think this might be happening, or stuff you tried that didn't work)
- Information about your environment (.NET version, OS).

## Use a Consistent Coding Style (C#/.NET)

* Follow the established C# coding conventions used in the project (generally aligned with [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)).
* Use `dotnet format` or the formatting settings configured in the IDE (e.g., Visual Studio, VS Code with C# extension) to ensure consistency.
* Use descriptive variable and method names.
* Add XML documentation comments (`///`) to public APIs.
* Keep methods focused and readable.

*(The previous C/Betty style guidelines no longer apply to new C# code).*

## Cross-Platform Development Guidelines (C#/.NET)

.NET is designed to be cross-platform. However, keep in mind:

1. Avoid platform-specific APIs (P/Invokes to native libraries) unless absolutely necessary and properly abstracted or conditionally compiled.
2. Test your changes on target platforms (Windows, Linux, macOS) if they involve potentially platform-sensitive areas (like file system access, process management, console interaction).
3. Use .NET Standard libraries where possible for maximum compatibility if creating shared libraries.

## License
By contributing, you agree that your contributions will be licensed under its MIT License.
