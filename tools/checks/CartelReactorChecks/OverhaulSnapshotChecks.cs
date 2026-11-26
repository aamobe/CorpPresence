namespace CorpPresence;

internal static class OverhaulSnapshotChecks
{
    private static void Check(bool ok,string detail) { if (!ok) throw new Exception(detail); }

    internal static void Run()
    {
        var shop = EmporiumEntry.Instance;
        var rack = shop.frontInvinvElement;
        var root = CorpState.Current;
        var meridian = root.Companies["meridian"];
        int day = StoreStation.GetDayCounter();
        var locked = Goods.Make(IndustrialGoods.Frame);
        rack.Put(locked);
        rack.mayInventoryRemoveItemFunc = (item, inventory) => item != locked;
        shop.ListedOutput = rack;
        var mill = Goods.Make(PowderClassifier.Key);
        rack.Put(mill);
        mill.ModifyTag("CORP_CLASSIFIER_BATCH",t => t.SetString("crystal.v1"));
        mill.ModifyTag("CORP_CLASSIFIER_NIGHTS",t => t.SetInt(1));
        var growth = MineralGrowthStation.Definition.Shell();
        growth.identifier = "corp_presence_" + MineralGrowthStation.Key;
        rack.Put(growth);
        growth.ModifyTag("CORP_MINERAL_GROWTH_RECIPE",t => t.SetString(MineralGrowthBatch.LightSaltBlueprint));
        growth.ModifyTag("CORP_MINERAL_GROWTH_PROGRESS",t => t.SetInt(100));
        var reactor = Goods.Make(CartelReactor.Key);
        reactor.ModifyTag("CORP_CARTEL_BLUE_BATCH",t => t.SetInt(2));
        rack.Put(reactor);
        var pure = Goods.Make("powder_box"); pure.Parts = new[]{120,0,0,0,1}; rack.Put(pure);
        var coarse = Goods.Make("powder_box"); coarse.Parts = new[]{120,0,0,0,15}; rack.Put(coarse);
        meridian.Orders.Add(new Order{Kind=MeridianIndustrial.OrderKind, Day=day, Items=new(){MeridianCommissionReceipt.FrameId}});
        root.Workshop.Commission = new MeridianCommissionReceipt { Quantity=2, Ingots=2, Alloy=2, Paid=22 };
        var snapshot = OverhaulWorkshopStock.Capture(day);
        Check(snapshot.Stock.GetValueOrDefault(Part.Frame) == 0 && locked.MaxNumRemove() == 1, "locked frames cannot be promised");
        Check(snapshot.PureMetal == 120 && snapshot.Powder[0] == 240, "pure and qualifying metal inventories");
        Check(snapshot.Escrow is { Quantity:2, Paid:22 } && snapshot.Deliveries[0] == 1, "real escrow and due-today frame");
        var credited = new Dictionary<Part,int>();
        snapshot.Queue.AddProducts(credited,0);
        Check(credited.GetValueOrDefault(Part.Gel) == 1 && credited.GetValueOrDefault(Part.Grit)==0, "mature blue vessel collectible today");
        snapshot.Queue.AddProducts(credited,1);
        Check(credited.GetValueOrDefault(Part.Grit) == 2 && credited.GetValueOrDefault(Part.Core) == 2, "one remaining working night in both machines");
        mill.ModifyTag("CORP_CLASSIFIER_BATCH",t => t.SetString("charge:crystal.v1"));
        root.Workshop.Commission.InFlight = true;
        meridian.Orders.Add(new Order{Kind=MeridianIndustrial.OrderKind, Day=day, Items=new(){"unexpected"}});
        var uncertain = OverhaulWorkshopStock.Capture(day);
        Check(uncertain.FrozenCommission && uncertain.Escrow == null && uncertain.UnknownDelivery && uncertain.Transit == 1,
            "unconfirmed escrow and malformed delivery never become credit");
        Check(!uncertain.Queue.Free(OverhaulBay.Mill,20) && uncertain.Queue.Batches.Any(b=>b.Bay==OverhaulBay.Mill && b.Day < 0),
            "charge-only input receipt does not manufacture predictable crystal output");
        root.Workshop.Commission = null;
        meridian.Orders.Clear();
        rack.mayInventoryRemoveItemFunc = null;
        Console.WriteLine("PASS physical schedule snapshot: locked stock, pure metal, both active batches, mature gel, due-today frames, uncertain receipts");
    }
}
