# ArbSh (C# Prototype) - Basic Usage Examples

This document provides examples for the currently implemented commands in the ArbSh C# prototype.

**Note:** The shell is in early development (v0.7.6). Features like advanced parsing, robust error handling, and Arabic language support are still under development. Pipeline execution is concurrent. Parsing for type literals, input redirection, and sub-expressions is implemented, but execution logic for these is pending.

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

## Variable Expansion (`$variableName`)

Variables start with `$` followed by their name. The parser replaces the variable token with its stored value *before* the token is used as an argument or a parameter value. Adjacent tokens (like `ValueIs:` and `$testVar`) are correctly concatenated into a single argument after expansion.

**Note:** Variable storage is currently a placeholder within the parser itself. A proper session state management system is needed for user-defined variables. The following examples use predefined test variables: `$testVar`, `$pathExample`, `$emptyVar`.

**Examples:**

*   **Simple Expansion:**
    ```powershell
    ArbSh> Write-Output $testVar
    DEBUG: Received command: Write-Output $testVar
    # ... (parser/executor debug output) ...
    Value from $testVar!
    ```
*   **Expansion with Adjacent Literals:**
    ```powershell
    ArbSh> Write-Output ValueIs:$testVar
    DEBUG: Received command: Write-Output ValueIs:$testVar
    # ... (parser/executor debug output) ...
    ValueIs:Value from $testVar!
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

## Escape Characters (`\`)

The backslash (`\`) is used as an escape character.
*   **Outside Quotes:** It causes the character immediately following it to be treated literally, ignoring its special meaning (like space, `$`, `;`, `|`).
*   **Inside Double Quotes (`"`):** Standard C-style escape sequences like `\n` (newline), `\t` (tab), `\"` (literal quote), `\\` (literal backslash), and `\$` (literal dollar) are interpreted. Unrecognized sequences (e.g., `\q`) result in the character following the backslash being included literally (e.g., `q`).
*   **Inside Single Quotes (`'`):** All characters, including backslashes, are treated literally.

**Examples:**

*   **Escaping Operators:**
    ```powershell
    ArbSh> Write-Output Command1 \| Command2 ; Write-Output Argument\;WithSemicolon
    # ... (DEBUG output) ...
    Command1 | Command2
    Argument;WithSemicolon
    ```
*   **Escaping Quotes Inside Double Quotes:**
    ```powershell
    ArbSh> Write-Output "Argument with \"escaped quote\""
    # ... (DEBUG output) ...
    Argument with "escaped quote"
    ```
*   **Escaping Backslash Inside Double Quotes:**
    ```powershell
    ArbSh> Write-Output "Path: C:\\Users\\Test"
    # ... (DEBUG output) ...
    Path: C:\Users\Test
    ```
*   **Newline and Tab Inside Double Quotes:**
    ```powershell
    ArbSh> Write-Output "First Line\nSecond Line\tIndented"
    # ... (DEBUG output) ...
    First Line
    Second Line	Indented
    ```
*   **Escaping Variable Expansion Inside Double Quotes:**
    ```powershell
    ArbSh> Write-Output "Value is \$testVar"
    # ... (DEBUG output) ...
    Value is $testVar
    ```
*   **Escaping Variable Expansion Outside Quotes:**
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
*   **Output Redirection (`>` Overwrite, `>>` Append):** Writes the final standard output (stdout) of a pipeline to a file instead of the console.
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
*   **Advanced Redirection (v0.7.5+):** The parser now recognizes more complex redirection operators.
    *   Redirect Standard Error (stderr, stream 2) to a file:
        ```powershell
        ArbSh> Some-Command-That-Errors 2> error.log # Creates/overwrites error.log with stderr
        ```
    *   Append Standard Error (stderr) to a file:
        ```powershell
        ArbSh> Another-Command-That-Errors 2>> error.log # Appends stderr to error.log
        ```
    *   Redirect Standard Error (stderr) to Standard Output (stdout, stream 1):
        ```powershell
        ArbSh> Command-With-Output-And-Error 2>&1 | Write-Output # Merges stderr into stdout stream
        ```
    *   Redirect Standard Output (stdout) to a file and Standard Error (stderr) to the same file (by merging stderr to stdout first):
        ```powershell
        ArbSh> Command-With-Output-And-Error > output_and_error.log 2>&1 # Both streams go to the file
        ```
    *   Redirect Standard Output (stdout) to a file and Standard Error (stderr) to a different file:
        ```powershell
        ArbSh> Command-With-Output-And-Error > output.log 2> error.log # Separates streams
        ```
    *(Note: The parser recognizes all these forms, including input redirection `<`. The `Executor` currently handles stdout (`>`, `>>`) and stderr (`2>`, `2>>`) file redirection. Stream merging (`2>&1`, `1>&2`) and input redirection (`<`) execution are not yet implemented.)*

## Type Literals (`[TypeName]`) (v0.7.6+)

The parser now recognizes type literals enclosed in square brackets, like `[int]`, `[string]`, `[System.ConsoleColor]`. Whitespace inside the brackets is allowed, and Unicode characters are supported in the type name.

