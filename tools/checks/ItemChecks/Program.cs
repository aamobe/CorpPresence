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
ItemRegistry.Install();
GameResources.Ready = true;
var retainedDirectory = new ModItemDirectory();
Check(DirectoryMaster.TryAddDirectory(retainedDirectory), "first shop load registers the global directory");
retainedDirectory.SceneObjectDestroyed = true;
var nextShopDirectory = new ModItemDirectory();
Check(retainedDirectory == null && !DirectoryMaster.TryAddDirectory(nextShopDirectory) && nextShopDirectory.Adds == 0,
    "reloading keeps the first managed directory and skips init on the rejected duplicate");
var crate = ItemRegistry.Create("corp_presence_other");
var paidGoods = ItemRegistry.Create("corp_presence_test");
var firstVisitCard = ItemRegistry.Create("corp_presence_card_test");
Check(crate?.identifier == "corp_presence_other" && paidGoods?.identifier == "corp_presence_test"
    && firstVisitCard?.identifier == "corp_presence_card_test",
    "reloading can create the delivery crate, paid goods and first-visit business card");
ItemRegistry.Uninstall();
Check(retainedDirectory.factoryDictionary.Count == 0,
    "uninstall releases owned entries from the retained managed directory");
Reset();
foreach(var nativeId in new[]{"ammo_box","blood_bag","c4","galaxy_blend"})
{
    DirectoryMaster.Native[nativeId]=()=>new(){identifier=nativeId};
    Check(ItemRegistry.IsAvailable(nativeId) && ItemRegistry.Create(nativeId)?.identifier==nativeId,"native factory remains available through existing shared entry: "+nativeId);
}
DirectoryMaster.Native.Clear();
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
int dummyBefore=DirectoryMaster.DummyCreates, postBefore=DirectoryMaster.PostprocessCalls;
Goods.ReturnNull=true;
Check(ItemRegistry.Create("corp_presence_test")==null && DirectoryMaster.DummyCreates==dummyBefore && DirectoryMaster.PostprocessCalls==postBefore,"failed owned factory never becomes an official dummy success");
Check(MaintenanceDiagnostics.Faults.Any(f => f.Code == "factory_returned_null" && f.ItemId == "corp_presence_test"),
    "reports a null factory result for the affected item");
bool directFailure=false;try{latest.factoryDictionary["corp_presence_test"]();}catch(InvalidOperationException){directFailure=true;}
Check(directFailure,"direct official use of registered factory also rejects null product");
Goods.ReturnNull=false;Goods.Builds=1;
Goods.Throw = true;
bool constructionThrew = false;
try { ItemRegistry.Create("corp_presence_test"); }
catch (InvalidOperationException) { constructionThrew = true; }
Check(constructionThrew && DirectoryMaster.DummyCreates == dummyBefore
    && MaintenanceDiagnostics.Faults.Any(f => f.Code == "factory_exception" && f.ItemId == "corp_presence_test"),
    "reports a throwing factory separately and does not create a dummy");
