param([string]$Version='4.2.5', [switch]$Preview)
& (Join-Path $PSScriptRoot 'package.ps1') -Version $Version -Preview:$Preview
