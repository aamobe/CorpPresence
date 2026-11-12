namespace CorpPresence;
internal class PixelElement { }
internal class PixelWindow { public PixelElement childElement; public void Detach(){childElement=null;} public void Attach(PixelElement e){childElement=e;} }
internal class GameInventory:PixelElement {public Func<GameItem,GameInventory,bool> mayInventoryAddItemFunc,mayInventoryRemoveItemFunc;}
internal class GameGridInventory:GameInventory {public int Width=8,Height=9;}
internal class GameSlotInventory:GameInventory {
 public string identifier;public GameItem childItem;public static bool FailAccept,ThrowAfterAccept;
 public GameSlotInventory(int w,int h){} public void SetBackgroundFadeSprite(string a,string b){}
 public bool UncheckedAccept(GameItem item){if(FailAccept)return false;childItem=item;item.parentInventory=this;if(ThrowAfterAccept)throw new Exception("UI refresh failed");return true;}
}
internal class MachineryPanel {public PixelElement Content;public MachineryPanel(PixelElement e){Content=e;}}
internal class MachineryPanelRow:PixelElement {public MachineryPanel[] Panels;public MachineryPanelRow(params MachineryPanel[] panels){Panels=panels;}}
internal static class MachineHelper {public static MachineryPanel Panel(string label,PixelElement e,string role)=>new(e);}
internal class GameItem {
 public string identifier;public PixelWindow contentWindow;public GameInventory parentInventory;public bool Owned,Destroyed,NativeEquipment,NativeAudio;
 public void Destroy(){Destroyed=true;}
}
internal static class DirectoryMaster {
 public static List<GameItem> Created=new();public static GameGridInventory Main;
 public static GameItem Item(string id,bool isOwned=true){var item=new GameItem{identifier=id,Owned=isOwned};if(id=="backpack_large"){Main=new();Main.mayInventoryAddItemFunc=(i,v)=>i.Owned&&i.identifier!="forbidden";item.NativeEquipment=item.NativeAudio=true;item.contentWindow=new(){childElement=Main};}Created.Add(item);return item;}
}
internal static class GraphUtils {
 public static List<T> FindAllChildrenType<T>(GameItem bag) where T:class => ((MachineryPanelRow)bag.contentWindow.childElement).Panels.Select(p=>p.Content).OfType<T>().ToList();
}
internal static class GeneralHelper {public static bool IsItemOwned(params GameItem[] items)=>items.All(i=>i!=null&&i.Owned);}
internal static class Settings {public static bool CleanupForUninstall;}
internal static class L {public static string K(string k,string z,string e)=>z;}
internal static class Goods {internal class Def {public string Name,NameEn,Flavor,FlavorEn,Desc,DescEn,Company,Art,Fallback,Shape;public int Value,W,H;public string[] Types;public Func<GameItem> Shell;}}
