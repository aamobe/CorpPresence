using System;
using System.Collections.Generic;
namespace CorpPresence {
internal class GameItem { public object parentInventory; public bool destroyed; public bool Backpack; public string Key; public string identifier; public GameItem SetContentWindow(object window)=>this; public List<GameItem> Items=new(); public void Destroy(){destroyed=true;parentInventory=null;foreach(var i in Items)i.Destroy();} }
internal static class Goods {
 internal class Def { public string Name,NameEn,Flavor,FlavorEn,Desc,DescEn,Company,Art,Shape,Fallback;public int Value,W,H;public bool Crate; public string[] Types;public Func<GameItem> Shell; }
 public static string FailKey;public static List<GameItem> Made=new();
 public static GameItem Make(string key){if(key==FailKey)return null;var i=new GameItem{Key=key};Made.Add(i);return i;}
}
internal static class Crates {public static bool Fail;public static GameItem CreateShell()=>new();public static bool Put(GameItem b,GameItem i){if(Fail)return false;b.Items.Add(i);i.parentInventory=b;return true;}}
internal static class Settings{public static bool CleanupForUninstall;}
internal class StorePhoneClient {public enum PhoneClientType{None,Supplier}public enum PhoneState{Regular,NotResponding}public PhoneClientType phoneClientType;public PhoneState phoneState;public string displayName,locID,dialogFuncId;public int cooldownDuration,currentCooldown;public bool dialedBefore;}
internal class Dialogue {public string Name,Text;public Action Start,End;public Dialogue SetText(string n,string t){Name=n;Text=t;return this;}public Dialogue SetStartAction(Action a){Start=a;return this;}public Dialogue SetEndAction(Action a){End=a;return this;}}
internal class PhoneUIManager {public static PhoneUIManager Instance=new();public StorePhoneClient currentPhoneClient;public int Ended;public void OnPhoneDialogEnd(){Ended++;currentPhoneClient=null;}}
internal class CorpState {public static CorpState Current=new();public int MangoGiftStatus;public static CorpState Get()=>Current;}
internal static class SaveSlot {public static PlayerStore CurrentStore=new();public static bool Writable=true;public static bool CanWrite(string k)=>Writable;}
internal readonly struct StoreSession {private readonly PlayerStore owner;private StoreSession(PlayerStore s){owner=s;}public static StoreSession Capture()=>new(SaveSlot.CurrentStore);public bool IsCurrent=>owner!=null&&ReferenceEquals(owner,SaveSlot.CurrentStore);}
internal class PlayerStore {public StoreClientManager storeClientManager=new();public Client currentClientInstance;public bool Full,ThrowAfterPlacement;public Action OnPlace;public GameItem Placed;public bool TryAddDirectSellingItemToTable(GameItem b,bool isOwend){if(Full)return false;Placed=b;b.parentInventory=this;OnPlace?.Invoke();if(ThrowAfterPlacement)throw new Exception("UI failure");return true;}}
internal class Client{public StoreClient Blueprint;public StoreClient GetClientBlueprint()=>Blueprint;}
internal class StoreClientManager {public List<StoreClient> clientStack=new();}
internal class StoreClient {public enum ClientIntent{DIALOGUE}public string identifier,displayName,spriteName,clientFaction;public ClientIntent clientIntent;public bool dismissable;public Dialogue mainDialogue;public void AddBasicDialog(){}public void CompleteClientCreation(){}}
internal static class GameResources{public static bool Ready=true;}
internal static class Portraits{public static string Ensure(string a,string b)=>a;}
internal static class ModHelper{public static void SetClientSource(StoreClient c,string s){}}
internal static class Main{public static string DisplayName=>"CorpPresence";}
internal static class L{public static string K(string key,string zh,string en)=>zh;}
internal class StoreUIManager{public static StoreUIManager Instance=new();public void Notify(string t){}}
internal class DialogUIManager{public void CurrentClientLeave(){}}
internal static class GeneralHelper{public static void SetItemOwned(GameItem i){} public static bool IsItemOwned(params GameItem[] items)=>true;}
internal static class StorePlacement{public static bool Destroyed(GameItem i)=>i==null||i.destroyed;}
internal static class ModLog{public static void Warn(string s){}}
internal static class CargoWarehouse{public const string MangoId="corp_presence_mango_warehouse"; public const string MangoKey="mango_warehouse",ModuleKey="cargo_warehouse_module",ControllerKey="cargo_sort_controller";}
}
namespace UnityEngine{public static class Object{public static T FindObjectOfType<T>() where T:new()=>new();}}

namespace CorpPresence { internal class GameInventory { public Func<GameItem,GameInventory,bool> mayInventoryAddItemFunc; } internal static class DirectoryUtils {public static (object,GameInventory) CreateInventoryWindow(int w,int h)=>(new object(),new GameInventory());} internal static class ItemDirectory {public static GameItem CreateEmptyItem()=>new();} internal static class ContainerHelper {public static void InitBackpackItem(GameInventory inv,GameItem item){item.Backpack=true;inv.mayInventoryAddItemFunc=(i,v)=>i.identifier!=CargoWarehouse.MangoId;}} }
