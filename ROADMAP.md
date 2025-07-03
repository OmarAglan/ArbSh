# ArbSh Development Roadmap

**Current Version:** 0.7.7.11
**Status:** Phase 4 Complete - Full BiDi Algorithm UAX #9 Compliance
**Next Phase:** Phase 5 - Console I/O with BiDi Rendering

This roadmap outlines the development phases for ArbSh - an Arabic-first command-line shell built on C#/.NET with PowerShell-inspired architecture and full Unicode BiDi compliance.

## 🎯 Project Vision

ArbSh aims to be the premier Arabic-first shell environment that:

- **Native Arabic Support:** Commands and parameters in Arabic script
- **Unicode BiDi Compliance:** Full UAX #9 bidirectional text algorithm implementation
- **Object Pipeline:** PowerShell-inspired object-based command pipeline
- **Cross-Platform:** .NET-based compatibility (Windows, macOS, Linux)
- **Arabic Developer Focus:** Built by and for the Arabic developer community

## 📋 Development Phases

### ✅ Phase 1: Project Foundation (Completed)
**C# Project Setup, Core Object Pipeline Design, Documentation**

- [✅] C#/.NET solution and console project structure
- [✅] Core pipeline classes (`PipelineObject`, `CmdletBase`)
- [✅] Command discovery framework (`CommandDiscovery.cs`)
- [✅] Project documentation updates
- [✅] Git configuration for C# development

### ✅ Phase 2: Core Shell Framework (Completed)
**Basic Cmdlet Framework & Execution Engine**

- [✅] REPL (Read-Eval-Print Loop) implementation
- [✅] Advanced parser with quote handling and escape sequences
- [✅] Reflection-based parameter binding with type conversion
- [✅] Task-based concurrent pipeline execution
- [✅] Core cmdlets: `Write-Output`, `Get-Help`, `Get-Command`
- [✅] File redirection support (`>`, `>>`, `2>`, `2>>`)
- [✅] Stream merging (`2>&1`, `1>&2`)
- [✅] Pipeline input binding (`ValueFromPipeline`)

### ✅ Phase 3: Advanced Parsing & Tokenization (Completed)
**Regex-Based Tokenizer with Arabic Support**

- [✅] Regex-based tokenizer replacing state machine approach
- [✅] Token type system (`TokenType` enum, `Token` struct)
- [✅] Advanced redirection parsing (input `<`, stream merging)
- [✅] Variable expansion with concatenation (`$var`)
- [✅] Subexpression parsing `$(...)` (recursive)
- [✅] Type literal parsing `[TypeName]`
- [✅] UTF-8 encoding resolution for Arabic text
- [✅] Arabic command name support via `[ArabicName]` attributes

### ✅ Phase 4: BiDi Algorithm UAX #9 Compliance & Advanced Execution (COMPLETED v0.7.7.11)
**Full Unicode BiDi Algorithm Implementation & Advanced Shell Features**

#### BiDi Algorithm Implementation (Complete UAX #9 Compliance)

**P Rules (Paragraph Level):**
- [✅] P2-P3: Paragraph embedding level determination
- [✅] Paragraph direction detection with first strong character analysis

**X Rules (Explicit Formatting):**
- [✅] X1-X10: Complete explicit formatting code handling
- [✅] LRE, RLE, PDF, LRO, RLO support with directional status stack
- [✅] LRI, RLI, FSI, PDI isolate support with proper pairing
- [✅] FSI direction detection with nested isolate boundary respect
- [✅] Stack overflow handling and BN character removal

**W Rules (Weak Type Resolution):**
- [✅] W1-W7: Complete weak type resolution implementation
- [✅] NSM, EN/AN context, AL simplification, separator resolution
- [✅] European terminator sequences and final EN to L conversion
- [✅] Isolating run sequence processing with sos/eos determination

**N Rules (Neutral Type Resolution):**
- [✅] N0-N2: Bracket pair processing and neutral type resolution
- [✅] Paired bracket detection with proper nesting and embedding levels
- [✅] Boundary neutral resolution with strong type context
- [✅] BN character handling throughout algorithm

**I Rules (Implicit Levels):**
- [✅] I1-I2: Implicit embedding level assignment for strong types
- [✅] Even/odd level handling for L, R, AN, EN characters
- [✅] BN character level inheritance from surrounding context

**L Rules (Level-Based Reordering):**
- [✅] L1-L4: Complete level-based reordering implementation
- [✅] Segment separator and paragraph separator handling
- [✅] Whitespace level resolution and combining mark processing
- [✅] Character mirroring for paired punctuation

#### Advanced Execution Features

**Subexpression Execution (WORKING):**
- [✅] PowerShell-style `$(...)` command substitution
- [✅] Full pipeline execution within subexpressions
- [✅] Output capture and string conversion
- [✅] Nested subexpression support with proper parsing
- [✅] Integration with parameter binding system

