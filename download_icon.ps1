# PowerShell script to download a terminal icon for our application

# URL of a terminal icon from a trusted source
$iconUrl = "https://raw.githubusercontent.com/microsoft/terminal/master/res/terminal.ico"

# Output path
$outputPath = "src/shell.ico"

# Create the output directory if it doesn't exist
$outputDir = Split-Path -Path $outputPath -Parent
if (-not (Test-Path -Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force
}

# Download the icon
Write-Host "Downloading terminal icon from $iconUrl..."
try {
    Invoke-WebRequest -Uri $iconUrl -OutFile $outputPath
    if (Test-Path -Path $outputPath) {
        Write-Host "Icon downloaded successfully to $outputPath"
    } else {
        Write-Host "Failed to download icon"
    }
} catch {
    Write-Host "Error downloading icon: $_"
    
    # Fallback: Create a simple icon using .NET
    Write-Host "Creating a fallback icon..."
    
    Add-Type -AssemblyName System.Drawing
    
    # Create a bitmap
    $bitmap = New-Object System.Drawing.Bitmap 32, 32
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    
    # Fill background
    $graphics.FillRectangle([System.Drawing.Brushes]::Black, 0, 0, 32, 32)
    
    # Draw a simple terminal-like shape
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::LightGreen, 2)
    $graphics.DrawRectangle($pen, 4, 4, 24, 24)
    $graphics.DrawLine($pen, 8, 12, 24, 12)
    $graphics.DrawLine($pen, 8, 16, 20, 16)
    $graphics.DrawLine($pen, 8, 20, 16, 20)
    
    # Save as icon
    $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Icon)
    
    $graphics.Dispose()
    $bitmap.Dispose()
    
    Write-Host "Fallback icon created at $outputPath"
}

Write-Host "Done!" 