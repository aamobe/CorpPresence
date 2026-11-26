using System.Text.Json;
using CorpPresence;

namespace CorpPresence
{
    internal static class IndustrialGoods
    {
        internal const string Frame = "pump_frame", Contact = "pump_contact", Unit = "filter_unit";
        internal static bool TryReadOrigin(GameItem item, out string run, out int serial, out int slot)
        {
            run = item.OriginRun;
            serial = item.OriginSerial;
            slot = item.OriginSlot;
            return run != null;
        }

        internal static bool IsFreshUnit(GameItem item) => item.Fresh;
    }
    internal static class MeridianIndustrial
    {
        internal static bool Forge;
        internal static bool Eligible(CorpState root) => root != null && LowerMachinist.Advanced && Forge;
    }
}

partial class Program
{
    private const string Frame = "corp_presence_pump_frame", Contact = "corp_presence_pump_contact", Unit = "corp_presence_filter_unit";
    private static FirstOverhaulState Overhaul => CorpState.Current.Workshop.FirstOverhaul;

    private static void CheckFirstOverhaul()
    {
        Reset();
        MeridianIndustrial.Forge = true;
        Check(!FirstOverhaulQuote.Eligible(CorpState.Current) && new FirstOverhaulQuote().Sign() == SaleStatus.Unavailable,
            "actual blue trial required to sign");
        Stock("corp_presence_sleep_moss_gel", 1);
        Check(new WorkshopSaleQuote(WorkshopSaleOffer.FirstGel).Submit() == SaleStatus.Paid, "qualifying blue trial");
        CartelShizuka.HasBlueprint = false;
        Check(!FirstOverhaulQuote.Eligible(CorpState.Current), "salt blueprint required");
        CartelShizuka.HasBlueprint = true;
        MeridianIndustrial.Forge = false;
        Check(!FirstOverhaulQuote.Eligible(CorpState.Current), "industrial service required");
        MeridianIndustrial.Forge = true;
        StoreStation.Day = 11;
        var unsigned = new FirstOverhaulQuote();
        Check(unsigned.Sign() == SaleStatus.Ready && unsigned.Sign() == SaleStatus.Unavailable
            && Overhaul.SignedDay == 11 && Overhaul.Trucks[0].DueDay == 17 && Overhaul.Trucks[1].DueDay == 21, "one instance signs and locks both due dates");
        Check(new FirstOverhaulQuote().Abandon() == SaleStatus.Ready, "abandon before any collection");
        Reload();
        Check(Overhaul.Abandoned && new FirstOverhaulQuote().Sign() == SaleStatus.Ready && Overhaul.SignedDay == 11,
            "same instance resumes after reload");

        var untouched = Stock(Frame, 2);
        var chosen = Stock(Frame, 3);
        var contacts = Stock(Contact, 4);
        EmporiumEntry.Instance.Owned.Add(chosen);
        var loose = Stock(Frame, 1);
        loose.parentInventory.Detached = true;
        var changed = new FirstOverhaulQuote();
        Check(changed.Stock.Count == 3 && changed.Slots.Length == 6 && changed.Slots.Count(id => id == Contact) == 4,
            "deduplicated store items, no detached storage");
        int chosenIndex = changed.Stock.FindIndex(stock => ReferenceEquals(stock.Item, chosen));
        changed.Sources[0] = changed.Sources[1] = chosenIndex;
        chosen.parentInventory = new();
        Check(changed.Submit() == SaleStatus.Changed && untouched.unitCount == 2 && Overhaul.Trucks[0].StartedDay == null,
            "moved selection requires new preview without substituting another frame");
        SaveSlot.CurrentStore.Cash = int.MaxValue - 359;
        Check(new FirstOverhaulQuote().Submit() == SaleStatus.WalletFull && !contacts.Dead && untouched.unitCount == 2,
            "wallet cap blocks before any removal");
        SaveSlot.CurrentStore.Cash = 0;
        StoreStation.Day = 17;
        var quote = new FirstOverhaulQuote();
        int frame = quote.Stock.FindIndex(stock => ReferenceEquals(stock.Item, chosen));
        quote.Sources[0] = quote.Sources[1] = frame;
        contacts.parentInventory.Remove = _ => throw new Exception("before expulsion");
        Check(quote.Amount == 360 && quote.Submit() == SaleStatus.Partial && Overhaul.Trucks[0].Received.SequenceEqual(new[] { 2, 0 })
            && Overhaul.Trucks[0].SourceIds.Take(2).All(id => id == chosen.GetUniqueID()) && untouched.unitCount == 2
            && chosen.unitCount == 1 && Cash == 0, "selected stack only, D+6 bonus and partial collection");
        Check(new FirstOverhaulQuote().Abandon() == SaleStatus.Outstanding, "partial delivery cannot be abandoned");
        Reload();
        StoreStation.Day = 18;
        contacts.parentInventory.Remove = item => { item.parentInventory = null; throw new Exception("late"); };
        int nested = 0;
        string midTransfer = null, saved = null;
        SaveSlot.CurrentStore.Pay = n =>
        {
            SaveSlot.CurrentStore.Cash += 17;
            midTransfer = JsonSerializer.Serialize(CorpState.Current.Workshop);
            Check(CompanyTransaction.DeferSave(SaveSlot.CurrentStore,
                () => saved = JsonSerializer.Serialize(CorpState.Current.Workshop)) && saved == null, "coalesce native save");
            if (new FirstOverhaulQuote().Submit() == SaleStatus.Unavailable) nested++;
            throw new Exception("after partial wallet credit");
        };
        quote = new FirstOverhaulQuote();
        Check(quote.Amount == 360 && quote.Slots.Length == 4 && quote.Submit() == SaleStatus.Partial
            && nested == 1 && Overhaul.Trucks[0].Paid == 17 && contacts.Dead && Cash == 17, "frozen bonus and actual partial credit");
        CorpState.Current.Workshop = JsonSerializer.Deserialize<WorkshopState>(midTransfer);
        Check(Overhaul.Valid() && new FirstOverhaulQuote().Submit() == SaleStatus.Frozen && Cash == 17, "ambiguous wallet snapshot freezes");
        CorpState.Current.Workshop = JsonSerializer.Deserialize<WorkshopState>(saved);
        SaveSlot.CurrentStore.Pay = n => { SaveSlot.CurrentStore.Cash += n; throw new Exception("after full credit"); };
        quote = new FirstOverhaulQuote();
        Check(quote.Slots.Length == 0 && quote.Submit() == SaleStatus.Paid && Cash == 360
            && quote.Submit() != SaleStatus.Paid && Overhaul.Trucks[0].CompletedDay == 18, "pay outstanding 343 once after reload");
        Check(new FirstOverhaulQuote().CanAbandon && new FirstOverhaulQuote().Abandon() == SaleStatus.Ready, "abandon only remaining second truck");
        Reload();
        Check(new FirstOverhaulQuote().Sign() == SaleStatus.Ready && Overhaul.Trucks[0].Paid == 360, "resume only second truck");
        var usedUnit = Stock(Unit, 2);
        var freshUnit = Stock(Unit, 1);
        freshUnit.Fresh = true;
        var fresh2 = Stock(Unit, 1);
        fresh2.Fresh = true;
        StoreStation.Day = 21;
        quote = new FirstOverhaulQuote();
        Check(quote.Amount == 480 && quote.Stock.Count == 2 && quote.Slots.Length == 2, "second truck excludes unmarked filters");
        fresh2.Fresh = false;
        Check(quote.Submit() == SaleStatus.Changed && !freshUnit.Dead && new FirstOverhaulQuote().Check() == SaleStatus.Short,
            "freshness rechecked before removing either filter");
        fresh2.Fresh = true;
        Check(new FirstOverhaulQuote().Submit() == SaleStatus.Paid && Cash == 840 && Overhaul.CompletedDay == 21
            && !usedUnit.Dead && freshUnit.Dead && fresh2.Dead, "two new filters paid on inclusive D+10");
        Reload();
        Check(new FirstOverhaulQuote().Sign() == SaleStatus.Paid && new FirstOverhaulQuote().Submit() == SaleStatus.Paid
            && Cash == 840 && Overhaul.SignedDay == 11 && !new FirstOverhaulQuote().CanAbandon, "settled instance cannot reset");
        Overhaul.CompletedDay = 22;
        Check(!CorpState.Current.Workshop.Valid(), "mismatched completion rejected");
        Overhaul.CompletedDay = 21;
        Overhaul.Trucks[0].SourceIds[0] = 0;
        Check(!CorpState.Current.Workshop.Valid(), "corrupt selected source record rejected");

        CorpState.Current.Workshop.FirstOverhaul = null;
        StoreStation.Day = 0;
        Check(new FirstOverhaulQuote().Sign() == SaleStatus.Ready, "day zero supported");
        Stock(Contact, 4);
        StoreStation.Day = 7;
        var late = new FirstOverhaulQuote();
        StoreStation.Day++;
        Check(late.Submit() == SaleStatus.Unavailable, "stale day rejects quote");
        var staleStore = new FirstOverhaulQuote();
        StoreSession.Revision++;
        Check(staleStore.Submit() == SaleStatus.Unavailable, "stale store rejects quote");
        Check(new FirstOverhaulQuote().Amount == 320 && new FirstOverhaulQuote().Submit() == SaleStatus.Paid, "late first truck retains base");
        StoreStation.Day = 11;
        usedUnit.Fresh = true;
        Check(new FirstOverhaulQuote().Amount == 420 && new FirstOverhaulQuote().Submit() == SaleStatus.Paid, "late second truck retains base");
    }
}
