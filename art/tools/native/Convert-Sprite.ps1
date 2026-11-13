#requires -Version 7.0
param(
 [Parameter(Mandatory)][string]$InputPath,
 [Parameter(Mandatory)][string]$OutputPath,
 [Parameter(Mandatory)][ValidateRange(1,65535)][int]$Width,
 [Parameter(Mandatory)][ValidateRange(1,65535)][int]$Height,
 [Parameter(Mandatory)][string]$PalettePath,
 [ValidateSet('center','majority','elastic')][string]$Method='center',
 [ValidateRange(0,0.45)][double]$Radius=0.4
)
. (Join-Path $PSScriptRoot 'Initialize-SpriteTools.ps1')
$source=(Resolve-Path -LiteralPath $InputPath).Path
$destination=[IO.Path]::GetFullPath($OutputPath)
if($source -eq $destination -or (Test-Path -LiteralPath $destination)){throw 'Output must be a new file, distinct from input.'}
$palette=@(Get-Content -LiteralPath $PalettePath -Raw | ConvertFrom-Json)
if(!$palette.Count -or @($palette|Where-Object {$_ -notmatch '^#?[0-9a-fA-F]{6}$'}).Count){throw 'Palette must be a JSON array of six-digit RGB values.'}
[IO.Directory]::CreateDirectory((Split-Path $destination))|Out-Null
$cuts=$null
if($Method -eq 'elastic'){$cuts=[SpriteRecover]::ConvertElastic($source,$destination,$Width,$Height,$palette,$Radius,$false)}
else{[SpriteRecover]::Convert($source,$destination,$Width,$Height,$palette,$Method,$false)}
[pscustomobject]@{Input=$source;InputSHA256=(Get-FileHash -LiteralPath $source).Hash;Output=$destination;Width=$Width;Height=$Height;Method=$Method;AlphaThreshold=128;Palette=$palette;Crop='none';CellRegion=$(if($Method -eq 'center'){'center pixel'}else{'central 60%'});Radius=$(if($Method -eq 'elastic'){$Radius}else{$null});Cuts=$cuts} |
 ConvertTo-Json -Depth 6 | Set-Content -LiteralPath ($destination+'.conversion.json') -Encoding utf8
Write-Output $destination
