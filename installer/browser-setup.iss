// Part of the normal installer. No browser profile changes or forced-install policies.
var
  BrowserPage: TInputOptionWizardPage;
  BrowserIds: array of String;
  BrowserCount, LaterIndex: Integer;

function BrowserInstalled(Executable, RelativePath: String): Boolean;
var Path: String;
begin
  Result := False;
  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\App Paths\' + Executable, '', Path) or
     RegQueryStringValue(HKLM64, 'Software\Microsoft\Windows\CurrentVersion\App Paths\' + Executable, '', Path) or
     RegQueryStringValue(HKLM32, 'Software\Microsoft\Windows\CurrentVersion\App Paths\' + Executable, '', Path) then begin
    StringChangeEx(Path, '"', '', True);
    Result := FileExists(Path);
  end;
  Result := Result or FileExists(ExpandConstant('{localappdata}\') + RelativePath) or
    FileExists(ExpandConstant('{pf64}\') + RelativePath) or
    FileExists(ExpandConstant('{pf32}\') + RelativePath);
end;

procedure AddBrowser(Name, Executable, RelativePath: String);
begin
  if not BrowserInstalled(Executable, RelativePath) then Exit;
  BrowserPage.Add(Name);
  BrowserIds[BrowserCount] := Name;
  BrowserCount := BrowserCount + 1;
end;

procedure InitializeWizard;
var DefaultId, Preferred: String; Index: Integer;
begin
  BrowserPage := CreateInputOptionPage(wpSelectTasks, CustomMessage('BrowserSetupTitle'),
    CustomMessage('BrowserSetupDescription'), CustomMessage('BrowserSetupHint'), True, False);
  SetArrayLength(BrowserIds, 3);
  AddBrowser('Chrome', 'chrome.exe', 'Google\Chrome\Application\chrome.exe');
  AddBrowser('Edge', 'msedge.exe', 'Microsoft\Edge\Application\msedge.exe');
  AddBrowser('Firefox', 'firefox.exe', 'Mozilla Firefox\firefox.exe');
  LaterIndex := BrowserCount;
  BrowserPage.Add(CustomMessage('BrowserSetupLater'));
  BrowserPage.SelectedValueIndex := LaterIndex;
  // Returning users keep their working integration unless they explicitly choose to revisit it.
  if RegKeyExists(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#AppId}_is1') then Exit;
  Preferred := '';
  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice', 'ProgId', DefaultId) then begin
    if Pos('Chrome', DefaultId) > 0 then Preferred := 'Chrome'
    else if Pos('MSEdge', DefaultId) > 0 then Preferred := 'Edge'
    else if Pos('Firefox', DefaultId) > 0 then Preferred := 'Firefox';
  end;
  if BrowserCount > 0 then BrowserPage.SelectedValueIndex := 0;
  for Index := 0 to BrowserCount - 1 do
    if BrowserIds[Index] = Preferred then BrowserPage.SelectedValueIndex := Index;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := (PageID = BrowserPage.ID) and (BrowserCount = 0);
end;

function BrowserLaunchParameters(Param: String): String;
var Index: Integer;
begin
  Result := '';
  Index := BrowserPage.SelectedValueIndex;
  if (Index >= 0) and (Index < BrowserCount) then
    Result := '--browser-setup ' + BrowserIds[Index];
end;
