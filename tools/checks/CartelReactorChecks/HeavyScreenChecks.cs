using CorpPresence;

internal static class HeavyScreenChecks
{
    internal static void Run()
    {
        const string pad = "heavy_screen_pad";
        var bench = CraftingStation.Definition.Shell();
        var buttons = ((object[])bench.contentWindow.childElement.GetPanel("control").Content).OfType<MachineryButton>().ToArray();
        for (int i = 0; i < 12 && bench.GetTagReadonly("CORP_CRAFT_MILL_RECIPE").Text != pad; i++) buttons[0].Click();
        if (bench.GetTagReadonly("CORP_CRAFT_MILL_RECIPE").Text != pad)
            throw new Exception("Heavy screen pad unavailable on ordinary workbench");
        var inputs = (GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content;
        inputs[0].Put(Goods.Make(ClassifierGoods.Coarse));
        inputs[1].Put(Goods.Make(IndustrialGoods.Alloy));
        buttons[1].Click();
        var made = bench.Slot("output").childItems.Single();
        if (Goods.IdOf(made) != pad || inputs.Any(slot => slot.childItem != null)) throw new Exception("Heavy pad recipe");
        bench.Slot("output").Expel(made);
        var mill = PowderClassifier.Definition.Shell();
        mill.Slot("pad").Put(made);
        var mode = ((object[])mill.contentWindow.childElement.GetPanel("control").Content).OfType<MachineryButton>().First();
        for (int i = 0; i < 4 && mill.GetTagReadonly("CORP_CLASSIFIER_MODE").Number != 3; i++) mode.Click();
        var box = mill.Slot("silica").Put(Goods.Make("powder_box"));
        box.Parts = new[] { 0, 179, 0, 0, 20 };
        var cell = mill.Slot("power").Put(new GameItem("energy_credit"){Charge = 15});
        mill.Night();
        cell.Charge = 16;
        mill.Slot("output").Capacity = 5;
        mill.Night();
        if (cell.Charge != 16 || box.Parts[1] != 179 || mill.Slot("output").childItems.Count != 0)
            throw new Exception("Double throughput charged without 16 power or six spaces");
        mill.Slot("output").Capacity = 6;
        mill.Night();
        var output = mill.Slot("output").childItems;
        if (cell.Charge != 0 || !box.Parts.SequenceEqual(new[]{0,59,0,0,20}) || made.unitCount != 1 ||
            output.Count(i=>Goods.IdOf(i)==ClassifierGoods.Coarse)!=4 || output.Count(i=>Goods.IdOf(i)==ClassifierGoods.Fine)!=2)
            throw new Exception("Double throughput did not deliver four coarse and two fine for 120 silica and 16 power");
        mode.Click();
        if (mill.GetTagReadonly("CORP_CLASSIFIER_MODE").Number != 0) throw new Exception("Single throughput selectable with pad installed");
        Console.WriteLine("PASS crafted permanent heavy screen pad, checked power and six spaces, processed one actual double batch.");
    }
}
