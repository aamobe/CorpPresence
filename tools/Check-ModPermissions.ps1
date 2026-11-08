#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameManaged,
    [Parameter(Mandatory = $true)][string]$ModFolder,
    [switch]$RequireNone
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$managed = (Resolve-Path -LiteralPath $GameManaged).Path
$folder = (Resolve-Path -LiteralPath $ModFolder).Path
$artifacts = Join-Path $repo 'artifacts/mod-scan-checks'
dotnet build (Join-Path $PSScriptRoot 'ModScanChecks/Checks.csproj') --artifacts-path $artifacts
if ($LASTEXITCODE -ne 0) { throw 'Permission check build failed.' }
$options = @()
if ($RequireNone) { $options += '--require-none' }
dotnet (Join-Path $artifacts 'bin/Checks/debug/Checks.dll') $managed $folder @options
if ($LASTEXITCODE -ne 0) { throw 'Game mod scanner rejected the package or found undeclared/unknown capabilities.' }
