using System.Text.Json;

namespace CorpPresence;

internal static class ShizukaSaleCheck
{
    internal static void Run(GameItem reactor)
    {
        var store = EmporiumEntry.Instance;
        var output = reactor.Slot("blue_output");
        var gel = output.childItems.Single();
        output.ParentItem = reactor;
        store.ListedOutput = output;
        const string station = "@" + AlcoholPreparationStation.Key;
        if (ShizukaSupplies.OrderError(station, CorpState.Current) == null ||
            new WorkshopSaleQuote(WorkshopSaleOffer.FirstGel).Check() != SaleStatus.Short)
            throw new Exception("Trial must reject virtual output and keep the alcohol station locked.");
        store.frontInvinvElement.Put(reactor);
        var quote = new WorkshopSaleQuote(WorkshopSaleOffer.FirstGel);
        if (!ReferenceEquals(quote.Selected(0)?.Item, gel) || quote.Submit() != SaleStatus.Paid ||
            SaveSlot.CurrentStore.GetCash() != 55 || output.childItems.Count != 0 || gel.unitCount != 0)
            throw new Exception("The trial must collect the actual overnight gel from the reactor and credit 55.");
        var saved = JsonSerializer.Serialize(CorpState.Current.Workshop);
        CorpState.Current.Workshop = JsonSerializer.Deserialize<WorkshopState>(saved);
        StoreStation.Day++;
        if (!CorpState.Current.Workshop.Valid() || ShizukaSupplies.OrderError(station, CorpState.Current) != null ||
            ShizukaSupplies.All.Single(entry => entry.Id == station).FixedListValue != 300 ||
            new WorkshopSaleQuote(WorkshopSaleOffer.FirstGel).Submit() != SaleStatus.TrialSettled ||
            SaveSlot.CurrentStore.GetCash() != 55)
            throw new Exception("Reload must retain the 300-credit station unlock and never pay the trial again.");
        Console.WriteLine("PASS actual blue vessel gel sold from its output for 55; 300-credit station unlocked after reload.");
    }
}

internal partial class CorpState
{
    internal static CorpState Current { get; } = new();
    internal string RunId = "cartel-check-run";
    internal WorkshopState Workshop = new();
}
internal static class CartelShizuka
{
    internal static bool Delivered;
    internal static bool BlueprintDelivered(CorpState s) => Delivered;
    internal static bool Introduced(CorpState s) => true;
}
internal static class LowerMachinist
{
    internal static bool StarterUnlocked(CorpState s) => true;
    internal static bool SecondVisit = true;
    internal static bool MineralizerUnlocked(CorpState s) => SecondVisit;
}
internal static class StoreStation { internal static int Day = 1; internal static int GetDayCounter() => Day; }
internal sealed class PlayerStore
{
    internal enum StoreState { OPEN, MORNING }
    internal StoreState storeState => StoreState.OPEN;
    private int _cash;
    internal int? PayoutLimit;
    internal int? DebitLimit;
    internal int GetCash() => _cash;
    internal void ModCash(int amount, bool playSound)
    {
        if (amount < 0 && DebitLimit is int debit) amount = Math.Max(amount, -debit);
        if (amount > 0 && PayoutLimit is int limit) amount = Math.Min(amount, limit);
        _cash += amount;
    }
}
internal sealed class EmporiumEntry
{
    internal static EmporiumEntry Instance { get; } = new();
    internal GameInventory ListedOutput;
    internal GameInventory frontInvinvElement = new();
    internal GameInventory invElement => null;
    internal GameInventory backInvinvElement = new();
    internal GameInventory backInvinvElementCounter => null;
    internal GameInventory docInvElement => null;
    internal GameInventory showcaseElement => null;
    internal GameInventory trashInvElement => null;
    internal GameInventory vendingFountainElement => null;
    internal GameInventory vendingMachineElement => null;
    internal GameInventory hiddenElement => null;
    internal List<GameItem> GetAllOwnedItems(bool includeAfterhour) => ListedOutput.childItems;
}
internal static class SaleInventoryBoundary
{
    private static int _lastId;
    internal static int GetUniqueID(this GameItem item)
    {
        if (item.uniqueId == 0) item.uniqueId = ++_lastId;
        return item.uniqueId;
    }
    internal static GameItem GetParentItem(this GameInventory inventory) => inventory.ParentItem;
    internal static bool MayRemove(this GameItem item) => item.parentInventory != null
        && item.parentInventory.mayInventoryRemoveItemFunc?.Invoke(item, item.parentInventory) != false;
    internal static int MaxNumRemove(this GameItem item) => item.unitCount; // Native count and permission are separate checks.
    internal static void Expel(this GameInventory inventory, GameItem item)
    {
        if (item.parentInventory != inventory || !inventory.childItems.Contains(item)) return;
        item.onSlotRemoveItemFunc?.Invoke(item, inventory, null);
        inventory.onSlotRemoveItemFunc?.Invoke(item, inventory, null);
        if (inventory.childItems.Remove(item)) item.parentInventory = null;
    }
}
internal static class StorePlacement { internal static bool Destroyed(GameItem item) => item.unitCount == 0; }
internal sealed class CatalogEntry
{
    internal string Id;
    internal int MinTier, FixedBaseValue, FixedListValue;
    internal bool IgnoreOrderRate, NoQuantityQuota;
    internal static GameItem MakeDefault(string id) => Goods.Make(id.TrimStart('@'));
}
