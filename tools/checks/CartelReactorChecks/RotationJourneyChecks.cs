using System.Text.Json;

namespace CorpPresence;

internal sealed class RotationJourneyChecks
{
    private readonly GameInventory counter = EmporiumEntry.Instance.backInvinvElement;
    private readonly GameInventory stock = EmporiumEntry.Instance.frontInvinvElement;
    private readonly GameItem bench = CraftingStation.Definition.Shell();
    private readonly int firstPaid = CorpState.Current.Workshop.FirstOverhaul.CompletedDay.Value;

    private static void Check(bool ok, string detail)
    {
        if (!ok) throw new Exception("Rotation: " + detail);
    }

    private static GameItem Move(GameItem item, GameInventory destination)
    {
        item.parentInventory?.Expel(item);
        return destination.Put(item);
    }

    private static void ReloadLedger()
    {
        var workshop = CorpState.Current.Workshop;
        Check(workshop.Valid(), "ledger valid before save");
        CorpState.Current.Workshop = JsonSerializer.Deserialize<WorkshopState>(JsonSerializer.Serialize(workshop));
        Check(CorpState.Current.Workshop.Valid(), "ledger valid after reload");
    }

    private sealed record TagState(int Number, long Long, string Text);

    private static GameItem ReloadItem(GameItem previous)
    {
        // Recreate the boundary's native item from saved tag values; no recipe or receipt restamps it.
        var json = JsonSerializer.Serialize(previous.Tags.ToDictionary(pair => pair.Key,
            pair => new TagState(pair.Value.Number, pair.Value.Long, pair.Value.Text)));
        var inventory = previous.parentInventory;
        var restored = new GameItem(previous.identifier) { Owned = previous.Owned, unitCount = previous.unitCount };
        foreach (var (key, tag) in JsonSerializer.Deserialize<Dictionary<string, TagState>>(json))
            restored.ModifyTag(key, t => t.SetInt(tag.Number).SetLong(tag.Long).SetString(tag.Text));
        previous.Destroy();
        return inventory.Put(restored);
    }

    private void Choose(string recipe)
    {
        var buttons = ((object[])bench.contentWindow.childElement.GetPanel("control").Content).OfType<MachineryButton>().ToArray();
        for (int i = 0; i < 16 && bench.GetTagReadonly("CORP_CRAFT_MILL_RECIPE").GetString() != recipe; i++)
            buttons[0].Click();
        Check(bench.GetTagReadonly("CORP_CRAFT_MILL_RECIPE").GetString() == recipe, "learned " + recipe);
    }

