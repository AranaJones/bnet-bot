@echo off
setlocal
cd /d "%~dp0"

echo.
echo ======================================
echo BNetDiscordBridge Installer Builder
echo ======================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: dotnet SDK not found in PATH
    exit /b 1
)

if exist "dist\install" rmdir /s /q "dist\install"

echo [1/2] Publishing standalone installer...
dotnet publish ".\installer\InstallerApp\InstallerApp.csproj" -c Release -r win-x64 -o ".\dist\install" --nologo
if errorlevel 1 (
    echo ERROR: Failed to publish installer
    exit /b 1
)

copy /y ".\dist\install\install.exe" ".\install.exe" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy install.exe to repository root
    exit /b 1
)

echo [2/2] Installer created successfully
echo.
echo Output files:
echo   %cd%\install.exe
echo   %cd%\dist\install\install.exe
