$ErrorActionPreference='Stop'
$taskRepo=Split-Path -Parent $PSScriptRoot
$taskVersion=([xml](Get-Content -LiteralPath (Join-Path $taskRepo 'Directory.Build.props'))).Project.PropertyGroup.Version
$taskSavedEnvironment=@{}
foreach($taskName in @('GITHUB_EVENT_NAME','GITHUB_REF_TYPE','GITHUB_REF_NAME','GITHUB_REPOSITORY','GH_RELEASE_TOKEN')){$taskSavedEnvironment[$taskName]=[Environment]::GetEnvironmentVariable($taskName)}
# All external commands are stubbed in this test scope; the real publisher reads real package hashes.
function git { $global:LASTEXITCODE=0 }
function Invoke-RestMethod {
 param([string]$Uri,[string]$Method='Get',$Headers,[string]$ContentType,[string]$Body,[string]$InFile)
 if($Uri -eq 'https://api.github.com/repos/ZiadHassanein/Vydrop'){return @{private=$false;full_name='ZiadHassanein/Vydrop';default_branch='main'}}
 if($Uri -eq 'https://api.github.com/repos/ZiadHassanein/Vydrop/releases' -and $Method -eq 'Post'){
  if(!(($Body|ConvertFrom-Json).draft)){throw 'Publication must begin as a draft.'}
  return @{id=123;html_url='https://github.com/ZiadHassanein/Vydrop/releases/tag/v'+$taskVersion}
 }
 if($Uri.StartsWith('https://uploads.github.com/repos/ZiadHassanein/Vydrop/releases/123/assets?name=')){
  $taskName=[Uri]::UnescapeDataString(($Uri -split 'name=',2)[1])
  $taskDigest='sha256:'+(Get-FileHash -LiteralPath $InFile -Algorithm SHA256).Hash.ToLowerInvariant()
  if($global:VydropReleasePublicationFixture.TamperInstaller -and $taskName.EndsWith('.exe')){$taskDigest='sha256:bad'}
  $taskAsset=@{id=$global:VydropReleasePublicationFixture.Assets.Count+1;name=$taskName;state='uploaded';size=(Get-Item -LiteralPath $InFile).Length;digest=$taskDigest}
  $global:VydropReleasePublicationFixture.Assets.Add($taskAsset);return $taskAsset
 }
 if($Uri -eq 'https://api.github.com/repos/ZiadHassanein/Vydrop/releases/123/assets'){return $global:VydropReleasePublicationFixture.Assets.ToArray()}
 if($Uri -eq 'https://api.github.com/repos/ZiadHassanein/Vydrop/releases/123' -and $Method -eq 'Patch'){
  if($global:VydropReleasePublicationFixture.Assets.Count -ne 2){throw 'Both assets must precede publication.'}
  $global:VydropReleasePublicationFixture.Published=$true;return @{html_url='https://github.com/ZiadHassanein/Vydrop/releases/tag/v'+$taskVersion}
 }
 throw ('Unexpected external request in offline test: '+$Uri)
}
try {
 $env:GITHUB_EVENT_NAME='push';$env:GITHUB_REF_TYPE='tag';$env:GITHUB_REF_NAME='v'+$taskVersion
 $env:GITHUB_REPOSITORY='ZiadHassanein/Vydrop';$env:GH_RELEASE_TOKEN='offline-test-only'
 foreach($taskTamper in @($false,$true)){
  $global:VydropReleasePublicationFixture=[pscustomobject]@{Assets=[Collections.Generic.List[object]]::new();Published=$false;TamperInstaller=$taskTamper}
  $taskFailed=$false
  try{$null=& (Join-Path $taskRepo 'scripts/release.ps1')}catch{
   if(!$global:VydropReleasePublicationFixture.TamperInstaller -or $_.Exception.Message -notlike '*digest did not match*'){throw}
   $taskFailed=$true
  }
  if(!$global:VydropReleasePublicationFixture.TamperInstaller -and (!$global:VydropReleasePublicationFixture.Published -or $taskFailed)){throw 'Valid artifacts did not publish in the offline simulation.'}
  if($global:VydropReleasePublicationFixture.TamperInstaller -and ($global:VydropReleasePublicationFixture.Published -or !$taskFailed)){throw 'A mismatched installer was allowed to publish.'}
 }
 Write-Output 'PASS: Publication starts as a draft, verifies both real asset hashes, and refuses a mismatched installer digest.'
} finally {Remove-Variable -Scope Global -Name VydropReleasePublicationFixture -ErrorAction SilentlyContinue; foreach($taskName in $taskSavedEnvironment.Keys){[Environment]::SetEnvironmentVariable($taskName,$taskSavedEnvironment[$taskName])}}
