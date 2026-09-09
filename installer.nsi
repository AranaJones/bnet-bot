; BNetDiscordBridge Installer
; NSIS Installer Script for Windows

!include "MUI2.nsh"

!ifndef OUTPUT_EXE
!define OUTPUT_EXE "install.exe"
!endif

; General
Name "BNetDiscordBridge"
OutFile "${OUTPUT_EXE}"
InstallDir "$PROGRAMFILES\BNetDiscordBridge"
InstallDirRegKey HKCU "Software\BNetDiscordBridge" "InstallDir"
RequestExecutionLevel admin

; MUI Settings
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

Section "Install"
  SetOutPath "$INSTDIR"

  ; App files built by build-installer.bat
  File /r "bin\Release\publish\*.*"
  File "appsettings.example.json"
  File "README.md"

  ; Per-user config defaults
  CreateDirectory "$APPDATA\BNetDiscordBridge"
  IfFileExists "$APPDATA\BNetDiscordBridge\appsettings.json" +2 0
  CopyFiles "$INSTDIR\appsettings.example.json" "$APPDATA\BNetDiscordBridge\appsettings.json"

  ; Write registry
  WriteRegStr HKCU "Software\BNetDiscordBridge" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\BNetDiscordBridge" "ConfigDir" "$APPDATA\BNetDiscordBridge"

  ; Create shortcuts
  CreateDirectory "$SMPROGRAMS\BNetDiscordBridge"
  CreateShortCut "$SMPROGRAMS\BNetDiscordBridge\BNetDiscordBridge.lnk" "$INSTDIR\bnet-bot.exe" "" "$INSTDIR\bnet-bot.exe" 0
  CreateShortCut "$SMPROGRAMS\BNetDiscordBridge\Configure Bot.lnk" "notepad.exe" "$APPDATA\BNetDiscordBridge\appsettings.json"
  CreateShortCut "$SMPROGRAMS\BNetDiscordBridge\Uninstall.lnk" "$INSTDIR\uninstall.exe"
  CreateShortCut "$DESKTOP\BNetDiscordBridge.lnk" "$INSTDIR\bnet-bot.exe" "" "$INSTDIR\bnet-bot.exe" 0

  ; Create uninstaller
  WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd

Section "Uninstall"
  ; Remove files
  RMDir /r "$INSTDIR"
  RMDir /r "$SMPROGRAMS\BNetDiscordBridge"
  Delete "$DESKTOP\BNetDiscordBridge.lnk"

  ; Remove registry
  DeleteRegKey HKCU "Software\BNetDiscordBridge"
SectionEnd
