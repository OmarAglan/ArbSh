#Requires -Version 5.1
<#
.SYNOPSIS
  Uninstalls ArbSh Terminal from the current user profile.
#>
param(
    [string]$InstallDir = (Join-Path $env:LocalAppData "Programs\ArbSh")
)

$ErrorActionPreference = 'Stop'

$contextKeys = @(
    "HKCU:\Software\Classes\Directory\Background\shell\ArbSh",
    "HKCU:\Software\Classes\Directory\shell\ArbSh",
    "HKCU:\Software\Classes\Drive\shell\ArbSh"
)

foreach ($key in $contextKeys) {
    if (Test-Path -Path $key) {
        Remove-Item -Path $key -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ArbSh"
if (Test-Path -Path $uninstallKey) {
    Remove-Item -Path $uninstallKey -Recurse -Force -ErrorAction SilentlyContinue
}

if (Test-Path -Path $InstallDir -PathType Container) {
    # Delete after this script exits so self-delete does not fail while in use.
    $cleanupCommand = "timeout /t 1 /nobreak > nul & rmdir /s /q `"$InstallDir`""
    Start-Process -FilePath "cmd.exe" -ArgumentList "/c $cleanupCommand" -WindowStyle Hidden
}

Write-Host "ArbSh uninstall completed. If files are locked, close ArbSh and run uninstall again."
