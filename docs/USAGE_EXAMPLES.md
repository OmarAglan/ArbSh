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

## Basic Pipeline and Redirection

*   **Pipeline (`|`):** Passes the output of one command to the input of the next.
    ```powershell
    ArbSh> Get-Command | Get-Help # Tries to get help for each command name from Get-Command (limited use currently)
    ```
*   **Output Redirection (`>` Overwrite, `>>` Append):** Writes the final output to a file instead of the console.
    ```powershell
    ArbSh> Get-Command > commands.txt
    ArbSh> Write-Output "Adding this line" >> commands.txt
    ```
*   **Command Separator (`;`):** Executes commands sequentially (currently only the *first* command before a `;` is processed).
    ```powershell
    ArbSh> Get-Command ; Write-Output "This part is currently ignored"
    ```

This covers the basic usage of the commands available in the current prototype.
