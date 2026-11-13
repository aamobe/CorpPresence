param([string]$OutputDirectory = $PSScriptRoot)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# Same RGB values as the delivered sleep-moss-block.png.
$ink = @{
    K = '#292234'
    O = '#334246'
    E = '#3D823F'
    Y = '#7AB053'
    R = '#7D4F4A'
    U = '#8F6B5A'
}

# Stamps are drawn at 1× on the 48×64 tray overlay, with a common contact line at y=46.
# "." leaves the layer below visible. The permanent inoculation patch is drawn last.
function Paint-Cluster($image, [int]$x, [int]$y, [string[]]$rows) {
    for ($row = 0; $row -lt $rows.Length; $row++) {
        for ($column = 0; $column -lt $rows[$row].Length; $column++) {
            $pixel = [string]$rows[$row][$column]
            if ($pixel -eq '.') { continue }
            $image.SetPixel(($x + $column), ($y + $row), [Drawing.ColorTranslator]::FromHtml($ink[$pixel]))
        }
    }
}

$seed = @(
    '....KKKK.....'
    '..KKYEEEKK...'
    '.KYYEEEEYYK..'
    'KYEEEEEEEEOK.'
    'KEEEEEOEOOOK.'
    'KOOOOROOROOK.'
    '.KRRURROROK..'
    '..KKKKKKKK...'
)
$left = @(
    '......KKKK......'
    '....KKYYEEK.....'
    '..KKYYEEEOOK....'
    '.KYYEEEEEEOOK...'
    '.KEEEEYEKEEOOK..'
    'KEEEEEEEEOEOOOK.'
    'KEEEEOOOOEOOOORK'
    '.KOORROOOOORROK.'
    '..KRRURROORRUK..'
    '...KKKKKKKKKK...'
)
$right = @(
    '....KKKKK.......'
    '..KKYEEEEKK.....'
    '.KYEEEEYYEEK....'
    '.KEEEEEYEEEEKK..'
    'KEEEOEEEEEEEOOK.'
    'KEEEEOOEOOEEOOK.'
    '.KOOOOROOORROOOK'
    '..KRURROOOOORRK.'
    '...KKKKKKKKKKK..'
)
$crown = @(
    '......KKKKKK........'
    '....KKYYEEEEKK......'
    '...KYYEEEEEEEOK.....'
    '..KYYEEEKKYEYYOK....'
    '.KYEYEEKEYYEEEEOK...'
    '.KEEEEOEOEEEEEEOKK..'
    'KEEEEOOEEEEEEEYEOKK.'
    'KEEEOEEEEEEEEEEEEOOK'
    '.KEEEEEEOEEEOOOOOOK.'
    '..KOOEOOOOOOOOOOROK.'
    '...KRROORROORRROOK..'
    '....KKKKKKKKKKKK....'
)

$stages = 'inoculated', 'spreading', 'harvestable'
for ($stage = 0; $stage -lt $stages.Length; $stage++) {
    $image = [Drawing.Bitmap]::new(48, 64, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        if ($stage -ge 1) {
            Paint-Cluster $image 9 36 $left
            Paint-Cluster $image 24 37 $right
        }
        if ($stage -ge 2) {
            Paint-Cluster $image 14 31 $crown
            Paint-Cluster $image 4 37 $left
            Paint-Cluster $image 29 38 $right
        }
        Paint-Cluster $image 17 39 $seed
        $path = Join-Path $OutputDirectory "sleep-moss-hydroponic-$($stages[$stage]).png"
        $image.Save($path, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $image.Dispose()
    }
}
