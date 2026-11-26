using System;

namespace CorpPresence;

internal static class WorkshopCounter
{
    // SaveGame serializes the shopkeeper's counter (backInvJSON).
    internal static bool HasRoom => EmporiumEntry.Instance?.backInvinvElement?.CanTakeAnything() == true;

    internal static bool CanFit(GameItem item) => CheckSlot(item, place: false);
    internal static bool TryPlaceOnCounter(GameItem item) => CheckSlot(item, place: true);

    internal static int DeliverCrate(int total, int remaining, Func<int, GameItem> make, Func<bool> current, string crateKey = "crate_spacer")
    {
        if (remaining <= 0 || remaining > total) return 0;
        GameItem crate = null;
        try
        {
            if (!current() || !HasRoom) return 0;
            crate = Goods.Make(crateKey);
            if (crate == null || !current() || !CanFit(crate)) return 0;
            int packed = 0;
            for (int index = total - remaining; index < total; index++)
            {
                if (!current()) return 0;
                var item = make(index);
                try
                {
                    if (item == null || !Crates.Put(crate, item)) break;
                    packed++;
                }
                finally { CompanyVisitors.DiscardUnshipped(item); }
            }
            return packed > 0 && current() && TryPlaceOnCounter(crate) ? packed : 0;
        }
        catch (Exception error)
        {
            ModLog.Warn("工坊领料中断，保留未交付清单：" + error.Message);
            return 0;
        }
        finally { CompanyVisitors.DiscardUnshipped(crate); }
    }

    private static bool CheckSlot(GameItem item, bool place)
    {
        var counter = EmporiumEntry.Instance?.backInvinvElement;
        if (counter == null || item == null) return false;
        GeneralHelper.SetItemOwned(item);
        return ProductionOutput.WithSeparateUnit(item, () =>
        {
            var slot = counter.TryFindOneValidInventorySlot(item);
            if (slot == null || slot.inventory != counter || slot.targetItem != null || slot.numTransfer != item.unitCount) return false;
            if (!place) return true;
            try { slot.TryAcceptOnce(); }
            catch (Exception error) { ModLog.Warn("工坊交货回调异常，按实际入柜结果登记：" + error.GetType().Name); }
            return ReferenceEquals(item.parentInventory, counter) && !StorePlacement.Destroyed(item);
        });
    }
}
