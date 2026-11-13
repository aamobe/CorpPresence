namespace CorpPresence;
// Session boundary is replaced here; real session invalidation is covered by SaveStateChecks.
readonly struct StoreSession
{
 private readonly CompanyState state;
 private StoreSession(CompanyState value) { state = value; }
 public static StoreSession Capture() => new(Companies.State);
 public bool IsCurrent => ReferenceEquals(state, Companies.State);
}
class TestCompany : Company {
 public string EntryError;
 public override string Id => "test";
 public override string Name => "Test";
 public override string CharacterId => "test";
 public override long PhoneNumber => 1;
 public override string[] TierNames => new[] {"one","two","three"};
 public override string[] FrontDesk => Array.Empty<string>();
 public override string FrontDeskDone => "";
 public override string CardFlavor => "";
 public override string DeskName => "";
 public override string DeskAsk => "";
 public override CatalogEntry[] Catalog => TestCatalog.ToArray();
 public List<CatalogEntry> TestCatalog = new();
 protected override string ContentOrderError(CatalogEntry entry) => EntryError;
}
class JacksonContact : TestCompany {}
class SolanaCo : TestCompany {}
class MeridianCo : TestCompany {}
class CompanyState {
 public double Cooperation; public int Coop = 50;
 public int LastOrderDay = -1;
 public List<Order> Orders = new();
 public List<object> Visitors = new();
 public Dictionary<string,int> LimitedBought = new();
}
class Order { public int Cash; public int Day; public List<string> Items = new(); }
class CatalogEntry {
 public string Id = "item";
 public int WindowDays = 7, Bundle = 1, MinTier = 1, Quota = 10;
 public int BaseValue() => 100; public int FixedValue = 100; public int Quote(double rate) => (int)Math.Round(FixedValue * rate);
 public int Limit() => Quota;
 public string Name() => Id;
}
static class Companies {
 public static CompanyState State = new();
 public static CompanyState StateOf(Company c) => State;
}
static class Ext {
 public static T Ask<T>(string name,Func<T> action,T fallback) => action();
}
static class Perfume { public static List<CatalogEntry> Catalog = new(); public static int CatalogPrice(CatalogEntry entry) => entry.FixedValue; }
static class Rosen { public static string SaleBlocked(string id) => null; }
static class ProductionWorkBay {
 public const string OrderId = "@production_work_bay", PurchaseKey = OrderId+"@lifetime";
 public static bool AlreadyPurchased(CompanyState s) => s.LimitedBought.ContainsKey(PurchaseKey) || s.Orders.Any(o=>o.Items.Contains(OrderId));
}
static class CargoWarehouse {
 public const string ControllerOrderId = "@cargo_sort_controller";
 public const string OrderId="@cargo_warehouse", ModuleOrderId="@cargo_warehouse_module";
 public const int WarehousePrice=15000, ModulePrice=2000, ControllerPrice=3000, MaxModules=4;
 public static bool IsUnlocked=true;
 public static bool Unlocked() => IsUnlocked;
 public static bool ControllerAvailable(CompanyState s,bool refresh) => !s.Orders.Any(o=>o.Items.Contains(ControllerOrderId));
 public static string UnlockHint() => "locked";
}
static class Settings {
 public static bool MaliceMode=false;
 public const double CoopCredits = 100; public static double OrderMarkup = 1.05;
 public static int Period(string key,int fallback) => fallback;
}
static class L { public static bool En = true; public static string K(string key,FormattableString text)=>text.ToString();
 public static string K(string key,string zh,string en) => en;
 public static string K(string key,string zh,FormattableString en) => en.ToString();
 public static string F(string key,string zh,string en,params object[] args) => string.Format(en,args);
}
static class ModLog { public static void Info(string s) {} public static void Warn(string s) {} }
static partial class Services {
 public static int Day => 20;
 public static string PeriodKey => "week";
 public static string PeriodWord => "week";
 public static int Charges {get=>PlayerStore.Instance.Calls;set=>PlayerStore.Instance.Calls=value;}
 public static Func<string> Payment {set {Malice.Malice.Handler=value==null ? null : (_,_,_)=>value();}}
 public static void AddCoop(Company c,double amount,string reason) { Companies.State.Cooperation += amount; }
}

class PlayerStore {
 public static PlayerStore Instance=new();public static bool IsInstanceExist()=>Instance!=null;
 public Dictionary<string,string> modData=new();public int Cash=1000000,Calls;
 public bool CanPay(int n)=>Cash>=n;
 public void PayMoney(int n,bool playRegisterSound){Cash-=n;Calls++;}
 public void ModCash(int n,bool playSound){Cash+=n;}
}
class EmporiumAudioPlayer {public static EmporiumAudioPlayer Instance=null;public void PlayRegister(){}}
class NegociationUIManager {public static NegociationUIManager Instance=null;public Audio audioSourceCashRegister=null;}
class Audio {public void Play(){}}
static class MaintenanceDiagnostics {public static void Record(string source,string stage,string code,Exception error){}}

class GameItem { public TagState GetTagReadonly(string key)=>new(); }
class TagState { public string GetString()=>null; }
