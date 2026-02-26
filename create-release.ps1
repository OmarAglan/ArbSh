#Requires -Version 5.1
<#
.SYNOPSIS
  Creates release artifacts for ArbSh.

.DESCRIPTION
  This script can:
  1) Update docs/CHANGELOG.md (optional)
  2) Publish ArbSh.Console and create a release zip
  3) Optionally publish ArbSh.Terminal and create an installer package zip
     containing:
       - App/ (published terminal files)
       - Install-ArbSh.ps1
       - Uninstall-ArbSh.ps1

  The installer scripts register Windows Explorer context menu entries:
    "Open in ArbSh" for folder/background/drive, passing --working-dir.

.PARAMETER Version
  Release version string (e.g. 0.8.1-alpha).

.PARAMETER RuntimeID
  Target runtime identifier (default: win-x64).

.PARAMETER TargetFramework
  Target framework (default: net9.0).

.PARAMETER CreateInstaller
  When set, also builds the terminal installer package zip.

.PARAMETER SkipChangelogUpdate
  When set, skips changelog modification.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$RuntimeID = "win-x64",
    [string]$TargetFramework = "net9.0",

    [switch]$CreateInstaller,
    [switch]$SkipChangelogUpdate
)

$ErrorActionPreference = "Stop"

# --- Paths ---
$ProjectRoot = $PSScriptRoot
$ReleasesDir = Join-Path $ProjectRoot "releases"
$ChangelogPath = Join-Path $ProjectRoot "docs\CHANGELOG.md"

$ConsoleProject = Join-Path $ProjectRoot "src_csharp\ArbSh.Console\ArbSh.Console.csproj"
$TerminalProject = Join-Path $ProjectRoot "src_csharp\ArbSh.Terminal\ArbSh.Terminal.csproj"

$ConsolePublishDir = Join-Path $ProjectRoot "src_csharp\ArbSh.Console\bin\Release\$TargetFramework\$RuntimeID\publish"
$TerminalPublishDir = Join-Path $ProjectRoot "src_csharp\ArbSh.Terminal\bin\Release\$TargetFramework\$RuntimeID\publish"

$ConsoleZipName = "ArbSh-v$Version-$RuntimeID.zip"
$ConsoleZipPath = Join-Path $ReleasesDir $ConsoleZipName

$InstallerPackageName = "ArbSh-v$Version-$RuntimeID-installer.zip"
$InstallerPackagePath = Join-Path $ReleasesDir $InstallerPackageName
$InstallerStageDir = Join-Path $ReleasesDir "ArbSh-v$Version-$RuntimeID-installer"
$InstallerTemplateDir = Join-Path $ProjectRoot "installer"

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message"
}

function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Update-Changelog {
    if ($SkipChangelogUpdate) {
        Write-Step "Skipping CHANGELOG update."
        return
    }

    if (-not (Test-Path -Path $ChangelogPath -PathType Leaf)) {
        Write-Warning "Changelog not found at: $ChangelogPath"
        return
    }

    Write-Step "Updating changelog header for version $Version"
    $content = Get-Content -Path $ChangelogPath -Raw
    $currentDate = Get-Date -Format "yyyy-MM-dd"
    $pattern = '(?m)^## \[Unreleased\](?:\s*-\s*YYYY-MM-DD)?\s*$'

    if ($content -notmatch $pattern) {
        Write-Warning "Could not find '## [Unreleased]' section. Skipping changelog update."
        return
    }

    if ($content -match ("(?m)^## \[" + [regex]::Escape($Version) + "\]\s*-\s*\d{4}-\d{2}-\d{2}\s*$")) {
        Write-Warning "Version $Version already exists in changelog. Skipping update."
        return
    }

    $replacement = @"
## [Unreleased]

### Added
- (Future changes go here)

## [$Version] - $currentDate
"@

    $updated = [regex]::Replace($content, $pattern, $replacement, 1)
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($ChangelogPath, $updated, $utf8NoBom)
}

