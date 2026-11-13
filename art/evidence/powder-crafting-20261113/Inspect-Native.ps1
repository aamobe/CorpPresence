$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dir=Join-Path $PSScriptRoot 'native'
$names=@('silicon-plate','electronic-substrate','electronic-blueprint','crafting-station','crafting-station-running')
$row=[Drawing.Bitmap]::new(224,48);$sheet=[Drawing.Bitmap]::new(1050,270)
$g=[Drawing.Graphics]::FromImage($row);$gs=[Drawing.Graphics]::FromImage($sheet);$font=[Drawing.Font]::new('Consolas',9)
try{
 $bg=[Drawing.Color]::FromArgb(255,45,42,48);$g.Clear($bg);$gs.Clear($bg);$gs.InterpolationMode='NearestNeighbor';$gs.PixelOffsetMode='Half'
 $x=0;$i=0
 foreach($name in $names){$path=Join-Path $dir ($name+'.png');if(!(Test-Path $path)){continue};$im=[Drawing.Bitmap]::FromFile($path)
  try{$g.DrawImageUnscaled($im,$x,0);$gs.DrawString($name,$font,[Drawing.Brushes]::White,$i*210,0);$gs.DrawImage($im,[Drawing.Rectangle]::new($i*210,32,$im.Width*4,$im.Height*4),0,0,$im.Width,$im.Height,[Drawing.GraphicsUnit]::Pixel);$x+=$im.Width+12;$i++}finally{$im.Dispose()}}
 $row.Save((Join-Path $dir 'native-row.png'));$sheet.Save((Join-Path $dir 'inspection-4x.png'))
}finally{$g.Dispose();$gs.Dispose();$font.Dispose();$row.Dispose();$sheet.Dispose()}
