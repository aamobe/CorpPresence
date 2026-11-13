param([string]$OutputRoot = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$machines = [ordered]@{
    'mineral-crusher' = 'mineral-recycling'
    'powder-sieve' = 'mineral-recycling'
    'sintering-station' = 'mineral-recycling'
    'mineral-growth-station' = 'mineral-recycling'
    'alcohol-preparation-station' = 'cartel-production'
    'twin-vessel-reactor' = 'cartel-production'
}

foreach ($name in $machines.Keys) {
    $group = $machines[$name]
    $front = [Drawing.Bitmap]::new((Join-Path $PSScriptRoot "$group/$name.png"))
    $sprite = [Drawing.Bitmap]::new($front.Width, $front.Height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $depth = 4
        # Project the front with a shallow stair-step and extrude back and right.
        # Plot rear slices first so front details occlude the exposed metal faces.
        for ($slice = $depth; $slice -ge 0; $slice--) {
            $top = [Drawing.ColorTranslator]::FromHtml('#A8AEA5')
            $bevel = [Drawing.ColorTranslator]::FromHtml('#C4C4B0')
            $side = [Drawing.ColorTranslator]::FromHtml('#44464C')
            $edge = [Drawing.ColorTranslator]::FromHtml('#292234')
            for ($y = 0; $y -lt $front.Height; $y++) {
                for ($x = 0; $x -lt $front.Width; $x++) {
                    $pixel = $front.GetPixel($x, $y)
                    if ($pixel.A -eq 0) { continue }
                    $px = $x + $slice - 2
                    $py = $y + [int][Math]::Floor($x / 16) - 1 - [int][Math]::Ceiling($slice / 2)
                    if ($px -lt 0 -or $px -ge $sprite.Width -or $py -lt 0 -or $py -ge $sprite.Height) {
                        throw "Projection clips $name at ($px,$py)."
                    }
                    $color = if ($slice -eq 0) { $pixel }
                    elseif ($y -gt 0 -and $front.GetPixel($x, $y - 1).A -eq 0) {
                        if ($slice -eq $depth) { $edge } elseif ($slice -eq 1) { $bevel } else { $top }
                    }
                    elseif ($slice -eq $depth) { $edge }
                    else { $side }
                    $sprite.SetPixel($px, $py, $color)
                }
            }
        }
        $sprite.Save((Join-Path $OutputRoot "$group/$name-oblique.png"), [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $front.Dispose(); $sprite.Dispose() }
}
