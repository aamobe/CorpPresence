#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameManaged,
    [Parameter(Mandatory = $true)][string]$ModFolder
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$managed = (Resolve-Path -LiteralPath $GameManaged).Path
$folder = (Resolve-Path -LiteralPath $ModFolder).Path
$artifacts = Join-Path $repo 'artifacts/mod-scan-checks'
dotnet build (Join-Path $PSScriptRoot 'ModScanChecks/Checks.csproj') --artifacts-path $artifacts
if ($LASTEXITCODE -ne 0) { throw 'Permission check build failed.' }
dotnet (Join-Path $artifacts 'bin/Checks/debug/Checks.dll') $managed $folder
if ($LASTEXITCODE -ne 0) { throw 'Game mod scanner rejected the package or found undeclared/unknown capabilities.' }
