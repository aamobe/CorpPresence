using System.Reflection;

namespace CorpPresence;

static class Program
{
    const string Paste = "corp_presence_fruit_paste";
    const string Power = "power_source_item_energy";
    static void Check(bool ok, string label)
    {
        if (!ok) throw new Exception(label);
        Console.WriteLine("PASS " + label);
    }
    static GameItem Build() => (GameItem)typeof(Canning).GetMethod("BuildSeafoodSealer", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    static void CheckRollback(int failure)
    {
        var machine = Build();
        var panels = machine.Window.Panels;
        var battery = new GameItem("battery");
        battery.EnableTag("power_source_item");
        battery.GetTagReadonly(Power).SetInt(18);
        ((GameSlotInventory)panels[0].Body).Add(battery);
        var water = new GameItem("water");
        water.EnableTag("LIQUID_CONTAINER_TAG");
        water.GetTagReadonly("water_ml").SetInt(100000);
        ((GameSlotInventory)panels[1].Body).Add(water);
        var mince = new GameItem("corp_presence_fish_mince") { unitCount = 4 };
        ((GameGridInventory)panels[2].Body).Add(mince);
        ((MachineryStack)panels[3].Body).Controls.OfType<MachineryRadioButton>().Single(m => m.Name == "Crab Stick").SetOn(true, notify: true);
        var plastic = new GameItem("printer_plastic");
        ((GameSlotInventory)panels[5].Body).Add(plastic);
        PowerHelper.FailureMode = failure;
        try { machine.onCycleEndSlotItemFunc(machine, null, null); }
        finally { PowerHelper.FailureMode = 0; }
        Check(((GameGridInventory)panels[4].Body).childItems.Count == 0 && mince.unitCount == 4 &&
            water.GetTagReadonly("water_ml").GetInt() == 100000 && battery.GetTagReadonly(Power).GetInt() == 18 &&
            ((GameSlotInventory)panels[5].Body).childItem == plastic, $"Power failure mode {failure} restores water, battery, ingredients and output");
    }
    static void Main()
    {
        for (int failure = 1; failure <= 4; failure++) CheckRollback(failure);
        var machine = Build();
        var window = machine.Window;
        var inputs = (GameGridInventory)window.Panels[2].Body;
        var output = (GameGridInventory)window.Panels[4].Body;
        var battery = new GameItem("battery"); battery.GetTagReadonly(Power).SetInt(18);
        ((GameSlotInventory)window.Panels[0].Body).childItems.Add(battery);
        var stack = (MachineryStack)window.Panels[3].Body;
        Check(stack.CrossAlignment == MachineryStack.Align.Start, "Mode controls share the same left edge");
        var controls = stack.Controls;
        var modes = controls.OfType<MachineryRadioButton>().ToArray();
        var inspect = controls.OfType<MachineryButton>().Single();
        Check(modes.Any(m => m.Name == "Galaxy Blend"), "Sealer offers Galaxy Blend mode");
        var galaxy = modes.Single(m => m.Name == "Galaxy Blend");
        galaxy.SetOn(true, notify: true);
        var paste = new GameItem(Paste) { unitCount = 2 };
        Check(inputs.mayInventoryAddItemFunc(paste, inputs), "Sealer accepts nutrifruit paste");
        Check(!inputs.mayInventoryAddItemFunc(new GameItem("nutrifruit"), inputs), "Whole fruit remains rejected");
        var fishMince = new GameItem("corp_presence_fish_mince") { unitCount = 4 };
        inputs.Add(paste);
        inputs.Add(fishMince);
        inspect.Click();
        Check(StoreUIManager.Instance.Message.StartsWith("First batch ready") && paste.unitCount == 2 && output.childItems.Count == 0 && battery.GetTagReadonly(Power).GetInt() == 18, "Inspect recognizes paste without water and spends nothing");
        output.Capacity = 0;
        machine.onCycleEndSlotItemFunc(machine, null, null);
        Check(paste.unitCount == 2 && battery.GetTagReadonly(Power).GetInt() == 18, "Full output preserves ingredients and power");
        output.Capacity = 2;
        machine.onCycleEndSlotItemFunc(machine, null, null);
        Check(output.childItems.Single().identifier == "galaxy_blend" && output.childItems.Single().owned && paste.unitCount == 1 && fishMince.unitCount == 4 && battery.GetTagReadonly(Power).GetInt() == 12, "One paste becomes one owned vanilla Galaxy Blend, consuming only paste and 6 charge");
        var restored = Build();
        foreach (var (key, tag) in machine.Tags) restored.Tags[key] = tag;
        restored.onLoaded(restored);
        Check(((MachineryStack)restored.Window.Panels[3].Body).Controls.OfType<MachineryRadioButton>().Single(m => m.IsOn).Name == "Galaxy Blend", "Saved Galaxy Blend mode is restored");
        var legacy = Build();
        legacy.EnableTag("CORP_SEAL_WHOLE_FISH_BOOL");
        legacy.onLoaded(legacy);
        Check(((MachineryStack)legacy.Window.Panels[3].Body).Controls.OfType<MachineryRadioButton>().Single(m => m.IsOn).Name == "Canned Fish", "Old fish mode tag still restores");
        modes.Single(m => m.Name == "Crab Stick").SetOn(true, notify: true);
        var plastic = new GameItem("printer_plastic");
        ((GameSlotInventory)window.Panels[5].Body).Add(plastic);
        inspect.Click();
        Check(StoreUIManager.Instance.Message.Contains("100 mL"), "Switching to crab clears Galaxy Blend mode and still requires water");
        var water = new GameItem("water"); water.EnableTag("LIQUID_CONTAINER_TAG"); water.GetTagReadonly("water_ml").SetInt(100000);
        ((GameSlotInventory)window.Panels[1].Body).Add(water);
        machine.onCycleEndSlotItemFunc(machine, null, null);
        Check(output.childItems.Last().identifier == "crab_stick" && paste.unitCount == 1 && inputs.childItems.Contains(plastic) == false && ((GameSlotInventory)window.Panels[5].Body).childItem == plastic && water.GetTagReadonly("water_ml").GetInt() == 0, "Crab production still uses water and preserves the reusable material");
    }
}
