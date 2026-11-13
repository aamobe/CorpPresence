$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path $PSScriptRoot 'native'
function Rect($x,$y,$w,$h,$c){for($j=$y;$j -lt $y+$h;$j++){for($i=$x;$i -lt $x+$w;$i++){$script:rows[$j][$i]=[char]$c}}}
foreach($file in Get-ChildItem $dir -Filter '*.pixels.json'){
 $data=Get-Content $file.FullName -Raw | ConvertFrom-Json
 $script:rows=@($data.Rows | ForEach-Object { ,$_.ToCharArray() })
 switch($file.Name){
  'powder-sieve.pixels.json' {
   # Straight support faces and three equally spaced sieve lattices.
   Rect 3 16 3 19 '4';Rect 40 29 3 6 '4'
   foreach($y in @(17,23,29)){Rect 14 $y 21 2 '9';foreach($x in @(14,17,20,23,26,29,32)){Rect $x $y 1 2 '1'}}
  }
  'mineral-crusher.pixels.json' {
   # Preserve toothed silhouette, consolidate flat roller recesses and motor vents.
   Rect 11 19 5 2 '1';Rect 20 19 5 2 '1'
   Rect 35 21 8 6 '1';foreach($x in @(35,37,39,41)){Rect $x 21 1 6 '9'}
   Rect 32 35 11 4 '1'
  }
  'sintering-station.pixels.json' {
   # Two continuous load-bearing posts, controlled highlight and dark outer edges.
   Rect 6 22 1 17 '3';Rect 7 22 2 17 '4';Rect 38 22 2 17 '4';Rect 40 22 1 17 '3'
   Rect 15 42 18 3 '8';Rect 21 42 1 2 'A';Rect 26 42 1 2 'A';Rect 21 44 6 1 'A'
  }
  'alcohol-preparation-station.pixels.json' {
   # Mash and boiler faces use contiguous colors; retain cap highlights and pipe elbows.
   Rect 14 8 9 2 'C';Rect 16 7 5 1 'C';Rect 12 11 10 1 '3'
   Rect 13 24 14 2 '5';Rect 12 34 16 2 '4'
  }
  'twin-vessel-reactor.pixels.json' {
   # Coherent liquid windows, two readable gauges, and three stable display slots.
   Rect 10 30 12 3 '9';Rect 10 29 10 1 'A';Rect 11 31 1 1 'A'
   Rect 43 30 12 3 '6';Rect 43 29 12 1 '7'
   Rect 27 31 10 3 '1';foreach($x in @(28,31,34)){Rect $x 31 1 3 '9'}
   Rect 27 25 2 1 '7';Rect 34 25 2 1 '7';Rect 28 25 1 1 'C';Rect 35 25 1 1 'C'
  }
 }
 $data.Rows=@($script:rows | ForEach-Object {-join $_})
 $data | ConvertTo-Json -Depth 5 | Set-Content $file.FullName -Encoding utf8
 $im=[Drawing.Bitmap]::new([int]$data.Width,[int]$data.Height)
 try{for($y=0;$y -lt $data.Height;$y++){for($x=0;$x -lt $data.Width;$x++){$ch=$data.Rows[$y][$x];$c=if($ch -eq '.'){[Drawing.Color]::FromArgb(0,0,0,0)}else{[Drawing.ColorTranslator]::FromHtml('#'+$data.Palette['0123456789ABCD'.IndexOf($ch)])};$im.SetPixel($x,$y,$c)}};$im.Save((Join-Path $dir ($file.Name.Replace('.pixels.json','.png'))))}finally{$im.Dispose()}
}
$names=@('mineral-crusher','powder-sieve','sintering-station','mineral-growth-station','alcohol-preparation-station','twin-vessel-reactor')
$one=[Drawing.Bitmap]::new(376,64);$big=[Drawing.Bitmap]::new(864,276)
$g=[Drawing.Graphics]::FromImage($one);$gb=[Drawing.Graphics]::FromImage($big)
try{
 $g.Clear([Drawing.Color]::FromArgb(255,45,42,48));$gb.Clear([Drawing.Color]::FromArgb(255,45,42,48))
 $gb.InterpolationMode='NearestNeighbor';$gb.PixelOffsetMode='Half'
 $x=0;$i=0
 foreach($name in $names){$im=[Drawing.Bitmap]::FromFile((Join-Path $dir ($name+'.png')));try{$g.DrawImageUnscaled($im,$x,0);$gb.DrawImage($im,[Drawing.Rectangle]::new($i*144,0,$im.Width*2,$im.Height*2),0,0,$im.Width,$im.Height,[Drawing.GraphicsUnit]::Pixel);$x+=$im.Width+12;$i++}finally{$im.Dispose()}}
 $one.Save((Join-Path $dir 'native-row.png'));$big.Save((Join-Path $dir 'inspection-final.png'))
}finally{$g.Dispose();$gb.Dispose();$one.Dispose();$big.Dispose()}
