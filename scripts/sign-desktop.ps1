param([Parameter(Mandatory)][string]$PackageDirectory,[string]$CertificateThumbprint,[uri]$TimestampServer,[switch]$ValidateOnly)
$ErrorActionPreference='Stop'
$taskExecutable=Join-Path ([IO.Path]::GetFullPath($PackageDirectory)) 'Vydrop.exe'
if(!(Test-Path -LiteralPath $taskExecutable -PathType Leaf)) { throw 'Choose an existing Vydrop package directory.' }
if($ValidateOnly) {
    Get-AuthenticodeSignature -LiteralPath $taskExecutable | Select-Object Status,StatusMessage
    return
}
if($CertificateThumbprint -notmatch '^[A-Fa-f0-9]{40,64}$' -or !$TimestampServer -or $TimestampServer.Scheme -ne 'http') {
    throw 'Supply your code-signing certificate thumbprint and HTTP Authenticode timestamp server. The Windows timestamp API does not support HTTPS.'
}
# No certificate-store enumeration, secret export, self-signed trust installation or publisher-account changes.
$taskCertificate=Get-Item -LiteralPath ("Cert:\CurrentUser\My\"+$CertificateThumbprint)
if(!$taskCertificate.HasPrivateKey -or $taskCertificate.NotAfter -le (Get-Date) -or !($taskCertificate.EnhancedKeyUsageList | Where-Object {$_.ObjectId.Value -eq '1.3.6.1.5.5.7.3.3'})) { throw 'The selected certificate is not a current code-signing certificate with a private key.' }
$taskSignature=Set-AuthenticodeSignature -LiteralPath $taskExecutable -Certificate $taskCertificate -HashAlgorithm SHA256 -TimestampServer $TimestampServer.AbsoluteUri -IncludeChain NotRoot
if($taskSignature.Status -ne 'Valid' -or !$taskSignature.TimeStamperCertificate) { throw 'Signing or timestamp verification failed. Do not publish this package.' }
Write-Output 'Desktop signature and timestamp verified. Regenerate package hashes after signing.'
