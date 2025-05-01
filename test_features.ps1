# PowerShell script to test ArbSh v0.6.0 features

$ErrorActionPreference = 'Stop' # Exit script on error

# --- Configuration ---
$arbshProjectDir = "src_csharp/ArbSh.Console"
$outputLogFile = "test_output.log"
$tempRedirectFile = "temp_redirect_test.txt"
$tempAppendFile = "temp_append_test.txt"

# --- Functions ---
function Write-Log {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logLine = "[$timestamp] $Message"
    Write-Host $logLine
    Add-Content -Path $outputLogFile -Value $logLine
}

# --- Preparation ---
# Clear previous log file
if (Test-Path $outputLogFile) {
    Remove-Item $outputLogFile
}
# Clear previous temp files
if (Test-Path $tempRedirectFile) {
    Remove-Item $tempRedirectFile
}
if (Test-Path $tempAppendFile) {
    Remove-Item $tempAppendFile
}

Write-Log "Starting ArbSh v0.6.0 Feature Test..."

# --- Build Project ---
Write-Log "Building ArbSh project..."
try {
    # Allow build output to show in console for diagnostics
    dotnet build $arbshProjectDir
    Write-Log "Build successful."
} catch {
    Write-Log ("BUILD FAILED: " + $_.Exception.Message) # Use concatenation
    Exit 1
}

# --- Define Test Commands ---
# Array of commands to feed into ArbSh stdin
# Using mostly single quotes for PowerShell to treat them literally
$arbshCommands = @(
    '# --- Basic Commands ---'
    'Get-Command'
    'Write-Output ''Hello from Write-Output!''' # Inner single quotes need escaping for PS
    'Test-Array-Binding one two three' # Corrected command name
    'Test-Array-Binding -MySwitch four five' # Corrected command name
    ''
    '# --- Help System ---'
    'Get-Help'
    'Get-Help Get-Command'
    'Get-Help Write-Output -Full'
    'Get-Help Test-Array-Binding -Full' # Corrected command name
    'Get-Help NonExistentCommand'
    ''
    '# --- Pipeline ---'
    'Get-Command | Write-Output'
    '# Test pipeline binding (CommandInfo object to Write-Outputs InputObject)'
    'Get-Command | Test-Array-Binding # This should show 0 strings, as CommandInfo doesnt match string[]' # Corrected command name
    ''
    '# --- Statement Separator ---'
    'Write-Output ''Statement 1''; Write-Output ''Statement 2''' # Escape inner quotes
    'Get-Command; Write-Output ''After Get-Command''' # Escape inner quotes
    ''
    '# --- Variable Expansion (Using hardcoded $testVar) ---'
    'Write-Output $testVar' # ArbSh should expand this
    'Write-Output ValueIs:$testVar' # ArbSh should expand this
    'Write-Output $nonExistentVar # Should be empty'
    ''
    '# --- Escape Characters ---'
    'Write-Output ''Literal $testVar -> \$testVar''' # Escape $ for ArbSh
    'Write-Output ''Literal Pipe -> \|''' # Escape | for ArbSh
    'Write-Output ''Literal Semicolon -> \;''' # Escape ; for ArbSh
    'Write-Output ''Literal Quote -> \"''' # Escape " for ArbSh
    'Write-Output "Quoted string with \\"escaped quote\\" inside"' # Keep PS double quotes here, escape internal \ and " for ArbSh
    'Write-Output Argument\\ WithSpace' # Escape \ for ArbSh
    ''
    '# --- Parameter Binding Errors ---'
    'Get-Help -NonExistentParam' # Binder currently ignores unknown named parameters
    'Test-Array-Binding -InputStrings should fail' # Corrected command name, test binder rejecting named param for positional array
    'Write-Output -InputObject ''@(1,2)''' # Pass PS array syntax as a string literal
    ''
    '# --- Redirection ---'
    'Write-Output ''Testing overwrite redirection'' > temp_redirect_test.txt' # Pass > literally
    'Get-Command >> temp_append_test.txt' # Pass >> literally
    'Write-Output ''Appending another line'' >> temp_append_test.txt' # Pass >> literally
    ''
    '# --- Exit ---'
    'exit'
)

# Join commands with newline characters for stdin
$inputString = ($arbshCommands | ForEach-Object { "$_" }) -join [Environment]::NewLine

# --- Execute ArbSh and Capture Output ---
Write-Log "Executing ArbSh with test commands..."
Write-Log "--- ArbSh Start Output ---"

# Use Start-Process to capture stdout and stderr together
$processInfo = New-Object System.Diagnostics.ProcessStartInfo
$processInfo.FileName = "dotnet"
$processInfo.Arguments = "run --project $arbshProjectDir"
$processInfo.RedirectStandardInput = $true
$processInfo.RedirectStandardOutput = $true
$processInfo.RedirectStandardError = $true
$processInfo.UseShellExecute = $false
$processInfo.CreateNoWindow = $true
# Set working directory if needed, assuming script runs from project root
# $processInfo.WorkingDirectory = (Get-Location).Path

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $processInfo

$process.Start() | Out-Null

# Write commands to stdin
$process.StandardInput.WriteLine($inputString)
$process.StandardInput.Close() # Signal end of input

# Read output and error streams
$output = $process.StandardOutput.ReadToEnd()
$errorOutput = $process.StandardError.ReadToEnd()

$process.WaitForExit()

# Append captured output to log file
Add-Content -Path $outputLogFile -Value $output
if ($errorOutput) {
    Write-Log "--- ArbSh Error Output ---"
    Add-Content -Path $outputLogFile -Value $errorOutput
    Write-Log "--- End ArbSh Error Output ---"
}
Write-Log "--- ArbSh End Output ---"
Write-Log ("ArbSh process exited with code " + $process.ExitCode + ".") # Use concatenation

# --- Verify Redirection ---
Write-Log "Verifying redirection files..."
if (Test-Path $tempRedirectFile) {
    Write-Log ("Content of " + $tempRedirectFile + ":") # Use concatenation
    Get-Content $tempRedirectFile | Out-File -Append $outputLogFile
    Remove-Item $tempRedirectFile # Cleanup
} else {
    Write-Log "ERROR: $tempRedirectFile was not created."
}

if (Test-Path $tempAppendFile) {
    Write-Log ("Content of " + $tempAppendFile + ":") # Use concatenation
    Get-Content $tempAppendFile | Out-File -Append $outputLogFile
    Remove-Item $tempAppendFile # Cleanup
} else {
    Write-Log "ERROR: $tempAppendFile was not created."
}

Write-Log "Test script finished."
Write-Host "Test output captured in $outputLogFile"
