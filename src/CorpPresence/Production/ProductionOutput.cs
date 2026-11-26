using System;
using System.Collections.Generic;

namespace CorpPresence;

// Placement only. Callers own the products and must discard a failed batch
// before committing any inputs. A batch can have been partially attached.
internal static class ProductionOutput
{
    private static readonly Func<GameItem, GameInventory, bool> AllowProducedItem = (item, inventory) => true;

    // Sintering stages an identifiable unit until the night's powder and power are committed.
    // The stock grid consults both items' stacking rules; a null rule forces a free footprint.
    internal static bool WithSeparateUnit(GameItem item, Func<bool> operation)
    {
        var prior = item.mayStackItemFunc;
        try
        {
            item.mayStackItemFunc = null;
            return operation();
        }
        finally { item.mayStackItemFunc = prior; }
    }

    internal static bool TryAdd(GameInventory inventory, GameItem item)
    {
        if (inventory == null || item == null) return false;
        var previous = inventory.mayInventoryAddItemFunc;
        inventory.mayInventoryAddItemFunc = AllowProducedItem;
        try { return Transfer(inventory, item); }
        catch (Exception error) { ModLog.Warn("成品放置回调中断：" + error.Message); return false; }
        finally { inventory.mayInventoryAddItemFunc = previous; }
    }

    internal static bool TryAddBatch(GameInventory inventory, List<GameItem> items)
    {
        if (inventory == null || items == null) return false;
        var previous = inventory.mayInventoryAddItemFunc;
        inventory.mayInventoryAddItemFunc = AllowProducedItem;
        try
        {
            for (int i = 0; i < items.Count; i++)
                if (!Transfer(inventory, items[i])) return false;
            return true;
        }
        catch (Exception error) { ModLog.Warn("成品批次放置失败，交回生产线回收：" + error.Message); return false; }
        finally { inventory.mayInventoryAddItemFunc = previous; }
    }

    internal static bool TryAddSeparate(GameInventory inventory, GameItem item) =>
        WithSeparateUnit(item, () => TryAdd(inventory, item) || (inventory != null && ReferenceEquals(item.parentInventory, inventory)));

    // On failure the caller still owns every distinct product and can undo a partial placement.
    internal static bool TryAddSeparateBatch(GameInventory inventory, List<GameItem> items)
    {
        foreach (var item in items)
            if (!TryAddSeparate(inventory, item)) return false;
        return true;
    }

    internal static void DiscardBatch(List<GameItem> products)
    {
        foreach (var product in products)
        {
            try { product.Destroy(); }
            catch (Exception error) { ModLog.Warn("撤回未交齐的成品时回调中断：" + error.GetType().Name); }
        }
    }

    private static bool Transfer(GameInventory inventory, GameItem item)
    {
        if (item == null || !GraphUtils.CanAccept(inventory, item)) return false;
        GraphUtils.TryAcceptAll(inventory, item);
        return item.parentInventory == inventory;
    }
}
