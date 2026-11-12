using System;
using System.Collections.Generic;
using HarmonyLib;

namespace CorpPresence;

internal static partial class Hooks
{
    private const string NativeLoadPlacement = "load: target inventory refused a saved item";
    private const int InventoryFailureDetailLimit = 64;
    private static InventoryDecode _inventoryDecode;

    private sealed class InventoryDecode
    {
        internal readonly LoadAttempt Attempt;
        internal readonly int Index;
        internal readonly InventoryDecode Previous;
        internal bool NodesSeen;
        internal GameInventory Target;
        internal string SavedJson;
        internal bool DecoderFinished;
        internal bool PendingCompanyImpact;
        internal readonly List<(string Reason, string ItemId, bool CompanyAffected)> PendingFailures = new();
        internal List<SaveItemNode> Nodes;
        internal HashSet<string> SavedIds;
        internal HashSet<long> CompanySubtrees;
        internal List<(GameItem Item, string Id, TagSystem State, TagSystem ModifiedState)> Placeholders;

        internal InventoryDecode(LoadAttempt attempt, int index, InventoryDecode previous)
        { Attempt = attempt; Index = index; Previous = previous; }
    }

    private static bool IsCurrentInventory(InventoryDecode decode) => decode != null
        && ReferenceEquals(_activeLoad, decode.Attempt) && decode.Attempt.IsCurrent;

    private static void RecordInventoryFailure(InventoryDecode decode, string reason, string itemId = null,
        bool companyAffected = false, bool observationOnly = false)
    {
        var attempt = decode.Attempt;
        companyAffected = !observationOnly && (companyAffected || ItemIdentity.Find(itemId, null) != null);
        if (!decode.DecoderFinished)
        {
            bool firstCompanyImpact = companyAffected && !decode.PendingCompanyImpact;
            decode.PendingCompanyImpact |= companyAffected;
            if (decode.PendingFailures.Count == InventoryFailureDetailLimit)
            {
                attempt.OmittedInventoryFailures++;
                if (!firstCompanyImpact) return;
                decode.PendingFailures.RemoveAt(decode.PendingFailures.Count - 1);
            }
            decode.PendingFailures.Add((reason, itemId, companyAffected));
            return;
        }
        bool pauseNow = !attempt.CompanyItemsAffected && companyAffected;
        attempt.CompanyItemsAffected |= pauseNow;
        if (attempt.InventoryFailures.Count == InventoryFailureDetailLimit)
        {
            attempt.OmittedInventoryFailures++;
            if (!pauseNow) return;
            attempt.InventoryFailures.RemoveAt(InventoryFailureDetailLimit - 1);
        }
        string name = decode.Index < 0 ? "未知库存" : InventoryNames[decode.Index];
        attempt.InventoryFailures.Add(name + (itemId == null ? "" : " · 物品=" + SafeSavedId(itemId)) + " · " + reason
            + (companyAffected ? " · 涉及公司物品" : ""));
        if (pauseNow)
            StoreSession.RecordInventoryLoad(FormatInventoryLoad(attempt, null), true);
    }

