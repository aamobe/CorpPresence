$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$destination = Join-Path $PSScriptRoot 'native'
New-Item -ItemType Directory -Force $destination | Out-Null
$source = [Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot 'approved-reference.png'))
$palette = @('292234','303A41','6B5269','777081','80929B','A8AEA5','C4C4B0','D5D2B8','275654','2F756F','8ABFBB','4A2F38','853737','C76A50')
$colors = @($palette | ForEach-Object { [Drawing.ColorTranslator]::FromHtml('#'+$_) })
$alphabet = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ'
# Square source sampling cells preserve proportions. Coordinates refer to the approved 2170×725 image.
$specs = @(
    @('mineral-crusher',48,48,0,184,5.7),
    @('powder-sieve',48,48,346,184,5.7),
    @('sintering-station',48,48,700,181,5.7),
    @('mineral-growth-station',48,64,1040,178,5.7),
    @('alcohol-preparation-station',48,48,1380,184,5.7),
    @('twin-vessel-reactor',64,48,1730,184,5.8)
)
$records = @()
foreach ($spec in $specs) {
    $name,$width,$height,$originX,$originY,$scale = $spec
    $bitmap = [Drawing.Bitmap]::new($width,$height)
    $rows = @()
    for ($y=0; $y -lt $height; $y++) {
        $row = ''
        for ($x=0; $x -lt $width; $x++) {
            $votes = [int[]]::new($colors.Count+1)
            for ($sy=[int][Math]::Ceiling($originY+$y*$scale); $sy -lt $originY+($y+1)*$scale; $sy++) {
                for ($sx=[int][Math]::Ceiling($originX+$x*$scale); $sx -lt $originX+($x+1)*$scale; $sx++) {
                    $c = $source.GetPixel($sx,$sy)
                    # The approved sheet background is neutral charcoal; the dark machine outline is purple.
                    if (($c.R -lt 62 -and $c.G -lt 60 -and $c.B -lt 65 -and [Math]::Abs([int]$c.R-$c.G) -lt 9 -and [Math]::Abs([int]$c.B-$c.R) -lt 9) -or ($c.R -gt 235 -and $c.G -gt 235 -and $c.B -gt 235)) {
                        $votes[$colors.Count]++; continue
                    }
                    $nearest=0; $distance=[double]::MaxValue
                    for($i=0;$i -lt $colors.Count;$i++) {
                        $p=$colors[$i]
                        $d=[Math]::Pow([int]$c.R-$p.R,2)+[Math]::Pow([int]$c.G-$p.G,2)+[Math]::Pow([int]$c.B-$p.B,2)
                        if($d -lt $distance){$distance=$d;$nearest=$i}
                    }
                    $votes[$nearest]++
                }
            }
            $winner=0
            for($i=1;$i -lt $votes.Length;$i++){if($votes[$i] -gt $votes[$winner]){$winner=$i}}
            if($winner -eq $colors.Count){$row+='.'}else{$row+=$alphabet[$winner];$bitmap.SetPixel($x,$y,$colors[$winner])}
        }
        $rows += $row
    }
    $bitmap.Save((Join-Path $destination "$name.png"))
    $bitmap.Dispose()
    @{Width=$width;Height=$height;Palette=$palette;Rows=$rows} | ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 (Join-Path $destination "$name.pixels.json")
    $records += @{Name=$name;Origin=@($originX,$originY);SourcePixelsPerNativePixel=$scale;Width=$width;Height=$height}
}
$source.Dispose()
$records | ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 (Join-Path $destination 'sampling.json')
# Inspection only: nearest-neighbor integer enlargement, kept outside runtime resources.
$preview=[Drawing.Bitmap]::new(376*4,64*4)
$g=[Drawing.Graphics]::FromImage($preview)
$g.Clear([Drawing.Color]::FromArgb(255,47,43,52))
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
$left=0
foreach($spec in $specs){$im=[Drawing.Bitmap]::FromFile((Join-Path $destination ($spec[0]+'.png')));$g.DrawImage($im,[Drawing.Rectangle]::new($left,0,$im.Width*4,$im.Height*4),0,0,$im.Width,$im.Height,[Drawing.GraphicsUnit]::Pixel);$left+=($im.Width+12)*4;$im.Dispose()}
$g.Dispose();$preview.Save((Join-Path $destination 'inspection-4x.png'));$preview.Dispose()
