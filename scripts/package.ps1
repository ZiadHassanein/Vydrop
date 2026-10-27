param([string]$Version='4.4.10', [switch]$Preview, [string]$CertificateThumbprint, [uri]$TimestampServer)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $projectRoot 'build/dotnet-home'
$env:NUGET_PACKAGES=Join-Path $projectRoot 'build/packages'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
if($Version -notmatch '^\d+\.\d+\.\d+(?:-preview\.\d+)?$' -or ($Version.Contains('-preview') -ne $Preview.IsPresent)) { throw 'Use a three-part stable version, or a preview version with -Preview.' }
$assemblyVersion=[regex]::Match($Version,'^\d+\.\d+\.\d+').Value + '.0'
$package=Join-Path $projectRoot "dist/Vydrop-$Version-win-x64"
New-Item -ItemType Directory -Path $package -Force | Out-Null
& dotnet publish (Join-Path $projectRoot 'src/Downloader.Desktop/Downloader.Desktop.csproj') -c Release -r win-x64 --self-contained true --disable-build-servers -m:1 -p:ViFreePreview=$($Preview.IsPresent.ToString().ToLowerInvariant()) -p:Version=$Version -p:AssemblyVersion=$assemblyVersion -p:FileVersion=$assemblyVersion -o $package
if($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'THIRD-PARTY-NOTICES.txt') -Destination $package -Force
$instructions=@'
Vydrop by Ziad Hassanein
Windows 10/11 x64. Extract this ZIP into a writable folder and open Vydrop.exe.
No separate .NET runtime, WinGet or tool setup script is needed.
On the first pasted link, the app checks compatible installed media tools and privately prepares only missing tools. Internet access and approximately 400 MB of free space may be required. Progress appears in the app. Global installations are never modified.
Downloader and Deno updates are checked automatically once daily when starting a download. Verified updates retain the previous version. Compatible extractor/runtime failures receive one automatic update/retry; access blocks are not retried. No account, API key or paid service is needed.
App updates: open Settings, choose Check for updates, then Install. The verified ZIP updates the app and both browser extension folders together and restarts Vydrop. Settings, queue, history and downloaded media are retained. Reload the browser extension after updating. Firefox temporary add-ons must be loaded again after a browser restart. Copies of ViFree or YouTube Downloader require one manual ZIP download to switch to Vydrop. Existing preferences and downloaded files are retained.
Preferences and queue jobs are saved locally in LocalAppData/YouTubeDownloader. Queue links and their source pages remain in queue.json and its recovery backup until removed/replaced. Interrupted jobs wait for your action; scheduled jobs require Vydrop to remain open. Existing settings and media are retained.
If upgrading from v3.x or a preview, close the older app and extract this ZIP into a new writable folder once. Future v4 updates are available through Settings > Check for updates.
The Browser folder contains Chrome/Edge and Firefox companions. Ordinary app use needs no extension. Read Browser/README.md to enable it; browser installation is an optional user action.
Only download content you have permission to save. Protected and sign-in-only content is not supported. YouTube limits can still occur.
Downloads and support: https://github.com/ZiadHassanein/Vydrop
'@
if ($Preview) {
    $instructions=@'
Vydrop Windows Preview by Ziad Hassanein
Extract this ZIP into a writable folder and open Vydrop.exe. Windows x64 only.
The .NET runtime is bundled. Compatible media tools are detected; missing tools are downloaded and verified automatically on the first link. Internet access is needed, with up to approximately 400 MB for private media tools. No separate tool setup is required.
Downloader and Deno updates are checked automatically once daily when starting a download, with verified staging, retained previous versions and one update/retry for compatible extractor/runtime failures. No account, API key or paid service is required. Access blocks and sign-in requirements are not retried.
This preview has separate preferences, queue and tool/resume state in LocalAppData/ViFreePreview. It does not replace the stable app or install stable updates. Queue links are saved locally, including their source URLs and recovery backup. Pause/Resume, scheduling and optional download limits are available. Scheduled jobs require this app to remain open.
Output options exposes MP3/M4A/Opus/WAV/FLAC and subtitles. Queue supports multiple links and per-job recovery. Choose videos selects playlist entries. Other websites depend on yt-dlp and public access; DRM and sign-in media are unsupported.
The optional browser companion is included in Browser. Ordinary app use needs no browser extension.
Test paste/quality selection, video/audio output, selected playlists, queue/retry/cancel, themes/languages, and folder access. Report issues before merging or publishing stable.
Clean Windows 10/11 VM compatibility has not been verified.
'@
}
$instructions += "`r`n`r`n" + [IO.File]::ReadAllText((Join-Path $projectRoot 'LICENSE'))
[IO.File]::WriteAllText((Join-Path $package 'README.txt'),$instructions,[Text.UTF8Encoding]::new($false))
$files=@('Vydrop.exe','README.txt','THIRD-PARTY-NOTICES.txt')
if($CertificateThumbprint) { & (Join-Path $PSScriptRoot 'sign-desktop.ps1') -PackageDirectory $package -CertificateThumbprint $CertificateThumbprint -TimestampServer $TimestampServer }
& (Join-Path $PSScriptRoot 'package-browser.ps1') -OutputDirectory (Join-Path $package 'Browser') -Preview:$Preview -NoArchive | Out-Null
$manifest=@{Version=$Version;Architecture='win-x64';BrowserStoresHash=(Get-FileHash -LiteralPath (Join-Path $projectRoot 'browser/store-listings.json') -Algorithm SHA256).Hash;Files=@{};BrowserFiles=@{}}
foreach($name in $files) { $manifest.Files[$name]=(Get-FileHash -LiteralPath (Join-Path $package $name) -Algorithm SHA256).Hash }
foreach($entry in Get-ChildItem -LiteralPath (Join-Path $package 'Browser') -File -Recurse) {
    $name=[IO.Path]::GetRelativePath($package,$entry.FullName).Replace('\','/')
    $manifest.BrowserFiles[$name]=(Get-FileHash -LiteralPath $entry.FullName -Algorithm SHA256).Hash
}
[IO.File]::WriteAllText((Join-Path $package 'package.json'),($manifest | ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
$zip=Join-Path $projectRoot "dist/Vydrop-v$Version-win-x64.zip"
$zipTemporary=$zip+'.'+[Guid]::NewGuid().ToString('N')+'.tmp'
try {
    $archive=[IO.Compression.ZipFile]::Open($zipTemporary,[IO.Compression.ZipArchiveMode]::Create)
    try {
        $packageEntries=@(($files+@('package.json')) | ForEach-Object { Get-Item -LiteralPath (Join-Path $package $_) }) + @(Get-ChildItem -LiteralPath (Join-Path $package 'Browser') -File -Recurse)
        foreach($entry in $packageEntries) {
            $entryName=[IO.Path]::GetRelativePath($package,$entry.FullName).Replace('\','/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$entry.FullName,$entryName,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally {$archive.Dispose()}
    Move-Item -LiteralPath $zipTemporary -Destination $zip -Force
} finally {if(Test-Path -LiteralPath $zipTemporary) {Remove-Item -LiteralPath $zipTemporary}}
$sum=@{File=(Split-Path -Leaf $zip);Hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash}
[IO.File]::WriteAllText((Join-Path $projectRoot 'dist/checksums-desktop.json'),($sum | ConvertTo-Json),[Text.UTF8Encoding]::new($false))
Write-Output "Packaged $zip"
