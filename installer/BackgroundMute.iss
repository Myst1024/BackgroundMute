#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#define MyAppName "BackgroundMute"
#define MyAppPublisher "Myst1024"
#define MyAppURL "https://github.com/Myst1024/BackgroundMute"
#define MyAppExeName "BackgroundMute.exe"

[Setup]
AppId={{A2374D89-7E9D-4D4D-8F18-93D4C2D2C7B4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=BackgroundMute-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
SetupIconFile=..\BackgroundMute.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\bin\Release\net10.0-windows\win-x64\publish\BackgroundMute.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\BackgroundMute"; Filename: "{app}\BackgroundMute.exe"
Name: "{group}\Uninstall BackgroundMute"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\BackgroundMute.exe"; Description: "Launch BackgroundMute"; Flags: nowait postinstall skipifsilent
