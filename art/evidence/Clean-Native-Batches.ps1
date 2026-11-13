$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$assets=(Resolve-Path (Join-Path $PSScriptRoot '../../src/CorpPresence/assets')).Path
$alphabet='0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ'
function Remap($x,$y,$w,$h,$from,$to){for($j=$y;$j -lt $y+$h;$j++){for($i=$x;$i -lt $x+$w;$i++){if($from.Contains([string]$script:rows[$j][$i])){$script:rows[$j][$i]=[char]$to}}}}
function Paint($x,$y,$w,$h,$c){for($j=$y;$j -lt $y+$h;$j++){for($i=$x;$i -lt $x+$w;$i++){if($script:rows[$j][$i] -ne '.' -and $script:rows[$j][$i] -ne '0'){$script:rows[$j][$i]=[char]$c}}}}
function SaveSprite($data,$name,$dest){
 $data.Rows=@($script:rows | ForEach-Object {-join $_})
 $data|ConvertTo-Json -Depth 5|Set-Content (Join-Path $dest ($name+'.pixels.json')) -Encoding utf8
 $im=[Drawing.Bitmap]::new([int]$data.Width,[int]$data.Height)
 try{for($y=0;$y -lt $data.Height;$y++){for($x=0;$x -lt $data.Width;$x++){$ch=$data.Rows[$y][$x];$c=if($ch -eq '.'){[Drawing.Color]::FromArgb(0,0,0,0)}else{[Drawing.ColorTranslator]::FromHtml('#'+$data.Palette[$alphabet.IndexOf($ch)])};$im.SetPixel($x,$y,$c)}};$im.Save((Join-Path $dest ($name+'.png')))}finally{$im.Dispose()}
}
$groups=@('machine-redesign-20261113','powder-crafting-20261113','mineral-growth-stages-20261113')
$records=@()
foreach($group in $groups){
 $source=Join-Path $assets $group;$dest=Join-Path $assets ($group+'-clean');New-Item -ItemType Directory -Force $dest | Out-Null
 foreach($file in Get-ChildItem $source -Filter '*.pixels.json'){
  $name=$file.Name.Replace('.pixels.json','');if($name -eq 'mineral-growth-station-running'){continue}
  $d=Get-Content $file.FullName -Raw|ConvertFrom-Json;$before=@($d.Rows);$script:rows=@($d.Rows | ForEach-Object {,$_.ToCharArray()})
  switch($name){
   'mineral-crusher' {
    Remap 6 23 25 2 '248B' '3';Remap 3 24 2 8 '348' '2'
    Remap 35 11 8 7 '67' '5';Paint 36 11 6 1 '6'
    Remap 32 31 11 3 '2' '3';Remap 10 29 11 1 '3' '5'
    foreach($x in @(10,19)){Remap $x 13 7 7 '8' '1';Remap $x 13 7 7 '5' '4'}
   }
   'powder-sieve' {
    Remap 18 11 10 3 '3' '4';Remap 16 7 15 1 '7' '6';Remap 9 38 29 1 '7' '6'
    Remap 4 8 4 4 '38' '4';Remap 39 8 4 4 '238' '4'
    Remap 3 36 3 4 '23' '4';Remap 40 36 3 4 '23' '4'
   }
   'sintering-station' {
    Remap 10 13 5 4 '48' '2';Remap 32 13 5 4 '48' '2'
    Remap 11 22 26 16 'B' '2';Remap 4 41 6 5 'B8' '2';Remap 38 41 6 5 'B8' '2'
    Remap 15 26 18 1 '7' '6';Remap 19 4 10 1 '7' '6'
   }
   'mineral-growth-station' {
    Remap 16 3 10 3 '57' '6';Remap 15 17 11 1 '34' '2'
    Remap 14 10 13 3 '2' '4';Remap 12 53 15 1 '67' '6'
    Remap 35 17 4 3 '48' '9';Remap 35 49 4 3 '9' '1'
   }
   'alcohol-preparation-station' {
    Remap 14 7 9 1 '2' 'C';Remap 10 25 20 3 '7' '6'
    Remap 6 28 25 3 '3' '2';Remap 12 32 16 4 '3' '4'
    Remap 35 23 4 10 '83' '9';foreach($y in @(25,29)){Remap 35 $y 4 1 '9' 'A'}
   }
   'twin-vessel-reactor' {
    Remap 8 17 14 2 '3' '5';Remap 42 17 13 2 '3' '5'
    Remap 8 23 14 2 '2' '3';Remap 42 23 13 2 '2' '3'
    Remap 8 36 14 1 '2' '3';Remap 42 36 13 1 '2' '3'
    Remap 8 33 14 2 '458' '1';Remap 42 33 13 2 '458' '1'
    Remap 27 25 3 1 'D' '7'
   }
   'silicon-plate' {Remap 6 7 15 1 '3' '4';Remap 6 22 18 1 '5' '4'}
   'electronic-substrate' {Remap 2 6 3 4 '6' 'B';Remap 26 6 3 4 '67' 'B'}
   'electronic-blueprint' {Remap 2 3 1 9 '4' '2';Remap 6 2 4 1 '3' '4'}
   {$_ -in @('crafting-station','crafting-station-running')} {
    Remap 21 10 5 2 '5' '2';Remap 27 10 5 2 '5' '2';Remap 33 10 4 2 '5' '2'
    Remap 21 16 16 3 '2' '3';Remap 3 20 2 9 '5' '4'
    Remap 13 36 18 1 '3' '5';Remap 32 38 8 1 '3' '2'
    Remap 7 40 3 1 '7' '5'
   }
   'ore-growing' {
    # Palette here is per-sprite; merge two tiny intermediate tones into existing gray facets.
    Remap 18 31 5 6 '3' '5'
   }
   'ore-mature' {Remap 17 24 7 12 '3' '1';Remap 17 28 7 9 '7' '4'}
   'light-salt-core-growing' { }
   'light-salt-core-mature' { }
  }
  $changed=0;for($y=0;$y -lt $d.Height;$y++){for($x=0;$x -lt $d.Width;$x++){if(($before[$y][$x] -eq '.') -ne ($script:rows[$y][$x] -eq '.')){throw 'Silhouette changed'};if($before[$y][$x] -ne $script:rows[$y][$x]){$changed++}}}
  SaveSprite $d $name $dest
  $records += [pscustomobject]@{Group=$group;File=$name+'.png';ChangedPixels=$changed;AlphaDifferences=0;Width=$d.Width;Height=$d.Height;SHA256=(Get-FileHash (Join-Path $dest ($name+'.png'))).Hash}
 }
}
# Rebase the chamber-only running light on the cleaned machine. Cultivation anchor and pedestal stay locked.
$dest=Join-Path $assets 'mineral-growth-stages-20261113-clean'
$d=Get-Content (Join-Path $assets 'machine-redesign-20261113-clean/mineral-growth-station.pixels.json') -Raw|ConvertFrom-Json
$d.Palette+= '45918B';$script:rows=@($d.Rows|ForEach-Object {,$_.ToCharArray()})
for($y=22;$y -le 44;$y++){for($x=9;$x -le 29;$x++){if($script:rows[$y][$x] -eq '8'){$script:rows[$y][$x]='9'}elseif($script:rows[$y][$x] -eq '9'){$script:rows[$y][$x]='E'}}}
SaveSprite $d 'mineral-growth-station-running' $dest
$records|ConvertTo-Json -Depth 5|Set-Content (Join-Path $PSScriptRoot 'cleanup-validation.json') -Encoding utf8
$records|Format-Table Group,File,ChangedPixels
