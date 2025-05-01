#Requires -Version 5.1
<#
.SYNOPSIS
  Creates a release build and archive for the ArbSh C# console application.

.DESCRIPTION
  This script automates the process of:
  1. Updating the CHANGELOG.md with the new version number and date.
  2. Publishing a self-contained release build for win-x64.
  3. Creating a zip archive of the published files.

.PARAMETER Version
  The semantic version number for the new release (e.g., "0.6.0"). This is mandatory.

.EXAMPLE
  .\create-release.ps1 -Version "0.6.0"

.NOTES
  - Assumes the script is run from the repository root.
  - Assumes the target framework is 'net9.0'. Modify if needed.
  - Requires PowerShell 5.1 or later.
  - Does not automatically update ROADMAP.md.
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [string]$RuntimeID = "win-x64", # Default target runtime
    [string]$TargetFramework = "net9.0" # Assumed target framework
)

# --- Configuration ---
$ProjectRoot = $PSScriptRoot # Assumes script is in the root
$ChangelogPath = Join-Path -Path $ProjectRoot -ChildPath "docs\CHANGELOG.md"
$ProjectFilePath = Join-Path -Path $ProjectRoot -ChildPath "src_csharp\ArbSh.Console\ArbSh.Console.csproj"
$PublishDirRelative = Join-Path -Path "bin\Release\$TargetFramework\$RuntimeID" -ChildPath "publish"
$PublishDirFullPath = Join-Path -Path $ProjectRoot -ChildPath "src_csharp\ArbSh.Console" | Join-Path -ChildPath $PublishDirRelative
$ReleasesDir = Join-Path -Path $ProjectRoot -ChildPath "releases"
$ZipFileName = "ArbSh-v$($Version)-$($RuntimeID).zip"
$ZipFilePath = Join-Path -Path $ReleasesDir -ChildPath $ZipFileName

Write-Host "Starting release process for version $Version..."

# --- 1. Update Changelog ---
Write-Host "Updating $ChangelogPath..."
try {
    $ChangelogContent = Get-Content -Path $ChangelogPath -Raw
    $CurrentDate = Get-Date -Format "yyyy-MM-dd"

    # Check if [Unreleased] section exists
    if ($ChangelogContent -match '(?ms)^## \[Unreleased\] - YYYY-MM-DD') {
        # Prepare the new Unreleased section header
        $NewUnreleasedHeader = "## [Unreleased] - YYYY-MM-DD`n`n### Added`n- (Future changes go here)`n"
        # Replace the existing Unreleased header with the new version header and insert the new Unreleased section above it
        $UpdatedContent = $ChangelogContent -replace '(?ms)^## \[Unreleased\] - YYYY-MM-DD', "$($NewUnreleasedHeader)`n## [$Version] - $CurrentDate"

        # Write back to the file
        Set-Content -Path $ChangelogPath -Value $UpdatedContent -Encoding UTF8 -NoNewline
        Write-Host "Changelog updated successfully."
    } else {
        Write-Warning "Could not find '## [Unreleased] - YYYY-MM-DD' section in $ChangelogPath. Skipping update."
    }
} catch {
    Write-Error "Failed to update changelog: $($_.Exception.Message)"
    # Decide if script should exit or continue
    # exit 1
}

# --- 2. Create Releases Directory ---
if (-not (Test-Path -Path $ReleasesDir -PathType Container)) {
    Write-Host "Creating releases directory: $ReleasesDir"
    try {
        New-Item -Path $ReleasesDir -ItemType Directory -Force | Out-Null
    } catch {
        Write-Error "Failed to create releases directory: $($_.Exception.Message)"
        exit 1
    }
}

# --- 3. Publish Application ---
Write-Host "Publishing application for $RuntimeID..."
$PublishArgs = @(
    "publish",
    "`"$ProjectFilePath`"",
    "-c", "Release",
    "-r", $RuntimeID,
    "--self-contained", "true" # Removed trailing comma
    # Output path is derived automatically based on TFM/RID
    # "-o", "`"$PublishDirFullPath`"" # Optional: Specify if needed, but default is usually correct
)
try {
    # Start the process and wait for it to complete
    $process = Start-Process dotnet -ArgumentList $PublishArgs -Wait -NoNewWindow -PassThru
    if ($process.ExitCode -ne 0) {
        throw "dotnet publish failed with exit code $($process.ExitCode)"
    }
    Write-Host "Application published successfully to default location (likely under $PublishDirFullPath)."
} catch {
    Write-Error "Failed to publish application: $($_.Exception.Message)"
    exit 1
}

# --- 4. Archive Published Files ---
Write-Host "Archiving published files to $ZipFilePath..."
# Verify the publish directory actually exists before archiving
if (-not (Test-Path -Path $PublishDirFullPath -PathType Container)) {
     Write-Error "Publish directory not found at expected location: $PublishDirFullPath. Cannot create archive."
     exit 1
}

try {
    Compress-Archive -Path "$PublishDirFullPath\*" -DestinationPath $ZipFilePath -Force
    Write-Host "Archive created successfully: $ZipFilePath"
} catch {
    Write-Error "Failed to create archive: $($_.Exception.Message)"
    # Attempting archive without wildcard if the first failed (less common issue)
    try {
         Write-Warning "Retrying archive creation without wildcard..."
         Compress-Archive -Path $PublishDirFullPath -DestinationPath $ZipFilePath -Force
         Write-Host "Archive created successfully on retry: $ZipFilePath"
    } catch {
         Write-Error "Failed to create archive on retry: $($_.Exception.Message)"
         exit 1
    }
}

Write-Host "Release process for version $Version completed."
