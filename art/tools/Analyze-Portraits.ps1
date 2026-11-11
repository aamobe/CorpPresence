param([string]$Root=(Join-Path $PSScriptRoot '..'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$Root=(Resolve-Path $Root).Path
$folder=Join-Path $Root 'evidence/portraits'
$manifest=Get-Content (Join-Path $folder 'selected/manifest.json') -Raw|ConvertFrom-Json
$results=foreach($entry in $manifest){
    $im=[Drawing.Bitmap]::FromFile((Join-Path $folder "selected/$($entry.File)"))
    try{
        $colors=@{};$alpha=@{};$main=0;$xmin=$im.Width;$ymin=$im.Height;$xmax=-1;$ymax=-1;$bottom=0
        for($y=0;$y -lt $im.Height;$y++){for($x=0;$x -lt $im.Width;$x++){
            $c=$im.GetPixel($x,$y);$a=[int]$c.A;$alpha[$a]=1+$alpha[$a]
            if($a -lt 128){continue}
            $main++;$key='{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B;$colors[$key]=1+$colors[$key]
            $xmin=[Math]::Min($xmin,$x);$xmax=[Math]::Max($xmax,$x);$ymin=[Math]::Min($ymin,$y);$ymax=[Math]::Max($ymax,$y)
            if($y -eq $im.Height-1){$bottom++}
        }}
        [pscustomobject]@{
            Name=$entry.Name;PathId=$entry.PathId;File=$entry.File;Width=$im.Width;Height=$im.Height
            MainColors=$colors.Count;MainPixels=$main;Coverage=[Math]::Round($main/($im.Width*$im.Height),4)
            BoundingBox=@($xmin,$ymin,$xmax,$ymax);BottomPixels=$bottom
            AlphaHistogram=@($alpha.GetEnumerator()|Sort-Object Name|ForEach-Object{[pscustomobject]@{Alpha=$_.Key;Pixels=$_.Value}})
            Palette=@($colors.GetEnumerator()|Sort-Object Value -Descending|ForEach-Object{[pscustomobject]@{Hex=$_.Key;Pixels=$_.Value}})
            SHA256=$entry.SHA256;PPU=$entry.PPU;Pivot=$entry.Pivot
        }
    }finally{$im.Dispose()}
}
$results|ConvertTo-Json -Depth 8|Set-Content (Join-Path $folder 'portrait-metrics.json') -Encoding utf8
$results|Select-Object Name,PathId,Width,Height,MainColors,MainPixels,Coverage,BottomPixels|Export-Csv (Join-Path $folder 'portrait-metrics.csv') -Encoding utf8 -NoTypeInformation
$sheet=[Drawing.Bitmap]::new(960,([int][Math]::Ceiling($manifest.Count/3.0)*260))
$g=[Drawing.Graphics]::FromImage($sheet);$font=[Drawing.Font]::new('Consolas',10)
try{
    $g.Clear([Drawing.Color]::FromArgb(255,45,42,48))
    $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
    for($i=0;$i -lt $manifest.Count;$i++){
        $entry=$manifest[$i];$x=($i%3)*320+16;$y=[int][Math]::Floor($i/3.0)*260+8
        $g.DrawString("$($entry.Name) / $($entry.PathId)  1x",$font,[Drawing.Brushes]::White,$x,$y)
        $im=[Drawing.Bitmap]::FromFile((Join-Path $folder "selected/$($entry.File)"))
        try{$g.DrawRectangle([Drawing.Pens]::DimGray,$x,$y+24,288,211);$g.DrawImage($im,[Drawing.Rectangle]::new($x,$y+24,288,211),0,0,288,211,[Drawing.GraphicsUnit]::Pixel)}finally{$im.Dispose()}
    }
    $sheet.Save((Join-Path $folder 'portraits-native-sheet.png'),[Drawing.Imaging.ImageFormat]::Png)
}finally{$g.Dispose();$font.Dispose();$sheet.Dispose()}
$results|Select-Object Name,PathId,MainColors,Coverage,@{Name='BBox';Expression={$_.BoundingBox -join ','}},BottomPixels|Format-Table

$diffs=foreach($ids in @(@(1038,1771),@(1053,1060),@(1037,1133),@(1142,1805))){
    $ea=$manifest|Where-Object PathId -EQ $ids[0];$eb=$manifest|Where-Object PathId -EQ $ids[1]
    $a=[Drawing.Bitmap]::FromFile((Join-Path $folder "selected/$($ea.File)"));$b=[Drawing.Bitmap]::FromFile((Join-Path $folder "selected/$($eb.File)"))
    try{
        $diff=0;$alpha=0;$xmin=288;$ymin=211;$xmax=-1;$ymax=-1
        for($y=0;$y -lt 211;$y++){for($x=0;$x -lt 288;$x++){
            $ca=$a.GetPixel($x,$y);$cb=$b.GetPixel($x,$y)
            if($ca.A -ne $cb.A){$alpha++}
            if($ca.ToArgb() -ne $cb.ToArgb()){
                $diff++;$xmin=[Math]::Min($xmin,$x);$xmax=[Math]::Max($xmax,$x);$ymin=[Math]::Min($ymin,$y);$ymax=[Math]::Max($ymax,$y)
            }
        }}
        [pscustomobject]@{Name=$ea.Name;From=$ids[0];To=$ids[1];RGBA=$diff;Alpha=$alpha;Bounds=@($xmin,$ymin,$xmax,$ymax)}
    }finally{$a.Dispose();$b.Dispose()}
}
$diffs|ConvertTo-Json -Depth 4|Set-Content (Join-Path $folder 'native-state-diffs.json') -Encoding utf8

# Fixed crop, same source-coordinate region for both states. Analysis only.
$ids=@(1038,1771,1053,1060)
$sheet=[Drawing.Bitmap]::new(800,900);$g=[Drawing.Graphics]::FromImage($sheet);$font=[Drawing.Font]::new('Consolas',11)
try{
    $g.Clear([Drawing.Color]::FromArgb(255,45,42,48));$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
    for($i=0;$i -lt $ids.Count;$i++){
        $e=$manifest|Where-Object PathId -EQ $ids[$i];$im=[Drawing.Bitmap]::FromFile((Join-Path $folder "selected/$($e.File)"))
        try{
            $x=($i%2)*400+12;$y=[int][Math]::Floor($i/2.0)*450+8
            $g.DrawString("$($e.Name) / $($e.PathId)`nSame crop (100,40,90,100), x4",$font,[Drawing.Brushes]::White,$x,$y)
            $g.DrawImage($im,[Drawing.Rectangle]::new($x,$y+40,360,400),100,40,90,100,[Drawing.GraphicsUnit]::Pixel)
        }finally{$im.Dispose()}
    }
    $sheet.Save((Join-Path $folder 'native-face-details.png'),[Drawing.Imaging.ImageFormat]::Png)
}finally{$g.Dispose();$font.Dispose();$sheet.Dispose()}
