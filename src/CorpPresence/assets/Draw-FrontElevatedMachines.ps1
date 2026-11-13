param([string]$OutputRoot = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$palette = @{
    edge='292234'; recess='303A41'; iron='44464C'; gray='5F6868'
    steel='80929B'; mist='A8AEA5'; pale='C4C4B0'; light='D5D2B8'
    teal='45918B'; aqua='67AAA5'; blue='355271'; bronze='8F6B5A'
}
$brush = @{}
foreach ($ink in $palette.Keys) {
    $brush[$ink] = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml("#$($palette[$ink])"))
}

function Face($ink,[int[]]$coords) {
    $points = for ($i=0; $i -lt $coords.Length; $i+=2) {
        [Drawing.Point]::new($coords[$i],$coords[$i+1])
    }
    $graphics.FillPolygon($brush[$ink],[Drawing.Point[]]$points)
}
function Bar($ink,$x,$y,$w,$h) { $graphics.FillRectangle($brush[$ink],$x,$y,$w,$h) }

# A top face widens towards the viewer, with horizontal front and rear edges.
function Top($left,$right,$rear,$front,$surface) {
    Face edge ($left,$front,($left+5),$rear,($right-5),$rear,$right,$front,$right,($front+2),$left,($front+2))
    Face $surface (($left+2),($front-1),($left+6),($rear+1),($right-6),($rear+1),($right-2),($front-1))
    Bar mist ($left+1) $front ($right-$left-2) 1
    Bar pale ($left+3) $front ($right-$left-6) 1
    Bar light ($left+5) $front 7 1
}

function Draw($group,$name,[scriptblock]$topLayer) {
    $base = [Drawing.Bitmap]::new((Join-Path $PSScriptRoot "$group/$name.png"))
    $frame = [Drawing.Bitmap]::new($base.Width,$base.Height,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($frame)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::None
    try {
        $graphics.DrawImageUnscaled($base,0,0)
        & $topLayer
        $frame.Save((Join-Path $OutputRoot "$group/$name-front-elevated.png"),[Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $graphics.Dispose(); $frame.Dispose(); $base.Dispose() }
}

try {
    Draw 'mineral-recycling' 'mineral-crusher' {
        # Open hopper, with the rear inner wall visible above the throat.
        Top 4 44 3 13 'steel'
        Face recess (8,11,12,5,36,5,40,11)
        Face gray (12,7,36,7,38,10,10,10)
        Bar iron 10 10 28 2
        Bar mist 8 12 32 1
        Bar pale 10 13 28 1
        Face iron (5,15,43,15,34,20,14,20)
        Bar steel 15 19 18 1
    }
    Draw 'mineral-recycling' 'powder-sieve' {
        # Screen housing top and the smaller recessed powder-box dock.
        Top 5 44 7 18 'teal'
        Face aqua (10,16,14,10,39,10,42,16)
        Face teal (12,15,15,11,38,11,40,15)
        Top 5 29 4 12 'steel'
        Face recess (8,11,11,6,23,6,26,11)
        Bar gray 10 9 14 1
    }
    Draw 'mineral-recycling' 'sintering-station' {
        Top 4 44 4 15 'steel'
        foreach ($x in 7,19,31) {
            Face mist ($x,12,($x+2),6,($x+9),6,($x+11),12)
            Face recess (($x+2),11,($x+3),7,($x+8),7,($x+9),11)
            Bar gray ($x+3) 10 5 1
        }
        Bar iron 5 16 38 1
    }
    Draw 'mineral-recycling' 'mineral-growth-station' {
        Top 4 44 3 13 'steel'
        Face gray (10,11,14,5,34,5,38,11)
        Face blue (13,10,16,6,32,6,35,10)
        Bar aqua 15 10 18 1
        Bar iron 6 14 36 2
    }
    Draw 'cartel-production' 'alcohol-preparation-station' {
        Top 4 24 4 12 'bronze'
        Face recess (7,11,10,6,18,6,21,11)
        Bar bronze 10 9 8 2
        Top 22 44 5 13 'steel'
        Bar mist 28 7 10 2
        Bar pale 29 7 8 1
        Bar iron 24 14 18 1
    }
    Draw 'cartel-production' 'twin-vessel-reactor' {
        # Paired flange tops share the same front elevation and depth.
        foreach ($x in 7,39) {
            $surface = if ($x -eq 7) { 'blue' } else { 'mist' }
            Face $surface ($x,16,($x+4),11,($x+14),11,($x+18),16)
            Bar pale ($x+1) 16 16 1
            Bar steel ($x+3) 17 12 1
            Bar gray ($x+7) 11 4 3
            Bar mist ($x+7) 11 4 1
        }
        Bar pale 17 9 13 1
        Bar mist 34 9 13 1
    }
}
finally { foreach ($item in $brush.Values) { $item.Dispose() } }
