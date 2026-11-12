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
        internal bool HasDecodeWarning;
        internal readonly bool PreviouslyPaused;
        internal readonly HashSet<int> RestoredInventories = new();
        internal readonly List<string> InventoryFailures = new();
        internal LoadAttempt(PlayerStore store, bool previouslyPaused)
        { Store = store; PreviouslyPaused = previouslyPaused; }
        internal bool IsCurrent => StoreSession.Revision == Revision && ReferenceEquals(Store, SaveSlot.CurrentStore);
    }

    internal static void InstallLoadEvents()
    {
        ItemRegistry.CompanyDecodeWarning += OnCompanyDecodeWarning;
        ModHook.OnGameLoadedLate += OnGameLoadedLate;
    }

    internal static void UninstallLoadEvents()
    {
        ModHook.OnGameLoadedLate -= OnGameLoadedLate;
        ItemRegistry.CompanyDecodeWarning -= OnCompanyDecodeWarning;
        ClearLoadState();
    }

    private static void ClearLoadState()
    {
        _activeLoad = null;
    }

    [HarmonyPriority(Priority.First)]
    private static void BeforeLoadGame(PlayerStore __instance, out LoadAttempt __state)
    {
        __state = null;
        _activeLoad = null;
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

    private static void OnCompanyDecodeWarning()
    {
        var attempt = _activeLoad;
        if (attempt != null && attempt.IsCurrent) attempt.HasDecodeWarning = true;
    }

    [HarmonyPriority(Priority.First)]
    private static void BeforeDecodeSaveItem(PlayerStore __instance, GameInventory __1,
        out (LoadAttempt Attempt, int Inventory) __state)
    {
        __state = default;
        var attempt = _activeLoad;
        if (attempt == null || !attempt.IsCurrent || !ReferenceEquals(attempt.Store, __instance)) return;
        __state = (attempt, InventoryIndex(__1));
    }

    [HarmonyPriority(Priority.First)]
    private static void AfterDecodeSaveItem(Exception __exception, bool __runOriginal,
        (LoadAttempt Attempt, int Inventory) __state)
    {
        var (attempt, inventory) = __state;
        if (attempt == null || !ReferenceEquals(_activeLoad, attempt) || !attempt.IsCurrent) return;
        if (__exception == null && __runOriginal)
        {
            if (inventory >= 0) attempt.RestoredInventories.Add(inventory);
            return;
        }
        if (attempt.InventoryFailures.Count >= 16) return;
        string name = inventory < 0 ? "未知库存" : InventoryNames[inventory];
        string reason = __exception == null ? "原版解码被跳过" : DescribeInventoryFailure(__exception);
        attempt.InventoryFailures.Add(name + " · " + reason);
        if (attempt.InventoryFailures.Count == 1)
            StoreSession.RecordInventoryLoad(FormatInventoryLoad(attempt, null), true);
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
            result = "解码失败";
        else if (attempt.Completed && completion == null)
            result = HasCompleteInventoryDecode(attempt) ? "完整解码" : "未验证";
        return "Late=" + (attempt.Completed ? "已到达" : "未到达") + " 库存恢复=" + result
            + (attempt.InventoryFailures.Count == 0 ? "" : "\n[维护] " + string.Join("\n[维护] ", attempt.InventoryFailures));
    }

    internal static void ShowInventoryLoadNotice()
    {
        if (_inventoryNoticeRevision != StoreSession.Revision || !StoreSession.CompanyProgressPaused
            || StoreUIManager.Instance == null) return;
        _inventoryNoticeRevision = null;
        Guard("提示库存解码失败", () => StoreUIManager.Instance.Notify(L.K(
            "integration.hooks.load.inventoryDecodeFailed",
            "本次读档有库存未能恢复。公司送货和来访结算已暂停。建议保留原档，排除问题后重读。详细原因可在公司增强设置中生成诊断报告。",
            "Some inventories could not be restored. Company deliveries and visits are paused. Keep the original save and reload after resolving the problem. See the diagnostic report in CorpPresence settings.")));
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
                bool failed = __state.InventoryFailures.Count != 0;
                StoreSession.RecordInventoryLoad(FormatInventoryLoad(__state, "读档未完成"), failed || __state.PreviouslyPaused);
                if (failed) WarnInventoryLoadFailure();
            }
            MaintenanceDiagnostics.RecordRuntime("load", "completion",
                !__runOriginal ? "native_skipped" : __state.Completed ? "session_changed" : "late_signal_missing", null);
            return;
        }

        ResetState();
        if (!ReferenceEquals(_activeLoad, __state) || !ReferenceEquals(__state.Store, SaveSlot.CurrentStore)) return;
        _activeLoad = null;
        bool inventoryFailed = __state.InventoryFailures.Count != 0;
        bool paused = inventoryFailed || (__state.PreviouslyPaused && !HasCompleteInventoryDecode(__state));
        StoreSession.RecordInventoryLoad(FormatInventoryLoad(__state, null), paused);
        if (inventoryFailed)
        {
            WarnInventoryLoadFailure();
            return;
        }
        if (paused) return;
        if (__state.HasDecodeWarning)
        {
            long revision = StoreSession.Revision;
            Guard("提示公司物品未就绪", () => StoreUIManager.Instance?.Notify(
                L.K("integration.hooks.load.companyItemsUnavailable", "部分公司物品尚未就绪，可能以占位物品加载。建议保留原存档，等物品工厂就绪后重读。", "Some CorpPresence items were unavailable and may have loaded as placeholders. Keep your original save and reload once the item factories are ready.")));
            if (StoreSession.Revision != revision || !ReferenceEquals(__state.Store, SaveSlot.CurrentStore)) return;
        }
        else if (!GameResources.Ready) return;
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
        if (!__state.HasDecodeWarning) Guard("读档后公司物品修复", LoadedItemRepair.Tick);
    }

    private static void WarnInventoryLoadFailure()
    {
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
                bool failed = __state.InventoryFailures.Count != 0;
                StoreSession.RecordInventoryLoad(FormatInventoryLoad(__state, "读档异常"), failed || __state.PreviouslyPaused);
                if (failed) WarnInventoryLoadFailure();
            }
            MaintenanceDiagnostics.RecordRuntime("load", "completion", "load_exception", __exception);
        }
    }
}
