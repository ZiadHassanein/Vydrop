#ifndef GeneratedConfig
  #error Pass GeneratedConfig from scripts/build-installer.ps1
#endif
#include GeneratedConfig

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Ziad Hassanein
AppPublisherURL=https://github.com/ZiadHassanein/Vydrop
AppSupportURL=https://github.com/ZiadHassanein/Vydrop/issues
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
WizardStyle=modern dynamic windows11 hidebevels
WizardSizePercent=100
WizardSmallImageFile={#RepoRoot}\assets\browser-icon-128.png
WizardImageFile={#RepoRoot}\assets\installer\panel-light.bmp
WizardImageFileDynamicDark={#RepoRoot}\assets\installer\panel-dark.bmp
WizardImageStretch=yes
SetupIconFile={#RepoRoot}\assets\app.ico
UninstallDisplayIcon={app}\Vydrop.exe
CloseApplications=yes
CloseApplicationsFilter=Vydrop.exe
RestartApplications=no
DisableProgramGroupPage=yes
DisableDirPage=auto
LicenseFile={#RepoRoot}\LICENSE
Compression=lzma2
SolidCompression=yes
OutputDir={#OutputDirectory}
OutputBaseFilename={#OutputName}
VersionInfoVersion={#NumericVersion}
UninstallDisplayName={#AppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Files]
Source: "{#PackageDirectory}\Vydrop.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PackageDirectory}\README.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PackageDirectory}\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PackageDirectory}\package.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PackageDirectory}\Browser\*"; DestDir: "{app}\Browser"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#InstallationMarker}"; DestDir: "{app}"; DestName: "installation.json"; Flags: ignoreversion; AfterInstall: WriteNativeManifests

[Icons]
Name: "{userprograms}\{#AppName}"; Filename: "{app}\Vydrop.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\Vydrop.exe"; Tasks: desktopicon; WorkingDir: "{app}"

[Registry]
Root: HKCU32; Subkey: "Software\Google\Chrome\NativeMessagingHosts\{#HostName}"; ValueType: string; ValueData: "{app}\Integration\{#HostName}.chromium.json"
Root: HKCU64; Subkey: "Software\Google\Chrome\NativeMessagingHosts\{#HostName}"; ValueType: string; ValueData: "{app}\Integration\{#HostName}.chromium.json"
Root: HKCU32; Subkey: "Software\Microsoft\Edge\NativeMessagingHosts\{#HostName}"; ValueType: string; ValueData: "{app}\Integration\{#HostName}.chromium.json"
Root: HKCU64; Subkey: "Software\Microsoft\Edge\NativeMessagingHosts\{#HostName}"; ValueType: string; ValueData: "{app}\Integration\{#HostName}.chromium.json"
Root: HKCU32; Subkey: "Software\Mozilla\NativeMessagingHosts\{#HostName}"; ValueType: string; ValueData: "{app}\Integration\{#HostName}.firefox.json"
Root: HKCU64; Subkey: "Software\Mozilla\NativeMessagingHosts\{#HostName}"; ValueType: string; ValueData: "{app}\Integration\{#HostName}.firefox.json"

[Run]
Filename: "{app}\Vydrop.exe"; Description: "{cm:OpenVydrop}"; Flags: nowait postinstall skipifsilent

[CustomMessages]
english.OpenVydrop=Open Vydrop
arabic.OpenVydrop=فتح Vydrop

english.CloseVydrop=Close Vydrop, then click Retry. Your settings and downloads are kept.
arabic.CloseVydrop=أغلق Vydrop ثم اضغط إعادة المحاولة. ستبقى إعداداتك وتنزيلاتك محفوظة.

[UninstallDelete]
Type: files; Name: "{app}\Integration\{#HostName}.chromium.json"
Type: files; Name: "{app}\Integration\{#HostName}.firefox.json"
Type: dirifempty; Name: "{app}\Integration"

[Code]
#include "app-lifecycle.iss"

function JsonString(Value: String): String;
begin
  StringChangeEx(Value, '\', '\\', True);
  StringChangeEx(Value, '"', '\"', True);
  Result := '"' + Value + '"';
end;

procedure WriteNativeManifests;
var Directory, Common: String;
begin
  Directory := ExpandConstant('{app}\Integration');
  if not ForceDirectories(Directory) then RaiseException('Could not create the browser connection folder.');
  Common := '{"name":"{#HostName}","description":"Send a media link to Vydrop","type":"stdio","path":' + JsonString(ExpandConstant('{app}\Vydrop.exe'));
  if not SaveStringToFile(Directory + '\{#HostName}.chromium.json', UTF8Encode(Common + ',"allowed_origins":{#ChromiumOrigins}}'), False) then RaiseException('Could not save the browser connection.');
  if not SaveStringToFile(Directory + '\{#HostName}.firefox.json', UTF8Encode(Common + ',"allowed_extensions":["{#FirefoxId}"]}'), False) then RaiseException('Could not save the Firefox connection.');
end;

procedure RemoveOwnedNative(RootKey: Integer; Key, Manifest: String);
var Current: String;
begin
  if RegQueryStringValue(RootKey, Key, '', Current) and (CompareText(Current, ExpandConstant('{app}\Integration\') + Manifest) = 0) then begin
    RegDeleteValue(RootKey, Key, '');
    RegDeleteKeyIfEmpty(RootKey, Key);
  end;
end;

// Cleanup is retained for older installers that wrote per-user external-store keys.
procedure RemoveOwnedStore(RootKey: Integer; Key, UpdateUrl: String);
var Current, Owner: String;
begin
  if RegQueryStringValue(RootKey, Key, 'vydrop_owner', Owner) and (CompareText(Owner, ExpandConstant('{app}')) = 0) then begin
    if RegQueryStringValue(RootKey, Key, 'update_url', Current) and (Current = UpdateUrl) then RegDeleteValue(RootKey, Key, 'update_url');
    RegDeleteValue(RootKey, Key, 'vydrop_owner');
    RegDeleteKeyIfEmpty(RootKey, Key);
  end;
end;

procedure RemoveOwnedConnections(RootKey: Integer);
begin
  RemoveOwnedNative(RootKey, 'Software\Google\Chrome\NativeMessagingHosts\{#HostName}', '{#HostName}.chromium.json');
  RemoveOwnedNative(RootKey, 'Software\Microsoft\Edge\NativeMessagingHosts\{#HostName}', '{#HostName}.chromium.json');
  RemoveOwnedNative(RootKey, 'Software\Mozilla\NativeMessagingHosts\{#HostName}', '{#HostName}.firefox.json');
#if ChromeStoreId != ""
  RemoveOwnedStore(RootKey, 'Software\Google\Chrome\Extensions\{#ChromeStoreId}', 'https://clients2.google.com/service/update2/crx');
#endif
#if EdgeStoreId != ""
  RemoveOwnedStore(RootKey, 'Software\Microsoft\Edge\Extensions\{#EdgeStoreId}', 'https://edge.microsoft.com/extensionwebstorebase/v1/crx');
#endif
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then begin
    RemoveOwnedConnections(HKCU32);
    RemoveOwnedConnections(HKCU64);
  end;
end;
