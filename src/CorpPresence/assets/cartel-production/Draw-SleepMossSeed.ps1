param([string]$OutputDirectory = $PSScriptRoot)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# K/W/P are the outline and original seed-packet paper. O/E/Y mark the moss.
$palette = @{ K='#292234'; W='#D5D2B8'; P='#C5AC96'; O='#334246'; E='#3D823F'; Y='#7AB053' }
$rows = @(
    '................'
    '..KKKKKKKKKKKK..'
    '.KWWWWWWWWWWWWK.'
    '.KPPPPPPPPPPPPK.'
    '..KWWWWWWWWWWK..'
    '..KWWOOOOOWWK...'
    '..KWOYYEEYOWK...'
    '..KWOYEEEEOWK...'
    '..KWOEEEOOOWK...'
    '..KWWOOOOWWWK...'
    '..KWWWWWWWWPK...'
    '..KWPPWWPPWPK...'
    '..KPPPPPPPPPK...'
    '.KPPPPPPPPPPPPK.'
    '..KKKKKKKKKKKK..'
    '................'
)
$image = [Drawing.Bitmap]::new(16, 16, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    for ($y = 0; $y -lt $rows.Length; $y++) {
        if ($rows[$y].Length -ne 16) { throw "Expected 16 pixels in row $y." }
        for ($x = 0; $x -lt 16; $x++) {
            $ink = [string]$rows[$y][$x]
            if ($ink -eq '.') { continue }
            $image.SetPixel($x, $y, [Drawing.ColorTranslator]::FromHtml($palette[$ink]))
        }
    }
    $image.Save((Join-Path $OutputDirectory 'sleep-moss-seed.png'), [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $image.Dispose()
}
