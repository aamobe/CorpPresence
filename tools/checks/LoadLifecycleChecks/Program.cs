using CorpPresence;
using CorpPresence.Runtime;
using HarmonyLib;
using UnityEngine.SceneManagement;

// The game's Unity/Mono Harmony build uses Cecil and legacy serialization for in-process patch metadata.
AppContext.SetData("MonoMod.DMDType", "cecil");
var harmony = new Harmony("test.corppresence.load-lifecycle");
var load = AccessTools.Method(typeof(PlayerStore), nameof(PlayerStore.LoadGame));
harmony.Patch(load, prefix: Hook("BeforeLoadGame"), postfix: Hook("AfterLoadGame"), finalizer: Hook("FinalizeLoadGame"));
harmony.Patch(load, prefix: new HarmonyMethod(typeof(Probe), nameof(Probe.SkipLoadWithLateSignal)) { priority = Priority.Last });
harmony.Patch(AccessTools.Method(typeof(PlayerStore), nameof(PlayerStore.SaveGame)), prefix: Hook("BeforeSave"));
harmony.Patch(AccessTools.Method(typeof(PlayerStore), nameof(PlayerStore.DecodeSaveItem)),
    prefix: Hook("BeforeDecodeSaveItem"), finalizer: Hook("AfterDecodeSaveItem"));
Hooks.InstallLoadEvents();
Hooks.InstallStoreScene();

var store = SaveSlot.CurrentStore = new PlayerStore();
var directory = new ModItemDirectory();
ItemRegistry.Install();
Check(DirectoryMaster.TryAddDirectory(directory), "native directory becomes available to the registry");

DirectoryMaster.Native["vanilla_test"] = () => new GameItem { identifier = "vanilla_test" };
var entry = EmporiumEntry.Instance = new EmporiumEntry();
var scene = entry.gameObject.scene = new Scene("InventoryScene");
SceneManager.Load(scene);
entry.RunStartIfEnabled();
store.SaveGame();
Check(entry.enabled && entry.Starts == 1 && store.Saves == 1 && store.Decoded?.identifier == "vanilla_test",
    "vanilla scene loads and saves before company resources are ready");

store.HasCompanyItem = true;
Array.Clear(Probe.RepairCalls);
store.LoadGame();
Check(DirectoryMaster.DummyCreates == 1 && store.Decoded?.spritePath == "unknown" && Probe.Repairs == 0
    && StoreUIManager.Instance.Notices == 1, "unavailable company item warns once and lets native decoding finish");
store.SaveGame();
Check(store.Saves == 2, "warning does not block native saving");
var otherStore = new PlayerStore();
otherStore.SaveGame();
Check(otherStore.Saves == 1, "another store can save normally");
int dummyCount = DirectoryMaster.DummyCreates;

GameResources.Ready = true;
ItemRegistry.Activate();
LoadedItemRepair.Tick();
LoadedItemRepair.Tick();
Check(Probe.RepairCalls.All(count => count == 1) && StoreUIManager.Instance.Notices == 1,
    "the warned load repairs once when resources become ready");
Array.Clear(Probe.RepairCalls);
store.LoadGame();
Check(store.Decoded?.identifier == "corp_presence_test" && Goods.Builds == 1 && DirectoryMaster.DummyCreates == dummyCount,
    "retry after readiness creates the real company item");
Check(Probe.RepairCalls.All(count => count == 1), "repeated late signals run each repair exactly once");
store.SaveGame();
Check(store.Saves == 3, "successful company retry leaves native saving available");

Array.Clear(Probe.RepairCalls);
store.HasSave = false;
store.LoadGame();
ModHook.FireLate();
LoadedItemRepair.Tick();
store.SaveGame();
Check(Probe.Repairs == 0 && store.Saves == 4, "missing file runs no company repairs and does not block vanilla saving");

store.HasSave = true;
Probe.SkipNativeLoad = true;
int loadsBeforeSkip = store.Loads;
store.LoadGame();
Check(store.Loads == loadsBeforeSkip && Probe.Repairs == 0, "another prefix cannot replace native loading with a late signal");
Probe.SkipNativeLoad = false;

store.ThrowAfterLate = true;
Check(Throws(store.LoadGame), "exception after the late signal still propagates");
LoadedItemRepair.Tick();
Check(Probe.Repairs == 0, "exception after the late signal runs no repairs");
store.ThrowAfterLate = false;
store.LoadGame();
Check(Probe.RepairCalls.All(count => count == 1), "next successful reload of the same store repairs once");

DirectoryMaster.Native["corp_presence_other"] = () => throw new Exception("Conflicting factory must not be invoked.");
store.LoadGame();
Check(store.Decoded?.identifier == "corp_presence_test", "unrelated company factory conflict leaves company load working");

var fallback = new GameItem { identifier = "corp_presence_test" };
DirectoryMaster.Native["corp_presence_test"] = () => fallback;
Check(ReferenceEquals(PlayerStore.DecodeCompanyItem(), fallback), "standalone company decode leaves a conflicting factory to native handling");
store.HasCompanyItem = false;
store.LoadGame();
store.SaveGame();
Check(store.Decoded?.identifier == "vanilla_test" && store.Saves == 5, "pure vanilla save still loads and saves despite company factory conflict");

Array.Clear(Probe.RepairCalls);
int noticesBeforeFailure = StoreUIManager.Instance.Notices;
store.FailInventory = true;
store.LoadGame();
store.SaveGame();
Check(store.DecodeFailures == 2 && Probe.Repairs == 0 && store.Saves == 6
    && StoreUIManager.Instance.Notices == noticesBeforeFailure + 1 && StoreSession.CompanyProgressPaused
    && StoreSession.InventoryLoadReport.Contains("Late=已到达 库存恢复=解码失败")
    && StoreSession.InventoryLoadReport.Contains("主库存 · InvalidOperationException · 打印机结构不兼容")
    && StoreSession.InventoryLoadReport.Contains("后仓 · InvalidOperationException · Future Tech 拦截"),
    "caught inventory failures still reach Late, warn once, skip company repair and allow native save");
store.FailInventory = false;
store.HasSave = false;
store.LoadGame();
Check(StoreSession.CompanyProgressPaused && Probe.Repairs == 0, "missing file does not lift a prior failure on the same store");
store.HasSave = true;
store.LoadGame();
Check(Probe.RepairCalls.All(count => count == 1) && StoreUIManager.Instance.Notices == noticesBeforeFailure + 1,
    "successful reload repairs once without repeating the warning");
Check(!StoreSession.CompanyProgressPaused && StoreSession.InventoryLoadReport.Contains("库存恢复=完整解码"),
    "complete inventory restoration releases the company pause");

static HarmonyMethod Hook(string name) => new(typeof(Hooks), name);
static bool Throws(Action run)
{
    try { run(); return false; }
    catch (InvalidOperationException) { return true; }
}
static void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    Console.WriteLine("PASS " + name);
}
