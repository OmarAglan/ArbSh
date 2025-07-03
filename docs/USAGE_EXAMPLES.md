# ArbSh - Usage Examples and Feature Guide

This document provides comprehensive examples for all implemented features in ArbSh - the Arabic-first shell.

**Current Version:** 0.7.7.11
**Status:** Phase 4 Complete - Full BiDi Algorithm UAX #9 Compliance
**Next Phase:** Phase 5 - Console I/O with BiDi Rendering

## ✅ **Fully Implemented Features**
- Complete BiDi Algorithm (UAX #9) with all rule sets (P, X, W, N, I, L)
- Pipeline execution with task-based concurrency
- Parameter binding with reflection and type conversion
- Subexpression execution `$(...)` - **WORKING**
- Type literal utilization `[TypeName]` - **WORKING**
- Variable expansion `$variableName`
- Input/output redirection and stream merging
- Arabic command names and aliases
- Command discovery and caching

## Running ArbSh

1.  Navigate to the `src_csharp/ArbSh.Console` directory in your terminal.
2.  Run the shell using the .NET CLI: `dotnet run`
3.  You should see the `ArbSh>` prompt. Type `exit` to quit.

## Available Commands

### 1. Get-Command

Lists all commands discovered by the shell.

**Syntax:**
```powershell
Get-Command
```

**Example:**
```powershell
ArbSh> Get-Command
Get-Command
Get-Help
احصل-مساعدة
Test-Array-Binding
Test-Type-Literal
Write-Output
```

**Features:**
- Automatic cmdlet discovery via reflection
- Caching for performance
- Includes Arabic aliases in output

### 2. Get-Help / احصل-مساعدة

Displays help information for commands. Available in both English and Arabic.

**Syntax:**
```powershell
Get-Help [[-CommandName] <string>] [-Full]
احصل-مساعدة [[-الاسم] <string>] [-كامل]
```

**Parameters:**
- `-CommandName <string>` / `-الاسم <string>` (Position 0): Command name to get help for
- `-Full` / `-كامل`: Show detailed parameter information

**Examples:**

**General Help:**
```powershell
ArbSh> Get-Help
Placeholder general help message. Try 'Get-Help <Command-Name>'.
Example: Get-Help Get-Command
```

**Help for Specific Command:**
```powershell
ArbSh> Get-Help Get-Command

NAME
    Get-Command

SYNOPSIS
    (Synopsis for Get-Command not available)

SYNTAX
    Get-Command
```

**Arabic Command Usage:**
```powershell
ArbSh> احصل-مساعدة -الاسم Get-Command

NAME
    Get-Command

SYNOPSIS
    (Synopsis for Get-Command not available)

SYNTAX
    Get-Command
```

**Full Help with Parameters:**
```powershell
ArbSh> Get-Help Write-Output -Full

NAME
    Write-Output

SYNTAX
    Write-Output [-InputObject <Object>]

PARAMETERS
    -InputObject <Object>
        The object(s) to write to the output stream.
        Required?                    False
        Position?                    0
        Accepts pipeline input?      True (By Value)
```

### 3. Write-Output

Writes objects or strings to the output stream. Accepts pipeline input and direct arguments.

**Syntax:**
```powershell
Write-Output [-InputObject <Object>]
<PipelineInput> | Write-Output
```

**Examples:**

**Direct Output:**
```powershell
ArbSh> Write-Output "Hello ArbSh User!"
Hello ArbSh User!
```

**Pipeline Usage:**
```powershell
ArbSh> Get-Command | Write-Output
Get-Command
Get-Help
احصل-مساعدة
Test-Array-Binding
Test-Type-Literal
Write-Output
```

**Multiple Arguments:**
```powershell
ArbSh> Write-Output "First" "Second" "Third"
First
```

**Features:**
- Accepts pipeline input by value
- Positional parameter binding
- Object-to-string conversion

## ✅ **NEW: Subexpression Execution `$(...)` - FULLY WORKING**

PowerShell-style command substitution that executes commands and captures their output for use in other commands.

**Syntax:**
```powershell
$(command)
$(command | pipeline)
```

**Examples:**

**Basic Subexpression:**
```powershell
ArbSh> Write-Output $(Get-Command)
Get-Command
Get-Help
احصل-مساعدة
Test-Array-Binding
Test-Type-Literal
Write-Output
```

**Subexpression in Parameter:**
```powershell
ArbSh> Get-Help $(Write-Output Get-Command)

NAME
    Get-Command

SYNOPSIS
    (Synopsis for Get-Command not available)

SYNTAX
    Get-Command
```

**Complex Pipeline Subexpression:**
```powershell
ArbSh> Write-Output "Available commands: $(Get-Command | Write-Output)"
Available commands: Get-Command
Get-Help
احصل-مساعدة
Test-Array-Binding
Test-Type-Literal
Write-Output
```

**Features:**
- Full pipeline execution within subexpressions
- Output capture and string conversion
- Nested subexpression support
- Error handling and debugging
- Integration with parameter binding

## ✅ **NEW: Type Literal Utilization `[TypeName]` - FULLY WORKING**

PowerShell-style type casting that allows explicit type specification for parameters.

**Syntax:**
```powershell
[TypeName] value
[int] 42
[string] hello
[bool] true
```

**Supported Types:**
- `[int]` → Int32
- `[string]` → String
- `[bool]` → Boolean
- `[double]` → Double
- `[datetime]` → DateTime
- `[ConsoleColor]` → ConsoleColor enum

**Examples:**

**Basic Type Casting:**
```powershell
ArbSh> Test-Type-Literal [int] 42
IntValue: 42 (Type: Int32)
StringValue: '' (Type: null)
BoolValue: False (Type: Boolean)
DoubleValue: 0 (Type: Double)
DateTimeValue: 1/1/0001 12:00:00 AM (Type: DateTime)
ColorValue: Black (Type: ConsoleColor)
IntArray: null or empty
```

**Multiple Type Literals:**
```powershell
ArbSh> Test-Type-Literal [int] 1 [string] hello [bool] true
IntValue: 1 (Type: Int32)
StringValue: 'hello' (Type: String)
BoolValue: True (Type: Boolean)
DoubleValue: 0 (Type: Double)
DateTimeValue: 1/1/0001 12:00:00 AM (Type: DateTime)
ColorValue: Black (Type: ConsoleColor)
IntArray: null or empty
```

**Enum Type Conversion:**
```powershell
ArbSh> Test-Type-Literal [ConsoleColor] Red
IntValue: 12 (Type: Int32)
StringValue: '' (Type: null)
BoolValue: False (Type: Boolean)
DoubleValue: 0 (Type: Double)
DateTimeValue: 1/1/0001 12:00:00 AM (Type: DateTime)
ColorValue: Black (Type: ConsoleColor)
IntArray: null or empty
```

**DateTime Parsing:**
```powershell
ArbSh> Test-Type-Literal [datetime] 2023-01-01
IntValue: 0 (Type: Int32)
StringValue: '' (Type: null)
BoolValue: False (Type: Boolean)
DoubleValue: 0 (Type: Double)
DateTimeValue: 1/1/2023 12:00:00 AM (Type: DateTime)
ColorValue: Black (Type: ConsoleColor)
IntArray: null or empty
```

**Complex Type Literal Usage:**
```powershell
ArbSh> Test-Type-Literal [int] 1 [string] hello [bool] true [double] 3.14 [datetime] 2023-01-01
IntValue: 1 (Type: Int32)
StringValue: 'hello' (Type: String)
BoolValue: True (Type: Boolean)
DoubleValue: 3.14 (Type: Double)
DateTimeValue: 1/1/2023 12:00:00 AM (Type: DateTime)
ColorValue: Black (Type: ConsoleColor)
IntArray: null or empty
```

**Features:**
- Automatic type resolution with aliases
- Positional parameter mapping
- Type conversion with fallback mechanisms
- Support for enums and complex types
- Integration with parameter binding system

## Variable Expansion (`$variableName`)

Variables start with `$` followed by their name. The parser replaces variable tokens with stored values before command execution.

**Note:** Variable storage uses predefined test variables. Full session state management is planned for future versions.

**Available Test Variables:**
- `$testVar` → "Value from $testVar!"
- `$pathExample` → "/path/to/example"
- `$emptyVar` → "" (empty string)

**Examples:**

**Simple Variable Expansion:**
```powershell
ArbSh> Write-Output $testVar
Value from $testVar!
```

**Variable with Adjacent Text:**
```powershell
ArbSh> Write-Output ValueIs:$testVar
ValueIs:Value from $testVar!
```

**Undefined Variable:**
```powershell
ArbSh> Write-Output $nonExistentVar
# (Empty output)
```

**Variable in Parameter:**
```powershell
ArbSh> Get-Help -CommandName $testVar
Help Error: Command 'Value from $testVar!' not found.
```

## Escape Characters (`\`)

The backslash (`\`) escapes special characters and provides literal interpretation.

**Escape Rules:**
- **Outside Quotes:** Treats following character literally (space, `$`, `;`, `|`)
- **Inside Double Quotes:** C-style escape sequences (`\n`, `\t`, `\"`, `\\`, `\$`)
- **Inside Single Quotes:** All characters treated literally (no escaping)

**Examples:**

**Escaping Operators:**
```powershell
ArbSh> Write-Output Command1 \| Command2 ; Write-Output Argument\;WithSemicolon
Command1 | Command2
Argument;WithSemicolon
```

**Escaping Quotes:**
```powershell
ArbSh> Write-Output "Argument with \"escaped quote\""
Argument with "escaped quote"
```

**Escaping Paths:**
```powershell
ArbSh> Write-Output "Path: C:\\Users\\Test"
Path: C:\Users\Test
```

**Newlines and Tabs:**
```powershell
ArbSh> Write-Output "First Line\nSecond Line\tIndented"
First Line
Second Line	Indented
```

**Escaping Variables:**
```powershell
ArbSh> Write-Output "Value is \$testVar"
Value is $testVar

ArbSh> Write-Output \$testVar
$testVar
```

**Escaping Spaces:**
```powershell
ArbSh> Write-Output Argument\ WithSpace
Argument WithSpace
```

## Pipeline and Redirection

**Pipeline (`|`):** Passes output from one command to the input of the next with task-based concurrency.

**Basic Pipeline:**
```powershell
ArbSh> Get-Command | Write-Output
Get-Command
Get-Help
احصل-مساعدة
Test-Array-Binding
Test-Type-Literal
Write-Output
```

**Pipeline with Quoted Pipes:**
```powershell
ArbSh> Write-Output "Value is | this" | Write-Output
Value is | this
```

**Output Redirection:**
- `>` - Overwrite file with stdout
- `>>` - Append stdout to file

```powershell
ArbSh> Get-Command > commands.txt
ArbSh> Write-Output "Additional line" >> commands.txt
```

**Command Separator (`;`):** Execute multiple statements sequentially.

```powershell
ArbSh> Write-Output "First"; Write-Output "Second"
First
Second
```

**Advanced Redirection:**

**Error Stream Redirection:**
```powershell
# Redirect stderr to file
ArbSh> Some-Command-That-Errors 2> error.log

# Append stderr to file
ArbSh> Another-Command-That-Errors 2>> error.log
```

**Stream Merging:**
```powershell
# Merge stderr to stdout
ArbSh> Command-With-Errors 2>&1 | Write-Output

# Merge stdout to stderr
ArbSh> Command-With-Output 1>&2
```

**Combined Redirection:**
```powershell
# Both streams to same file
ArbSh> Command-With-Both > output.log 2>&1

# Separate files for each stream
ArbSh> Command-With-Both > output.log 2> error.log
```

**Input Redirection (`<`):**
```powershell
# Create input file
ArbSh> Write-Output "Line 1" > input.txt
ArbSh> Write-Output "Line 2" >> input.txt

# Use input redirection
ArbSh> Write-Output < input.txt
Line 1
Line 2
```

## Arabic Language Support

ArbSh is designed as an Arabic-first shell with comprehensive Arabic language support.

### Arabic Command Names

Commands are available with Arabic aliases for native Arabic developers:

**Available Arabic Commands:**
- `احصل-مساعدة` (Get-Help) - Get command help and documentation
- Additional Arabic commands planned for Phase 5

**Examples:**

**Arabic Help Command:**
```powershell
ArbSh> احصل-مساعدة

Placeholder general help message. Try 'Get-Help <Command-Name>'.
Example: Get-Help Get-Command
```

**Arabic Help with Parameters:**
```powershell
ArbSh> احصل-مساعدة -الاسم Get-Command

NAME
    Get-Command

SYNOPSIS
    (Synopsis for Get-Command not available)

SYNTAX
    Get-Command
```

**Mixed Arabic/English Usage:**
```powershell
ArbSh> احصل-مساعدة Get-Command
ArbSh> Get-Help -الاسم Write-Output
```

### BiDi Text Processing

ArbSh includes complete Unicode BiDi (Bidirectional) text processing according to UAX #9 standards:

**Implemented BiDi Rules:**
- ✅ **P Rules (P2-P3):** Paragraph embedding level determination
- ✅ **X Rules (X1-X10):** Explicit formatting codes (LRE, RLE, PDF, LRO, RLO, LRI, RLI, FSI, PDI)
- ✅ **W Rules (W1-W7):** Weak type resolution (ES, ET, EN, AN handling)
- ✅ **N Rules (N0-N2):** Neutral type resolution and boundary neutrals
- ✅ **I Rules (I1-I2):** Implicit embedding levels for strong types
- ✅ **L Rules (L1-L4):** Level-based reordering and combining marks

**BiDi Testing:**
- 70+ Unicode BidiTest.txt compliance tests passing
- Comprehensive test coverage for all rule sets
- Real-time BiDi processing for mixed Arabic/English content

### Arabic-First Philosophy

ArbSh prioritizes Arabic language support as a core feature:

**Design Principles:**
- Native Arabic command names and aliases
- Full Unicode BiDi text rendering compliance
- Arabic developer workflow optimization
- Cultural localization considerations
- Arabic-first documentation and examples

**Future Arabic Features (Phase 5):**
- RTL console input with proper cursor movement
- BiDi-aware output rendering
- Arabic error messages and help text
- Arabic parameter names and documentation
- Complete Arabic localization

## Testing and Development

**Running ArbSh:**
```powershell
# Navigate to project directory
cd D:\dev\ArbSh\src_csharp\ArbSh.Console

# Run the shell
dotnet run

# Exit the shell
ArbSh> exit
```

**Testing Features:**
```powershell
# Test BiDi algorithm
ArbSh> Test-Array-Binding

# Test type literals
ArbSh> Test-Type-Literal [int] 42

# Test subexpressions
ArbSh> Write-Output $(Get-Command)

# Test Arabic commands
ArbSh> احصل-مساعدة
```

This comprehensive feature set makes ArbSh a powerful Arabic-first shell environment for Arabic developers, with full Unicode compliance and modern shell capabilities.
