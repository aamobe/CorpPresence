#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Assembly,
    [string]$Dotnet = 'dotnet'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (!(Test-Path -LiteralPath (Join-Path $repo '.git'))) { throw 'Release verification requires the source Git checkout.' }
$revision = & git -C $repo rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot read source revision.' }
$status = & git -C $repo status --porcelain --untracked-files=normal
if ($LASTEXITCODE -ne 0 -or $status) { throw 'Release verification requires a clean worktree.' }
$manifest = [xml](Get-Content -Raw -LiteralPath (Join-Path $repo 'manifest.xml'))
$version = [string]$manifest.Mod.ModVersion
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid release manifest version.' }
$tag = & git -C $repo tag --points-at HEAD --list "v$version"
if ($LASTEXITCODE -ne 0 -or $tag -cne "v$version") { throw 'Current commit needs the matching release tag.' }
$output = Join-Path $repo ('artifacts/release-build-checks/' + [guid]::NewGuid().ToString('N'))
& $Dotnet build (Join-Path $PSScriptRoot 'checks/BuildIdentityChecks/Checks.csproj') --artifacts-path $output -warnaserror
if ($LASTEXITCODE -ne 0) { throw 'Identity checker build failed.' }
& $Dotnet (Join-Path $output 'bin/Checks/debug/Checks.dll') (Resolve-Path -LiteralPath $Assembly).Path --release $version $revision
if ($LASTEXITCODE -ne 0) { throw 'Release artifact identity check failed.' }
Write-Output 'Release identity verified. Permissions, item identity baseline and gameplay acceptance remain separate gates.'
