using System;
using System.Linq;

namespace CorpPresence;

internal static class IndustrialPump
{
    internal const string Key = "industrial_pump";
    internal enum Test { Flow, Restart, Pressure, WetContact }
    internal sealed record Rating(int Charge, int Volume); // Battery units and microlitres.
    private enum Stop { Ready, Cover, Parts, ScoredSleeve, Oil, Battery, Feed, Turbid, Receiver }
    private sealed record WetRun(Rating Rating, GameItem Source, GameItem Receiver,
        int[] SourceBefore, int[] SourceAfter, int[] ReceiverBefore, int[] ReceiverAfter);

    private sealed class Pump
    {
        internal readonly GameSlotInventory[] Parts = new GameSlotInventory[PartKeys.Length];
        internal readonly GameSlotInventory Battery = new();
        internal readonly GameSlotInventory Source = new(4, 4) { mayInventoryAddItemFunc = (item, _) => IsVessel(item) };
        internal readonly GameSlotInventory Receiver = new(4, 4) { mayInventoryAddItemFunc = (item, _) => IsVessel(item) };
        internal bool Open;

        internal Pump()
        {
            for (int i = 0; i < Parts.Length; i++)
            {
                string key = PartKeys[i];
                Parts[i] = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) => MachineService.IsStockPart(item, key) };
            }
        }
    }

    private static readonly string[] PartKeys =
        { RodMaterial.FeedShaft, RodMaterial.FeedSleeve, RodMaterial.IndustrialCoil, RodMaterial.FilterBasket,
            RodMaterial.PressureShell, RodMaterial.WetContact };
    private static readonly string[] ReportTags =
        { "CORP_PUMP_TRIAL_FLOW", "CORP_PUMP_TRIAL_RESTART", "CORP_PUMP_TRIAL_PRESSURE", "CORP_PUMP_TRIAL_WET_CONTACT" };
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GameItem, Pump> Pumps = new();
    private const int BaseCharge = 12;
    private const int TestVolume = 40_000;
    private const int BaseParts = 4, ShellIndex = 4, ContactIndex = 5;

    internal static readonly Goods.Def Definition = new()
    {
        Name = "工业试水泵", NameEn = "Industrial Test Pump",
        Company = "mineral", Art = "industrial-pump", Fallback = "furnace",
        W = 3, H = 3, Value = 130, Types = new[] { "MACHINE" }, Shell = Build
    };

    internal static bool Learned() => MachineService.Handbook && MeridianIndustrial.Eligible(CorpState.Current);

    internal static bool ReadyForCollection(GameItem item) => Pumps.TryGetValue(item, out var pump) && !pump.Open
        && pump.Battery.childItem == null && pump.Source.childItem == null && pump.Receiver.childItem == null;

    internal static bool PopulateRepairPump(GameItem item, System.Collections.Generic.List<GameItem> staged)
    {
        if (item == null || !Pumps.TryGetValue(item, out var pump)) return false;
        for (int i = 0; i < BaseParts; i++)
        {
            var part = MakeRecoveredPart(i);
            if (part == null) return false;
            staged.Add(part);
            if (!ProductionOutput.TryAddSeparate(pump.Parts[i], part)) return false;
        }
        return true;
    }

    internal static GameItem MakeOldWinding() => MakeRecoveredPart(2);

    private static GameItem MakeRecoveredPart(int i)
    {
        var part = DirectoryMaster.Item("corp_presence_" + PartKeys[i], isOwned: true);
        if (part == null) return null;
        var assay = i < 2 ? new RodMaterial(64, 18, 8, 8, 2) : new RodMaterial(64, 7, 7, 20, 2);
        var mass = i < 2 ? new RodMaterial(28, 8, 4, 4, 1) : new RodMaterial(42, 6, 5, 16, 1);
        mass.Write(part);
        assay.Write(part, RodMaterial.AssayTag);
        if (i == 0) RodCondition.Evaluate(assay, RodProcess.Temper).WriteProcess(part);
        if (i == 1) part.ModifyTag(RodCondition.ScoredSleeveTag, tag => tag.Enable().SetInt(1), skipValidate: true);
        return part;
    }

    private static bool IsVessel(GameItem item) => item?.unitCount == 1 && AlcoholPreparationStation.IsWaterContainer(item);

    // A job can use this read-only qualification without the power or trial vessels present.
    internal static Rating Passed(GameItem item, Test test)
    {
        if (item?.identifier != "corp_presence_" + Key || !Pumps.TryGetValue(item, out var pump)) return null;
        string saved = item.GetTagReadonly(ReportTags[(int)test]).GetString();
        if (string.IsNullOrEmpty(saved)) return null;
        var rating = Rate(pump, test);
        return rating != null && saved == Encode(TestParts(pump, test), rating) ? rating : null;
    }

    private static int PartCount(Test test) => test is Test.Pressure or Test.WetContact ? BaseParts + 1 : BaseParts;

    private static int PartIndex(Test test, int index) => test == Test.WetContact && index == BaseParts ? ContactIndex : index;

    private static GameItem[] TestParts(Pump pump, Test test)
    {
        var items = new GameItem[PartCount(test)];
        for (int i = 0; i < items.Length; i++) items[i] = pump.Parts[PartIndex(test, i)].childItem;
        return items;
    }

    private static Rating Rate(Pump pump, Test test)
    {
        if (RodCondition.IsScoredSleeve(pump.Parts[1].childItem)) return null;
        for (int i = 0; i < PartCount(test); i++)
        {
            int index = PartIndex(test, i);
            if (!MachineService.IsStockPart(pump.Parts[index].childItem, PartKeys[index])) return null;
        }
        var shaft = RodCondition.Read(pump.Parts[0].childItem);
        if (test == Test.Restart && (shaft.Traits & RodTrait.Fatigue) == 0)
            return null;
        if (test == Test.Pressure && !PressureStock(RodCondition.Read(pump.Parts[ShellIndex].childItem)))
            return null;

        int charge = (RodCondition.Read(pump.Parts[2].childItem).Traits & RodTrait.LowLoss) != 0 ? 9 : BaseCharge;
        if (test == Test.WetContact)
        {
            var contact = RodCondition.Read(pump.Parts[ContactIndex].childItem);
            if (!WetPumpContact.Qualified(contact) || RodCondition.Read(pump.Parts[3].childItem).Surface.Corrosion < 45)
                return null;
            return new(charge + ((contact.Traits & RodTrait.LowLoss) != 0 ? 1 : 3), TestVolume);
        }
        return test switch
        {
            Test.Restart => new(charge + (shaft.Jacket?.Material.PowderPoints >= 6 && shaft.Surface.Wear >= 65 ? 4 : 6), 2 * TestVolume),
            Test.Pressure => new(charge + 4, TestVolume),
            _ => new(charge, TestVolume)
        };
    }

    internal static bool PressureStock(RodCondition stock) =>
        stock?.Core.Load >= 50 && stock.Surface.Corrosion >= 35 && stock.Surface.Porosity <= 3;

    private static string Encode(GameItem[] parts, Rating rating)
    {
        var fields = new string[8];
        fields[0] = "1";
        fields[1] = FormattableString.Invariant($"{rating.Charge}");
        fields[2] = FormattableString.Invariant($"{rating.Volume}");
        fields[^1] = "";
        for (int i = 0; i < parts.Length; i++)
        {
            var item = parts[i];
            if (item.uniqueId <= 0) return null;
            string mass = item.GetTagReadonly(RodMaterial.Tag).GetString();
            string assay = item.GetTagReadonly(RodMaterial.AssayTag).GetString();
            string process = item.GetTagReadonly(RodCondition.ProcessTag).GetString();
            fields[i + 3] = FormattableString.Invariant($"{item.uniqueId}:{mass}:{assay}:{process}");
            if (RodJacket.HasTags(item))
                fields[i + 3] += ":" + item.GetTagReadonly(RodJacket.MassTag).GetString()
                    + ":" + item.GetTagReadonly(RodJacket.AssayTag).GetString() + ":" + item.GetTagReadonly(RodJacket.ProcessTag).GetString();
        }
        return string.Join(";", fields);
    }

    private static Stop Plan(Pump pump, Test test, out WetRun plan, bool checkPower = true)
    {
        plan = null;
        if (pump.Open) return Stop.Cover;
        if (RodCondition.IsScoredSleeve(pump.Parts[1].childItem)) return Stop.ScoredSleeve;
        var rating = Rate(pump, test);
        if (rating == null) return Stop.Parts;
        if (test == Test.Restart && SleeveOiling.Oil(pump.Parts[1].childItem) == 0) return Stop.Oil;
        if (checkPower && !WorkshopCycle.CanDrawPower(pump.Battery, rating.Charge)) return Stop.Battery;

        var source = pump.Source.childItem;
        var receiver = pump.Receiver.childItem;
        if (!IsVessel(source)) return Stop.Feed;
        var from = WaterHelper.GetLiquidParts(source);
        int water = Liquid.Liquids.FindIndex(liquid => liquid.identifier == "water");
        int dirt = Liquid.Liquids.FindIndex(liquid => liquid.identifier == "physical_contaminant");
        long total = from.Sum(part => (long)part);
        if (water < 0 || from.Any(part => part < 0) || total < rating.Volume)
            return Stop.Feed;
        if (test == Test.WetContact)
        {
            if (dirt < 0 || (long)from[water] + from[dirt] != total
                || 4L * from[water] < 3L * total || 5L * from[water] >= 4L * total) return Stop.Feed;
        }
        else if (5L * from[water] < 4L * total) return Stop.Feed;
        if (dirt >= 0 && 20L * from[dirt] > total && RodCondition.Read(pump.Parts[3].childItem).Surface.Corrosion < 35)
            return Stop.Turbid;
        if (!IsVessel(receiver) || ReferenceEquals(source, receiver) || WaterHelper.GetFreeCapacity(receiver) < rating.Volume)
            return Stop.Receiver;
        var to = WaterHelper.GetLiquidParts(receiver);
        if (to.Any(part => part < 0)) return Stop.Receiver;
        var moved = WaterHelper.SplitProportionally(from, rating.Volume);
        var fromAfter = new int[from.Length];
        var toAfter = new int[to.Length];
        for (int i = 0; i < moved.Length; i++)
        {
            if (moved[i] < 0 || moved[i] > from[i] || (long)to[i] + moved[i] > int.MaxValue) return Stop.Receiver;
            fromAfter[i] = from[i] - moved[i];
            toAfter[i] = to[i] + moved[i];
        }
        plan = new(rating, source, receiver, from, fromAfter, to, toAfter);
        return Stop.Ready;
    }

    internal static bool ReadyForCommissioning(GameItem machine) => machine != null && Pumps.TryGetValue(machine, out var pump)
        && !pump.Open && Passed(machine, Test.Restart) != null && Rate(pump, Test.Pressure) != null;

    // The station owns the enclosing workshop transaction during a field shift.
    internal static bool TryCommissioningShift(GameItem machine, bool pressure, Func<bool> dockIsCurrent = null)
    {
        if (!Pumps.TryGetValue(machine, out var pump)) return false;
        var test = pressure ? Test.Pressure : Test.Flow;
        if (!pressure && Plan(pump, Test.Flow, out _) == Stop.Feed
            && Plan(pump, Test.WetContact, out _) == Stop.Ready) test = Test.WetContact;
        return Run(machine, pump, test, dockIsCurrent);
    }

    private static bool Run(GameItem machine, Pump pump, Test test, Func<bool> dockIsCurrent = null)
    {
        if (!ShopInventory.Contains(machine) || Plan(pump, test, out var plan) != Stop.Ready) return false;
        var parts = TestParts(pump, test);
        foreach (var part in parts) part.GetUniqueID();
        string record = Encode(parts, plan.Rating);
        var contact = test == Test.WetContact ? RodCondition.Read(parts[^1]) : null;
        if (record == null || !WorkshopCycle.DrawForCurrentStock(pump.Battery, plan.Rating.Charge, StockIsCurrent)) return false;

        AlcoholPreparationStation.WriteParts(plan.Source, plan.SourceBefore, plan.SourceAfter);
        AlcoholPreparationStation.WriteParts(plan.Receiver, plan.ReceiverBefore, plan.ReceiverAfter);
        if (test == Test.Restart) SleeveOiling.Consume(parts[1]);
        if (contact != null)
        {
            WetPumpContact.Wear(parts[^1], contact);
            record = Encode(parts, plan.Rating);
        }
        machine.ModifyTag(ReportTags[(int)test], tag => tag.Enable().SetString(record), skipValidate: true);
        HookGuard.Run("刷新试水凭据", machine.ValidateShapeState);
        HookGuard.Run("刷新试水进液", () => AlcoholPreparationStation.RefreshLiquid(plan.Source));
        HookGuard.Run("刷新试水回液", () => AlcoholPreparationStation.RefreshLiquid(plan.Receiver));
        return true;

        bool StockIsCurrent() => ShopInventory.Contains(machine) && (dockIsCurrent?.Invoke() ?? true)
            && Plan(pump, test, out var current, checkPower: false) == Stop.Ready
            && current.Rating == plan.Rating && current.Source == plan.Source && current.Receiver == plan.Receiver
            && current.SourceBefore.SequenceEqual(plan.SourceBefore) && current.ReceiverBefore.SequenceEqual(plan.ReceiverBefore)
            && TestParts(pump, test).SequenceEqual(parts) && Encode(parts, plan.Rating) == record;
    }

    private static GameItem Build()
    {
        var pump = new Pump();
        var status = new MachineryText(uppercase: false);
        var flow = new MachineryButton(L.K("workshop.pump.flow", "普通试水", "Flow test"));
        var restart = new MachineryButton(L.K("workshop.pump.restart", "连续起停", "Repeated starts"));
        var pressure = new MachineryButton(L.K("workshop.pump.pressure", "加压试水", "Pressure test"));
        var close = new MachineryButton(L.K("workshop.pump.close", "合上检修盖", "Close cover"));
        var wet = new MachineryButton(L.K("workshop.pump.wetContact", "高杂质试水", "High-grit test"));
        var covers = new[] {
            MachineHelper.Panel(Goods.NameOf(RodMaterial.FeedShaft), pump.Parts[0], "shaft"),
            MachineHelper.Panel(Goods.NameOf(RodMaterial.FeedSleeve), pump.Parts[1], "sleeve"),
            MachineHelper.Panel(Goods.NameOf(RodMaterial.IndustrialCoil), pump.Parts[2], "coil"),
            MachineHelper.Panel(Goods.NameOf(RodMaterial.FilterBasket), pump.Parts[3], "basket"),
            MachineHelper.Panel(Goods.NameOf(RodMaterial.PressureShell), pump.Parts[ShellIndex], "shell"),
            MachineHelper.Panel(Goods.NameOf(RodMaterial.WetContact), pump.Parts[ContactIndex], "wet_contact")
        };
        var window = new PixelWindow(true, null, WindowStyle.Machinery);
        window.Attach(new MachineryPanelRow(
            MachineHelper.PowerPanel(pump.Battery),
            MachineHelper.Panel(L.K("production.alcohol.waterIn", "进水容器", "Water container"), pump.Source, "water"),
            MachineHelper.Panel(L.K("workshop.pump.receiver", "接液容器", "Receiving vessel"), pump.Receiver, "receiver"),
            covers[0], covers[1], covers[2], covers[3], covers[4],
            MachineHelper.Panel(L.K("workshop.pump.tests", "带载试水", "Wet tests"), MachineryStack.Vertical(status, flow, restart, pressure, close, wet), "control"),
            covers[5])
            .SetColumns(2).SetDisplayOrder(0, 8, 1, 2, 3, 4, 5, 6, 7, 9));
        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");
        MachineHelper.SetupBatterySlot(pump.Battery, machine);
        MachineryHelper.InitMachinery(machine, BaseCharge);
        Pumps.Add(machine, pump);
        MachineService.Register(machine, () => machine.identifier == "corp_presence_" + Key, SetOpen);
        SetOpen(false);
        close.SetCallback(() => MachineService.Close(machine));
        flow.SetCallback(() => Operate(Test.Flow));
        restart.SetCallback(() => Operate(Test.Restart));
        pressure.SetCallback(() => Operate(Test.Pressure));
        wet.SetCallback(() => Operate(Test.WetContact));
        foreach (var slot in pump.Parts.Append(pump.Battery).Append(pump.Source).Append(pump.Receiver))
        {
            slot.onSlotAddItemEndFunc += (_, _, _) => Refresh();
            slot.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
            slot.onSlotChangeAmountItemFunc += (_, _, _) => Refresh();
        }
        window.openCallback += () => { MachineService.OpenWindow(machine); Refresh(); };
        window.closeCallback += () => MachineService.Close(machine);
        return machine;

        void SetOpen(bool visible)
        {
            pump.Open = visible;
            foreach (var panel in covers) panel.SetHidden(!visible);
            Refresh();
        }

        void Operate(Test test)
        {
            WorkshopCycle.Run(machine, () => Run(machine, pump, test));
            Refresh();
        }

        void Refresh()
        {
            string summary = string.Join("\n", Line(Test.Flow, L.K("workshop.pump.flowShort", "流量", "Flow")),
                Line(Test.Restart, L.K("workshop.pump.restartShort", "起停", "Restarts")), Line(Test.Pressure, L.K("workshop.pump.pressureShort", "加压", "Pressure")));
            if (pump.Parts[ContactIndex].childItem is { } item)
            {
                summary += "\n" + Line(Test.WetContact, L.K("workshop.pump.wetShort", "高杂质", "High grit"));
                summary += "\n" + L.F("workshop.pump.contactWear", "触点剩余外套 {0:0.#} 点", "Contact jacket remaining {0:0.#} points",
                    RodCondition.Read(item)?.Jacket?.Material.PowderPoints ?? 0);
            }
            status.SetText(summary);
        }

        string Line(Test test, string label)
        {
            var result = Passed(machine, test);
            var rating = Rate(pump, test);
            string report = result == null ? "" : L.K("workshop.pump.certified", " · 有效凭据", " · Certified");
            string cost = rating == null ? "" : L.F("workshop.pump.cost", "{0} mL／{1} 电 · ", "{0} mL / {1} charge · ",
                rating.Volume / 1000, rating.Charge);
            return label + " · " + cost + Reason(Plan(pump, test, out _), test) + report;
        }
    }

    private static string Reason(Stop stop, Test test) => stop switch
    {
        Stop.Cover => L.K("workshop.pump.coverFirst", "先合盖", "Close cover first"),
        Stop.ScoredSleeve => L.K("workshop.pump.scoredSleeve", "轴套拉伤。用螺丝刀开盖更换。", "The sleeve is scored. Open the cover with a screwdriver to replace it."),
        Stop.Parts when test == Test.Restart => L.K("workshop.pump.needRestart", "须装齐部件与耐疲劳轴", "Needs all parts and a fatigue-rated shaft"),
        Stop.Parts when test == Test.Pressure => L.K("workshop.pump.needPressure", "须装齐部件与承压壳", "Needs all parts and a pressure shell"),
        Stop.Parts when test == Test.WetContact => L.K("workshop.pump.needContact", "须有耐蚀 ≥45 的滤篮和外套完好的耐蚀触点",
            "Needs a basket rated ≥45 for corrosion and a wet contact with its jacket intact"),
        Stop.Parts => L.K("workshop.pump.needParts", "须装齐轴、轴套、线圈、滤篮", "Install shaft, sleeve, coil and basket"),
        Stop.Oil => L.K("workshop.pump.oil", "轴套须浸油", "Oil the sleeve"),
        Stop.Battery => L.K("workshop.pump.battery", "电量不足", "Insufficient charge"),
        Stop.Feed when test == Test.WetContact => L.K("workshop.pump.highGritFeed", "至少 40 mL，仅含水与超过 20%、至多 25% 的物理杂质",
            "At least 40 mL of water and grit only, with over 20% and at most 25% grit"),
        Stop.Feed => L.K("workshop.pump.needFeed", "试液不足或水含量低于 80%", "Too little feed or below 80% water"),
        Stop.Turbid => L.K("workshop.pump.turbid", "浑水需耐蚀 ≥35 的滤篮", "Turbid feed requires basket corrosion resistance ≥35"),
        Stop.Receiver => L.K("workshop.pump.needReceiver", "接液容器缺失或空间不足", "Receiving vessel missing or full"),
        _ => L.K("workshop.pump.ready", "可试水", "Ready")
    };
}
