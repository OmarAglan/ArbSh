# Contributing to Simple Shell

We love your input! We want to make contributing to Simple Shell as easy and transparent as possible, whether it's:

- Reporting a bug
- Discussing the current state of the code
- Submitting a fix
- Proposing new features
- Improving Arabic language support
- Enhancing integration with the Baa language ecosystem

## Development Process

We use GitHub to host code, to track issues and feature requests, as well as accept pull requests.

1. Fork the repo and create your branch from `master`.
2. If you've added code that should be tested, add tests.
3. If you've changed APIs, update the documentation.
4. Ensure the test suite passes.
5. Make sure your code follows the existing style.
6. Issue that pull request!

## Code Style

* Use 4 spaces for indentation
* Follow the Betty style guide for C
* Keep functions under 40 lines
* Use descriptive variable names
* Comment complex logic
* Each file should have a header comment explaining its purpose
* Use UTF-8 encoding for all source files
* Document any string literals or messages that need translation
* Include Arabic comments where appropriate for Arabic-specific code

## Arabic Language Support Guidelines

When working on Arabic language features:

1. **Text Handling**
   * Always use UTF-8 for string handling
   * Never assume fixed character width
   * Test with a variety of Arabic text (classical, modern, with/without diacritics)
   * Consider text shaping and joining behavior

2. **Bidirectional Text**
   * Follow the [Unicode Bidirectional Algorithm](https://www.unicode.org/reports/tr9/)
   * Test mixed Arabic/Latin text carefully
   * Pay attention to bracket and quote mirroring
   * Handle numeric sequences correctly in RTL context

3. **UI Elements**
   * Design UI for both LTR and RTL layouts
   * Consider text alignment, cursor position, and input direction
   * Test with various terminal widths

4. **Testing Arabic Features**
   * Include test cases with Arabic text
   * Test with different Arabic keyboard layouts
   * Verify correct display of Arabic text in various contexts

## Baa Language Integration Guidelines

When working on Baa language integration:

1. **API Compatibility**
   * Ensure shell features work properly with Baa language constructs
   * Follow Baa language conventions for error handling and output
   * Test integration points thoroughly

2. **Terminal Features**
   * Implement features that benefit Baa development workflow
   * Support Baa-specific command completion and syntax highlighting
   * Ensure proper handling of Baa language output

3. **Documentation**
   * Document how your changes benefit Baa language users
   * Update Baa integration examples when adding new features
   * Reference the official Baa language documentation when appropriate

## Cross-Platform Guidelines

When implementing features:

1. Use platform-agnostic APIs where possible
2. Isolate platform-specific code in dedicated modules
3. Test on both Windows and Unix/Linux platforms
4. Document any platform-specific behaviors or limitations
5. Ensure consistent Arabic text handling across platforms

## Bug Reports

Use GitHub issues to report bugs. Report a bug by opening a new issue.

When reporting issues with Arabic text handling or Baa language integration, please include:

1. Sample text that reproduces the issue
2. Terminal environment details (OS, locale settings, terminal emulator)
3. Steps to reproduce
4. Expected vs. actual behavior
5. Screenshots if applicable

## Feature Requests

We welcome feature requests, especially those that improve Arabic language support or Baa language integration. When proposing a new feature:

1. Describe the problem your feature solves
2. Explain how your solution would work
3. Provide examples of similar implementations if available
4. Indicate if the feature is platform-specific or cross-platform

## Documentation

When contributing documentation:

1. Follow Markdown guidelines
2. Include code examples where appropriate
3. Update relevant sections in other documents as needed
4. Add entries to DOCUMENTATION_INDEX.md for new documents
5. Consider including both English and Arabic versions for key documentation

## License

By contributing, you agree that your contributions will be licensed under the same license as the original project.
