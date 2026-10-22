#ifndef AppVersion
  #define AppVersion "1.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\publish"
#endif
#ifndef ArtifactsDir
  #define ArtifactsDir "..\artifacts"
#endif

[Setup]
AppId={{CD496C86-0CF4-47DB-BFAA-2BBEA538846B}
AppName=Sweetch
AppVersion={#AppVersion}
AppVerName=Sweetch {#AppVersion}
AppPublisher=mmmorozov
AppPublisherURL=https://t.me/mmmorozov
AppSupportURL=https://github.com/krllmrzv/Sweetch/issues
DefaultDirName={localappdata}\Programs\Sweetch
DefaultGroupName=Sweetch
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0.17763
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#ArtifactsDir}
OutputBaseFilename=Sweetch-{#AppVersion}-Setup-win-x64
SetupIconFile=..\src\Sweetch.App\Assets\Sweetch.ico
UninstallDisplayIcon={app}\Sweetch.exe
UninstallDisplayName=Sweetch
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[CustomMessages]
english.Integration=Windows integration:
russian.Integration=Интеграция с Windows:
english.Autostart=Start Sweetch when I sign in to Windows
russian.Autostart=Запускать Sweetch вместе с Windows
english.ContextMenu=Add “Sweetch…” to the File Explorer right-click menu
russian.ContextMenu=Добавить «Sweetch…» в контекстное меню Проводника

[Tasks]
Name: "autostart"; Description: "{cm:Autostart}"; GroupDescription: "{cm:Integration}"
Name: "contextmenu"; Description: "{cm:ContextMenu}"; GroupDescription: "{cm:Integration}"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

; The same entries the app's own switches write (Shell/ShellIntegration.cs), so the two agree. Set here so a
; reinstall over old settings still gets them; the app keeps them pointed at its exe, --unregister removes them.
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Sweetch"; ValueData: """{app}\Sweetch.exe"" --background"; Flags: uninsdeletevalue; Tasks: autostart
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"; ValueType: none; ValueName: "Sweetch"; Flags: deletevalue; Tasks: autostart
Root: HKCU; Subkey: "Software\Classes\*\shell\Sweetch"; ValueType: string; ValueName: ""; ValueData: "Sweetch…"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\*\shell\Sweetch"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Sweetch.exe"",0"; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\*\shell\Sweetch"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Player"; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\*\shell\Sweetch\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Sweetch.exe"" --wheel ""%1"""; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Sweetch"; ValueType: string; ValueName: ""; ValueData: "Sweetch…"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Sweetch"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Sweetch.exe"",0"; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Sweetch"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Player"; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Sweetch\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Sweetch.exe"" --wheel ""%1"""; Tasks: contextmenu

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{userprograms}\Sweetch"; Filename: "{app}\Sweetch.exe"
Name: "{userdesktop}\Sweetch"; Filename: "{app}\Sweetch.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Sweetch.exe"; Description: "{cm:LaunchProgram,Sweetch}"; Flags: nowait postinstall skipifsilent
; A silent update started from inside Sweetch (Shell/Updater.cs passes /UPDATED=1): start the new version.
Filename: "{app}\Sweetch.exe"; Parameters: "--updated"; Flags: nowait; Check: IsAppUpdate

[Code]
function IsAppUpdate: Boolean;
begin
  Result := ExpandConstant('{param:UPDATED|0}') = '1';
end;

[UninstallRun]
Filename: "{app}\Sweetch.exe"; Parameters: "--unregister"; Flags: runhidden waituntilterminated; RunOnceId: "Unregister"
