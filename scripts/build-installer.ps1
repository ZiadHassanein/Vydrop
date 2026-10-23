param([string]$Version='4.4.1',[switch]$Preview,[string]$CompilerPath,[switch]$UseExistingPackage,[string]$OutputDirectory,[switch]$RequireStoreListings)
$ErrorActionPreference='Stop'
$taskRepo=Split-Path -Parent $PSScriptRoot
if($Version -notmatch '^\d+\.\d+\.\d+(?:-preview\.\d+)?$' -or $Version.Contains('-preview.') -ne $Preview.IsPresent) {throw 'Choose a matching stable or preview version/channel.'}
$taskCatalog=Get-Content -LiteralPath (Join-Path $taskRepo 'browser/store-listings.json') -Raw|ConvertFrom-Json
if($taskCatalog.format -ne 1){throw 'Unsupported store catalog.'}
foreach($taskId in @($taskCatalog.chromeId,$taskCatalog.edgeId)){if($null -eq $taskId -or ($taskId -ne '' -and $taskId -cnotmatch '^[a-p]{32}$')){throw 'Use the assigned Chrome/Edge store IDs.'}}
if($taskCatalog.firefoxListingUrl -ne '' -and $taskCatalog.firefoxListingUrl -cnotmatch '^https://addons\.mozilla\.org/(?:[A-Za-z-]+/)?firefox/addon/[a-z0-9-]+/$'){throw 'Use the official Firefox listing URL.'}
if($RequireStoreListings -and ($Preview -or $taskCatalog.chromeId -eq '' -or $taskCatalog.edgeId -eq '' -or $taskCatalog.firefoxListingUrl -eq '')){throw 'Store-based extension setup requires a stable build and real Chrome, Edge and Firefox listings. Register publisher accounts, submit the companions, and fill browser/store-listings.json first.'}
if(!$OutputDirectory){$OutputDirectory=Join-Path $taskRepo 'dist/installers'}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
$taskPackage=Join-Path $taskRepo "dist/Vydrop-$Version-win-x64"
if(!$UseExistingPackage){& (Join-Path $PSScriptRoot 'package.ps1') -Version $Version -Preview:$Preview; if($LASTEXITCODE -ne 0){throw 'App packaging failed.'}}
$taskManifest=Get-Content -LiteralPath (Join-Path $taskPackage 'package.json') -Raw|ConvertFrom-Json
if($taskManifest.Version -ne $Version -or $taskManifest.BrowserStoresHash -ne (Get-FileHash -LiteralPath (Join-Path $taskRepo 'browser/store-listings.json')).Hash){throw 'Rebuild the app with the current version and browser-store configuration.'}
foreach($taskFile in $taskManifest.Files.PSObject.Properties){if((Get-FileHash -LiteralPath (Join-Path $taskPackage $taskFile.Name)).Hash -ne $taskFile.Value){throw 'Package integrity check failed.'}}
if(!$CompilerPath){$CompilerPath=Join-Path $taskRepo 'build/tools/inno-6.7.3/ISCC.exe'}
if(!(Test-Path -LiteralPath $CompilerPath)) {
    $taskTools=Join-Path $taskRepo 'build/tools'; New-Item -ItemType Directory -Path $taskTools -Force|Out-Null
    $taskDownload=Join-Path $taskTools 'innosetup-6.7.3.exe'
    Invoke-WebRequest -Uri 'https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe' -OutFile $taskDownload
    if((Get-FileHash -LiteralPath $taskDownload).Hash -ne '9C73C3BAE7ED48D44112A0F48E66742C00090BDB5BEF71D9D3C056C66E97B732'){throw 'Compiler checksum mismatch.'}
    $taskSignature=Get-AuthenticodeSignature -LiteralPath $taskDownload
    if($taskSignature.Status -ne 'Valid' -or $taskSignature.SignerCertificate.Subject -notmatch 'Pyrsys B.V.'){throw 'Compiler publisher verification failed.'}
    $taskProcess=Start-Process -FilePath $taskDownload -ArgumentList @('/PORTABLE=1','/VERYSILENT','/CURRENTUSER','/SP-','/NORESTART','/NOICONS',('/DIR="'+(Split-Path -Parent $CompilerPath)+'"')) -WindowStyle Hidden -Wait -PassThru
    if($taskProcess.ExitCode -ne 0 -or !(Test-Path -LiteralPath $CompilerPath)){throw 'Portable compiler preparation failed.'}
}
$taskStage=Join-Path $taskRepo ('build/installer-'+$Version); New-Item -ItemType Directory -Path $taskStage -Force|Out-Null
$taskName=if($Preview){'Vydrop Preview'}else{'Vydrop'}
$taskChrome=if($Preview){''}else{$taskCatalog.chromeId}; $taskEdge=if($Preview){''}else{$taskCatalog.edgeId}
$taskOrigins=@('cpoiefoeogidkjcgbacikpcdjkjeekcf',$taskChrome,$taskEdge)|Where-Object {$_}|Select-Object -Unique|ForEach-Object {'chrome-extension://'+$_+'/'}
$taskConfig=[ordered]@{
    AppId=$(if($Preview){'Vydrop.Preview.Desktop'}else{'Vydrop.Desktop'})
    AppName=$taskName;AppVersion=$Version;NumericVersion=([regex]::Match($Version,'^\d+\.\d+\.\d+').Value+'.0')
    AppMutex=$(if($Preview){'Local\ViFree.Preview.Desktop'}else{'Local\YouTubeDownloader.Desktop'})
    RepoRoot=$taskRepo;PackageDirectory=$taskPackage;OutputDirectory=$OutputDirectory
    OutputName="VydropSetup-v$Version-win-x64";InstallationMarker=(Join-Path $taskStage 'installation.json')
    HostName=$(if($Preview){'com.vifree.preview'}else{'com.vifree'})
    FirefoxId=$(if($Preview){'vifree-preview@ziadhassanein.github.io'}else{'vifree@ziadhassanein.github.io'})
    ChromeStoreId=$taskChrome;EdgeStoreId=$taskEdge
    ChromiumOrigins=(ConvertTo-Json -InputObject @($taskOrigins) -Compress)
}
$taskMarker=@{format=1;kind='installer';channel=$(if($Preview){'preview'}else{'stable'});version=$Version}
[IO.File]::WriteAllText($taskConfig.InstallationMarker,($taskMarker|ConvertTo-Json -Compress),[Text.UTF8Encoding]::new($false))
$taskLines=foreach($taskValue in $taskConfig.GetEnumerator()){'#define '+$taskValue.Key+' "'+([string]$taskValue.Value).Replace('"','""')+'"'}
$taskConfigPath=Join-Path $taskStage 'generated.iss'; [IO.File]::WriteAllLines($taskConfigPath,$taskLines,[Text.UTF8Encoding]::new($false))
& $CompilerPath ("/DGeneratedConfig=$taskConfigPath") (Join-Path $taskRepo 'installer/vydrop.iss')
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed.'}
$taskOutput=Join-Path $OutputDirectory ($taskConfig.OutputName+'.exe')
$taskProof=@{file=[IO.Path]::GetFileName($taskOutput);version=$Version;sha256=(Get-FileHash -LiteralPath $taskOutput).Hash;signature=(Get-AuthenticodeSignature -LiteralPath $taskOutput).Status.ToString();channel=$taskMarker.channel;storeListingsReady=(!$Preview -and $taskChrome -ne '' -and $taskEdge -ne '' -and $taskCatalog.firefoxListingUrl -ne '')}
[IO.File]::WriteAllText(($taskOutput+'.json'),($taskProof|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
Write-Output "Installer ready: $taskOutput"
