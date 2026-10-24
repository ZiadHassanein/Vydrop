param([Parameter(Mandatory)][string]$Installer,[Parameter(Mandatory)][string]$TestDirectory,[string]$PlaywrightPath)
$ErrorActionPreference='Stop'
$Installer=(Resolve-Path -LiteralPath $Installer).Path
$TestDirectory=[IO.Path]::GetFullPath($TestDirectory)
if([IO.Path]::GetFileName($Installer) -notmatch '^VydropSetup-v[\d.]+-preview\.\d+-win-x64\.exe$'){throw 'Only preview installers are allowed in this lifecycle test.'}
if(Test-Path -LiteralPath $TestDirectory){throw 'Choose a fresh test directory; existing files are never replaced by this test.'}
function Test-TaskPreviewRunning {
    $taskMutex=$null
    try {return [Threading.Mutex]::TryOpenExisting('Local\ViFree.Preview.Desktop',[ref]$taskMutex)}
    finally {if($taskMutex){$taskMutex.Dispose()}}
}
if(Test-TaskPreviewRunning){throw 'Close the preview before testing; existing preview instances are never stopped.'}
$taskUninstall='Software\Microsoft\Windows\CurrentVersion\Uninstall\Vydrop.Preview.Desktop_is1'
$taskData=$TestDirectory+'.user-data'
if(Test-Path -LiteralPath $taskData){throw 'Choose a fresh test directory with no existing test profile.'}
foreach($taskName in @('queue.json','queue.json.backup')){if(Test-Path -LiteralPath (Join-Path $taskData $taskName)){throw 'An existing preview queue must not be disturbed.'}}
$taskPaths=@('Software\Google\Chrome\NativeMessagingHosts\com.vifree.preview','Software\Microsoft\Edge\NativeMessagingHosts\com.vifree.preview','Software\Mozilla\NativeMessagingHosts\com.vifree.preview')
$taskSnapshots=@(); $taskSettings=@{}; $taskHost=$null; $taskUi=$null
$taskUrl='https://example.com/vydrop-installer-fixture-'+[Guid]::NewGuid().ToString('N')+'.mp4'
$taskReport=[ordered]@{}
function Assert-Task([bool]$Value,[string]$Message){if(!$Value){throw $Message}; Write-Output ('PASS: '+$Message)}
function Run-TaskSetup {
    $taskProc=Start-Process -FilePath $Installer -ArgumentList @('/VERYSILENT','/SP-','/NORESTART','/SUPPRESSMSGBOXES',('/DIR="'+$TestDirectory+'"'),('/LOG="'+$TestDirectory+'.install.log"')) -WindowStyle Hidden -Wait -PassThru
    return $taskProc.ExitCode
}
function Read-TaskFrame([IO.Stream]$Stream,[Threading.CancellationToken]$Token) {
    $taskHeader=[byte[]]::new(4); $taskOffset=0
    while($taskOffset -lt 4){$taskCount=$Stream.ReadAsync($taskHeader,$taskOffset,4-$taskOffset,$Token).GetAwaiter().GetResult();if(!$taskCount){throw 'No native reply.'};$taskOffset+=$taskCount}
    $taskLength=[BitConverter]::ToUInt32($taskHeader,0); if(!$taskLength -or $taskLength -gt 65536){throw 'Invalid native frame.'}
    $taskBody=[byte[]]::new($taskLength);$taskOffset=0
    while($taskOffset -lt $taskBody.Length){$taskCount=$Stream.ReadAsync($taskBody,$taskOffset,$taskBody.Length-$taskOffset,$Token).GetAwaiter().GetResult();if(!$taskCount){throw 'Truncated native reply.'};$taskOffset+=$taskCount}
    return [Text.Encoding]::UTF8.GetString($taskBody)|ConvertFrom-Json
}
foreach($taskView in @([Microsoft.Win32.RegistryView]::Registry32,[Microsoft.Win32.RegistryView]::Registry64)) {
    $taskUser=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$taskView)
    try {
        $taskPrior=$taskUser.OpenSubKey($taskUninstall)
        if($taskPrior){$taskPrior.Dispose();throw 'An existing preview installer must not be replaced by the test.'}
        foreach($taskPath in $taskPaths){$taskKey=$taskUser.OpenSubKey($taskPath);try {$taskSnapshots+=@{view=$taskView;path=$taskPath;exists=($null -ne $taskKey);value=$(if($taskKey){$taskKey.GetValue('')}else{$null});kind=$(if($taskKey -and $null -ne $taskKey.GetValue('')){$taskKey.GetValueKind('')}else{$null})}}finally{if($taskKey){$taskKey.Dispose()}}}
    } finally {$taskUser.Dispose()}
}
foreach($taskName in @('settings.json','settings.json.backup','Browser/verified-connection.json')){$taskFile=Join-Path $taskData $taskName;$taskSettings[$taskName]=if(Test-Path -LiteralPath $taskFile){[IO.File]::ReadAllBytes($taskFile)}else{$null}}
$taskOriginalPreviewRoot=$env:VYDROP_PREVIEW_DATA_ROOT
$env:VYDROP_PREVIEW_DATA_ROOT=$taskData
New-Item -ItemType Directory -Path $TestDirectory|Out-Null
$taskExe=Join-Path $TestDirectory 'Vydrop.exe'; $taskUninstaller=Join-Path $TestDirectory 'unins000.exe'
try {
    Assert-Task ((Run-TaskSetup) -eq 0) 'Preview installer completes without administrator privileges'
    Assert-Task ((Test-Path -LiteralPath $taskExe) -and (Test-Path -LiteralPath $taskUninstaller)) 'App and Windows uninstaller are installed'
    $taskMarker=Get-Content -LiteralPath (Join-Path $TestDirectory 'installation.json') -Raw|ConvertFrom-Json
    Assert-Task ($taskMarker.kind -eq 'installer' -and $taskMarker.channel -eq 'preview') 'Installed channel marker is correct'
    $taskExpected=@((Join-Path $TestDirectory 'Integration/com.vifree.preview.chromium.json'),(Join-Path $TestDirectory 'Integration/com.vifree.preview.chromium.json'),(Join-Path $TestDirectory 'Integration/com.vifree.preview.firefox.json'))
    foreach($taskSnapshot in $taskSnapshots){$taskUser=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$taskSnapshot.view);try{$taskKey=$taskUser.OpenSubKey($taskSnapshot.path);try{Assert-Task ($taskKey.GetValue('') -eq $taskExpected[[array]::IndexOf($taskPaths,$taskSnapshot.path)]) ('Native connection registered in '+$taskSnapshot.view+': '+$taskSnapshot.path)}finally{$taskKey.Dispose()}}finally{$taskUser.Dispose()}}
    $taskChromium=Get-Content -LiteralPath $taskExpected[0] -Raw|ConvertFrom-Json; $taskFirefox=Get-Content -LiteralPath $taskExpected[2] -Raw|ConvertFrom-Json
    Assert-Task ($taskChromium.path -eq $taskExe -and $taskChromium.allowed_origins -contains 'chrome-extension://cpoiefoeogidkjcgbacikpcdjkjeekcf/' -and $taskFirefox.path -eq $taskExe -and $taskFirefox.allowed_extensions -contains 'vifree-preview@ziadhassanein.github.io') 'Generated manifests preserve identities and use the actual installed executable'
    Assert-Task (!(Test-TaskPreviewRunning)) 'Silent installation does not launch the preview or interfere with the stable app'
    $taskLegacyMutex=[Threading.Mutex]::new($false,'Local\ViFree.Preview.Desktop')
    try {Assert-Task ((Run-TaskSetup) -ne 0) 'An older app without graceful-close support receives a retry message before replacement'}
    finally {$taskLegacyMutex.Dispose()}
    $taskSentinel=Join-Path $TestDirectory 'unrelated-user-file.txt';[IO.File]::WriteAllText($taskSentinel,'retain me')
    Assert-Task ((Run-TaskSetup) -eq 0 -and [IO.File]::ReadAllText($taskSentinel) -eq 'retain me') 'In-place upgrade preserves unrelated files'
    $taskReport.install=$true;$taskReport.upgrade=$true;$taskReport.nativeRegistrations=6

    $taskStart=[Diagnostics.ProcessStartInfo]::new($taskExe);$taskStart.UseShellExecute=$false;$taskStart.CreateNoWindow=$true
    $taskStart.RedirectStandardInput=$true;$taskStart.RedirectStandardOutput=$true;$taskStart.RedirectStandardError=$true
    $taskStart.ArgumentList.Add('chrome-extension://cpoiefoeogidkjcgbacikpcdjkjeekcf/');$taskStart.ArgumentList.Add('--parent-window=0')
    $taskHost=[Diagnostics.Process]::Start($taskStart)
    $taskId='installer_'+[Guid]::NewGuid().ToString('N')
    $taskBytes=[Text.Encoding]::UTF8.GetBytes((@{version=2;type='enqueue';requestId=$taskId;url=$taskUrl;sourcePage='https://example.com/owned-test-page';mediaId='owned-test-asset'}|ConvertTo-Json -Compress))
    $taskHost.StandardInput.BaseStream.Write([BitConverter]::GetBytes([uint32]$taskBytes.Length));$taskHost.StandardInput.BaseStream.Write($taskBytes);$taskHost.StandardInput.BaseStream.Flush()
    $taskDeadline=[Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(20))
    try{$taskReply=Read-TaskFrame $taskHost.StandardOutput.BaseStream $taskDeadline.Token}finally{$taskDeadline.Dispose()}
    Assert-Task ($taskReply.ok -and $taskReply.code -eq 'queued' -and $taskReply.requestId -eq $taskId) ('Installed native host cold-starts Vydrop and acknowledges the selected link: '+($taskReply|ConvertTo-Json -Compress))
    for($taskWait=0;$taskWait -lt 60 -and !$taskUi;$taskWait++){$taskUi=Get-Process -Name Vydrop -ErrorAction SilentlyContinue|Where-Object {$_.Id -ne $taskHost.Id -and $_.Path -eq $taskExe -and $_.MainWindowHandle -ne 0}|Select-Object -First 1;if(!$taskUi){Start-Sleep -Milliseconds 100}}
    Assert-Task ($null -ne $taskUi) 'Actual installed desktop window appears after cold start'
    $taskQueue=Get-Content -LiteralPath (Join-Path $taskData 'queue.json') -Raw|ConvertFrom-Json
    Assert-Task ($taskQueue.Jobs.Count -eq 1 -and $taskQueue.Jobs[0].Options.Url -eq $taskUrl -and $taskQueue.Jobs[0].State -eq 0) 'Cold-start link is durably pending and does not download automatically'
    $taskHost.StandardInput.Close();Assert-Task ($taskHost.WaitForExit(5000)) 'Native host exits cleanly';$taskHost.Dispose();$taskHost=$null
    Assert-Task ((Run-TaskSetup) -eq 0 -and $taskUi.WaitForExit(10000)) 'Upgrade gracefully closes the running preview and completes without forcing termination';$taskUi=$null
    $taskRetained=Get-Content -LiteralPath (Join-Path $taskData 'queue.json') -Raw|ConvertFrom-Json
    Assert-Task ($taskRetained.Jobs.Count -eq 1 -and $taskRetained.Jobs[0].Options.Url -eq $taskUrl) 'A running-app upgrade preserves its persisted queue'
    $taskReport.coldStart=$true;$taskReport.pendingWithoutDownload=$true;$taskReport.runningAppGuard=$true;$taskReport.gracefulUpgrade=$true
    if($PlaywrightPath) {
        $taskOriginalPlaywright=$env:VYDROP_PLAYWRIGHT
        try {
            $env:VYDROP_PLAYWRIGHT=$PlaywrightPath
            & node (Join-Path $PSScriptRoot '../tests/browser-installed-native.cjs') $TestDirectory $taskUrl ($TestDirectory+'.browser.json')
            Assert-Task ($LASTEXITCODE -eq 0) 'A real isolated Edge companion connects through the installed native registration with Vydrop closed'
        } finally {$env:VYDROP_PLAYWRIGHT=$taskOriginalPlaywright}
        $taskUi=Get-Process -Name Vydrop -ErrorAction SilentlyContinue|Where-Object {$_.Path -eq $taskExe -and $_.MainWindowHandle -ne 0}|Select-Object -First 1
        Assert-Task ($null -ne $taskUi) 'Browser-launched installed desktop is running'
        $null=$taskUi.CloseMainWindow();Assert-Task ($taskUi.WaitForExit(10000)) 'Browser-launched test app closes cleanly';$taskUi=$null
        $taskReport.realBrowserColdStart=$true
    }

    # Simulate a user reconnecting Firefox to a different app copy before uninstalling this one.
    $taskOther=Join-Path $TestDirectory 'another-copy-manifest.json';[IO.File]::WriteAllText($taskOther,'{}')
    foreach($taskView in @([Microsoft.Win32.RegistryView]::Registry32,[Microsoft.Win32.RegistryView]::Registry64)){$taskUser=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$taskView);try{$taskKey=$taskUser.CreateSubKey($taskPaths[2]);try{$taskKey.SetValue('',$taskOther)}finally{$taskKey.Dispose()}}finally{$taskUser.Dispose()}}
    $taskProc=Start-Process -FilePath $taskUninstaller -ArgumentList @('/VERYSILENT','/NORESTART','/SUPPRESSMSGBOXES',('/LOG="'+$TestDirectory+'.uninstall.log"')) -WindowStyle Hidden -Wait -PassThru
    Assert-Task ($taskProc.ExitCode -eq 0 -and !(Test-Path -LiteralPath $taskExe)) 'Windows uninstall removes the installed app'
    foreach($taskSnapshot in $taskSnapshots){$taskUser=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$taskSnapshot.view);try{$taskKey=$taskUser.OpenSubKey($taskSnapshot.path);try{$taskValue=if($taskKey){$taskKey.GetValue('')}else{$null};Assert-Task ($(if($taskSnapshot.path -eq $taskPaths[2]){$taskValue -eq $taskOther}else{$null -eq $taskValue})) 'Uninstall removes owned registrations and preserves a redirected connection'}finally{if($taskKey){$taskKey.Dispose()}}}finally{$taskUser.Dispose()}}
    Assert-Task ([IO.File]::ReadAllText($taskSentinel) -eq 'retain me' -and (Test-Path -LiteralPath (Join-Path $taskData 'queue.json'))) 'Uninstall preserves unrelated files and local user queue data'
    $taskReport.uninstall=$true;$taskReport.ownershipCleanup=$true;$taskReport.userDataPreserved=$true;$taskReport.result='PASS'
    [IO.File]::WriteAllText(($TestDirectory+'.verification.json'),($taskReport|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
} finally {
    $env:VYDROP_PREVIEW_DATA_ROOT=$taskOriginalPreviewRoot
    if($taskHost){$taskHost.StandardInput.Close();if(!$taskHost.WaitForExit(3000)){$taskHost.Kill()};$taskHost.Dispose()}
    if(!$taskUi){$taskUi=Get-Process -Name Vydrop -ErrorAction SilentlyContinue|Where-Object {$_.Path -eq $taskExe -and $_.MainWindowHandle -ne 0}|Select-Object -First 1}
    if($taskUi){$null=$taskUi.CloseMainWindow();if(!$taskUi.WaitForExit(10000)){throw 'The test app is still open; preserve its state.'}}
    foreach($taskName in @('queue.json','queue.json.backup')){$taskFile=Join-Path $taskData $taskName;if(Test-Path -LiteralPath $taskFile){$taskSaved=Get-Content -LiteralPath $taskFile -Raw|ConvertFrom-Json;if(@($taskSaved.Jobs|Where-Object {$_.Options.Url -ne $taskUrl}).Count -gt 0){throw 'Other jobs appeared; preserve them.'};Remove-Item -LiteralPath $taskFile}}
    foreach($taskName in $taskSettings.Keys){$taskFile=Join-Path $taskData $taskName;if($null -ne $taskSettings[$taskName]){[IO.File]::WriteAllBytes($taskFile,[byte[]]$taskSettings[$taskName])}elseif(Test-Path -LiteralPath $taskFile){Remove-Item -LiteralPath $taskFile}}
    foreach($taskSnapshot in $taskSnapshots){$taskUser=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$taskSnapshot.view);try{$taskKey=$taskUser.CreateSubKey($taskSnapshot.path);try{if($null -ne $taskSnapshot.value){$taskKey.SetValue('',$taskSnapshot.value,$taskSnapshot.kind)}else{$taskKey.DeleteValue('',$false)}}finally{$taskKey.Dispose()};if(!$taskSnapshot.exists){$taskKey=$taskUser.OpenSubKey($taskSnapshot.path);try{$taskEmpty=$taskKey.ValueCount -eq 0 -and $taskKey.SubKeyCount -eq 0}finally{$taskKey.Dispose()};if($taskEmpty){$taskUser.DeleteSubKey($taskSnapshot.path,$false)}}}finally{$taskUser.Dispose()}}
}
