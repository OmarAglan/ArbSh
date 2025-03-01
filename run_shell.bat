@echo off
echo Launching Simple Shell...

REM Check if the executable exists in the root of build_standalone
if exist build_standalone\hsh.exe (
    REM Launch the shell
    start "" "build_standalone\hsh.exe"
) else (
    REM Check if the executable exists in the bin subdirectory
    if exist build_standalone\bin\hsh.exe (
        REM Launch the shell
        start "" "build_standalone\bin\hsh.exe"
    ) else (
        echo Shell executable not found.
        echo Please build the shell first using build_standalone.bat
        pause
    )
) 