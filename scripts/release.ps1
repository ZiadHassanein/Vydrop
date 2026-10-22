$ErrorActionPreference = 'Stop'
if ($env:GITHUB_EVENT_NAME -ne 'push' -or $env:GITHUB_REF_TYPE -ne 'tag' -or
    $env:GITHUB_REF_NAME -notmatch '^v\d+\.\d+\.\d+$' -or !$env:GH_RELEASE_TOKEN) {
    throw 'Release publishing requires an explicit version tag and the job token.'
}
$sourceRepository = 'ZiadHassanein/Vydrop'
$repository = $sourceRepository
if ($env:GITHUB_REPOSITORY -ne $sourceRepository) { throw 'Unexpected source repository.' }
& git merge-base --is-ancestor HEAD origin/main
if ($LASTEXITCODE -ne 0) { throw 'Only a commit already merged to main can be released.' }
$version = $env:GITHUB_REF_NAME.Substring(1)
$root = Split-Path -Parent $PSScriptRoot
$sumPath = Join-Path $root 'dist/checksums-desktop.json'
$sum = Get-Content -LiteralPath $sumPath -Raw | ConvertFrom-Json
$expectedName = "Vydrop-v$version-win-x64.zip"
if ($sum.File -ne $expectedName) { throw 'The artifact version does not match the tag.' }
$zip = Join-Path $root ('dist/' + $expectedName)
if ((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $sum.Hash) { throw 'The tested artifact checksum does not match.' }
$installerName = "VydropSetup-v$version-win-x64.exe"
$installer = Join-Path $root ('dist/installers/' + $installerName)
$installerProof = Get-Content -LiteralPath ($installer + '.json') -Raw | ConvertFrom-Json
if ($installerProof.file -ne $installerName -or $installerProof.version -ne $version -or $installerProof.channel -ne 'stable' -or
    (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash -ne $installerProof.sha256) {
    throw 'The installer does not match the tested stable release.'
}
$headers = @{Authorization = 'Bearer ' + $env:GH_RELEASE_TOKEN; Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28'}
$destination = Invoke-RestMethod -Uri "https://api.github.com/repos/$repository" -Headers $headers
if ($destination.private -or $destination.full_name -ne $repository -or $destination.default_branch -ne 'main') {
    throw 'Releases must use the main public Vydrop repository.'
}
$api = "https://api.github.com/repos/$repository/releases"
$url = "https://github.com/$repository/releases/download/$($env:GITHUB_REF_NAME)/$expectedName"
$installerUrl = "https://github.com/$repository/releases/download/$($env:GITHUB_REF_NAME)/$installerName"
$changelog = Get-Content -LiteralPath (Join-Path $root 'CHANGELOG.md') -Raw
$releaseNotes = [regex]::Match($changelog, "(?ms)^## \[$([regex]::Escape($version))\][^\r\n]*\r?\n(?<notes>.*?)(?=^## \[|\z)").Groups['notes'].Value.Trim()
if (!$releaseNotes) { throw 'The tagged version needs a changelog section before publication.' }
$body = @"
Vydrop is a free Windows video and audio downloader for YouTube and 1,000+ public sites supported by yt-dlp, with English and Arabic interfaces.

**[Download for Windows — Installer]($installerUrl)** · **[Portable ZIP]($url)**

Run the installer and open **Vydrop** from Start. Alternatively, extract the complete portable ZIP and open **Vydrop.exe**. Windows 10/11 x64; .NET is bundled and missing media tools are prepared automatically.

The installer registers the browser connection for your account. Browser-store listings are still pending: use **Settings → Browser integration → Manual setup and troubleshooting → Load the local companion**, finish the browser loading step, then use **Test connection**. Browser approval is required.

## Changes in $version

$releaseNotes

[Documentation](https://ziadhassanein.github.io/Vydrop/) · [العربية](https://ziadhassanein.github.io/Vydrop/ar/) · [Illustrated Chrome, Edge and Firefox setup](https://github.com/$repository/blob/$($env:GITHUB_REF_NAME)/docs/browser-extension-setup.md)

### Downloads and media support

- MP4, MKV and WebM video where inspected streams are compatible.
- MP3, M4A, Opus, WAV and FLAC audio with source-aware conversion choices.
- Public media from YouTube, TikTok, Facebook, Vimeo and other yt-dlp-supported sites, plus compatible direct media links. Private, sign-in-only and DRM content is unsupported.
- Queue, playlist selection and available SRT/VTT subtitles.
- English/Arabic and light/dark themes.
- Browser companion 1.5.0 inside Browser: in-player format/quality choices, confirmed download starts, closed-app launching, connection testing and optional media discovery. Earlier companion/native-host identities and version-1 fallback remain compatible.
- Automatic daily verified yt-dlp/Deno checks and one compatible update-and-retry.
- Settings keeps video errors in the download screen; manual app updates use this repository.

**Upgrading:** close the old app, then run the installer. For a portable copy, extract the complete new ZIP into a permanent writable folder. Stable settings, queue, history and verified media tools retain their compatible storage location; preview state stays separate. If a portable app moved, reconnect it from Browser integration and reload/load its Browser companion. Versions through 4.2.1 require one manual desktop download after the repository rename.

The installer and app are not Authenticode-signed. GitHub's asset digests and these SHA-256 values identify the published files:

- Installer: $($installerProof.sha256.ToLowerInvariant())
- Portable ZIP: $($sum.Hash.ToLowerInvariant())

By **Ziad Hassanein**. Only save content you have permission to download.
"@
$payload = @{tag_name = $env:GITHUB_REF_NAME; target_commitish = 'main'; name = "Vydrop $($env:GITHUB_REF_NAME)"; body = $body; draft = $true; prerelease = $false} | ConvertTo-Json
$release = Invoke-RestMethod -Uri $api -Method Post -Headers $headers -ContentType 'application/json' -Body $payload
$downloads = @(
    @{Name=$expectedName; Path=$zip; Hash=$sum.Hash; ContentType='application/zip'},
    @{Name=$installerName; Path=$installer; Hash=$installerProof.sha256; ContentType='application/octet-stream'}
)
foreach ($download in $downloads) {
    $name = [Uri]::EscapeDataString($download.Name)
    $upload = "https://uploads.github.com/repos/$repository/releases/$($release.id)/assets?name=$name"
    $asset = Invoke-RestMethod -Uri $upload -Method Post -Headers $headers -ContentType $download.ContentType -InFile $download.Path
    if ($asset.state -ne 'uploaded') { throw 'An asset did not finish uploading. The release remains a draft.' }
    for($attempt=0; $attempt -lt 10 -and !$asset.digest; $attempt++) {
        Start-Sleep -Milliseconds 500
        $asset=Invoke-RestMethod -Uri "https://api.github.com/repos/$repository/releases/assets/$($asset.id)" -Headers $headers
    }
    if ($asset.size -ne (Get-Item -LiteralPath $download.Path).Length -or $asset.digest -ne ('sha256:'+$download.Hash.ToLowerInvariant())) {
        throw 'An uploaded asset digest did not match. The release remains a draft.'
    }
}
$releaseAssets=Invoke-RestMethod -Uri "$api/$($release.id)/assets" -Headers $headers
if (@($releaseAssets).Count -ne 2 -or ((@($releaseAssets.name | Sort-Object) -join '|') -ne (@($downloads.Name | Sort-Object) -join '|'))) {
    throw 'The release must contain exactly the verified installer and portable ZIP.'
}
$published = Invoke-RestMethod -Uri "$api/$($release.id)" -Method Patch -Headers $headers -ContentType 'application/json' -Body '{"draft":false,"make_latest":"true"}'
Write-Output "Published $($published.html_url)"
