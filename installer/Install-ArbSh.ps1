#Requires -Version 5.1
<#
.SYNOPSIS
  Installs ArbSh Terminal for the current user and adds Explorer context menu entries.
#>
param(
    [string]$InstallDir = (Join-Path $env:LocalAppData "Programs\ArbSh"),
    [switch]$SkipContextMenu
)

$ErrorActionPreference = 'Stop'

function Set-RegistryDefaultValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    if (-not (Test-Path -Path $Path)) {
        New-Item -Path $Path -Force | Out-Null
    }

    New-ItemProperty -Path $Path -Name '(default)' -Value $Value -Force | Out-Null
}

function Register-ContextMenu {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExecutablePath
    )

    $menuLabel = "Open in ArbSh"

    $entries = @(
        @{ Key = "HKCU:\Software\Classes\Directory\Background\shell\ArbSh"; Token = "%V" },
        @{ Key = "HKCU:\Software\Classes\Directory\shell\ArbSh"; Token = "%1" },
        @{ Key = "HKCU:\Software\Classes\Drive\shell\ArbSh"; Token = "%1" }
    )

    foreach ($entry in $entries) {
        $shellKey = $entry.Key
        $commandKey = Join-Path $shellKey "command"

        Set-RegistryDefaultValue -Path $shellKey -Value $menuLabel
        New-ItemProperty -Path $shellKey -Name "Icon" -Value $ExecutablePath -Force | Out-Null

        $commandValue = "`"$ExecutablePath`" --working-dir `"$($entry.Token)`""
        Set-RegistryDefaultValue -Path $commandKey -Value $commandValue
    }
}

$packageAppDir = Join-Path $PSScriptRoot "App"
if (-not (Test-Path -Path $packageAppDir -PathType Container)) {
    throw "App folder not found next to installer script: $packageAppDir"
}

if (-not (Test-Path -Path $InstallDir -PathType Container)) {
    New-Item -Path $InstallDir -ItemType Directory -Force | Out-Null
}

Copy-Item -Path (Join-Path $packageAppDir '*') -Destination $InstallDir -Recurse -Force

$exePath = Join-Path $InstallDir "ArbSh.Terminal.exe"
if (-not (Test-Path -Path $exePath -PathType Leaf)) {
    throw "ArbSh.Terminal.exe was not found in installation directory: $InstallDir"
}

$uninstallSource = Join-Path $PSScriptRoot "Uninstall-ArbSh.ps1"
$uninstallTarget = Join-Path $InstallDir "Uninstall-ArbSh.ps1"
if (-not (Test-Path -Path $uninstallSource -PathType Leaf)) {
    throw "Uninstall-ArbSh.ps1 is missing beside installer script."
}

Copy-Item -Path $uninstallSource -Destination $uninstallTarget -Force

if (-not $SkipContextMenu) {
    Register-ContextMenu -ExecutablePath $exePath
}

$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath).ProductVersion
if ([string]::IsNullOrWhiteSpace($version)) {
    $version = "0.0.0"
}

$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ArbSh"
if (-not (Test-Path -Path $uninstallKey)) {
    New-Item -Path $uninstallKey -Force | Out-Null
}

$uninstallCommand = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$uninstallTarget`" -InstallDir `"$InstallDir`""

New-ItemProperty -Path $uninstallKey -Name "DisplayName" -Value "ArbSh Terminal" -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name "DisplayVersion" -Value $version -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name "Publisher" -Value "ArbSh" -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name "InstallLocation" -Value $InstallDir -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name "UninstallString" -Value $uninstallCommand -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name "QuietUninstallString" -Value $uninstallCommand -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name "NoModify" -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name "NoRepair" -Value 1 -PropertyType DWord -Force | Out-Null

Write-Host "ArbSh installed to: $InstallDir"
if ($SkipContextMenu) {
    Write-Host "Context menu registration skipped."
} else {
    Write-Host "Explorer context menu entries registered."
}
Write-Host "You can uninstall from Windows settings or by running: $uninstallTarget"
