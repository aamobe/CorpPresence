$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path $PSScriptRoot 'native'
$alphabet='0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ'
function LoadMatrix($name){$script:m=Get-Content (Join-Path $dir ($name+'.pixels.json')) -Raw|ConvertFrom-Json;$script:rows=@($m.Rows|ForEach-Object{,@($_.ToCharArray())})}
function P($x,$y,$v){$script:rows[$y][$x]=[char]$v}
function FillRect($x,$y,$w,$h,$v){for($dy=0;$dy -lt $h;$dy++){for($dx=0;$dx -lt $w;$dx++){P ($x+$dx) ($y+$dy) $v}}}
function SaveMatrix($name){
 $m.Rows=@($rows|ForEach-Object{-join $_});$m|ConvertTo-Json -Depth 5|Set-Content (Join-Path $dir ($name+'.pixels.json')) -Encoding utf8
 $b=[Drawing.Bitmap]::new([int]$m.Width,[int]$m.Height)
 try{for($y=0;$y -lt $m.Height;$y++){for($x=0;$x -lt $m.Width;$x++){$v=$rows[$y][$x];if($v -ne '.'){$b.SetPixel($x,$y,[Drawing.ColorTranslator]::FromHtml('#'+$m.Palette[$alphabet.IndexOf($v)]))}}};$b.Save((Join-Path $dir ($name+'.png')))}finally{$b.Dispose()}
}
# The generated substrate has one stray bright sample on the upper edge.
LoadMatrix 'electronic-substrate'
P 22 4 '0'
SaveMatrix 'electronic-substrate'
# Reconnect the generated three-node drawing on the actual 16px grid.
LoadMatrix 'electronic-blueprint'
FillRect 4 5 10 9 '7'
FillRect 7 5 3 3 '8';P 8 6 '7';P 8 8 '8'
FillRect 6 9 6 1 '8'
FillRect 5 10 3 3 '8';P 6 11 '7'
FillRect 10 10 3 3 '8';P 11 11 '7'
P 2 11 'B'
SaveMatrix 'electronic-blueprint'
if(Test-Path (Join-Path $dir 'crafting-station.pixels.json')){
 LoadMatrix 'crafting-station'
 # Continuous rear rim, intentional separators, and a flat output drawer.
 FillRect 11 6 8 1 '6';FillRect 21 6 8 1 '6';FillRect 31 6 6 1 '6'
 FillRect 9 11 30 2 '3';FillRect 19 11 1 2 '2';FillRect 29 11 1 2 '2'
 P 5 12 '3';P 41 12 '3';P 39 9 '5';P 40 9 '5'
 P 10 37 '8'
 # Idle and running share the exact shell, crank, workpiece silhouette and paper.
 FillRect 38 4 2 2 '8'
 FillRect 21 18 4 3 '8';FillRect 21 21 4 1 '9'
 SaveMatrix 'crafting-station'
 FillRect 38 4 2 2 'A'
 FillRect 21 18 4 3 '9';FillRect 21 21 4 1 'D'
 SaveMatrix 'crafting-station-running'
}
