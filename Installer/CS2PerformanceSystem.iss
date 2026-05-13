[Setup]
AppName=CS2 Performance System
AppVersion=1.0.0
AppPublisher=Norevia
DefaultDirName={autopf}\Norevia\CS2 Performance System
DefaultGroupName=CS2 Performance System
DisableProgramGroupPage=yes
OutputDir=C:\Users\almir\Desktop\CS2Installer
OutputBaseFilename=Setup_CS2PerformanceSystem_1.0.0
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

; opcionalno (ako želiš ikonu):
SetupIconFile=C:\Users\almir\Projects\Project2Apps\CS2PerformanceSystem\CS2PerformanceSystem\Assets\Icons\app.ico

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "C:\Users\almir\Projects\Project2Apps\CS2PerformanceSystem\CS2PerformanceSystem\bin\Release\net8.0-windows\publish\win-x64\*"; Excludes: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\CS2 Performance System"; Filename: "{app}\CS2PerformanceSystem.exe"
Name: "{autodesktop}\CS2 Performance System"; Filename: "{app}\CS2PerformanceSystem.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\CS2PerformanceSystem.exe"; Description: "Launch CS2 Performance System"; Flags: nowait postinstall skipifsilent