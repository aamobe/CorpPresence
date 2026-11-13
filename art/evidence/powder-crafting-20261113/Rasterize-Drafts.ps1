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
$paletteHex=@('292234','303A41','6B5269','777081','80929B','A8AEA5','C4C4B0','D5D2B8','275654','2F756F','8ABFBB','CCB987','79746E','45918B','3D823F','9C3738')
$palette=@($paletteHex | ForEach-Object {[Drawing.ColorTranslator]::FromHtml('#'+$_)})
$specs=@(@('silicon-plate',32,32),@('electronic-substrate',32,16),@('electronic-blueprint',16,16),@('crafting-station',48,48))
$out=Join-Path $PSScriptRoot 'native';New-Item -ItemType Directory -Force $out | Out-Null
foreach($s in $specs){
 if(!(Test-Path (Join-Path $PSScriptRoot ($s[0]+'-generated.png')))){continue}
 $src=[Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot ($s[0]+'-generated.png')))
 try{$dst=[NativeRaster]::Convert($src,$s[1],$s[2],$palette)}finally{$src.Dispose()}
 try{
  $rows=for($y=0;$y -lt $dst.Height;$y++){$line='';for($x=0;$x -lt $dst.Width;$x++){$c=$dst.GetPixel($x,$y);if(!$c.A){$line+='.'}else{$idx=[Array]::FindIndex($palette,[Predicate[Drawing.Color]]{param($p) $p.ToArgb() -eq $c.ToArgb()});$line+='0123456789ABCDEF'[$idx]}};$line}
  [pscustomobject]@{Width=$dst.Width;Height=$dst.Height;Palette=$paletteHex;Rows=@($rows)} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $out ($s[0]+'.pixels.json')) -Encoding utf8
  $dst.Save((Join-Path $out ($s[0]+'.png')))
 }finally{$dst.Dispose()}
}
