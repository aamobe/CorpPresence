$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path $PSScriptRoot 'native'
$machine=(Resolve-Path (Join-Path $PSScriptRoot '../../../src/CorpPresence/assets/machine-redesign-20261113/mineral-growth-station.png')).Path
$names=@('ore-growing','ore-mature','light-salt-core-growing','light-salt-core-mature')
$row=[Drawing.Bitmap]::new(228,64);$big=[Drawing.Bitmap]::new(768,256)
$g=[Drawing.Graphics]::FromImage($row);$gb=[Drawing.Graphics]::FromImage($big)
try{
 $bg=[Drawing.Color]::FromArgb(255,45,42,48);$g.Clear($bg);$gb.Clear($bg);$gb.InterpolationMode='NearestNeighbor';$gb.PixelOffsetMode='Half'
 for($i=0;$i -lt $names.Count;$i++){
  $path=Join-Path $dir ($names[$i]+'.png');if(!(Test-Path $path)){continue}
  $basePath=if($names[$i].EndsWith('growing')){Join-Path $dir 'mineral-growth-station-running.png'}else{$machine}
  $base=[Drawing.Bitmap]::FromFile($basePath);$overlay=[Drawing.Bitmap]::FromFile($path);$composite=[Drawing.Bitmap]::new($base)
  try{$gc=[Drawing.Graphics]::FromImage($composite);try{$gc.DrawImageUnscaled($overlay,0,0)}finally{$gc.Dispose()};$g.DrawImageUnscaled($composite,$i*60,0);$gb.DrawImage($composite,[Drawing.Rectangle]::new($i*192,0,192,256),0,0,48,64,[Drawing.GraphicsUnit]::Pixel)}finally{$base.Dispose();$overlay.Dispose();$composite.Dispose()}
 }
 $row.Save((Join-Path $dir 'stage-comparison-native.png'));$big.Save((Join-Path $dir 'stage-comparison-4x.png'))
}finally{$g.Dispose();$gb.Dispose();$row.Dispose();$big.Dispose()}
$pair=[Drawing.Bitmap]::new(108,64);$gp=[Drawing.Graphics]::FromImage($pair)
try{$gp.Clear([Drawing.Color]::FromArgb(255,45,42,48));$im=[Drawing.Bitmap]::FromFile($machine);try{$gp.DrawImageUnscaled($im,0,0)}finally{$im.Dispose()};$im=[Drawing.Bitmap]::FromFile((Join-Path $dir 'mineral-growth-station-running.png'));try{$gp.DrawImageUnscaled($im,60,0)}finally{$im.Dispose()};$pair.Save((Join-Path $dir 'idle-running-native.png'))}finally{$gp.Dispose();$pair.Dispose()}
