namespace CorpPresence {
static class GeneralHelper {public static void SetItemOwned(GameItem item){}}
class Visitor {public int Day;public string Kind;}
static class Companies {public static List<Company> All=new();public static CompanyState StateOf(Company company)=>CorpState.Current.Companies[company.Id];}
static class NightSettlement {public static bool TryBegin(CompanyState s,int day)=>true;}
class Company {public string Id="pastel",CharacterId="character",Name="Company";public List<CatalogEntry> Catalog;}
class CompanyState {public bool TrafficOn,VipOn,DealerJoined;public int DealerSinceDay;public double WeekSales;public List<Visitor> Visitors=new();public Dictionary<string,int> LimitedBought=new();public List<Order> Orders=new();}
class Order {public int Day,Cash;public string Kind;public List<string> Items=new();}
class GameInventory {public List<GameItem> childItems=new();public Func<GameItem,GameInventory,SlotMarker,int> maxSlotAmountItemFunc;public Func<GameItem,GameInventory,bool> mayInventoryAddItemFunc;public Action<GameItem,GameInventory,SlotMarker> onSlotRemoveItemEndFunc;}
class GameGridInventory:GameInventory {public bool HasCrateSlot=true;public bool CanTakeAnything()=>true;public SlotMarker TryFindOneValidInventorySlot(GameItem item)=>HasCrateSlot?new SlotMarker():null;}
class GameItemElement:GameItem {public bool IsDestroyed()=>DestroyCount>0;}
class GraphNodeStorage {}
class SlotMarker {}
class PixelWindow {public GameInventory Inventory;}
static class DirectoryUtils {public static (PixelWindow,GameInventory) CreateInventoryWindow(int w,int h){var inv=new GameInventory();return (new PixelWindow{Inventory=inv},inv);}}
static class ItemDirectory {public static GameItem CreateEmptyItem()=>new GameItem();}
static class Compat {public static T Cast<T>(object value)=>(T)value;}
static class GraphUtils {public static void EmporiumTryAdd(GameInventory inventory,GameItem item){}public static bool CanAccept(GameInventory inventory,GameItem item)=>inventory.childItems.Count<Capacity;public static int Capacity=2;public static bool Stacking,IgnoreDeclaredLimit;public static List<T> FindChildrenType<T>(GraphNodeStorage node)=>new(){(T)(object)((GameItem)node).Inner};public static List<T> FindAllChildrenType<T>(GraphNodeStorage node)=>((GameItem)node).Inner.childItems.Cast<T>().ToList();public static void TryAcceptAll(GameInventory inv,GameItem item){if(Stacking){var cap=IgnoreDeclaredLimit?5:(inv.maxSlotAmountItemFunc?.Invoke(item,inv,null)??5);var target=inv.childItems.FirstOrDefault(x=>x.identifier==item.identifier&&x.unitCount<cap);if(target!=null){target.unitCount+=item.unitCount;item.unitCount=0;return;}}if(inv.childItems.Count>=Capacity)return; inv.childItems.Add(item);item.parentInventory=inv;}}
class ItemMouseDragHandler {public static ItemMouseDragHandler current;public bool IsDraggingItem;}
class EmporiumEntry {public static EmporiumEntry Instance;public GameGridInventory frontInvinvElement;public GameInventory invElement,backInvinvElement;public List<GameItem> OwnedItems=new();public List<GameItem> GetAllOwnedItems(bool isBurglary=false,bool includeAfterhour=true)=>OwnedItems;}
class StoreClient {}
class Client {public StoreClient GetClientBlueprint()=>null;}
class DialogUIManager {public void CurrentClientLeave(){}}
class StoreUIManager {public static StoreUIManager Instance;public void Notify(string message){}}
static partial class CompanyVisitors {private static int Day=>StoreStation.Day;public static Action Receipt(Company c,CompanyState s,List<Order> orders)=>CreateDeliveryReceipt(c,s,orders,false);}
static partial class Services {private static int _closedDay=-1;private static bool _nightlyRunning;private static int Day=>StoreStation.Day;public static Func<string> PayHook;public static string Pay(Company c,int fee,string kind)=>PayHook?.Invoke();public static void MaybeGiveCard(string id){}}
static class CompanyMoney {public static void DeliverCash(PlayerStore store,Order o){o.Cash=0;}}
static class OrderContents {public static void ExpandModBoxes(Order o,Func<string> roll){}}
class Character {public GameItem MakeItem(string id)=>Goods.Factory();}
static class Engine {public static Character Find(string id)=>null;}
}
namespace CorpPresence.Malice {class RebelCo:CorpPresence.Company {}static class CrateFillGate {public static bool Allows(CorpPresence.GameInventory inv)=>true;public static object Enter(CorpPresence.GameInventory inv)=>null;public static void Restore(object value){}}}
namespace UnityEngine {class Object {public static T FindObjectOfType<T>() where T:class=>null;}}
namespace CorpPresence.Malice {static class MaliceState {public static void Get(){}}}
