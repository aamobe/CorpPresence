using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// Deterministic experimental conversion; not an image-generation model.
public static class SpriteRecover {
 static int Distance(int a,int b) {int r=((a>>16)&255)-((b>>16)&255),g=((a>>8)&255)-((b>>8)&255),v=(a&255)-(b&255);return r*r+g*g+v*v;}
 static int Map(int argb,int[] palette) {
  if(((uint)argb>>24)<128)return 0;
  int rgb=argb&0xffffff,best=0,dist=int.MaxValue;
  for(int i=0;i<palette.Length;i++){int d=Distance(rgb,palette[i]);if(d<dist){dist=d;best=i;}}
  return unchecked((int)0xff000000)|palette[best];
 }
 public static void Convert(string input,string output,int width,int height,string[] hex,string method,bool raw) {
  if(width<1||height<1)throw new ArgumentException("Positive dimensions required.");
  if(method!="center"&&method!="majority")throw new ArgumentException("Method must be center or majority.");
  int[] palette=Array.ConvertAll(hex,h=>System.Convert.ToInt32(h.TrimStart('#'),16));
  if(!raw&&palette.Length==0)throw new ArgumentException("Palette required.");
  using(var src=new Bitmap(input))using(var dst=new Bitmap(width,height,PixelFormat.Format32bppArgb)) {
   for(int y=0;y<height;y++)for(int x=0;x<width;x++) {
    int center=src.GetPixel(Math.Min(src.Width-1,(int)((x+0.5)*src.Width/width)),Math.Min(src.Height-1,(int)((y+0.5)*src.Height/height))).ToArgb();
    int result=raw?center:Map(center,palette);
    if(method=="majority") {
     // Central 60% of each cell excludes adjacent boundaries; exact integer bounds logged by formula.
     int x0=(int)Math.Ceiling((x+0.2)*src.Width/width),x1=(int)Math.Ceiling((x+0.8)*src.Width/width);
     int y0=(int)Math.Ceiling((y+0.2)*src.Height/height),y1=(int)Math.Ceiling((y+0.8)*src.Height/height);
     var votes=new Dictionary<int,int>();int max=0;
     for(int sy=y0;sy<y1;sy++)for(int sx=x0;sx<x1;sx++) {
      int value=src.GetPixel(sx,sy).ToArgb();if(!raw)value=Map(value,palette);
      if(!votes.ContainsKey(value))votes[value]=0;votes[value]++;
     }
     // Center wins equal votes when possible; otherwise deterministic first encountered value.
     if(votes.ContainsKey(result))max=votes[result];
     foreach(var pair in votes)if(pair.Value>max){result=pair.Key;max=pair.Value;}
    }
    dst.SetPixel(x,y,Color.FromArgb(result));
   }
   dst.Save(output,ImageFormat.Png);
  }
 }
 public static void Enlarge(string input,string output,int scale) {
  using(var src=new Bitmap(input))using(var dst=new Bitmap(src.Width*scale,src.Height*scale,PixelFormat.Format32bppArgb)){
   for(int y=0;y<dst.Height;y++)for(int x=0;x<dst.Width;x++)dst.SetPixel(x,y,src.GetPixel(x/scale,y/scale));
   dst.Save(output,ImageFormat.Png);
  }
 }
 // Global axis cuts: bounded displacement, monotone dynamic programming.
 // This is our baseline, not a reproduction of Pixel Snapper's local walk.
 static int[] ElasticCuts(double[] edge,int count,double radius) {
  int length=edge.Length-1;double step=(double)length/count,max=0;
  foreach(double e in edge)max=Math.Max(max,e);
  var positions=new int[count+1][];var score=new double[count+1][];var previous=new int[count+1][];
  positions[0]=new[]{0};score[0]=new[]{0.0};previous[0]=new[]{-1};
  for(int i=1;i<=count;i++) {
   int lo=i==count?length:Math.Max(1,(int)Math.Ceiling((i-radius)*step));
   int hi=i==count?length:Math.Min(length-1,(int)Math.Floor((i+radius)*step));
   // A zero/narrow radius may contain no integer coordinate at fractional scale.
   if(lo>hi)lo=hi=Math.Max(1,Math.Min(length-1,(int)Math.Round(i*step)));
   positions[i]=new int[hi-lo+1];score[i]=new double[hi-lo+1];previous[i]=new int[hi-lo+1];
   for(int j=0;j<positions[i].Length;j++) {
    int p=lo+j;positions[i][j]=p;score[i][j]=double.NegativeInfinity;previous[i][j]=-1;
    double displacement=(p-i*step)/step;
    double reward=(max>0?edge[p]/max:0)-0.25*displacement*displacement;
    for(int k=0;k<positions[i-1].Length;k++) {
     double width=(p-positions[i-1][k])/step;
     if(width<0.3||width>1.7)continue;
     double candidate=score[i-1][k]+reward-0.05*(width-1)*(width-1);
     if(candidate>score[i][j]){score[i][j]=candidate;previous[i][j]=k;}
    }
   }
  }
  if(double.IsNegativeInfinity(score[count][0]))throw new Exception("No valid elastic grid.");
  int[] cuts=new int[count+1];int index=0;
  for(int i=count;i>=0;i--){cuts[i]=positions[i][index];index=previous[i][index];}
  return cuts;
 }
 public static int[][] ConvertElastic(string input,string output,int width,int height,string[] hex,double radius,bool raw) {
  if(radius<0||radius>0.45)throw new ArgumentException("Radius must be 0..0.45 logical cells.");
  int[] palette=Array.ConvertAll(hex,h=>System.Convert.ToInt32(h.TrimStart('#'),16));
  using(var src=new Bitmap(input))using(var dst=new Bitmap(width,height,PixelFormat.Format32bppArgb)) {
   if(width<1||height<1||width>src.Width||height>src.Height)throw new ArgumentException("Invalid target size.");
   if(palette.Length==0)throw new ArgumentException("Palette needed for edge analysis.");
   int w=src.Width,h=src.Height;int[] original=new int[w*h],labels=new int[w*h];
   var cache=new Dictionary<int,int>();
   for(int y=0;y<h;y++)for(int x=0;x<w;x++){
    int p=y*w+x,c=src.GetPixel(x,y).ToArgb();original[p]=c;
    if(!cache.ContainsKey(c))cache[c]=Map(c,palette);labels[p]=cache[c];
   }
   double[] ex=new double[w+1],ey=new double[h+1];
   for(int y=0;y<h;y++)for(int x=0;x<w;x++){
    int p=y*w+x;if(x>0&&labels[p]!=labels[p-1])ex[x]++;if(y>0&&labels[p]!=labels[p-w])ey[y]++;
   }
   int[] xs=ElasticCuts(ex,width,radius),ys=ElasticCuts(ey,height,radius);
   for(int y=0;y<height;y++)for(int x=0;x<width;x++){
    int cw=xs[x+1]-xs[x],ch=ys[y+1]-ys[y],cx=(xs[x]+xs[x+1])/2,cy=(ys[y]+ys[y+1])/2;
    int result=raw?original[cy*w+cx]:labels[cy*w+cx];
    var votes=new Dictionary<int,int>();int max=0;
    for(int sy=(int)Math.Ceiling(ys[y]+ch*0.2);sy<(int)Math.Ceiling(ys[y]+ch*0.8);sy++)
     for(int sx=(int)Math.Ceiling(xs[x]+cw*0.2);sx<(int)Math.Ceiling(xs[x]+cw*0.8);sx++){
      int v=raw?original[sy*w+sx]:labels[sy*w+sx];if(!votes.ContainsKey(v))votes[v]=0;votes[v]++;
     }
    if(votes.ContainsKey(result))max=votes[result];foreach(var pair in votes)if(pair.Value>max){max=pair.Value;result=pair.Key;}
    dst.SetPixel(x,y,Color.FromArgb(result));
   }
   dst.Save(output,ImageFormat.Png);return new[]{xs,ys};
  }
 }
 public static int Difference(string first,string second) {
  using(var a=new Bitmap(first))using(var b=new Bitmap(second)){
   if(a.Size!=b.Size)throw new ArgumentException("Dimensions differ.");int different=0;
   for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++)if(a.GetPixel(x,y).ToArgb()!=b.GetPixel(x,y).ToArgb())different++;
   return different;
  }
 }
 public static int[][] ScanRuns(string input,string[] hex,string targetHex,int minimumLength) {
  int[] palette=Array.ConvertAll(hex,h=>System.Convert.ToInt32(h.TrimStart('#'),16));
  int target=unchecked((int)0xff000000)|System.Convert.ToInt32(targetHex.TrimStart('#'),16);
  var rows=new List<int[]>();
  using(var b=new Bitmap(input))for(int y=0;y<b.Height;y++){
   var row=new List<int>();row.Add(y);int start=-1;
   for(int x=0;x<=b.Width;x++){
    bool hit=x<b.Width && Map(b.GetPixel(x,y).ToArgb(),palette)==target;
    if(hit && start<0)start=x;
    if(!hit && start>=0){if(x-start>=minimumLength){row.Add(start);row.Add(x-1);}start=-1;}
   }
   if(row.Count>1)rows.Add(row.ToArray());
  }
  return rows.ToArray();
 }
 public static void SelfTest(string original,string dir) {
  Directory.CreateDirectory(dir);string up=Path.Combine(dir,"original-8x.png");Enlarge(original,up,8);
  using(var b=new Bitmap(original))foreach(string method in new[]{"center","majority"}){
   string output=Path.Combine(dir,"roundtrip-"+method+".png");Convert(up,output,b.Width,b.Height,new string[0],method,true);
   int delta=Difference(original,output);if(delta!=0)throw new Exception(method+" RGBA round-trip mismatch: "+delta);
  }
 }
}
