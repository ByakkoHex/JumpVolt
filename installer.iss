; Wersja może być przekazana z zewnątrz przez ISCC /DMyAppVersion=x.y.z
; (GitHub Actions robi to automatycznie z taga git)
#ifndef MyAppVersion
  #define MyAppVersion "0.2.0"
#endif

[Setup]
AppName=JumpVolt
AppVersion={#MyAppVersion}
AppVerName=JumpVolt {#MyAppVersion}
AppPublisher=JumpVolt
AppPublisherURL=https://github.com/ByakkoHex/JumpVolt
AppSupportURL=https://github.com/ByakkoHex/JumpVolt/issues
AppUpdatesURL=https://github.com/ByakkoHex/JumpVolt/releases

DefaultDirName={autopf}\JumpVolt
DefaultGroupName=JumpVolt
DisableProgramGroupPage=yes

OutputDir=installer_output
OutputBaseFilename=JumpVolt_Setup_{#MyAppVersion}
SetupIconFile=SklepMotoryzacyjny\Resources\app.ico

Compression=lzma2
SolidCompression=yes

; Uninstaller
UninstallDisplayName=JumpVolt - System Sprzedaży
UninstallDisplayIcon={app}\SklepMotoryzacyjny.exe
CreateUninstallRegKey=yes

; Wymagaj uprawnień administratora (instalacja w Program Files)
PrivilegesRequired=admin

; Minimalna wersja Windowsa: Windows 10
MinVersion=10.0

; Języki
[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"

[Tasks]
Name: "desktopicon";    Description: "Utwórz skrót na pulpicie";  GroupDescription: "Dodatkowe skróty:"
Name: "startupentry";   Description: "Uruchamiaj JumpVolt razem z Windows"; GroupDescription: "Autostart:"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Menu Start
Name: "{group}\JumpVolt";            Filename: "{app}\SklepMotoryzacyjny.exe"
Name: "{group}\Odinstaluj JumpVolt"; Filename: "{uninstallexe}"

; Pulpit (opcjonalnie)
Name: "{commondesktop}\JumpVolt"; Filename: "{app}\SklepMotoryzacyjny.exe"; Tasks: desktopicon

[Registry]
; Autostart (opcjonalnie — zadanie startupentry)
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "JumpVolt"; ValueData: """{app}\SklepMotoryzacyjny.exe"""; Flags: uninsdeletevalue; Tasks: startupentry

[Run]
; Uruchom aplikację po instalacji (opcjonalnie)
Filename: "{app}\SklepMotoryzacyjny.exe"; Description: "Uruchom JumpVolt"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Zamknij aplikację przed odinstalowaniem (jeśli działa)
Filename: "taskkill.exe"; Parameters: "/F /IM SklepMotoryzacyjny.exe"; Flags: runhidden; RunOnceId: "KillApp"

[UninstallDelete]
; Usuń dane aplikacji (baza, logi) — opcjonalne, zakomentuj jeśli niepotrzebne
; Type: filesandordirs; Name: "{localappdata}\JumpVolt"
