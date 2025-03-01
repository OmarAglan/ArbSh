@echo off
echo Building standalone Simple Shell with Modern UI...

REM Create build directory if it doesn't exist
if not exist build_standalone mkdir build_standalone

REM Change to build directory
cd build_standalone

REM Configure with CMake
echo Configuring with CMake...
cmake .. -G "MinGW Makefiles" -DBUILD_STATIC=ON -DWIN_GUI=ON
if %ERRORLEVEL% NEQ 0 (
    echo CMake configuration failed with error code %ERRORLEVEL%
    cd ..
    pause
    exit /b %ERRORLEVEL%
)

REM Build the project
echo Building project...
mingw32-make
if %ERRORLEVEL% NEQ 0 (
    echo Build failed with error code %ERRORLEVEL%
    cd ..
    pause
    exit /b %ERRORLEVEL%
)

REM Check if executable was created in the root directory
if exist hsh.exe (
    echo.
    echo Build completed successfully!
    echo The executable is located at:
    echo %CD%\hsh.exe
    echo.
    echo You can now run the shell with its modern UI interface.
) else (
    REM Check if executable was created in the bin directory
    if exist bin\hsh.exe (
        echo.
        echo Build completed successfully!
        echo The executable is located at:
        echo %CD%\bin\hsh.exe
        echo.
        echo You can now run the shell with its modern UI interface.
    ) else (
        echo.
        echo Build completed, but executable was not found.
        echo Please check the build logs for errors.
    )
)

REM Return to original directory
cd ..

pause 