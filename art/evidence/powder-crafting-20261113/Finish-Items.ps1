$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path $PSScriptRoot 'native'
function Paint($x,$y,$w,$h,$c){for($j=$y;$j -lt $y+$h;$j++){for($i=$x;$i -lt $x+$w;$i++){$script:rows[$j][$i]=[char]$c}}}
foreach($name in @('silicon-plate','electronic-substrate','electronic-blueprint')){
 $p=Join-Path $dir ($name+'.pixels.json');$d=Get-Content $p -Raw|ConvertFrom-Json
 $script:rows=@($d.Rows | ForEach-Object {,$_.ToCharArray()})
 switch($name){
  'silicon-plate' {
   # Remove isolated color changes on the flat face and keep a controlled border.
   Paint 20 6 1 1 '7';Paint 7 10 1 10 '4';Paint 23 11 1 10 '5'
  }
  'electronic-substrate' {
   # Continuous upper outline; three equal warm-metal contact pads.
   Paint 3 2 26 1 '0';foreach($x in @(5,14,23)){Paint $x 12 4 1 'B'}
  }
  'electronic-blueprint' {
   # Restore the circuit symbol and both tracks on the native 16px grid.
   Paint 6 7 5 3 '7';Paint 7 8 3 1 '9';Paint 4 8 2 1 '7';Paint 11 8 2 1 '7'
   Paint 4 4 2 1 '9';Paint 10 4 2 1 '9'
  }
 }
 $d.Rows=@($script:rows|ForEach-Object {-join $_});$d|ConvertTo-Json -Depth 5|Set-Content $p -Encoding utf8
 $im=[Drawing.Bitmap]::new([int]$d.Width,[int]$d.Height)
 try{for($y=0;$y -lt $d.Height;$y++){for($x=0;$x -lt $d.Width;$x++){$ch=$d.Rows[$y][$x];$c=if($ch -eq '.'){[Drawing.Color]::FromArgb(0,0,0,0)}else{[Drawing.ColorTranslator]::FromHtml('#'+$d.Palette['0123456789ABCDEF'.IndexOf($ch)])};$im.SetPixel($x,$y,$c)}};$im.Save((Join-Path $dir ($name+'.png')))}finally{$im.Dispose()}
}
