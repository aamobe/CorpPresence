param([string]$MineralOutput = $PSScriptRoot, [string]$CartelOutput = (Join-Path $PSScriptRoot '../cartel-production'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$colors = @{
    edge='292234'; dark='303A41'; iron='44464C'; gray='5F6868'; steel='80929B'
    mist='A8AEA5'; pale='C4C4B0'; light='D5D2B8'; blue='355271'; teal='45918B'
    mint='8ABFBB'; rust='7D4F4A'; copper='B68B74'; amber='8F6B5A'; green='3D823F'
}

function Fill($x, $y, $w, $h, $ink) {
    $color = [Drawing.ColorTranslator]::FromHtml("#$($colors[$ink])")
    $brush = [Drawing.SolidBrush]::new($color)
    try { $graphics.FillRectangle($brush, $x, $y, $w, $h) }
    finally { $brush.Dispose() }
}

function Draw-Machine($name, $height, $directory, [scriptblock]$layers) {
    $image = [Drawing.Bitmap]::new(48, $height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($image)
    try {
        & $layers
        $image.Save((Join-Path $directory "$name.png"), [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $graphics.Dispose(); $image.Dispose() }
}

Draw-Machine 'mineral-crusher' 48 $MineralOutput {
    # Chassis, feet and reinforced receiving dock.
    Fill 3 8 42 34 edge
    Fill 5 10 38 30 dark
    Fill 5 39 38 4 iron
    Fill 5 43 8 2 edge
    Fill 35 43 8 2 edge
    Fill 5 39 6 2 gray
    Fill 37 39 6 2 steel
    # Broad scrap intake with stepped funnel.
    Fill 5 4 38 10 edge
    Fill 6 5 36 6 steel
    Fill 8 6 32 3 dark
    Fill 8 9 32 2 mist
    Fill 10 11 28 3 gray
    Fill 13 14 22 3 steel
    Fill 17 17 14 3 iron
    # Removable inspection cover, fasteners and recessed grip.
    Fill 8 18 32 14 gray
    Fill 9 18 12 1 mist
    Fill 21 18 9 1 pale
    Fill 30 18 8 1 steel
    Fill 10 20 28 10 steel
    Fill 12 22 2 2 iron
    Fill 34 22 2 2 iron
    Fill 17 25 14 3 dark
    Fill 18 27 12 1 mist
    Fill 12 29 5 1 rust
    Fill 12 28 3 1 copper
    # Beveled steel on the hopper lip and inspection door.
    Fill 7 5 10 1 pale
    Fill 17 5 14 1 light
    Fill 31 5 10 1 mist
    Fill 10 20 28 2 mist
    Fill 11 20 8 1 pale
    Fill 19 20 11 1 light
    Fill 30 20 7 1 pale
    Fill 10 30 28 1 iron
    # Powder chute, receiver rails and one status lamp.
    Fill 16 31 16 5 iron
    Fill 19 32 10 3 gray
    Fill 11 35 26 8 dark
    Fill 11 35 3 8 steel
    Fill 34 35 3 8 steel
    Fill 13 41 22 2 gray
    Fill 38 34 3 2 green
}

Draw-Machine 'powder-sieve' 48 $MineralOutput {
    # Rail frame and a shallow left-hand box dock.
    Fill 4 7 40 36 edge
    Fill 6 8 36 34 iron
    Fill 4 40 40 3 gray
    Fill 6 43 7 2 edge
    Fill 35 43 7 2 edge
    Fill 5 5 23 10 edge
    Fill 6 6 21 8 steel
    Fill 8 7 17 5 dark
    Fill 9 11 15 1 pale
    Fill 7 13 18 3 gray
    Fill 6 6 7 1 pale
    Fill 13 6 8 1 light
    Fill 21 6 6 1 mist
    Fill 7 13 18 1 mist
    # Broad vibrating screen cassette and contrasting external drive.
    Fill 6 17 36 12 dark
    Fill 8 18 27 7 teal
    Fill 8 18 9 1 mint
    Fill 17 18 12 1 mist
    Fill 29 18 6 1 steel
    Fill 11 21 22 2 dark
    Fill 12 22 20 1 steel
    Fill 36 19 6 8 gray
    Fill 38 20 3 4 copper
    Fill 37 25 4 2 rust
    Fill 10 25 3 5 steel
    Fill 30 25 3 5 steel
    Fill 8 19 27 1 mint
    Fill 36 19 6 1 pale
    Fill 5 40 29 1 steel
    # Five adjacent classification drawers and detached recovery bin.
    Fill 6 29 28 12 dark
    foreach ($x in 7,12,17,22,27) {
        Fill $x 31 4 8 steel
        Fill $x 32 4 1 pale
        Fill ($x+1) 35 2 1 dark
    }
    Fill 35 29 7 12 dark
    Fill 36 31 6 8 amber
    Fill 36 31 6 1 copper
    Fill 37 35 4 2 dark
    Fill 33 26 4 3 gray
    Fill 35 28 2 3 copper
}

Draw-Machine 'mineral-growth-station' 64 $MineralOutput {
    # Tall hydroponic frame, service head and output plinth.
    Fill 4 6 40 52 edge
    Fill 6 8 36 47 iron
    Fill 3 3 42 10 edge
    Fill 5 4 38 7 steel
    Fill 6 5 12 1 mist
    Fill 18 5 14 1 light
    Fill 32 5 9 1 pale
    Fill 7 9 34 2 dark
    Fill 7 11 34 1 mint
    # Feed side with separate crystal socket and substrate tray.
    Fill 5 17 9 27 dark
    Fill 6 19 7 8 steel
    Fill 8 20 3 4 pale
    Fill 6 32 7 9 gray
    Fill 7 33 5 2 copper
    Fill 12 23 5 3 steel
    Fill 12 36 5 3 gray
    # Wide cultivation window and seated crystal bed.
    Fill 15 14 20 31 dark
    Fill 16 15 18 2 mist
    Fill 16 17 18 24 blue
    Fill 17 18 16 3 teal
    Fill 17 21 2 12 mint
    Fill 19 21 1 7 steel
    Fill 18 38 14 3 gray
    Fill 22 34 4 4 mint
    Fill 24 31 3 7 mist
    Fill 27 35 3 3 teal
    Fill 17 41 16 2 pale
    Fill 5 4 11 1 pale
    Fill 16 4 15 1 light
    Fill 31 4 12 1 mist
    Fill 16 15 5 1 pale
    Fill 21 15 8 1 light
    Fill 29 15 5 1 mist
    # Exposed water supply with adapter and return along the frame.
    Fill 37 14 4 32 dark
    Fill 38 15 2 29 teal
    Fill 34 19 7 3 gray
    Fill 35 19 6 1 mint
    Fill 36 31 6 4 copper
    Fill 38 35 2 9 mint
    Fill 34 43 7 3 steel
    Fill 5 47 38 11 gray
    Fill 6 47 36 2 steel
    Fill 15 50 20 7 dark
    Fill 17 54 16 2 mist
    Fill 37 51 4 2 green
    Fill 6 58 9 2 dark
    Fill 33 58 9 2 dark
}

Draw-Machine 'alcohol-preparation-station' 48 $CartelOutput {
    # Same left-feed, right-module, lower receiving layout as the reclaimer.
    Fill 4 7 40 36 edge
    Fill 6 8 36 33 dark
    Fill 5 40 38 3 iron
    Fill 7 43 8 2 edge
    Fill 33 43 8 2 edge
    # Nutrifruit feed tray and raised hopper throat.
    Fill 5 5 15 5 edge
    Fill 5 8 18 5 copper
    Fill 6 8 16 2 pale
    Fill 6 10 16 2 dark
    Fill 7 12 14 3 amber
    Fill 10 15 9 3 rust
    Fill 14 17 11 3 gray
    Fill 17 17 6 1 steel
    # Water connector and exposed inlet pipe with a mismatched collar.
    Fill 6 20 12 17 iron
    Fill 7 22 8 13 blue
    Fill 8 22 5 2 mint
    Fill 13 25 10 3 teal
    Fill 17 24 3 5 copper
    Fill 18 24 2 1 pale
    Fill 21 25 4 2 mint
    # Retrofitted industrial module, nameplate and capped fasteners.
    Fill 23 6 20 24 edge
    Fill 24 7 17 4 pale
    Fill 25 7 6 1 light
    Fill 31 7 5 1 mist
    Fill 24 11 18 16 steel
    Fill 24 11 2 16 gray
    Fill 39 11 3 16 gray
    Fill 27 14 10 4 dark
    Fill 28 15 8 2 pale
    Fill 29 15 4 1 iron
    Fill 28 21 8 3 gray
    Fill 29 21 4 1 mist
    Fill 40 13 1 2 pale
    Fill 40 23 1 2 mist
    Fill 24 8 17 2 mist
    Fill 25 8 6 1 pale
    Fill 31 8 6 1 light
    Fill 37 8 4 1 pale
    Fill 24 11 2 13 mist
    Fill 39 11 3 1 pale
    Fill 26 25 13 1 gray
    # Outlet ends above the clamped receiving bottle.
    Fill 27 27 13 4 iron
    Fill 30 29 5 3 steel
    Fill 32 31 2 2 pale
    Fill 30 33 6 2 mist
    Fill 29 35 8 5 blue
    Fill 30 35 1 4 mint
    Fill 27 37 3 5 gray
    Fill 36 37 3 5 gray
    Fill 28 40 10 2 steel
    Fill 38 32 3 1 pale
    Fill 38 35 2 1 mist
}
