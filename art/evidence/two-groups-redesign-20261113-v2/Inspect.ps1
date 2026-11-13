$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path $PSScriptRoot 'native'
$bg=[Drawing.Color]::FromArgb(255,45,42,48)
function Row($names,$name,$growth){
 $width=if($growth){228}else{224};$height=if($growth){64}else{48}
 $row=[Drawing.Bitmap]::new($width,$height);$g=[Drawing.Graphics]::FromImage($row);$g.Clear($bg);$x=0
 try{
  foreach($n in $names){$path=Join-Path $dir ($n+'.png');if(!(Test-Path $path)){continue};$im=[Drawing.Bitmap]::FromFile($path)
   try{
    if($growth){$machine=if($n.EndsWith('growing')){Join-Path $dir 'mineral-growth-station-running.png'}else{Join-Path $PSScriptRoot '../../../src/CorpPresence/assets/machine-redesign-20261113-clean/mineral-growth-station.png'};$b=[Drawing.Bitmap]::FromFile($machine);try{$g.DrawImageUnscaled($b,$x,0)}finally{$b.Dispose()}}
    $g.DrawImageUnscaled($im,$x,0);$x+=$im.Width+12
   }finally{$im.Dispose()}
  }
  $row.Save((Join-Path $PSScriptRoot ($name+'-native.png')))
  $big=[Drawing.Bitmap]::new(($width*4),($height*4));$gb=[Drawing.Graphics]::FromImage($big)
  try{$gb.InterpolationMode='NearestNeighbor';$gb.PixelOffsetMode='Half';$gb.DrawImage($row,[Drawing.Rectangle]::new(0,0,$big.Width,$big.Height),0,0,$width,$height,[Drawing.GraphicsUnit]::Pixel);$big.Save((Join-Path $PSScriptRoot ($name+'-4x.png')))}finally{$gb.Dispose();$big.Dispose()}
 }finally{$g.Dispose();$row.Dispose()}
}
Row @('silicon-plate','electronic-substrate','electronic-blueprint','crafting-station','crafting-station-running') 'crafting' $false
Row @('ore-growing','ore-mature','light-salt-core-growing','light-salt-core-mature') 'growth' $true
