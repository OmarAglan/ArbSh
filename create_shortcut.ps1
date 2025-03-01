# PowerShell script to create a shortcut for the Simple Shell

# Get the current directory
$currentDir = (Get-Location).Path
$shellPath = Join-Path -Path $currentDir -ChildPath "build_standalone\hsh.exe"

# Check if the shell executable exists
if (-not (Test-Path $shellPath)) {
    $shellPath = Join-Path -Path $currentDir -ChildPath "build_standalone\bin\hsh.exe"
    if (-not (Test-Path $shellPath)) {
        Write-Host "Shell executable not found. Please build the shell first using build_standalone.bat"
        exit 1
    }
}

# Create a shortcut on the desktop
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\Simple Shell.lnk")
$Shortcut.TargetPath = $shellPath
$Shortcut.Description = "Simple Shell - A cross-platform shell with UTF-8 support"
$Shortcut.WorkingDirectory = $currentDir
$Shortcut.Save()

Write-Host "Shortcut created on the desktop."

# Create a shortcut in the Start Menu
$startMenuPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Simple Shell.lnk"
$Shortcut = $WshShell.CreateShortcut($startMenuPath)
$Shortcut.TargetPath = $shellPath
$Shortcut.Description = "Simple Shell - A cross-platform shell with UTF-8 support"
$Shortcut.WorkingDirectory = $currentDir
$Shortcut.Save()

Write-Host "Shortcut created in the Start Menu."
Write-Host "You can now launch the Simple Shell from the desktop or Start Menu." 