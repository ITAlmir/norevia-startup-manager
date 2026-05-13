#define MyAppName "Norevia Startup Manager Lite"
#define MyAppExeName "Norevia Startup Manager Lite.exe"
#define MyAppVersion "1.0.0"
#define MyCompany "Norevia"
#define MyURL "https://norevia.app"

[Setup]
AppId={{6F9D8A90-7D7A-4F2C-9A2C-0B3B6F0C1001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyCompany}
AppPublisherURL={#MyURL}
AppSupportURL={#MyURL}
AppUpdatesURL={#MyURL}
DefaultDirName={autopf}\{#MyCompany}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=output
OutputBaseFilename={#MyAppName}-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
WizardStyle=modern

; Ako imaš app.ico u Assets folderu projekta, uključi ovu liniju.
; Ako nemaš, ostavi zakomentarisano.
; SetupIconFile=..\Assets\app.ico

UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
; ✅ Tvoj publish EXE (tačna putanja relativno iz Installer foldera)
Source: "..\bin\Release\net8.0-windows\publish\win-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent