#requires -Version 7.0
param([Parameter(Mandatory)][string]$OutputDirectory)
. (Join-Path $PSScriptRoot 'Initialize-SpriteTools.ps1')
$art=Split-Path (Split-Path $PSScriptRoot)
$example=Join-Path $art 'examples/mineral-native'
$work=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $work){throw 'Choose a new output directory to preserve existing evidence.'}
[IO.Directory]::CreateDirectory($work)|Out-Null
[SpriteProbe]::SelfTest((Join-Path $work 'probe'))
[SpriteRecover]::SelfTest((Join-Path $art 'evidence/native/Sprite/furnace.png'),(Join-Path $work 'roundtrip'))
$palettePath=Join-Path $example 'palette.json'
$palette=@(Get-Content -LiteralPath $palettePath -Raw|ConvertFrom-Json)
$zeroCuts=[SpriteRecover]::ConvertElastic((Join-Path $example 'generated.png'),(Join-Path $work 'elastic-zero-radius.png'),48,64,$palette,0,$false)
foreach($axis in 0,1){$cuts=$zeroCuts[$axis];if($cuts[0] -ne 0){throw 'Invalid grid origin.'};for($i=1;$i -lt $cuts.Length;$i++){if($cuts[$i] -le $cuts[$i-1]){throw 'Nonmonotone zero-radius grid.'}}}
if($zeroCuts[0][-1] -ne 1086 -or $zeroCuts[1][-1] -ne 1448){throw 'Grid endpoints do not preserve full input.'}
& (Join-Path $PSScriptRoot 'Convert-Sprite.ps1') -InputPath (Join-Path $example 'generated.png') -OutputPath (Join-Path $work 'automatic.png') -Width 48 -Height 64 -PalettePath $palettePath | Out-Null
& (Join-Path $PSScriptRoot 'Replay-PixelEdits.ps1') -InputPath (Join-Path $work 'automatic.png') -EditsPath (Join-Path $example 'refinement-edits.json') -OutputPath (Join-Path $work 'refined.png') | Out-Null
& (Join-Path $PSScriptRoot 'Replay-PixelEdits.ps1') -InputPath (Join-Path $work 'refined.png') -EditsPath (Join-Path $example 'material-edits.json') -OutputPath (Join-Path $work 'mineral.png') | Out-Null
foreach($file in @('automatic.png','refined.png','mineral.png')){if([SpriteRecover]::Difference((Join-Path $work $file),(Join-Path $example $file)) -ne 0){throw "Replay differs from committed $file"}}
$first=[SpriteRecover]::Difference((Join-Path $work 'automatic.png'),(Join-Path $work 'refined.png'))
$second=[SpriteRecover]::Difference((Join-Path $work 'refined.png'),(Join-Path $work 'mineral.png'))
$net=[SpriteRecover]::Difference((Join-Path $work 'automatic.png'),(Join-Path $work 'mineral.png'))
if($first -ne 351 -or $second -ne 108 -or $net -ne 422){throw 'Edit accounting changed.'}
$a=[Drawing.Bitmap]::new((Join-Path $work 'automatic.png'));$b=[Drawing.Bitmap]::new((Join-Path $work 'mineral.png'))
try{for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 48;$x++){if($a.GetPixel($x,$y).A -ne $b.GetPixel($x,$y).A){throw 'Silhouette/alpha changed.'}}}}finally{$a.Dispose();$b.Dispose()}
$metrics=[SpriteProbe]::Analyze((Join-Path $work 'mineral.png'),48,64,$palette)
if($metrics.Width -ne 48 -or $metrics.Height -ne 64 -or $metrics.MainRgb -ne 23 -or $metrics.PartialAlpha -ne 0 -or $metrics.ExactPaletteFraction -ne 1 -or $metrics.MainPixels -ne 1252){throw 'Final image constraints changed.'}
# Guard against applying this edit list to the wrong revision.
$rejected=$false
try{& (Join-Path $PSScriptRoot 'Replay-PixelEdits.ps1') -InputPath (Join-Path $work 'automatic.png') -EditsPath (Join-Path $example 'material-edits.json') -OutputPath (Join-Path $work 'wrong-version.png') | Out-Null}catch{if($_.Exception.Message -notmatch 'Input/version mismatch'){throw};$rejected=$true}
if(-not $rejected){throw 'Wrong base version was not rejected.'}
$manifest=Get-Content -LiteralPath (Join-Path $example 'manifest.json') -Raw|ConvertFrom-Json
foreach($file in $manifest.files){
 $path=Join-Path $example $file.path
 if($file.hashMode -eq 'utf8-lf'){
  $text=[IO.File]::ReadAllText($path).Replace("`r`n","`n").Replace("`r","`n").TrimStart([char]0xFEFF)
  $hasher=[Security.Cryptography.SHA256]::Create()
  try{$hash=[BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($text))).Replace('-','')}finally{$hasher.Dispose()}
 }else{$hash=(Get-FileHash -LiteralPath $path).Hash}
 if($hash -ne $file.sha256){throw "Manifest hash mismatch: $($file.path)"}
}
[pscustomobject]@{Passed=$true;FirstEdits=$first;SecondEdits=$second;NetEdits=$net;AlphaChanges=0;ReplayRGBADifferences=0;WrongVersionRejected=$rejected;Metrics=$metrics}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $work 'verification.json') -Encoding utf8
Write-Output "PASS: grid/components fixtures, original RGBA round-trip, sample replay, 351/108/422 edit counts, alpha, palette, manifest hashes, wrong-base rejection. Report: $work"
Write-Output 'Does not regenerate AI output, re-export Aseprite, judge artistic quality, or run the game.'
