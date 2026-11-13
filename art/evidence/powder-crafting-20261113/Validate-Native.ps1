$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path $PSScriptRoot 'native'
$specs=@(@('silicon-plate',32,32),@('electronic-substrate',32,16),@('electronic-blueprint',16,16),@('crafting-station',48,48),@('crafting-station-running',48,48))
$stats=foreach($s in $specs){
 $path=Join-Path $dir ($s[0]+'.png');$d=Get-Content (Join-Path $dir ($s[0]+'.pixels.json')) -Raw|ConvertFrom-Json
 $im=[Drawing.Bitmap]::FromFile($path)
 try{
  if($im.Width -ne $s[1] -or $im.Height -ne $s[2]){throw 'Wrong native size'}
  $colors=@{};$opaque=0;$xmin=$im.Width;$ymin=$im.Height;$xmax=-1;$ymax=-1
  for($y=0;$y -lt $im.Height;$y++){if($d.Rows[$y].Length -ne $im.Width){throw 'Wrong matrix row width'};for($x=0;$x -lt $im.Width;$x++){
   $c=$im.GetPixel($x,$y);if($c.A -notin @(0,255)){throw 'Non-binary alpha'}
   $ch=$d.Rows[$y][$x];$expected=if($ch -eq '.'){0}else{[Drawing.ColorTranslator]::FromHtml('#'+$d.Palette['0123456789ABCDEF'.IndexOf($ch)]).ToArgb()}
   if($c.ToArgb() -ne $expected){throw 'PNG differs from editable matrix'}
   if($c.A){$opaque++;$colors[$c.ToArgb()]=1;$xmin=[Math]::Min($xmin,$x);$ymin=[Math]::Min($ymin,$y);$xmax=[Math]::Max($xmax,$x);$ymax=[Math]::Max($ymax,$y)}elseif($c.R -or $c.G -or $c.B){throw 'Hidden transparent RGB'}
  }}
  $mask=for($y=8;$y -lt $im.Height;$y+=16){$row='';for($x=8;$x -lt $im.Width;$x+=16){$row+=if($im.GetPixel($x,$y).A){'1'}else{'0'}};$row}
  [pscustomobject]@{File=$s[0]+'.png';Width=$im.Width;Height=$im.Height;Footprint=@(($im.Width/16),($im.Height/16));Shape='full rectangle';Pivot=@(0.5,0.5);PPU=100;Filter='Point';Colors=$colors.Count;OpaquePixels=$opaque;BBox=@($xmin,$ymin,$xmax,$ymax);CenterAlphaMask=$mask -join '/';SHA256=(Get-FileHash $path).Hash;SourceMatrixVerified=$true}
 }finally{$im.Dispose()}
}
$a=[Drawing.Bitmap]::FromFile((Join-Path $dir 'crafting-station.png'));$b=[Drawing.Bitmap]::FromFile((Join-Path $dir 'crafting-station-running.png'))
try{
 $changed=0;$outside=0;$alpha=0
 for($y=0;$y -lt 48;$y++){for($x=0;$x -lt 48;$x++){$ca=$a.GetPixel($x,$y);$cb=$b.GetPixel($x,$y);if($ca.A -ne $cb.A){$alpha++};if($ca.ToArgb() -ne $cb.ToArgb()){$changed++;if(!(($x -ge 16 -and $x -le 21 -and $y -ge 22 -and $y -le 25) -or ($x -eq 33 -and $y -ge 35 -and $y -le 36))){$outside++}}}}
 if($outside -or $alpha -or $changed -ne 26){throw 'Running state difference failed'}
 $state=[pscustomobject]@{ChangedRGBA=$changed;ChangedAlpha=$alpha;OutsideMask=$outside;Masks=@(@(16,22,6,4),@(33,35,1,2));Coordinates='top-left x,y,width,height'}
}finally{$a.Dispose();$b.Dispose()}
[pscustomobject]@{Sprites=$stats;RunningState=$state;GameIntegration='not performed'}|ConvertTo-Json -Depth 8|Set-Content (Join-Path $dir 'validation.json') -Encoding utf8
$stats|Format-Table File,Width,Height,Colors,CenterAlphaMask
$state|Format-List
