#ifndef AppVersion
  #error AppVersion must be supplied to ISCC
#endif
#ifndef PublishDir
  #error PublishDir must be supplied to ISCC
#endif
#ifndef BootstrapperPath
  #error BootstrapperPath must be supplied to ISCC
#endif

[Setup]
AppId={{517F2044-7136-4F81-A618-136696592967}
AppName=MarkdownExporter
AppVersion={#AppVersion}
AppPublisher=Ovis
AppPublisherURL=https://github.com/Ovis/MarkdownExporter
AppSupportURL=https://github.com/Ovis/MarkdownExporter/issues
DefaultDirName={localappdata}\Programs\MarkdownExporter
DefaultGroupName=MarkdownExporter
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
MinVersion=10.0.26100
OutputBaseFilename=MarkdownExporter-{#AppVersion}-win-x64-setup
SetupIconFile=..\MarkdownExporter\Assets\AppIcon.ico
UninstallDisplayIcon={app}\mdexport.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
ChangesEnvironment=yes
ChangesAssociations=yes

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ja"; MessagesFile: "compiler:Languages\Japanese.isl"

[CustomMessages]
en.DesktopShortcut=Create a desktop shortcut
ja.DesktopShortcut=デスクトップにショートカットを作成する
en.SendToShortcut=Add MarkdownExporter to the Send To menu
ja.SendToShortcut=エクスプローラーの「送る」にMarkdownExporterを追加する
en.SendToUpdateFailed=Could not update the Send To shortcut.
ja.SendToUpdateFailed=「送る」メニューのショートカットを更新できませんでした。
en.ContextMenuCommand=Open in MarkdownExporter
ja.ContextMenuCommand=MarkdownExporterで開く
en.ContextMenuTask=Add MarkdownExporter to the classic File Explorer context menu
ja.ContextMenuTask=エクスプローラーの右クリックメニューにMarkdownExporterを追加する
en.ContextMenuUpdateFailed=Could not update the File Explorer context menu.
ja.ContextMenuUpdateFailed=エクスプローラーの右クリックメニューを更新できませんでした。
en.AddToPath=Add MarkdownExporter to the user PATH
ja.AddToPath=MarkdownExporterをユーザーのPATHに追加する
en.PathUpdateFailed=Could not update the user PATH. MarkdownExporter was installed, but mdexport.exe cannot be run by name until PATH is updated.
ja.PathUpdateFailed=ユーザーのPATHを更新できませんでした。MarkdownExporterはインストールされましたが、PATHを更新するまでmdexport.exeを名前だけで実行できません。
en.RuntimeInstallFailed=WebView2 Runtime could not be installed. Check your internet connection and try again. You can install it manually from https://developer.microsoft.com/microsoft-edge/webview2/.
ja.RuntimeInstallFailed=WebView2 Runtimeをインストールできませんでした。ネットワーク接続を確認して再試行してください。https://developer.microsoft.com/microsoft-edge/webview2/ から手動でもインストールできます。

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopShortcut}"; Flags: unchecked
Name: "sendto"; Description: "{cm:SendToShortcut}"
Name: "contextmenu"; Description: "{cm:ContextMenuTask}"
Name: "addtopath"; Description: "{cm:AddToPath}"

[Files]
Source: "{#PublishDir}\mdexport.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\Template\*"; DestDir: "{app}\Template"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PublishDir}\WebAssets\*"; DestDir: "{app}\WebAssets"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PublishDir}\Licence\*"; DestDir: "{app}\Licence"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PublishDir}\Tools\qpdf\*"; DestDir: "{app}\Tools\qpdf"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.en.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BootstrapperPath}"; DestName: "MicrosoftEdgeWebview2Setup.exe"; Flags: dontcopy

[Icons]
Name: "{group}\MarkdownExporter"; Filename: "{app}\mdexport.exe"
Name: "{autodesktop}\MarkdownExporter"; Filename: "{app}\mdexport.exe"; Tasks: desktopicon
Name: "{usersendto}\MarkdownExporter"; Filename: "{app}\mdexport.exe"; Tasks: sendto

[Code]
const
  UserEnvironmentKey = 'Environment';
  InstallerStateKey = 'Software\MarkdownExporter\Installer';
  ContextMenuKey = 'Software\Classes\AllFilesystemObjects\shell\EsheepDev.MarkdownExporter';
  RegSz = 1;
  RegExpandSz = 2;
  KeyQueryValue = 1;

function RegOpenKeyExW(RootKey: LongWord; SubKey: String; Options, DesiredAccess: LongWord; var Key: LongWord): LongWord;
  external 'RegOpenKeyExW@advapi32.dll stdcall';
