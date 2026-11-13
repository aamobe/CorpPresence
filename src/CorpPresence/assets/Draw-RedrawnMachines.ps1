param([string]$OutputRoot = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$palette = @{
    edge='292234'; dark='303A41'; iron='44464C'; gray='5F6868'; olive='88948A'
    steel='80929B'; mist='A8AEA5'; pale='C4C4B0'; light='D5D2B8'
    teal='45918B'; aqua='67AAA5'; mint='8ABFBB'; blue='355271'
    navy='273456'; rust='7D4F4A'; copper='B68B74'; green='7AB053'
}
$brushes = @{}
foreach ($ink in $palette.Keys) {
    $brushes[$ink] = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml("#$($palette[$ink])"))
}
function Bar($ink,$x,$y,$w,$h) { $graphics.FillRectangle($brushes[$ink],$x,$y,$w,$h) }
function Face($ink,[int[]]$coords) {
    $points = for ($i=0; $i -lt $coords.Length; $i+=2) {
        [Drawing.Point]::new($coords[$i],$coords[$i+1])
    }
    $graphics.FillPolygon($brushes[$ink],[Drawing.Point[]]$points)
}

function Draw($group,$name,$width,$height,[scriptblock]$layers) {
    $frame = [Drawing.Bitmap]::new($width,$height,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($frame)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::None
    try {
        & $layers
        $frame.Save((Join-Path $OutputRoot "$group/$name-redrawn.png"),[Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $graphics.Dispose(); $frame.Dispose() }
}

try {
    Draw 'mineral-recycling' 'mineral-crusher' 48 48 {
        # Hopper rim, rear wall and sloping throat.
        Face edge (3,13,7,5,41,5,45,13,45,18,39,23,9,23,3,18)
        Face pale (4,13,8,6,40,6,44,13,44,15,4,15)
        Face dark (7,12,10,7,38,7,41,12,37,13,11,13)
        Face gray (11,11,13,9,35,9,37,11,34,12,14,12)
        Bar light 10 14 28 1
        Face steel (5,15,43,15,38,21,10,21)
        Face iron (7,17,41,17,37,21,11,21)
        # Cast shoulders, bolted inspection door and a recessed receiver.
        Face edge (7,20,41,20,45,23,45,43,41,46,7,46,3,43,3,23)
        Face steel (4,23,8,21,40,21,44,23,44,25,4,25)
        Bar iron 5 25 38 17
        Bar mist 8 23 32 1
        Bar dark 9 26 30 10
        Bar gray 10 27 28 8
        Bar pale 11 27 26 1
        Bar steel 12 28 24 5
        Bar dark 17 30 14 2
        Bar mist 18 32 12 1
        Bar pale 12 29 2 2
        Bar pale 34 29 2 2
        Bar copper 33 33 4 1
        Bar dark 10 36 28 7
        Face gray (11,42,15,37,33,37,37,42)
        Face mist (11,42,15,37,17,37,14,42)
        Face mist (34,42,31,37,33,37,37,42)
        Bar steel 5 43 38 1
    }

    Draw 'mineral-recycling' 'powder-sieve' 48 48 {
        # Full-width inclined screen; the feed dock is raised on the left.
        Face edge (3,15,7,5,41,5,45,15,45,42,41,45,7,45,3,42)
        Face teal (4,15,8,6,40,6,44,15,44,19,4,19)
        Face aqua (7,14,10,7,38,7,41,14)
        Face teal (21,13,23,8,37,8,39,13)
        Bar mint 5 15 38 1
        Bar dark 4 19 40 23
        Face edge (5,12,8,5,22,5,25,12,24,16,6,16)
        Face pale (6,12,9,6,21,6,24,12,24,13,6,13)
        Face dark (8,11,10,7,20,7,22,11)
        Bar steel 7 13 16 2
        # Screen cassette, external drive and classification drawer bank.
        Bar teal 6 20 29 8
        Bar mint 7 20 27 1
        Bar dark 8 22 25 4
        Bar steel 9 25 23 1
        Bar gray 36 21 7 9
        Bar copper 37 21 5 2
        Bar steel 38 23 4 5
        Bar iron 37 27 5 1
        Bar steel 7 28 26 2
        foreach ($x in 6,11,16,21,26) {
            Face mist ($x,32,($x+1),30,($x+3),30,($x+4),32)
            Bar steel $x 32 4 7
            Bar dark ($x+1) 35 2 1
        }
        Bar copper 34 31 8 2
        Bar rust 34 33 8 6
        Bar pale 35 33 6 1
        Bar dark 36 36 4 1
        Bar gray 5 40 38 3
        Bar steel 6 40 36 1
    }

    Draw 'mineral-recycling' 'sintering-station' 48 48 {
        # Wide deck with three deep sockets on the same upper plane.
        Face edge (3,16,7,5,41,5,45,16,45,42,41,45,7,45,3,42)
        Face mist (4,16,8,6,40,6,44,16,44,19,4,19)
        Bar light 6 16 36 1
        foreach ($x in 8,19,30) {
            Face steel ($x,14,($x+1),8,($x+8),8,($x+10),14)
            Face dark (($x+1),13,($x+2),9,($x+7),9,($x+8),13)
            Bar pale ($x+2) 13 6 1
        }
        Bar iron 4 19 40 23
        Bar steel 5 20 38 2
        Bar teal 6 24 6 12
        Bar teal 36 24 6 12
        Bar aqua 6 24 6 1
        Bar aqua 36 24 6 1
        Bar dark 13 22 22 15
        Bar steel 14 22 20 13
        Bar pale 15 22 18 2
        Bar gray 15 24 18 9
        Bar dark 17 26 14 4
        Bar rust 19 28 10 1
        Bar mist 17 30 14 1
        Bar dark 16 35 16 4
        Face pale (13,41,16,37,32,37,35,41)
        Face gray (16,40,18,38,30,38,32,40)
        Bar steel 13 41 22 2
    }

    Draw 'mineral-recycling' 'mineral-growth-station' 48 64 {
        # Upper canopy with inset glass; frame and base share its centreline.
        Face edge (3,14,7,5,41,5,45,14,45,58,41,61,7,61,3,58)
        Face steel (4,14,8,6,40,6,44,14,44,17,4,17)
        Face pale (8,13,12,7,36,7,40,13)
        Face blue (10,12,13,8,35,8,38,12)
        Bar light 6 14 36 1
        Bar iron 5 17 38 39
        Bar steel 5 17 38 2
        Bar teal 7 19 34 2
        # Cultivation chamber, two feed ports and external water line.
        Bar gray 12 23 24 26
        Bar pale 12 23 24 2
        Bar navy 14 25 20 20
        Face blue (14,25,20,25,20,32,14,38)
        Bar teal 15 26 18 1
        Face aqua (17,44,17,39,20,36,22,39,22,44)
        Face mint (21,44,21,35,25,30,27,34,27,44)
        Face teal (25,44,25,31,27,34,27,44)
        Face aqua (27,44,28,40,31,37,32,40,32,44)
        Bar olive 15 44 18 2
        Bar mist 13 47 22 1
        foreach ($y in 27,36) {
            Bar steel 6 $y 5 6
            Bar pale 6 $y 5 1
            Bar dark 7 ($y+3) 3 1
        }
        Bar teal 38 24 3 24
        Bar mint 38 25 1 22
        Bar copper 37 30 5 2
        Bar steel 36 46 5 2
        Bar steel 5 49 38 3
        Bar mist 6 49 36 1
        Bar dark 12 53 24 6
        Face steel (13,58,17,54,31,54,35,58)
        Bar pale 14 58 20 1
        Bar green 39 55 2 2
    }

    Draw 'cartel-production' 'alcohol-preparation-station' 48 48 {
        # Single bench plinth supports the intake and factory module.
        Face edge (3,22,7,18,41,18,45,22,45,43,41,46,7,46,3,43)
        Bar iron 4 23 40 19
        Bar steel 5 42 38 2
        Bar mist 7 42 34 1
        # Fruit hopper and throat.
        Face edge (3,13,7,5,19,5,23,13,22,17,18,22,8,22,4,17)
        Face pale (4,13,8,6,18,6,22,13,22,14,4,14)
        Face dark (6,12,9,7,17,7,20,12)
        Bar rust 9 9 5 3
        Bar copper 14 10 4 2
        Face rust (5,14,21,14,18,20,8,20)
        Bar copper 7 15 12 1
        Bar steel 10 20 7 3
        # Water intake and collar lead into the preparation module.
        Bar blue 6 23 12 17
        Bar aqua 6 23 12 2
        Bar teal 14 27 13 3
        Bar copper 17 26 3 5
        Bar pale 17 26 3 1
        Face edge (21,13,25,5,41,5,45,13,45,31,22,31)
        Face mist (22,13,26,6,40,6,44,13,44,15,23,15)
        Bar pale 26 13 16 1
        Bar light 28 13 8 1
        Bar steel 23 15 21 13
        Bar gray 23 16 2 11
        Bar gray 42 16 2 11
        Bar dark 27 18 13 5
        Bar mist 28 19 8 2
        Bar teal 27 25 12 1
        # Outlet and clamped receiver.
        Bar mist 31 28 6 2
        Bar steel 33 30 2 3
        Face aqua (29,35,31,33,37,33,39,35,39,40,29,40)
        Bar blue 30 37 8 3
        Bar light 30 35 1 3
        Bar gray 27 36 2 6
        Bar gray 39 36 2 6
        Bar mist 27 41 14 1
    }

    Draw 'cartel-production' 'twin-vessel-reactor' 64 48 {
        # Upper crossbeam and base share the same symmetric projection.
        Face edge (3,11,7,5,57,5,61,11,61,42,57,46,7,46,3,42)
        Face steel (4,11,8,6,56,6,60,11,60,13,4,13)
        Bar mist 8 7 48 1
        Bar pale 8 11 48 1
        Bar dark 5 13 54 28
        Bar gray 5 13 3 28
        Bar gray 56 13 3 28
        Bar steel 7 39 50 4
        Bar mist 7 40 50 1
        # Flange tops, face panels, sight glasses and individual outlets.
        foreach ($vessel in @(
            @{X=8; Body='blue'; Cap='aqua'; Lip='mint'; Shade='navy'},
            @{X=38; Body='mist'; Cap='pale'; Lip='light'; Shade='gray'}
        )) {
            $x=$vessel.X
            Face edge ($x,19,($x+4),14,($x+14),14,($x+18),19,($x+18),34,($x+15),38,($x+3),38,$x,34)
            Face $vessel.Cap (($x+1),19,($x+5),15,($x+13),15,($x+17),19)
            Bar $vessel.Lip ($x+1) 19 16 1
            Bar $vessel.Body ($x+1) 20 16 13
            Bar $vessel.Shade ($x+1) 21 2 12
            Bar $vessel.Shade ($x+15) 21 2 12
            Bar dark ($x+6) 22 6 8
            Bar aqua ($x+8) 23 2 5
            Bar $vessel.Lip ($x+8) 23 2 1
            Bar $vessel.Lip ($x+1) 33 16 1
            Bar $vessel.Shade ($x+3) 34 12 3
            Bar pale ($x+7) 34 4 2
            Bar steel ($x+7) 37 4 2
        }
        Bar steel 16 10 32 2
        Bar pale 17 10 30 1
        Bar steel 16 12 3 4
        Bar steel 45 12 3 4
        Bar gray 30 11 4 12
        Bar mist 30 12 1 8
        Bar steel 27 23 10 11
        Bar pale 27 23 10 1
        Bar dark 29 25 6 5
        Bar rust 31 27 5 2
        Bar copper 33 26 3 1
        Bar iron 8 43 8 2
        Bar iron 48 43 8 2
    }
}
finally { foreach ($item in $brushes.Values) { $item.Dispose() } }
