# ArbSh - Arabic-First Shell

**Current Version:** 0.8.1-alpha
**Status:** Phase 6 In Progress - Non-Interactive External Commands Ready
**Next Step:** Process-Tree Ownership and PTY/ConPTY

ArbSh is an Arabic-first command-line shell built on C#/.NET, designed specifically for Arabic developers and users. Inspired by PowerShell's object pipeline architecture, ArbSh provides a powerful, extensible environment with native Arabic language support and full Unicode BiDi compliance.

## Eco Ecosystem Role

ArbSh is Eco's official Arabic-first shell and standalone terminal direction.
It owns shell parsing, interactive sessions, process hosting, and terminal UX;
it does not own Baa compilation, Takween project semantics, Nazm encoding, or
Qalam editor features. Baa, Nazm, and Takween remain usable without ArbSh.

The planned [`arbsh-host-v1`](docs/ARBSH_HOST_V1.md) boundary will let Qalam
host the ArbSh CLI through PTY/ConPTY while the standalone Avalonia terminal
uses the same core and process behavior.

## 🌟 Key Features

### Arabic-First Design
- **Native Arabic Commands:** Execute commands using Arabic script (`الأوامر`, `مساعدة`, `اطبع`, `انتقل`, `اعرض`, `المسار`, `اخرج`)
- **Full BiDi Support:** Complete Unicode BiDi Algorithm (UAX #9) implementation
- **RTL Text Handling:** Proper Right-to-Left text rendering and processing
- **Arabic Parameter Names:** Support for Arabic-first parameters (e.g., `-الأمر`, `-كامل`, `-النص`)

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
- **Structured Process Core:** Direct argv/cwd/environment launch with separate
  UTF-8 stdout/stderr, exit status, failure classification, and cancellation
  without `cmd /c` or another intermediate shell
- **External Commands:** Unresolved commands run through the structured layer
  with built-in precedence, session working directory, line-oriented pipelines,
  redirection, and exact child exit-code preservation

## 🚀 Current Status (Version 0.8.1-alpha)

### ✅ Phase 5 Complete: Custom GUI Terminal Baseline

**Completed Features:**
- **Complete BiDi Algorithm Implementation:** All rule sets (P, X, W, N, I, L) fully implemented
- **Subexpression Execution:** PowerShell-style `$(...)` command substitution **WORKING**
- **Type Literal Utilization:** `[TypeName]` type casting functionality **WORKING**
- **70+ BiDi Tests Passing:** Comprehensive Unicode BidiTest.txt compliance
- **Arabic Command Surface:** Runtime command discovery and invocation are Arabic-only for user-facing cmdlets
- **File Management Commands:** Added Arabic-first directory navigation/listing commands with session-scoped working directory
- **Windows Context Menu Installer Flow:** Added installer packaging scripts that register "Open in ArbSh" Explorer entries

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
- Arabic parameter names (e.g., `-الأمر`, `-كامل`, `-النص`)
- Full Unicode text processing and BiDi algorithm compliance

**Available Commands:**
- `اطبع` - Output objects to pipeline or console
- `مساعدة` - Display command help and documentation
- `الأوامر` - List all available commands
- `انتقل` - Change current session directory
- `المسار` - Print current session directory
- `اعرض` - List files/folders in current or target directory
- `اختبار-مصفوفة` - Validate array parameter binding behavior
- `اختبار-نوع` - Validate type literal conversion behavior
- `اخرج` - Exit the current host session (host command)

**BiDi Algorithm Implementation:**
- Complete UAX #9 compliance with all rule sets (P, X, W, N, I, L)
- ICU4N library integration for accurate Unicode character properties
- 70+ BidiTest.txt compliance tests passing
- Real-time BiDi processing for mixed Arabic/English content

## 🎯 Next Step: Interactive Process Hosting

**Upcoming Features:**
- Own descendant processes with Windows Job Objects and POSIX process groups
- Add PTY/ConPTY hosting, live streams, resize, and terminal control flow
- Baa compiler output hosting with flawless Arabic rendering
- Add the Arabic `تشغيل` workflow for Baa files and Takween projects

## 📁 Project Structure

```
ArbSh/
├── src_csharp/                 # C#/.NET Implementation
│   ├── ArbSh.Core/             # Shell engine and structured process layer
│   ├── ArbSh.Console/          # Console REPL host
│   ├── ArbSh.Terminal/         # Avalonia GUI terminal host
│   ├── ArbSh.ProcessFixture/   # External-process contract test helper
│   ├── ArbSh.Test/             # xUnit test suite
│   └── ArbSh.sln               # Visual Studio solution
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
- .NET 10 SDK (the feature band is pinned in `global.json`)
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
   ArbSh> الأوامر
   ArbSh> المسار
   ArbSh> اعرض
   ArbSh> انتقل مشروع
   ArbSh> مساعدة
   ArbSh> اطبع $(الأوامر)
   ArbSh> اختبار-نوع [int] 42
   ArbSh> اخرج
   ```

### Building a Release

A PowerShell script (`create-release.ps1`) automates release creation:

2. **Run the release script:**
   ```powershell
   .\create-release.ps1 -Version "0.8.1-alpha"
   ```

This creates a self-contained release build and packages it into `releases/` directory.

3. **Build release + installer package (Windows context menu):**
   ```powershell
   .\create-release.ps1 -Version "0.8.1-alpha" -CreateInstaller
   ```

This also creates `ArbSh-v<version>-<rid>-installer.zip` with:
- `Install-ArbSh.ps1`
- `Uninstall-ArbSh.ps1`
- `App/` published `ArbSh.Terminal`

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

**Current Focus (Phase 6):**
- Explicit process-tree ownership and interactive PTY/ConPTY hosting
- Baa compiler integration workflow
- Live foreground/background stream integration

**Future Phases:**
- Qalam-hosted ArbSh sessions and developer-kit admission
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

**Current Status:** Phase 6 In Progress - Non-Interactive External Commands Ready
