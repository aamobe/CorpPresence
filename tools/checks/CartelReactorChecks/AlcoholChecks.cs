namespace CorpPresence;

internal static class AlcoholChecks
{
    internal static GameItem Run()
    {
        static void Check(bool condition, string step) { if (!condition) throw new Exception(step); }
        static GameItem Vessel(int capacity, int water, int dirt)
        {
            var vessel = new GameItem("container");
            vessel.EnableTag("LIQUID_CONTAINER_TAG");
            vessel.EnableTag("LIQUID_CONTAINER_BASE_PRICE_TAG");
            vessel.ModifyTag("capacity", t => t.SetInt(capacity));
            vessel.ModifyTag("water", t => t.SetInt(water));
            vessel.ModifyTag("physical_contaminant", t => t.SetInt(dirt));
            return vessel;
        }

        var station = AlcoholPreparationStation.Definition.Shell();
        var cell = station.Slot("power").Put(new GameItem("energy_credit") { Charge = 7 });
        var input = station.Slot("water").Put(Vessel(250000, 200000, 20000));
        var receiver = station.Slot("receiver").Put(Vessel(150000, 0, 0));
        var first = station.Slot("fruit").Put(new GameItem("nutrifruit"));
        var second = station.Slot("fruit").Put(new GameItem("nutrifruit") { unitCount = 3 });
        first.ModifyTag("CALORIE_VALUE_TAG", t => t.SetInt(100));
        second.ModifyTag("CALORIE_VALUE_TAG", t => t.SetInt(80));
        station.Night();
        Check(cell.Charge == 7 && first.unitCount == 1 && second.unitCount == 3, "Low battery consumed fruit");
        cell.Charge = 8;
        receiver.ModifyTag("capacity", t => t.SetInt(139999));
        station.Night();
        Check(cell.Charge == 8 && WaterHelper.GetLiquidParts(input)[0] == 200000, "Short receiver consumed inputs");
        receiver.ModifyTag("capacity", t => t.SetInt(150000));
        first.AfterDestroy = () => throw new InvalidOperationException("native callback after fruit removal");
        second.FailCountRedraw = true;
        station.Night();
        Check(cell.Charge == 0 && first.parentInventory == null && second.unitCount == 1, "Fruit debits and power did not settle together");
        Check(WaterHelper.GetLiquidParts(input).SequenceEqual(new[] { 100000, 0, 10000 }) &&
            WaterHelper.GetLiquidParts(receiver).SequenceEqual(new[] { 100000, 30000, 10000 }), "Paid batch lost its liquid transfer");
        Check(station.GetTagReadonly("CORP_ALCOHOL_FRUIT_KCAL").GetLong() == 20, "Unused fruit heat missing");
        Console.WriteLine("PASS alcohol station settles all fruit, mixed liquid and power despite native fruit redraw failures");
        station.Slot("receiver").Expel(receiver);
        return receiver;
    }
}
