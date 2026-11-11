using CorpPresence;

int passed=0;
void Check(bool ok,string name){if(!ok)throw new Exception(name);passed++;Console.WriteLine("PASS "+name);}
void Reset()
{
    ItemRegistry.Uninstall();DirectoryMaster.Native.Clear();DirectoryMaster.Current=null;
    GameResources.Ready=false;ModLog.Warnings.Clear();Goods.Builds=Cards.Builds=0;
}
ModItemDirectory Directory()
{
    var directory=new ModItemDirectory();DirectoryMaster.Current=directory;ModHook.Fire(directory);return directory;
}
Reset();
ItemRegistry.Install();ItemRegistry.Install();Check(ModHook.Count==1,"install subscribes once");
var first=Directory();var latest=Directory();
Check(first.Adds==0 && latest.Adds==0 && Goods.Builds==0,"waiting for resources does not register or build items");
bool rejected=false;try{ItemRegistry.Activate();}catch(InvalidOperationException){rejected=true;}
Check(rejected,"activation before readiness rejected");
GameResources.Ready=true;ItemRegistry.Activate();
Check(first.Adds==0 && latest.factoryDictionary.Count==3,"activation uses latest pending directory");
Check(Goods.Builds==0 && Cards.Builds==0,"registration only installs factories");
ModHook.Fire(latest);ItemRegistry.Activate();Check(latest.Adds==3,"repeat registration and activation do not duplicate factories");
Check(ItemRegistry.Create("corp_presence_test")?.identifier=="corp_presence_test" && Goods.Builds==1,"goods use actual registered factory");
Check(ItemRegistry.Create("corp_presence_card_test")?.identifier=="corp_presence_card_test" && Cards.Builds==1,"cards use card factory");
var counters=new List<MaintenanceCounter>();ItemRegistry.AppendDiagnostics(counters);
Check(counters.Single(c=>c.Name=="items.registered").Value==3 && Goods.Builds==1 && Cards.Builds==1,"diagnostics do not instantiate items");
Func<GameItem> external=()=>new(){identifier="external"};latest.factoryDictionary["corp_presence_other"]=external;
latest.factoryDictionary["foreign"]=external;ItemRegistry.Uninstall();ItemRegistry.Uninstall();
Check(ModHook.Count==0 && latest.factoryDictionary.Count==2 && ReferenceEquals(latest.factoryDictionary["corp_presence_other"],external),"uninstall removes only owned factories and preserves replacements");
ItemRegistry.Install();GameResources.Ready=true;var fresh=Directory();
Check(fresh.factoryDictionary.Count==3,"reinstall registers new directory");
Reset();ItemRegistry.Install();GameResources.Ready=true;
var collision=new ModItemDirectory();collision.factoryDictionary["corp_presence_test"]=external;
DirectoryMaster.Native["corp_presence_other"]=external;DirectoryMaster.Current=collision;ModHook.Fire(collision);
Check(ReferenceEquals(collision.factoryDictionary["corp_presence_test"],external) && !collision.Has("corp_presence_other") && collision.Has("corp_presence_card_test"),"both mod and native collisions preserve existing factories and allow other entries");
int warnings=ModLog.Warnings.Count;ModHook.Fire(collision);Check(ModLog.Warnings.Count==warnings,"collision warnings deduplicate");
Reset();ItemRegistry.Install();GameResources.Ready=true;
var partial=new ModItemDirectory {RejectId="corp_presence_test",ThrowId="corp_presence_other"};DirectoryMaster.Current=partial;ModHook.Fire(partial);
Check(partial.factoryDictionary.Count==1 && partial.Has("corp_presence_card_test"),"rejected and throwing registrations do not block later entries");
partial.RejectId=partial.ThrowId=null;ModHook.Fire(partial);Check(partial.factoryDictionary.Count==3,"later directory notification retries failed entries");
var replacement=Directory();Check(replacement.factoryDictionary.Count==3,"a replacement directory receives all factories");
// Earlier directory ownership is deliberately not claimed by these counts; see the handoff boundary.
Reset();ItemRegistry.Install();GameResources.Ready=true;var migration=Directory();
var legacy=new GameItem {identifier="ammo_box",spritePath="corp_med_test"};
Check(ItemRegistry.Migrate(legacy) && legacy.identifier=="corp_presence_test" && !ItemRegistry.Migrate(legacy),"legacy migration changes identity once");
var vanilla=new GameItem {identifier="ammo_box",spritePath="ammo_box"};
Check(!ItemRegistry.Migrate(vanilla) && vanilla.identifier=="ammo_box","ordinary vanilla item is not migrated");
var node=new SaveItemNode {identifier="ammo_box",spritePath="corp_med_test"};
ItemRegistry.BeforeDecode(new(){null,node});Check(node.identifier=="corp_presence_test","decode migrates only recognized old node");
DirectoryMaster.Current=null;var unavailable=new GameItem {identifier="ammo_box",spritePath="corp_med_test"};
Check(!ItemRegistry.Migrate(unavailable) && unavailable.identifier=="ammo_box" && ItemRegistry.Create("corp_presence_test")==null,"unavailable destination prevents migration and creation");
DirectoryMaster.Current=migration;
foreach(var (parent,destroyed,expected) in new[]{(false,false,1),(true,false,0),(false,true,0)})
{
    var wrong=new GameItem {identifier="wrong",parentInventory=parent?new object():null,IsDestroyed=destroyed};
    migration.factoryDictionary["corp_presence_test"]=()=>wrong;
    Check(ItemRegistry.Create("corp_presence_test")==null && wrong.DestroyCalls==expected,"wrong identity cleanup respects ownership "+(parent,destroyed));
}
Check(ItemRegistry.Create(null)==null && !ItemRegistry.Migrate(null),"null inputs remain harmless");
Reset();
Console.WriteLine($"{passed} registry checks passed; game objects and identity inputs substituted.");