**Type Literal Utilization (WORKING):**
- [✅] PowerShell-style `[TypeName]` type casting
- [✅] Automatic type resolution with aliases (int, string, bool, etc.)
- [✅] Positional parameter mapping with type conversion
- [✅] Support for enums and complex types (DateTime, ConsoleColor)
- [✅] Integration with reflection-based parameter binding

#### Testing & Quality Assurance
- [✅] 70+ Unicode BidiTest.txt compliance tests passing
- [✅] Comprehensive test coverage for all BiDi rule sets
- [✅] Real-time BiDi processing verification
- [✅] Subexpression and type literal functionality testing

### 🚧 Phase 5: Console I/O with BiDi Rendering (CURRENT - In Progress)
**RTL Console Integration & Arabic Text Rendering**

#### ✅ Major Breakthroughs Achieved (v0.7.7.7)

**Arabic Console Input Resolution:**
- ✅ **Windows Console API Integration:** Successfully implemented ReadConsoleW direct API calls
  - Bypassed Console.ReadLine() limitations that converted Arabic to null characters
  - Created ArabicConsoleInput with multiple input strategies and auto-detection
  - Added comprehensive error handling and fallback mechanisms
  - Arabic commands like `احصل-مساعدة` now work perfectly

**Input/Output Processing Separation:**
- ✅ **Architectural Separation:** Implemented logical vs visual text processing
  - Input processing keeps Arabic text in logical order for command parsing
  - Output processing applies full BiDi algorithm for proper RTL display
  - Created BiDiTextProcessor with ProcessInputForParsing and ProcessOutputForDisplay
  - Integrated with complete UAX #9 BiDi algorithm (BidiAlgorithm.ProcessString)

#### Console BiDi Integration
- [✅] **BiDi Output Rendering:** Successfully integrated BiDi algorithm with console output
  - ✅ BiDi algorithm working correctly for Arabic text processing
  - ✅ Arabic text properly reordered for RTL display
  - ✅ Mixed Arabic/English content processing functional
  - ✅ **RESOLVED:** Input/Output separation implemented and working
- [✅] **RTL Input Handling:** **BREAKTHROUGH - Arabic input now working!**
  - ✅ **RESOLVED:** Windows Console API ReadConsoleW bypasses input limitations
  - ✅ Arabic input properly captured and processed
  - ✅ Arabic commands execute successfully (احصل-مساعدة working)
  - ✅ Multiple input strategies with auto-detection and fallback
- [✅] **Mixed-Direction Text:** Working with proper separation
  - ✅ Arabic text display with proper BiDi processing
  - ✅ Input remains in logical order for command parsing
  - ✅ Output processed through BiDi algorithm for display
- [🔧] **Console Display Issues:** Current focus area
  - ❌ **Prompt Positioning:** ArbSh> prompt appears on left instead of right for RTL
  - ❌ **Arabic Character Connection:** Arabic characters appear disconnected/malformed
  - ❌ **RTL Layout:** Console layout needs proper RTL text flow adjustments

#### ✅ Technical Challenges Resolved
- [✅] **Console Input Encoding:** **RESOLVED** - Windows console Arabic input working
  - **Solution Implemented:** ReadConsoleW direct Windows Console API access
  - **Result:** Arabic commands can now be typed and executed successfully
  - **Architecture:** Multiple input strategies with auto-detection and fallback
- [✅] **Input/Output Processing Separation:** **RESOLVED** - Architecture implemented
  - **Implementation:** BiDiTextProcessor with separate logical/visual processing
  - **Result:** Input remains in logical order, output gets proper BiDi processing
  - **Integration:** Full UAX #9 BiDi algorithm integration for display output

#### 🔧 Current Technical Challenges
- [🔧] **Console Display Rendering:** Windows Console RTL display limitations
  - **Issue:** Prompt positioning - ArbSh> appears on left instead of right
  - **Issue:** Arabic character shaping - characters appear disconnected
  - **Issue:** RTL text flow - overall console layout needs RTL adjustments
  - **Root Cause:** Windows Console rendering doesn't handle RTL layout properly
  - **Solutions to Implement:**
    - Custom prompt positioning for RTL layout
    - Arabic character shaping and connection handling
    - Console-specific RTL text positioning adjustments

#### Arabic Localization Enhancement
- [ ] **Arabic Error Messages:** Translate all error messages to Arabic
- [ ] **Arabic Help Text:** Complete Arabic help system and documentation
- [ ] **Arabic Parameter Names:** Expand Arabic parameter support across all cmdlets
- [ ] **Cultural Localization:** Arabic number formatting and date/time display

#### Console Enhancement
- [ ] **Enhanced REPL:** Improved Read-Eval-Print Loop with BiDi support
- [ ] **Text Selection:** RTL-aware text selection and clipboard operations
- [ ] **Command History:** BiDi-aware command history navigation
- [ ] **Tab Completion:** Arabic-aware command and parameter completion

### 📋 Phase 6: External Process Integration (Future)
**System Command Execution & Process Management**

