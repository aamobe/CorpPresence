using System;

namespace CorpPresence;

internal static class PowderSieve
{
    internal const string Key = "powder_sieve";
    private static readonly (string Zh, string En)[] Names =
    {
        ("金属", "Metal"), ("硅质", "Silica"), ("稀有金属", "Rare metal"),
        ("导电料", "Conductive material"), ("渣料", "Residue"), ("回收粉", "Mixed tailings")
    };
    private static readonly string[] Bins = MakeTags("CORP_SIEVE_BIN_");
    private static readonly string[] Tailings = MakeTags("CORP_SIEVE_RECOVERY_");

    internal static readonly Goods.Def Definition = new()
    {
        Name = "矿粉筛分机", NameEn = "Powder Sieve", Value = 260,
        Company = "mineral", Art = "powder-sieve", Fallback = "furnace",
        W = 3, H = 3, Types = new[] { "MACHINE" }, Shell = Build
    };

    private static string[] MakeTags(string prefix)
    {
        var tags = new string[PowderBox.ComponentCount];
        for (int i = 0; i < tags.Length; i++) tags[i] = prefix + i;
        return tags;
    }

    private static int Read(GameItem machine, string tag) => Math.Max(0, machine.GetTagReadonly(tag).GetInt());

    private static void Write(GameItem machine, string tag, int amount) =>
        machine.ModifyTag(tag, state => state.Enable().SetInt(amount), skipValidate: true);

    private static bool Ready(GameItem machine) => GeneralHelper.IsItemOwned(machine) && SaveSlot.CanWrite("CorpPresence");

    private static int Throughput(GameItem machine) =>
        Math.Max(0, machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt());

    private static int Recovery(GameItem machine) =>
        PowderSieveRules.RecoveryRate(machine.GetTagReadonly("TOTAL_PERCENTAGE_QUALITY_BONUS_INT").GetInt());

