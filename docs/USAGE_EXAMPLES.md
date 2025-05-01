# ArbSh (C# Prototype) - Basic Usage Examples

This document provides examples for the currently implemented commands in the ArbSh C# prototype.

**Note:** The shell is in early development. Features like advanced parsing, full pipeline support, error handling, and Arabic language support are still under development.

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
DEBUG (Executor): Executing 1 command(s)...
DEBUG (Executor Pipeline): Processing stage 0: 'Get-Command'...
DEBUG (Binder): Binding parameters for GetCommandCmdlet...
DEBUG (Executor Pipeline): Final pipeline output to Console:
Get-Command
Get-Help
Write-Output
DEBUG (Executor): Pipeline execution finished.
```

### 2. Get-Help

Displays help information for commands.

**Syntax:**

```powershell
Get-Help [[-CommandName] <string>] [-Full]
```

**Parameters:**

*   `-CommandName <string>` (Positional 0): The name of the command to get help for. If omitted, general help is shown.
*   `-Full`: Displays detailed parameter information (if available).

**Examples:**

*   **General Help:**
    ```powershell
    ArbSh> Get-Help
    DEBUG (Executor): Executing 1 command(s)...
    DEBUG (Executor Pipeline): Processing stage 0: 'Get-Help'...
    DEBUG (Binder): Binding parameters for GetHelpCmdlet...
    DEBUG (Executor Pipeline): Final pipeline output to Console:
    Placeholder general help message. Try 'Get-Help <Command-Name>'.
    Example: Get-Help Get-Command
    DEBUG (Executor): Pipeline execution finished.
    ```
*   **Help for a specific command:**
    ```powershell
    ArbSh> Get-Help Get-Command
    DEBUG (Executor): Executing 1 command(s)...
    DEBUG (Executor Pipeline): Processing stage 0: 'Get-Help'...
    DEBUG (Binder): Binding parameters for GetHelpCmdlet...
    DEBUG (Binder): Bound positional parameter at 0 ('Get-Command') to property 'CommandName' (Type: String)

    NAME
        Get-Command

    SYNOPSIS
        (Synopsis for Get-Command not available)

    SYNTAX
        Get-Command

    DEBUG (Executor): Pipeline execution finished.
    ```
*   **Full help for a specific command:**
    ```powershell
    ArbSh> Get-Help -CommandName Write-Output -Full
    DEBUG (Executor): Executing 1 command(s)...
    DEBUG (Executor Pipeline): Processing stage 0: 'Get-Help'...
    DEBUG (Binder): Binding parameters for GetHelpCmdlet...
    DEBUG (Binder): Bound named parameter '-CommandName' to value 'Write-Output' (Type: String)
    DEBUG (Binder): Bound switch parameter '-Full' to true (no value provided).

    NAME
        Write-Output

    SYNOPSIS
        (Synopsis for Write-Output not available)

    SYNTAX
        Write-Output

    PARAMETERS
        (No parameters defined for Write-Output yet)

    DEBUG (Executor): Pipeline execution finished.
    ```
    *(Note: Parameter details depend on the target cmdlet having `[Parameter]` attributes defined)*

### 3. Write-Output

Writes objects or strings to the output (currently the console or redirected file). It primarily processes pipeline input but can also take direct arguments.

**Syntax:**

```powershell
Write-Output [<arguments...>]
<PipelineInput> | Write-Output
```

**Examples:**

*   **Writing arguments directly:**
    ```powershell
    ArbSh> Write-Output Hello ArbSh User!
    DEBUG (Executor): Executing 1 command(s)...
    DEBUG (Executor Pipeline): Processing stage 0: 'Write-Output'...
    DEBUG (Binder): Binding parameters for WriteOutputCmdlet...
    DEBUG (Executor Pipeline): Final pipeline output to Console:
    Hello ArbSh User!
    DEBUG (Executor): Pipeline execution finished.
    ```
*   **Using in a pipeline:**
    ```powershell
    ArbSh> Get-Command | Write-Output
    DEBUG (Executor): Executing 2 command(s)...
    DEBUG (Executor Pipeline): Processing stage 0: 'Get-Command'...
    DEBUG (Binder): Binding parameters for GetCommandCmdlet...
    DEBUG (Executor Pipeline): Processing stage 1: 'Write-Output'...
    DEBUG (Binder): Binding parameters for WriteOutputCmdlet...
    DEBUG (Executor Pipeline): Final pipeline output to Console:
    Get-Command
    Get-Help
    Write-Output
    DEBUG (Executor): Pipeline execution finished.
    ```

## Escape Characters (`\`)

The backslash (`\`) is used as an escape character. It causes the character immediately following it to be treated literally, ignoring its special meaning (like space, `$`, `;`, `|`, `"`). This works both outside and inside double quotes.

