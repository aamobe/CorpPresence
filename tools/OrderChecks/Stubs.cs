namespace CorpPresence;
// Session boundary is replaced here; real session invalidation is covered by SaveStateChecks.
readonly struct StoreSession
{
 private readonly CompanyState state;
 private StoreSession(CompanyState value) { state = value; }
 public static StoreSession Capture() => new(Companies.State);
 public bool IsCurrent => ReferenceEquals(state, Companies.State);
}
class Company { public string Name => "Test"; public int CatalogTier(CompanyState s) => 2; }
class JacksonContact : Company {}
class SolanaCo : Company {}
class MeridianCo : Company {}
class CompanyState {
 public double Cooperation;
 public int LastOrderDay = -1;
 public List<Order> Orders = new();
 public Dictionary<string,int> LimitedBought = new();
}
class Order { public int Day; public List<string> Items = new(); }
class CatalogEntry {
 public string Id = "item";
 public int WindowDays = 7, Bundle = 1, MinTier = 1, Quota = 10;
 public int BaseValue() => 100;
 public int Limit() => Quota;
 public string Name() => Id;
}
static class Companies {
 public static CompanyState State = new();
 public static CompanyState StateOf(Company c) => State;
}
static class Ext {
 public static Func<Company,string> OrderBlocked => null;
 public static T Ask<T>(string name,Func<T> action,T fallback) => action();
}
static class Perfume { public static List<CatalogEntry> Catalog = new(); }
static class Rosen { public static string SaleBlocked(string id) => null; }
static class ProductionWorkBay {
 public const string OrderId = "@production_work_bay", PurchaseKey = OrderId+"@lifetime";
 public static bool AlreadyPurchased(CompanyState s) => false;
}
static class CargoWarehouse {
 public const string ControllerOrderId = "@cargo_sort_controller";
 public static int Price(string id) => 0;
 public static int Limit(string id) => 0;
 public static bool Unlocked() => true;
 public static bool ControllerAvailable(CompanyState s,bool refresh) => true;
 public static string UnlockHint() => "locked";
}
static class Settings {
 public const double CoopCredits = 100;
 public static int Period(string key,int fallback) => fallback;
}
static class L {
 public static string K(string key,string zh,string en) => en;
 public static string K(string key,string zh,FormattableString en) => en.ToString();
 public static string F(string key,string zh,string en,params object[] args) => string.Format(en,args);
}
static class ModLog { public static void Info(string s) {} public static void Warn(string s) {} }
static partial class Services {
 public static int Day => 20;
 public static string PeriodKey => "week";
 public static string PeriodWord => "week";
 public static int Charges;
 public static Func<string> Payment;
 public static string Pay(Company c,int amount,string kind) {
  string error=Payment?.Invoke();
  if(error == null) Charges++;
  return error;
 }
 public static void AddCoop(Company c,double amount,string reason) { Companies.State.Cooperation += amount; }
}
