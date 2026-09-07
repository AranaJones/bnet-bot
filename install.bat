@echo off
REM BNetDiscordBridge Installation Script
REM This script sets up the bot for first-time use

echo.
echo ======================================
echo BNetDiscordBridge - Installation
echo ======================================
echo.

REM Check if .NET is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [1/5] .NET SDK found: 
dotnet --version
echo.

REM Create appsettings.json if it doesn't exist
echo [2/5] Checking configuration...
if exist "appsettings.json" (
    echo appsettings.json already exists
) else (
    echo Creating appsettings.json from template...
    if exist "appsettings.example.json" (
        copy appsettings.example.json appsettings.json
        echo Configuration file created. Please edit appsettings.json with your settings.
    ) else (
        echo ERROR: appsettings.example.json not found!
        pause
        exit /b 1
    )
)
echo.

REM Restore dependencies
echo [3/5] Restoring NuGet packages...
dotnet restore
if errorlevel 1 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)
echo.

REM Build the project
echo [4/5] Building project...
dotnet build -c Release
if errorlevel 1 (
    echo ERROR: Build failed
    pause
    exit /b 1
)
echo.

REM Create run script
echo [5/5] Creating launcher scripts...

REM Create run.bat
echo Creating run.bat...
(
    echo @echo off
    echo REM Run BNetDiscordBridge Bot
    echo dotnet run --no-build -c Release
    echo pause
) > run.bat

REM Create run-no-pause.bat for background running
echo Creating run-no-pause.bat...
(
    echo @echo off
    echo REM Run BNetDiscordBridge Bot (no pause on exit^)
    echo dotnet run --no-build -c Release
) > run-no-pause.bat

echo.
echo ======================================
echo Installation Complete!
echo ======================================
echo.
echo Next steps:
echo 1. Edit appsettings.json with your settings:
echo    - Discord token and channel ID
echo    - Battle.net username and password
echo    - Your Discord user ID (for owner notifications^)
echo    - BNLS server address (default: localhost:9367^)
echo.
echo 2. Make sure BNLS is running
echo    Download: https://github.com/HarpyWar/bncsutil
echo.
echo 3. Run the bot:
echo    - Double-click: run.bat
echo    - Or run from command line: dotnet run
echo.
echo 4. Configure Discord:
echo    - Go to https://discord.com/developers
echo    - Create bot and enable Message Content intent
echo    - Copy token to appsettings.json
echo.
echo Documentation: See README.md
echo.
pause
