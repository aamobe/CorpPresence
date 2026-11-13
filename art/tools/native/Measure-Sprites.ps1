#requires -Version 7.0
param(
 [Parameter(Mandatory)][string[]]$InputPaths,
 [Parameter(Mandatory)][int]$LogicalWidth,
 [Parameter(Mandatory)][int]$LogicalHeight,
 [Parameter(Mandatory)][string]$PalettePath,
 [Parameter(Mandatory)][string]$OutputPath
)
. (Join-Path $PSScriptRoot 'Initialize-SpriteTools.ps1')
$palette=@(Get-Content -LiteralPath $PalettePath -Raw|ConvertFrom-Json)
if(!$palette.Count -or @($palette|Where-Object {$_ -notmatch '^#?[0-9a-fA-F]{6}$'}).Count){throw 'Palette must contain six-digit RGB values.'}
$destination=[IO.Path]::GetFullPath($OutputPath)
foreach($p in $InputPaths){if((Resolve-Path -LiteralPath $p).Path -eq $destination){throw 'Report cannot overwrite an input image.'}}
[IO.Directory]::CreateDirectory((Split-Path $destination))|Out-Null
$results=@(foreach($p in $InputPaths){[SpriteProbe]::Analyze((Resolve-Path -LiteralPath $p).Path,$LogicalWidth,$LogicalHeight,$palette)})
$results|ConvertTo-Json -Depth 5|Set-Content -LiteralPath $destination -Encoding utf8
$results|Select-Object Source,Width,Height,MainRgb,PartialAlpha,ExactPaletteFraction,OpaqueComponents4
