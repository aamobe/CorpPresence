using System;
using System.Collections.Generic;
using CorpPresence.Runtime;
using HarmonyLib;

namespace CorpPresence;

internal static partial class Hooks
{
    private static LoadAttempt _activeLoad;
    private static long? _inventoryNoticeRevision;
    private const int RequiredInventoryCount = 10;

    private sealed class LoadAttempt
    {
        internal readonly PlayerStore Store;
        internal readonly long Revision = StoreSession.Revision;
        internal bool Completed;
        internal bool HasHardDecodeFailure;
        internal bool CompanyItemsAffected;
        internal string CompanyDataFailure;
        internal int OmittedInventoryFailures;
        internal int VerifiedReplacementCount;
        internal readonly bool PreviouslyPaused;
        internal readonly HashSet<int> RestoredInventories = new();
        internal readonly List<string> InventoryFailures = new();
        internal readonly List<InventoryDecode> ReplacedInventories = new();
        internal LoadAttempt(PlayerStore store, bool previouslyPaused)
        { Store = store; PreviouslyPaused = previouslyPaused; }
        internal bool IsCurrent => StoreSession.Revision == Revision && ReferenceEquals(Store, SaveSlot.CurrentStore);
    }

    internal static void InstallLoadEvents()
    {
        ItemRegistry.CompanyDecodeWarning += OnCompanyDecodeWarning;
        ItemRegistry.AllowLoadPlaceholder = TryAllowLoadPlaceholder;
        ModHook.OnGameLoadedLate += OnGameLoadedLate;
    }

    internal static void UninstallLoadEvents()
    {
        ModHook.OnGameLoadedLate -= OnGameLoadedLate;
        ItemRegistry.CompanyDecodeWarning -= OnCompanyDecodeWarning;
        ItemRegistry.AllowLoadPlaceholder = null;
        ClearLoadState();
    }

    private static void ClearLoadState()
    {
        _activeLoad = null;
        _inventoryDecode = null;
        _inventoryNoticeRevision = null;
    }

    [HarmonyPriority(Priority.First)]
    private static void BeforeLoadGame(PlayerStore __instance, out LoadAttempt __state)
    {
        __state = null;
        ClearLoadState();
        bool previouslyPaused = ReferenceEquals(__instance, SaveSlot.CurrentStore) && StoreSession.CompanyProgressPaused;
        BeforeStoreTransition();
        _activeLoad = __state = new LoadAttempt(__instance, previouslyPaused);
        if (previouslyPaused) StoreSession.RecordInventoryLoad("库存重读进行中", true);
    }

    private static void OnGameLoadedLate()
    {
        var attempt = _activeLoad;
        if (attempt != null && attempt.IsCurrent) attempt.Completed = true;
    }

    [HarmonyPriority(Priority.First)]
    private static void BeforeDecodeSaveItem(PlayerStore __instance, string __0, GameInventory __1,
        out InventoryDecode __state)
    {
        __state = default;
        var attempt = _activeLoad;
        if (attempt == null || !attempt.IsCurrent || !ReferenceEquals(attempt.Store, __instance)) return;
        _inventoryDecode = __state = new InventoryDecode(attempt, InventoryIndex(__1), _inventoryDecode) { Target = __1, SavedJson = __0 };
    }

    [HarmonyPriority(Priority.First)]
    private static void AfterDecodeSaveItem(Exception __exception, bool __runOriginal,
        InventoryDecode __state)
    {
        if (__state == null) return;
        if (ReferenceEquals(_inventoryDecode, __state)) _inventoryDecode = __state.Previous;
        var attempt = __state.Attempt;
        if (!IsCurrentInventory(__state)) return;
        bool replaced = __exception == null && !__runOriginal;
        __state.DecoderFinished = true;
        foreach (var finding in __state.PendingFailures)
            RecordInventoryFailure(__state, replaced ? "替代解码中间阶段：" + finding.Reason : finding.Reason,
                finding.ItemId, finding.CompanyAffected, observationOnly: replaced);
        __state.PendingFailures.Clear();
        if (__exception == null && __runOriginal)
        {
            if (__state.Index >= 0) attempt.RestoredInventories.Add(__state.Index);
            return;
        }
        if (replaced)
        {
            attempt.ReplacedInventories.Add(__state);
            return;
        }
        attempt.HasHardDecodeFailure = true;
        RecordInventoryFailure(__state, DescribeInventoryFailure(__exception),
            companyAffected: HasUnrestoredCompanyItems(__state));
    }

