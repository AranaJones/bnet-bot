@echo off
setlocal
cd /d "%~dp0"
REM BNetDiscordBridge - Automated Installer Builder
REM This script builds the NSIS installer

echo.
echo ======================================
echo BNetDiscordBridge Installer Builder
echo ======================================
echo.

set PUBLISH_DIR=bin\Release\publish
set INSTALLER_NAME=install.exe

REM Check if NSIS is installed
set NSIS_PATH=
if exist "C:\Program Files (x86)\NSIS\makensis.exe" (
    set NSIS_PATH=C:\Program Files (x86)\NSIS\makensis.exe
) else if exist "C:\Program Files\NSIS\makensis.exe" (
    set NSIS_PATH=C:\Program Files\NSIS\makensis.exe
) else (
    where makensis >nul 2>&1
    if errorlevel 1 (
        echo ERROR: NSIS is not installed!
        echo Download from: https://nsis.sourceforge.io/download
        echo.
        pause
        exit /b 1
    )
    set NSIS_PATH=makensis
)

echo [1/4] NSIS found: %NSIS_PATH%
echo.

REM Check if .NET is installed
echo [2/4] Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Download from: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)
dotnet --version
echo.

REM Build the release executable
echo [3/4] Building release executable...
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
call dotnet publish -c Release -o "%PUBLISH_DIR%" --self-contained -r win-x64
if errorlevel 1 (
    echo ERROR: Build failed!
    echo.
    pause
    exit /b 1
)
echo Build completed successfully!
echo.

REM Build the installer
echo [4/4] Building installer...
"%NSIS_PATH%" /V2 /DOUTPUT_EXE=%INSTALLER_NAME% installer.nsi
if errorlevel 1 (
    echo ERROR: Installer build failed!
    echo.
    pause
    exit /b 1
)

echo.
echo ======================================
echo Installer Created Successfully!
echo ======================================
echo.
echo Installer location: %INSTALLER_NAME%
echo.
echo You can now:
echo 1. Share %INSTALLER_NAME% with others
echo 2. Double-click to install
echo.
pause