#### Process Execution
- [ ] **External Commands:** Execute system commands (git, notepad, etc.)
- [ ] **Process Pipeline:** Integrate external processes with object pipeline
- [ ] **Stream Handling:** Proper stdin/stdout/stderr handling for external processes
- [ ] **Arabic Path Support:** Handle Arabic file and directory names

#### System Integration
- [ ] **File System Operations:** Arabic-aware file system cmdlets
- [ ] **Registry Access:** Windows registry operations with Arabic support
- [ ] **Environment Variables:** Arabic-aware environment variable handling
- [ ] **Service Management:** System service control with Arabic interface

### 🔧 Phase 7: Advanced Scripting Features (Future)
**Complete Shell Scripting Environment**

#### Scripting Language
- [ ] **User Variables:** Dynamic variable creation and management
- [ ] **Functions:** User-defined function support with Arabic names
- [ ] **Flow Control:** if/else, loops, switch statements with Arabic keywords
- [ ] **Script Files:** .arbsh script file execution and module system

#### Advanced Features
- [ ] **Error Handling:** try/catch/finally blocks with Arabic keywords
- [ ] **Object Manipulation:** Advanced object property access and manipulation
- [ ] **Regular Expressions:** Arabic-aware regex support
- [ ] **Debugging:** Script debugging capabilities with Arabic interface

### 🎯 Long-Term Vision (Phase 8+)
**Community & Ecosystem Development**

#### Community Features
- [ ] **Package Manager:** Arabic cmdlet package distribution system
- [ ] **Community Modules:** Third-party Arabic cmdlet ecosystem
- [ ] **Documentation Platform:** Arabic developer documentation portal
- [ ] **Learning Resources:** Arabic shell scripting tutorials and guides

#### Advanced Integration
- [ ] **IDE Integration:** Visual Studio Code extension for ArbSh
- [ ] **Cloud Integration:** Azure/AWS cmdlets with Arabic interface
- [ ] **Database Connectivity:** Arabic-aware database cmdlets
- [ ] **Web Services:** REST API cmdlets with Arabic parameter names

## 📊 Current Status Summary

### ✅ Completed Phases (1-4)
- **Phase 1:** Project foundation and C# architecture ✅
- **Phase 2:** Core shell framework with object pipeline ✅
- **Phase 3:** Advanced parsing and Arabic tokenization ✅
- **Phase 4:** Complete BiDi UAX #9 compliance + advanced execution ✅

### 🎯 Current Milestone: Version 0.7.7.11
- **70+ BiDi Tests Passing:** Full Unicode BidiTest.txt compliance
- **Working Features:** Subexpression execution `$(...)` and type literals `[TypeName]`
- **Arabic Commands:** `احصل-مساعدة` (Get-Help) with Arabic parameters
- **Complete BiDi Algorithm:** All rule sets (P, X, W, N, I, L) implemented

### 🚧 Next Major Milestone: Phase 5 - Console I/O Integration
**Target:** RTL console input/output with BiDi rendering integration

#### Current Phase 5 Status
**Progress:** BiDi output rendering working, critical input issues identified
- ✅ **BiDi Algorithm Integration:** Successfully integrated with console output
- ✅ **Arabic Text Display:** Proper RTL rendering of Arabic text
- ❌ **Arabic Input Support:** Blocked by Windows console limitations
- 🔧 **Next Priority:** Resolve Arabic console input encoding issues

#### Critical Issues Requiring Resolution
1. **Arabic Console Input:** Windows console input encoding prevents Arabic character recognition
2. **Input/Output Separation:** Need architectural separation between input parsing and output display
3. **Console Environment Detection:** Implement fallback mechanisms for different console environments

## 🔄 Development Methodology

### Systematic Approach
1. **Study:** Comprehensive research and design documentation
2. **Design:** Technical specification with detailed implementation plan
3. **Implement:** Systematic feature implementation with testing
4. **Test:** Comprehensive testing including edge cases
5. **Document:** Update documentation and examples

### Quality Standards
- **Unicode Compliance:** Full UAX #9 BiDi algorithm implementation
- **Comprehensive Testing:** 70+ automated tests with BidiTest.txt validation
- **Arabic-First Design:** Native Arabic support, not translation layer
- **Cross-Platform:** .NET-based Windows/macOS/Linux compatibility
- **Documentation:** Technical design docs for each major feature

### Version Management
- **Incremental Versioning:** Patch-level increments for implementation phases
- **Changelog Maintenance:** Detailed technical change documentation
- **Roadmap Updates:** Regular progress tracking and milestone updates

## 🌟 Project Philosophy

**ArbSh is designed as an Arabic-first shell for the Arabic developer community.** Our approach prioritizes:

- **Cultural Authenticity:** Built by Arabic developers for Arabic developers
- **Technical Excellence:** Modern architecture with Unicode compliance
- **Community Focus:** Open development with Arabic developer input
- **Innovation:** Pioneering Arabic-native command-line computing

**Current Achievement:** Phase 4 complete with full BiDi algorithm implementation. Phase 5 in progress with BiDi output rendering working but Arabic console input issues identified requiring alternative input solutions.
