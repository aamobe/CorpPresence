#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[a-zA-Z]{2,8}(?:[-_][a-zA-Z0-9]{1,8})*$')][string]$Locale,
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [string]$Name,
    [switch]$BlankValues,
    [switch]$Force
)
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'checks/LocalizationChecks/Migration.json'
$mapping = Get-Content -Raw -LiteralPath $source -Encoding utf8 | ConvertFrom-Json -AsHashtable
$translations = [ordered]@{}
[string[]]$keys = @($mapping.Keys)
[Array]::Sort($keys, [StringComparer]::Ordinal)
foreach ($key in $keys) { $translations[$key] = if ($BlankValues) { '' } else { $mapping[$key] } }
$code = $Locale.Replace('_', '-').ToLowerInvariant()
$document = [ordered]@{ schemaVersion = 1; locale = $code; name = $(if ($Name) { $Name } else { $code }); translations = $translations }
$destination = [IO.Path]::GetFullPath($OutputPath)
if ((Test-Path -LiteralPath $destination) -and !$Force) { throw 'Output exists. Choose another path or explicitly use -Force.' }
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
[IO.File]::WriteAllText($destination, ($document | ConvertTo-Json -Depth 5) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
Write-Output "Exported $($keys.Count) keys for $code to $destination"
Write-Output 'Reference template only. JSON is reference-only; the current release loads official Localization/*.csv through the game. Use en.csv as the editable translation template.'
