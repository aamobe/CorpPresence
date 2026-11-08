param([string]$Root = (Join-Path $PSScriptRoot '..'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$Root=(Resolve-Path $Root).Path
$src=[Drawing.Bitmap]::FromFile((Join-Path $Root 'evidence/scaling-source.png'))
$palette=@('211D28','36343F','454953','5D626B','85868E','B2B7C0','8E604E','B5643D','EE9855','938074','BEAA98','B3DB62','E86A59','F2BF59','92A6B6','65584F') | ForEach-Object { [Drawing.ColorTranslator]::FromHtml("#$_") }
$phases=@(0,7,8,9,12,15);$outputs=@{}
try {
    foreach($phase in $phases) {
        $pixels=[int[]]::new(64*48)
        for($y=0;$y -lt 48;$y++){for($x=0;$x -lt 64;$x++){
            $c=$src.GetPixel(($x*16+$phase),($y*16+$phase))
            if($c.A -lt 192){continue}
            $best=$palette[0];$distance=[double]::MaxValue
            foreach($p in $palette){
                $d=2*[Math]::Pow(($c.R-$p.R),2)+3*[Math]::Pow(($c.G-$p.G),2)+[Math]::Pow(($c.B-$p.B),2)
                if($d -lt $distance){$distance=$d;$best=$p}
            }
            $pixels[$y*64+$x]=$best.ToArgb()
        }}
        $outputs[$phase]=$pixels
    }
} finally { $src.Dispose() }
$results=foreach($phase in $phases){
    $diff=0;for($i=0;$i -lt 3072;$i++){if($outputs[$phase][$i] -ne $outputs[8][$i]){$diff++}}
    [pscustomobject]@{SampleOffset=$phase;BaselineOffset=8;ChangedPixels=$diff;CanvasPixels=3072;ChangedPercent=[Math]::Round(100*$diff/3072,2)}
}
$results|ConvertTo-Json|Set-Content (Join-Path $Root 'evidence/scaling-phase-results.json') -Encoding utf8
$results|Format-Table
$sheet=[Drawing.Bitmap]::new(900,480);$g=[Drawing.Graphics]::FromImage($sheet);$font=[Drawing.Font]::new('Consolas',11)
try{
    $g.Clear([Drawing.Color]::FromArgb(255,45,42,48))
    $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
    for($j=0;$j -lt $phases.Count;$j++){
        $phase=$phases[$j];$im=[Drawing.Bitmap]::new(64,48)
        try{
            for($y=0;$y -lt 48;$y++){for($x=0;$x -lt 64;$x++){$im.SetPixel($x,$y,[Drawing.Color]::FromArgb($outputs[$phase][$y*64+$x]))}}
            $dx=($j%3)*300+12;$dy=[int][Math]::Floor($j/3.0)*240+8
            $g.DrawString("Sample offset $phase / 16",$font,[Drawing.Brushes]::White,$dx,$dy)
            $g.DrawImage($im,[Drawing.Rectangle]::new($dx,$dy+30,256,192),0,0,64,48,[Drawing.GraphicsUnit]::Pixel)
        }finally{$im.Dispose()}
    }
    $sheet.Save((Join-Path $Root 'evidence/scaling-phase-comparison.png'),[Drawing.Imaging.ImageFormat]::Png)
}finally{$g.Dispose();$font.Dispose();$sheet.Dispose()}
