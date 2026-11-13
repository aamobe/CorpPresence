param([string]$OutputDirectory = $PSScriptRoot)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function Fill-Pixels($image, $x, $y, $width, $height, $hex, $alpha = 255) {
    $rgb = [Drawing.ColorTranslator]::FromHtml("#$hex")
    $color = [Drawing.Color]::FromArgb($alpha, $rgb)
    for ($row = $y; $row -lt $y + $height; $row++) {
        for ($col = $x; $col -lt $x + $width; $col++) {
            $image.SetPixel($col, $row, $color)
        }
    }
}


$body = [Drawing.Bitmap]::new(48, 48, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    # Lid, beveled box rim and side walls.
    Fill-Pixels $body 6 5 36 39 '292234'
    Fill-Pixels $body 3 7 42 35 '292234'
    Fill-Pixels $body 2 10 44 5 '292234'
    Fill-Pixels $body 7 6 34 2 '80929B'
    Fill-Pixels $body 4 8 40 3 'A8AEA5'
    Fill-Pixels $body 7 8 34 1 'C4C4B0'
    Fill-Pixels $body 3 11 42 2 'D5D2B8'
    Fill-Pixels $body 3 13 42 2 '5F6868'
    Fill-Pixels $body 4 15 40 26 '5F6868'
    Fill-Pixels $body 6 15 36 25 '80929B' 112
    Fill-Pixels $body 5 15 1 24 '80929B'
    Fill-Pixels $body 42 15 1 24 '80929B'
    Fill-Pixels $body 6 16 2 9 'A8AEA5'
    Fill-Pixels $body 7 16 1 4 'D5D2B8'
    Fill-Pixels $body 40 20 2 10 'A8AEA5'
    Fill-Pixels $body 6 40 36 1 '80929B'
    Fill-Pixels $body 7 41 34 2 '44464C'

    # Front latch.
    Fill-Pixels $body 19 11 10 7 '44464C'
    Fill-Pixels $body 20 11 8 2 'D5D2B8'
    Fill-Pixels $body 20 13 8 2 'A8AEA5'
    Fill-Pixels $body 20 15 8 2 '79746E'
    $body.Save((Join-Path $OutputDirectory 'powder-box-body.png'), [Drawing.Imaging.ImageFormat]::Png)

    # At 500 points capacity, one production batch raises the surface by one step.
    foreach ($percent in 20, 40, 60, 80, 100) {
        $powder = [Drawing.Bitmap]::new(48, 48, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $surface = 42 - $percent / 5
            Fill-Pixels $powder 8 $surface 32 (40 - $surface) 'DEDEDE'
            Fill-Pixels $powder 10 ($surface - 1) 14 1 'DEDEDE'
            Fill-Pixels $powder 14 ($surface - 2) 8 1 'FFFFFF'
            Fill-Pixels $powder 24 $surface 12 1 'FFFFFF'
            Fill-Pixels $powder 32 ($surface + 1) 8 1 'C4C4C4'
            Fill-Pixels $powder 8 39 32 1 'A6A6A6'

            $name = if ($percent -eq 100) { 'powder-box-fill.png' } else { "powder-box-fill-$percent.png" }
            $powder.Save((Join-Path $OutputDirectory $name), [Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $powder.Dispose()
        }
    }
}
finally {
    $body.Dispose()
}
