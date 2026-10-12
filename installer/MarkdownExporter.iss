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

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ja"; MessagesFile: "compiler:Languages\Japanese.isl"

[CustomMessages]
en.DesktopShortcut=Create a desktop shortcut
ja.DesktopShortcut=デスクトップにショートカットを作成する
en.RuntimeInstallFailed=WebView2 Runtime could not be installed. Check your internet connection and try again. You can install it manually from https://developer.microsoft.com/microsoft-edge/webview2/.
ja.RuntimeInstallFailed=WebView2 Runtimeをインストールできませんでした。ネットワーク接続を確認して再試行してください。https://developer.microsoft.com/microsoft-edge/webview2/ から手動でもインストールできます。

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopShortcut}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\mdexport.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\Template\*"; DestDir: "{app}\Template"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PublishDir}\WebAssets\*"; DestDir: "{app}\WebAssets"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PublishDir}\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\PDFsharp-LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.en.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BootstrapperPath}"; DestName: "MicrosoftEdgeWebview2Setup.exe"; Flags: dontcopy

[Icons]
Name: "{group}\MarkdownExporter"; Filename: "{app}\mdexport.exe"
Name: "{autodesktop}\MarkdownExporter"; Filename: "{app}\mdexport.exe"; Tasks: desktopicon

[Code]
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