Currently, the parser identifies these tokens and passes the extracted type name (e.g., "int", "System.ConsoleColor") as a special string argument (`"TypeLiteral:TypeName"`) to the command. The shell does not yet *use* this type information for casting or parameter validation.

**Examples:**

```powershell
ArbSh> Write-Output [int] 123
# ... (DEBUG output) ...
DEBUG (Parser): Added TypeLiteral 'int' as argument.
# ...
TypeLiteral:int # Output shows the parsed argument

ArbSh> Write-Output [System.ConsoleColor] "Red"
# ... (DEBUG output) ...
DEBUG (Parser): Added TypeLiteral 'System.ConsoleColor' as argument.
# ...
TypeLiteral:System.ConsoleColor # Output shows the parsed argument

ArbSh> Write-Output Before[string]After
# ... (DEBUG output) ...
DEBUG (Parser): Added TypeLiteral 'string' as argument.
# ...
Before # Write-Output receives "Before", "TypeLiteral:string", "After" as args
       # and outputs the first one.
```

## Input Redirection (`<`) (v0.7.6+)

The parser now recognizes the input redirection operator `<` followed by a filename (which can be quoted).

Currently, the parser identifies the operator and filename and stores the path in the `ParsedCommand` object. The `Executor` does not yet use this information to redirect standard input for the command.

**Examples:**

```powershell
# First, create a file to read from:
ArbSh> Write-Output "This is the input file." > input.txt

# Now, use input redirection (Parser recognizes it, Executor ignores it for now):
ArbSh> Get-Command < input.txt
# ... (DEBUG output) ...
DEBUG (ParsedCommand): Set input redirection to: < input.txt
# ... (Output of Get-Command is shown, as stdin isn't actually redirected yet) ...
احصل-مساعدة
Get-Command
Get-Help
Test-Array-Binding
Write-Output
```

## Sub-expressions (`$(...)`) (v0.7.6+)

The parser now correctly recognizes and parses sub-expressions enclosed in `$()`. It handles nested sub-expressions and pipelines within them.

The parser recursively parses the content inside the `$()` and stores the resulting command structure (a `List<ParsedCommand>`) as an argument object passed to the outer command.

**Important:** The `Executor` does **not** yet execute these sub-expressions. Therefore, the outer command receives the parsed structure itself as an argument, not the *output* of the sub-expression.

**Examples:**

*   **Simple Sub-expression:**
    ```powershell
    ArbSh> Write-Output $(Get-Command)
    # ... (DEBUG output) ...
    DEBUG (Parser): Recursively parsing subexpression content: 'Get-Command'
    DEBUG (Parser): Added parsed subexpression (statement 0) as argument.
    WARN (Binder): Skipping non-string positional argument at index 0 for parameter 'InputObject'. Subexpression execution not implemented.
    # ... (No output from Write-Output as it received a List<ParsedCommand> object)
    ```
*   **Sub-expression with Pipeline:**
    ```powershell
    ArbSh> Write-Output $(Get-Command | Write-Output)
    # ... (DEBUG output) ...
    DEBUG (Parser): Recursively parsing subexpression content: 'Get-Command | Write-Output'
    DEBUG (Parser): Added parsed subexpression (statement 0) as argument.
    WARN (Binder): Skipping non-string positional argument at index 0 for parameter 'InputObject'. Subexpression execution not implemented.
    # ... (No output from outer Write-Output)
    ```
*   **Sub-expression as Parameter Value:**
    ```powershell
    ArbSh> Get-Help -CommandName $(Write-Output Get-Command)
    # ... (DEBUG output) ...
    DEBUG (Parser): Recursively parsing subexpression content: 'Write-Output Get-Command'
    DEBUG (Parser): Added parsed subexpression (statement 0) as argument.
    WARN (Binder): Skipping non-string positional argument at index 0 for parameter 'CommandName'. Subexpression execution not implemented.
    # ... (Get-Help likely shows general help or an error as CommandName wasn't bound)
    ```

## Arabic Command and Parameter Names (v0.7.0+)

Cmdlets and their parameters can be invoked using Arabic names if they have the `[ArabicName]` attribute applied. The previous encoding issues that prevented this from working correctly in test scripts have been resolved.

**Example (using `Get-Help` / `احصل-مساعدة` which has aliases defined):**

*   **Arabic Command Name:**
    ```powershell
    ArbSh> احصل-مساعدة Get-Command
    # ... (Executor debug output) ...
    NAME
        Get-Command
    # ...
    ```
*   **Arabic Parameter Name:**
    ```powershell
    ArbSh> Get-Help -الاسم Get-Command
    # ... (Executor debug output) ...
    DEBUG (Binder): Found parameter via Arabic name '-الاسم'.
    # ...
    NAME
        Get-Command
    # ...
    ```
*   **Arabic Command and Parameter Name:**
    ```powershell
    ArbSh> احصل-مساعدة -الاسم Get-Command
    # ... (Executor debug output) ...
    DEBUG (Binder): Found parameter via Arabic name '-الاسم'.
    # ...
    NAME
        Get-Command
    # ...
    ```

This covers the basic usage of the commands available in the current prototype.
