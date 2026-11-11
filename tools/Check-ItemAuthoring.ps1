#requires -Version 7.0
[CmdletBinding()]
param([string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$output=Join-Path $root 'artifacts/item-authoring-checks'
$env:UseSharedCompilation='false'
Push-Location $root
try {
    & $Dotnet build (Join-Path $PSScriptRoot 'checks/ItemAuthoringChecks/Checks.csproj') --artifacts-path $output
    if($LASTEXITCODE -ne 0){throw 'Item authoring checks failed to compile.'}
    & $Dotnet (Join-Path $output 'bin/Checks/debug/Checks.dll')
    if($LASTEXITCODE -ne 0){throw 'Item authoring checks failed.'}
} finally {Pop-Location}
