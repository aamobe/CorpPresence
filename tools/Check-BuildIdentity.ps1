#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameManaged,
    [string]$Dotnet = 'dotnet',
    [string]$OutputRoot
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (!$OutputRoot) { $OutputRoot = Join-Path $repoRoot ('artifacts/build-identity-checks/' + [guid]::NewGuid().ToString('N')) }
$artifacts = [IO.Path]::GetFullPath($OutputRoot)
function Invoke-Dotnet([string[]]$Arguments) {
    & $Dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Build identity check failed: dotnet $($Arguments -join ' ')" }
}
Invoke-Dotnet @('build', (Join-Path $PSScriptRoot 'BuildIdentityChecks/Checks.csproj'), '--artifacts-path', (Join-Path $artifacts 'runner'))
$runner = Join-Path $artifacts 'runner/bin/Checks/debug/Checks.dll'
$identity = '-dev+unknown'
# Match the production target: do not inherit a parent checkout's identity.
if ((Test-Path -LiteralPath (Join-Path $repoRoot '.git')) -and (Get-Command git -ErrorAction SilentlyContinue)) {
    $revision = & git -C $repoRoot rev-parse HEAD
    $revisionOk = $LASTEXITCODE -eq 0 -and $revision -match '^[0-9a-fA-F]{40}([0-9a-fA-F]{24})?$'
    if ($revisionOk) {
        $status = & git -C $repoRoot status --porcelain --untracked-files=normal
        if ($LASTEXITCODE -eq 0) {
            $identity = "-dev+$revision"
            if ($status) { $identity += '-dirty' }
        }
    }
}
foreach ($maintenance in @($false, $true)) {
    $flavor = if ($maintenance) { 'maintenance' } else { 'regular' }
    $output = Join-Path $artifacts $flavor
    Invoke-Dotnet @('build', (Join-Path $repoRoot 'src/CorpPresence.csproj'), '-c', 'Release', '--artifacts-path', $output, "-p:GameManaged=$GameManaged", "-p:Maintenance=$maintenance")
    $expected = $identity + $(if ($maintenance) { '-maintenance' } else { '' })
    Invoke-Dotnet @($runner, (Join-Path $output 'bin/CorpPresence/release/CorpPresence.dll'), $expected)
}
# SDK revision overrides must not impersonate a different source commit or break generated C#.
foreach ($override in @('0000000000000000000000000000000000000000', 'not-a-valid-hash')) {
    $output = Join-Path $artifacts 'revision-override'
    Invoke-Dotnet @('build', (Join-Path $repoRoot 'src/CorpPresence.csproj'), '-c', 'Release', '--artifacts-path', $output, "-p:GameManaged=$GameManaged", "-p:SourceRevisionId=$override")
    Invoke-Dotnet @($runner, (Join-Path $output 'bin/CorpPresence/release/CorpPresence.dll'), $identity)
}
# Exercise the same target and actual version declaration in an archive without its own Git metadata.
# No temporary Git repository is initialized, and no game files are copied.
$fixture = Join-Path $artifacts 'archive'
New-Item -ItemType Directory -Force -Path $fixture | Out-Null
Set-Content -LiteralPath (Join-Path $fixture 'NuGet.Config') -Value '<configuration><packageSources><clear /></packageSources></configuration>'
Copy-Item -LiteralPath (Join-Path $repoRoot 'src/BuildIdentity.targets') -Destination $fixture
Copy-Item -LiteralPath (Join-Path $repoRoot 'src/Properties/AssemblyInfo.cs') -Destination $fixture
$main = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src/CorpPresence/Main.cs')
$version = [regex]::Match($main, '(?s)private const string ReleaseVersion.*?#endif').Value
if (!$version) { throw 'Cannot locate the actual version declaration for archive fixture.' }
Set-Content -LiteralPath (Join-Path $fixture 'Main.cs') -Value "namespace CorpPresence { public class Main { $version`n} }"
Set-Content -LiteralPath (Join-Path $fixture 'Fixture.csproj') -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>netstandard2.1</TargetFramework><GenerateAssemblyInfo>false</GenerateAssemblyInfo></PropertyGroup><Import Project="BuildIdentity.targets" /></Project>'
Invoke-Dotnet @('build', (Join-Path $fixture 'Fixture.csproj'), '-c', 'Release', '--artifacts-path', (Join-Path $fixture 'artifacts'))
Invoke-Dotnet @($runner, (Join-Path $fixture 'artifacts/bin/Fixture/release/Fixture.dll'), '-dev+unknown')
Write-Output "PASS archive without its own Git metadata (including parent-checkout isolation); fixture at $fixture"
Write-Output 'Build-time metadata checks only; no game launch, deployment or release package.'
