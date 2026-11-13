$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies @([Drawing.Bitmap].Assembly.Location,[Drawing.Color].Assembly.Location) -TypeDefinition @'
using System;
using System.Drawing;
public static class NativeRaster {
 public static Rectangle Bounds(Bitmap src) {
  int l=src.Width,t=src.Height,r=-1,b=-1;
  for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++)if(src.GetPixel(x,y).A>=128){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);}
  if(r<l)throw new Exception("No visible specimen");return new Rectangle(l,t,r-l+1,b-t+1);
 }
 public static Bitmap Convert(Bitmap src,int w,int h,Color[] palette) {
  Bitmap dst=new Bitmap(w,h);
  for(int y=0;y<h;y++) for(int x=0;x<w;x++) {
   int[] votes=new int[palette.Length+1];
   for(int j=0;j<7;j++) for(int i=0;i<7;i++) {
    int sx=Math.Min(src.Width-1,(int)((x+(i+0.5)/7)*src.Width/w));
    int sy=Math.Min(src.Height-1,(int)((y+(j+0.5)/7)*src.Height/h));
    Color c=src.GetPixel(sx,sy);
    if(c.A<128 || (c.G>180 && c.G>c.R*1.65 && c.G>c.B*1.65)){votes[palette.Length]++;continue;}
    int best=0;double dist=double.MaxValue;
    for(int k=0;k<palette.Length;k++) {
     Color p=palette[k];double dr=c.R-p.R,dg=c.G-p.G,db=c.B-p.B;
     double d=dr*dr*0.3+dg*dg*0.5+db*db*0.2;
     if(d<dist){dist=d;best=k;}
    }
    votes[best]++;
   }
   int chosen=0;for(int k=1;k<votes.Length;k++)if(votes[k]>votes[chosen])chosen=k;
   dst.SetPixel(x,y,chosen==palette.Length?Color.FromArgb(0,0,0,0):palette[chosen]);
  }
  return dst;
 }
}
'@
$paletteHex=@('292234','303A41','6B5269','777081','80929B','A8AEA5','C4C4B0','D5D2B8')
$palette=@($paletteHex | ForEach-Object {[Drawing.ColorTranslator]::FromHtml('#'+$_)})
$out=Join-Path $PSScriptRoot 'native';New-Item -ItemType Directory -Force $out | Out-Null
$machine=(Resolve-Path (Join-Path $PSScriptRoot '../../../src/CorpPresence/assets/machine-redesign-20261113/mineral-growth-station.png')).Path
function SaveMatrix($im,$name){
 $p=@{};$list=[Collections.Generic.List[string]]::new();$rows=for($y=0;$y -lt $im.Height;$y++){$line='';for($x=0;$x -lt $im.Width;$x++){$c=$im.GetPixel($x,$y);if(!$c.A){$line+='.'}else{$hex='{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B;if(!$p.ContainsKey($hex)){$p[$hex]=$list.Count;$list.Add($hex)};$line+='0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ'[$p[$hex]]}};$line}
 [pscustomobject]@{Width=$im.Width;Height=$im.Height;Palette=@($list);Rows=@($rows)}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $out ($name+'.pixels.json')) -Encoding utf8
 $im.Save((Join-Path $out ($name+'.png')))
}
$base=[Drawing.Bitmap]::FromFile($machine)
try{
 $glow=[Drawing.Bitmap]::new($base)
 try{
  for($y=22;$y -le 44;$y++){for($x=9;$x -le 29;$x++){$c=$glow.GetPixel($x,$y);$h='{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B;if($h -eq '275654'){$glow.SetPixel($x,$y,[Drawing.ColorTranslator]::FromHtml('#2F756F'))}elseif($h -eq '2F756F'){$glow.SetPixel($x,$y,[Drawing.ColorTranslator]::FromHtml('#45918B'))}}}
  SaveMatrix $glow 'mineral-growth-station-running'
 }finally{$glow.Dispose()}
}finally{$base.Dispose()}
$specs=@(@('ore-growing',6,8),@('ore-mature',10,16),@('light-salt-core-growing',8,7),@('light-salt-core-mature',14,13))
$layout=@()
foreach($s in $specs){
 $sourceName=if($s[0].StartsWith('ore-')){$s[0]+'-vertical-generated.png'}else{$s[0]+'-generated.png'}
 $path=Join-Path $PSScriptRoot $sourceName;if(!(Test-Path $path)){continue}
 $src=[Drawing.Bitmap]::FromFile($path)
 try{
  $bbox=[NativeRaster]::Bounds($src);$crop=$src.Clone($bbox,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
  try{$scale=[Math]::Min($s[1]/$bbox.Width,$s[2]/$bbox.Height);$w=[Math]::Max(1,[int][Math]::Round($bbox.Width*$scale));$h=[Math]::Max(1,[int][Math]::Round($bbox.Height*$scale));$small=[NativeRaster]::Convert($crop,$w,$h,$palette)}finally{$crop.Dispose()}
  try{
   $im=[Drawing.Bitmap]::new(48,64);$x0=20-[int][Math]::Floor($w/2);$y0=38-$h
   try{for($y=0;$y -lt $h;$y++){for($x=0;$x -lt $w;$x++){$im.SetPixel($x0+$x,$y0+$y,$small.GetPixel($x,$y))}};SaveMatrix $im $s[0]}finally{$im.Dispose()}
   $layout += [pscustomobject]@{Name=$s[0];SourceBounds=@($bbox.X,$bbox.Y,$bbox.Width,$bbox.Height);PlacedRect=@($x0,$y0,$w,$h);ContactY=37;Canvas=@(48,64);Pivot=@(0.5,0.5);Offset=@(0,0)}
  }finally{$small.Dispose()}
 }finally{$src.Dispose()}
}
$layout|ConvertTo-Json -Depth 5|Set-Content (Join-Path $out 'layout.json') -Encoding utf8
