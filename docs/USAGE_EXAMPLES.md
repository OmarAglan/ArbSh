# ArbSh (C# Prototype) - Basic Usage Examples

This document provides examples for the currently implemented commands in the ArbSh C# prototype.

**Note:** The shell is in early development (v0.6.0). Features like advanced parsing, robust error handling, and Arabic language support are still under development. Pipeline execution is now concurrent.

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
DEBUG (Executor): Executing 1 statement(s)...
DEBUG (Executor): --- Executing Statement (1 command(s)) ---
DEBUG (Executor Pipeline): Preparing stage 0: 'Get-Command'...
DEBUG (Executor): Waiting for 1 task(s) in the pipeline to complete...
DEBUG (Executor Task): Starting task for 'Get-Command'...
DEBUG (Binder): Binding parameters for GetCommandCmdlet...
DEBUG (Executor Task): 'Get-Command' has no pipeline input, calling ProcessRecord once.
DEBUG (Executor Task): 'Get-Command' finished processing.
DEBUG (Executor Task): Stage 'Get-Command' completed adding output.
DEBUG (Executor): All pipeline tasks for the statement completed.
DEBUG (Executor Pipeline): Final pipeline output to Console:
Get-Command
Get-Help
Write-Output
DEBUG (Executor): --- Statement execution finished ---
DEBUG (Executor): All statements executed.

```
*(Note: The output objects are now `ArbSh.Console.Models.CommandInfo`, although their default string representation is the command name.)*

### 2. Get-Help

Displays help information for commands.

**Syntax:**

```powershell
Get-Help [[-CommandName] <string>] [-Full]
```

**Parameters:**

*   `-CommandName <string>` (Positional 0): The name of the command to get help for. If omitted, general help is shown.
*   `-Full`: Displays detailed parameter information, including pipeline input acceptance.

**Examples:**

*   **General Help:**
    ```powershell
    ArbSh> Get-Help
    # ... (Executor debug output similar to Get-Command example) ...
    Placeholder general help message. Try 'Get-Help <Command-Name>'.
    Example: Get-Help Get-Command
    # ... (Executor debug output) ...
    ```
*   **Help for a specific command:**
    ```powershell
    ArbSh> Get-Help Get-Command
    ArbSh> Get-Help Get-Command
    # ... (Executor debug output) ...
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
    ArbSh> Get-Help -CommandName Write-Output -Full
    # ... (Executor debug output) ...
    DEBUG (Binder): Bound named parameter '-CommandName' to value 'Write-Output' (Type: String)
    DEBUG (Binder): Bound switch parameter '-Full' to true (no value provided).

    NAME
        Write-Output

    SYNOPSIS
        (Synopsis for Write-Output not available)

    SYNTAX
        Write-Output [-InputObject <Object>]

    PARAMETERS
        -InputObject <Object>
            The object(s) to write to the output stream.
            Required?                    False
            Position?                    0
            Accepts pipeline input?      True (By Value)

    # ... (Executor debug output) ...
    ```

### 3. Write-Output

Writes objects or strings to the output (console or redirected file). It accepts pipeline input or direct arguments via its `-InputObject` parameter.

**Syntax:**

```powershell
Write-Output [-InputObject <Object>]
<PipelineInput> | Write-Output
```

**Examples:**

*   **Writing arguments directly (positional binding to -InputObject):**
    ```powershell
    ArbSh> Write-Output "Hello ArbSh User!"
    # ... (Executor debug output) ...
    DEBUG (Binder): Bound positional parameter at 0 ('Hello ArbSh User!') to property 'InputObject' (Type: Object)
    # ...
    Hello ArbSh User!
    # ...
    ```
*   **Using in a pipeline (Get-Command output objects piped to Write-Output):**
    ```powershell
    ArbSh> Get-Command | Write-Output
    # ... (Executor debug output showing concurrent tasks) ...
    DEBUG (Executor Task): 'Write-Output' consuming input...
    DEBUG (Executor Task): 'Write-Output' finished consuming input.
    # ...
    Get-Command
    Get-Help
    Write-Output
    # ...
    ```
*   **Binding multiple positional arguments to an array parameter (Example - if a cmdlet had `[Parameter(Position=0)] public string[] Paths { get; set; }`):**
    ```powershell
    ArbSh> Some-Cmdlet file1.txt file2.log path/to/dir
    # ... (Executor debug output) ...
    DEBUG (Binder): Bound 3 remaining positional argument(s) starting at 0 to array parameter 'Paths' (Type: String[])
    # ...
    ```
    *(Note: This array binding currently works for positional parameters.)*

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
*   **Output Redirection (`>` Overwrite, `>>` Append):** Writes the final output of a pipeline to a file instead of the console.
    ```powershell
    ArbSh> Get-Command | Write-Output > commands.txt
    ArbSh> Write-Output "Adding this line" >> commands.txt
    ```
*   **Command Separator (`;`):** Executes multiple statements sequentially. Each statement can contain its own pipeline, which runs concurrently within that statement.
    ```powershell
    ArbSh> Write-Output "Statement 1"; Get-Command | Write-Output
    # ... (Executor debug output for statement 1) ...
    Statement 1
    # ... (Executor debug output for statement 2 pipeline) ...
    Get-Command
    Get-Help
    Write-Output
    # ...
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
