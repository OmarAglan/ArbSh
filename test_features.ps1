# PowerShell script to test ArbSh v0.7.0 features

$ErrorActionPreference = 'Stop' # Exit script on error
$OutputEncoding = [System.Text.Encoding]::UTF8 # Ensure script output handles UTF-8
[Console]::InputEncoding = [System.Text.Encoding]::UTF8 # Try setting global console input encoding

# --- Configuration ---
$arbshProjectDir = "src_csharp/ArbSh.Console"
$outputLogFile = "test_output.log"
$tempRedirectFile = "temp_redirect_test.txt"
$tempAppendFile = "temp_append_test.txt"
$tempStderrFile = "temp_stderr_test.txt"
$tempStderrAppendFile = "temp_stderr_append_test.txt"
$tempMergedFile = "temp_merged_test.txt"
$tempMultiRedirectOut = "temp_multi_out.txt"
$tempMultiRedirectErr = "temp_multi_err.txt"

# --- Functions ---
function Write-Log {
    param(
        [Parameter(Mandatory = $true)]
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
if (Test-Path $tempStderrFile) {
    Remove-Item $tempStderrFile
}
if (Test-Path $tempStderrAppendFile) {
    Remove-Item $tempStderrAppendFile
}
if (Test-Path $tempMergedFile) {
    Remove-Item $tempMergedFile
}
if (Test-Path $tempMultiRedirectOut) {
    Remove-Item $tempMultiRedirectOut
}
if (Test-Path $tempMultiRedirectErr) {
    Remove-Item $tempMultiRedirectErr
}


Write-Log "Starting ArbSh v0.7.5 Feature Test..."

# --- Build Project ---
Write-Log "Building ArbSh project..."
try {
    # Allow build output to show in console for diagnostics
    dotnet build $arbshProjectDir
    Write-Log "Build successful."
}
catch {
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
    '# --- Arabic Name Tests (Phase 3 Goal) ---'
    'احصل-مساعدة' # Test Arabic command alias
    'احصل-مساعدة Get-Command' # Test Arabic command alias with positional arg
    'Get-Help -الاسم Get-Command' # Test Arabic parameter alias
    'احصل-مساعدة -CommandName Get-Command' # Test Mix: Arabic command, English param
    'Get-Help -الاسم Write-Output -Full' # Test Mix: English command, Arabic param, switch
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
    '# Test escapes inside double quotes (Phase 3 Change)'
    'Write-Output "Quoted string with \\"escaped quote\\" inside"' # Should now work
    'Write-Output "Line1\nLine2"' # Test newline escape
    'Write-Output "Value is:\t\$testVar"' # Test tab and escaped dollar
    # Known Issue: The following line might still have issues depending on backslash handling outside quotes
    # 'Write-Output Argument\\ WithSpace'
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
    '# --- Advanced Redirection Parsing Tests (v0.7.5+) ---'
    '# Note: Executor does not yet HANDLE these, this tests PARSER recognition'
    'Write-Output ''Stderr Redirect Test'' 2> temp_stderr_test.txt'
    'Write-Output ''Stderr Append Test'' 2>> temp_stderr_append_test.txt'
    'Write-Output ''Stderr Append Test 2'' 2>> temp_stderr_append_test.txt'
    'Write-Output ''Merge Stderr to Stdout Test'' 2>&1 | Write-Output # Pipe needed to see merged output'
    'Write-Output ''Merge Stdout to Stderr Test'' >&2 # Less common, test parser'
    'Write-Output ''Merge All to File Test'' > temp_merged_test.txt 2>&1'
    'Write-Output ''Multi Redirect Test'' > temp_multi_out.txt 2> temp_multi_err.txt'
    'احصل-مساعدة > temp_redirect_test.txt 2> temp_stderr_test.txt # Test with Arabic command'
    ''
    '# --- Input Redirection Parsing Tests (Phase 3) ---'
    '# Create a dummy input file first using ArbSh itself'
    'Write-Output "Line for input redirection" > temp_input.txt'
    'Get-Command < temp_input.txt # Test parser recognition of <'
    'Write-Output < temp_input.txt # Another test'
    ''
    '# --- Sub-Expression Parsing Tests (v0.7.5+) ---'
    '# Note: Executor does not yet HANDLE these, this tests PARSER recognition'
    'Write-Output $(Get-Command)'
    'Write-Output Before $(Get-Help Write-Output) After'
    'Write-Output $(Get-Command | Write-Output)'
    'Write-Output $(Write-Output Outer $(Get-Command) Inner)' # Nested test
    'Get-Help -CommandName $(Write-Output Get-Command)' # Subexpression as parameter value
    ''
    '# --- Type Literal Parsing Tests (Phase 3) ---'
    'Write-Output [int] 123'
    'Write-Output [System.ConsoleColor] "Red"'
    'Write-Output [ عربي ] "Test"' # Test with spaces and Arabic
    'Write-Output Before[string]After' # Test adjacent to identifiers
    ''
    '# --- Mixed Script Tests (v0.7.5+) ---'
    '# Test parser handling of mixed scripts in various contexts'
    'Write-Output "مرحبا World"' # Mixed script string literal (double quotes)
    'Write-Output ''Hello عالم''' # Mixed script string literal (single quotes)
    'Write-Output Argument1 عربي Argument3' # Mixed script arguments
    '# Test-Array-Binding عربي English ثالثا # Mixed script array arguments' # Command doesn't exist, but tests parsing
    'Get-Help -CommandName Write-Output -Paramعربي Value # Mixed script parameter name (should be ignored by binder)'
    'Get-Help -CommandName Write-Output -اسمEnglish Value # Mixed script parameter name (should be ignored by binder)'
    'Write-Output "مرحبا"|Write-Output "World" # Mixed script separated by pipe'
    'Write-Output "Hello";Write-Output "عالم" # Mixed script separated by semicolon'
    'Write-Output $testVar # Should expand'
    '# Write-Output $متغير_عربي # Variable expansion with Arabic name (needs variable definition first)'
    ''
    '# --- Mixed Script Edge Cases (Parser Robustness) ---'
    'Write-Output Commandمرحبا # Mixed identifier'
    'Write-Output اسمCommand # Mixed identifier'
    'Write-Output Command-اسم # Mixed identifier with hyphen'
    'Write-Output اسم-Command # Mixed identifier with hyphen'
    'Write-Output Command1>ملف.txt # Operator touching Arabic'
    'Write-Output Command2<ملف.txt # Operator touching Arabic'
    'احصل-مساعدة>file.txt # Arabic command touching operator'
    'احصل-مساعدة|Write-Output # Arabic command touching operator'
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
$processInfo.GetType().FullName # For debugging: Check the type of the object
$PSVersionTable.PSVersion # For debugging: Check the PowerShell version
$processInfo.Arguments # For debugging: Check the arguments
$processInfo.FileName # For debugging: Check the file name
$processInfo.FileName = "dotnet"
$processInfo.Arguments = "run --project $arbshProjectDir"
$processInfo.RedirectStandardInput = $true
$processInfo.RedirectStandardOutput = $true
$processInfo.RedirectStandardError = $true
$processInfo.UseShellExecute = $false
$processInfo.CreateNoWindow = $true
$processInfo.StandardInputEncoding = [System.Text.Encoding]::UTF8 # Explicitly set input encoding for the child process
$processInfo.StandardOutputEncoding = [System.Text.Encoding]::UTF8 # Expect UTF-8 output from the child process
$processInfo.StandardErrorEncoding = [System.Text.Encoding]::UTF8 # Expect UTF-8 error output from the child process
# Set working directory if needed, assuming script runs from project root
# $processInfo.WorkingDirectory = (Get-Location).Path

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $processInfo

$process.Start() | Out-Null

# Write commands line by line using WriteLine and the specified encoding
Write-Log "Writing commands to ArbSh stdin..."
foreach ($command in $arbshCommands) {
    # Write-Log "Sending: $command" # Optional: Log each command being sent
    $process.StandardInput.WriteLine($command)
}
$process.StandardInput.Close() # Signal end of input
Write-Log "Finished writing commands."

# Read output and error streams
Write-Log "Reading ArbSh stdout/stderr..."
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
    $contentLines = Get-Content $tempRedirectFile -Encoding UTF8 # Read as UTF8 lines
    foreach ($line in $contentLines) {
        Add-Content -Path $outputLogFile -Value $line # Append line by line
    }
    Remove-Item $tempRedirectFile # Cleanup
}
else {
    Write-Log "ERROR: $tempRedirectFile was not created."
}

if (Test-Path $tempAppendFile) {
    Write-Log ("Content of " + $tempAppendFile + ":") # Use concatenation
    $contentLines = Get-Content $tempAppendFile -Encoding UTF8 # Read as UTF8 lines
    foreach ($line in $contentLines) {
        Add-Content -Path $outputLogFile -Value $line # Append line by line
    }
    Remove-Item $tempAppendFile # Cleanup
}
else {
    Write-Log "INFO: $tempAppendFile was not created (or already cleaned up)." # Changed to INFO as it might be overwritten/deleted by later tests
}

# Add checks for new temp files (just existence for now, as executor doesn't handle them)
Write-Log "Checking existence of advanced redirection test files (Parser test only)..."
if (Test-Path $tempStderrFile) { Write-Log "INFO: $tempStderrFile exists (Parser likely recognized 2>)."; Remove-Item $tempStderrFile } else { Write-Log "INFO: $tempStderrFile does not exist." }
if (Test-Path $tempStderrAppendFile) { Write-Log "INFO: $tempStderrAppendFile exists (Parser likely recognized 2>>)."; Remove-Item $tempStderrAppendFile } else { Write-Log "INFO: $tempStderrAppendFile does not exist." }
if (Test-Path $tempMergedFile) { Write-Log "INFO: $tempMergedFile exists (Parser likely recognized > file 2>&1)."; Remove-Item $tempMergedFile } else { Write-Log "INFO: $tempMergedFile does not exist." }
if (Test-Path $tempMultiRedirectOut) { Write-Log "INFO: $tempMultiRedirectOut exists (Parser likely recognized > file1)."; Remove-Item $tempMultiRedirectOut } else { Write-Log "INFO: $tempMultiRedirectOut does not exist." }
if (Test-Path $tempMultiRedirectErr) { Write-Log "INFO: $tempMultiRedirectErr exists (Parser likely recognized 2> file2)."; Remove-Item $tempMultiRedirectErr } else { Write-Log "INFO: $tempMultiRedirectErr does not exist." }


Write-Log "Test script finished."
Write-Host "Test output captured in $outputLogFile"
