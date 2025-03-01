# PowerShell installation script for the simple shell on Windows systems

# Default installation directory
$InstallDir = "$env:ProgramFiles\SimpleShell"
$ShellName = "hsh.exe"
$ShellPath = Join-Path -Path $InstallDir -ChildPath $ShellName

# Function to show usage
function Show-Usage {
    Write-Host "Usage: .\install_windows.ps1 [OPTIONS]"
    Write-Host "Install the simple shell on your Windows system."
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -InstallDir <path>   Install binaries in specified directory (default: $env:ProgramFiles\SimpleShell)"
    Write-Host "  -CreateShortcut      Create desktop shortcut"
    Write-Host "  -Help                Display this help and exit"
    Write-Host ""
}

# Parse command line arguments
param (
    [string]$InstallDir = "$env:ProgramFiles\SimpleShell",
    [switch]$CreateShortcut = $false,
    [switch]$Help = $false
)

if ($Help) {
    Show-Usage
    exit 0
}

# Update shell path based on installation directory
$ShellPath = Join-Path -Path $InstallDir -ChildPath $ShellName

# Check if running as administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "This script requires administrator privileges. Please run as administrator." -ForegroundColor Red
    exit 1
}

# Check if build directory exists
if (-not (Test-Path -Path "build")) {
    Write-Host "Build directory not found. Creating and building..." -ForegroundColor Yellow
    
    # Create build directory
    New-Item -Path "build" -ItemType Directory -Force | Out-Null
    
    # Navigate to build directory
    Push-Location -Path "build"
    
    # Run CMake and build
    & cmake .. -DBUILD_STATIC=ON
    & cmake --build . --config Release
    
    # Return to original directory
    Pop-Location
}

# Check if the binary exists
$BinaryPath = Join-Path -Path "build\bin\Release" -ChildPath $ShellName
if (-not (Test-Path -Path $BinaryPath)) {
    Write-Host "Binary not found. Please build the project first." -ForegroundColor Red
    Write-Host "Run: mkdir build; cd build; cmake ..; cmake --build . --config Release"
    exit 1
}

# Create installation directory if it doesn't exist
if (-not (Test-Path -Path $InstallDir)) {
    Write-Host "Creating installation directory: $InstallDir" -ForegroundColor Yellow
    New-Item -Path $InstallDir -ItemType Directory -Force | Out-Null
}

# Install the shell
Write-Host "Installing $ShellName to $ShellPath" -ForegroundColor Green
Copy-Item -Path $BinaryPath -Destination $ShellPath -Force

# Add to PATH if not already there
$currentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
if (-not $currentPath.Contains($InstallDir)) {
    Write-Host "Adding $InstallDir to system PATH" -ForegroundColor Yellow
    [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$InstallDir", "Machine")
}

# Create desktop shortcut if requested
if ($CreateShortcut) {
    $DesktopPath = [Environment]::GetFolderPath("Desktop")
    $ShortcutPath = Join-Path -Path $DesktopPath -ChildPath "Simple Shell.lnk"
    
    Write-Host "Creating desktop shortcut" -ForegroundColor Yellow
    
    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = $ShellPath
    $Shortcut.WorkingDirectory = Split-Path -Path $ShellPath -Parent
    $Shortcut.Description = "Simple Shell - Cross-platform command line interpreter"
    $Shortcut.IconLocation = $ShellPath
    $Shortcut.Save()
}

Write-Host "Installation complete!" -ForegroundColor Green
Write-Host "You can run the shell by typing: $ShellName"
Write-Host "Note: You may need to restart your terminal or computer for PATH changes to take effect."

exit 0 