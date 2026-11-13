param([string]$OutputRoot = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# Palette from furnace, animal_cage, bottle_printer and portable_water_purifier.
$palette = @{
    edge='292234'; dark='303A41'; iron='44464C'; gray='5F6868'; olive='88948A'
    steel='80929B'; mist='A8AEA5'; pale='C4C4B0'; ivory='D5D2B8'
    blue='355271'; navy='273456'; teal='45918B'; aqua='67AAA5'; mint='8ABFBB'
    rust='7D4F4A'; bronze='8F6B5A'; copper='B68B74'; red='853737'; green='7AB053'
}
$brushes = @{}
foreach ($key in $palette.Keys) {
    $brushes[$key] = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml("#$($palette[$key])"))
}

function Face($ink, [int[]]$vertices) {
    $points = for ($i=0; $i -lt $vertices.Length; $i+=2) {
        [Drawing.Point]::new($vertices[$i],$vertices[$i+1])
    }
    $graphics.FillPolygon($brushes[$ink],[Drawing.Point[]]$points)
}
function Rect($ink,$x,$y,$width,$height) {
    $graphics.FillRectangle($brushes[$ink],$x,$y,$width,$height)
}
function Draw($group,$name,$width,$height,[scriptblock]$layers) {
    $image = [Drawing.Bitmap]::new($width,$height,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($image)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::None
    try {
        & $layers
        $image.Save((Join-Path $OutputRoot "$group/$name-original-style.png"),[Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $graphics.Dispose(); $image.Dispose() }
}

try {
    Draw 'mineral-recycling' 'mineral-crusher' 48 48 {
        # Reinforced feed-side mounting ear, tucked behind the hopper lip.
        Face edge (6,8,10,5,19,5,19,12,9,17,6,15)
        Face steel (7,8,11,6,16,6,12,10,7,10)
        # Lower crusher casting and right-side transmission case.
        Face edge (4,21,15,13,45,13,46,36,34,45,4,45)
        Face gray (5,22,33,22,33,43,5,43)
        Face iron (34,22,45,14,45,35,34,43)
        Face steel (35,24,43,18,43,27,35,33)
        Face dark (36,25,42,21,42,23,36,27)
        Face bronze (38,28,43,24,43,27,38,31)
        # Scrap hopper. Open rim, rear wall and converging throat.
        Face edge (3,13,15,3,46,3,46,11,34,22,28,25,12,25,4,19)
        Face pale (4,13,16,4,45,4,33,14)
        Face dark (8,12,17,5,42,5,32,12)
        Face iron (12,11,19,7,38,7,32,12)
        Face mist (4,13,33,13,33,16,5,16)
        Face steel (5,16,33,16,28,22,12,22)
        Face iron (33,15,45,5,45,10,34,20,29,23)
        # Service lid and powder-box rails in the front recess.
        Rect dark 8 24 22 10
        Rect steel 9 24 20 8
        Rect pale 10 24 15 1
        Rect mist 25 24 3 1
        Rect dark 15 27 9 2
        Rect mist 10 29 2 2
        Rect olive 26 29 2 2
        Rect edge 8 35 22 7
        Face gray (9,41,15,36,27,36,27,40,25,42,9,42)
        Face pale (9,40,14,36,16,36,11,41,9,41)
        Face steel (25,40,29,36,30,36,29,41,25,42)
        Rect iron 5 43 6 2
        Rect green 30 34 2 1
    }

    Draw 'mineral-recycling' 'powder-sieve' 48 48 {
        # Rear cover of the screen drive sits behind the inclined deck.
        Face edge (31,10,37,5,45,5,46,7,46,15,35,19)
        Face steel (34,10,38,6,44,6,44,9,40,12)
        # Screen deck seen from above, with a distinct side-mounted motor.
        Face edge (3,18,14,9,44,9,46,12,46,36,34,45,3,45)
        Face teal (4,18,15,10,44,10,33,18)
        Face mint (4,18,33,18,43,11,45,11,34,20,4,20)
        Face iron (34,20,45,12,45,35,34,44)
        Rect iron 4 20 30 24
        Rect teal 6 21 26 8
        Rect aqua 6 21 26 1
        Rect dark 8 24 22 3
        Rect steel 8 27 22 1
        Face bronze (34,26,40,21,44,21,44,29,38,34,34,34)
        Face aqua (37,25,42,21,45,22,45,28,40,32,37,32)
        Face blue (38,28,45,23,45,28,40,32,38,32)
        Face mint (38,26,43,22,44,22,40,26)
        # Raised open intake with a diagonal right wall.
        Face edge (4,11,9,5,14,3,27,3,27,9,17,17,7,17)
        Face pale (5,11,10,6,15,4,26,4,17,12,5,12)
        Face dark (8,10,16,5,24,5,17,10)
        Face steel (5,12,17,12,17,15,7,15)
        Face gray (17,12,26,5,26,8,17,16)
        Rect dark 5 30 28 13
        foreach ($x in 6,10,14,18,22) {
            Face mist ($x,33,($x+2),31,($x+4),31,($x+3),33)
            Rect steel $x 33 3 7
            Rect dark $x 37 2 1
        }
        Rect bronze 27 32 5 8
        Rect copper 27 32 5 1
        Rect dark 28 36 3 1
        Rect steel 5 41 28 1
    }

    Draw 'mineral-recycling' 'sintering-station' 48 48 {
        # Thickened left end of the worktop supports the first feed socket.
        Face edge (5,12,9,5,16,4,16,11,7,17,5,16)
        Face steel (6,12,10,6,14,6,12,10,7,14)
        # Thick working deck and three inset sockets, each on the top plane.
        Face edge (3,15,14,4,46,4,46,35,35,45,3,45)
        Face mist (4,15,15,5,45,5,34,15)
        Face ivory (4,15,34,15,44,6,45,6,35,17,4,17)
        foreach ($x in 8,17,26) {
            Face steel ($x,13,($x+6),7,($x+13),7,($x+7),13)
            Face dark (($x+2),12,($x+6),8,($x+11),8,($x+7),12)
            Rect pale ($x+2) 12 4 1
        }
        Rect gray 4 17 30 26
        Face iron (34,17,45,7,45,34,34,44)
        Face steel (35,18,44,10,44,12,35,20)
        Face dark (38,24,44,19,44,21,38,26)
        Face dark (38,28,44,23,44,25,38,30)
        Face teal (35,38,43,31,44,31,44,33,35,41)
        # Recessed press, projecting outlet tray and two cool side modules.
        Rect dark 10 19 20 18
        Rect teal 5 22 4 12
        Rect teal 30 22 4 12
        Rect aqua 5 22 4 1
        Rect aqua 30 22 4 1
        Rect steel 12 21 16 13
        Rect mist 12 21 16 2
        Rect pale 15 21 10 1
        Rect dark 14 25 12 5
        Rect bronze 16 27 8 1
        Rect olive 14 29 12 1
        Face pale (12,39,17,34,27,34,24,39)
        Face dark (15,38,18,35,26,35,24,38)
        Rect mist 12 39 12 2
        Rect iron 4 43 30 2
    }

    Draw 'mineral-recycling' 'mineral-growth-station' 48 64 {
        # Left lid hinge block.
        Face edge (6,9,10,5,17,5,17,12,7,16,6,15)
        Face steel (7,9,11,6,16,6,12,10,7,11)
        # Full-depth cabinet with lid glass and a braced service side.
        Face edge (3,14,15,3,46,3,46,50,34,61,3,61)
        Face steel (4,14,16,4,45,4,33,14)
        Face blue (11,12,20,5,37,5,29,12)
        Face aqua (12,12,29,12,37,5,39,5,30,13,11,13)
        Rect mist 4 14 30 3
        Rect iron 4 17 30 42
        Face dark (34,15,45,5,45,50,34,60)
        Face steel (35,20,44,12,44,15,35,23)
        Face gray (35,39,44,30,44,33,35,42)
        Face gray (35,40,44,44,44,47,35,43)
        Face steel (35,55,44,47,44,49,35,58)
        Rect teal 5 18 28 2
        # Front cultivation window, crystal bed and pipe clamps.
        Rect gray 10 23 16 25
        Rect pale 10 23 16 1
        Rect navy 11 25 14 20
        Face blue (11,26,16,26,16,33,11,38)
        Rect aqua 12 26 11 1
        Face mint (16,43,16,36,19,32,21,34,21,43)
        Face teal (19,43,19,33,21,34,21,43)
        Face aqua (12,43,12,39,14,37,16,39,16,43)
        Face aqua (21,43,22,38,24,36,25,38,25,43)
        Rect olive 12 43 12 2
        Rect mist 11 46 14 1
        foreach ($y in 28,37) {
            Rect steel 5 $y 4 5
            Rect dark 6 ($y+2) 2 1
        }
        Rect teal 29 25 3 22
        Rect mint 29 25 1 20
        Rect copper 28 30 5 2
        Rect bronze 28 40 5 2
        Rect steel 5 49 28 3
        Rect ivory 5 49 28 1
        Rect dark 11 53 15 6
        Face mist (11,58,15,54,25,54,23,58)
        Rect steel 11 58 12 2
        Rect green 29 55 2 2
    }

    Draw 'cartel-production' 'alcohol-preparation-station' 48 48 {
        # Deep platform and offset preparation module.
        Face edge (3,38,14,29,46,29,46,36,35,45,3,45)
        Face steel (4,39,15,30,44,30,34,39)
        Rect iron 4 39 31 5
        Face dark (35,39,45,30,45,36,35,44)
        Face edge (20,13,31,4,46,4,46,31,35,40,20,40)
        Face mist (21,13,32,5,45,5,35,13)
        Face pale (22,12,34,12,43,5,45,5,35,14,21,14)
        Rect steel 21 14 14 13
        Face iron (35,14,45,6,45,25,35,33)
        Face dark (38,16,43,12,43,14,38,18)
        Face dark (38,20,43,16,43,18,38,22)
        Face steel (35,27,45,19,45,21,35,29)
        Rect dark 23 17 10 5
        Rect mist 25 18 6 2
        Rect teal 24 23 9 1
        # Offset fruit hopper with open rim, retaining a broad rear face.
        Face edge (3,12,7,6,13,4,26,4,26,9,17,19,9,20,4,17)
        Face pale (4,12,8,7,14,5,25,5,17,13)
        Face dark (7,11,15,6,23,6,17,11)
        Rect red 11 8 3 3
        Rect rust 15 9 4 2
        Rect olive 15 7 2 2
        Face bronze (4,13,17,13,17,17,13,19,9,19,5,16)
        Face iron (17,13,25,6,25,9,17,17)
        # Blue tank, field plumbing and bottle in a receiving cradle.
        Face aqua (6,26,12,21,20,21,15,26)
        Rect blue 6 26 10 12
        Face navy (16,26,20,22,20,33,16,38)
        Rect teal 5 20 6 4
        Rect mint 6 20 4 1
        Rect teal 5 22 2 7
        Rect pale 5 24 3 2
        Rect iron 21 26 14 13
        Rect mist 27 27 5 2
        Rect steel 29 29 2 3
        Rect teal 27 32 6 6
        Rect aqua 27 34 6 2
        Rect ivory 27 33 1 3
        Rect mist 25 35 2 5
        Rect mist 33 35 2 5
        Rect gray 25 39 10 2
    }

    Draw 'cartel-production' 'twin-vessel-reactor' 64 48 {
        # The near corner has a welded gusset behind the diagonal rail.
        Face edge (6,9,10,5,18,5,18,9,8,15,6,14)
        Face steel (7,9,11,6,15,6,12,9,7,11)
        # Frame runs from a low near rail to a raised rear rail.
        Face edge (3,12,14,3,61,3,62,5,62,36,51,45,3,45)
        Face dark (4,13,15,4,61,4,50,13)
        Rect iron 4 13 46 30
        Face dark (50,13,61,4,61,35,50,44)
        Face steel (4,12,15,4,18,4,7,13)
        Face mist (16,4,60,4,57,7,13,7)
        Face steel (49,12,59,4,61,4,61,7,51,15)
        Face gray (51,38,60,31,61,31,61,35,51,43)
        Rect gray 4 13 3 29
        Rect gray 48 14 3 28
        # Set the right vessel one pixel behind the blue vessel.
        foreach ($vessel in @(
            @{X=7; Y=14; Front='blue'; Side='navy'; Top='aqua'; Rim='mint'},
            @{X=31; Y=13; Front='pale'; Side='olive'; Top='mist'; Rim='ivory'}
        )) {
            $x=$vessel.X; $y=$vessel.Y
            Face edge ($x,($y+4),($x+6),$y,($x+19),$y,($x+21),($y+2),($x+21),($y+19),($x+15),($y+25),($x+1),($y+25),$x,($y+23))
            Face $vessel.Top (($x+1),($y+4),($x+6),($y+1),($x+19),($y+1),($x+20),($y+2),($x+15),($y+5),($x+2),($y+5))
            Face $vessel.Side (($x+15),($y+6),($x+20),($y+2),($x+20),($y+19),($x+15),($y+24))
            Rect $vessel.Front ($x+1) ($y+6) 14 17
            Rect $vessel.Rim ($x+1) ($y+5) 14 1
            Face $vessel.Rim (($x+15),($y+5),($x+20),($y+2),($x+20),($y+3),($x+15),($y+6))
            Rect dark ($x+5) ($y+9) 5 8
            Rect mint ($x+6) ($y+10) 2 6
            Rect $vessel.Rim ($x+1) ($y+20) 14 1
            Rect iron ($x+5) ($y+20) 5 4
            Rect pale ($x+6) ($y+21) 3 2
            Rect rust ($x+7) ($y+21) 1 1
            Rect steel ($x+7) ($y+24) 3 2
        }
        # Overhead feed line and front diverter.
        Rect iron 14 8 34 4
        Rect steel 14 8 34 2
        Rect pale 17 8 11 1
        Rect mist 28 8 16 1
        Rect steel 14 10 4 6
        Rect steel 44 10 4 5
        Rect pale 13 13 6 2
        Rect pale 43 12 6 2
        Rect gray 28 11 3 15
        Rect mist 28 11 1 13
        Rect steel 25 26 9 9
        Rect pale 25 26 9 1
        Rect rust 28 28 4 2
        Rect copper 29 28 4 1
        Rect gray 4 40 47 3
        Rect mist 5 40 45 1
        Rect iron 4 43 7 2
        Rect iron 45 43 7 2
    }
}
finally { foreach ($brush in $brushes.Values) { $brush.Dispose() } }
