// Ask this channel's app to use its normal close path, which persists the queue
// and stops workers before its executable can be replaced. Never terminate it.
function OpenEvent(Access: LongWord; Inherit: Boolean; Name: String): LongWord;
  external 'OpenEventW@kernel32.dll stdcall';
function SetEvent(Handle: LongWord): Boolean;
  external 'SetEvent@kernel32.dll stdcall';
function CloseHandle(Handle: LongWord): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';

function PrepareToInstall(var NeedsRestart: Boolean): String;
var Handle: LongWord; Attempt: Integer;
begin
  Result := '';
  if not CheckForMutexes('{#AppMutex}') then Exit;
  Handle := OpenEvent(2, False, '{#AppMutex}.InstallClose');
  if Handle <> 0 then begin
    try
      SetEvent(Handle);
      for Attempt := 1 to 100 do begin
        if not CheckForMutexes('{#AppMutex}') then Exit;
        Sleep(100);
      end;
    finally
      CloseHandle(Handle);
    end;
  end;
  // Older versions do not expose the graceful-close event. Do not force them closed.
  Result := CustomMessage('CloseVydrop');
end;

function InitializeUninstall: Boolean;
begin
  Result := not CheckForMutexes('{#AppMutex}');
  if not Result then MsgBox(CustomMessage('CloseVydrop'), mbInformation, MB_OK);
end;
