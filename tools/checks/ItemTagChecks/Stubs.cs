namespace CorpPresence;
// Only ItemTags is production code. The native container classifier is a boundary probe.
class GraphNodeStorage { public List<GameItem> Children = new(); }
class TagSystem { public Dictionary<string,int> dict = new(); }
class Feature { public string category; }
class GameItem : GraphNodeStorage {
 public string identifier, spritePath; public long unitValue=73; public object parentInventory;
 public TagSystem state=new(), modifiedState=new(); public List<string> itemTypes=new(); public List<Feature> itemFeatures=new();
 public bool IsTag(string key)=>modifiedState.dict.ContainsKey(key)||itemTypes.Contains(key);
 public void EnableTag(string key)=>modifiedState.dict[key]=1;
 public void RemoveGameItemType(string key)=>itemTypes.Remove(key);
 public void RemoveAllGameItemType()=>itemTypes.Clear();
 public void SetGameItemType(string key){if(!itemTypes.Contains(key))itemTypes.Add(key);}
 public void SetValue(long value)=>unitValue=value;
 public void Destroy() {} public void RemoveItemFeature(Feature f)=>itemFeatures.Remove(f);
}
static class WaterHelper {
 public static Action<GameItem> OnClassify;
 public static void UpdateContainerGoodType(GameItem item)=>OnClassify(item);
}
static class ItemIdentity {
 public record Entry(string Goods,string LegacyId);
 public static Entry Find(string id,string sprite)=>id is "corp_presence_silver_blood" or "corp_presence_isomer_vial" or "corp_presence_tool" ? new(id[14..],"base") : null;
 public static string GoodsKey(string id,string sprite)=>Find(id,sprite)?.Goods;
}
static class Goods {
 public static string[] TypesOf(string id)=>id==null?null:new[]{"MEDICAL"};
 public static bool HasIndependentShell(string id)=>true;
 public static void Relabel(GameItem item,string id){}
 public static long ValueOf(string id)=>1;
}
static class Perfume {public static void RepairFlowerGenetics(GameItem item){}}
static class ProductionCompatibility {public static void RemoveLegacyPause(GameItem item){}}
static class Cards {public static void Relabel(GameItem item,ItemIdentity.Entry e){}}
static class PastelCo {public static void AfterGenuineAction(GameItem item){}}
static class RifleMods {public static void Attach(GameItem item){}}
static class PowerHelper {public static void InitDisposable(GameItem item){}}
static class ContrabandHelper {public static void InitContrabandItem(GameItem item){}}
static class Forge {public static void ApplyLuxury(GameItem item){}}
static class ItemRegistry {public static GameItem Create(string id)=>new();}
static class StorePlacement {public static bool Destroyed(GameItem item)=>false;}
static class Compat {public static T Cast<T>(object item)=>(T)item;}
static class GraphUtils {public static List<T> FindAllChildrenType<T>(GraphNodeStorage root)=>root.Children.Cast<T>().ToList();}
class EmporiumEntry {public static EmporiumEntry Instance;public List<GameItem> GetAllItems()=>new();}
static class ModLog {public static void Warn(string text)=>throw new Exception(text);}

static class UfoBackpack { public const string ItemId="corp_presence_ufo_backpack"; }
