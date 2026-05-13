#define MyAppName "CS2 Performance System"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Norevia"
#define MyAppURL "https://norevia.app"
#define MyAppExeName "CS2PerformanceSystem.exe"

[Setup]
AppId={{C1C7C5D1-9C60-4F2A-BF33-001122334455}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\Norevia\CS2PerformanceSystem
DefaultGroupName=Norevia
OutputDir=C:\Builds\Installer
OutputBaseFilename=CS2PerformanceSystem_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "C:\Users\almir\OneDrive\Desktop\project2\CS2ModelSystem\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\CS2 Performance System"; Filename: "{app}\CS2PerformanceSystem.exe"
Name: "{group}\Uninstall CS2 Performance System"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\CS2PerformanceSystem.exe"; Description: "Launch CS2 Performance System"; Flags: nowait postinstall skipifsilent