Goods.Throw = false;
Goods.Builds = 1;
DirectoryMaster.Native["native_empty"]=()=>null;
Check(ItemRegistry.Create("native_empty")?.spritePath=="unknown" && DirectoryMaster.DummyCreates==dummyBefore+1,"unowned vanilla empty factories retain official behavior");
DirectoryMaster.Native.Remove("native_empty");
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
int foreignCalls=0;
collision.factoryDictionary["corp_presence_test"]=()=>{foreignCalls++;return new(){identifier="corp_presence_test"};};
var blockedNode=new SaveItemNode {identifier="ammo_box",spritePath="corp_med_test"};
ItemRegistry.BeforeDecode(new(){blockedNode});
Check(!ItemRegistry.IsAvailable("corp_presence_test") && ItemRegistry.Create("corp_presence_test")==null && foreignCalls==0 && blockedNode.identifier=="ammo_box","conflicted factories cannot create or receive migrated saves");
int warnings=ModLog.Warnings.Count;ModHook.Fire(collision);Check(ModLog.Warnings.Count==warnings,"collision warnings deduplicate");
Reset();ItemRegistry.Install();GameResources.Ready=true;
var partial=new ModItemDirectory {RejectId="corp_presence_test",ThrowId="corp_presence_other"};DirectoryMaster.Current=partial;ModHook.Fire(partial);
Check(partial.factoryDictionary.Count==1 && partial.Has("corp_presence_card_test"),"rejected and throwing registrations do not block later entries");
partial.RejectId=partial.ThrowId=null;ModHook.Fire(partial);Check(partial.factoryDictionary.Count==3,"later directory notification retries failed entries");
var replacement=Directory();Check(replacement.factoryDictionary.Count==3,"a replacement directory receives all factories");
Check(partial.factoryDictionary.Count==0,"switching directories releases the previous owned factories");
Reset();ItemRegistry.Install();GameResources.Ready=true;var migration=Directory();
var legacy=new GameItem {identifier="ammo_box",spritePath="corp_med_test"};
Check(ItemRegistry.Migrate(legacy) && legacy.identifier=="corp_presence_test" && !ItemRegistry.Migrate(legacy),"legacy migration changes identity once");
var vanilla=new GameItem {identifier="ammo_box",spritePath="ammo_box"};
Check(!ItemRegistry.Migrate(vanilla) && vanilla.identifier=="ammo_box","ordinary vanilla item is not migrated");
var node=new SaveItemNode {identifier="ammo_box",spritePath="corp_med_test"};
ItemRegistry.BeforeDecode(new(){null,node});Check(node.identifier=="corp_presence_test","decode migrates only recognized old node");
DirectoryMaster.Current=null;var unavailable=new GameItem {identifier="ammo_box",spritePath="corp_med_test"};
Check(!ItemRegistry.Migrate(unavailable) && unavailable.identifier=="ammo_box" && ItemRegistry.Create("corp_presence_test")==null,"unavailable destination prevents migration and creation");
Check(MaintenanceDiagnostics.Faults.Any(f => f.Stage == "resolve" && f.Code == "global_entry_missing" && f.ItemId == "corp_presence_test"),
    "reports a missing global entry while retaining the local registration");
DirectoryMaster.Current=migration;
var ownedFactory=migration.factoryDictionary["corp_presence_test"];
foreignCalls=0;
migration.factoryDictionary["corp_presence_test"]=()=>{foreignCalls++;return new(){identifier="corp_presence_test"};};
Check(ItemRegistry.Create("corp_presence_test")==null && foreignCalls==0,"replacement factory is not invoked even if its product would match the ID");
migration.factoryDictionary["corp_presence_test"]=ownedFactory;
DirectoryMaster.Native["corp_presence_test"]=()=>{foreignCalls++;return new(){identifier="corp_presence_test"};};
Check(ItemRegistry.Create("corp_presence_test")==null && foreignCalls==0,"global factory shadowing blocks creation even while local registration survives");
Check(MaintenanceDiagnostics.Faults.Any(f => f.Stage == "resolve" && f.Code == "global_factory_replaced" && f.ItemId == "corp_presence_test"),
    "reports a conflicting global factory separately from a missing entry");