    private static readonly string[] InventoryNames = {
        "主库存", "后仓", "隐藏库存", "展示柜", "已售物品", "雇员", "自动售货机", "饮料机", "垃圾桶", "档案", "磁带播放器"
    };

    private static int InventoryIndex(GameInventory inventory)
    {
        var shop = EmporiumEntry.Instance;
        if (shop == null || inventory == null) return -1;
        if (ReferenceEquals(inventory, shop.invElement)) return 0;
        if (ReferenceEquals(inventory, shop.backInvinvElement)) return 1;
        if (ReferenceEquals(inventory, shop.hiddenElement)) return 2;
        if (ReferenceEquals(inventory, shop.showcaseElement)) return 3;
        if (ReferenceEquals(inventory, shop.soldElement)) return 4;
        if (ReferenceEquals(inventory, shop.hirelingInv)) return 5;
        if (ReferenceEquals(inventory, shop.vendingMachineElement)) return 6;
        if (ReferenceEquals(inventory, shop.vendingFountainElement)) return 7;
        if (ReferenceEquals(inventory, shop.trashcan?.contentWindow?.child)) return 8;
        if (ReferenceEquals(inventory, shop.dossier?.contentWindow?.child)) return 9;
        return ReferenceEquals(inventory, shop.cassettePlayerElement) ? 10 : -1;
    }

    private static string DescribeInventoryFailure(Exception error)
    {
        string message = error.Message ?? "";
        string reason = message.StartsWith("FutureTech stopped an unsafe store decode", StringComparison.Ordinal)
            ? "Future Tech 拦截后续库存解码"
            : message.Contains("打印机结构不兼容") ? "打印机结构不兼容"
            : error is KeyNotFoundException ? "解码引用的条目不存在"
            : error is NullReferenceException ? "解码访问了未就绪的对象"
            : "库存解码抛出异常，详情见原版日志";
        return error.GetType().Name + " · " + reason;
    }

    private static bool HasCompleteInventoryDecode(LoadAttempt attempt)
    {
        int expected = RequiredInventoryCount + (string.IsNullOrEmpty(attempt.Store.cassettePlayerInvJSON) ? 0 : 1);
        for (int i = 0; i < expected; i++)
            if (!attempt.RestoredInventories.Contains(i)) return false;
        return true;
    }

    private static string FormatInventoryLoad(LoadAttempt attempt, string completion)
    {
        string result = completion;
        if (attempt.InventoryFailures.Count != 0)
            result = attempt.HasHardDecodeFailure ? "解码失败"
                : attempt.ReplacedInventories.Count != 0 && HasCompleteInventoryDecode(attempt)
                    && !attempt.CompanyItemsAffected && completion == null
                    ? "公司物品已核验" : attempt.ReplacedInventories.Count == 0 ? "恢复不完整" : "恢复未确认";
        else if (attempt.Completed && completion == null)
            result = HasCompleteInventoryDecode(attempt) ? "完整解码" : "未验证";
        return "Late=" + (attempt.Completed ? "已到达" : "未到达") + " 库存恢复=" + result
            + (attempt.CompanyDataFailure == null ? "" : "\n[维护] " + attempt.CompanyDataFailure)
            + (attempt.InventoryFailures.Count == 0 ? "" : "\n[维护] " + string.Join("\n[维护] ", attempt.InventoryFailures))
            + (attempt.OmittedInventoryFailures == 0 ? "" : $"\n[维护] 另有 {attempt.OmittedInventoryFailures} 条库存问题未展开");
    }

    internal static void ShowInventoryLoadNotice()
    {
        if (_inventoryNoticeRevision != StoreSession.Revision || !StoreSession.CompanyProgressPaused
            || StoreUIManager.Instance == null) return;
        _inventoryNoticeRevision = null;
        Guard("提示库存解码失败", () => StoreUIManager.Instance.Notify(L.K(
            "integration.hooks.load.inventoryDecodeFailed",
            "本次读档有公司物品或公司存档数据未能恢复。公司送货和来访已暂停。请勿覆盖原档，排除问题后重读。详情已写入 Player.log。",
            "Some company items or company save data could not be restored. Company deliveries and visits are paused. Do not overwrite the original save. Reload after resolving the issue. Details are in Player.log.")));
    }

