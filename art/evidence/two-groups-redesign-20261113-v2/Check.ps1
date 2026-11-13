$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path $PSScriptRoot 'native'
$alphabet='0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ'
$records=@()
foreach($file in Get-ChildItem $dir -Filter '*.pixels.json'){
 $m=Get-Content $file.FullName -Raw|ConvertFrom-Json;$png=$file.FullName.Replace('.pixels.json','.png');$b=[Drawing.Bitmap]::FromFile($png)
 try{
  if($b.Width -ne $m.Width -or $b.Height -ne $m.Height){throw 'Size mismatch'}
  $colors=[Collections.Generic.HashSet[string]]::new();$opaque=0;$l=$b.Width;$t=$b.Height;$r=-1;$bottom=-1
  for($y=0;$y -lt $b.Height;$y++){if($m.Rows[$y].Length -ne $b.Width){throw 'Invalid matrix row'};for($x=0;$x -lt $b.Width;$x++){
   $c=$b.GetPixel($x,$y);$s=$m.Rows[$y][$x];$wanted=if($s -eq '.'){[Drawing.Color]::FromArgb(0,0,0,0)}else{[Drawing.ColorTranslator]::FromHtml('#'+$m.Palette[$alphabet.IndexOf($s)])}
   if($c.ToArgb() -ne $wanted.ToArgb()){throw "Matrix mismatch $png $x,$y"}
   if($c.A -notin @(0,255)){throw 'Non-binary alpha'}
   if($c.A){$opaque++;[void]$colors.Add(('{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B));$l=[Math]::Min($l,$x);$t=[Math]::Min($t,$y);$r=[Math]::Max($r,$x);$bottom=[Math]::Max($bottom,$y)}elseif($c.R -or $c.G -or $c.B){throw 'Transparent RGB not zero'}
  }}
  $name=[IO.Path]::GetFileNameWithoutExtension($png)
  if($name -match '^(ore|light-salt-core)-'){
   if($b.Width -ne 48 -or $b.Height -ne 64 -or $l -lt 12 -or $r -gt 27 -or $t -lt 22 -or $bottom -ne 37){throw "Growth placement $name"}
   if($name.StartsWith('ore-') -and (($bottom-$t) -le ($r-$l))){throw 'Ore must be upright'}
  }
  $records+=@{Name=$name;Size=@($b.Width,$b.Height);Colors=$colors.Count;Opaque=$opaque;Bounds=@($l,$t,$r,$bottom);BinaryAlpha=$true;TransparentRgbZero=$true;MatrixMatches=$true;SHA256=(Get-FileHash $png -Algorithm SHA256).Hash}
 }finally{$b.Dispose()}
}
$diffs=@()
foreach($pair in @(@('crafting-station','crafting-station-running'),@('mineral-growth-station','mineral-growth-station-running'))){
 $aPath=Join-Path $dir ($pair[0]+'.png');if(!(Test-Path $aPath)){$aPath=Join-Path $PSScriptRoot '../../../src/CorpPresence/assets/machine-redesign-20261113-clean/mineral-growth-station.png'}
 $a=[Drawing.Bitmap]::FromFile($aPath);$b=[Drawing.Bitmap]::FromFile((Join-Path $dir ($pair[1]+'.png')));$changed=@()
 try{for($y=0;$y -lt $a.Height;$y++){for($x=0;$x -lt $a.Width;$x++){$p=$a.GetPixel($x,$y);$q=$b.GetPixel($x,$y);if($p.A -ne $q.A){throw 'State alpha mismatch'};if($p.ToArgb() -ne $q.ToArgb()){$changed+=,@($x,$y)}}};$diffs+=@{Base=$pair[0];State=$pair[1];Changed=$changed.Count;AlphaChanged=0;PixelMask=$changed}}finally{$a.Dispose();$b.Dispose()}
}
@{Status='pending-user-review';Sprites=$records;StateDiffs=$diffs;GameTested=$false}|ConvertTo-Json -Depth 8|Set-Content (Join-Path $PSScriptRoot 'validation.json') -Encoding utf8
$records|Select-Object Name,Size,Colors,Bounds|Format-Table
$diffs|Select-Object Base,Changed,AlphaChanged|Format-Table
