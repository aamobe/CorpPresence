#requires -Version 7.0
[CmdletBinding()]
param([switch]$CheckReleaseBaseline)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$failures = New-Object 'System.Collections.Generic.List[string]'
$documents = @()
$documents += Get-ChildItem -LiteralPath $repoRoot -File -Filter '*.md'
$documents += Get-ChildItem -LiteralPath (Join-Path $repoRoot 'docs') -File -Recurse -Filter '*.md'
$documents += Get-ChildItem -LiteralPath (Join-Path $repoRoot 'tools') -File -Recurse -Filter '*.md' |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
$linkCount = 0
foreach ($document in $documents) {
    $text = Get-Content -LiteralPath $document.FullName -Raw -Encoding UTF8
    foreach ($link in [regex]::Matches($text, '\[[^\]\r\n]*\]\((?<target><[^>]+>|[^)\r\n]+)\)')) {
        $target = $link.Groups['target'].Value.Trim().Trim('<', '>')
        if ($target -match '^(?:[a-zA-Z][a-zA-Z0-9+.-]*:|#)') { continue }
        $relative = [Uri]::UnescapeDataString(($target -split '[?#]', 2)[0])
        if (!$relative) { continue }
        $linkCount++
        $resolved = if ($relative.StartsWith('/')) {
            Join-Path $repoRoot $relative.TrimStart('/')
        } else {
            Join-Path $document.DirectoryName $relative
        }
        if (!(Test-Path -LiteralPath $resolved)) {
            $failures.Add("Broken local link in $($document.FullName): $target")
        }
    }
}
[xml]$manifest = Get-Content -LiteralPath (Join-Path $repoRoot 'manifest.xml') -Raw -Encoding UTF8
$version = [string]$manifest.Mod.ModVersion
$main = Get-Content -LiteralPath (Join-Path $repoRoot 'src/CorpPresence/Main.cs') -Raw -Encoding UTF8
$assemblyInfo = Get-Content -LiteralPath (Join-Path $repoRoot 'src/Properties/AssemblyInfo.cs') -Raw -Encoding UTF8
$mainVersion = [regex]::Match($main, 'ReleaseVersion\s*=\s*"([^"]+)"').Groups[1].Value
$fileVersion = [regex]::Match($assemblyInfo, 'AssemblyFileVersion\("([^"]+)"\)').Groups[1].Value
if (!$version -or $mainVersion -ne $version -or $fileVersion -ne "$version.0") {
    $failures.Add("Version mismatch: manifest=$version, Main=$mainVersion, file=$fileVersion")
}
if ($manifest.Mod.ID -ne 'CorpPresence') { $failures.Add('Unexpected mod ID.') }
$translations = Get-Content -LiteralPath (Join-Path $repoRoot 'src/CorpPresence/Presentation/Translations.json') -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable
if ($null -eq $translations) { $failures.Add('Translation JSON is empty.') }
$snapshotCount = 0
function Get-ReleaseHash([string]$Path, [string]$Mode) {
    if ($Mode -eq 'utf8-lf') {
        $text = [IO.File]::ReadAllText($Path).Replace("`r`n", "`n").Replace("`r", "`n").TrimStart([char]0xFEFF)
        $bytes = [Text.UTF8Encoding]::new($false).GetBytes($text)
        $hasher = [Security.Cryptography.SHA256]::Create()
        try { return [BitConverter]::ToString($hasher.ComputeHash($bytes)).Replace('-', '') }
        finally { $hasher.Dispose() }
    }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}
if ($CheckReleaseBaseline) {
    $snapshotPath = Join-Path (Join-Path $repoRoot 'docs/releases') "$version-source-manifest.json"
    $snapshot = Get-Content -LiteralPath $snapshotPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($snapshot.version -ne $version) { $failures.Add('Release snapshot version mismatch.') }
    foreach ($file in $snapshot.files) {
        $snapshotCount++
        $path = Join-Path $repoRoot $file.path
        if (!(Test-Path -LiteralPath $path -PathType Leaf)) {
            $failures.Add("Missing release file: $($file.path)")
        } elseif ((Get-ReleaseHash $path $file.hashMode) -ne $file.sha256) {
            $failures.Add("Changed since release snapshot: $($file.path)")
        }
    }
    $currentSources = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -File -Recurse |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    if ($currentSources.Count -ne $snapshot.sourceFileCount) { $failures.Add('Release source file count mismatch.') }
}
if ($failures.Count) { throw ($failures -join [Environment]::NewLine) }
Write-Output "OK: $($documents.Count) Markdown files, $linkCount local links, version $version, valid translations JSON."
if ($CheckReleaseBaseline) { Write-Output "OK: $snapshotCount release files match SHA256 records (UTF-8/LF normalized text, exact binary bytes)." }
Write-Output 'Static checks only; no game or automated game tests were run. External URLs and Markdown anchors are not checked.'
