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
AppId={{7208B1D9-0975-47EB-82C8-13526FD93FF3}
AppName=Forma
AppVersion={#AppVersion}
AppVerName=Forma {#AppVersion}
AppPublisher=mmmorozov
AppPublisherURL=https://t.me/mmmorozov
AppSupportURL=https://github.com/krllmrzv/Sweetch/issues
DefaultDirName={localappdata}\Programs\Forma
DefaultGroupName=Forma
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0.17763
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#ArtifactsDir}
OutputBaseFilename=Sweetch-{#AppVersion}-Setup-win-x64
SetupIconFile=..\src\Sweetch.App\Assets\Sweetch.ico
UninstallDisplayIcon={app}\Sweetch.exe
UninstallDisplayName=Forma
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
english.Autostart=Start Forma when I sign in to Windows
russian.Autostart=Запускать Forma вместе с Windows
english.ContextMenu=Add “Forma…” to the File Explorer right-click menu
russian.ContextMenu=Добавить «Forma…» в контекстное меню Проводника

[Tasks]
Name: "autostart"; Description: "{cm:Autostart}"; GroupDescription: "{cm:Integration}"
Name: "contextmenu"; Description: "{cm:ContextMenu}"; GroupDescription: "{cm:Integration}"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

; The same entries the app's own switches write (Shell/ShellIntegration.cs), so the two agree. Set here so a
; reinstall over old settings still gets them; the app keeps them pointed at its exe, --unregister removes them.
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Forma"; ValueData: """{app}\Sweetch.exe"" --background"; Flags: uninsdeletevalue; Tasks: autostart; Check: KeepAutostart
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"; ValueType: none; ValueName: "Forma"; Flags: deletevalue; Tasks: autostart; Check: KeepAutostart
Root: HKCU; Subkey: "Software\Classes\*\shell\Forma"; ValueType: string; ValueName: ""; ValueData: "Forma…"; Flags: uninsdeletekey; Tasks: contextmenu; Check: KeepContextMenu
Root: HKCU; Subkey: "Software\Classes\*\shell\Forma"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Sweetch.exe"",0"; Tasks: contextmenu; Check: KeepContextMenu
Root: HKCU; Subkey: "Software\Classes\*\shell\Forma"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Player"; Tasks: contextmenu; Check: KeepContextMenu
Root: HKCU; Subkey: "Software\Classes\*\shell\Forma\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Sweetch.exe"" --wheel ""%1"""; Tasks: contextmenu; Check: KeepContextMenu
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Forma"; ValueType: string; ValueName: ""; ValueData: "Forma…"; Flags: uninsdeletekey; Tasks: contextmenu; Check: KeepContextMenu
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Forma"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Sweetch.exe"",0"; Tasks: contextmenu; Check: KeepContextMenu
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Forma"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Player"; Tasks: contextmenu; Check: KeepContextMenu
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Forma\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Sweetch.exe"" --wheel ""%1"""; Tasks: contextmenu; Check: KeepContextMenu

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[InstallDelete]
; Shortcuts from before the rename.
Type: files; Name: "{userprograms}\Sweetch.lnk"
Type: files; Name: "{userdesktop}\Sweetch.lnk"

[Icons]
Name: "{userprograms}\Forma"; Filename: "{app}\Sweetch.exe"
Name: "{userdesktop}\Forma"; Filename: "{app}\Sweetch.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Sweetch.exe"; Description: "{cm:LaunchProgram,Forma}"; Flags: nowait postinstall skipifsilent
; A silent update started from inside Sweetch (Shell/Updater.cs passes /UPDATED=1): start the new version.
Filename: "{app}\Sweetch.exe"; Parameters: "--updated"; Flags: nowait; Check: IsAppUpdate

[Code]
const
  // Forma 1.2.0 was installed under the original Sweetch's AppId and folder.
  OldKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{CD496C86-0CF4-47DB-BFAA-2BBEA538846B}_is1';

var
  Migrated, OldAutostart, OldContextMenu: Boolean;

function IsAppUpdate: Boolean;
begin
  Result := ExpandConstant('{param:UPDATED|0}') = '1';
end;

// Coming from 1.2.0, keep what the user had chosen there instead of this installer's defaults.
function KeepAutostart: Boolean;
begin
  Result := not Migrated or OldAutostart;
end;

function KeepContextMenu: Boolean;
begin
  Result := not Migrated or OldContextMenu;
end;

function PointsTo(const Key, Name, Dir: String): Boolean;
var
  Value: String;
begin
  Result := RegQueryStringValue(HKCU, Key, Name, Value) and (Pos(Lowercase(Dir), Lowercase(Value)) > 0);
end;

// Removes Forma 1.2.0. An entry under that AppId named anything else is the original Sweetch and stays.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Name, Dir, Uninstaller, Quoted: String;
  Code, Tries: Integer;
begin
  Result := '';
  if not (RegQueryStringValue(HKCU, OldKey, 'DisplayName', Name) and (Name = 'Forma')
      and RegQueryStringValue(HKCU, OldKey, 'InstallLocation', Dir)
      and RegQueryStringValue(HKCU, OldKey, 'UninstallString', Uninstaller)) then
    Exit;
  Dir := RemoveBackslash(Dir);
  OldAutostart := PointsTo('Software\Microsoft\Windows\CurrentVersion\Run', 'Sweetch', Dir);
  OldContextMenu := PointsTo('Software\Classes\*\shell\Sweetch\command', '', Dir);
  Quoted := Dir;
  StringChangeEx(Quoted, '''', '''''', True);
  Exec('powershell.exe', '-NoProfile -Command "Get-Process Sweetch -ErrorAction SilentlyContinue | Where-Object { $_.Path -like ''' + Quoted + '\*'' } | Stop-Process -Force"',
    '', SW_HIDE, ewWaitUntilTerminated, Code);
  Exec(RemoveQuotes(Uninstaller), '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, Code);
  // The uninstaller hands over to a copy of itself and returns at once; wait for it to finish.
  Tries := 0;
  while RegKeyExists(HKCU, OldKey) and (Tries < 120) do
  begin
    Sleep(500);
    Tries := Tries + 1;
  end;
  Migrated := True;
end;

[UninstallRun]
Filename: "{app}\Sweetch.exe"; Parameters: "--unregister"; Flags: runhidden waituntilterminated; RunOnceId: "Unregister"