function Invoke-DotNetPublish {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    if (-not (Test-Path -Path $ProjectPath -PathType Leaf)) {
        throw "Project file not found: $ProjectPath"
    }

    Write-Step "Publishing: $ProjectPath"
    & dotnet publish $ProjectPath -c Release -r $RuntimeID --self-contained true
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $ProjectPath (exit code $LASTEXITCODE)."
    }
}

function Build-ConsoleReleaseZip {
    Write-Step "Publishing ArbSh.Console"
    Invoke-DotNetPublish -ProjectPath $ConsoleProject

    if (-not (Test-Path -Path $ConsolePublishDir -PathType Container)) {
        throw "Console publish directory not found: $ConsolePublishDir"
    }

    Write-Step "Creating console zip: $ConsoleZipPath"
    if (Test-Path -Path $ConsoleZipPath) {
        Remove-Item -Path $ConsoleZipPath -Force
    }

    Compress-Archive -Path (Join-Path $ConsolePublishDir '*') -DestinationPath $ConsoleZipPath -Force
}

function Build-InstallerPackage {
    Write-Step "Publishing ArbSh.Terminal"
    Invoke-DotNetPublish -ProjectPath $TerminalProject

    if (-not (Test-Path -Path $TerminalPublishDir -PathType Container)) {
        throw "Terminal publish directory not found: $TerminalPublishDir"
    }

    if (-not (Test-Path -Path $InstallerTemplateDir -PathType Container)) {
        throw "Installer template directory not found: $InstallerTemplateDir"
    }

    $installScript = Join-Path $InstallerTemplateDir "Install-ArbSh.ps1"
    $uninstallScript = Join-Path $InstallerTemplateDir "Uninstall-ArbSh.ps1"
    if (-not (Test-Path -Path $installScript -PathType Leaf)) {
        throw "Install script missing: $installScript"
    }
    if (-not (Test-Path -Path $uninstallScript -PathType Leaf)) {
        throw "Uninstall script missing: $uninstallScript"
    }

    if (Test-Path -Path $InstallerStageDir -PathType Container) {
        Remove-Item -Path $InstallerStageDir -Recurse -Force
    }

    $stageAppDir = Join-Path $InstallerStageDir "App"
    Ensure-Directory -Path $stageAppDir

    Write-Step "Staging installer package files"
    Copy-Item -Path (Join-Path $TerminalPublishDir '*') -Destination $stageAppDir -Recurse -Force
    Copy-Item -Path $installScript -Destination (Join-Path $InstallerStageDir "Install-ArbSh.ps1") -Force
    Copy-Item -Path $uninstallScript -Destination (Join-Path $InstallerStageDir "Uninstall-ArbSh.ps1") -Force

    $readmePath = Join-Path $InstallerStageDir "README.txt"
    $readme = @"
ArbSh Installer Package
Version: $Version
Runtime: $RuntimeID

How to install:
1) Open PowerShell
2) Run: .\Install-ArbSh.ps1

This installs ArbSh.Terminal for the current user and adds:
 - Open in ArbSh (folder background)
 - Open in ArbSh (folder)
 - Open in ArbSh (drive)

It passes the selected location through:
  --working-dir
"@
    Set-Content -Path $readmePath -Value $readme -Encoding UTF8

    if (Test-Path -Path $InstallerPackagePath -PathType Leaf) {
        Remove-Item -Path $InstallerPackagePath -Force
    }

    Write-Step "Creating installer package zip: $InstallerPackagePath"
    Compress-Archive -Path (Join-Path $InstallerStageDir '*') -DestinationPath $InstallerPackagePath -Force
}

Write-Step "Starting release process for version $Version"
Ensure-Directory -Path $ReleasesDir

Update-Changelog
Build-ConsoleReleaseZip

if ($CreateInstaller) {
    Build-InstallerPackage
} else {
    Write-Step "Installer packaging skipped. Use -CreateInstaller to enable."
}

Write-Step "Release process completed."
Write-Host "Console artifact: $ConsoleZipPath"
if ($CreateInstaller) {
    Write-Host "Installer package: $InstallerPackagePath"
}
