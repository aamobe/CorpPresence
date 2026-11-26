using System.Text.Json;
using CorpPresence;

partial class Program
{
    static int checks;
    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        checks++;
    }

    static void Reset()
    {
        StoreSession.Revision++;
        StoreSession.CompanyProgressPaused = false;
        CorpState.Current = new();
        EmporiumEntry.Instance = new();
        SaveSlot.Writable = true;
        SaveSlot.CurrentStore = new();
        SaveSlot.CurrentStore.Pay = amount => SaveSlot.CurrentStore.Cash += amount;
        LowerMachinist.Starter = LowerMachinist.Advanced = true;
        CartelShizuka.Visited = CartelShizuka.HasBlueprint = true;
        StoreStation.Day = 1;
    }
    static GameItem Stock(string id, int count, params int[] parts)
    {
        var item = new GameItem { identifier = id, unitCount = count, Parts = parts.Length > 0 ? parts : null };
        EmporiumEntry.Instance.Owned.Add(item);
        return item;
    }
    static WorkshopSaleQuote Quote(string id) => new(WorkshopSaleOffer.Find(id));
    static WorkshopSaleState Sales => CorpState.Current.Workshop.Sales;
    static int Cash => SaveSlot.CurrentStore.Cash;
    static void Reload()
    {
        var json = JsonSerializer.Serialize(CorpState.Current.Workshop);
        StoreSession.Revision++;
        CorpState.Current = new();
        CorpState.Current.Workshop = JsonSerializer.Deserialize<WorkshopState>(json);
        Check(CorpState.Current.Workshop.Valid(), "serialized workshop and receipts validate");
    }

    static void Main()
    {
        CheckRodSales();
        foreach (var attachment in new[] { "source", "other", "dangling graph edge" })
        {
            Reset();
            var interruptedTile = Stock("corp_presence_hearth_tile", 1);
            var original = interruptedTile.parentInventory;
            original.Remove = item =>
            {
                item.Destroy();
                if (attachment == "other") item.parentInventory = new();
                if (attachment == "dangling graph edge")
                {
                    item.parentInventory = null;
                    original.childItems.Add(item);
                }
                throw new InvalidOperationException("native callback interrupted detach");
            };
            Check(Quote("hearth_tile").Submit() == SaleStatus.Frozen && Sales.Product.InFlight && Sales.Product.Received == 0
                && Cash == 0, "destroyed flag cannot settle attached stock: " + attachment);
            Reload();
            Check(Quote("hearth_tile").Submit() == SaleStatus.Frozen && Cash == 0, "unresolved removal stays frozen on reload");
        }
        Reset();
        Stock("corp_presence_pump_contact",4);
        LowerMachinist.Advanced=false;
        Check(Quote("pump_contact").Check()==SaleStatus.Unavailable,"repair desk requires Cen's second sale");
        LowerMachinist.Advanced=true;
        Check(Quote("pump_contact").Submit()==SaleStatus.Paid && Cash==76,"two pump contacts sell for 76");
        Reload();
        Stock("wire",2);
        Check(Quote("pump_contact").Submit()==SaleStatus.DailyLimit && Quote("wire").Submit()==SaleStatus.Paid,
            "repair desk has its own daily quota after reload");
        CheckFirstOverhaul();
        CheckRotations();
        CheckSalvageBasketRoute();
        CheckShizukaSales();
        Reset();
        var glass = Stock("box", 1, 0, 85, 0, 0, 15);
        var wire = Stock("wire", 3);
        var quote = Quote("wire");
        Check(quote.Submit() == SaleStatus.Paid && wire.unitCount == 1 && Cash == 64, "wire stack and payment");
        Check(quote.Submit() != SaleStatus.Paid && Quote("hearth_tile").Submit() == SaleStatus.DailyLimit, "shared product limit");
        quote = Quote("silica");
        Check(quote.Amount == 18 && quote.RemainingParts().SequenceEqual(new[] { 0,25,0,0,15 }), "powder preview");
        Check(quote.Submit() == SaleStatus.Paid && !glass.Dead && glass.Parts.SequenceEqual(new[] { 0,25,0,0,15 }) && Cash == 82, "independent powder order only deducts silica");
        Reload();
        Check(Quote("metal").Submit() == SaleStatus.DailyLimit && Quote("wire").Submit() == SaleStatus.DailyLimit, "same day survives reload");
        StoreStation.Day++;
        Check(Quote("wire").Submit() == SaleStatus.Short, "next day can quote again");

        Reset();
        Stock("box", 1, 80,19,0,0,0);  // 80.808...%, displayed rounding must not qualify.
        Stock("box", 1, 35,0,0,0,0);
        Stock("box", 1, 35,0,0,0,0);
        Stock("box", 1, 99,1,0,0,0).Bound = true;
        Stock("box", 1, 501,0,0,0,0);
        Check(Quote("metal").Check() == SaleStatus.Short, "no averaging or internal-bin proxy");
        Stock("box", 1, 81,19,0,0,0);
        var pure = Stock("box", 1, 99,1,0,0,0);
        quote = Quote("metal");
        Check(quote.Stock.Count == 2 && quote.Amount == 22, "81 percent base rate");
        quote.Sources[0] = 1;
        Check(quote.Amount == 27 && quote.Submit() == SaleStatus.Paid && pure.Parts[0] == 39 && Cash == 27, "99 percent premium rounds down");
        foreach (var pair in new[] { ("rare", 2, 10, 20), ("conductive", 3, 20, 20), ("residue", 4, 40, 15) })
        {
            StoreStation.Day++;
            var parts = new int[5]; parts[pair.Item2] = pair.Item3;
            var box = Stock("box", 1, parts);
            quote = Quote(pair.Item1);
            Check(quote.Amount == pair.Item4 && quote.Submit() == SaleStatus.Paid && box.Parts.Sum() == 0 && !box.Dead, "pure " + pair.Item1);
        }

        Reset();
        var first = Stock("wire", 1);
        EmporiumEntry.Instance.Owned.Add(first);
        Check(Quote("wire").Check() == SaleStatus.Short, "duplicate stock references");
        var second = Stock("wire", 1);
        second.parentInventory.Remove = _ => throw new Exception("before removal");
        Check(Quote("wire").Submit() == SaleStatus.Partial && first.Dead && !second.Dead && Sales.Product.Received == 1 && Cash == 0, "partial receipt");
        Reload();
        StoreStation.Day++;
        Check(Quote("silicon_plate").Check() == SaleStatus.Outstanding, "pending first delivery retained");
        second.parentInventory.Remove = item => { item.parentInventory = null; throw new Exception("after removal"); };
        SaveSlot.CurrentStore.Pay = amount => { SaveSlot.CurrentStore.Cash += 17; throw new Exception("partial native credit"); };
        Check(Quote("wire").Submit() == SaleStatus.Partial && second.Dead && Sales.Product.Paid == 17 && Cash == 17, "late expel and partial credit");
        Reload();
        SaveSlot.CurrentStore.Pay = amount => { SaveSlot.CurrentStore.Cash += amount; throw new Exception("after credit"); };
        Check(Quote("wire").Submit() == SaleStatus.Paid && Cash == 64 && Sales.Product.Day == 1, "resume payment exactly once");
        Reload();
        Check(Sales.Product.CompletedDay == 2 && Quote("silicon_plate").Check() == SaleStatus.DailyLimit, "cross-day settlement uses payment day's quota");
        Stock("box", 1, 60,0,0,0,0);
        Check(Quote("metal").Submit() == SaleStatus.Paid && Cash == 91, "cross-day product payment leaves powder quota");
        StoreStation.Day++;
        Check(Quote("wire").Check() == SaleStatus.Short, "day after settlement reopens quota");

        Reset();
        var reserved = Stock("wire", 3);
        reserved.Limit = 1;
        Check(Quote("wire").Check() == SaleStatus.Short, "protected stack minimum");
        var loose = Stock("wire", 1);
        quote = Quote("wire");
        loose.parentInventory.Locked = true;
        Check(quote.Submit() == SaleStatus.Changed && Cash == 0 && reserved.unitCount == 3, "one source locked after preview");
        Check(Quote("wire").Check() == SaleStatus.Short, "locked stock excluded from preview");
        loose.parentInventory.Locked = false;
        loose.parentInventory.Owned = false;
        Check(Quote("wire").Check() == SaleStatus.Short, "non-owned container excluded");
        loose.parentInventory.Owned = true;
        Check(Quote("wire").Submit() == SaleStatus.Paid && reserved.unitCount == 2 && loose.Dead, "uses only allowed portion");
        StoreStation.Day++;
        var lockedBox = Stock("box", 1, 60,0,0,0,0);
        quote = Quote("metal");
        lockedBox.Limit = 0;
        Check(quote.Submit() == SaleStatus.Changed && lockedBox.Parts[0] == 60, "powder permissions rechecked");

        Reset();
        var tile = Stock("corp_presence_hearth_tile", 1);
        quote = Quote("hearth_tile");
        SaveSlot.CurrentStore.Cash = int.MaxValue - 23;
        Check(quote.Submit() == SaleStatus.WalletFull && !tile.Dead && Sales.Product == null, "wallet capacity");
        SaveSlot.CurrentStore.Cash = 0;
        tile.parentInventory = new();
        Check(quote.Submit() == SaleStatus.Changed && !tile.Dead, "moved preview");
        quote = Quote("hearth_tile"); StoreSession.Revision++;
        Check(quote.Submit() == SaleStatus.Unavailable && !tile.Dead, "stale store");
        quote = Quote("hearth_tile"); StoreStation.Day++;
        Check(quote.Submit() == SaleStatus.Unavailable && !tile.Dead, "stale day");
        SaveSlot.Writable = false;
        Check(Quote("hearth_tile").Submit() == SaleStatus.Unavailable && !tile.Dead, "protected slot");
        SaveSlot.Writable = true;
        LowerMachinist.Starter = LowerMachinist.Advanced = false;
        Check(Quote("hearth_tile").Submit() == SaleStatus.Unavailable && Quote("metal").Check() == SaleStatus.Unavailable, "story gates");
        LowerMachinist.Starter = true;
        Check(Quote("hearth_tile").Check() == SaleStatus.Ready && Quote("metal").Check() == SaleStatus.Unavailable, "first visit unlocks products only");
        string interruptedSnapshot = null;
        string normalSave = null;
        SaveSlot.CurrentStore.Pay = amount => {
            SaveSlot.CurrentStore.Cash += amount;
            interruptedSnapshot = JsonSerializer.Serialize(Sales);
            Check(CompanyTransaction.DeferSave(SaveSlot.CurrentStore, () => normalSave = JsonSerializer.Serialize(Sales))
                && normalSave == null, "native save deferred during settlement");
            Check(Quote("hearth_tile").Submit() == SaleStatus.Unavailable, "nested settlement rejected");
        };
        Check(Quote("hearth_tile").Submit() == SaleStatus.Paid && Cash == 24, "normal completion after snapshot");
        Check(JsonSerializer.Deserialize<WorkshopSaleState>(normalSave).Product.Complete, "deferred native save sees the completed receipt");
        CorpState.Current.Workshop.Sales = JsonSerializer.Deserialize<WorkshopSaleState>(interruptedSnapshot);
        Check(Sales.Valid() && Quote("hearth_tile").Submit() == SaleStatus.Frozen && Cash == 24, "ambiguous snapshot never recredits");
        Sales.Product.Amount = 900;
        Check(!Sales.Valid(), "corrupt receipt rejects load");
        Console.WriteLine($"WorkshopSaleChecks passed ({checks} assertions).");
    }

    static void CheckRodSales()
    {
        Reset();
        var first = Stock("corp_presence_trial_alloy_rod", 1);
        var chosen = Stock("corp_presence_trial_alloy_rod", 1);
        var unknown = Stock("corp_presence_trial_alloy_rod", 1);
        var offer = WorkshopSaleOffer.FatigueRod;
        new RodMaterial(65, 20, 5, 8, 2).Write(first);
        new RodMaterial(64, 18, 8, 8, 2).Write(chosen);
        RodCondition.Evaluate(RodMaterial.Read(first), RodProcess.Temper).WriteProcess(first);
        RodCondition.Evaluate(RodMaterial.Read(chosen), RodProcess.Temper).WriteProcess(chosen);
        Check(new WorkshopSaleQuote(offer, first).Amount == 50, "Fatigue base quote");
        Check(new WorkshopSaleQuote(offer, unknown).Check() == SaleStatus.Short, "Unknown rod record cannot enter a spec order");
        Check(new WorkshopSaleQuote(offer).Check() == SaleStatus.Short, "Unselected rods are never taken from stock");
        var stale = new WorkshopSaleQuote(offer, chosen);
        new RodMaterial(65, 20, 5, 8, 2).Write(chosen);
        Check(stale.Submit() == SaleStatus.Changed && !chosen.Dead && Cash == 0, "Spec price and material are locked to the inspected rod");
        new RodMaterial(64, 18, 8, 8, 2).Write(chosen);
        SaveSlot.CurrentStore.Pay = _ => SaveSlot.CurrentStore.Cash += 19;
        var quote = new WorkshopSaleQuote(offer, chosen);
        Check(quote.Amount == 54 && quote.Submit() == SaleStatus.Partial
            && !first.Dead && chosen.Dead && Cash == 19 && Sales.Rods.Received == 1, "Premium consumes only the selected physical rod");
        Reload();
        Check(new WorkshopSaleQuote(WorkshopSaleOffer.HardRod, first).Check() == SaleStatus.Outstanding, "Unpaid rod receipt has priority");
        SaveSlot.CurrentStore.Pay = due => SaveSlot.CurrentStore.Cash += due;
        Check(new WorkshopSaleQuote(offer).Submit() == SaleStatus.Paid && Cash == 54 && !first.Dead, "Payment resumes without any rod");
        Reload();
        Check(new WorkshopSaleQuote(offer, first).Check() == SaleStatus.DailyLimit && Cash == 54, "Same-day reload cannot pay twice");
        Stock("wire", 2);
        Check(Quote("wire").Submit() == SaleStatus.Paid && Cash == 118, "Rod quota is independent of workshop products");
        StoreStation.Day++;
        var hard = Stock("corp_presence_trial_alloy_rod", 1);
        new RodMaterial(60, 27, 7, 4, 2).Write(hard);
        Check(new WorkshopSaleQuote(WorkshopSaleOffer.HardRod, hard).Check() == SaleStatus.Ready,
            "The high hardness procurement tests hardness and not the tooth-face trait");
        var shaft = Stock("corp_presence_feed_shaft", 1);
        new RodMaterial(30,8,4,3,0).Write(shaft);
        var shaftAssay = new RodMaterial(64,18,8,8,2);
        shaftAssay.Write(shaft, RodMaterial.AssayTag);
        var shaftOrder = WorkshopSaleOffer.Find("part_feed_shaft");
        Check(new WorkshopSaleQuote(shaftOrder, shaft).Check() == SaleStatus.Short, "A regular shaft does not pass the fatigue order");
        RodCondition.Evaluate(shaftAssay, RodProcess.Temper).WriteProcess(shaft);
        Check(new WorkshopSaleQuote(WorkshopSaleOffer.BatchRod, shaft).Check() == SaleStatus.Short, "A finished shaft is not wholesale bar stock");
        Check(new WorkshopSaleQuote(shaftOrder, shaft).Submit() == SaleStatus.Paid
            && Cash == 186 && shaft.Dead && !hard.Dead, "The fatigue shaft pays 68 without taking the inspected loose bar");
        Reload();
        Check(new WorkshopSaleQuote(WorkshopSaleOffer.HardRod, hard).Check() == SaleStatus.DailyLimit,
            "Parts and bars share the new daily category after reload");
    }
}
