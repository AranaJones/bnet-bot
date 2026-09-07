; BNetDiscordBridge Installer
; NSIS Installer Script for Windows

!include "MUI2.nsh"
!include "FileFunc.nsh"

; General
Name "BNetDiscordBridge"
OutFile "BNetDiscordBridge-Setup.exe"
InstallDir "$PROGRAMFILES\BNetDiscordBridge"
InstallDirRegKey HKCU "Software\BNetDiscordBridge" "InstallDir"
RequestExecutionLevel admin

; MUI Settings
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

; Check .NET 8.0
Function CheckDotNet
  ReadRegStr $0 HKLM "SOFTWARE\dotnet\Setup\InstalledVersions\x64" "release"
  ${If} $0 == ""
    MessageBox MB_YESNO ".NET 8.0 SDK not found. Install now?" IDYES InstallDotNet IDNO SkipDotNet
    
    InstallDotNet:
      DetailPrint "Downloading .NET 8.0 SDK..."
      nsExec::ExecToLog "powershell -Command `
        $url = 'https://dot.net/v1/dotnet-install.ps1'; `
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; `
        (New-Object System.Net.WebClient).DownloadFile($url, '$TEMP\dotnet-install.ps1'); `
        powershell -ExecutionPolicy Bypass -File $TEMP\dotnet-install.ps1 -Channel 8.0 -InstallDir $env:ProgramFiles\dotnet"
      Pop $0
      ${If} $0 != 0
        MessageBox MB_OK "Failed to install .NET 8.0. Please install manually from https://dotnet.microsoft.com/download"
        Abort
      ${EndIf}
      DetailPrint ".NET 8.0 installed successfully"
    SkipDotNet:
  ${EndIf}
FunctionEnd

Section "Install"
  SetOutPath "$INSTDIR"
  
  ; Check .NET
  Call CheckDotNet
  
  ; Create installation directory
  CreateDirectory "$INSTDIR"
  CreateDirectory "$INSTDIR\bin"
  CreateDirectory "$APPDATA\BNetDiscordBridge"
  
  DetailPrint "Cloning BNetDiscordBridge from GitHub..."
  nsExec::ExecToLog "git clone https://github.com/AranaJones/bnet-bot.git $INSTDIR"
  Pop $0
  ${If} $0 != 0
    MessageBox MB_OK "Failed to clone repository. Make sure Git is installed."
    Abort
  ${EndIf}
  
  DetailPrint "Installing dependencies..."
  SetOutPath "$INSTDIR"
  nsExec::ExecToLog "dotnet restore"
  Pop $0
  ${If} $0 != 0
    MessageBox MB_OK "Failed to restore NuGet packages."
    Abort
  ${EndIf}
  
  DetailPrint "Building project..."
  nsExec::ExecToLog "dotnet build -c Release"
  Pop $0
  ${If} $0 != 0
    MessageBox MB_OK "Failed to build project."
    Abort
  ${EndIf}
  
  DetailPrint "Publishing executable..."
  nsExec::ExecToLog "dotnet publish -c Release -o $INSTDIR\bin"
  Pop $0
  ${If} $0 != 0
    MessageBox MB_OK "Failed to publish executable."
    Abort
  ${EndIf}
  
  ; Copy config template
  ${If} ${FileExists} "$INSTDIR\appsettings.example.json"
    CopyFiles "$INSTDIR\appsettings.example.json" "$APPDATA\BNetDiscordBridge\appsettings.json"
  ${EndIf}
  
  ; Write registry
  WriteRegStr HKCU "Software\BNetDiscordBridge" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\BNetDiscordBridge" "ConfigDir" "$APPDATA\BNetDiscordBridge"
  
  ; Create shortcuts
  CreateDirectory "$SMPROGRAMS\BNetDiscordBridge"
  CreateShortCut "$SMPROGRAMS\BNetDiscordBridge\BNetDiscordBridge.lnk" "$INSTDIR\bin\bnet-bot.exe" "" "$INSTDIR\bin\bnet-bot.exe" 0
  CreateShortCut "$SMPROGRAMS\BNetDiscordBridge\Configure Bot.lnk" "notepad.exe" "$APPDATA\BNetDiscordBridge\appsettings.json"
  CreateShortCut "$SMPROGRAMS\BNetDiscordBridge\Uninstall.lnk" "$INSTDIR\uninstall.exe"
  CreateShortCut "$DESKTOP\BNetDiscordBridge.lnk" "$INSTDIR\bin\bnet-bot.exe" "" "$INSTDIR\bin\bnet-bot.exe" 0
  
  ; Create uninstaller
  WriteUninstaller "$INSTDIR\uninstall.exe"
  
  DetailPrint "Installation complete!"
  MessageBox MB_OK "Installation complete!$\n$\nNext steps:$\n1. Edit config: $APPDATA\BNetDiscordBridge\appsettings.json$\n2. Start BNLS server$\n3. Run BNetDiscordBridge from Start Menu"
SectionEnd

Section "Uninstall"
  DetailPrint "Uninstalling BNetDiscordBridge..."
  
  ; Remove files
  RMDir /r "$INSTDIR"
  RMDir /r "$SMPROGRAMS\BNetDiscordBridge"
  Delete "$DESKTOP\BNetDiscordBridge.lnk"
  
  ; Remove registry
  DeleteRegKey HKCU "Software\BNetDiscordBridge"
  
  MessageBox MB_OK "Uninstallation complete."
SectionEnd