    [HarmonyPriority(Priority.Last)]
    private static void AfterLoadGame(bool __runOriginal, LoadAttempt __state)
    {
        if (__state == null || !ReferenceEquals(_activeLoad, __state)) return;
        if (!__runOriginal || !__state.Completed || !__state.IsCurrent)
        {
            _activeLoad = null;
            if (__state.IsCurrent)
            {
                CheckReplacedInventories(__state, loadCompleted: false);
                RecordLoadOutcome(__state, "读档未完成", __state.CompanyItemsAffected || __state.PreviouslyPaused);
            }
            MaintenanceDiagnostics.RecordRuntime("load", "completion",
                !__runOriginal ? "native_skipped" : __state.Completed ? "session_changed" : "late_signal_missing", null);
            return;
        }

        CheckReplacedInventories(__state, loadCompleted: true);
        ResetState();
        if (!ReferenceEquals(_activeLoad, __state) || !ReferenceEquals(__state.Store, SaveSlot.CurrentStore)) return;
        _activeLoad = null;
        Guard("检查公司存档数据", () => __state.CompanyDataFailure = CheckCompanySaveData());
        bool paused = __state.CompanyItemsAffected || __state.CompanyDataFailure != null
            || (__state.PreviouslyPaused && !HasCompleteInventoryDecode(__state));
        RecordLoadOutcome(__state, null, paused);
        if (paused || !GameResources.Ready) return;
        LoadedItemRepair.Request(() =>
        {
            if (!StoreSession.Capture().IsCurrent)
            {
                MaintenanceDiagnostics.RecordRuntime("load", "completion", "session_not_bound", null);
                return;
            }
            Guard("读档后配送箱清理绑定", Crates.RebindLoaded);
            Guard("清理旧药品的纯度特征", ItemTags.RemoveRestoredMedicinePurity);
            Guard("修复旧物品占格", Goods.RestoreLegacyFootprints);
            Guard("读档后定制刀分类", Forge.AfterStoreLoaded);
        });
        Guard("读档后公司物品修复", LoadedItemRepair.Tick);
    }

    private static string CheckCompanySaveData()
    {
        var data = SaveSlot.CurrentStore?.modData;
        if (data == null) return null;
        if (data.ContainsKey("CorpPresence"))
        {
            CorpState.Get();
            if (!SaveSlot.CanWrite("CorpPresence")) return "公司存档数据不可读，已写保护：CorpPresence";
        }
        if (!Settings.MaliceMode || !data.ContainsKey("CorpPresence.Malice")) return null;
        Malice.MaliceState.Get();
        return SaveSlot.CanWrite("CorpPresence.Malice") ? null : "公司存档数据不可读，已写保护：CorpPresence.Malice";
    }

    private static void RecordLoadOutcome(LoadAttempt attempt, string completion, bool paused)
    {
        string report = FormatInventoryLoad(attempt, completion);
        StoreSession.RecordInventoryLoad(report, paused);
        if (attempt.InventoryFailures.Count == 0 && attempt.CompanyDataFailure == null) return;

        bool companyAffected = attempt.CompanyItemsAffected || attempt.CompanyDataFailure != null;
        Guard("写入读档恢复日志", () =>
        {
            string status = "公司送货和来访暂停=" + paused;
            if (paused && !companyAffected) status += "（已有暂停，等待完整重读核验）";
            const string attribution = "仅记录读档现象，不代表故障由公司增强造成。";
            if (companyAffected)
                ModLog.Warn("[读档恢复失败] [维护信息] 检测到公司物品或存档数据受影响。" + attribution + " " + status + " " + report);
            else
            {
                bool verified = attempt.ReplacedInventories.Count != 0
                    && attempt.VerifiedReplacementCount == attempt.ReplacedInventories.Count
                    && !attempt.HasHardDecodeFailure && !paused;
                string label = verified ? "[读档核验]" : "[读档恢复失败]";
                ModLog.Info(label + " [维护信息] 未发现公司物品或存档数据受影响。" + attribution + " " + status + " " + report);
            }
        });
        if (!companyAffected) return;
        _inventoryNoticeRevision = StoreSession.Revision;
        ShowInventoryLoadNotice();
    }

    private static void FinalizeLoadGame(Exception __exception, LoadAttempt __state)
    {
        if (__exception != null && __state != null && ReferenceEquals(_activeLoad, __state))
        {
            _activeLoad = null;
            if (__state.IsCurrent)
            {
                CheckReplacedInventories(__state, loadCompleted: false);
                RecordLoadOutcome(__state, "读档异常", __state.CompanyItemsAffected || __state.PreviouslyPaused);
            }
            MaintenanceDiagnostics.RecordRuntime("load", "completion", "load_exception", __exception);
        }
    }
}
