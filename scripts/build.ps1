param([string]$Version='4.4.17', [switch]$Preview)
& (Join-Path $PSScriptRoot 'package.ps1') -Version $Version -Preview:$Preview
