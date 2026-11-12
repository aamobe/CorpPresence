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
var nodes = AccessTools.Method(typeof(SaveManager), nameof(SaveManager.DecodeNodes));
harmony.Patch(nodes, prefix: new HarmonyMethod(typeof(ItemRegistry), nameof(ItemRegistry.BeforeDecode)));
harmony.Patch(nodes, prefix: Hook("BeforeInventoryNodes"), finalizer: Hook("AfterInventoryNodes"));
harmony.Patch(AccessTools.Method(typeof(GraphUtils), nameof(GraphUtils.DestroyIfUnplaced)),
    prefix: Hook("BeforeNativePlacementCleanup"), postfix: Hook("AfterNativePlacementCleanup"));
harmony.Patch(AccessTools.Method(typeof(ItemDirectory), nameof(ItemDirectory.CreateEmptyItem)),
    postfix: Hook("AfterCreateSavedPlaceholder"));
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
Check(Probe.Repairs == 0 && StoreSession.CompanyProgressPaused && StoreUIManager.Instance.Notices == 1,
    "resource readiness alone cannot resume a load containing placeholders");
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
Check(store.DecodeFailures == 2 && Probe.RepairCalls.All(n => n == 1) && store.Saves == 6
    && StoreUIManager.Instance.Notices == noticesBeforeFailure && !StoreSession.CompanyProgressPaused
    && StoreSession.InventoryLoadReport.Contains("Late=已到达 库存恢复=解码失败")
    && StoreSession.InventoryLoadReport.Contains("主库存 · InvalidOperationException · 打印机结构不兼容")
    && StoreSession.InventoryLoadReport.Contains("后仓 · InvalidOperationException · Future Tech 拦截"),
    "decode errors without evidence of company loss are logged without pausing company work");
store.FailInventory = false;
Array.Clear(Probe.RepairCalls);
store.HasSave = false;
store.LoadGame();
Check(!StoreSession.CompanyProgressPaused && Probe.Repairs == 0, "missing file does not create a company pause");
store.HasSave = true;
store.LoadGame();
Check(Probe.RepairCalls.All(count => count == 1) && StoreUIManager.Instance.Notices == noticesBeforeFailure,
    "successful reload repairs once without repeating the warning");
Check(!StoreSession.CompanyProgressPaused && StoreSession.InventoryLoadReport.Contains("库存恢复=完整解码"),
    "complete inventory restoration releases the company pause");

store.HasCompanyItem = true;
DirectoryMaster.Native.Remove("corp_presence_test");
store.NextDecoded = null;
Goods.Throw = true;
store.SavedMachineTags = true;
int failedBeforeFactory = store.DecodeFailures;
store.LoadGame();
Goods.Throw = false;
store.SavedMachineTags = false;
Check(store.DecodeFailures == failedBeforeFactory && store.Decoded?.spritePath == "unknown"
    && store.Decoded.IsTag("IS_OWNED_TAG") && store.NextDecoded?.identifier == "vanilla_test" && StoreSession.CompanyProgressPaused,
    "single failed company factory leaves later vanilla objects in the same inventory recoverable");
Check(StoreSession.InventoryLoadReport.Contains("主库存 · 物品=corp_presence_test · 公司物品创建失败"),
    "factory diagnostic reports inventory, item and reason");
store.HasSave = false;
store.LoadGame();
Check(StoreSession.CompanyProgressPaused, "missing file does not lift a confirmed company pause");
store.HasSave = true;

Goods.Throw = true;
Check(Throws(() => PlayerStore.DecodeCompanyItem()), "standalone creation never receives the load-only placeholder fallback");
Goods.Throw = false;
foreach (bool wrongIdentity in new[] { false, true })
{
    Goods.ReturnNull = !wrongIdentity;
    Goods.Product = wrongIdentity ? new GameItem { identifier = "wrong_item" } : null;
    store.LoadGame();
    Check(store.Decoded?.spritePath == "unknown" && store.NextDecoded?.parentInventory != null
        && StoreSession.InventoryLoadReport.Contains(wrongIdentity ? "wrong_identity" : "factory_returned_null"),
        "other saved items survive factory " + (wrongIdentity ? "wrong identity" : "null result"));
    Goods.ReturnNull = false;
    Goods.Product = null;
}