function RegQueryValueExW(Key: LongWord; ValueName: String; Reserved: LongWord;
  var ValueType: LongWord; Data: LongWord; var DataSize: LongWord): LongWord;
  external 'RegQueryValueExW@advapi32.dll stdcall';
function RegCloseKeyW(Key: LongWord): LongWord;
  external 'RegCloseKey@advapi32.dll stdcall';
function SendMessageTimeoutW(Window, Message, WParam: LongWord; LParam: String;
  Flags, Timeout: LongWord; var MessageResult: LongWord): LongWord;
  external 'SendMessageTimeoutW@user32.dll stdcall';

function UserPathType: LongWord;
var
  Key, ValueType, DataSize: LongWord;
begin
  Result := RegExpandSz;
  if RegOpenKeyExW(HKCU, UserEnvironmentKey, 0, KeyQueryValue, Key) <> 0 then
    Exit;
  try
    DataSize := 0;
    ValueType := 0;
    if (RegQueryValueExW(Key, 'Path', 0, ValueType, 0, DataSize) = 0) and
       ((ValueType = RegSz) or (ValueType = RegExpandSz)) then
      Result := ValueType;
  finally
    RegCloseKeyW(Key);
  end;
end;

function ReadUserPath(var Value: String): Boolean;
begin
  Value := '';
  Result := not RegValueExists(HKCU, UserEnvironmentKey, 'Path') or
    RegQueryStringValue(HKCU, UserEnvironmentKey, 'Path', Value);
end;

function WriteUserPath(const Value: String): Boolean;
begin
  if UserPathType = RegSz then
    Result := RegWriteStringValue(HKCU, UserEnvironmentKey, 'Path', Value)
  else
    Result := RegWriteExpandStringValue(HKCU, UserEnvironmentKey, 'Path', Value);
end;

