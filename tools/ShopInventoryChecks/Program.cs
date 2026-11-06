using CorpPresence;
int passed=0,failed=0;
void Assert(bool value){if(!value)throw new Exception("assertion");}
void Check(string name,Action test){try{EmporiumEntry.Instance=new();PlayerStore.Instance=new();Companies.State=new(){Licensed=true};Services.Day=10;test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
GameItem Sample(GameInventory inv){var i=new GameItem{identifier="blood",parentInventory=inv};inv.childItems.Add(i);EmporiumEntry.Instance.Owned.Add(i);return i;}
Check("foreign backroom goods excluded from recovery quote",()=>{Sample(EmporiumEntry.Instance.invElement);Sample(new GameInventory());Assert(Services.BloodRecoveryCount()==1);});
Check("recovery collects only local goods and pays once",()=>{var own=Sample(EmporiumEntry.Instance.invElement);var foreign=Sample(new GameInventory());Services.RequestBloodRecovery(new CarmichaelCo(),1);Assert(own.Destroyed&&foreign.parentInventory!=null&&!foreign.Destroyed&&PlayerStore.Instance.Cash==62);});
Check("normal shop container contents remain eligible",()=>{var box=new GameItem{parentInventory=EmporiumEntry.Instance.backInvinvElement};var inner=new GameInventory{ParentItem=box};Sample(inner);Assert(Services.BloodRecoveryCount()==1);});
Check("machine input goods remain protected",()=>{var machine=new GameItem{Machine=true,parentInventory=EmporiumEntry.Instance.invElement};Sample(new GameInventory{ParentItem=machine});Assert(Services.BloodRecoveryCount()==0);});
Check("goods moved to foreign storage after quote are not collected",()=>{var own=Sample(EmporiumEntry.Instance.invElement);Assert(Services.BloodRecoveryCount()==1);own.parentInventory=new GameInventory();Services.RequestBloodRecovery(new CarmichaelCo(),1);Assert(!own.Destroyed&&PlayerStore.Instance.Cash==0&&Companies.State.BloodRecoveryLastDay==-1);});
Check("native hidden storage and showcase remain eligible",()=>{Sample(EmporiumEntry.Instance.hiddenElement);Sample(EmporiumEntry.Instance.showcaseElement);Assert(Services.BloodRecoveryCount()==2);});
Check("cyclic foreign container ancestry fails closed",()=>{var inv=new GameInventory();var owner=new GameItem{parentInventory=inv};inv.ParentItem=owner;Sample(inv);Assert(Services.BloodRecoveryCount()==0);});
Console.WriteLine($"{passed} passed, {failed} failed");return failed==0?0:1;
namespace CorpPresence {
 class GameItem{public string identifier;public GameInventory parentInventory;public bool Machine,Destroyed;public bool IsGameItemType(string s)=>Machine&&s=="MACHINE";public bool IsTag(string s)=>false;public void Destroy(){Destroyed=true;}}
 class GraphNodeStorage{}
 class GameInventory:GraphNodeStorage{public GameItem ParentItem;public List<GameItem> childItems=new();public GameItem GetParentItem()=>ParentItem;public void Expel(GameItem i){childItems.Remove(i);i.parentInventory=null;}}
 class EmporiumEntry{public static EmporiumEntry Instance;public GameInventory invElement=new(),backInvinvElement=new(),backInvinvElementCounter=new(),frontInvinvElement=new(),docInvElement=new(),showcaseElement=new(),trashInvElement=new(),vendingFountainElement=new(),vendingMachineElement=new(),hiddenElement=new();public List<GameItem> Owned=new();public List<GameItem> GetAllOwnedItems(bool isBurglary=false,bool includeAfterhour=true)=>Owned;}
 class Company{}class CarmichaelCo:Company{}
 class CompanyState{public bool Licensed;public int BloodRecoveryLastDay=-1;}
 static class Companies{public static CompanyState State;public static CompanyState StateOf(Company c)=>State;}
 class PlayerStore{public static PlayerStore Instance;public int Cash;public int GetCash()=>Cash;public void ModCash(int n,bool playSound)=>Cash+=n;}
 static class SilverBlood{public static bool IsRecoverySample(GameItem i)=>i!=null&&i.identifier=="blood"&&i.parentInventory!=null&&!i.Destroyed;}
 static partial class Services{public static int Day;}
 static class L{public static string T(string a,string b)=>a; public static string Translate(FormattableString text)=>text.ToString();}
 static class ModLog{public static void Warn(string s){}public static void Info(string s){}}
}
