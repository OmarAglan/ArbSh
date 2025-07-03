# ArbSh - Arabic-First Shell

**Current Version:** 0.7.7.11
**Status:** Phase 4 Complete - Full BiDi Algorithm UAX #9 Compliance
**Next Phase:** Phase 5 - Console I/O with BiDi Rendering

ArbSh is an Arabic-first command-line shell built on C#/.NET, designed specifically for Arabic developers and users. Inspired by PowerShell's object pipeline architecture, ArbSh provides a powerful, extensible environment with native Arabic language support and full Unicode BiDi compliance.

## 🌟 Key Features

### Arabic-First Design
- **Native Arabic Commands:** Execute commands using Arabic script (`احصل-مساعدة` for Get-Help)
- **Full BiDi Support:** Complete Unicode BiDi Algorithm (UAX #9) implementation
- **RTL Text Handling:** Proper Right-to-Left text rendering and processing
- **Arabic Parameter Names:** Support for Arabic parameter aliases (`-الاسم`)

### Modern Shell Architecture
- **Object Pipeline:** PowerShell-inspired object-based command pipeline
- **Task-Based Concurrency:** Efficient parallel pipeline execution
- **Reflection-Based Binding:** Dynamic parameter binding with type conversion
- **Subexpression Execution:** PowerShell-style `$(...)` command substitution
- **Type Literal Support:** `[TypeName]` type casting functionality

### Cross-Platform Compatibility
- **Built on .NET:** Cross-platform support (Windows, macOS, Linux)
- **Unicode Compliant:** Full UTF-8 and Unicode text processing
- **Modern C# Architecture:** Extensible cmdlet framework

## 🚀 Current Status (Version 0.7.7.11)

### ✅ Phase 4 Complete: Full BiDi Algorithm UAX #9 Compliance

**Completed Features:**
- **Complete BiDi Algorithm Implementation:** All rule sets (P, X, W, N, I, L) fully implemented
- **Subexpression Execution:** PowerShell-style `$(...)` command substitution **WORKING**
- **Type Literal Utilization:** `[TypeName]` type casting functionality **WORKING**
- **70+ BiDi Tests Passing:** Comprehensive Unicode BidiTest.txt compliance
- **Arabic Command Support:** `احصل-مساعدة` (Get-Help) with Arabic parameters

### 🏗️ Core Architecture (Fully Functional)

**Pipeline System:**
- Object-based pipeline with task-based concurrency
- Dynamic parameter binding using reflection
- Command discovery and caching
- Stream redirection and merging (`>`, `>>`, `2>`, `2>&1`, `<`)

**Advanced Parsing:**
- Quote handling (`"..."`, `'...'`) with escape sequences
- Variable expansion (`$variableName`) with concatenation
- Statement separation (`;`) and pipeline operators (`|`)
- Subexpression parsing `$(...)` with recursive command structures
- Type literal parsing `[TypeName]` with whitespace support

**Arabic Language Integration:**
- Arabic command names via `[ArabicName]` attributes
- Arabic parameter aliases (e.g., `-الاسم` for `-CommandName`)
- Full Unicode text processing and BiDi algorithm compliance

**Available Cmdlets:**
- `Write-Output` - Output objects to pipeline or console
- `Get-Help` / `احصل-مساعدة` - Display command help and documentation
- `Get-Command` - List all available commands
- `Test-Array-Binding` - Array parameter binding testing
- `Test-Type-Literal` - Type literal functionality testing

**BiDi Algorithm Implementation:**
- Complete UAX #9 compliance with all rule sets (P, X, W, N, I, L)
- ICU4N library integration for accurate Unicode character properties
- 70+ BidiTest.txt compliance tests passing
- Real-time BiDi processing for mixed Arabic/English content

## 🎯 Next Phase: Phase 5 - Console I/O with BiDi Rendering

**Upcoming Features:**
- RTL console input with proper cursor movement
- BiDi-aware output rendering using implemented algorithm
- Arabic error messages and help text
- Complete Arabic localization
- Enhanced Arabic developer workflow

## 📁 Project Structure

```
ArbSh/
├── src_csharp/                 # C#/.NET Implementation
│   ├── ArbSh.Console/          # Main console application
│   │   ├── Commands/           # Cmdlet implementations
│   │   ├── Parsing/            # Parser and tokenizer
│   │   ├── I18n/               # BiDi algorithm and Arabic support
│   │   ├── Models/             # Data models and pipeline objects
│   │   └── Program.cs          # REPL entry point
│   └── ArbSh.sln              # Visual Studio solution
├── docs/                       # Comprehensive documentation
│   ├── BIDI_*_RULES_DESIGN.md # BiDi algorithm technical specs
│   ├── USAGE_EXAMPLES.md      # Complete feature guide
│   └── PROJECT_ORGANIZATION.md # Architecture documentation
├── old_c_code/                # Original C implementation (reference)
├── ROADMAP.md                 # Development phases and progress
├── CHANGELOG.md               # Version history
└── README.md                  # This file
```

**Build System:** Standard .NET CLI (`dotnet build`, `dotnet run`)

## 🚀 Getting Started

### Prerequisites
- .NET 6.0 or later
- Windows, macOS, or Linux

### Running ArbSh

1. **Clone the repository:**
   ```bash
   git clone https://github.com/OmarAglan/ArbSh.git
   cd ArbSh
   ```

2. **Navigate to the console project:**
   ```bash
   cd src_csharp/ArbSh.Console
   ```

3. **Run the shell:**
   ```bash
   dotnet run
   ```

4. **Try some commands:**
   ```powershell
   ArbSh> Get-Command
   ArbSh> احصل-مساعدة
   ArbSh> Write-Output $(Get-Command)
   ArbSh> Test-Type-Literal [int] 42
   ArbSh> exit
   ```

### Building a Release

A PowerShell script (`create-release.ps1`) automates release creation:

2. **Run the release script:**
   ```powershell
   .\create-release.ps1 -Version "0.7.7.11"
   ```

This creates a self-contained release build and packages it into `releases/` directory.

## 📖 Documentation

- **[USAGE_EXAMPLES.md](docs/USAGE_EXAMPLES.md)** - Complete feature guide with working examples
- **[PROJECT_ORGANIZATION.md](docs/PROJECT_ORGANIZATION.md)** - Architecture and project structure
- **[ROADMAP.md](ROADMAP.md)** - Development phases and progress tracking
- **[CHANGELOG.md](CHANGELOG.md)** - Version history and technical details
- **[docs/DOCUMENTATION_INDEX.md](docs/DOCUMENTATION_INDEX.md)** - Complete documentation index

## 🤝 Contributing

ArbSh welcomes contributions from developers interested in Arabic language computing and modern shell development. See our documentation for:

- Project architecture and organization
- BiDi algorithm implementation details
- Arabic language integration patterns
- Testing frameworks and standards

## 🎯 Arabic-First Philosophy

ArbSh is designed specifically for Arabic developers and users, not as a bilingual shell. Our approach:

- **Native Arabic Commands:** Primary interface in Arabic script
- **Cultural Localization:** Arabic developer workflow optimization
- **Unicode Compliance:** Full BiDi algorithm implementation
- **Community Focus:** Built by and for the Arabic developer community

## 📋 Current Limitations

**Planned for Phase 5:**
- RTL console input and cursor movement
- BiDi-aware visual text rendering
- Arabic error messages and help text

**Future Phases:**
- External process execution (Phase 6)
- Advanced scripting features (Phase 7)
- Tab completion and command history
- Rich error handling and reporting

## 🌟 Vision

ArbSh aims to be the premier command-line shell for Arabic developers, providing:
- Seamless Arabic language integration
- Modern object-oriented pipeline architecture
- Full Unicode and BiDi compliance
- Cross-platform compatibility
- Extensible cmdlet framework

**Current Status:** Phase 4 Complete - Ready for Phase 5 Console I/O Integration
