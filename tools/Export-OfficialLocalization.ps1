#requires -Version 7.0
[CmdletBinding()]
param([string]$OutputDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Localization'))
$ErrorActionPreference = 'Stop'
$mapping = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'LocalizationChecks/Migration.json') -Raw -Encoding utf8 | ConvertFrom-Json -AsHashtable
$builtIn = Get-Content -LiteralPath (Join-Path (Split-Path -Parent $PSScriptRoot) 'src/CorpPresence/Presentation/Translations.Keys.json') -Raw -Encoding utf8 | ConvertFrom-Json -AsHashtable
[string[]]$keys = @($mapping.Keys)
[Array]::Sort($keys, [StringComparer]::Ordinal)
[IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($OutputDirectory)) | Out-Null
foreach ($locale in @('en', 'ru', 'fr')) {
    $lines = [Collections.Generic.List[string]]::new()
    foreach ($key in $keys) {
        $value = [string]$mapping[$key]
        $index = if ($locale -eq 'ru') { 0 } elseif ($locale -eq 'fr') { 1 } else { -1 }
        if ($index -ge 0 -and $builtIn[$key].Count -gt $index -and ![string]::IsNullOrEmpty($builtIn[$key][$index])) { $value = $builtIn[$key][$index] }
        # The game's parser supports quoted values, doubled quotes and literal \n.
        if ($value.Contains('\n')) { throw "Literal backslash-n cannot round-trip through the official CSV parser: $key" }
        $encoded = $value.Replace("`r`n", '\n').Replace("`r", '\n').Replace("`n", '\n').Replace('"', '""')
        $lines.Add($key + ',"' + $encoded + '"')
    }
    [IO.File]::WriteAllText((Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) "$locale.csv"), [string]::Join("`n", $lines) + "`n", [Text.UTF8Encoding]::new($false))
    Write-Output "Exported $($keys.Count) official CSV entries: $locale"
}
