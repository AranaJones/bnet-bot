@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

REM BNetDiscordBridge - Create Standalone Installer EXE
REM This script packages everything needed into a single install.exe

echo.
echo ======================================
echo BNetDiscordBridge Installer Generator
echo ======================================
echo.

REM Check for required tools
echo Checking for required tools...

REM Check for Python (for PyInstaller)
python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python not found in PATH
    echo Please install Python 3.9+ from https://www.python.org/downloads/
    echo Make sure to check "Add Python to PATH" during installation
    echo.
    pause
    exit /b 1
)
echo ✓ Python found
echo.

REM Install PyInstaller if not present
echo Checking for PyInstaller...
python -m pip show pyinstaller >nul 2>&1
if errorlevel 1 (
    echo Installing PyInstaller...
    python -m pip install pyinstaller -q
)
echo ✓ PyInstaller ready
echo.

REM Create the Python installer script
echo Creating installer script...

(
echo import os
echo import sys
echo import subprocess
echo import shutil
echo from pathlib import Path
echo import tkinter as tk
echo from tkinter import messagebox, ttk
echo import threading
echo.
echo class InstallerGUI:
echo     def __init__(self, root^):
echo         self.root = root
echo         self.root.title("BNetDiscordBridge Installer"^)
echo         self.root.geometry("600x500"^)
echo         self.root.resizable(False, False^)
echo.
echo         ^# Title
echo         title_label = tk.Label(self.root, text="BNetDiscordBridge Installer", font=("Arial", 16, "bold"^)^)
echo         title_label.pack(pady=20^)
echo.
echo         ^# Info text
echo         info_text = tk.Label(self.root, text=
echo             "This installer will:\n\n"
echo             "• Install to: C:\\Program Files\\BNetDiscordBridge\n"
echo             "• Clone from GitHub\n"
echo             "• Build the bot automatically\n"
echo             "• Create shortcuts\n\n"
echo             "Requirements:\n"
echo             "• .NET 8.0 SDK\n"
echo             "• Administrator access",
echo             justify=tk.LEFT, font=("Arial", 10^)^)
echo         info_text.pack(pady=10, padx=20^)
echo.
echo         ^# Progress bar
echo         self.progress = ttk.Progressbar(self.root, mode='indeterminate'^)
echo         self.progress.pack(pady=10, padx=20, fill=tk.X^)
echo.
echo         ^# Status text
echo         self.status = tk.Label(self.root, text="Ready to install", font=("Arial", 9^), fg="blue"^)
echo         self.status.pack(pady=5^)
echo.
echo         ^# Buttons
echo         button_frame = tk.Frame(self.root^)
echo         button_frame.pack(pady=20^)
echo.
echo         self.install_btn = tk.Button(button_frame, text="Install", width=15, command=self.install, bg="green", fg="white"^)
echo         self.install_btn.pack(side=tk.LEFT, padx=5^)
echo.
echo         cancel_btn = tk.Button(button_frame, text="Cancel", width=15, command=self.root.quit, bg="red", fg="white"^)
echo         cancel_btn.pack(side=tk.LEFT, padx=5^)
echo.
echo     def update_status(self, message^):
echo         self.status.config(text=message^)
echo         self.root.update()
echo.
echo     def install(self^):
echo         self.install_btn.config(state=tk.DISABLED^)
echo         self.progress.start()
echo         thread = threading.Thread(target=self.run_install^)
echo         thread.start()
echo.
echo     def run_install(self^):
echo         try:
echo             self.update_status("[1/5] Checking .NET 8.0..."^)
echo             result = subprocess.run(["dotnet", "--version"], capture_output=True^)
echo             if result.returncode != 0:
echo                 raise Exception(".NET 8.0 SDK not found"^)
echo             self.update_status("[2/5] Cloning repository..."^)
echo             install_dir = r"C:\Program Files\BNetDiscordBridge"
echo             if os.path.exists(install_dir^):
echo                 shutil.rmtree(install_dir^)
echo             subprocess.run(["git", "clone", "https://github.com/AranaJones/bnet-bot.git", install_dir], check=True^)
echo             self.update_status("[3/5] Installing dependencies..."^)
echo             os.chdir(install_dir^)
echo             subprocess.run(["dotnet", "restore"], check=True^)
echo             self.update_status("[4/5] Building project..."^)
echo             subprocess.run(["dotnet", "publish", "-c", "Release", "-o", "bin/Release/publish", "--self-contained", "-r", "win-x64"], check=True^)
echo             self.update_status("[5/5] Creating shortcuts..."^)
echo             self.create_shortcuts(install_dir^)
echo             self.progress.stop()
echo             messagebox.showinfo("Success", "Installation complete!\n\nNext steps:\n1. Edit appsettings.json\n2. Run BNetDiscordBridge from Start Menu"^)
echo             self.root.quit()
echo         except Exception as e:
echo             self.progress.stop()
echo             messagebox.showerror("Error", f"Installation failed:\n{str(e)}"^)
echo             self.install_btn.config(state=tk.NORMAL^)
echo.
echo     def create_shortcuts(self, install_dir^):
echo         import win32com.client
echo         shell = win32com.client.Dispatch("WScript.Shell"^)
echo         exe_path = os.path.join(install_dir, "bin/Release/publish/bnet-bot.exe"^)
echo         start_menu = os.path.join(os.getenv("APPDATA"^), r"Microsoft\Windows\Start Menu\Programs\BNetDiscordBridge"^)
echo         os.makedirs(start_menu, exist_ok=True^)
echo         shortcut = shell.CreateShortcut(os.path.join(start_menu, "BNetDiscordBridge.lnk"^)^)
echo         shortcut.TargetPath = exe_path
echo         shortcut.WorkingDirectory = install_dir
echo         shortcut.Save()
echo.
echo if __name__ == "__main__":
echo     root = tk.Tk()
echo     app = InstallerGUI(root^)
echo     root.mainloop()
) > installer_gui.py

echo ✓ Installer script created
echo.

REM Build the EXE
echo Building install.exe...
python -m PyInstaller --onefile --windowed --name install ^
    --icon=NONE ^
    --add-data ".:." ^
    installer_gui.py

if errorlevel 1 (
    echo ERROR: Failed to build installer
    pause
    exit /b 1
)

REM Copy to repo root
if exist "dist\install.exe" (
    copy "dist\install.exe" "install.exe"
    echo ✓ Success! Created: install.exe
    echo.
    echo Location: %cd%\install.exe
    echo.
    echo You can now:
    echo 1. Share install.exe with others
    echo 2. Double-click to run
    echo 3. Follow the installer wizard
) else (
    echo ERROR: Failed to create install.exe
)

echo.
pause
