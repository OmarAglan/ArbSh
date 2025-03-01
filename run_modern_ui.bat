@echo off
echo Starting Simple Shell with Modern UI...

REM Check if executable exists in the build_standalone directory
if exist build_standalone\hsh.exe (
    start "" "build_standalone\hsh.exe"
    echo Shell started successfully!
) else if exist build_standalone\bin\hsh.exe (
    start "" "build_standalone\bin\hsh.exe"
    echo Shell started successfully!
) else (
    echo Shell executable not found.
    echo Please build the shell first using build_standalone.bat
    pause
    exit /b 1
)

echo You can close this window now. 