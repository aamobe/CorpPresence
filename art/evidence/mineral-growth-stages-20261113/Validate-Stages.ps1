$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path $PSScriptRoot 'native'
$basePath=(Resolve-Path (Join-Path $PSScriptRoot '../../../src/CorpPresence/assets/machine-redesign-20261113/mineral-growth-station.png')).Path
$layout=Get-Content (Join-Path $dir 'layout.json') -Raw|ConvertFrom-Json
$stats=foreach($name in @('ore-growing','ore-mature','light-salt-core-growing','light-salt-core-mature','mineral-growth-station-running')){
 $path=Join-Path $dir ($name+'.png');$d=Get-Content (Join-Path $dir ($name+'.pixels.json')) -Raw|ConvertFrom-Json
 $im=[Drawing.Bitmap]::FromFile($path)
 try{
  if($im.Width -ne 48 -or $im.Height -ne 64){throw 'Wrong canvas'}
  $colors=@{};$n=0;$xmin=48;$ymin=64;$xmax=-1;$ymax=-1
  for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 48;$x++){
   $c=$im.GetPixel($x,$y);if($c.A -notin @(0,255)){throw 'Nonbinary alpha'}
   $ch=$d.Rows[$y][$x];$expected=if($ch -eq '.'){0}else{[Drawing.ColorTranslator]::FromHtml('#'+$d.Palette['0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ'.IndexOf($ch)]).ToArgb()};if($c.ToArgb() -ne $expected){throw 'Source matrix mismatch'}
   if($c.A){$n++;$colors[$c.ToArgb()]=1;$xmin=[Math]::Min($xmin,$x);$ymin=[Math]::Min($ymin,$y);$xmax=[Math]::Max($xmax,$x);$ymax=[Math]::Max($ymax,$y)
    if($name -ne 'mineral-growth-station-running' -and ($x -lt 12 -or $x -gt 27 -or $y -lt 22 -or $y -gt 37)){throw 'Specimen escapes chamber mask'}
   }elseif($c.R -or $c.G -or $c.B){throw 'Hidden transparent RGB'}
  }}
  if($name -ne 'mineral-growth-station-running' -and $ymax -ne 37){throw 'Specimen not touching contact row'}
  if($name.StartsWith('ore-') -and ($ymax-$ymin) -le ($xmax-$xmin)){throw 'Ore must be upright'}
  [pscustomobject]@{File=$name+'.png';Canvas=@(48,64);BBox=@($xmin,$ymin,$xmax,$ymax);OpaquePixels=$n;Colors=$colors.Count;SHA256=(Get-FileHash $path).Hash;MatrixMatches=$true}
 }finally{$im.Dispose()}
}
$base=[Drawing.Bitmap]::FromFile($basePath);$run=[Drawing.Bitmap]::FromFile((Join-Path $dir 'mineral-growth-station-running.png'))
try{
 $changed=0;$outside=0;$alpha=0
 for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 48;$x++){$a=$base.GetPixel($x,$y);$b=$run.GetPixel($x,$y);if($a.A -ne $b.A){$alpha++};if($a.ToArgb() -ne $b.ToArgb()){$changed++;if($x -lt 9 -or $x -gt 29 -or $y -lt 22 -or $y -gt 44){$outside++}}}}
 if($outside -or $alpha -or !$changed){throw 'Glow mask mismatch'}
 $glow=[pscustomobject]@{ChangedRGBA=$changed;ChangedAlpha=$alpha;OutsideMask=$outside;Mask=@(9,22,21,23);PaletteChanges=@('275654 -> 2F756F','2F756F -> 45918B')}
}finally{$base.Dispose();$run.Dispose()}
[pscustomobject]@{MachineSource='machine-redesign-20261113/mineral-growth-station.png';MachineSHA256=(Get-FileHash $basePath).Hash;Sprites=$stats;Layout=$layout;Glow=$glow;PPU=100;Pivot=@(0.5,0.5);OverlayOffset=@(0,0);Footprint=@(3,4);Filter='Point';GameTested=$false}|ConvertTo-Json -Depth 8|Set-Content (Join-Path $dir 'validation.json') -Encoding utf8
$stats|Format-Table File,Colors,OpaquePixels,BBox
$glow|Format-List
