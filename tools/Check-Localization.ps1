#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameManaged,
    [string]$Dotnet = 'dotnet',
    [string]$CompiledAssembly
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $PSScriptRoot 'LocalizationChecks/Checks.csproj'
$artifacts = Join-Path $repoRoot 'artifacts/localization-checks'
$managed = (Resolve-Path -LiteralPath $GameManaged).Path
$migration = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'LocalizationChecks/Migration.json') | ConvertFrom-Json -AsHashtable
$catalog = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src/CorpPresence/Presentation/Translations.Keys.json') | ConvertFrom-Json -AsHashtable
$references = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($source in Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Recurse -Filter '*.cs') {
    $text = Get-Content -Raw -LiteralPath $source.FullName
    foreach ($match in [regex]::Matches($text, 'L\.K\("([^"]+)"')) { [void]$references.Add($match.Groups[1].Value) }
    # Explicit key metadata also feeds dynamic fields, indexed arrays and config identities.
    foreach ($match in [regex]::Matches($text, '"([a-z][A-Za-z0-9]*(?:\.[a-z][A-Za-z0-9]*)+)"')) {
        if ($catalog.ContainsKey($match.Groups[1].Value)) { [void]$references.Add($match.Groups[1].Value) }
    }
    if ($source.Name -eq 'MenuLabels.cs') {
        $keys = [regex]::Match($text, '(?s)string\[\]\[\] Keys = \{(.*?)\r?\n \};').Groups[1].Value
        foreach ($match in [regex]::Matches($keys, '"([^"]+)"')) { [void]$references.Add($match.Groups[1].Value) }
    }
}
foreach ($key in $references) {
    if (!$catalog.ContainsKey($key) -or !$migration.ContainsKey($key)) { throw "Unrecorded localization key: $key" }
}
foreach ($key in $catalog.Keys) {
    if ($key -cnotmatch '^[a-z][A-Za-z0-9]*(\.[a-z][A-Za-z0-9]*)+$') { throw "Invalid stable key: $key" }
    if (!$references.Contains($key) -or !$migration.ContainsKey($key)) { throw "Unreferenced localization key: $key" }
    if ($catalog[$key] -isnot [array] -or $catalog[$key].Count -gt 2) { throw "Invalid ru/fr translation array: $key" }
}
if ($catalog.Count -ne $migration.Count) { throw 'Migration audit and catalog differ.' }
Write-Output "OK: $($references.Count) stable keys match source references and migration audit."
& $Dotnet build $project --artifacts-path $artifacts "-p:GameManaged=$managed"
if ($LASTEXITCODE -ne 0) { throw 'Localization checks failed to compile.' }
$runner = Join-Path $artifacts 'bin/Checks/debug/Checks.dll'
if ($CompiledAssembly) { & $Dotnet $runner (Resolve-Path -LiteralPath $CompiledAssembly).Path }
else { & $Dotnet $runner }
if ($LASTEXITCODE -ne 0) { throw 'Localization checks failed.' }
$csvArtifacts = Join-Path $repoRoot 'artifacts/official-csv-checks'
& $Dotnet build (Join-Path $PSScriptRoot 'OfficialCsvChecks/Checks.csproj') --artifacts-path $csvArtifacts
if ($LASTEXITCODE -ne 0) { throw 'Official CSV checks failed to compile.' }
& $Dotnet (Join-Path $csvArtifacts 'bin/Checks/debug/Checks.dll') $managed $repoRoot (Join-Path $repoRoot 'Localization')
if ($LASTEXITCODE -ne 0) { throw 'Official game CSV parser checks failed.' }
Write-Output 'Translation code and embedded resource checks only; no game launch or gameplay test.'
