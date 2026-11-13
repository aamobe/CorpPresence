$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path $PSScriptRoot 'native'
$alphabet='0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ'
function Fill($x,$y,$w,$h,$color){for($j=$y;$j -lt $y+$h;$j++){for($i=$x;$i -lt $x+$w;$i++){$script:rows[$j][$i]=[char]$color}}}
function Save($name,$data){
 $data.Rows=@($script:rows|ForEach-Object{-join $_})
 $data|ConvertTo-Json -Depth 5|Set-Content -Encoding utf8 (Join-Path $dir "$name.pixels.json")
 $im=[Drawing.Bitmap]::new([int]$data.Width,[int]$data.Height)
 for($y=0;$y -lt $data.Height;$y++){for($x=0;$x -lt $data.Width;$x++){$c=$script:rows[$y][$x];if($c -ne '.'){$im.SetPixel($x,$y,[Drawing.ColorTranslator]::FromHtml('#'+$data.Palette[$alphabet.IndexOf($c)]))}}}
 $im.Save((Join-Path $dir "$name.png"));$im.Dispose()
}
$masks=@{ 'mineral-crusher'=@(39,12,3,1); 'powder-sieve'=@(43,23,2,1); 'sintering-station'=@(25,42,2,1) }
foreach($name in @('mineral-crusher','powder-sieve','sintering-station','mineral-growth-station','alcohol-preparation-station','twin-vessel-reactor')){
 $data=Get-Content -Raw (Join-Path $dir "$name.pixels.json")|ConvertFrom-Json
 $script:rows=@($data.Rows|ForEach-Object{,$_.ToCharArray()})
 switch($name){
 'mineral-crusher'{
  Fill 9 10 20 10 '1'
  foreach($x in @(10,20)){foreach($y in @(11,14,17)){Fill ($x+2) $y 4 1 '5';Fill $x ($y+1) 2 1 '4';Fill ($x+6) ($y+1) 2 1 '4';Fill ($x+2) ($y+2) 4 1 '1'}}
  Fill 8 20 20 1 '6';Fill 8 21 20 1 '5';Fill 38 20 7 7 '1';foreach($x in @(39,42,45)){Fill $x 20 1 7 '9'}
  Fill 14 32 9 3 'B';Fill 39 12 3 1 '1';Fill 35 29 10 3 '4'
 }
 'powder-sieve'{
  foreach($y in @(14,21,28)){Fill 12 $y 27 1 '4';Fill 12 ($y+1) 27 2 '1';foreach($x in @(14,17,20,23,26,29,32,35)){Fill $x ($y+1) 1 2 '9'};Fill 12 ($y+3) 27 1 '5';Fill 12 ($y+4) 27 1 '4'}
  Fill 43 23 2 1 '8'
 }
 'sintering-station'{Fill 16 28 17 7 '1';Fill 18 29 13 5 'C';Fill 18 29 13 1 'D';Fill 18 32 13 1 'D';Fill 16 35 18 1 '6';Fill 25 42 2 1 '8'}
 'mineral-growth-station'{}
 'alcohol-preparation-station'{}
 'twin-vessel-reactor'{Fill 29 8 6 2 '1';Fill 10 28 13 4 '8';Fill 10 28 13 1 '9';Fill 41 29 13 2 '6';Fill 42 29 11 1 '5';Fill 29 23 3 2 'C';Fill 35 23 2 2 'C'}
 }
 Save $name $data
 if($masks.ContainsKey($name)){$m=$masks[$name];Fill $m[0] $m[1] $m[2] $m[3] 'A';Save ($name+'-running') $data}
}
$records=@()
foreach($file in Get-ChildItem $dir -Filter '*.pixels.json'){
 $d=Get-Content -Raw $file.FullName|ConvertFrom-Json;$png=Join-Path $dir ($file.BaseName.Replace('.pixels','')+'.png');$im=[Drawing.Bitmap]::FromFile($png);$colors=@{};$opaque=0
 for($y=0;$y -lt $im.Height;$y++){for($x=0;$x -lt $im.Width;$x++){$c=$im.GetPixel($x,$y);if($c.A -notin @(0,255)){throw 'Invalid alpha'};if($c.A){$opaque++;$colors[$c.ToArgb()]=1};$ch=$d.Rows[$y][$x];$expected=if($ch -eq '.'){0}else{[Drawing.ColorTranslator]::FromHtml('#'+$d.Palette[$alphabet.IndexOf($ch)]).ToArgb()};if($expected -ne $c.ToArgb()){throw 'Matrix mismatch'}}}
 $records+=@{File=[IO.Path]::GetFileName($png);Width=$im.Width;Height=$im.Height;Colors=$colors.Count;OpaquePixels=$opaque;BinaryAlpha=$true;MatrixMatches=$true;SHA256=(Get-FileHash $png).Hash};$im.Dispose()
}
$states=@()
foreach($name in $masks.Keys){$a=[Drawing.Bitmap]::FromFile((Join-Path $dir "$name.png"));$b=[Drawing.Bitmap]::FromFile((Join-Path $dir "$name-running.png"));$m=$masks[$name];$changes=0;for($y=0;$y -lt $a.Height;$y++){for($x=0;$x -lt $a.Width;$x++){if($a.GetPixel($x,$y).ToArgb() -ne $b.GetPixel($x,$y).ToArgb()){if($x -lt $m[0] -or $x -ge $m[0]+$m[2] -or $y -lt $m[1] -or $y -ge $m[1]+$m[3]){throw 'Out of mask'};$changes++};if($a.GetPixel($x,$y).A -ne $b.GetPixel($x,$y).A){throw 'Alpha mismatch'}}};$states+=@{File="$name-running.png";Mask=$m;ChangedPixels=$changes;OutsideMask=0;AlphaChanges=0};$a.Dispose();$b.Dispose()}
@{Sprites=$records;States=$states;SourceSHA256=(Get-FileHash (Join-Path $PSScriptRoot 'approved-reference.png')).Hash}|ConvertTo-Json -Depth 8|Set-Content -Encoding utf8 (Join-Path $dir 'validation.json')
foreach($factor in @(1,4)){
 $preview=[Drawing.Bitmap]::new(376*$factor,64*$factor);$g=[Drawing.Graphics]::FromImage($preview);$g.Clear([Drawing.Color]::FromArgb(255,47,43,52));$g.InterpolationMode='NearestNeighbor';$g.PixelOffsetMode='Half';$left=0
 foreach($name in @('mineral-crusher','powder-sieve','sintering-station','mineral-growth-station','alcohol-preparation-station','twin-vessel-reactor')){$im=[Drawing.Bitmap]::FromFile((Join-Path $dir "$name.png"));$g.DrawImage($im,[Drawing.Rectangle]::new($left,0,$im.Width*$factor,$im.Height*$factor),0,0,$im.Width,$im.Height,[Drawing.GraphicsUnit]::Pixel);$left+=($im.Width+12)*$factor;$im.Dispose()};$g.Dispose();$preview.Save((Join-Path $dir "inspection-$($factor)x.png"));$preview.Dispose()
}
Write-Output 'Verified 9 native sprites, editable matrices, binary alpha, and 3 state masks.'
