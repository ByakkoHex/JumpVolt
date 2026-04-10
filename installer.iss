[Setup]
AppName=JumpVolt
AppVersion=1.0
DefaultDirName={autopf}\JumpVolt
DefaultGroupName=JumpVolt
OutputDir=installer_output
OutputBaseFilename=JumpVolt_Setup
SetupIconFile=SklepMotoryzacyjny\Resources\app.ico
Compression=lzma2
SolidCompression=yes

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\JumpVolt"; Filename: "{app}\SklepMotoryzacyjny.exe"
Name: "{commondesktop}\JumpVolt"; Filename: "{app}\SklepMotoryzacyjny.exe"