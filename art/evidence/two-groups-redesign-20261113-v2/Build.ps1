$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies @([Drawing.Bitmap].Assembly.Location,[Drawing.Color].Assembly.Location) -TypeDefinition @'
using System;
using System.Drawing;
public static class SpriteV2 {
 public static bool Background(Color c) {return c.A<128 || (c.G>150 && c.G>c.R*1.6 && c.G>c.B*1.6);}
 public static Rectangle Bounds(Bitmap b) {int l=b.Width,t=b.Height,r=-1,d=-1;for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(!Background(b.GetPixel(x,y))){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);d=Math.Max(d,y);}return new Rectangle(l,t,r-l+1,d-t+1);}
 public static Bitmap Sample(Bitmap src,Rectangle box,int w,int h,Color[] palette){
  var b=new Bitmap(w,h);for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int[] votes=new int[palette.Length+1];
   for(int j=0;j<7;j++)for(int i=0;i<7;i++){
    var c=src.GetPixel(box.X+Math.Min(box.Width-1,(int)((x+(i+.5)/7)*box.Width/w)),box.Y+Math.Min(box.Height-1,(int)((y+(j+.5)/7)*box.Height/h)));
    if(Background(c)){votes[palette.Length]++;continue;}
    int best=0;double bd=double.MaxValue;for(int k=0;k<palette.Length;k++){var p=palette[k];double r=c.R-p.R,g=c.G-p.G,z=c.B-p.B,d=.3*r*r+.5*g*g+.2*z*z;if(d<bd){best=k;bd=d;}}votes[best]++;
   }
   int v=0;for(int k=1;k<votes.Length;k++)if(votes[k]>votes[v])v=k;b.SetPixel(x,y,v==palette.Length?Color.FromArgb(0,0,0,0):palette[v]);
  }return b;
 }
}
'@
$hex=@('292234','303A41','6B5269','777081','80929B','A8AEA5','C4C4B0','D5D2B8','275654','2F756F','8ABFBB','CCB987','79746E','45918B','88948A','5F6868','273456','355271','435A5F','9C3738')
$palette=@($hex|ForEach-Object{[Drawing.ColorTranslator]::FromHtml('#'+$_)})
$out=Join-Path $PSScriptRoot 'native'; New-Item -ItemType Directory -Force $out|Out-Null
function SaveSprite($b,$name){
 $rows=for($y=0;$y -lt $b.Height;$y++){$r='';for($x=0;$x -lt $b.Width;$x++){$c=$b.GetPixel($x,$y);if(!$c.A){$r+='.'}else{$h='{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B;$r+='0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ'[[array]::IndexOf($hex,$h)]}};$r}
 @{Width=$b.Width;Height=$b.Height;Palette=$hex;Rows=@($rows)}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $out ($name+'.pixels.json')) -Encoding utf8
 $b.Save((Join-Path $out ($name+'.png')))
}
$specs=@(@('silicon-plate',32,32,30,30),@('electronic-substrate',32,16,30,14),@('electronic-blueprint',16,16,14,14),@('crafting-station',48,48,46,46),@('ore-growing',48,64,7,8),@('ore-mature',48,64,11,16),@('light-salt-core-growing',48,64,7,7),@('light-salt-core-mature',48,64,12,14))
$layout=@()
foreach($s in $specs){
 $path=Join-Path $PSScriptRoot ($s[0]+'-generated.png');if(!(Test-Path $path)){continue}
 $src=[Drawing.Bitmap]::FromFile($path)
 try{
  $box=[SpriteV2]::Bounds($src);$scale=[Math]::Min($s[3]/$box.Width,$s[4]/$box.Height);$w=[Math]::Max(1,[int][Math]::Round($scale*$box.Width));$h=[Math]::Max(1,[int][Math]::Round($scale*$box.Height))
  $sm=[SpriteV2]::Sample($src,$box,$w,$h,$palette);$dst=[Drawing.Bitmap]::new([int]$s[1],[int]$s[2]);$x0=[int][Math]::Floor(($s[1]-$w)/2);$y0=[int][Math]::Floor(($s[2]-$h)/2)
  if($s[2] -eq 64){$x0=20-[int][Math]::Floor($w/2);$y0=38-$h}
  try{for($y=0;$y -lt $h;$y++){for($x=0;$x -lt $w;$x++){$dst.SetPixel($x+$x0,$y+$y0,$sm.GetPixel($x,$y))}};SaveSprite $dst $s[0]}finally{$sm.Dispose();$dst.Dispose()}
  $layout+=@{Name=$s[0];SourceSize=@($src.Width,$src.Height);SourceBounds=@($box.X,$box.Y,$box.Width,$box.Height);PlacedRect=@($x0,$y0,$w,$h)}
 }finally{$src.Dispose()}
}
$layout|ConvertTo-Json -Depth 5|Set-Content (Join-Path $PSScriptRoot 'layout.json') -Encoding utf8