store.HasCompanyItem = false;
store.RefusePlacement = store.IncludeNestedItem = store.MissingChildSlot = true;
Array.Clear(Probe.RepairCalls);
int noticesBeforeLoss = StoreUIManager.Instance.Notices;
int logsBeforeLoss = ModLog.Information.Count;
int warningsBeforeLoss = ModLog.Warnings.Count;
store.LoadGame();
Hooks.ShowInventoryLoadNotice();
Check(store.NextDecoded.IsDestroyed && !StoreSession.CompanyProgressPaused && Probe.RepairCalls.All(n => n == 1)
    && StoreSession.InventoryLoadReport.Contains("Late=已到达 库存恢复=恢复不完整")
    && StoreSession.InventoryLoadReport.Contains("主库存 · 物品=vanilla_test · 原版目标库存拒收物品")
    && StoreSession.InventoryLoadReport.Contains("主库存 · 物品=nested_item · 容器子槽缺失")
    && StoreUIManager.Instance.Notices == noticesBeforeLoss,
    "third-party placement loss and missing child slot leave company work active");
string[] automaticReports = ModLog.Information.Skip(logsBeforeLoss)
    .Where(message => message.StartsWith("[读档恢复失败]", StringComparison.Ordinal)).ToArray();
Check(automaticReports.Length == 1
    && !ModLog.Warnings.Skip(warningsBeforeLoss).Any(message => message.StartsWith("[读档恢复失败]", StringComparison.Ordinal))
    && automaticReports[0].Contains("[维护信息] 未发现公司物品或存档数据受影响。仅记录读档现象，不代表故障由公司增强造成。 公司送货和来访暂停=False")
    && automaticReports[0].Contains("Late=已到达 库存恢复=恢复不完整")
    && automaticReports[0].Contains("主库存 · 物品=vanilla_test · 原版目标库存拒收物品")
    && automaticReports[0].Contains("主库存 · 物品=nested_item · 容器子槽缺失"),
    "a failed load automatically logs one report containing both item failures");
store.SaveGame();
Check(store.Saves == 7, "native save still runs with placement losses");
store.CompanyNestedItem = true;
store.LoadGame();
Check(StoreSession.CompanyProgressPaused
    && StoreSession.InventoryLoadReport.Contains("物品=corp_presence_test · 容器子槽缺失")
    && StoreUIManager.Instance.Notices == noticesBeforeLoss + 1
    && StoreUIManager.Instance.LastNotice.Contains("Do not overwrite the original save"),
    "a company item lost inside a third-party container still pauses company work");
store.CompanyNestedItem = false;
store.RefusePlacement = store.MissingChildSlot = false;
Array.Clear(Probe.RepairCalls);
int logsBeforeRecovery = ModLog.Warnings.Count;
int infoBeforeRecovery = ModLog.Information.Count;
store.LoadGame();
Check(!StoreSession.CompanyProgressPaused && Probe.RepairCalls.All(n => n == 1)
    && store.NextDecoded.parentInventory != null && StoreUIManager.Instance.Notices == noticesBeforeLoss + 1,
    "successful reload with a valid child slot resumes company work without repeating the warning");
Check(!ModLog.Warnings.Skip(logsBeforeRecovery).Concat(ModLog.Information.Skip(infoBeforeRecovery))
    .Any(message => message.StartsWith("[读档恢复失败]", StringComparison.Ordinal)),
    "successful reload does not log an inventory recovery warning");

store.CompanyGrandchild = store.MissingChildSlot = true;
store.LoadGame();
Check(StoreSession.CompanyProgressPaused && StoreSession.InventoryLoadReport.Contains("物品=nested_item · 容器子槽缺失"),
    "missing outer slot pauses when a company item remains attached to the detached inner container");
store.MissingChildSlot = false;
store.RefuseFirstPlacement = true;
store.LoadGame();
Check(StoreSession.CompanyProgressPaused && StoreSession.InventoryLoadReport.Contains("原版目标库存拒收物品"),
    "refusing a third-party container with a company descendant pauses company work");
store.RefuseFirstPlacement = false;
store.RefusePlacement = true;
store.LoadGame();
Check(!StoreSession.CompanyProgressPaused,
    "successful company item restoration clears the pause even if an unrelated item still fails");
store.CompanyGrandchild = store.RefusePlacement = false;

store.modData["CorpPresence"] = "unreadable";
Array.Clear(Probe.RepairCalls);
store.LoadGame();
Check(StoreSession.CompanyProgressPaused && Probe.Repairs == 0
    && StoreSession.InventoryLoadReport.Contains("CorpPresence"), "unreadable company save data pauses company work");
store.modData.Remove("CorpPresence");
store.LoadGame();
Check(!StoreSession.CompanyProgressPaused && Probe.RepairCalls.All(n => n == 1),
    "company work resumes when company data is readable");
store.modData["CorpPresence.Malice"] = "unreadable";
store.LoadGame();
Check(!StoreSession.CompanyProgressPaused, "inactive Malice data does not pause company work");
Settings.MaliceMode = true;
store.LoadGame();
Check(StoreSession.CompanyProgressPaused && StoreSession.InventoryLoadReport.Contains("CorpPresence.Malice"),
    "unreadable active Malice data pauses company work");

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
