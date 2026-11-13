param([string]$OutputRoot = $PSScriptRoot)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-RunningState($group, $machine, $state, $indicators) {
    $source = Join-Path $PSScriptRoot "$group/$machine.png"
    $target = Join-Path $OutputRoot "$group/$machine-$state.png"
    $frame = [Drawing.Bitmap]::new($source)
    try {
        foreach ($indicator in $indicators) {
            $x, $y, $width, $height, $hex = $indicator
            $color = [Drawing.ColorTranslator]::FromHtml("#$hex")
            for ($row = $y; $row -lt $y + $height; $row++) {
                for ($column = $x; $column -lt $x + $width; $column++) {
                    $pixel = $frame.GetPixel($column, $row)
                    if ($pixel.A -ne 255 -or $pixel.ToArgb() -eq $color.ToArgb()) {
                        throw "Invalid indicator pixel in $machine at ($column,$row)."
                    }
                    $frame.SetPixel($column, $row, $color)
                }
            }
        }
        $frame.Save($target, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $frame.Dispose() }
}

# Brighten the power light, screen aperture and cultivation panel respectively.
New-RunningState 'mineral-recycling' 'mineral-crusher' 'running' @(,(38,34,3,2,'7AB053'))
New-RunningState 'mineral-recycling' 'powder-sieve' 'running' @(,(17,21,10,1,'8ABFBB'))
New-RunningState 'mineral-recycling' 'mineral-growth-station' 'running' @(,(37,51,4,2,'7AB053'))

# The preparation module lights up inside its recessed front strip.
New-RunningState 'cartel-production' 'alcohol-preparation-station' 'running' @(,(29,22,6,1,'67AAA5'))

# Each active vessel changes its own sight glass and gauge mark.
$blue = @((15,21,1,3,'67AAA5'), (29,23,1,1,'8ABFBB'))
$white = @((47,21,1,3,'D5D2B8'), (37,23,1,1,'D5D2B8'))
New-RunningState 'cartel-production' 'twin-vessel-reactor' 'running' ($blue + $white)
