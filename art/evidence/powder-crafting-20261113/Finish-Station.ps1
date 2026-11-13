$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path $PSScriptRoot 'native'
$p=Join-Path $dir 'crafting-station.pixels.json';$d=Get-Content $p -Raw | ConvertFrom-Json
$script:rows=@($d.Rows | ForEach-Object {,$_.ToCharArray()})
function Paint($x,$y,$w,$h,$c){for($j=$y;$j -lt $y+$h;$j++){for($i=$x;$i -lt $x+$w;$i++){$script:rows[$j][$i]=[char]$c}}}
function Export($name){
 $d.Rows=@($script:rows | ForEach-Object {-join $_});$d|ConvertTo-Json -Depth 5|Set-Content (Join-Path $dir ($name+'.pixels.json')) -Encoding utf8
 $im=[Drawing.Bitmap]::new([int]$d.Width,[int]$d.Height)
 try{for($y=0;$y -lt $d.Height;$y++){for($x=0;$x -lt $d.Width;$x++){$ch=$d.Rows[$y][$x];$c=if($ch -eq '.'){[Drawing.Color]::FromArgb(0,0,0,0)}else{[Drawing.ColorTranslator]::FromHtml('#'+$d.Palette['0123456789ABCDEF'.IndexOf($ch)])};$im.SetPixel($x,$y,$c)}};$im.Save((Join-Path $dir ($name+'.png')))}finally{$im.Dispose()}
}
# Redraw the schematic as a readable symbol, and consolidate worktop/patch surfaces.
Paint 5 8 12 7 '9';Paint 7 10 4 3 '7';Paint 8 11 2 1 '9';Paint 6 11 1 1 '7';Paint 11 11 4 1 '7';Paint 14 9 1 2 '7'
Paint 6 22 2 7 '7';Paint 4 36 2 4 '3';Paint 6 38 2 3 '3'
# Stable central jig and inactive lamp. Keep the lever and fixture alignment unchanged.
Paint 16 22 6 4 '9';Paint 17 23 4 2 '8';Paint 33 35 1 2 '8'
Export 'crafting-station'
# Generic operating mask: illuminated fixture plus the front work indicator.
Paint 16 22 6 4 'A';Paint 17 23 4 2 '9';Paint 33 35 1 2 'A'
Export 'crafting-station-running'
