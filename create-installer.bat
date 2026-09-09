@echo off
REM BNetDiscordBridge - Simple PowerShell Installer
REM This creates a standalone installer without requiring NSIS

echo Creating BNetDiscordBridge Installer...
echo.

REM Create temp directory
set TEMP_DIR=%TEMP%\bnet-bot-build
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%"
mkdir "%TEMP_DIR%"

REM Create PowerShell installer script
(
echo ^# BNetDiscordBridge Installer
echo ^$ErrorActionPreference = 'Stop'
echo.
echo write-host "======================================" -ForegroundColor Cyan
echo write-host "BNetDiscordBridge Installer" -ForegroundColor Cyan
echo write-host "======================================" -ForegroundColor Cyan
echo write-host ""
echo.
echo ^# Check admin rights
echo if ^(-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent^(^)^).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator'^)^) {
echo     write-host "ERROR: Please run this installer as Administrator!" -ForegroundColor Red
echo     pause
echo     exit 1
echo }
echo.
echo write-host "[1/5] Checking .NET 8.0..." -ForegroundColor Yellow
echo ^$dotnetPath = where.exe dotnet 2^>$null
echo if ^(-not $dotnetPath^) {
echo     write-host "ERROR: .NET 8.0 SDK not found!" -ForegroundColor Red
echo     write-host "Please download from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
echo     pause
echo     exit 1
echo }
echo write-host "^.NET SDK found: $dotnetPath" -ForegroundColor Green
echo write-host ""
echo.
echo write-host "[2/5] Installing to Program Files..." -ForegroundColor Yellow
echo ^$InstallDir = "$env:ProgramFiles\BNetDiscordBridge"
echo if ^(Test-Path $InstallDir^) {
echo     write-host "Removing previous installation..." -ForegroundColor Cyan
echo     Remove-Item -Path $InstallDir -Recurse -Force -ErrorAction SilentlyContinue
echo }
echo New-Item -ItemType Directory -Path $InstallDir -Force ^| Out-Null
echo write-host "Installation directory: $InstallDir" -ForegroundColor Green
echo write-host ""
echo.
echo write-host "[3/5] Cloning repository..." -ForegroundColor Yellow
echo ^$RepoUrl = "https://github.com/AranaJones/bnet-bot.git"
echo if ^(-not ^(where.exe git 2^>$null^)^) {
echo     write-host "ERROR: Git is not installed!" -ForegroundColor Red
echo     write-host "Please download from: https://git-scm.com/download/win" -ForegroundColor Yellow
echo     pause
echo     exit 1
echo }
echo ^& git clone $RepoUrl $InstallDir 2^>^&1
echo if ^($LASTEXITCODE -ne 0^) {
echo     write-host "ERROR: Failed to clone repository" -ForegroundColor Red
echo     pause
echo     exit 1
echo }
echo write-host "Repository cloned successfully" -ForegroundColor Green
echo write-host ""
echo.
echo write-host "[4/5] Building project..." -ForegroundColor Yellow
echo Push-Location $InstallDir
echo ^& dotnet publish -c Release -o bin\Release\publish --self-contained -r win-x64 2^>^&1
echo if ^($LASTEXITCODE -ne 0^) {
echo     write-host "ERROR: Build failed" -ForegroundColor Red
echo     Pop-Location
echo     pause
echo     exit 1
echo }
echo write-host "Build completed successfully" -ForegroundColor Green
echo write-host ""
echo.
echo write-host "[5/5] Creating shortcuts..." -ForegroundColor Yellow
echo ^$StartMenu = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\BNetDiscordBridge"
echo New-Item -ItemType Directory -Path $StartMenu -Force ^| Out-Null
echo.
echo ^$WshShell = New-Object -ComObject WScript.Shell
echo ^$ExePath = "$InstallDir\bin\Release\publish\bnet-bot.exe"
echo.
echo ^$Shortcut = $WshShell.CreateShortcut^("$StartMenu\BNetDiscordBridge.lnk"^)
echo $Shortcut.TargetPath = $ExePath
echo $Shortcut.WorkingDirectory = $InstallDir
echo $Shortcut.Save^(^)
echo.
echo ^$DesktopPath = [System.Environment]::GetFolderPath^([System.Environment+SpecialFolder]::Desktop^)
echo ^$Shortcut = $WshShell.CreateShortcut^("$DesktopPath\BNetDiscordBridge.lnk"^)
echo $Shortcut.TargetPath = $ExePath
echo $Shortcut.WorkingDirectory = $InstallDir
echo $Shortcut.Save^(^)
echo.
echo write-host "Shortcuts created" -ForegroundColor Green
echo Pop-Location
echo write-host ""
echo.
echo write-host "======================================" -ForegroundColor Green
echo write-host "Installation Complete!" -ForegroundColor Green
echo write-host "======================================" -ForegroundColor Green
echo write-host ""
echo write-host "Next steps:" -ForegroundColor Cyan
echo write-host "1. Edit config file:" -ForegroundColor White
echo write-host "   $InstallDir\appsettings.json" -ForegroundColor Yellow
echo write-host "2. Start BNLS server ^(if using BNLS^)" -ForegroundColor White
echo write-host "3. Run 'BNetDiscordBridge' from Start Menu or Desktop" -ForegroundColor White
echo write-host ""
echo write-host "Documentation: $InstallDir\README.md" -ForegroundColor Cyan
echo write-host ""
echo pause
) > "%TEMP_DIR%\install.ps1"

