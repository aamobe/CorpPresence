using AssetStudio;
using SixLabors.ImageSharp;
using System.Runtime.Loader;
using System.Text.Json;
using System.Security.Cryptography;
var lib=Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving+=(c,n)=>{var p=Path.Combine(lib,n.Name+".dll");return File.Exists(p)?c.LoadFromAssemblyPath(p):null;};
var output=Path.GetFullPath(args[2]); Directory.CreateDirectory(output);
var selected=new HashSet<string>(new[]{"maleLower_0","femaleLower_0","maleUpper_0","femaleUpper_1","maleSecOff_0","femaleSci_0","femaleMed_0","femaleEng_0","Mentor","jackson","odin"},StringComparer.OrdinalIgnoreCase);
var am=new AssetsManager{Game=new Game(GameType.Normal),Silent=true,ResolveDependencies=false};
am.LoadFiles(args[1]);
var records=new List<object>();
foreach(var file in am.assetsFileList) foreach(var obj in file.Objects) {
    if(obj is not Sprite sp || !selected.Contains(sp.m_Name))continue;
    var filename=sp.m_Name+"__"+sp.m_PathID+".png";
    using var image=sp.GetImage();
    if(image==null)throw new Exception("No image for "+filename);
    var target=Path.Combine(output,filename); image.SaveAsPng(target);
    sp.m_RD.texture.TryGet(out Texture2D texture);
    records.Add(new{Name=sp.m_Name,PathId=sp.m_PathID,File=filename,Width=image.Width,Height=image.Height,PPU=sp.m_PixelsToUnits,Pivot=new[]{sp.m_Pivot.X,sp.m_Pivot.Y},Rect=new[]{sp.m_Rect.x,sp.m_Rect.y,sp.m_Rect.width,sp.m_Rect.height},Texture=texture==null?null:new{Name=texture.m_Name,Width=texture.m_Width,Height=texture.m_Height,MipCount=texture.m_MipCount,FilterMode=texture.m_TextureSettings.m_FilterMode},SHA256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(target)))});
}
File.WriteAllText(Path.Combine(output,"manifest.json"),JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("Exported "+records.Count+" uniquely named sprite objects.");
am.Clear();
