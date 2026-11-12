namespace CorpPresence;
internal class TagState { public int Value; public int GetInt()=>Value; public TagState SetInt(int n){Value=n;return this;} public TagState Enable()=>this; }
internal class GameItem {
 public string identifier; public int unitCount=1; public bool Owned=true,destroyed,FailConsume; public object parentInventory=new();
 public Func<GameItem,GameItem,bool> mayThisTargetItemFunc,canThisTargetItemFunc; public Action<GameItem,GameItem> onThisTargetItemFunc; public Func<GameItem,GameItem,bool> mayItemTargetThisFunc,canItemTargetThisFunc; public Action<GameItem,GameItem> onItemTargetThisFunc;
 readonly Dictionary<string,TagState> tags=new();
 public TagState GetTagReadonly(string key)=>tags.TryGetValue(key,out var v)?new TagState{Value=v.Value}:null;
 public void ModifyTag(string key,Action<TagState> f){if(!tags.ContainsKey(key))tags[key]=new();f(tags[key]);}
 public void DisableTag(string key){} public void EnableTag(string key){}
 public bool IsTag(string key)=>tags.ContainsKey(key);
 public void SetUnitCount(int n){if(FailConsume)throw new InvalidOperationException();unitCount=n;}
 public void Destroy(){if(FailConsume)throw new InvalidOperationException();destroyed=true;parentInventory=null;}
}
internal static class Goods {
 internal class Def { public string Name,NameEn,Flavor,FlavorEn,Desc,DescEn,Company,Art,Fallback;public int W,H;public long Value;public int[] Power;public string[] Types;public Action<GameItem> Setup;public Func<GameItem> Shell; }
 public static void RefreshBattery(GameItem i){} public static void Redraw(GameItem i){}
}
internal static class StorePlacement { public static bool Destroyed(GameItem i)=>i==null||i.destroyed; }
internal static class SaveSlot {public static bool Writable=true;public static bool CanWrite(string id)=>Writable;}
internal static class Settings {public static bool CleanupForUninstall=false;}
internal static class ItemDirectory { public static GameItem CreateEmptyItem()=>new(); }
internal static class GeneralHelper {public static bool IsItemOwned(params GameItem[] items)=>items.All(i=>i.Owned);}
