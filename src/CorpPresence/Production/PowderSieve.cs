using System;

namespace CorpPresence;

internal static partial class PowderSieve
{
    internal const string Key = "powder_sieve";
    private const string ReturnTag = "CORP_SIEVE_RESIEVE_ONCE", ReturnNightTag = "CORP_SIEVE_RETURN_NIGHT";
    internal const string TailingsRecord = "tailings";
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

    private static bool IsScreen(GameItem item) => Goods.IdOf(item) == ClassifierGoods.Screen
        && item.unitCount == 1 && GeneralHelper.IsItemOwned(item) && !StorePlacement.Destroyed(item);

    private static bool IsGuide(GameItem item) => MachineService.IsStockPart(item, RodMaterial.ReturnGuide)
        && AlloyForms.QualifiesReturnGuide(RodCondition.Read(item));

    internal static bool TailingsQueued(GameItem machine) => machine.GetTagReadonly(ReturnTag).GetBool();

    private static bool ReturnedThisNight(GameItem machine) =>
        machine.GetTagReadonly(ReturnNightTag).GetLong() is > 0 and var settled && settled == (long)StoreStation.GetDayCounter() + 1;

    internal static bool AcceptsFactoryFeed(GameItem machine) => !TailingsQueued(machine) && !ReturnedThisNight(machine);