REM Create the installer executable using PowerShell
powershell -NoProfile -ExecutionPolicy Bypass -Command "^
[Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms') | Out-Null; ^
[Reflection.Assembly]::LoadWithPartialName('System.Drawing') | Out-Null; ^
^
^$form = New-Object System.Windows.Forms.Form; ^
^$form.Text = 'BNetDiscordBridge Installer'; ^
^$form.Width = 600; ^
^$form.Height = 400; ^
^$form.StartPosition = 'CenterScreen'; ^
^$form.Font = New-Object System.Drawing.Font('Segoe UI', 10); ^
^
^$label = New-Object System.Windows.Forms.Label; ^
^$label.Text = 'BNetDiscordBridge Installer'; ^
^$label.Font = New-Object System.Drawing.Font('Segoe UI', 16, [System.Drawing.FontStyle]::Bold); ^
^$label.Location = New-Object System.Drawing.Point(20, 20); ^
^$label.Size = New-Object System.Drawing.Size(560, 40); ^
^$form.Controls.Add(^$label); ^
^
^$info = New-Object System.Windows.Forms.Label; ^
^$info.Text = @' ^
This will install BNetDiscordBridge to: ^
C:\Program Files\BNetDiscordBridge ^
^
Requirements: ^
- .NET 8.0 SDK ^
- Git ^
- Administrator access ^
'@; ^
^$info.Location = New-Object System.Drawing.Point(20, 70); ^
^$info.Size = New-Object System.Drawing.Size(560, 150); ^
^$form.Controls.Add(^$info); ^
^
^$installBtn = New-Object System.Windows.Forms.Button; ^
^$installBtn.Text = 'Install'; ^
^$installBtn.Location = New-Object System.Drawing.Point(200, 240); ^
^$installBtn.Size = New-Object System.Drawing.Size(100, 40); ^
^$installBtn.Add_Click({ ^
    ^$form.Close(); ^
    powershell -NoProfile -ExecutionPolicy Bypass -File '%TEMP_DIR%\install.ps1'; ^
}); ^
^$form.Controls.Add(^$installBtn); ^
^
^$cancelBtn = New-Object System.Windows.Forms.Button; ^
^$cancelBtn.Text = 'Cancel'; ^
^$cancelBtn.Location = New-Object System.Drawing.Point(320, 240); ^
^$cancelBtn.Size = New-Object System.Drawing.Size(100, 40); ^
^$cancelBtn.Add_Click({ ^$form.Close(); }); ^
^$form.Controls.Add(^$cancelBtn); ^
^
^$form.ShowDialog() | Out-Null; ^
"

echo.
echo Installer created successfully!
echo Output: install.exe
pause
