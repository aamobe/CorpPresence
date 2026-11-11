#requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Assembly,[string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$dll=(Resolve-Path -LiteralPath $Assembly).Path
$output=Join-Path $repo ('artifacts/release-identities-'+[Guid]::NewGuid().ToString('N'))
& $Dotnet build (Join-Path $PSScriptRoot 'ItemChecks/Checks.csproj') --artifacts-path $output -warnaserror
if($LASTEXITCODE -ne 0){throw 'Identity checker build failed'}
& $Dotnet (Join-Path $output 'bin/Checks/debug/Checks.dll') $dll --release-baseline
if($LASTEXITCODE -ne 0){throw 'Release identity baseline mismatch; review and append new identities before publishing'}
Write-Output 'PASS release identity baseline; no package, deployment or upload performed'
