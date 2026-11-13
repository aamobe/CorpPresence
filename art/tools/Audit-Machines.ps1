param([string]$ReferenceRoot = (Join-Path $PSScriptRoot '../../../原版参考-100-Rev13'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$out=Join-Path $PSScriptRoot '../evidence/machine-audit'
New-Item -ItemType Directory -Force $out | Out-Null
$groups=[ordered]@{
 MachinePurifier='purifier,purifier_w,purifier_on'; MachineFurnace='furnace,furnace_on';
 MachineAutoButcher='gibber'; MachineBlender='blender,blender_b';
 MachineBottlePrinter='bottle_printer'; MachineBoxDispenser='bottle_printer';
 MachineChemFinisher='chem_finisher,chem_finisher_b'; MachineEvaporator='evaporator,evaporator_b';
 MachineHeatingPlate='heating_plate,heating_plate_b'; MachinePrinter='printer_off,printer_on';
 MachineAlarm='alarm_system,alarm_system_r,alarm_system_b'; MachineMoistureFarm='moisture_farm,moisture_farm_w,moisture_farm_r';
 MachineDealMaker='dealmaker'; MachineDesequencer='desequencer,desequencer_r';
 MachineDnaManipulator='dna_manipulator,dna_manipulator_working'; MachineFeedDispenser='animal_feeder';
 MachineFridge='fridge'; MachineHydroponic='hydroponic,hydroponic_off';
 MachineMeatCulture='meat_culture_machine'; MachineProjector='projector';
 MachineRAS='ras_empty,ras,ras_full,ras_tank'; MachineRecharger='recharger_base,recharger_top,recharger_g,recharger_o';
 MachineStorageCoordinator='storage_coordinator'; MachineTurboBoosterAdv='turbo_booster_adv,turbo_booster_adv_ready,turbo_booster_adv_charging';
 MachineWineRack='wine_rack,wine_bottom'; MachineCassettePlayer='cassette_player';
 MachineBrokenHelper='broken_alarm,broken_furnace,broken_moisture_farm';
 Comparison='portable_water_purifier,animal_cage'
}
$files=Get-ChildItem (Join-Path $ReferenceRoot '美术') -Recurse -Filter '*.png' | Where-Object { $_.Directory.Name -eq 'Sprite' }
$records=@();$seen=@{}
foreach($group in $groups.Keys){foreach($name in $groups[$group].Split(',')){
 if($seen.ContainsKey($name)){continue};$seen[$name]=$true
 $matches=@($files | Where-Object BaseName -EQ $name)
 $code=Join-Path $ReferenceRoot "代码/$group.cs"
 $usage=if(Test-Path $code){ @(Select-String -LiteralPath $code -SimpleMatch ('"'+$name+'"') | ForEach-Object { $_.LineNumber }) }else{@()}
 if(!$matches.Count){$records+= [pscustomobject]@{Name=$name;Class=$group;Status='missing export';CodeLines=$usage};continue}
 $file=$matches[0];$im=[Drawing.Bitmap]::FromFile($file.FullName)
 try{
  $palette=@{};$alpha=@{};$n=0;$xmin=$im.Width;$ymin=$im.Height;$xmax=-1;$ymax=-1
  for($y=0;$y -lt $im.Height;$y++){for($x=0;$x -lt $im.Width;$x++){
   $c=$im.GetPixel($x,$y);$a=[string]$c.A;$alpha[$a]++
   if($c.A -ge 128){$hex='{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B;$palette[$hex]++;$n++;$xmin=[Math]::Min($xmin,$x);$xmax=[Math]::Max($xmax,$x);$ymin=[Math]::Min($ymin,$y);$ymax=[Math]::Max($ymax,$y)}
  }}
  $records += [pscustomobject]@{Name=$name;Class=$group;Status=if($n){'visible'}else{'no opaque pixels'};CodeLines=$usage;Source=$file.FullName;SHA256=(Get-FileHash $file.FullName).Hash;Width=$im.Width;Height=$im.Height;BBox=@($xmin,$ymin,$xmax,$ymax);MainPixels=$n;Alpha=$alpha;Palette=@($palette.GetEnumerator() | Sort-Object Value -Descending | ForEach-Object { [pscustomobject]@{RGB=$_.Key;Pixels=$_.Value} })}
 }finally{$im.Dispose()}
}}
$records | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $out 'inventory.json') -Encoding utf8
$valid=@($records | Where-Object Source)
for($page=0;$page -lt [Math]::Ceiling($valid.Count/12.0);$page++){
 $sheet=[Drawing.Bitmap]::new(1120,1170);$g=[Drawing.Graphics]::FromImage($sheet);$font=[Drawing.Font]::new('Consolas',10)
 try{
  $g.Clear([Drawing.Color]::FromArgb(255,45,42,48));$g.InterpolationMode='NearestNeighbor';$g.PixelOffsetMode='Half'
  for($j=0;$j -lt 12;$j++){
   $i=$page*12+$j;if($i -ge $valid.Count){break};$r=$valid[$i];$im=[Drawing.Bitmap]::FromFile($r.Source)
   try{
    $x=($j%4)*280+8;$y=[int][Math]::Floor($j/4.0)*390+8
    $g.DrawString("$($r.Name)`n$($r.Width)x$($r.Height)  $($r.Palette.Count) colors",$font,[Drawing.Brushes]::White,$x,$y)
    $g.DrawImage($im,[Drawing.Rectangle]::new($x,$y+42,$im.Width,$im.Height),0,0,$im.Width,$im.Height,[Drawing.GraphicsUnit]::Pixel)
    $g.DrawImage($im,[Drawing.Rectangle]::new($x,$y+112,$im.Width*3,$im.Height*3),0,0,$im.Width,$im.Height,[Drawing.GraphicsUnit]::Pixel)
    $k=0;foreach($p in $r.Palette){if($k -ge 27){break};$b=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#'+$p.RGB));try{$g.FillRectangle($b,$x+$k*10,$y+365,9,10)}finally{$b.Dispose()};$k++}
   }finally{$im.Dispose()}
  }
  $sheet.Save((Join-Path $out ('machines-{0}.png' -f ($page+1))))
 }finally{$g.Dispose();$font.Dispose();$sheet.Dispose()}
}
$rows=@('# 原版机器资源清单','','来源：本机 `原版参考-100-Rev13`；扫描 Machine*.cs（排除 Machinery UI 类），补查动态状态、叠加层和同名前缀。机器工厂 26 个，其中两种分配器共用 bottle_printer；3 个辅助类不另算机器。原文件只读。','','图板每格上方 1×、下方 3×最近邻；色板按主体使用量排序，主体阈值 Alpha≥128。阅读器缩放会影响实际显示倍率。无源码行号的同名前缀图仅为补充资源，不据名称推断当前游戏会使用。','','|资源|类别|尺寸|主体色数|代码行号|状态|','|---|---|---|---:|---|---|')
foreach($r in $records){$rows+='|'+(@($r.Name,$r.Class,"$($r.Width)×$($r.Height)",$r.Palette.Count,($r.CodeLines -join ','),$r.Status)-join '|')+'|'}
$rows | Set-Content (Join-Path $out 'INDEX.md') -Encoding utf8
$records | Group-Object Status | Select-Object Name,Count
