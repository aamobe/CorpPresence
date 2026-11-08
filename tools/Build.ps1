[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameManaged,
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',
    [string]$Dotnet = 'dotnet'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$managedPath = (Resolve-Path -LiteralPath $GameManaged).Path
foreach ($reference in @('Assembly-CSharp.dll', '0Harmony.dll', 'Newtonsoft.Json.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $managedPath $reference) -PathType Leaf)) {
        throw "Missing game reference: $reference"
    }
}
if (!(Get-ChildItem -LiteralPath $managedPath -Filter 'Unity*.dll' -File | Select-Object -First 1)) {
    throw 'Missing Unity game references.'
}
$sdkVersion = & $Dotnet --version
if ($LASTEXITCODE -ne 0 -or $sdkVersion -notmatch '^(\d+)\.' -or [int]$Matches[1] -lt 8) {
    throw '.NET SDK 8 or newer is required. A runtime-only dotnet installation cannot build this project.'
}
$project = Join-Path $repoRoot 'src/CorpPresence.csproj'
$artifacts = Join-Path $repoRoot 'artifacts/build'
& $Dotnet build $project -c $Configuration --artifacts-path $artifacts "-p:GameManaged=$managedPath"
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }
$assembly = Join-Path $artifacts ("bin/CorpPresence/{0}/CorpPresence.dll" -f $Configuration.ToLowerInvariant())
if (!(Test-Path -LiteralPath $assembly -PathType Leaf)) { throw "Build output not found: $assembly" }
Write-Output "Built: $assembly"
Write-Output 'Compilation only. No game launch, deployment, package or upload was performed.'
