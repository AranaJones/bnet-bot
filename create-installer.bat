@echo off
cd /d "%~dp0"
echo Delegating to build-installer.bat...
call build-installer.bat
exit /b %errorlevel%