function NormalizedPathEntry(const Value: String): String;
begin
  Result := Trim(Value);
  if (Length(Result) >= 2) and (Result[1] = '"') and
     (Result[Length(Result)] = '"') then
    Result := Copy(Result, 2, Length(Result) - 2);
  while (Length(Result) > 3) and (Result[Length(Result)] = '\') do
    Delete(Result, Length(Result), 1);
  Result := LowerCase(Result);
end;

function FindPathEntry(const PathValue, Candidate: String;
  var EntryStart, EntryEnd: Integer): Boolean;
var
  StartAt, Separator: Integer;
  Wanted: String;
begin
  Result := False;
  Wanted := NormalizedPathEntry(Candidate);
  StartAt := 1;
  while StartAt <= Length(PathValue) + 1 do
  begin
    Separator := Pos(';', Copy(PathValue, StartAt, MaxInt));
    if Separator = 0 then
      EntryEnd := Length(PathValue) + 1
    else
      EntryEnd := StartAt + Separator - 1;
    if NormalizedPathEntry(Copy(PathValue, StartAt, EntryEnd - StartAt)) = Wanted then
    begin
      EntryStart := StartAt;
      Result := True;
    end;
    if Separator = 0 then
      Break;
    StartAt := EntryEnd + 1;
  end;
end;

function RemovePathEntry(const PathValue, Candidate: String; var NewValue: String): Boolean;
var
  EntryStart, EntryEnd: Integer;
begin
  Result := FindPathEntry(PathValue, Candidate, EntryStart, EntryEnd);
  if not Result then
    Exit;
  if EntryStart > 1 then
    NewValue := Copy(PathValue, 1, EntryStart - 2) +
      Copy(PathValue, EntryEnd, MaxInt)
  else if EntryEnd <= Length(PathValue) then
    NewValue := Copy(PathValue, EntryEnd + 1, MaxInt)
  else
    NewValue := '';
end;

function RemoveOwnedPath: Boolean;
var
  AddedPath, CurrentPath, NewPath: String;
begin
  Result := True;
  if not RegQueryStringValue(HKCU, InstallerStateKey, 'PathAdded', AddedPath) then
    Exit;
  if AddedPath = '' then
  begin
    Result := False;
    Exit;
  end;
  if not ReadUserPath(CurrentPath) then
  begin
    Result := False;
    Exit;
  end;
  if RemovePathEntry(CurrentPath, AddedPath, NewPath) and
     not WriteUserPath(NewPath) then
  begin
    Result := False;
    Exit;
  end;
  RegDeleteValue(HKCU, InstallerStateKey, 'PathAdded');
  RegDeleteKeyIfEmpty(HKCU, InstallerStateKey);
end;

function AddToUserPath: Boolean;
var
  CurrentPath, OriginalPath, AddedPath, AppPath: String;
  EntryStart, EntryEnd: Integer;
begin
  Result := False;
  AppPath := ExpandConstant('{app}');
  if RegQueryStringValue(HKCU, InstallerStateKey, 'PathAdded', AddedPath) and
     (NormalizedPathEntry(AddedPath) <> NormalizedPathEntry(AppPath)) and
     not RemoveOwnedPath then
    Exit;
  if not ReadUserPath(CurrentPath) then
    Exit;
  if FindPathEntry(CurrentPath, AppPath, EntryStart, EntryEnd) then
  begin
    Result := True;
    Exit;
  end;
  OriginalPath := CurrentPath;
  if (CurrentPath <> '') and (CurrentPath[Length(CurrentPath)] <> ';') then
    CurrentPath := CurrentPath + ';';
  if not WriteUserPath(CurrentPath + AppPath) then
    Exit;
  if not RegWriteStringValue(HKCU, InstallerStateKey, 'PathAdded', AppPath) then
  begin
    WriteUserPath(OriginalPath);
    Exit;
  end;
  Result := True;
end;

function SyncSendToShortcut: Boolean;
var
  OwnedShortcut: String;
begin
  Result := False;
  if WizardIsTaskSelected('sendto') then
  begin
    Result := RegWriteStringValue(HKCU, InstallerStateKey, 'SendToAdded',
      ExpandConstant('{usersendto}\MarkdownExporter.lnk'));
    Exit;
  end;

  if RegQueryStringValue(HKCU, InstallerStateKey, 'SendToAdded', OwnedShortcut) then
  begin
    if FileExists(OwnedShortcut) and not DeleteFile(OwnedShortcut) then
      Exit;
    RegDeleteValue(HKCU, InstallerStateKey, 'SendToAdded');
    RegDeleteKeyIfEmpty(HKCU, InstallerStateKey);
  end;
  Result := True;
end;

function SyncContextMenu: Boolean;
var
  Command: String;
begin
  if not WizardIsTaskSelected('contextmenu') then
  begin
    Result := not RegKeyExists(HKCU, ContextMenuKey) or
      RegDeleteKeyIncludingSubkeys(HKCU, ContextMenuKey);
    Exit;
  end;

  Command := '"' + ExpandConstant('{app}\mdexport.exe') + '" "%1"';
  Result :=
    RegWriteStringValue(HKCU, ContextMenuKey, '', CustomMessage('ContextMenuCommand')) and
    RegWriteStringValue(HKCU, ContextMenuKey, 'Icon',
      '"' + ExpandConstant('{app}\mdexport.exe') + '",0') and
    RegWriteStringValue(HKCU, ContextMenuKey, 'MultiSelectModel', 'Player') and
    RegWriteStringValue(HKCU, ContextMenuKey, 'NeverDefault', '') and
    RegWriteStringValue(HKCU, ContextMenuKey + '\command', '', Command);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep <> ssPostInstall then
    Exit;
  if not SyncSendToShortcut then
    RaiseException(CustomMessage('SendToUpdateFailed'));
  if not SyncContextMenu then
    RaiseException(CustomMessage('ContextMenuUpdateFailed'));
  if WizardIsTaskSelected('addtopath') then
  begin
    if not AddToUserPath then
      RaiseException(CustomMessage('PathUpdateFailed'));
  end
  else if not RemoveOwnedPath then
    RaiseException(CustomMessage('PathUpdateFailed'));
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  MessageResult: LongWord;
begin
  if CurUninstallStep <> usPostUninstall then
    Exit;
  if RemoveOwnedPath then
    SendMessageTimeoutW($FFFF, $001A, 0, 'Environment', 2, 5000, MessageResult);
  RegDeleteValue(HKCU, InstallerStateKey, 'SendToAdded');
  RegDeleteKeyIfEmpty(HKCU, InstallerStateKey);
  RegDeleteKeyIncludingSubkeys(HKCU, ContextMenuKey);
end;

function HasRuntimeVersion(const Version: String): Boolean;
begin
  Result := (Version <> '') and (Version <> '0.0.0.0');
end;

function IsWebView2RuntimeInstalled(): Boolean;
var
  Version: String;
begin
  Result :=
    (RegQueryStringValue(HKLM32, 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) and HasRuntimeVersion(Version)) or
    (RegQueryStringValue(HKCU, 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) and HasRuntimeVersion(Version));
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ExitCode: Integer;
begin
  Result := '';
  if IsWebView2RuntimeInstalled() then
    Exit;

  ExtractTemporaryFile('MicrosoftEdgeWebview2Setup.exe');
  if not Exec(ExpandConstant('{tmp}\MicrosoftEdgeWebview2Setup.exe'), '/silent /install', '', SW_HIDE, ewWaitUntilTerminated, ExitCode) then
  begin
    Result := CustomMessage('RuntimeInstallFailed');
    Exit;
  end;
  if not IsWebView2RuntimeInstalled() then
    Result := CustomMessage('RuntimeInstallFailed');
end;
