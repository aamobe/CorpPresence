using CorpPresence;
int passed = 0, failed = 0;
void Check(string name, Action test) { try { SaveSlot.CurrentStore = new PlayerStore(); CorpState.Current = new CorpState(SaveSlot.CurrentStore); GameResources.Ready = true; GameResources.Generation++; Goods.Factory = () => new GameItem(); StoreStation.Day = 1; GraphUtils.Stacking=false; GraphUtils.IgnoreDeclaredLimit=false; test(); passed++; Console.WriteLine("PASS " + name); } catch(Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); } }
void Assert(bool value) { if(!value) throw new Exception("assertion"); }
Check("escapee ignores the unsaved front counter", () =>
{
    var store = new EmporiumEntry { invElement = new GameInventory(), backInvinvElement = new GameInventory(), frontInvinvElement = new GameGridInventory() };
    var creature = new GameItemElement();
    Assert(EscapeePlacement.TryPlace(store, creature) && store.invElement.childItems.Contains(creature));
    while (GraphUtils.CanAccept(store.invElement, null)) store.invElement.childItems.Add(new GameItem());
    creature = new GameItemElement();
    Assert(EscapeePlacement.TryPlace(store, creature) && store.backInvinvElement.childItems.Contains(creature));
    while (GraphUtils.CanAccept(store.backInvinvElement, null)) store.backInvinvElement.childItems.Add(new GameItem());
    var pending = new GameItemElement();
    Assert(!EscapeePlacement.TryPlace(store, pending) && pending.parentInventory == null && store.frontInvinvElement.childItems.Count == 0);
});
Check("panel catalog resolves language live and retries a failed name", () =>
{
    var panel = new CorpPresence.UI.PanelCatalogState();
    Goods.Factory = () => null;
    Assert(panel.Name("sample") == "sample");
    var sample = new GameItem(); Goods.Factory = () => sample;
    L.En = false; Assert(panel.Name("sample") == "中文");
    L.En = true; Assert(panel.Name("sample") == "English");
    panel.Reset(); panel.Reset(); Assert(sample.DestroyCount == 1);
    L.En = false;
});
CatalogEntry Entry() => new CatalogEntry { Id="sample", Page="test" };
Check("bundle overflow fails closed", () => { Goods.Factory=()=>new GameItem{Value=int.MaxValue}; Assert(new CatalogEntry{Id="sample",Bundle=2}.BaseValue()==-1); });
Check("nonfinite rate fails closed", () => Assert(Entry().Quote(double.PositiveInfinity)==0));
Check("catalog and order prices include contraband markups and optional events", () =>
{
    var sample = new GameItem { Value = 100, EventPercent = 20, IsContraband = true };
    Goods.Factory = () => sample;
    var entry = Entry();
    int OrderQuote() => entry.Quote(1.2);
    foreach (var (level, list, order) in new[] { ("LOW", 155, 186), ("MID", 190, 228), ("HIGH", 220, 264), ("CRITICAL", 270, 324) })
    {
        sample.Level = "CONTRABAND_LEVEL_" + level;
        Assert(entry.ListValue() == list && OrderQuote() == order);
        Assert(entry.BaseValue() == 100);
    }
    sample.Level = "CONTRABAND_LEVEL_LOW";
    SaveSlot.CurrentStore.contrabandMarkupLow = 50;
    Assert(OrderQuote() == 204);
    Settings.OrderEvents = false;
    try { Assert(OrderQuote() == 180); }
    finally { Settings.OrderEvents = true; }
    sample.IsContraband = false;
    Assert(OrderQuote() == 144);
});

Check("restricted classes add their markups and luxury alone only uses event pricing", () =>
{
    var sample = new GameItem { Value = 100, EventPercent = 20 };
    Goods.Factory = () => sample;
    var entry = Entry();
    foreach (var (type, expected) in new[] { ("RESTRICTED_1", 137), ("RESTRICTED_2", 155), ("RESTRICTED_3", 170), ("LUXURY_ITEM", 120) })
    {
        sample.RestrictedType = type;
        Assert(entry.ListValue() == expected && entry.Quote(1) == expected && entry.BaseValue() == 100);
    }
});

