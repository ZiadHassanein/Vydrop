param([Parameter(Mandatory)][string]$Executable,[Parameter(Mandatory)][string]$TestRoot)
$ErrorActionPreference='Stop'
$Executable=(Resolve-Path -LiteralPath $Executable).Path
if((Get-Item -LiteralPath $Executable).VersionInfo.ProductVersion -notmatch '-preview\.\d+') {throw 'Use a preview build so stable browser connections remain untouched.'}
$TestRoot=[IO.Path]::GetFullPath($TestRoot)
if(Test-Path -LiteralPath $TestRoot){throw 'Choose a fresh test directory.'}
$taskMutex=$null
try {if([Threading.Mutex]::TryOpenExisting('Local\ViFree.Preview.Desktop',[ref]$taskMutex)){throw 'An existing preview must not be interrupted.'}} finally {if($taskMutex){$taskMutex.Dispose()}}
$taskPaths=@('Software\Google\Chrome\NativeMessagingHosts\com.vifree.preview','Software\Microsoft\Edge\NativeMessagingHosts\com.vifree.preview','Software\Mozilla\NativeMessagingHosts\com.vifree.preview')
$taskSnapshots=@();$taskProcess=$null;$taskOriginalData=$env:VYDROP_PREVIEW_DATA_ROOT
New-Item -ItemType Directory -Path $TestRoot | Out-Null
$taskData=Join-Path $TestRoot 'data';$env:VYDROP_PREVIEW_DATA_ROOT=$taskData
foreach($taskView in @([Microsoft.Win32.RegistryView]::Registry32,[Microsoft.Win32.RegistryView]::Registry64)) {
  $taskUser=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$taskView)
  try {foreach($taskPath in $taskPaths){$taskKey=$taskUser.OpenSubKey($taskPath);try{$taskSnapshots+=@{view=$taskView;path=$taskPath;exists=($null -ne $taskKey);value=$(if($taskKey){$taskKey.GetValue('')}else{$null});kind=$(if($taskKey -and $null -ne $taskKey.GetValue('')){$taskKey.GetValueKind('')}else{$null})}}finally{if($taskKey){$taskKey.Dispose()}}}}finally{$taskUser.Dispose()}
}
function Assert-Task([bool]$Condition,[string]$Message){if(!$Condition){throw $Message};Write-Output ('PASS: '+$Message)}
function Close-TaskApp {
  if($taskProcess -and !$taskProcess.HasExited){$null=$taskProcess.CloseMainWindow();if(!$taskProcess.WaitForExit(10000)){throw 'The owned test app did not close normally.'}}
  if($taskProcess){$taskProcess.Dispose();$script:taskProcess=$null}
}
function Start-TaskApp([string]$Path) {
  $script:taskProcess=Start-Process -FilePath $Path -WindowStyle Hidden -PassThru
  for($taskIndex=0;$taskIndex -lt 200;$taskIndex++){$taskProcess.Refresh();if($taskProcess.HasExited){throw 'Test app exited before showing its window.'};if($taskProcess.MainWindowHandle -ne 0){return};Start-Sleep -Milliseconds 100}
  throw 'The owned test app did not show its window.'
}
function Check-TaskRegistration([string]$Path) {
  foreach($taskSnapshot in $taskSnapshots){
    $taskUser=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$taskSnapshot.view)
    try {$taskKey=$taskUser.OpenSubKey($taskSnapshot.path);try{
      $taskFirefox=$taskSnapshot.path.Contains('Mozilla');$taskManifest=Join-Path $taskData $(if($taskFirefox){'Browser/com.vifree.preview.firefox.json'}else{'Browser/com.vifree.preview.chromium.json'})
      Assert-Task ($taskKey -and $taskKey.GetValue('') -eq $taskManifest) 'Ordinary portable startup registers its own native manifest'
      $taskJson=Get-Content -LiteralPath $taskManifest -Raw|ConvertFrom-Json
      Assert-Task ($taskJson.path -eq $Path) 'The native manifest names the current portable app executable'
    }finally{if($taskKey){$taskKey.Dispose()}}}finally{$taskUser.Dispose()}
  }
}
try {
  Start-TaskApp $Executable;Check-TaskRegistration $Executable;Close-TaskApp
  $taskMovedDirectory=Join-Path $TestRoot 'moved';New-Item -ItemType Directory -Path $taskMovedDirectory|Out-Null
  $taskMoved=Join-Path $taskMovedDirectory 'Vydrop.exe';Copy-Item -LiteralPath $Executable -Destination $taskMoved
  Start-TaskApp $taskMoved;Check-TaskRegistration $taskMoved;Close-TaskApp
  $taskMarker=Join-Path $taskData 'Browser/automatic-connection.disabled';[IO.File]::WriteAllText($taskMarker,'disabled')
  foreach($taskSnapshot in $taskSnapshots){$taskUser=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$taskSnapshot.view);try{$taskKey=$taskUser.OpenSubKey($taskSnapshot.path,$true);try{if($taskKey){$taskValue=$taskKey.GetValue('');if($taskValue -and !$taskValue.StartsWith($taskData+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Refusing to change a registration outside this owned test profile.'};$taskKey.DeleteValue('',$false)}}finally{if($taskKey){$taskKey.Dispose()}}}finally{$taskUser.Dispose()}}
  Start-TaskApp $taskMoved
  foreach($taskSnapshot in $taskSnapshots){$taskUser=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$taskSnapshot.view);try{$taskKey=$taskUser.OpenSubKey($taskSnapshot.path);try{Assert-Task (!$taskKey -or $null -eq $taskKey.GetValue('')) 'An explicit disable marker prevents automatic registration on the next real launch'}finally{if($taskKey){$taskKey.Dispose()}}}finally{$taskUser.Dispose()}}
  Close-TaskApp
  [IO.File]::WriteAllText((Join-Path $TestRoot 'result.json'),'{"result":"PASS","firstLaunch":true,"movedPathRepair":true,"explicitDisablePreserved":true}')
} finally {
  try {Close-TaskApp} finally {
    $env:VYDROP_PREVIEW_DATA_ROOT=$taskOriginalData
    foreach($taskSnapshot in $taskSnapshots){$taskUser=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$taskSnapshot.view);try{$taskKey=$taskUser.CreateSubKey($taskSnapshot.path);try{if($null -ne $taskSnapshot.value){$taskKey.SetValue('',$taskSnapshot.value,$taskSnapshot.kind)}else{$taskKey.DeleteValue('',$false)}}finally{$taskKey.Dispose()};if(!$taskSnapshot.exists){$taskKey=$taskUser.OpenSubKey($taskSnapshot.path);try{$taskEmpty=$taskKey.ValueCount -eq 0 -and $taskKey.SubKeyCount -eq 0}finally{$taskKey.Dispose()};if($taskEmpty){$taskUser.DeleteSubKey($taskSnapshot.path,$false)}}}finally{$taskUser.Dispose()}}
  }
}