    private static GameItem Build()
    {
        var battery = new GameSlotInventory();
        var modules = MineralModuleBay.Create();
        var input = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) => PowderBox.IsOwnedBox(item) };
        var legacy = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (_, _) => false };
        var legacyPanel = MachineHelper.Panel(L.T("取回旧接粉盒", "Retrieve old powder box"), legacy, "output");
        legacyPanel.SetHidden(true);
        var status = new MachineryText(uppercase: false);
        var bins = new GameSlotInventory[Names.Length];
        var labels = new MachineryText[Names.Length];
        var displays = new MachineryStack[Names.Length];
        for (int i = 0; i < bins.Length; i++)
        {
            bins[i] = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (_, _) => false };
            labels[i] = new MachineryText(uppercase: false);
            displays[i] = MachineryStack.Vertical(labels[i], bins[i]);
        }
        var window = MachineHelper.CreateMachineryWindow(true,
            MachineHelper.PowerPanel(battery),
            MachineHelper.Panel(L.T("模组", "Modules"), modules, "module"),
            MachineHelper.Panel(L.T("进料盒", "Feed box"), input, "input"),
            legacyPanel,
            MachineHelper.Panel(L.T("过夜筛分", "Overnight sieving"), status, "control"),
            MachineHelper.Panel(L.T("内置粉盒", "Internal powder boxes"), MachineryStack.Vertical(
                MachineryStack.Horizontal(displays[0], displays[1], displays[2]),
                MachineryStack.Horizontal(displays[3], displays[4], displays[5])), "bins"));
        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");
        machine.EnableTag("STANDARD_MACHINE_TAG");
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineryHelper.InitMachinery(machine, 12);
        machine.ModifyTag("BASE_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(PowderSieveRules.BaseThroughput));
        machine.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(PowderSieveRules.BaseThroughput));

        void Refresh()
        {
            int power = MachineryHelper.GetMachinePowerUsage(machine);
            status.SetText(L.T($"每夜 {Throughput(machine)} 点 · {power} 电\n回收率 {Recovery(machine)}%\n选中内置粉盒，再点目标盒",
                $"{Throughput(machine)} points/night · {power} charge\n{Recovery(machine)}% recovery\nSelect a bin, then click the target box"));
            long tailings = 0;
            for (int i = 0; i < Bins.Length; i++)
            {
                labels[i].SetText($"{L.T(Names[i].Zh, Names[i].En)}\n{Read(machine, Bins[i])}/{PowderSieveRules.BinCapacity}");
                tailings += Read(machine, Tailings[i]);
            }
            labels[Bins.Length].SetText($"{L.T(Names[Bins.Length].Zh, Names[Bins.Length].En)}\n{tailings}/{PowderSieveRules.BinCapacity}");
            foreach (var slot in bins)
                if (slot.childItem != null) Goods.Redraw(slot.childItem);
        }

        void Open()
        {
            legacyPanel.SetHidden(legacy.childItem == null);
            for (int i = 0; i < bins.Length; i++)
            {
                int index = i;
                var slot = bins[i];
                var box = slot.childItem;
                if (box != null && PowderBox.IsBoundBin(box)) continue;
                if (box == null)
                {
                    if (!Ready(machine)) continue;
                    box = Goods.Make(PowderBox.Key);
                    if (box == null) continue;
                    GeneralHelper.SetItemOwned(box);
                    if (!slot.UncheckedAccept(box))
                    {
                        box.Destroy();
                        continue;
                    }
                }
                PowderBox.BindBin(box, () => ReadBin(machine, index), parts =>
                {
                    if (index < Bins.Length) Write(machine, Bins[index], parts[index]);
                    else
                        for (int j = 0; j < Tailings.Length; j++) Write(machine, Tailings[j], parts[j]);
                    machine.ValidateShapeState();
                    Refresh();
                });
            }
            Refresh();
        }

        MachineHelper.SetupModuleBay(modules, machine, Refresh);
        legacy.onSlotRemoveItemEndFunc += (_, _, _) => legacyPanel.SetHidden(legacy.childItem == null);
        window.openCallback += Open;
        machine.onLoaded += _ => Open();
        machine.onCycleEndSlotItemFunc = (_, _, _) => HookGuard.Run("过夜筛粉", () =>
        {
            if (!Ready(machine)) return;
            ModuleHelper.ComputeModuleEffect(modules, null, machine);
            ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
            Sieve(machine, battery, input.childItem, preview: false);
            Refresh();
        });
        MachineAppearance.WatchCycle(machine, () => Sieve(machine, battery, input.childItem, preview: true));
        Refresh();
        return machine;
    }

    private static bool Sieve(GameItem machine, GameSlotInventory battery, GameItem box, bool preview)
    {
        if (!Ready(machine) || !machine.GetTagReadonly(MachineryHelper.IS_MACHINERY_POWERED).GetBool()
            || !PowderBox.IsOwnedBox(box) || !MachineHelper.CanPower(machine, battery)) return false;

        var parts = new int[PowderBox.ComponentCount];
        long total = PowderBox.Read(box, parts);
        if (total <= 0 || total > PowderColor.Capacity) return false;
        int amount = Math.Min((int)total, Throughput(machine));
        if (amount <= 0) return false;
        var batch = WaterHelper.SplitProportionally(parts, amount);
        Span<int> sorted = stackalloc int[PowderBox.ComponentCount];
        Span<int> tailings = stackalloc int[PowderBox.ComponentCount];
        int rate = Recovery(machine);
        long tailingsTotal = 0;

        for (int i = 0; i < parts.Length; i++)
        {
            int recovered = PowderSieveRules.Recovered(batch[i], rate);
            long sortedAmount = (long)Read(machine, Bins[i]) + recovered;
            if (sortedAmount > PowderSieveRules.BinCapacity) return false;
            sorted[i] = (int)sortedAmount;
            long tailing = (long)Read(machine, Tailings[i]) + batch[i] - recovered;
            tailingsTotal += tailing;
            if (tailingsTotal > PowderSieveRules.BinCapacity) return false;
            tailings[i] = (int)tailing;
            parts[i] -= batch[i];
        }
        if (preview) return true;
        if (!MachineHelper.TryDrawCyclePower(machine, battery)) return false;

        for (int i = 0; i < parts.Length; i++)
        {
            Write(machine, Bins[i], sorted[i]);
            Write(machine, Tailings[i], tailings[i]);
        }
        machine.ValidateShapeState();
        PowderBox.WriteComposition(box, parts);
        return true;
    }

    private static int[] ReadBin(GameItem machine, int index)
    {
        var parts = new int[PowderBox.ComponentCount];
        if (index < Bins.Length)
            parts[index] = Read(machine, Bins[index]);
        else
            for (int i = 0; i < parts.Length; i++) parts[i] = Read(machine, Tailings[i]);
        return parts;
    }
}
