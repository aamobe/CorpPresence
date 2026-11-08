param([string]$Root = (Join-Path $PSScriptRoot '..'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$Root = (Resolve-Path $Root).Path
$records = foreach ($file in Get-ChildItem (Join-Path $Root 'evidence/native/Sprite') -Filter '*.png' | Sort-Object Name) {
    $im = [Drawing.Bitmap]::FromFile($file.FullName)
    try {
        $palette = @{}; $alphas = @{}; $visible = 0; $isolated = 0
        $minX = $im.Width; $minY = $im.Height; $maxX = -1; $maxY = -1
        for ($y=0; $y -lt $im.Height; $y++) { for ($x=0; $x -lt $im.Width; $x++) {
            $c = $im.GetPixel($x,$y); $a = [int]$c.A; $alphas[$a] = 1 + $alphas[$a]
            if ($a -lt 128) { continue }
            $visible++; $hex = '{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B
            $palette[$hex] = 1 + $palette[$hex]
            $minX=[Math]::Min($x,$minX); $maxX=[Math]::Max($x,$maxX)
            $minY=[Math]::Min($y,$minY); $maxY=[Math]::Max($y,$maxY)
            $same = $false
            foreach ($offset in @(@(-1,0),@(1,0),@(0,-1),@(0,1))) {
                $nx=$x+$offset[0]; $ny=$y+$offset[1]
                if ($nx -lt 0 -or $ny -lt 0 -or $nx -ge $im.Width -or $ny -ge $im.Height) { continue }
                $n=$im.GetPixel($nx,$ny)
                if ($n.A -ge 128 -and $n.R -eq $c.R -and $n.G -eq $c.G -and $n.B -eq $c.B) { $same=$true; break }
            }
            if (-not $same) { $isolated++ }
        }}
        $ordered = @($palette.GetEnumerator() | Sort-Object Value -Descending)
        [pscustomobject]@{
            Name=$file.BaseName; Width=$im.Width; Height=$im.Height
            Grid16Aligned=($im.Width%16 -eq 0 -and $im.Height%16 -eq 0)
            MainColors=$palette.Count; MainPixels=$visible
            MainBoundingBox=@($minX,$minY,$maxX,$maxY)
            MainCoverage=[Math]::Round($visible/($im.Width*$im.Height),4)
            IsolatedSameColorPixels=$isolated
            IsolatedFraction=if($visible){[Math]::Round($isolated/$visible,4)}else{0}
            AlphaHistogram=@($alphas.GetEnumerator() | Sort-Object Name | ForEach-Object { [pscustomobject]@{Alpha=$_.Key; Pixels=$_.Value} })
            Palette=@($ordered | ForEach-Object { [pscustomobject]@{Hex=$_.Key;Pixels=$_.Value} })
            SHA256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        }
    } finally { $im.Dispose() }
}
$records | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $Root 'evidence/sprite-metrics.json') -Encoding utf8
$records | Select-Object Name,Width,Height,Grid16Aligned,MainColors,MainPixels,MainCoverage,IsolatedSameColorPixels,IsolatedFraction | Export-Csv (Join-Path $Root 'evidence/sprite-metrics.csv') -NoTypeInformation -Encoding utf8
$records | Group-Object Width,Height | ForEach-Object {
    $colors=$_.Group.MainColors | Measure-Object -Minimum -Maximum -Average
    [pscustomobject]@{Size=$_.Name;Count=$_.Count;ColorsMin=$colors.Minimum;ColorsMax=$colors.Maximum;ColorsMean=[Math]::Round($colors.Average,2)}
} | Sort-Object Size | Format-Table

# Diagnostic contact sheet only: source PNGs are never rewritten.
$names=@('meat_fish_tiny1','rat_s_m','injector_pink_alt','battery100','rat_m_m','tilapia_small1','meat_fish_small1','fish_food','portable_water_purifier','tilapia1','revolver','bottle_printer','tilapia_large1','rat_xxl_f','furnace','furnace_on','gibber','animal_cage','AQUACULTURE','AQUACULTURE_II')
$names=@($names | Where-Object { Test-Path (Join-Path $Root "evidence/native/Sprite/$_.png") })
$sheet=[Drawing.Bitmap]::new(1200,([int][Math]::Ceiling($names.Count/4.0)*240))
$g=[Drawing.Graphics]::FromImage($sheet); $font=[Drawing.Font]::new('Consolas',11)
try {
    $g.Clear([Drawing.Color]::FromArgb(255,45,42,48))
    $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
    for($i=0;$i -lt $names.Count;$i++) {
        $name=$names[$i];$im=[Drawing.Bitmap]::FromFile((Join-Path $Root "evidence/native/Sprite/$name.png"))
        try {
            $x=($i%4)*300+12;$y=[int][Math]::Floor($i/4.0)*240+8
            $g.DrawString("$name`n$($im.Width)x$($im.Height), preview x4",$font,[Drawing.Brushes]::White,$x,$y)
            $g.DrawImage($im,[Drawing.Rectangle]::new($x,$y+42,$im.Width*4,$im.Height*4),0,0,$im.Width,$im.Height,[Drawing.GraphicsUnit]::Pixel)
        } finally { $im.Dispose() }
    }
    $sheet.Save((Join-Path $Root 'evidence/native-contact-sheet.png'),[Drawing.Imaging.ImageFormat]::Png)
} finally { $g.Dispose();$font.Dispose();$sheet.Dispose() }
