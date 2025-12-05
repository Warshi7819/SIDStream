[Setup]
AppName=SIDstream
AppVersion=1.0
DefaultDirName={autopf}\SIDstream
DefaultGroupName=SIDstream
OutputBaseFilename=SIDstream Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern dynamic

[Files]
Source: "<insert build directory here>\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\SIDstream"; Filename: "{app}\SIDstream.exe"

[Run]
Filename: "{app}\SIDstream.exe"; Description: "Launch SIDstream"; Flags: nowait postinstall skipifdoesntexist

[UninstallRun]
Filename: "{app}\unins000.exe"; Parameters: "/SILENT"; RunOnceId: UninstallSIDstreamerSilentCleanup