ModHook.Fire(migration);
var shadowCounters=new List<MaintenanceCounter>();ItemRegistry.AppendDiagnostics(shadowCounters);
Check(shadowCounters.Single(c=>c.Name=="items.registered").Value==3,"directory notification retains local ownership when a global factory shadows it");
DirectoryMaster.Native.Remove("corp_presence_test");
foreach(var (parent,destroyed,expected) in new[]{(false,false,1),(true,false,0),(false,true,0)})
{
    var wrong=new GameItem {identifier="wrong",parentInventory=parent?new object():null,IsDestroyed=destroyed};
    Goods.Product=wrong;
    Check(ItemRegistry.Create("corp_presence_test")==null && wrong.DestroyCalls==expected,"wrong identity cleanup respects ownership "+(parent,destroyed));
}
Goods.Product=null;
Check(ItemRegistry.Create(null)==null && !ItemRegistry.Migrate(null),"null inputs remain harmless");
DirectoryMaster.Native["corp_presence_test"]=external;ModHook.Fire(migration);ItemRegistry.Uninstall();
Check(migration.factoryDictionary.Count==0 && ReferenceEquals(DirectoryMaster.Native["corp_presence_test"],external),"uninstall releases shadowed local factories and preserves the global replacement");
Reset();
Console.WriteLine($"{passed} registry checks passed; game objects and identity inputs substituted.");
bool RegistryFlag(string name)=>(bool)(typeof(ItemRegistry).GetProperty(name,System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)?.GetValue(null) ?? false);
var lifetime=new CorpPresence.Runtime.RuntimeLifetime(_=>{});
lifetime.Start(()=>lifetime.Own(ItemRegistry.Install,ItemRegistry.Uninstall));
var waiting=Directory();
Check(RegistryFlag("IsInstalled") && !RegistryFlag("LastReleaseFailed") && waiting.Adds==0,"runtime owns item subscription before resources are ready");
GameResources.Ready=true;lifetime.Continue(ItemRegistry.Activate);
Check(waiting.Adds==3 && Goods.Builds==0,"resource continuation registers factories without creating items");
waiting.ThrowRemoveId="corp_presence_test";
lifetime.Stop();
Check(!RegistryFlag("IsInstalled") && RegistryFlag("LastReleaseFailed") && waiting.factoryDictionary.Count==1 && ModHook.Count==0,"failed release continues other removals and exposes degraded module state");
Check(!ItemRegistry.IsAvailable("corp_presence_test") && MaintenanceDiagnostics.Faults.Any(f=>f.Source=="items" && f.Stage=="release"),"failed cleanup does not authorize stale factory and reports fixed module stage");
bool refusedRestart=false;try { ItemRegistry.Install(); } catch(InvalidOperationException) {refusedRestart=true;}
Check(refusedRestart && ModHook.Count==0 && RegistryFlag("LastReleaseFailed"),"reinstall refuses to hide cleanup that still fails");
waiting.ThrowRemoveId=null;
ItemRegistry.Uninstall();
Check(waiting.factoryDictionary.Count==0 && !RegistryFlag("LastReleaseFailed"),"repeat uninstall retries retained owned cleanup");
lifetime.Start(()=>lifetime.Own(ItemRegistry.Install,ItemRegistry.Uninstall));
var restarted=Directory();
Check(restarted.factoryDictionary.Count==3 && ModHook.Count==1,"runtime restart has one subscription and one factory per identity");
lifetime.Stop();lifetime.Stop();
Check(restarted.factoryDictionary.Count==0 && ModHook.Count==0,"runtime repeated stop leaves no owned registrations");
try { lifetime.Start(()=>{lifetime.Own(ItemRegistry.Install,ItemRegistry.Uninstall);Directory();throw new InvalidOperationException("later module failed");}); } catch(InvalidOperationException) {}
Check(ModHook.Count==0 && DirectoryMaster.Current.factoryDictionary.Count==0,"later module startup failure rolls back item ownership");
MaintenanceDiagnostics.Fail=true;
ItemRegistry.Install();var diagnosticCollision=new ModItemDirectory();diagnosticCollision.factoryDictionary["corp_presence_test"]=external;
DirectoryMaster.Current=diagnosticCollision;ModHook.Fire(diagnosticCollision);
Check(diagnosticCollision.Has("corp_presence_card_test"),"diagnostics failure cannot abort other item registration");
MaintenanceDiagnostics.Fail=false;Reset();

var saved=new IdentityEntry("corp_presence_saved","ammo_box","corp_med_saved","saved",null);
var appended=new IdentityEntry("corp_presence_new",null,"corp_med_new","new",null);
Check(IdentityContract.Validate(new[]{saved,appended},new[]{saved}).Count==0,"identity contract permits additions");
Check(IdentityContract.ValidateRelease(new[]{saved,appended},new[]{saved}).Count>0,"release rejects identities absent from reviewed baseline");
Check(IdentityContract.ValidateRelease(new[]{saved},new[]{saved}).Count==0,"release accepts the exact reviewed identity set");
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
if(args.Length<1 || args.Length>2 || (args.Length==2 && args[1]!="--release-baseline"))throw new ArgumentException("Pass the freshly compiled regular CorpPresence.dll path and optional --release-baseline.");
var baseline=IdentityContract.ReadBaseline(Path.Combine(AppContext.BaseDirectory,"IdentityBaseline.json"));
Check(baseline.SourceCommit=="99719c4" && baseline.Entries.Count==90,"identity baseline is the explicit reviewed 90-entry snapshot");
var actual=IdentityContract.ReadAssembly(args[0]);
var errors=args.Length==2 ? IdentityContract.ValidateRelease(actual,baseline.Entries) : IdentityContract.Validate(actual,baseline.Entries);
if(errors.Count!=0)throw new Exception(string.Join(Environment.NewLine,errors));
Check(actual.Count>=90,$"actual compiled identity table preserves baseline; entries={actual.Count}");
Console.WriteLine($"ALL ITEM CHECKS PASSED: {passed}; no Unity factories or game loaded.");
