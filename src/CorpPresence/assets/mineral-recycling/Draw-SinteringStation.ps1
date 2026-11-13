param([string]$OutputDirectory = $PSScriptRoot)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$frame = [Drawing.Bitmap]::new(48, 48, [Drawing.Imaging.PixelFormat]::Format32bppArgb)

function Paint($x, $y, $width, $height, $hex) {
    $color = [Drawing.ColorTranslator]::FromHtml("#$hex")
    for ($row = $y; $row -lt $y + $height; $row++) {
        for ($column = $x; $column -lt $x + $width; $column++) {
            $frame.SetPixel($column, $row, $color)
        }
    }
}

try {
    # Bench frame, beveled crossbeam and feet.
    Paint 5 5 38 38 '292234'
    Paint 3 9 42 33 '292234'
    Paint 4 12 40 28 '303A41'
    Paint 5 39 38 4 '44464C'
    Paint 6 43 8 2 '292234'
    Paint 34 43 8 2 '292234'
    Paint 5 39 38 1 '80929B'

    # Three powder-box docks share one feed rail.
    Paint 6 6 36 7 '5F6868'
    Paint 6 6 11 1 '80929B'
    Paint 17 6 14 1 'C4C4B0'
    Paint 31 6 11 1 'A8AEA5'
    foreach ($x in 7,20,33) {
        Paint $x 7 8 4 '303A41'
        Paint ($x + 1) 10 6 1 '45918B'
    }
    Paint 8 12 32 2 '80929B'
    Paint 20 14 8 3 '5F6868'

    # The closed central press hangs between the two supports.
    Paint 7 15 5 21 '44464C'
    Paint 36 15 5 21 '44464C'
    Paint 7 17 2 17 '5F6868'
    Paint 39 17 2 17 '5F6868'
    Paint 12 17 24 17 '80929B'
    Paint 12 17 7 1 'A8AEA5'
    Paint 19 17 12 1 'C4C4B0'
    Paint 13 20 22 10 '44464C'
    Paint 15 22 18 6 '303A41'
    Paint 16 27 16 1 '5F6868'
    Paint 22 20 4 2 'A8AEA5'
    Paint 14 31 20 2 '5F6868'

    # Outlet below the press and a low receiving tray.
    Paint 19 34 10 3 '80929B'
    Paint 12 37 24 3 '303A41'
    Paint 12 39 24 1 'A8AEA5'
    Paint 13 40 22 2 '5F6868'
    Paint 6 31 3 2 '7D4F4A'
    Paint 39 31 2 2 'A8AEA5'

    # Direct light catches the feed rail and the press cover's chamfer.
    Paint 8 12 10 1 'A8AEA5'
    Paint 18 12 12 1 'D5D2B8'
    Paint 30 12 10 1 'C4C4B0'
    Paint 12 18 24 2 'A8AEA5'
    Paint 13 18 7 1 'C4C4B0'
    Paint 20 18 10 1 'D5D2B8'
    Paint 30 18 5 1 'C4C4B0'
    Paint 14 33 20 1 '44464C'

    $frame.Save((Join-Path $OutputDirectory 'sintering-station.png'), [Drawing.Imaging.ImageFormat]::Png)

    # Warm core inside the sight glass, leaving the window frame untouched.
    Paint 18 24 12 2 '7D4F4A'
    Paint 21 24 6 1 'B68B74'
    $frame.Save((Join-Path $OutputDirectory 'sintering-station-running.png'), [Drawing.Imaging.ImageFormat]::Png)
}
finally { $frame.Dispose() }