    private GameItem[] Craft(string recipe, params GameItem[] ingredients)
    {
        Choose(recipe);
        var slots = (GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content;
        var output = bench.Slot("output");
        Check(output.childItems.Count == 0, "take every result before next batch");
        for (int i = 0; i < ingredients.Length; i++)
            Move(ingredients[i], i < slots.Length ? slots[i] : bench.Slot("extra"));
        var assemble = ((object[])bench.contentWindow.childElement.GetPanel("control").Content).OfType<MachineryButton>().Last();
        assemble.Click();
        var results = output.childItems.ToArray();
        Check(results.Length > 0, "bench produced " + recipe);
        foreach (var result in results) Move(result, stock);
        return results;
    }

    private GameItem Build(GameItem frame) => Craft(IndustrialGoods.Unit, frame, Goods.Make(IndustrialGoods.Blank),
        Goods.Make(IndustrialGoods.Liner), Goods.Make(CartelReactor.Gel)).Single();

    private void Stock(string key, int count)
    {
        var item = stock.Put(Goods.Make(key));
        item.SetUnitCount(count);
    }

    private RotationContract Sign(int offset, RotationKind kind)
    {
        StoreStation.Day = firstPaid + offset;
        var offer = new RotationQuote();
        Check(offer.Kind == kind && offer.Contract == null && offer.Sign() == SaleStatus.Ready, "sign day " + offset);
        return new RotationQuote().Contract;
    }

    private static void Pay(int amount)
    {
        int before = SaveSlot.CurrentStore.GetCash();
        var quote = new RotationQuote();
        Check(quote.Amount == amount && quote.Submit() == SaleStatus.Paid && SaveSlot.CurrentStore.GetCash() == before + amount,
            "truck paid " + amount);
        quote.Submit();
        Check(SaveSlot.CurrentStore.GetCash() == before + amount, "same truck paid exactly once");
    }

    private GameItem Claim(RotationContract repair)
    {
        Check(RepairReturns.Claim(repair) == RepairResult.Done, "shell placed on shop counter");
        var shell = counter.childItems.Last();
        Check(Goods.IdOf(shell) == IndustrialGoods.OldShell && RepairReturns.Outstanding(shell), "shell identity");
        return Move(shell, stock);
    }

    private void FirstCycle(GameItem alcohol)
    {
        Check(new RotationQuote().Sign() == SaleStatus.Unavailable, "rotation begins the day after first settlement");
        Sign(1, RotationKind.Expansion);
        Stock(IndustrialGoods.Frame, 4);
        for (int i = 0; i < 2; i++)
            Craft(IndustrialGoods.Pad, Goods.Make(ClassifierGoods.Coarse), Goods.Make(IndustrialGoods.Alloy));
        Pay(680);
        ReloadLedger();
        Sign(4, RotationKind.Supplies);
        Stock(ClassifierGoods.Fine, 3);
        Stock(IndustrialGoods.Liner, 2);
        Stock(CartelReactor.Gel, 2);
        Pay(330);

        var repair = Sign(7, RotationKind.Repair);
        Stock(IndustrialGoods.Contact, 2);
        Pay(105);
        Check(repair.Shells.All(s => !s.Issued), "first truck does not require taking housings");
        counter.Capacity = 0;
        Check(RepairReturns.Claim(repair) == RepairResult.NoRoom && RepairReturns.Unclaimed(repair) == 2, "full counter keeps both claims");
        counter.Capacity = 10;
        var a = Claim(repair);
        var b = Claim(repair);
        Check(RepairReturns.Claim(repair) == RepairResult.Unavailable && RepairReturns.Selectable(repair).Length == 2, "two numbered shells only");
        Choose(IndustrialGoods.CleanShell);
        Move(alcohol, bench.Slot("liquid"));
        WashChecks.Refusals(bench, a, alcohol);
        var cleanA = Craft(IndustrialGoods.CleanShell, a).Single();
        var cleanB = Craft(IndustrialGoods.CleanShell, b).Single();
        Check(WaterHelper.GetLiquidParts(alcohol).SequenceEqual(new[] { 100000, 0, 10000 }), "two washes spent exactly 30 mL of produced alcohol");
        var usedA = Build(cleanA);
        var usedB = Build(cleanB);
        Check(!IndustrialGoods.IsFreshUnit(usedA) && !IndustrialGoods.IsFreshUnit(usedB), "rebuilt units are not fresh");
        Check(new RotationQuote().Abandon() == SaleStatus.Ready, "abandon after first truck");
        ReloadLedger();
        usedA = ReloadItem(usedA);
        usedB = ReloadItem(usedB);
        Check(RepairReturns.Return(repair, usedA) == RepairResult.Unavailable, "stale pre-reload contract callback refused");
        repair = RepairReturns.Pending().Single();
        int before = SaveSlot.CurrentStore.GetCash();
        counter.Capacity = 2;
        Check(RepairReturns.Return(repair, usedA) == RepairResult.NoRoom && usedA.parentInventory == stock
            && counter.childItems.Count == 0 && RepairReturns.Outstanding(usedA), "short refund space retains assembly, removes staged parts");
        counter.Capacity = 3;
        bool savedConsistently = false;
        counter.mayInventoryAddItemFunc = (item, _) =>
        {
            if (Goods.IdOf(item) != IndustrialGoods.Liner) return true;
            Check(RepairReturns.Return(repair, usedA) == RepairResult.Unavailable, "nested return rejected");
            Check(CompanyTransaction.DeferSave(SaveSlot.CurrentStore, () =>
                savedConsistently = counter.childItems.Count == 0 && RepairReturns.Outstanding(usedA)
                    && CorpState.Current.Workshop.Valid()), "save deferred during staged refund");
            throw new InvalidOperationException("injected slot lookup after first part");
        };
        Check(RepairReturns.Return(repair, usedA) == RepairResult.NoRoom && savedConsistently && usedA.parentInventory == stock,
            "interrupted slot lookup rolls back before deferred save");
        counter.mayInventoryAddItemFunc = null;
        Check(RepairReturns.Return(repair, usedA) == RepairResult.Done && usedA.unitCount == 0
            && counter.childItems.Select(Goods.IdOf).SequenceEqual(new[] { IndustrialGoods.Blank, IndustrialGoods.Liner, CartelReactor.Gel })
            && counter.childItems.All(p => p.unitCount == 1) && SaveSlot.CurrentStore.GetCash() == before, "return only the three invested goods");
        Check(RepairReturns.Return(repair, usedA) != RepairResult.Done, "no second refund");
        var firstTruck = repair.Trucks[0];
        StoreStation.Day++;
        Check(new RotationQuote().Sign() == SaleStatus.Ready && ReferenceEquals(new RotationQuote().Contract, repair)
            && firstTruck.Paid == 105 && repair.SignedDay == firstPaid + 7, "resume original quote and paid truck");
        var parts = counter.childItems.ToArray();
        Craft(IndustrialGoods.Unit, Goods.Make(IndustrialGoods.Frame), parts[0], parts[1], parts[2]);
        Pay(365);
        Check(repair.Shells.Count(s => s.Returned) == 1 && repair.Shells.Count(s => s.Delivered) == 1, "one returned housing and one delivered housing");
        ReloadLedger();
        repair = RepairReturns.Pending().Single();
        counter.Capacity = 0;
        Check(RepairReturns.ClaimLining(repair) == RepairResult.NoRoom && RepairReturns.LiningDue(repair), "full counter saves lining entitlement");
        counter.Capacity = 8;
        before = SaveSlot.CurrentStore.GetCash();
        Check(RepairReturns.ClaimLining(repair) == RepairResult.Done && Goods.IdOf(counter.childItems.Single()) == "spent_lining"
            && SaveSlot.CurrentStore.GetCash() == before, "lining received without paying the truck twice");
        ReloadLedger();
        Check(!RepairReturns.Pending().Any() && RepairReturns.ClaimLining(CorpState.Current.Workshop.Rotations.Last()) != RepairResult.Done,
            "lining receipt persists");

        Sign(10, RotationKind.Joint);
        Stock(IndustrialGoods.Frame, 2);
        Stock(IndustrialGoods.Contact, 4);
        Pay(360);
        Build(Goods.Make(IndustrialGoods.Frame));
        Build(Goods.Make(IndustrialGoods.Frame));
        Pay(480);
        StoreStation.Day = firstPaid + 13;
        Check(new RotationQuote() is { Kind: RotationKind.Expansion, Window: 4, CanSign: true }, "day 13 opens next expansion");
    }

    private void RetiredShells()
    {
        var old = Sign(19, RotationKind.Repair);
        var a = Claim(old);
        var b = Claim(old);
        var oldAlcohol = bench.Slot("liquid").childItems.Single();
        bench.Slot("liquid").Expel(oldAlcohol);
        Choose(IndustrialGoods.CleanShell);
        Move(AlcoholChecks.Run(), bench.Slot("liquid"));
        var unit = Build(Craft(IndustrialGoods.CleanShell, a).Single());
        Check(new RotationQuote().Abandon() == SaleStatus.Ready, "retire with two physical housing forms");
        ReloadLedger();
        b = ReloadItem(b);
        unit = ReloadItem(unit);
        old = RepairReturns.Pending().Single();
        var joint = Sign(22, RotationKind.Joint);
        Stock(IndustrialGoods.Frame, 2);
        Stock(IndustrialGoods.Contact, 4);
        Pay(360);
        Build(Goods.Make(IndustrialGoods.Frame));
        var priorDay = new RotationQuote();
        StoreStation.Day = firstPaid + 25;
        var carried = new RotationQuote();
        Check(priorDay.Submit() == SaleStatus.Unavailable && carried.Window == 8 && carried.Kind == RotationKind.Joint
            && ReferenceEquals(carried.Contract, joint) && !carried.CanSign && carried.Check() == SaleStatus.Short
            && carried.Stock.All(s => s.Item != unit), "new window keeps the signed joint contract and refuses its retired repair unit");
        Check(carried.Abandon() == SaleStatus.Ready && new RotationQuote() is { Kind: RotationKind.Expansion, Contract: null },
            "suspending outside the signing window immediately reveals the current offer");
        var current = Sign(31, RotationKind.Repair);
        Stock(IndustrialGoods.Contact, 2);
        Pay(105);
        Check(new RotationQuote().Check() == SaleStatus.Short && !new RotationQuote().Stock.Any(s => s.Item == unit)
            && RepairReturns.Return(current, unit) == RepairResult.Changed, "same-type later instance cannot use or refund earlier unit");
        string run = CorpState.Current.RunId;
        CorpState.Current.RunId = run + "-other";
        Check(!RepairReturns.Outstanding(unit) && RepairReturns.Return(old, b) == RepairResult.Changed, "foreign save rejects stamped stock");
        CorpState.Current.RunId = run;
        int before = SaveSlot.CurrentStore.GetCash();
        Choose(IndustrialWorkbench.Dismantle);
        WashChecks.DismantleRollback(bench, unit);
        var recovered = Craft(IndustrialWorkbench.Dismantle, unit);
        Check(recovered.Select(Goods.IdOf).Order().SequenceEqual(new[] {
            IndustrialGoods.CleanShell, IndustrialGoods.Blank, IndustrialGoods.Liner, CartelReactor.Gel }.Order())
            && recovered.All(p => p.unitCount == 1), "dismantle retired unit into its four components");
        var clean = recovered.Single(i => Goods.IdOf(i) == IndustrialGoods.CleanShell);
        Check(RepairReturns.Return(old, clean) == RepairResult.Done && RepairReturns.Return(old, b) == RepairResult.Done
            && old.Shells.All(s => s.Returned) && SaveSlot.CurrentStore.GetCash() == before, "original retired contract accepts both clean and raw housings without cash");
        ReloadLedger();
        Check(!RepairReturns.Pending().Any(c => c.Serial == old.Serial) && RepairReturns.Unclaimed(new RotationQuote().Contract) == 2,
            "retired claims stay closed and new instance retains its own pair");
    }

    private void InterruptedRollback()
    {
        var current = new RotationQuote().Contract;
        var unit = Build(Craft(IndustrialGoods.CleanShell, Claim(current)).Single());
        counter.Capacity = counter.childItems.Count + 1;
        counter.mayInventoryAddItemFunc = (item, _) =>
        {
            if (Goods.IdOf(item) == IndustrialGoods.Blank) item.BeforeDestroy = () =>
            {
                item.unitCount = 0; // The native destroyed flag is already set when Expel callbacks run.
                throw new InvalidOperationException("destroy interrupted before graph detach");
            };
            return true;
        };
        int before = SaveSlot.CurrentStore.GetCash();
        Check(RepairReturns.Return(current, unit) == RepairResult.Frozen
            && counter.childItems.Any(p => Goods.IdOf(p) == IndustrialGoods.Blank && StorePlacement.Destroyed(p))
            && unit.parentInventory == stock && current.Shells[0].InFlight && !current.Shells[0].Returned,
            "destroyed refund still attached to save graph keeps its slot frozen");
        ReloadLedger();
        Check(RepairReturns.Return(new RotationQuote().Contract, unit) != RepairResult.Done && SaveSlot.CurrentStore.GetCash() == before,
            "interrupted rollback cannot refund or pay again");
    }

    internal static void Run(GameItem alcohol)
    {
        var shop = EmporiumEntry.Instance;
        var front = shop.frontInvinvElement;
        var back = shop.backInvinvElement;
        var listed = shop.ListedOutput;
        var day = StoreStation.Day;
        var workshop = CorpState.Current.Workshop;
        try
        {
            // The first overhaul was actually paid by IndustrialChecks. Give this scenario isolated inventory grids.
            ReloadLedger();
            shop.ListedOutput = shop.frontInvinvElement = new();
            shop.backInvinvElement = new();
            var path = new RotationJourneyChecks();
            path.FirstCycle(alcohol);
            path.RetiredShells();
            path.InterruptedRollback();
        }
        finally
        {
            shop.frontInvinvElement = front;
            shop.backInvinvElement = back;
            shop.ListedOutput = listed;
            StoreStation.Day = day;
            CorpState.Current.Workshop = workshop;
        }
        Console.WriteLine("PASS 12-day rotation +680/+330/+105/+365/+360/+480, full counter, numbered repairs, wash/assemble/dismantle, retirement and reload.");
    }
}
