param([string]$Root = (Join-Path $PSScriptRoot '..'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$Root=(Resolve-Path $Root).Path
$metrics=Get-Content (Join-Path $Root 'evidence/sprite-metrics.json') -Raw|ConvertFrom-Json
$diffs=foreach($pair in @(@('battery0','battery100'),@('furnace','furnace_on'))){
    $a=[Drawing.Bitmap]::FromFile((Join-Path $Root "evidence/native/Sprite/$($pair[0]).png"))
    $b=[Drawing.Bitmap]::FromFile((Join-Path $Root "evidence/native/Sprite/$($pair[1]).png"))
    try{
        if($a.Width -ne $b.Width -or $a.Height -ne $b.Height){throw 'State pair dimensions differ'}
        $rgba=0;$alpha=0
        for($y=0;$y -lt $a.Height;$y++){for($x=0;$x -lt $a.Width;$x++){
            $ca=$a.GetPixel($x,$y);$cb=$b.GetPixel($x,$y)
            if($ca.ToArgb() -ne $cb.ToArgb()){$rgba++}
            if($ca.A -ne $cb.A){$alpha++}
        }}
        [pscustomobject]@{From=$pair[0];To=$pair[1];Width=$a.Width;Height=$a.Height;ChangedRGBA=$rgba;ChangedAlpha=$alpha}
    }finally{$a.Dispose();$b.Dispose()}
}
$diffs|ConvertTo-Json|Set-Content (Join-Path $Root 'evidence/style-state-diffs.json') -Encoding utf8
$groups=@{
    machines=@('animal_cage','bottle_printer','portable_water_purifier','furnace','furnace_on','gibber')
    small=@('med_bottle_small_pink','meat_fish_tiny1','battery100','battery0','med_bottle_blue','med_bottle_red','black_bottle','injector_pink','injector_pink_alt')
}
foreach($group in $groups.Keys){
    $names=$groups[$group];$sheet=[Drawing.Bitmap]::new(1200,([int][Math]::Ceiling($names.Count/3.0)*320))
    $g=[Drawing.Graphics]::FromImage($sheet);$font=[Drawing.Font]::new('Consolas',11)
    try{
        $g.Clear([Drawing.Color]::FromArgb(255,45,42,48))
        $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
        for($i=0;$i -lt $names.Count;$i++){
            $name=$names[$i];$m=$metrics|Where-Object Name -EQ $name
            if(-not $m -or $m.MainPixels -eq 0){throw "No valid sample: $name"}
            $im=[Drawing.Bitmap]::FromFile((Join-Path $Root "evidence/native/Sprite/$name.png"))
            try{
                $x=($i%3)*400+12;$y=[int][Math]::Floor($i/3.0)*320+8
                $g.DrawString("$name  $($im.Width)x$($im.Height)`n$($m.MainColors) RGB colors (alpha >=128)",$font,[Drawing.Brushes]::White,$x,$y)
                $g.DrawString('1x',$font,[Drawing.Brushes]::LightGray,$x,$y+40)
                $g.DrawString('4x nearest',$font,[Drawing.Brushes]::LightGray,$x+90,$y+40)
                $g.DrawImage($im,[Drawing.Rectangle]::new($x,$y+64,$im.Width,$im.Height),0,0,$im.Width,$im.Height,[Drawing.GraphicsUnit]::Pixel)
                $g.DrawImage($im,[Drawing.Rectangle]::new($x+90,$y+64,$im.Width*4,$im.Height*4),0,0,$im.Width,$im.Height,[Drawing.GraphicsUnit]::Pixel)
                $j=0
                foreach($entry in $m.Palette){
                    $brush=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#'+$entry.Hex))
                    try{$g.FillRectangle($brush,($x+$j*14),($y+270),12,16)}finally{$brush.Dispose()}
                    $j++
                }
                $g.DrawString('Palette: most-used first',$font,[Drawing.Brushes]::LightGray,$x,$y+290)
            }finally{$im.Dispose()}
        }
        $sheet.Save((Join-Path $Root "evidence/style-$group.png"),[Drawing.Imaging.ImageFormat]::Png)
    }finally{$g.Dispose();$font.Dispose();$sheet.Dispose()}
}
