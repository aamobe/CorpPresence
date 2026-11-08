param([string]$Root=(Join-Path $PSScriptRoot '..'))
$ErrorActionPreference='Stop';Add-Type -AssemblyName System.Drawing
$Root=(Resolve-Path $Root).Path;$folder=Join-Path $Root 'evidence/portraits'
$results=foreach($file in Get-ChildItem (Join-Path $folder 'envoy') -Filter '*.png'|Sort-Object Name){
    $im=[Drawing.Bitmap]::FromFile($file.FullName)
    try{
        $palette=@{};$alphas=@{};$main=0;$xmin=$im.Width;$ymin=$im.Height;$xmax=-1;$ymax=-1
        for($y=0;$y -lt $im.Height;$y++){for($x=0;$x -lt $im.Width;$x++){
            $c=$im.GetPixel($x,$y);$a=[int]$c.A;$alphas[$a]=1+$alphas[$a]
            if($a -lt 128){continue};$main++
            $hex='{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B;$palette[$hex]=1+$palette[$hex]
            $xmin=[Math]::Min($xmin,$x);$xmax=[Math]::Max($xmax,$x);$ymin=[Math]::Min($ymin,$y);$ymax=[Math]::Max($ymax,$y)
        }}
        [pscustomobject]@{Name=$file.BaseName;Width=$im.Width;Height=$im.Height;MainColors=$palette.Count;MainPixels=$main;Coverage=[Math]::Round($main/($im.Width*$im.Height),5);BoundingBox=@($xmin,$ymin,$xmax,$ymax);DisplayWidthAt211=[Math]::Round($im.Width*211.0/$im.Height,6);PPUAt100=[Math]::Round(100*$im.Height/211.0,6);AlphaHistogram=@($alphas.GetEnumerator()|Sort-Object Name|ForEach-Object{[pscustomobject]@{Alpha=$_.Key;Pixels=$_.Value}});TopColors=@($palette.GetEnumerator()|Sort-Object Value -Descending|Select-Object -First 24|ForEach-Object{[pscustomobject]@{Hex=$_.Key;Pixels=$_.Value}});SHA256=(Get-FileHash $file.FullName).Hash}
    }finally{$im.Dispose()}
}
$results|ConvertTo-Json -Depth 8|Set-Content (Join-Path $folder 'envoy-metrics.json') -Encoding utf8
$results|Select-Object Name,Width,Height,MainColors,Coverage,DisplayWidthAt211,PPUAt100|Format-Table
$comparisons=foreach($pair in @(@('n1x','n2x',2),@('n1x','n3x',4),@('s1x','s2x',2),@('s1x','s3x',3))){
    $a=[Drawing.Bitmap]::FromFile((Join-Path $folder "envoy/CorpPresence.portraits.malice_envoy_$($pair[0]).png"));$b=[Drawing.Bitmap]::FromFile((Join-Path $folder "envoy/CorpPresence.portraits.malice_envoy_$($pair[1]).png"))
    try{
        $n=[int]$pair[2];$diff=0
        if($a.Width*$n -ne $b.Width -or $a.Height*$n -ne $b.Height){throw 'Not exact integer dimensions'}
        for($y=0;$y -lt $b.Height;$y++){for($x=0;$x -lt $b.Width;$x++){
            $expected=$a.GetPixel([int][Math]::Floor($x/$n),[int][Math]::Floor($y/$n)).ToArgb()
            if($expected -ne $b.GetPixel($x,$y).ToArgb()){$diff++}
        }}
        [pscustomobject]@{From=$pair[0];To=$pair[1];Scale=$n;ComparedPixels=$b.Width*$b.Height;DifferentRGBA=$diff;DifferentPercent=[Math]::Round(100*$diff/($b.Width*$b.Height),3)}
    }finally{$a.Dispose();$b.Dispose()}
}
$comparisons|ConvertTo-Json|Set-Content (Join-Path $folder 'envoy-integer-copy-check.json') -Encoding utf8
$comparisons|Format-Table