Check("a permit granted the same day refreshes the quoted classification", () =>
{
    var store = SaveSlot.CurrentStore;
    var unlicensed = new GameItem { Value = 100, IsContraband = true, Level = "CONTRABAND_LEVEL_MID" };
    var licensed = new GameItem { Value = 100, RestrictedType = "RESTRICTED_2" };
    Goods.Factory = () => store.ActivePermits.Count == 0 ? unlicensed : licensed;
    var entry = Entry();
    Assert(entry.Quote(1) == 170);
    store.ActivePermits.Add(("FIREARM", 2));
    Assert(entry.Quote(1) == 135 && unlicensed.DestroyCount == 1);
});

Check("day change releases old sample", () => { var sample=new GameItem(); Goods.Factory=()=>sample; var e=Entry(); e.BaseValue(); StoreStation.Day++; Goods.Factory=()=>new GameItem(); e.BaseValue(); Assert(sample.DestroyCount==1); });
Check("inventory sample is never destroyed or quoted", () => { var sample=new GameItem{parentInventory=new GameInventory()}; Goods.Factory=()=>sample; var e=Entry(); Assert(e.BaseValue()==-1); Assert(sample.DestroyCount==0); });
Check("same-store modData replacement invalidates sample", () => {var sample=new GameItem(); Goods.Factory=()=>sample; var e=Entry(); e.BaseValue(); SaveSlot.CurrentStore.modData=new(); Goods.Factory=()=>new GameItem(); e.BaseValue(); Assert(sample.DestroyCount==1);});
Check("value callback changing generation fails closed", () => { Goods.Factory=()=>new GameItem{Read=()=>{GameResources.Generation++;return 10;}}; Assert(Entry().BaseValue()==-1); });
Check("session invalidated by raw binding replacement", () => {var s=StoreSession.Capture(); Assert(s.IsCurrent); SaveSlot.CurrentStore.modData["CorpPresence"]="new"; Assert(!s.IsCurrent);});
Check("reset releases sample once", () => { var sample=new GameItem(); Goods.Factory=()=>sample; Entry().BaseValue(); CatalogPricing.Reset(); CatalogPricing.Reset(); Assert(sample.DestroyCount==1); });
Check("language is resolved from current sample", () => { var e=Entry(); L.En=false; Assert(e.Name()=="中文"); L.En=true; Assert(e.Name()=="English"); });
Check("crate reset cancels old queued cleanup", () => { Crates.Reset(); var crate=Crates.CreateShell(); var inv=Crates.InnerOf(crate); inv.onSlotRemoveItemEndFunc(null, inv, null); Crates.Reset(); Crates.Tick(); Assert(crate.DestroyCount==0); });
Check("crate cleanup reentry and reset preserve next crate", () => { Crates.Reset(); var a=Crates.CreateShell(); var b=Crates.CreateShell(); var ai=Crates.InnerOf(a); var bi=Crates.InnerOf(b); ai.onSlotRemoveItemEndFunc(null,ai,null); bi.onSlotRemoveItemEndFunc(null,bi,null); a.OnDestroy=()=>{Crates.Tick();Crates.Reset();}; Crates.Tick(); Assert(a.DestroyCount==1&&b.DestroyCount==0); });
Check("crate callback cannot cross dictionary replacement", () => {Crates.Reset();var crate=Crates.CreateShell();var inv=Crates.InnerOf(crate);SaveSlot.CurrentStore.modData=new();inv.onSlotRemoveItemEndFunc(null,inv,null);Crates.Tick();Assert(crate.DestroyCount==0);});
Check("delivery merges orders into maximum three crates", () => { Crates.Reset(); PlayerStore.Placed.Clear(); GraphUtils.Capacity=2; Goods.Factory=()=>new GameItem(); var c=new Company();var s=new CompanyState();var orders=Enumerable.Range(0,4).Select(i=>new Order{Items=new List<string>{"a","b"}}).ToList();s.Orders.AddRange(orders);CorpState.Current.Companies[c.Id]=s;CompanyVisitors.Receipt(c,s,orders)();Assert(PlayerStore.Placed.Count==3);Assert(orders.Sum(o=>o.Items.Count)==2);Assert(s.Orders.Count==1); });
Check("Jackson paid overdue equipment reaches a real registered crate",()=>{Crates.Reset();PlayerStore.Placed.Clear();GraphUtils.Capacity=10;var c=new Company{Id="jackson_private",Catalog=new(){new CatalogEntry{Id="@rat_wheel_generator"},new CatalogEntry{Id="animal_cage"},new CatalogEntry{Id="livestock_feed_dispenser"}}};var s=new CompanyState();var o=new Order{Day=0,Items=new(){"@rat_wheel_generator","animal_cage","livestock_feed_dispenser"}};s.Orders.Add(o);CorpState.Current.Companies[c.Id]=s;CompanyVisitors.Receipt(c,s,new(){o})();Assert(PlayerStore.Placed.Count==1&&o.Items.Count==0&&s.Orders.Count==0&&PlayerStore.Placed[0].Inner.childItems.Count==3);});
Check("every equipment supplier maps to a registered crate",()=>{foreach(var id in new[]{"pastel","davre","solana","spacer","meridian","carmicheal","jackson_private"})Assert(Goods.RegisteredCrates.Contains(Crates.GoodsKey(new Company{Id=id})));Assert(Goods.RegisteredCrates.Contains(Crates.GoodsKey(new CorpPresence.Malice.RebelCo())));});
Check("missing Jackson crate preserves the paid order for retry",()=>{Crates.Reset();PlayerStore.Placed.Clear();GraphUtils.Capacity=10;var c=new Company{Id="jackson_private"};var s=new CompanyState();var o=new Order{Day=0,Items=new(){"@rat_wheel_generator"}};s.Orders.Add(o);CorpState.Current.Companies[c.Id]=s;var key=Crates.GoodsKey(c);Assert(Goods.RegisteredCrates.Remove(key));try{CompanyVisitors.Receipt(c,s,new(){o})();Assert(PlayerStore.Placed.Count==0&&o.Items.Count==1&&s.Orders.Count==1);}finally{Goods.RegisteredCrates.Add(key);}StoreStation.Day++;CompanyVisitors.Receipt(c,s,new(){o})();Assert(PlayerStore.Placed.Count==1&&o.Items.Count==0&&s.Orders.Count==0);});
Check("receipt for replaced dictionary leaves all orders", () => {var c=new Company();var s=new CompanyState();var o=new Order{Items=new List<string>{"a"}};s.Orders.Add(o);CorpState.Current.Companies[c.Id]=s;var receipt=CompanyVisitors.Receipt(c,s,new(){o});SaveSlot.CurrentStore.modData=new();receipt();Assert(o.Items.Count==1&&o.Day==0);});
Check("fragmented counter defers before creating goods and retries when a crate fits", () =>
{
    Crates.Reset();
    PlayerStore.Placed.Clear();
    GraphUtils.Capacity = 10;
    var counter = new GameGridInventory { HasCrateSlot = false };
    EmporiumEntry.Instance = new EmporiumEntry { frontInvinvElement = counter };
    try
    {
        int made = 0;
        Goods.Factory = () => { made++; return new GameItem(); };
        var company = new Company();
        var state = new CompanyState();
        var order = new Order { Day = StoreStation.Day, Items = new() { "a", "b" } };
        state.Orders.Add(order);
        CorpState.Current.Companies[company.Id] = state;
        CompanyVisitors.Receipt(company, state, new() { order })();
        Assert(made == 0 && PlayerStore.Placed.Count == 0);
        Assert(order.Day == StoreStation.Day + 1 && order.Items.SequenceEqual(new[] { "a", "b" }) && state.Orders.Contains(order));
        StoreStation.Day++;
        counter.HasCrateSlot = true;
        CompanyVisitors.Receipt(company, state, new() { order })();
        Assert(made == 2 && PlayerStore.Placed.Count == 1 && state.Orders.Count == 0);
    }
    finally { EmporiumEntry.Instance = null; }
});
Check("a successful slot probe does not commit an order when placement fails", () =>
{
    Crates.Reset();
    PlayerStore.Placed.Clear();
    GraphUtils.Capacity = 10;
    EmporiumEntry.Instance = new EmporiumEntry { frontInvinvElement = new GameGridInventory() };
    try
    {
        var store = SaveSlot.CurrentStore;
        store.RefusePlacement = true;
        var company = new Company();
        var state = new CompanyState();
        var order = new Order { Day = StoreStation.Day, Items = new() { "a" } };
        state.Orders.Add(order);
        CorpState.Current.Companies[company.Id] = state;
        CompanyVisitors.Receipt(company, state, new() { order })();
        Assert(PlayerStore.Placed.Count == 0 && order.Day == StoreStation.Day + 1);
        Assert(order.Items.SequenceEqual(new[] { "a" }) && state.Orders.Contains(order));
    }
    finally { EmporiumEntry.Instance = null; }
});
Check("nightly replacement during payment stops later writes", () => {var c=new Company();var s=new CompanyState{TrafficOn=true,VipOn=true,DealerJoined=true,WeekSales=100,DealerSinceDay=-5};CorpState.Current.Companies[c.Id]=s;Companies.All=new(){c};Services.PayHook=()=>{SaveSlot.CurrentStore.modData=new();return null;};Services.OnNightlyServices();Assert(s.Visitors.Count==0&&s.VipOn&&s.WeekSales==100&&s.Orders.Count==0);});
Check("nightly raw value change during payment stops later writes", () => {var c=new Company();var s=new CompanyState{TrafficOn=true,VipOn=true};CorpState.Current.Companies[c.Id]=s;Companies.All=new(){c};Services.PayHook=()=>{SaveSlot.CurrentStore.modData["CorpPresence"]="new";return "error";};Services.OnNightlyServices();Assert(s.TrafficOn&&s.VipOn&&s.Visitors.Count==0);});
Check("delivery refuses borrowed inventory factory result", () => {var sample=new GameItem{parentInventory=new GameInventory()};var owner=sample.parentInventory;Goods.Factory=()=>sample;var c=new Company();var s=new CompanyState();var o=new Order{Items=new(){"a"}};s.Orders.Add(o);CorpState.Current.Companies[c.Id]=s;CompanyVisitors.Receipt(c,s,new(){o})();Assert(sample.parentInventory==owner&&sample.DestroyCount==0&&o.Items.Count==1);});
Check("native loaded crate rebinds after state and cleanup reset", () => {Crates.Reset();var loaded=Crates.CreateShell();loaded.identifier="crate_company";loaded.parentInventory=new GameInventory();var inv=Crates.InnerOf(loaded);CorpState.Current=null;Crates.Reset();CorpState.Current=new CorpState(SaveSlot.CurrentStore);EmporiumEntry.Instance=new EmporiumEntry{OwnedItems=new(){loaded}};typeof(Crates).GetMethod("RebindLoaded")?.Invoke(null,null);inv.onSlotRemoveItemEndFunc(null,inv,null);Crates.Tick();Assert(loaded.DestroyCount==1);});
Check("postload rebind excludes retained old world crate", () => {Crates.Reset();var stale=Crates.CreateShell();stale.identifier="crate_company";stale.parentInventory=new GameInventory();var oldInventory=Crates.InnerOf(stale);SaveSlot.CurrentStore=new PlayerStore();CorpState.Current=new CorpState(SaveSlot.CurrentStore);Crates.Reset();var current=Crates.CreateShell();current.identifier="crate_company";current.parentInventory=new GameInventory();EmporiumEntry.Instance=new EmporiumEntry{OwnedItems=new(){current}};typeof(Crates).GetMethod("RebindLoaded")?.Invoke(null,null);oldInventory.onSlotRemoveItemEndFunc(null,oldInventory,null);Crates.Tick();Assert(stale.DestroyCount==0);var inv=Crates.InnerOf(current);inv.onSlotRemoveItemEndFunc(null,inv,null);Crates.Tick();Assert(current.DestroyCount==1);});
Check("unready native shell binds once after resources complete", () => {Crates.Reset();GameResources.Ready=false;var loaded=Crates.CreateShell();loaded.identifier="crate_company";loaded.parentInventory=new GameInventory();var inv=Crates.InnerOf(loaded);CorpState.Current=null;Crates.Reset();GameResources.Ready=true;CorpState.Current=new CorpState(SaveSlot.CurrentStore);EmporiumEntry.Instance=new EmporiumEntry{OwnedItems=new(){loaded}};Crates.RebindLoaded();Crates.RebindLoaded();Assert(inv.onSlotRemoveItemEndFunc.GetInvocationList().Length==1);inv.onSlotRemoveItemEndFunc(null,inv,null);Crates.Tick();Assert(loaded.DestroyCount==1);});
Check("stack mod cap five delivers two stacks and clears ten order entries",()=>{Crates.Reset();PlayerStore.Placed.Clear();GraphUtils.Capacity=120;GraphUtils.Stacking=true;var c=new Company();var s=new CompanyState();var o=new Order{Items=Enumerable.Repeat("a",10).ToList()};s.Orders.Add(o);CorpState.Current.Companies[c.Id]=s;CompanyVisitors.Receipt(c,s,new(){o})();Assert(PlayerStore.Placed.Count==1);var items=PlayerStore.Placed[0].Inner.childItems;Assert(items.Count==2&&items.All(x=>x.unitCount==5));Assert(o.Items.Count==0&&s.Orders.Count==0);});
Check("merged transfer is acknowledged even if source remains alive at zero",()=>{Crates.Reset();GraphUtils.Capacity=120;GraphUtils.Stacking=true;GraphUtils.IgnoreDeclaredLimit=true;var box=Crates.CreateShell();Assert(Crates.Put(box,new GameItem()));var source=new GameItem();Assert(Crates.Put(box,source));Assert(source.unitCount==0&&source.DestroyCount==0&&box.Inner.childItems.Single().unitCount==2);});
Check("loaded crate preserves native slot policy and existing stack",()=>{Crates.Reset();var box=Crates.CreateShell();box.identifier="crate_company";box.parentInventory=new GameInventory();var inv=box.Inner;Func<GameItem,GameInventory,SlotMarker,int> policy=(_,_,_)=>9;inv.maxSlotAmountItemFunc=policy;inv.childItems.Add(new GameItem{unitCount=5,parentInventory=inv});EmporiumEntry.Instance=new EmporiumEntry{OwnedItems=new(){box}};Crates.RebindLoaded();Assert(ReferenceEquals(inv.maxSlotAmountItemFunc,policy)&&inv.childItems.Single().unitCount==5);});
Check("refused crate transfer preserves source quantity",()=>{Crates.Reset();GraphUtils.Capacity=0;var box=Crates.CreateShell();var source=new GameItem();Assert(!Crates.Put(box,source)&&source.unitCount==1&&box.Inner.childItems.Count==0);});
Check("without stacking delivery retains ten individual items",()=>{Crates.Reset();PlayerStore.Placed.Clear();GraphUtils.Capacity=120;var c=new Company();var s=new CompanyState();var o=new Order{Items=Enumerable.Repeat("a",10).ToList()};s.Orders.Add(o);CorpState.Current.Companies[c.Id]=s;CompanyVisitors.Receipt(c,s,new(){o})();Assert(PlayerStore.Placed.Count==1&&PlayerStore.Placed[0].Inner.childItems.Count==10&&o.Items.Count==0);});
Check("different identifiers retain independent quantities",()=>{Crates.Reset();GraphUtils.Capacity=120;GraphUtils.Stacking=true;var box=Crates.CreateShell();foreach(var id in new[]{"a","a","b","b","b"})Assert(Crates.Put(box,new GameItem{identifier=id}));Assert(box.Inner.childItems.Count==2&&box.Inner.childItems.Single(x=>x.identifier=="a").unitCount==2&&box.Inner.childItems.Single(x=>x.identifier=="b").unitCount==3);});
Check("stacking preserves crate native default capacity policy",()=>{Crates.Reset();var box=Crates.CreateShell();Assert(box.Inner.maxSlotAmountItemFunc==null);});
Check("placed crate retains a live controller after clearing its order", () =>
{
    Crates.Reset(); PlayerStore.Placed.Clear(); GraphUtils.Capacity = 10;
    Goods.Factory = () => new GameItem { identifier = "corp_presence_cargo_sort_controller" };
    var company = new Company { Id = "jackson_private", Catalog = new() { new CatalogEntry { Id = "@cargo_sort_controller" } } };
    var state = new CompanyState();
    var order = new Order { Items = new() { "@cargo_sort_controller" } };
    state.Orders.Add(order); CorpState.Current.Companies[company.Id] = state;
    CompanyVisitors.Receipt(company, state, new() { order })();
    Assert(PlayerStore.Placed.Count == 1 && state.Orders.Count == 0 && order.Items.Count == 0);
    var crate = PlayerStore.Placed.Single();
    var inventory = Crates.InnerOf(crate);
    var controller = inventory.childItems.Single();
    Assert(crate.parentInventory != null && controller.parentInventory == inventory && controller.unitCount == 1
        && controller.identifier == "corp_presence_cargo_sort_controller" && controller.DestroyCount == 0);
});
Console.WriteLine($"{passed} passed, {failed} failed"); return failed==0?0:1;
namespace CorpPresence {
class PlayerStore {public List<(string,int)> ActivePermits=new(); public int contrabandMarkupLow=35, contrabandMarkupMid=70, contrabandMarkupHigh=100, contrabandMarkupCritial=150;public string runID="run"; public Dictionary<string,string> modData=new();public static PlayerStore Instance=>SaveSlot.CurrentStore;public void AddNightLog(string text,string color=null){}public Client currentClientInstance; public static List<GameItem> Placed=new();public bool RefusePlacement; public bool TryAddDirectSellingItemToTable(GameItem crate,bool isOwend){if(RefusePlacement)return false;Placed.Add(crate);crate.parentInventory=new GameInventory();return true;}}
static class SaveSlot {public static PlayerStore CurrentStore;public static bool CanWrite(string key)=>true;}
static class GameResources {public static bool Ready=true;public static long Generation;}
class CorpState {public static CorpState Current;public string RunId="run";public Dictionary<string,CompanyState> Companies=new();SaveBinding binding; public CorpState(PlayerStore s){binding=new SaveBinding(s,"CorpPresence");} public bool IsBoundTo(PlayerStore s)=>binding.Matches(s);public static CorpState Get()=>Current;}
class TagState {public string Value; public string GetString()=>Value;}
class GameItem:GraphNodeStorage {public bool IsContraband; public string Level; public string RestrictedType; public bool IsGameItemType(string type)=>type==RestrictedType; public int EventPercent; public bool IsTag(string tag)=>tag=="CONTRABAND_ITEM_TAG" && IsContraband; public TagState GetTagReadonly(string tag)=>new TagState{Value=Level};public GameInventory parentInventory;public GameInventory Inner;public string identifier="item"; public int unitCount=1; public long Value=10;public int DestroyCount;public Func<long> Read;public Action OnDestroy; public long GetCurrentValue(bool useRetailMarkup,bool withChild,bool includeEvents,bool forceMarkup,long extraMarketMod)=>Read?.Invoke()??(long)Math.Round(Value*(1+((includeEvents?EventPercent:0)+extraMarketMod)/100.0));public void Destroy(){DestroyCount++;OnDestroy?.Invoke();}public string GetDisplayName()=>L.En?"English":"中文";public GameItem SetContentWindow(PixelWindow window){Inner=window.Inventory;return this;}public void EnableTag(string tag){}}
static partial class Goods {public static Func<GameItem> Factory;public static bool Has(string id)=>false;public static GameItem MakeSpecial(string id)=>Factory();public static string IdOf(GameItem item)=>item.identifier;public static GameItem Make(string id)=>id.StartsWith("crate_")?(RegisteredCrates.Contains(id)?Crates.CreateShell():null):Factory();}
static class ItemRegistry {public static GameItem Create(string id)=>Goods.Factory();}
static class DirectoryMaster {public static GameItem Item(string id,bool isOwned)=>Goods.Factory();}
static class PastelCo {public const int TrafficFee=10,RebatePeriod=7;public static GameItem GenuineHemingway(bool limited=false)=>Goods.Factory();}
class CarmichaelCo:Company {public static GameItem MakeGenuine(string id)=>Goods.Factory();}
static class MeridianCo {public static GameItem MakeNew(string id)=>Goods.Factory();public static GameItem MakeRefurbished(string id)=>Goods.Factory();}
static class DavreCo {public static GameItem MakePrebuilt(string id)=>Goods.Factory();}
class SpacerCo:Company {public const int MutualFee=10;public static GameItem MakeGun(string id)=>Goods.Factory();public static string RollModBox()=>"a";}
static class Settings {public static bool OrderEvents=true,OrderLimits=true,MaliceMode;}
static class L
{
    public static bool En;
    public static string T(string zh, string en) => En ? en : zh;
    public static string Translate(FormattableString text) => text.ToString();
    public static string K(string key, string zh, string en) => T(zh, en);
    public static string K(string key, string zh, FormattableString en) => En ? en.ToString() : zh;
    public static string K(string key, FormattableString zh, FormattableString en) =>
        (En ? en : zh).ToString();
}
static class ModLog {public static void Warn(string s){}public static void Info(string s){}}
static class Ext {public static T Ask<T>(string s,Func<T> f,T other)=>f();}
static class StorePlacement {public static bool Destroyed(GameItem i)=>i.DestroyCount>0;}
class StoreEvent {public string identifier;}
class EventManager {public List<StoreEvent> GetActiveEvents()=>new();}
class StoreStation {public static int Day;public static StoreStation Instance=new();public EventManager storeEventManager=new();public static int GetDayCounter()=>Day;}
}