    private static string SafeSavedId(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 128) return "未知";
        foreach (char c in id)
            if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_' || c == '-' || c == '.'))
                return "未知";
        return id;
    }

    private static void OnCompanyDecodeWarning(string id, string reason, Exception error)
    {
        var decode = _inventoryDecode;
        if (IsCurrentInventory(decode) && !decode.NodesSeen)
            RecordInventoryFailure(decode, "公司物品工厂不可用：" + reason, id);
    }

    private static bool TryAllowLoadPlaceholder(string id, string reason, Exception error)
    {
        var decode = _inventoryDecode;
        if (!IsCurrentInventory(decode) || decode.SavedIds?.Contains(id) != true) return false;
        string detail = error == null ? reason : reason + " · " + error.GetType().Name;
        RecordInventoryFailure(decode, "公司物品创建失败，交由原版加载占位物品：" + detail, id);
        return true;
    }

    // Run after ItemRegistry's ID migration. Only the primary graph of each saved inventory gets fallback handling.
    [HarmonyPriority(Priority.Last)]
    private static void BeforeInventoryNodes(List<SaveItemNode> __0, out InventoryDecode __state)
    {
        __state = null;
        var decode = _inventoryDecode;
        if (__0 == null || !IsCurrentInventory(decode) || decode.NodesSeen) return;
        decode.NodesSeen = true;
        decode.Nodes = __0;
        __state = decode;
        try
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in __0)
                if (node?.identifier != null) ids.Add(node.identifier);
            decode.SavedIds = ids;
        }
        catch (Exception error)
        {
            RecordInventoryFailure(decode, "存档物品核对失败 · " + error.GetType().Name);
        }
    }

    [HarmonyPriority(Priority.First)]
    private static void AfterInventoryNodes(List<SaveItemNode> __0, Exception __exception, bool __runOriginal, InventoryDecode __state)
    {
        if (__state == null) return;
        __state.SavedIds = null;
        if (!IsCurrentInventory(__state)) return;
        if (__exception != null || !__runOriginal) return;
        if (__state.Placeholders != null)
            foreach (var placeholder in __state.Placeholders) PrepareSavedPlaceholder(__state, placeholder);
        try { CheckRestoredChildSlots(__state, __0); }
        catch (Exception error)
        {
            RecordInventoryFailure(__state, "容器子槽核对失败 · " + error.GetType().Name);
        }
    }

    private static void AfterCreateSavedPlaceholder(string __0, GameItem __result)
    {
        var decode = _inventoryDecode;
        if (__result == null || !IsCurrentInventory(decode) || decode.SavedIds?.Contains(__0) != true
            || ItemIdentity.Find(__0, null) == null) return;
        try
        {
            decode.Placeholders ??= new();
            decode.Placeholders.Add((__result, __0, __result.state.Clone(), __result.modifiedState.Clone()));
        }
        catch (Exception error)
        {
            RecordInventoryFailure(decode, "占位物品初始状态读取失败 · " + error.GetType().Name, __0);
        }
    }

    private static void PrepareSavedPlaceholder(InventoryDecode decode,
        (GameItem Item, string Id, TagSystem State, TagSystem ModifiedState) placeholder)
    {
        var (item, id, state, modifiedState) = placeholder;
        try
        {
            // Saved machine tags can activate native load handlers on a dummy without machine slots.
            bool owned = item.IsTag("IS_OWNED_TAG");
            item.state = state;
            item.modifiedState = modifiedState;
            if (owned) item.EnableTag("IS_OWNED_TAG");
        }
        catch (Exception error)
        {
            RecordInventoryFailure(decode, "占位物品初始化失败 · " + error.GetType().Name, id);
        }
    }

    private static void CheckRestoredChildSlots(InventoryDecode decode, List<SaveItemNode> nodes)
    {
        var byId = new Dictionary<long, SaveItemNode>(nodes.Count);
        foreach (var node in nodes)
            if (node != null) byId[node.uuid] = node;
        foreach (var parent in nodes)
        {
            if (parent?.childItems == null || parent.childItems.Count == 0) continue;
            var slots = EnumerateSavedInventorySlots(parent.tempLink);
            for (int i = 0; i < parent.childItems.Count; i++)
            {
                byId.TryGetValue(parent.childItems[i], out var child);
                int slot = parent.childItemInventoryNode != null && i < parent.childItemInventoryNode.Count
                    ? parent.childItemInventoryNode[i] : -1;
                string location = " · 容器=" + SafeSavedId(parent.identifier) + " · 槽位=" + slot;
                if (slot < 0 || slot >= slots.Count || slots[slot] is not GameInventory target)
                    RecordInventoryFailure(decode, "容器子槽缺失" + location, child?.identifier ?? "未知",
                        IsSavedCompanyItem(parent) || CompanySubtreeContains(decode, child));
                else if (child?.tempLink == null || !ReferenceEquals(child.tempLink.parentInventory, target))
                    RecordInventoryFailure(decode, "容器子槽未接收物品" + location, child?.identifier ?? "未知",
                        CompanySubtreeContains(decode, child));
            }
        }
    }

    private static bool IsSavedCompanyItem(SaveItemNode node) =>
        node != null && ItemIdentity.Find(node.identifier, node.spritePath) != null;

    private static bool CompanySubtreeContains(InventoryDecode decode, SaveItemNode node)
    {
        if (node == null) return false;
        decode.CompanySubtrees ??= FindCompanySubtrees(decode.Nodes);
        return decode.CompanySubtrees.Contains(node.uuid);
    }

    // A missing outer container can strand company items even when their immediate inner slot decoded successfully.
    private static HashSet<long> FindCompanySubtrees(List<SaveItemNode> nodes)
    {
        var companySubtrees = new HashSet<long>();
        var parents = new Dictionary<long, List<long>>();
        var pending = new Queue<long>();
        foreach (var node in nodes)
        {
            if (node == null) continue;
            if (IsSavedCompanyItem(node) && companySubtrees.Add(node.uuid))
                pending.Enqueue(node.uuid);
            if (node.childItems == null) continue;
            foreach (long child in node.childItems)
            {
                if (!parents.TryGetValue(child, out var owners)) parents.Add(child, owners = new());
                owners.Add(node.uuid);
            }
        }
        while (pending.Count != 0)
        {
            if (!parents.TryGetValue(pending.Dequeue(), out var owners)) continue;
            foreach (long parent in owners)
                if (companySubtrees.Add(parent)) pending.Enqueue(parent);
        }
        return companySubtrees;
    }

    private static void CheckReplacedInventories(LoadAttempt attempt, bool loadCompleted)
    {
        foreach (var decode in attempt.ReplacedInventories)
        {
            const string skipped = "原版解码被跳过";
            if (!loadCompleted)
            {
                RecordInventoryFailure(decode, skipped + "，读档未完成，未核验公司物品");
                continue;
            }
            List<SaveItemNode> saved;
            Dictionary<(string Id, int UniqueId), long> actual = new();
            try
            {
                // Use the original input: a replacement may bypass DecodeNodes or leave its links detached.
                saved = UnityEngine.JsonUtility.FromJson<SaveState>(decode.SavedJson)?.saveItems;
                if (saved == null || decode.Target == null || decode.Index < 0)
                {
                    RecordInventoryFailure(decode, skipped + "，无法核验公司物品");
                    continue;
                }
                var pending = new Stack<GraphNodeStorage>();
                var seen = new HashSet<GraphNodeStorage>();
                pending.Push(decode.Target);
                while (pending.Count != 0)
                {
                    var node = pending.Pop();
                    if (node == null || !seen.Add(node)) continue;
                    if (node is GameItem item)
                    {
                        if (StorePlacement.Destroyed(item) || item.parentInventory == null) continue;
                        string id = ItemIdentity.ResolveId(item.identifier, item.spritePath);
                        if (id != null && item.spritePath != "unknown" && item.unitCount > 0)
                        {
                            var key = (id, item.uniqueId);
                            actual.TryGetValue(key, out long units);
                            actual[key] = units + item.unitCount;
                        }
                    }
                    foreach (var child in node.children) pending.Push(child);
                }
            }
            catch (Exception)
            {
                RecordInventoryFailure(decode, skipped + "，无法核验公司物品");
                continue;
            }

            bool missing = false;
            foreach (var node in saved)
            {
                if (!IsSavedCompanyItem(node) || node.unitCount <= 0) continue;
                var key = (ItemIdentity.ResolveId(node.identifier, node.spritePath), node.uniqueId);
                actual.TryGetValue(key, out long units);
                actual[key] = units - node.unitCount;
                if (units >= node.unitCount) continue;
                missing = true;
                RecordInventoryFailure(decode, skipped + "，替代解码后未在目标库存找到足量公司物品", key.Item1,
                    companyAffected: true);
            }
            if (missing) continue;
            attempt.RestoredInventories.Add(decode.Index);
            attempt.VerifiedReplacementCount++;
            RecordInventoryFailure(decode, skipped + "，替代解码后已核验公司物品");
        }
    }

    private static bool HasUnrestoredCompanyItems(InventoryDecode decode)
    {
        if (decode.Nodes == null) return false;
        foreach (var node in decode.Nodes)
        {
            if (node == null) continue;
            var item = node.tempLink;
            if ((item == null || StorePlacement.Destroyed(item) || item.parentInventory == null)
                && CompanySubtreeContains(decode, node)) return true;
        }
        return false;
    }

    // The native decoder indexes non-item descendants in breadth-first order.
    private static List<GraphNodeStorage> EnumerateSavedInventorySlots(GraphNodeStorage parent)
    {
        var slots = new List<GraphNodeStorage>();
        if (parent?.children == null) return slots;
        var pending = new Queue<GraphNodeStorage>(parent.children);
        var seen = new HashSet<GraphNodeStorage>();
        while (pending.Count != 0)
        {
            var node = pending.Dequeue();
            if (node == null || node is GameItem || !seen.Add(node)) continue;
            slots.Add(node);
            foreach (var child in node.children) pending.Enqueue(child);
        }
        return slots;
    }

    private static void BeforeNativePlacementCleanup(GameItem __0, string __1,
        out (InventoryDecode Decode, GameItem Item, string ItemId, bool CompanyItem) __state)
    {
        __state = default;
        var decode = _inventoryDecode;
        if (__1 != NativeLoadPlacement || !IsCurrentInventory(decode) || __0 == null || __0.unitCount <= 0) return;
        __state = (decode, __0, __0.identifier ?? "未知", ItemIdentity.Find(__0.identifier, __0.spritePath) != null);
    }

    private static void AfterNativePlacementCleanup(bool __runOriginal, bool __result,
        (InventoryDecode Decode, GameItem Item, string ItemId, bool CompanyItem) __state)
    {
        var decode = __state.Decode;
        if (!__runOriginal || __result || !IsCurrentInventory(decode)) return;
        bool companyAffected = __state.CompanyItem;
        if (!companyAffected && decode.Nodes != null)
        {
            var node = decode.Nodes.Find(saved => saved != null && ReferenceEquals(saved.tempLink, __state.Item));
            companyAffected = CompanySubtreeContains(decode, node);
        }
        RecordInventoryFailure(decode, "原版目标库存拒收物品", __state.ItemId, companyAffected);
    }
}