**Examples:**

*   **Escaping Operators:**
    ```powershell
    ArbSh> Write-Output Command1 \| Command2 ; Write-Output Argument\;WithSemicolon
    # ... (DEBUG output) ...
    Command1 | Command2
    Argument;WithSemicolon
    ```
*   **Escaping Quotes Inside Quotes:**
    ```powershell
    ArbSh> Write-Output "Argument with \"escaped quote\""
    # ... (DEBUG output) ...
    Argument with "escaped quote"
    ```
*   **Escaping Backslash:**
    ```powershell
    ArbSh> Write-Output Argument\\WithBackslash
    # ... (DEBUG output) ...
    Argument\WithBackslash
    ```
*   **Escaping Variable Expansion:**
    ```powershell
    ArbSh> Write-Output \$testVar
    # ... (DEBUG output) ...
    $testVar
    ```
*   **Escaping Space:**
    ```powershell
    ArbSh> Write-Output Argument\ WithSpace
    # ... (DEBUG output) ...
    Argument WithSpace
    ```

## Basic Pipeline and Redirection

*   **Pipeline (`|`):** Passes the output of one command to the input of the next. The parser correctly handles pipes inside quotes or when escaped.
    ```powershell
    ArbSh> Get-Command | Write-Output # Standard pipeline
    # ... (DEBUG output) ...
    Get-Command
    Get-Help
    Write-Output

    ArbSh> Write-Output "Value is | this" | Write-Output "Next stage" # Pipe inside quotes is ignored by parser
    # ... (DEBUG output) ...
    Value is | this
    ```
*   **Output Redirection (`>` Overwrite, `>>` Append):** Writes the final output to a file instead of the console. (Note: Redirection parsing is still basic and happens *after* tokenization).
    ```powershell
    ArbSh> Get-Command > commands.txt
    ArbSh> Write-Output "Adding this line" >> commands.txt
    ```
*   **Command Separator (`;`):** Executes commands sequentially (currently only the *first* command before a `;` is processed).
    ```powershell
    ArbSh> Write-Output "First part; still first part" ; Write-Output Second part # Semicolon outside quotes separates statements
    # ... (DEBUG output) ...
    First part; still first part
    Second part
    ```

## Variable Expansion (`$variableName`)

The parser now supports basic variable expansion. Variables start with `$` followed by their name. The parser replaces the variable token with its stored value *before* the token is used as an argument or a parameter value.

**Note:** Variable storage is currently a placeholder within the parser itself. A proper session state management system is needed for user-defined variables. The following examples use predefined test variables: `$testVar`, `$pathExample`, `$emptyVar`.

**Examples:**

*   **Simple Expansion:**
    ```powershell
    ArbSh> Write-Output $testVar
    DEBUG: Received command: Write-Output $testVar
    # ... (parser/executor debug output) ...
    Value from $testVar!
    ```
*   **Expansion with Literals:** (Note: String interpolation like `"Path: $pathExample"` is not yet supported, variables must be separate tokens)
    ```powershell
    ArbSh> Write-Output Path is: $pathExample
    DEBUG: Received command: Write-Output Path is: $pathExample
    # ... (parser/executor debug output) ...
    Path is: C:\Users
    ```
*   **Undefined Variable:**
    ```powershell
    ArbSh> Write-Output $nonExistentVar
    DEBUG: Received command: Write-Output $nonExistentVar
    # ... (parser/executor debug output) ...
    # (Output is an empty line)
    ```
*   **Expansion in Parameter Value:**
    ```powershell
    ArbSh> Get-Help -CommandName $testVar
    DEBUG: Received command: Get-Help -CommandName $testVar
    # ... (parser/executor debug output) ...
    Help Error: Command 'Value from $testVar!' not found.
    ```

This covers the basic usage of the commands available in the current prototype.