var saved=new IdentityEntry("corp_presence_saved","ammo_box","corp_med_saved","saved",null);
var appended=new IdentityEntry("corp_presence_new",null,"corp_med_new","new",null);
Check(IdentityContract.Validate(new[]{saved,appended},new[]{saved}).Count==0,"identity contract permits additions");
Check(IdentityContract.Validate(Array.Empty<IdentityEntry>(),new[]{saved}).Any(e=>e.Contains("removed")),"identity contract rejects removal");
foreach(var changed in new[]{saved with {LegacyId="different"},saved with {LegacySprite="different"},saved with {Goods="different"},saved with {Company="different"}})
    Check(IdentityContract.Validate(new[]{changed},new[]{saved}).Any(e=>e.Contains("changed")),"identity contract rejects changed old mapping");
Check(IdentityContract.Validate(new[]{saved,saved},new[]{saved}).Any(e=>e.Contains("duplicate")),"identity contract rejects duplicate IDs");
Check(IdentityContract.Validate(new[]{saved with {Id=" "}},Array.Empty<IdentityEntry>()).Any(e=>e.Contains("invalid")),"identity contract rejects blank identity");
Check(IdentityContract.Validate(new[]{appended with {Goods=null}},Array.Empty<IdentityEntry>()).Any(e=>e.Contains("factory")),"identity contract rejects missing factory kind");
Check(IdentityContract.Validate(new[]{appended with {Company="test"}},Array.Empty<IdentityEntry>()).Any(e=>e.Contains("factory")),"identity contract rejects ambiguous factory kind");
foreach(var entry in new[]{appended with {Goods="",Company="test"},appended with {Company=" "}})
    Check(IdentityContract.Validate(new[]{entry},Array.Empty<IdentityEntry>()).Any(e=>e.Contains("factory")),"factory null and whitespace have distinct semantics");
Check(IdentityContract.Validate(new[]{saved,appended with {LegacyId=saved.LegacyId,LegacySprite=saved.LegacySprite}},new[]{saved}).Any(e=>e.Contains("legacy")),"identity contract rejects ambiguous legacy pair");
Check(IdentityContract.ValidateRejectedPairs((id,sprite)=>sprite==saved.LegacySprite?saved.Id:null,new[]{saved}).Count>0,"resolver check rejects sprite-only recognition");
Check(IdentityContract.ValidateRejectedPairs((id,sprite)=>id==saved.LegacyId?saved.Id:null,new[]{saved}).Count>0,"resolver check rejects legacy-ID-only recognition");
if(args.Length!=1)throw new ArgumentException("Pass the freshly compiled regular CorpPresence.dll path.");
var baseline=IdentityContract.ReadBaseline(Path.Combine(AppContext.BaseDirectory,"IdentityBaseline.json"));
Check(baseline.SourceCommit=="99719c4" && baseline.Entries.Count==90,"identity baseline is the explicit reviewed 90-entry snapshot");
var actual=IdentityContract.ReadAssembly(args[0]);
var errors=IdentityContract.Validate(actual,baseline.Entries);
if(errors.Count!=0)throw new Exception(string.Join(Environment.NewLine,errors));
Check(actual.Count>=90,$"actual compiled identity table preserves baseline; entries={actual.Count}");
Console.WriteLine($"ALL ITEM CHECKS PASSED: {passed}; no Unity factories or game loaded.");
