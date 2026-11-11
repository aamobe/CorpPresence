namespace CorpPresence {
internal class PlayerStore {
 internal static PlayerStore Instance=new(); internal static bool IsInstanceExist()=>Instance!=null;
 internal Dictionary<string,string> modData=new();internal int Cash=1000,Calls;internal bool ThrowAfterDebit;
 internal bool CanPay(int n)=>Cash>=n;
 internal void PayMoney(int n,bool playRegisterSound){Calls++;Cash-=n;if(ThrowAfterDebit)throw new InvalidOperationException("debit committed");}
 internal void ModCash(int n,bool playSound){Cash+=n;}
}
internal class Company {}
internal static class Companies {internal static CompanyState State=new();internal static CompanyState StateOf(Company c)=>State;}
internal static class Settings {internal static bool MaliceMode;}
internal static class Ext {internal static Func<Company,int,string,string> Charge;}
internal static class L {internal static string K(string k,string zh,string en)=>en;}
internal static class ModLog {internal static void Warn(string s){}}
internal class EmporiumAudioPlayer {internal static EmporiumAudioPlayer Instance=new();internal static bool Fail;internal void PlayRegister(){if(Fail)throw new Exception("audio");}}
internal class NegociationUIManager {internal static NegociationUIManager Instance=null;internal Audio audioSourceCashRegister=null;}
internal class Audio {internal void Play(){}}
internal static class MaintenanceDiagnostics {internal static List<string> Codes=new();internal static bool Fail;internal static void Record(string source,string stage,string code,Exception e){if(Fail)throw new Exception("diagnostic");Codes.Add(stage+":"+code);}}
}
namespace CorpPresence.Malice {internal static class MaliceState {internal static void Get(){}}}
