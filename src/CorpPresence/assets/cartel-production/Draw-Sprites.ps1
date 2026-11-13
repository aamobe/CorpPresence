param([string]$OutputDirectory = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$palette = @{
    K='292234'; D='373541'; S='44464C'; G='53585B'; M='5F6868'; H='88948A'
    L='A8AEA5'; P='C4C4B0'; W='D5D2B8'; N='80929B'; A='435A5F'
    B='273456'; C='355271'; J='67AAA5'; T='45918B'; I='8ABFBB'
    V='743D70'; Q='5E2B45'; R='7D4F4A'; U='8F6B5A'; F='B68B74'
    O='334246'; E='3D823F'; Y='7AB053'; Z='303A41'
}

function Start-Sprite($width,$height) {
    $script:bitmap = [Drawing.Bitmap]::new($width,$height,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
}
function Paint($x,$y,$width,$height,$ink) {
    $color = [Drawing.ColorTranslator]::FromHtml('#' + $palette[$ink])
    for($row=$y; $row -lt $y+$height; $row++) {
        for($col=$x; $col -lt $x+$width; $col++) {
            $bitmap.SetPixel($col,$row,$color)
        }
    }
}
function Save-Sprite($name) {
    try { $bitmap.Save((Join-Path $OutputDirectory "$name.png"),[Drawing.Imaging.ImageFormat]::Png) }
    finally { $bitmap.Dispose() }
}
function Draw-Item($name,$rows) {
    Start-Sprite 16 16
    for($y=0; $y -lt $rows.Count; $y++) {
        if($rows[$y].Length -ne 16) { throw "$name row $y must contain 16 pixels." }
        for($x=0; $x -lt 16; $x++) {
            $ink = [string]$rows[$y][$x]
            if($ink -ne '.') { Paint $x ($y+3) 1 1 $ink }
        }
    }
    Save-Sprite $name
}

Draw-Item 'zol-tablets' @(
    '...KKKKKKKKKK...'
    '..KLLNNNNLLLGK..'
    '..KVVQVVQVVVGK..'
    '..KNLLLLLLLLGK..'
    '..KNWNLWNLWNGK..'
    '..KWPWWPWWPWGK..'
    '..KNPMLPMLPMGK..'
    '..KNNNNNNNNMGK..'
    '...KKKKKKKKKK...'
)
Draw-Item 'nicotine-tin' @(
    '.....KKKKKK.....'
    '...KKWWWWWWKK...'
    '..KWPPPPPPPPWK..'
    '..KWPPPPPPPPWK..'
    '..KFUUUUUUUURK..'
    '..KUPPPPPPPPUK..'
    '..KSPPPPPPPPSK..'
    '...KSMMMMMMSK...'
    '....KSSSSSSK....'
    '.....KKKKKK.....'
)
Draw-Item 'ice-shard-pouch' @(
    '...KKKKKKKKKK...'
    '...KPNNNNNNPK...'
    '....KNNNNNAK....'
    '....KNNNWKKKK...'
    '....KNWINKWQK...'
    '....KNIJIKPPK...'
    '....KIAJJKWQK...'
    '....KAJINIKK....'
    '...KNNIAAIMK....'
    '...KKKKKKKKK....'
)
Draw-Item 'sleep-moss-block' @(
    '......KKKK......'
    '....KKYEEEKK....'
    '...KYEEEEYYYK...'
    '..KYEYYEEYEEK...'
    '..KEEEEEEEEOOK..'
    '..KEOOOEOOOORK..'
    '...KRURRURRRK...'
    '...KRROORROOK...'
    '....KKKKKKKK....'
)
Draw-Item 'blue-shell-resin' @(
    '.......KKKK.....'
    '.....KKNNCCK....'
    '....KNNCCCCCK...'
    '...KNNCCCCCCBK..'
    '..KCCCCCCCCCBBK.'
    '..KCCCCCCCBBBAK.'
    '...KBCBCCBBAAK..'
    '....KBAABAAAAK..'
    '.....KKKKKKKK...'
)
Draw-Item 'light-salt-core' @(
    '.......KK.......'
    '......KPWK......'
    '.....KPWPLK.....'
    '....KPWPPMLK....'
    '...KPWPPMMLMK...'
    '...KLPPLLMMSK...'
    '....KLLMMSMK....'
    '.....KMSSGK.....'
    '......KKKK......'
)
Draw-Item 'sleep-moss-gel' @(
    '.....KKKKKK.....'
    '...KKLNNNLNKK...'
    '...KCNNNCNCBK...'
    '...KABBBBBBAK...'
    '...KAJTTTTTAK...'
    '...KAITTTTOAK...'
    '...KAJTTTOOAK...'
    '...KATTTTOOAK...'
    '....KNNAAAMK....'
    '.....KKKKKK.....'
)
Draw-Item 'light-salt-precipitate' @(
    '........KK......'
    '.......KWPK.....'
    '.....KKWPPKK....'
    '....KWPPWPPPK...'
    '...KWPPWPPLLLK..'
    '..KLLPPWPLLNLLK.'
    '..KNNLLLLLLNNNK.'
    '...KMMNNNNMMK...'
    '....KKKKKKKK....'
)

# 回酒器。分别画承力架、旧罐、泵、工业分离模块和接瓶工位。
Start-Sprite 48 48
Paint 5 39 38 5 K
Paint 6 39 36 2 H
Paint 6 41 36 2 S
Paint 7 43 8 2 D
Paint 33 43 8 2 D
Paint 8 26 5 14 D
Paint 34 26 5 14 D
# 旧罐和两道束带
Paint 8 7 10 3 K
Paint 9 8 8 2 F
Paint 6 8 14 21 K
Paint 4 11 17 16 K
Paint 5 12 15 14 U
Paint 7 9 12 3 F
Paint 6 13 2 12 R
Paint 7 26 12 2 R
Paint 5 14 15 2 D
Paint 5 14 15 1 H
Paint 5 23 15 2 D
Paint 5 23 15 1 M
Paint 10 6 5 3 S
Paint 10 6 5 1 L
# 手泵和异厂进料转接
Paint 14 18 9 5 Z
Paint 14 18 8 1 N
Paint 16 20 5 7 D
Paint 17 21 3 4 M
Paint 17 16 3 5 M
Paint 16 15 8 2 K
Paint 16 15 7 1 R
Paint 20 21 4 3 H
Paint 21 22 5 2 A
# 工业模块、工厂端板和铭牌
Paint 24 7 18 23 K
Paint 22 9 21 18 K
Paint 24 8 17 3 P
Paint 25 8 7 1 W
Paint 32 8 5 1 L
Paint 23 11 19 15 A
Paint 25 11 13 12 N
Paint 25 12 10 2 H
Paint 23 11 2 15 M
Paint 39 11 3 15 M
Paint 40 12 1 2 L
Paint 40 22 1 2 H
Paint 27 15 8 4 Z
Paint 28 16 6 2 P
Paint 29 16 2 1 S
Paint 33 17 1 1 G
Paint 25 24 13 3 G
Paint 29 26 8 3 N
# 出料嘴、刻度、瓶身、卡扣
Paint 31 29 4 3 S
Paint 32 29 2 2 L
Paint 31 31 4 2 A
Paint 30 33 7 7 K
Paint 31 33 5 6 N
Paint 31 37 5 2 C
Paint 32 34 1 3 P
Paint 38 30 4 8 Z
Paint 38 31 3 1 W
Paint 38 34 2 1 L
Paint 28 36 2 4 G
Paint 36 36 2 4 G
Paint 29 38 8 2 M
Paint 29 38 2 1 L
Paint 35 38 2 1 H
# 补装板与手写检修短记
Paint 8 33 11 5 S
Paint 8 33 8 1 M
Paint 10 35 2 1 F
Paint 12 34 1 1 F
Paint 14 35 2 1 U
Paint 16 33 3 1 H
Save-Sprite 'hand-pump-reclaimer'

# 双胆槽。支撑、分流、两只同厂反应胆和两套表分别绘制。
Start-Sprite 64 48
Paint 7 6 50 4 D
Paint 8 6 48 1 M
Paint 8 7 4 8 S
Paint 52 7 5 8 S
Paint 8 8 1 6 M
Paint 55 8 1 6 M
Paint 4 37 56 7 K
Paint 5 37 54 2 M
Paint 5 39 54 4 S
Paint 6 43 9 2 D
Paint 49 43 9 2 D
Paint 7 12 6 26 D
Paint 8 12 3 26 M
Paint 51 12 6 26 D
Paint 53 12 3 26 M
Paint 25 20 14 18 S
Paint 27 21 10 16 G
# 手拨闸、共用进料和外露分管
Paint 28 4 8 5 K
Paint 29 5 6 3 L
Paint 30 5 4 1 W
Paint 30 8 4 9 D
Paint 31 8 2 7 N
Paint 16 9 32 4 D
Paint 17 9 30 2 N
Paint 15 10 4 7 S
Paint 45 10 4 7 S
Paint 16 11 2 5 L
Paint 46 11 2 5 P
Paint 28 12 8 7 K
Paint 29 13 6 5 M
Paint 30 14 4 3 H
Paint 32 15 7 2 D
Paint 37 14 3 3 R
Paint 37 14 2 1 F
foreach($side in @(
    @{ X=7; Base='C'; Edge='B'; Light='N'; Glass='I' },
    @{ X=39; Base='P'; Edge='H'; Light='W'; Glass='L' }
)) {
    $x = $side.X
    Paint ($x+1) 12 16 23 K
    Paint $x 15 18 16 K
    Paint ($x+2) 13 14 3 $side.Light
    Paint ($x+1) 16 16 13 $side.Base
    Paint ($x+1) 16 2 13 $side.Edge
    Paint ($x+15) 16 2 13 $side.Edge
    Paint ($x+1) 15 16 2 N
    Paint ($x+3) 15 5 1 L
    Paint ($x+8) 15 6 1 P
    Paint ($x+1) 28 16 3 M
    Paint ($x+3) 28 4 1 N
    Paint ($x+7) 28 6 1 L
    Paint ($x+2) 31 14 3 $side.Edge
    Paint ($x+7) 19 5 8 Z
    Paint ($x+8) 20 3 6 $side.Edge
    Paint ($x+8) 21 1 3 $side.Glass
    Paint ($x+9) 24 2 1 $side.Glass
    Paint ($x+6) 34 7 4 Z
    Paint ($x+7) 34 5 1 N
    Paint ($x+8) 36 3 1 $side.Base
    # 法兰螺栓有固定间距。
    Paint ($x+2) 16 1 1 G
    Paint ($x+15) 16 1 1 S
    Paint ($x+2) 29 1 1 H
    Paint ($x+15) 29 1 1 H
}
# 两只独立机械进度表，置于中间支架。
foreach($x in 25,33) {
    Paint $x 22 6 8 Z
    Paint ($x+1) 23 4 5 P
    Paint ($x+1) 23 1 1 N
    Paint ($x+4) 23 1 1 T
    Paint ($x+2) 26 2 1 D
    Paint ($x+3) 25 1 1 D
    Paint ($x+1) 28 4 1 N
}
# 补焊和不规则检修牌留在架上。
Paint 24 39 13 3 Z
Paint 26 39 10 1 H
Paint 28 40 2 1 F
Paint 32 40 3 1 U
Paint 6 40 5 1 M
Paint 51 40 5 1 M
# The shared frame and factory flange bevels catch the overhead light.
Paint 9 6 13 1 H
Paint 22 6 19 1 L
Paint 41 6 13 1 N
foreach ($x in 7,39) {
    Paint ($x+2) 15 5 1 L
    Paint ($x+7) 15 6 1 W
    Paint ($x+13) 15 3 1 P
    Paint ($x+3) 28 4 1 L
    Paint ($x+7) 28 6 1 P
}
Paint 6 38 16 1 H
Paint 22 38 19 1 N
Paint 41 38 17 1 H
Save-Sprite 'twin-vessel-reactor'
