namespace CorpPresence { internal static class MaintenanceDiagnostics {internal static bool Fail;internal static int Reports;internal static void Record(string source,string stage,string code,Exception error){Reports++;if(Fail)throw new Exception();}} }
namespace UnityEngine {internal struct Rect {}}
namespace CorpPresence.UI {internal class Theme {}}
namespace CorpPresence {
internal class Company {} internal class CompanyState {} internal class CatalogEntry {} internal class StoreClient {} internal class GameItem {} internal class StoreClientManager {} internal class Dialogue {}
internal static class ModLog {internal static void Warn(string s){}}
internal static class L {internal static string K(string k,string zh,string en)=>zh;}
}
namespace CorpPresence.Malice {
internal static partial class Malice {
internal static bool On=true;internal static int Days;
internal static bool Applies(Company c)=>On;
private static void OnDayStart(){Days++;}
private static string OrderBlocked(Company c)=>null;
private static int CatalogTier(Company c,CompanyState s,int tier)=>tier;
private static int? Limit(CatalogEntry e)=>null;
private static int ServicePrice(Company c,string k,int n)=>n;
private static string Charge(Company c,int n,string k)=>null;
}
internal static class MaliceState {internal static int Saves,Resets;internal static void Save(){Saves++;}internal static void Reset(){Resets++;}}
internal static class MalicePanel {internal static void Draw(Company c,UnityEngine.Rect r,CorpPresence.UI.Theme t){}}
internal static class Rebels {internal static StoreClient SpawnVisitor(Company c,string s)=>null;internal static void SoldToClient(StoreClient c,GameItem i,int n){}internal static double BuyFactor(GameItem i)=>1;}
internal static class Envoy {internal static void SoldToClient(StoreClient c,GameItem i,int n){}internal static void Schedule(StoreClientManager m){}internal static Dialogue MentorCall(Company c)=>null;internal static bool ClientBuys(StoreClient c,GameItem i)=>false;internal static int LuckBonus()=>0;internal static double OrderFactor()=>1;}
internal static class RebelStory {internal static void SoldToClient(StoreClient c,GameItem i,int n){}internal static void Schedule(StoreClientManager m){}internal static void OnReported(string s,int n){}internal static bool ClientBuys(StoreClient c,GameItem i)=>false;internal static double BuyFactor(GameItem i)=>1;}
}
