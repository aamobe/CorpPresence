using System;
using System.Collections.Generic;
using System.Drawing;

public sealed class SpriteProbeResult {
 public string Source;
 public int Width, Height, MainPixels, Transparent, Opaque, PartialAlpha, MainRgb;
 public double ExactPaletteFraction, MeanPaletteDistance;
 public int LogicalWidth, LogicalHeight;
 public int BodyMinX,BodyMinY,BodyMaxX,BodyMaxY,OpaqueComponents4,SmallComponents4;
 public double MeanCellDominance, MixedCellFraction;
 public double XBoundaryScore, YBoundaryScore, XPhaseFraction, YPhaseFraction;
}

public static class SpriteProbe {
 // Read-only diagnostics. Quantized labels exist only in memory, never replace source pixels.
 public static SpriteProbeResult Analyze(string path, int lw, int lh, string[] hex) {
  if(lw<1 || lh<1) throw new ArgumentException("Logical dimensions must be positive.");
  if(hex==null || hex.Length==0) throw new ArgumentException("Supply a measured reference palette.");
  int[] palette=new int[hex.Length];
  for(int k=0;k<hex.Length;k++)palette[k]=Convert.ToInt32(hex[k].TrimStart('#'),16);
  using(Bitmap b=new Bitmap(path)) {
   if(lw>b.Width || lh>b.Height)throw new ArgumentException("Logical canvas exceeds source; this probe does not upscale.");
   var r=new SpriteProbeResult { Source=path,Width=b.Width,Height=b.Height,LogicalWidth=lw,LogicalHeight=lh };
   int w=b.Width,h=b.Height; int[] rgba=new int[w*h],labels=new int[w*h];
   var colors=new HashSet<int>();var cache=new Dictionary<int,int>();var distances=new Dictionary<int,double>();
   double totalDistance=0;int exact=0;
   for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
    Color c=b.GetPixel(x,y);int pos=y*w+x, rgb=c.ToArgb()&0xffffff;rgba[pos]=c.ToArgb();
    if(c.A==0)r.Transparent++;else if(c.A==255)r.Opaque++;else r.PartialAlpha++;
    if(c.A<128){labels[pos]=-1;continue;}
    r.MainPixels++;colors.Add(rgb);
    if(!cache.TryGetValue(rgb,out int label)) {
     double best=double.MaxValue;label=0;
     for(int k=0;k<palette.Length;k++){double d=Distance(rgb,palette[k]);if(d<best){best=d;label=k;}}
     cache[rgb]=label;distances[rgb]=Math.Sqrt(best);
    }
    labels[pos]=label;totalDistance+=distances[rgb];if(rgb==palette[label])exact++;
   }
   r.BodyMinX=w;r.BodyMinY=h;r.BodyMaxX=-1;r.BodyMaxY=-1;
   bool[] visited=new bool[w*h];var pending=new Queue<int>();
   for(int p=0;p<labels.Length;p++)if(labels[p]>=0){
    int px=p%w,py=p/w;r.BodyMinX=Math.Min(r.BodyMinX,px);r.BodyMinY=Math.Min(r.BodyMinY,py);r.BodyMaxX=Math.Max(r.BodyMaxX,px);r.BodyMaxY=Math.Max(r.BodyMaxY,py);
    if(visited[p])continue;int count=0;r.OpaqueComponents4++;pending.Enqueue(p);visited[p]=true;
    while(pending.Count>0){int q=pending.Dequeue();count++;int x=q%w,y=q/w;
     int[] neighbors=new[]{x>0?q-1:-1,x<w-1?q+1:-1,y>0?q-w:-1,y<h-1?q+w:-1};
     foreach(int n in neighbors)if(n>=0&&!visited[n]&&labels[n]>=0){visited[n]=true;pending.Enqueue(n);}
    }
    if(count<=2)r.SmallComponents4++;
   }
   if(r.MainPixels==0){r.BodyMinX=-1;r.BodyMinY=-1;}
   r.MainRgb=colors.Count;r.ExactPaletteFraction=r.MainPixels==0?0:(double)exact/r.MainPixels;
   r.MeanPaletteDistance=r.MainPixels==0?0:totalDistance/r.MainPixels;
   double dominance=0;int cells=0,mixed=0;
   for(int cy=0;cy<lh;cy++)for(int cx=0;cx<lw;cx++) {
    int x0=cx*w/lw,x1=(cx+1)*w/lw,y0=cy*h/lh,y1=(cy+1)*h/lh;
    int[] votes=new int[palette.Length+1];int count=0,max=0;
    for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++){int k=labels[y*w+x]+1;votes[k]++;count++;}
    foreach(int n in votes)if(n>max)max=n;
    double d=(double)max/count;dominance+=d;cells++;if(d<0.9)mixed++;
   }
   r.MeanCellDominance=dominance/cells;r.MixedCellFraction=(double)mixed/cells;
   // Strong visible edges only; omit transparent RGB and ignore tiny RGB noise.
   double[] ex=new double[w],ey=new double[h];
   for(int y=0;y<h;y++)for(int x=0;x<w;x++){
    int p=y*w+x;
    if(x>0)ex[x]+=Edge(rgba[p-1],rgba[p]);
    if(y>0)ey[y]+=Edge(rgba[p-w],rgba[p]);
   }
   double[] sx=Fit(ex,(double)w/lw),sy=Fit(ey,(double)h/lh);
   r.XBoundaryScore=sx[0];r.XPhaseFraction=sx[1];r.YBoundaryScore=sy[0];r.YPhaseFraction=sy[1];return r;
  }
 }
 static double Distance(int a,int b){int dr=((a>>16)&255)-((b>>16)&255),dg=((a>>8)&255)-((b>>8)&255),db=(a&255)-(b&255);return dr*dr+dg*dg+db*db;}
 static double Edge(int a,int b){int aa=(int)((uint)a>>24),ab=(int)((uint)b>>24);if(aa<128 && ab<128)return 0;if((aa<128)!=(ab<128))return 1;return Distance(a,b)>=900?1:0;}
 static double[] Fit(double[] edges,double period){
  double total=0;foreach(double e in edges)total+=e;if(total==0)return new double[]{0,0};
  double best=-1,phase=0;
  for(int f=0;f<64;f++){double shift=period*f/64,aligned=0;for(int x=1;x<edges.Length;x++){double q=(x-shift)/period;if(Math.Abs(q-Math.Round(q))<=0.1)aligned+=edges[x];}double score=aligned/total;if(score>best){best=score;phase=(double)f/64;}}
  return new double[]{best,phase};
 }
 public static void SelfTest(string directory){
  System.IO.Directory.CreateDirectory(directory);string p=System.IO.Path.Combine(directory,"probe-fixture.png");
  using(var b=new Bitmap(24,16)){
   for(int y=0;y<16;y++)for(int x=0;x<24;x++)b.SetPixel(x,y,((x/4+y/4)%2==0)?Color.Red:Color.Blue);
   b.Save(p,System.Drawing.Imaging.ImageFormat.Png);
  }
  var r=Analyze(p,6,4,new[]{"FF0000","0000FF"});
  if(r.MainRgb!=2 || r.Opaque!=384 || r.ExactPaletteFraction!=1 || r.MeanCellDominance!=1 || r.MixedCellFraction!=0 || r.XBoundaryScore!=1 || r.YBoundaryScore!=1)throw new Exception("Exact 4x pixel-block fixture failed.");
  var wrong=Analyze(p,5,3,new[]{"FF0000","0000FF"});
  if(wrong.MeanCellDominance>=0.9 || wrong.MixedCellFraction<=0)throw new Exception("Incorrect-grid fixture failed to detect mixed cells.");
  string disconnected=System.IO.Path.Combine(directory,"disconnected-fixture.png");
  using(var b=new Bitmap(3,3)){b.SetPixel(0,0,Color.Red);b.SetPixel(2,2,Color.Blue);b.Save(disconnected,System.Drawing.Imaging.ImageFormat.Png);}
  var parts=Analyze(disconnected,3,3,new[]{"FF0000","0000FF"});
  if(parts.OpaqueComponents4!=2||parts.SmallComponents4!=2||parts.MainPixels!=2||parts.BodyMinX!=0||parts.BodyMinY!=0||parts.BodyMaxX!=2||parts.BodyMaxY!=2)throw new Exception("Disconnected component fixture failed.");
 }
}
