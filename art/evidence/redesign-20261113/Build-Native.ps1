$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies @([Drawing.Bitmap].Assembly.Location,[Drawing.Color].Assembly.Location) -TypeDefinition @'
using System;
using System.Drawing;
public static class NativeRaster {
 public static Bitmap Convert(Bitmap src,int w,int h,Color[] palette) {
  Bitmap dst=new Bitmap(w,h);
  for(int y=0;y<h;y++) for(int x=0;x<w;x++) {
   int[] votes=new int[palette.Length+1];
   for(int j=0;j<7;j++) for(int i=0;i<7;i++) {
    int sx=Math.Min(src.Width-1,(int)((x+(i+0.5)/7)*src.Width/w));
    int sy=Math.Min(src.Height-1,(int)((y+(j+0.5)/7)*src.Height/h));
    Color c=src.GetPixel(sx,sy);
    if(c.G>180 && c.G>c.R*1.65 && c.G>c.B*1.65){votes[palette.Length]++;continue;}
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
$palette=@('292234','303A41','6B5269','777081','80929B','A8AEA5','C4C4B0','D5D2B8','275654','2F756F','8ABFBB','4A2F38','853737','C76A50') | ForEach-Object {[Drawing.ColorTranslator]::FromHtml('#'+$_)}
$specs=@(@('mineral-crusher',48,48),@('powder-sieve',48,48),@('sintering-station',48,48),@('mineral-growth-station',48,64),@('alcohol-preparation-station',48,48),@('twin-vessel-reactor',64,48))
$out=Join-Path $PSScriptRoot 'native';New-Item -ItemType Directory -Force $out | Out-Null
foreach($s in $specs){
 $src=[Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot ($s[0]+'-generated.png')))
 try{$dst=[NativeRaster]::Convert($src,$s[1],$s[2],$palette)}finally{$src.Dispose()}
 try{
  # Editable native pixel matrix, with explicit palette rather than a lossy image editor source.
  $rows=for($y=0;$y -lt $dst.Height;$y++){$line='';for($x=0;$x -lt $dst.Width;$x++){$c=$dst.GetPixel($x,$y);if(!$c.A){$line+='.'}else{$idx=[Array]::FindIndex($palette,[Predicate[Drawing.Color]]{param($p) $p.ToArgb() -eq $c.ToArgb()});$line+='0123456789ABCD'[$idx]}};$line}
  [pscustomobject]@{Width=$dst.Width;Height=$dst.Height;Palette=@($palette | ForEach-Object {'{0:X2}{1:X2}{2:X2}' -f $_.R,$_.G,$_.B});Rows=@($rows)} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $out ($s[0]+'.pixels.json')) -Encoding utf8
  $dst.Save((Join-Path $out ($s[0]+'.png')))
 }finally{$dst.Dispose()}
}
$sheet=[Drawing.Bitmap]::new(672,300);$g=[Drawing.Graphics]::FromImage($sheet)
try{
 $g.Clear([Drawing.Color]::FromArgb(255,45,42,48));$g.InterpolationMode='NearestNeighbor';$g.PixelOffsetMode='Half'
 for($i=0;$i -lt $specs.Count;$i++){$s=$specs[$i];$im=[Drawing.Bitmap]::FromFile((Join-Path $out ($s[0]+'.png')));try{$x=$i*110;$g.DrawImage($im,[Drawing.Rectangle]::new($x,0,$im.Width,$im.Height),0,0,$im.Width,$im.Height,[Drawing.GraphicsUnit]::Pixel);$g.DrawImage($im,[Drawing.Rectangle]::new($x,76,$im.Width*2,$im.Height*2),0,0,$im.Width,$im.Height,[Drawing.GraphicsUnit]::Pixel)}finally{$im.Dispose()}}
 $sheet.Save((Join-Path $out 'inspection.png'))
}finally{$g.Dispose();$sheet.Dispose()}
