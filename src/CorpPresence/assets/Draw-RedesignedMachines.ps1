param([string]$OutputRoot = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$inks = @{
    edge='292234'; dark='303A41'; iron='4A2F38'; gray='6B5269'
    steel='777081'; mist='A8AEA5'; pale='C4C4B0'; light='D5D2B8'
    teal='275654'; aqua='2F756F'; blue='2F756F'; navy='275654'
    rust='6B5269'; copper='88948A'; green='7AB053'; violet='777081'
}
$brush = @{}
foreach($key in $inks.Keys) {
    $brush[$key]=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml("#$($inks[$key])"))
}
function Rect($ink,$x,$y,$w,$h) {
    $g.FillRectangle($brush[$ink],$x,$y,$w,$h)
}
function Face($ink,[int[]]$coords) {
    $points=for($i=0;$i -lt $coords.Count;$i+=2) {
        [Drawing.Point]::new($coords[$i],$coords[$i+1])
    }
    $g.FillPolygon($brush[$ink],[Drawing.Point[]]$points)
}
function Sprite($group,$name,$width,$height,[scriptblock]$art) {
    $bitmap=[Drawing.Bitmap]::new($width,$height,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::None
    try {
        & $art
        $bitmap.Save((Join-Path $OutputRoot "$group/$name-redesign.png"),[Drawing.Imaging.ImageFormat]::Png)
    } finally { $g.Dispose(); $bitmap.Dispose() }
}
try {
    Sprite 'mineral-recycling' 'mineral-crusher' 48 48 {
        # Armored twin-roll capsule: left loading drawer, right recessed receiver.
        Face edge (2,16,7,5,40,5,46,16,46,31,42,35,42,43,38,46,6,46,2,42)
        Face pale (4,15,8,6,39,6,44,15)
        Face steel (7,14,11,8,36,8,41,14)
        Rect mist 6 14 36 2
        Rect light 10 14 15 1
        Rect pale 25 14 13 1
        Rect iron 4 16 40 15
        Rect gray 5 17 38 2
        Face dark (6,21,10,18,29,18,34,22,34,29,7,29)
        Rect steel 8 22 23 6
        Rect mist 8 22 23 1
        Rect dark 14 24 11 2
        Rect pale 16 25 7 1
        Face violet (34,21,37,18,41,18,44,21,44,27,41,30,37,30,34,27)
        Rect dark 37 22 5 5
        Rect steel 38 22 3 3
        Rect copper 39 22 2 1
        Rect gray 4 31 37 12
        Rect iron 4 33 37 10
        Rect dark 21 34 18 9
        Face steel (21,42,25,38,35,38,39,42)
        Rect mist 22 42 16 1
        Rect dark 7 35 8 5
        Rect mist 8 35 2 1
        Rect green 12 36 2 1
        Rect steel 6 44 32 1
    }

    Sprite 'mineral-recycling' 'powder-sieve' 48 48 {
        # Purifier-like removable cartridge rack, with a central sealed shaker drum.
        Face edge (3,12,7,5,41,5,45,12,45,39,41,45,7,45,3,40)
        Face gray (4,12,8,6,40,6,44,12)
        Rect mist 6 11 36 2
        Rect pale 9 11 14 1
        Rect light 23 11 11 1
        Rect iron 4 13 40 28
        Face steel (11,11,14,7,34,7,37,11,35,17,13,17)
        Rect dark 14 10 20 3
        Rect pale 15 13 18 1
        Face dark (9,22,13,17,35,17,39,22,39,28,35,31,13,31,9,28)
        Face teal (10,22,14,18,34,18,38,22,38,27,34,29,14,29,10,27)
        Rect aqua 14 19 20 2
        Rect mist 16 19 6 1
        Rect dark 16 23 16 3
        Rect steel 17 25 14 1
        Rect copper 39 22 4 4
        Rect gray 5 31 38 11
        foreach($x in 6,11,16,21,26) {
            Rect dark $x 32 4 9
            Rect steel $x 33 3 7
            Rect pale $x 33 3 1
            Rect teal $x 38 3 1
        }
        Rect dark 33 31 10 10
        Rect rust 34 32 8 8
        Rect copper 34 32 8 2
        Rect dark 36 36 4 1
        Rect steel 7 42 34 1
    }

    Sprite 'mineral-recycling' 'sintering-station' 48 48 {
        # Overhead press gantry; three feed cassettes plug into its lower face.
        Face edge (2,14,6,5,42,5,46,14,46,43,42,46,6,46,2,43)
        Face pale (3,14,7,6,41,6,45,14)
        Face mist (6,12,9,7,39,7,42,12)
        Rect light 6 13 15 1
        Rect pale 21 13 13 1
        Rect mist 34 13 9 1
        Rect dark 4 15 40 27
        Rect gray 4 15 40 7
        foreach($x in 7,19,31) {
            Rect iron $x 15 10 7
            Rect steel ($x+1) 16 8 5
            Rect dark ($x+2) 17 6 2
            Rect pale ($x+2) 19 6 1
        }
        Rect steel 5 23 5 17
        Rect steel 38 23 5 17
        Rect pale 5 23 5 1
        Rect pale 38 23 5 1
        Rect iron 14 22 20 13
        Rect mist 14 23 20 2
        Rect dark 17 27 14 5
        Rect rust 19 29 10 1
        Face gray (11,41,16,35,32,35,37,41,37,43,11,43)
        Face steel (13,40,17,36,31,36,35,40)
        Rect pale 12 41 24 1
        Rect iron 5 43 38 2
    }

    Sprite 'mineral-recycling' 'mineral-growth-station' 48 64 {
        # Large sealed culture drum over a bank of feed cassettes.
        Face edge (3,11,7,5,41,5,45,11,45,57,41,62,7,62,3,57)
        Face pale (4,11,8,6,40,6,44,11)
        Rect light 7 10 17 1
        Rect mist 24 10 17 1
        Rect iron 4 12 40 46
        Rect steel 5 13 38 2
        Rect dark 5 16 38 23
        Face teal (9,22,14,16,34,16,39,22,39,34,34,39,14,39,9,34)
        Face blue (11,23,15,18,33,18,37,23,37,33,33,37,15,37,11,33)
        Rect aqua 15 19 18 2
        Rect mist 16 19 9 1
        Face aqua (15,34,18,29,22,34)
        Face mist (21,34,25,24,28,27,29,34)
        Face teal (25,34,25,25,28,27,29,34)
        Rect gray 15 34 18 2
        Rect teal 6 19 2 17
        Rect copper 5 25 4 2
        Rect steel 40 19 2 17
        Rect pale 39 31 4 2
        foreach($x in 6,19,32) {
            Rect gray $x 41 10 10
            Rect steel ($x+1) 42 8 7
            Rect dark ($x+2) 45 6 2
        }
        Rect teal 34 43 6 1
        Rect dark 12 53 24 6
        Face steel (12,58,16,54,32,54,36,58)
        Rect mist 13 58 22 1
        Rect green 39 54 2 2
    }

    Sprite 'cartel-production' 'alcohol-preparation-station' 48 48 {
        # Preparation head above a central bottle bay; water and fruit flank it.
        Face edge (2,14,7,5,41,5,46,14,46,41,42,45,6,45,2,41)
        Face mist (3,14,8,6,40,6,45,14)
        Face steel (7,12,11,7,37,7,41,12)
        Rect pale 6 13 36 1
        Rect light 13 13 14 1
        Rect iron 4 15 40 28
        Rect gray 5 15 38 8
        Rect dark 15 16 18 5
        Rect pale 17 17 6 2
        Rect teal 25 17 6 2
        Rect dark 15 23 18 19
        Rect steel 22 23 4 7
        Rect pale 20 26 8 2
        Face aqua (19,33,22,30,26,30,29,33,29,39,19,39)
        Rect blue 20 35 8 4
        Rect light 20 33 1 3
        Rect mist 17 37 2 4
        Rect mist 29 37 2 4
        Rect steel 17 40 14 2
        # Left cartridge coupling and right front-loaded fruit cup.
        Rect teal 6 24 6 14
        Rect aqua 6 24 6 2
        Rect copper 6 29 6 2
        Rect steel 10 22 6 3
        Face copper (34,26,37,23,42,23,44,26,43,33,35,33)
        Rect dark 36 25 6 2
        Rect rust 36 27 6 5
        Rect mist 36 33 6 2
        Rect steel 5 42 38 1
    }

    Sprite 'cartel-production' 'twin-vessel-reactor' 64 48 {
        # Shared top manifold, with two inset pressure cassettes and lower diverter.
        Face edge (3,13,7,5,57,5,61,13,61,42,57,46,7,46,3,42)
        Face steel (4,13,8,6,56,6,60,13)
        Rect mist 9 7 46 2
        Rect pale 12 7 16 1
        Rect light 28 7 10 1
        Rect pale 7 12 50 1
        Rect iron 4 14 56 28
        Rect gray 5 14 54 6
        Rect steel 15 14 34 3
        Rect pale 15 14 34 1
        Rect copper 30 13 4 5
        foreach($v in @(
            @{X=8; Mid='blue'; Low='navy'; Rim='aqua'},
            @{X=34; Mid='mist'; Low='gray'; Rim='pale'}
        )){
            $x=$v.X
            Face dark ($x,23,($x+4),18,($x+18),18,($x+22),23,($x+22),35,($x+18),39,($x+4),39,$x,35)
            Face $v.Rim (($x+1),23,($x+5),19,($x+17),19,($x+21),23)
            Rect $v.Mid ($x+1) 23 20 11
            Rect $v.Low ($x+1) 24 2 10
            Rect $v.Low ($x+19) 24 2 10
            Rect dark ($x+5) 25 12 5
            Rect aqua ($x+6) 26 10 2
            Rect light ($x+6) 26 4 1
            Rect $v.Rim ($x+2) 33 18 1
            Rect steel ($x+7) 35 8 3
            Rect dark ($x+8) 36 6 1
        }
        Rect gray 6 39 52 4
        Rect steel 7 40 50 1
        Rect dark 26 38 12 6
        Rect pale 27 39 4 3
        Rect pale 33 39 4 3
        Rect rust 28 39 1 2
        Rect rust 34 39 1 2
        Rect iron 6 43 52 2
    }
} finally { foreach($item in $brush.Values){$item.Dispose()} }
