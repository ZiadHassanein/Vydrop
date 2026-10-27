param([Parameter(Mandatory)][string]$BrowserDirectory)
$ErrorActionPreference='Stop'
$taskStoreRoot=[IO.Path]::GetFullPath($BrowserDirectory)
$taskChromiumManifest=Get-Content -LiteralPath (Join-Path $taskStoreRoot 'chromium/manifest.json') -Raw|ConvertFrom-Json
$taskFirefoxManifest=Get-Content -LiteralPath (Join-Path $taskStoreRoot 'firefox/manifest.json') -Raw|ConvertFrom-Json
if($taskChromiumManifest.name -ne 'Vydrop' -or $taskFirefoxManifest.name -ne 'Vydrop' -or $taskFirefoxManifest.browser_specific_settings.gecko.id -ne 'vifree@ziadhassanein.github.io'){throw 'Public store submissions require the stable Vydrop companion. Preview identities must not be published as stable.'}
foreach($taskStore in @('chrome','edge','firefox')) {
    $taskSource=Join-Path $taskStoreRoot $(if($taskStore -eq 'firefox') {'firefox'} else {'chromium'})
    $taskManifest=Get-Content -LiteralPath (Join-Path $taskSource 'manifest.json') -Raw | ConvertFrom-Json -AsHashtable
    # Store-assigned Chromium IDs must be added to the app allowlist before publication.
    $taskManifest.Remove('key') | Out-Null
    $taskArchive=Join-Path $taskStoreRoot ("Vydrop-$taskStore-unsigned.zip")
    $taskTemporary=$taskArchive+'.'+[Guid]::NewGuid().ToString('N')+'.tmp'
    try {
        $taskZip=[IO.Compression.ZipFile]::Open($taskTemporary,[IO.Compression.ZipArchiveMode]::Create)
        try {
            $taskWriter=[IO.StreamWriter]::new($taskZip.CreateEntry('manifest.json').Open(),[Text.UTF8Encoding]::new($false))
            try { $taskWriter.Write(($taskManifest | ConvertTo-Json -Depth 10)) } finally { $taskWriter.Dispose() }
            foreach($taskFile in Get-ChildItem -LiteralPath $taskSource -Recurse -File | Where-Object { $_.Name -notin @('manifest.json','companion-update.json') }) {
                $taskEntry=[IO.Path]::GetRelativePath($taskSource,$taskFile.FullName).Replace('\','/')
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($taskZip,$taskFile.FullName,$taskEntry,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
            }
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($taskZip,(Join-Path $taskStoreRoot 'LICENSE.txt'),'LICENSE.txt',[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        } finally { $taskZip.Dispose() }
        Move-Item -LiteralPath $taskTemporary -Destination $taskArchive -Force
    } finally { if(Test-Path -LiteralPath $taskTemporary) { Remove-Item -LiteralPath $taskTemporary } }
}
$taskReadiness=@{
    ready_for_publication=$false
    version=$taskManifest.version
    completed=@('Root-manifest archives for Chrome, Edge and Firefox','Optional network discovery permission','No cookie or account-data import','Firefox declares local URL transfer using built-in data consent','Source and license included in repository')
    required_before_publication=@('Publisher accounts and store registration','Record assigned Chromium/Edge IDs and add them alongside the existing ID in native-host origins and caller validation','Sign Firefox package through AMO using its retained add-on ID','Review permission/privacy declarations','Test actual signed/store-installed packages with native host and cold start')
    preserves_unpacked_identity=$true
    installer_registers_native_connection=$true
    unsigned_archives_are_installable_extensions=$false
    restart_only_install_supported=$false
    browser_approval_required=$true
    references=@('https://developer.chrome.com/docs/webstore/publish','https://extensionworkshop.com/documentation/publish/signing-and-distribution-overview/')
}
[IO.File]::WriteAllText((Join-Path $taskStoreRoot 'store-readiness.json'),($taskReadiness | ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
Write-Output 'Unsigned store archives prepared. Publication and identity verification remain pending.'