    private static GameItem Build()
    {
        var battery = new GameSlotInventory();
        var modules = MineralModuleBay.Create();
        var input = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) => PowderBox.IsOwnedBox(item) };
        var legacy = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (_, _) => false };
        var legacyPanel = MachineHelper.Panel(L.T("取回旧接粉盒", "Retrieve old powder box"), legacy, "output");
        legacyPanel.SetHidden(true);
        var coil = new GameSlotInventory(2, 2) { mayInventoryAddItemFunc = (item, _) => MachineService.Handbook
            && MachineService.IsStockPart(item, RodMaterial.IndustrialCoil) };
        var coilPanel = MachineHelper.Panel(L.T("动力 · 工业线圈", "Power · Industrial coil"), coil, "service_coil").SetHidden(true);
        var screen = new GameSlotInventory(2, 2) { mayInventoryAddItemFunc = (item, _) => MachineService.Handbook && IsScreen(item) };
        var guide = new GameSlotInventory(2, 2) { mayInventoryAddItemFunc = (item, _) => MachineService.Handbook && IsGuide(item) };
        var screenPanel = MachineHelper.Panel(Goods.NameOf(ClassifierGoods.Screen), screen, "return_screen").SetHidden(true);
        var guidePanel = MachineHelper.Panel(Goods.NameOf(RodMaterial.ReturnGuide), guide, "return_guide").SetHidden(true);
        var status = new MachineryText(uppercase: false);
        var closeCover = new MachineryButton(L.T("合上检修盖", "Close cover"));
        var queueReturn = new MachineryButton(MineralNightReport.Reason(MineralStop.SieveParts));
        var bins = new GameSlotInventory[Names.Length];
        var labels = new MachineryText[Names.Length];
        var displays = new MachineryStack[Names.Length];
        for (int i = 0; i < bins.Length; i++)
        {
            bins[i] = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (_, _) => false };
            labels[i] = new MachineryText(uppercase: false);
            displays[i] = MachineryStack.Vertical(labels[i], bins[i]);
        }
        GameItem machine = null;
        var receiver = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) => PowderBox.IsOwnedBox(item) };
        var tabs = new MachineryTabs();
        tabs.AddTab(L.K("workshop.sieve.bins", "粉仓", "Bins"), MachineryStack.Vertical(
            MachineryStack.Horizontal(displays[0], displays[1], displays[2]),
            MachineryStack.Horizontal(displays[3], displays[4], displays[5])));
        Action refreshMix = null;
        var mixing = MixingPanel(() => machine, input, bins, receiver, Refresh, out var updateMix);
        refreshMix = updateMix;
        tabs.AddTab(L.K("workshop.sieve.mix", "配料", "Blend"), mixing);
        tabs.onChanged += _ => refreshMix();
        var window = MachineHelper.CreateMachineryWindow(true,
            MachineHelper.PowerPanel(battery),
            MachineHelper.Panel(L.T("模组", "Modules"), modules, "module"),
            MachineHelper.Panel(L.T("进料盒", "Feed box"), input, "input"),
            legacyPanel,
            MachineHelper.Panel(L.T("过夜筛分", "Overnight sieving"), MachineryStack.Vertical(status,
                MineralNightReport.Button(() => machine, MineralNightReport.Sieve), closeCover, queueReturn), "control"),
            MachineHelper.Panel(L.T("内置粉盒", "Internal powder boxes"), tabs, "bins"),
            coilPanel, screenPanel, guidePanel);
        machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        MachineService.Register(machine, () => machine.identifier == "corp_presence_" + Key && MachineService.Handbook,
            visible =>
            {
                coilPanel.SetHidden(!visible);
                screenPanel.SetHidden(!visible);
                guidePanel.SetHidden(!visible);
            });
        closeCover.SetCallback(() => MachineService.Close(machine));
        bool ReturnFitted() => MachineService.Handbook && IsScreen(screen.childItem) && IsGuide(guide.childItem);
        queueReturn.SetCallback(() =>
        {
            if (!Ready(machine)) return;
            bool queued = TailingsQueued(machine);
            if (!queued && !ReturnFitted()) return;
            machine.ModifyTag(ReturnTag, tag => tag.Enable().SetBool(!queued), skipValidate: true);
            Refresh();
        });
        machine.EnableTag("CONTAINER_TAG");
        machine.EnableTag("STANDARD_MACHINE_TAG");
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineryHelper.InitMachinery(machine, 12);
        machine.ModifyTag("BASE_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(PowderSieveRules.BaseThroughput));
        machine.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(PowderSieveRules.BaseThroughput));

        void Refresh()
        {
            int power = MachineService.CoilCharge(machine, coil.childItem);
            bool returning = TailingsQueued(machine);
            var condition = PlanNight(machine, battery, input.childItem, power, ReturnFitted(), out var night);
            string forecast = night == null ? MineralNightReport.Reason(condition, returning) :
                L.F("workshop.sieve.forecast", "预计入分类仓 {0}", "Expected in sorted bins {0}", MineralNightReport.Vector(night.Sorted));
            if (returning)
                forecast = L.K("workshop.sieve.returning", "已排一批回筛 · 新料等一夜", "Tailings queued · new feed waits one night") + "\n" + forecast;
            queueReturn.SetText(returning ? L.K("workshop.sieve.cancelReturn", "取消回筛", "Cancel tailings batch") :
                ReturnFitted() ? L.K("workshop.sieve.queueReturn", "排一批尾粉回筛", "Queue one tailings batch") :
                MineralNightReport.Reason(MineralStop.SieveParts));
            string goalId = machine.GetTagReadonly(TargetTag).GetString();
            if (!string.IsNullOrEmpty(goalId))
            {
                var target = GoalFor(goalId);
                var goal = target.Ingredient;
                int bin = (int)goal.Component;
                int stocked = Read(machine, Bins[bin]);
                int projected = night?.Sorted[bin] ?? 0;
                forecast += "\n" + L.F("workshop.sieve.goalStock", "{0}：仓有 {1}，预计新增 {2}，还差 {3}",
                    "{0}: bin holds {1}, expected gain {2}, still short {3}", GoalName(target),
                    stocked, projected, Math.Max(0, goal.Points - stocked - projected));
            }
            status.SetText(L.T($"每夜 {Throughput(machine)} 点 · {power} 电\n回收率 {Recovery(machine)}%\n选中内置粉盒，再点目标盒",
                $"{Throughput(machine)} points/night · {power} charge\n{Recovery(machine)}% recovery\nSelect a bin, then click the target box") + "\n" + forecast);
            refreshMix?.Invoke();
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
                PowderBox.BindBin(box, machine, () => ReadBin(machine, index), parts =>
                {
                    if (index < Bins.Length) Write(machine, Bins[index], parts[index]);
                    else
                        for (int j = 0; j < Tailings.Length; j++) Write(machine, Tailings[j], parts[j]);
                }, () =>
                {
                    machine.ValidateShapeState();
                    Refresh();
                });
            }
            Refresh();
        }

        input.onSlotAddItemEndFunc += (_, _, _) => Refresh();
        input.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        battery.onSlotAddItemEndFunc += (_, _, _) => Refresh();
        battery.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        coil.onSlotAddItemEndFunc += (_, _, _) => Refresh();
        coil.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        screen.onSlotAddItemEndFunc += (_, _, _) => Refresh();
        screen.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        guide.onSlotAddItemEndFunc += (_, _, _) => Refresh();
        guide.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        MachineHelper.SetupModuleBay(modules, machine, Refresh);
        legacy.onSlotRemoveItemEndFunc += (_, _, _) => legacyPanel.SetHidden(legacy.childItem == null);
        window.openCallback += () => MachineService.OpenWindow(machine);
        window.openCallback += Open;
        window.closeCallback += () => MachineService.Close(machine);
        machine.onLoaded += _ => Open();
        FoldedFactory.RegisterSieve(machine, input, () =>
        {
            if (!Ready(machine)) return;
            ModuleHelper.ComputeModuleEffect(modules, null, machine);
            ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
            RunNight(machine, battery, input.childItem, MachineService.CoilCharge(machine, coil.childItem), ReturnFitted());
            Refresh();
        }, Refresh);
        MachineAppearance.WatchCycle(machine, () => PlanNight(machine, battery, input.childItem,
            MachineService.CoilCharge(machine, coil.childItem), ReturnFitted(), out _) == MineralStop.Ready);
        Refresh();
        return machine;
    }

    private sealed record SieveNight(int[] Remaining, int[] Feed, int[] Sorted, int[] Tailings, int[] Deposits);

    private static MineralStop PlanNight(GameItem machine, GameSlotInventory battery, GameItem box, int charge,
        bool returnFitted, out SieveNight night)
    {
        night = null;
        if (!Ready(machine)) return MineralStop.Locked;
        if (ReturnedThisNight(machine)) return MineralStop.SieveSettled;
        bool returning = TailingsQueued(machine);
        var parts = new int[PowderBox.ComponentCount];
        long total = 0;
        if (returning)
        {
            if (!returnFitted) return MineralStop.SieveParts;
            parts = ReadBin(machine, Bins.Length);
            foreach (int part in parts) total += part;
        }
        else
        {
            if (!PowderBox.IsOwnedBox(box)) return MineralStop.NoBox;
            total = PowderBox.Read(box, parts);
        }
        if (total <= 0 || total > (returning ? PowderSieveRules.BinCapacity : PowderColor.Capacity) || Throughput(machine) == 0)
            return MineralStop.NoFeed;
        var batch = WaterHelper.SplitProportionally(parts, Math.Min((int)total, Throughput(machine)));
        var sorted = PowderSieveRules.Sort(batch, Recovery(machine));
        if (sorted == null) return MineralStop.NoRecovery;
        var tailings = new int[parts.Length];
        var deposits = new int[parts.Length];
        long tailingsTotal = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            if ((long)Read(machine, Bins[i]) + sorted[i] > PowderSieveRules.BinCapacity) return MineralStop.Full;
            deposits[i] = batch[i] - sorted[i];
            long after = (long)Read(machine, Tailings[i]) + deposits[i] - (returning ? batch[i] : 0);
            tailingsTotal += after;
            if (tailingsTotal > PowderSieveRules.BinCapacity) return MineralStop.Full;
            tailings[i] = (int)after;
            parts[i] -= batch[i];
        }
        if (!machine.GetTagReadonly(MachineryHelper.IS_MACHINERY_POWERED).GetBool()
            || !WorkshopCycle.CanDrawPower(battery, charge)) return MineralStop.NoPower;
        night = new SieveNight(parts, batch, sorted, tailings, deposits);
        return MineralStop.Ready;
    }

    private static void RunNight(GameItem machine, GameSlotInventory battery, GameItem box, int charge, bool returnFitted)
    {
        var condition = PlanNight(machine, battery, box, charge, returnFitted, out var night);
        if (condition == MineralStop.SieveSettled) return;
        bool returning = TailingsQueued(machine);
        if (condition == MineralStop.Ready && !WorkshopCycle.DrawPower(battery, charge)) condition = MineralStop.NoPower;
        if (condition != MineralStop.Ready)
        {
            MineralNightReport.Save(machine, MineralNightReport.Sieve, condition, recipe: returning ? TailingsRecord : "");
            return;
        }
        for (int i = 0; i < night.Sorted.Length; i++)
        {
            Write(machine, Bins[i], Read(machine, Bins[i]) + night.Sorted[i]);
            Write(machine, Tailings[i], night.Tailings[i]);
        }
        if (returning)
        {
            machine.ModifyTag(ReturnNightTag, tag => tag.Enable().SetLong((long)StoreStation.GetDayCounter() + 1), skipValidate: true);
            machine.DisableTag(ReturnTag, skipValidate: true);
        }
        else PowderBox.CommitComposition(box, night.Remaining);
        MineralNightReport.Save(machine, MineralNightReport.Sieve, condition, 1, charge,
            night.Feed, night.Sorted, returning ? night.Sorted : night.Deposits, returning ? TailingsRecord : "");
        PowderBox.Changed(machine);
        if (!returning) PowderBox.RefreshComposition(box, night.Remaining);
        machine.ValidateShapeState();
    }

    internal static long QualifyingStock(GameItem machine, long[] contributions = null, Action<int, int, long, long> report = null)
    {
        long total = 0;
        for (int i = 0; i < Names.Length; i++)
        {
            var parts = ReadBin(machine, i);
            long amount = 0;
            foreach (int part in parts) amount += part;
            if (amount == 0) continue;
            int purity = PowderClassification.FromParts(parts).PurityBasisPoints;
            long credit = purity >= 8100 ? amount : 0;
            total += credit;
            if (contributions != null) contributions[i] += credit;
            report?.Invoke(i, purity, amount, credit);
        }
        return total;
    }

    internal static int[] ReadBin(GameItem machine, int index)
    {
        var parts = new int[PowderBox.ComponentCount];
        if (index < Bins.Length)
            parts[index] = Read(machine, Bins[index]);
        else
            for (int i = 0; i < parts.Length; i++) parts[i] = Read(machine, Tailings[i]);
        return parts;
    }

    internal static int FeedFactory(GameItem machine, PowderComponent component, GameItem target, int[] targetParts,
        int reserve, int needed)
    {
        if (!Ready(machine) || !PowderBox.IsOwnedBox(target)) return 0;
        int index = (int)component;
        int before = Read(machine, Bins[index]);
        int space = PowderColor.Capacity;
        foreach (int part in targetParts) space -= part;
        int amount = Math.Min(Math.Min(before - reserve, space), needed);
        if (amount <= 0) return 0;
        targetParts[index] += amount;
        PowderBox.CommitComposition(target, targetParts);
        Write(machine, Bins[index], before - amount);
        PowderBox.Changed(machine);
        HookGuard.Run("刷新折叠粉料盒", () => PowderBox.RefreshComposition(target, targetParts));
        return amount;
    }
}
