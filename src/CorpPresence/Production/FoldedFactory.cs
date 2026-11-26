using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CorpPresence;

internal static class FoldedFactory
{
    internal const string Key = "folded_factory";
    private const string MadeRunTag = "CORP_FOLD_MADE_RUN";
    private const string CycleTag = "CORP_FOLD_NIGHT", ModeTag = "CORP_FOLD_MODE", ReserveTag = "CORP_FOLD_RESERVE";
    private const string ConductiveTag = "CORP_FOLD_HOLD_CONDUCTIVE", PickupTag = "CORP_FOLD_PICKUP", ReportTag = "CORP_FOLD_REPORT";
    private enum Priority { Paused, Glass, Sort }
    private sealed record Ports(string Label, Action Process, GameSlotInventory Powder = null,
        GameSlotInventory[] Inputs = null, Func<SinteringRecipes.Recipe> Recipe = null, GameGridInventory Output = null,
        Action Refresh = null);
    private sealed record Bays(GameSlotInventory Crusher, GameSlotInventory Sieve, GameSlotInventory Furnace,
        GameGridInventory Pickup, Action Refresh);

    private static readonly ConditionalWeakTable<GameItem, Ports> Machines = new();
    private static readonly ConditionalWeakTable<GameItem, Bays> Controllers = new();
    private static readonly int[] Reserves = { 0, 30, 60, 120 };

    internal static readonly Goods.Def Definition = new()
    {
        Name = "折叠工厂", NameEn = "Folded Factory", Value = 450,
        Company = "mineral", Art = "folded-factory", Fallback = "furnace",
        W = 3, H = 3, Types = new[] { "MACHINE" }, Shell = Build
    };

    internal static bool AcceptMaterial(GameItem item, string key) => key switch
    {
        MeridianCoreIntake.TimingCore => CoreCalibration.IsVerified(item, IndustrialCoreKind.Timing),
        RodMaterial.PressureShell => MachineService.IsStockPart(item, key) && IndustrialPump.PressureStock(RodCondition.Read(item)),
        RodMaterial.FeedShaft => MachineService.IsStockPart(item, key) && (RodCondition.Read(item).Traits & RodTrait.Fatigue) != 0,
        RodMaterial.IndustrialCoil => MachineService.IsStockPart(item, key),
        _ => false
    };

    internal static bool IsFoldedPlate(GameItem plate) => IsPlate(plate) && GeneralHelper.IsItemOwned(plate)
        && !StorePlacement.Destroyed(plate) && CorpState.Current?.RunId is { Length: > 0 } run
        && plate.GetTagReadonly(MadeRunTag).GetString() == run;

    internal static void PreservePlate(GameItem item)
    {
        if (HasFoldedStamp(item)) item.mayStackItemFunc = null;
    }

    internal static bool HasFoldedStamp(GameItem item) =>
        IsPlate(item) && !string.IsNullOrEmpty(item.GetTagReadonly(MadeRunTag).GetString());

    private static bool IsPlate(GameItem item) =>
        item?.identifier == "corp_presence_" + CraftingStation.SiliconPlate && item.unitCount == 1;

    internal static void RegisterCrusher(GameItem machine, GameSlotInventory receiver, Action process) =>
        Register(machine, new("矿粉破碎", process, Powder: receiver));

    internal static void RegisterSieve(GameItem machine, GameSlotInventory inlet, Action process, Action refresh) =>
        Register(machine, new("过夜筛粉", process, Powder: inlet, Refresh: refresh));

    internal static void RegisterFurnace(GameItem machine, GameSlotInventory[] inputs, Func<SinteringRecipes.Recipe> recipe,
        GameGridInventory output, Action process) =>
        Register(machine, new("过夜烧结", process, Inputs: inputs, Recipe: recipe, Output: output));

    private static void Register(GameItem machine, Ports ports)
    {
        Machines.Add(machine, ports);
        machine.onCycleEndSlotItemFunc = (_, _, _) => HookGuard.Run(ports.Label, () =>
        {
            long night = (long)StoreStation.GetDayCounter() + 1;
            if (night > 0 && machine.GetTagReadonly(CycleTag).GetLong() == night) return;
            var bay = machine.parentInventory;
            var controller = bay?.GetParentItem();
            if (controller != null && Controllers.TryGetValue(controller, out var docks) && Mode(controller) != Priority.Paused
                && (bay == docks.Crusher || bay == docks.Sieve || bay == docks.Furnace) && ShopInventory.Contains(controller))
                RunNight(controller, docks);
            else WorkshopCycle.Run(machine, () =>
            {
                if (night > 0) Stamp(machine, -night);
                ports.Process();
            });
        });
    }

    private static bool IsMachine(GameItem item, string key) => item?.identifier == "corp_presence_" + key
        && item.unitCount == 1 && GeneralHelper.IsItemOwned(item) && !StorePlacement.Destroyed(item);

    private static Ports GetPorts(GameItem machine) => machine != null && machine.unitCount == 1
        && GeneralHelper.IsItemOwned(machine) && !StorePlacement.Destroyed(machine)
        && Machines.TryGetValue(machine, out var ports) ? ports : null;

    private static Priority Mode(GameItem factory) => factory.GetTagReadonly(ModeTag).GetInt() switch
    {
        1 => Priority.Glass,
        2 => Priority.Sort,
        _ => Priority.Paused
    };

    private static int Reserve(GameItem factory)
    {
        int value = factory.GetTagReadonly(ReserveTag).GetInt();
        return Array.IndexOf(Reserves, value) < 0 ? 0 : value;
    }

    private static void RunNight(GameItem factory, Bays bays)
    {
        WorkshopCycle.Run(factory, () =>
        {
            var mode = Mode(factory);
            long night = (long)StoreStation.GetDayCounter() + 1;
            if (mode == Priority.Paused || night <= 0 || factory.GetTagReadonly(CycleTag).GetLong() == night
                || !ShopInventory.Contains(factory)) return;
            Stamp(factory, night);
            var session = StoreSession.Capture();
            var crusher = bays.Crusher.childItem;
            var sieve = bays.Sieve.childItem;
            var furnace = bays.Furnace.childItem;
            var crusherPorts = GetPorts(crusher);
            var sievePorts = GetPorts(sieve);
            var furnacePorts = GetPorts(furnace);
            int reserve = Reserve(factory);
            bool hold = factory.GetTagReadonly(ConductiveTag).GetBool();
            bool pickup = factory.GetTagReadonly(PickupTag).GetBool();
            int direct = 0, sorted = 0, toKiln = 0, collected = 0;

            bool Present(GameSlotInventory slot, GameItem item) =>
                session.IsCurrent && item != null && ReferenceEquals(slot.childItem, item) && ShopInventory.Contains(factory);

            if (Present(bays.Crusher, crusher)) RunMachine(crusher, crusherPorts, night);
            if (crusherPorts?.Powder?.childItem is { } supply && Present(bays.Crusher, crusher))
            {
                var powder = Classify(supply);
                var silica = Present(bays.Furnace, furnace) ? furnacePorts?.Recipe()?.Ingredients
                    .FirstOrDefault(ingredient => ingredient.Component == PowderComponent.Silica) : null;
                if (mode == Priority.Glass && silica != null && Qualifies(powder, silica))
                    direct = FeedSilica(supply, furnacePorts.Inputs, silica, reserve,
                        () => Present(bays.Crusher, crusher) && Present(bays.Furnace, furnace) && crusherPorts.Powder.childItem == supply);
                else if (!(hold && powder.Branch == PowderComponent.Conductive) && Present(bays.Sieve, sieve)
                    && PowderSieve.AcceptsFactoryFeed(sieve)
                    && sievePorts?.Powder?.childItem is { } inlet)
                {
                    var stock = PowderBox.InspectTransfer(supply, inlet);
                    if (stock != null) sorted = Move(stock, Math.Min(stock.Limit, stock.From.Sum() - reserve));
                }
            }

            if (Present(bays.Sieve, sieve)) RunMachine(sieve, sievePorts, night);
            if (Present(bays.Furnace, furnace) && furnacePorts != null)
            {
                bool FurnacePresent() => Present(bays.Furnace, furnace);
                if (pickup) collected += Collect(furnacePorts.Output, bays.Pickup, FurnacePresent);
                if (FurnacePresent() && Present(bays.Sieve, sieve) && sievePorts != null && furnacePorts.Recipe() is { } recipe)
                    toKiln = FeedBins(sieve, sievePorts.Refresh, furnacePorts.Inputs, recipe, reserve, hold,
                        () => FurnacePresent() && Present(bays.Sieve, sieve));
                if (FurnacePresent())
                {
                    var before = furnacePorts.Output.childItems.ToHashSet();
                    RunMachine(furnace, furnacePorts, night);
                    if (FurnacePresent() && CorpState.Current?.RunId is { Length: > 0 } run)
                        foreach (var plate in furnacePorts.Output.childItems.Where(item => !before.Contains(item) && IsPlate(item)).ToArray())
                        {
                            plate.ModifyTag(MadeRunTag, tag => tag.Enable().SetString(run), skipValidate: true);
                            plate.mayStackItemFunc = null;
                            HookGuard.Run("刷新折叠硅板产地", plate.ValidateShapeState);
                            Goods.Relabel(plate, CraftingStation.SiliconPlate);
                        }
                }
                if (pickup && FurnacePresent()) collected += Collect(furnacePorts.Output, bays.Pickup, FurnacePresent);
            }
            if (!session.IsCurrent) return;
            string report = FormattableString.Invariant($"{direct}/{sorted}/{toKiln}/{collected}");
            factory.ModifyTag(ReportTag, tag => tag.Enable().SetString(report), skipValidate: true);
            HookGuard.Run("刷新折叠调度实绩", factory.ValidateShapeState);
            bays.Refresh();
        });
    }

    private static void Stamp(GameItem machine, long night)
    {
        machine.ModifyTag(CycleTag, tag => tag.Enable().SetLong(night), skipValidate: true);
        HookGuard.Run("刷新过夜调度记录", machine.ValidateShapeState);
    }

    private static void RunMachine(GameItem machine, Ports ports, long night)
    {
        if (ports == null) return;
        long last = machine.GetTagReadonly(CycleTag).GetLong();
        if (last == night || last == -night) return;
        Stamp(machine, night);
        HookGuard.Run(ports.Label, ports.Process);
    }

    private static PowderClassification Classify(GameItem box)
    {
        var parts = new int[PowderBox.ComponentCount];
        return PowderBox.IsOwnedBox(box) && PowderBox.Read(box, parts) <= PowderColor.Capacity
            ? PowderClassification.FromParts(parts) : default;
    }

    private static bool Qualifies(PowderClassification powder, SinteringIngredient want) =>
        powder.Branch == want.Component && powder.Grade >= want.MinimumGrade;

    private static int Need(GameSlotInventory[] inputs, SinteringIngredient want)
    {
        int have = 0;
        var parts = new int[PowderBox.ComponentCount];
        foreach (var slot in inputs)
        {
            var box = slot.childItem;
            if (PowderBox.IsOwnedBox(box) && PowderBox.Read(box, parts) <= PowderColor.Capacity
                && Qualifies(PowderClassification.FromParts(parts), want))
                have += parts[(int)want.Component];
        }
        return Math.Max(0, want.Points - have);
    }

    private static int FeedSilica(GameItem supply, GameSlotInventory[] inputs, SinteringIngredient want, int reserve, Func<bool> current)
    {
        int moved = 0;
        int needed = Need(inputs, want);
        foreach (var slot in inputs)
        {
            if (needed == 0 || !current()) break;
            var stock = PowderBox.InspectTransfer(supply, slot.childItem);
            if (stock == null) continue;
            var target = PowderClassification.FromParts(stock.To);
            if (target.Grade != PowderGrade.Empty && !Qualifies(target, want)) continue;
            int available = stock.From.Sum() - reserve;
            if (available <= 0) break;
            int limit = Math.Min(stock.Limit, available);
            if (limit == 0) continue;
            int component = (int)want.Component;
            var goal = want with { Points = stock.To[component] + needed };
            int amount = PowderTransfer.ToGoal(stock.From, stock.To, stock.Capacity, goal, WaterHelper.SplitProportionally);
            if (amount < 0 || amount > limit) amount = limit;
            var plan = stock.Plan(amount);
            if (plan == null || !plan.Qualified(want)) continue;
            int sent = Move(stock, amount);
            if (sent == 0) continue;
            moved += sent;
            needed -= Math.Min(needed, plan.Moved[component]);
        }
        return moved;
    }

    private static int Move(PowderBox.TransferStock stock, int amount)
    {
        if (amount <= 0) return 0;
        try { return PowderBox.Transfer(stock, amount) ? amount : 0; }
        catch (Exception error)
        {
            ModLog.Warn("折叠转料刷新中断：" + error.GetType().Name);
            var expected = stock.Plan(amount);
            var actual = PowderBox.InspectTransfer(stock.Source, stock.Target);
            return expected != null && actual != null && expected.SourceAfter.SequenceEqual(actual.From)
                && expected.TargetAfter.SequenceEqual(actual.To) ? amount : 0;
        }
    }

    private static int FeedBins(GameItem sieve, Action refresh, GameSlotInventory[] inputs,
        SinteringRecipes.Recipe recipe, int reserve, bool hold, Func<bool> current)
    {
        int moved = 0;
        foreach (var want in recipe.Ingredients)
        {
            if (!current()) break;
            if (hold && want.Component == PowderComponent.Conductive) continue;
            int needed = Need(inputs, want);
            foreach (var slot in inputs)
            {
                if (needed == 0 || !current()) break;
                var box = slot.childItem;
                var parts = new int[PowderBox.ComponentCount];
                if (!PowderBox.IsOwnedBox(box) || PowderBox.Read(box, parts) >= PowderColor.Capacity) continue;
                var stock = PowderClassification.FromParts(parts);
                if (stock.Grade != PowderGrade.Empty && !Qualifies(stock, want)) continue;
                int sent = PowderSieve.FeedFactory(sieve, want.Component, box, parts, reserve, needed);
                needed -= sent;
                moved += sent;
            }
        }
        if (moved > 0 && current()) HookGuard.Run("刷新折叠工厂筛分仓", refresh);
        return moved;
    }

    private static int Collect(GameGridInventory source, GameGridInventory target, Func<bool> current)
    {
        int moved = 0;
        foreach (var item in source.childItems.ToArray())
        {
            if (!current()) break;
            if (item.parentInventory != source || !item.MayRemove() || item.MaxNumRemove() < item.unitCount) continue;
            if (ProductionOutput.TryAddSeparate(target, item)) moved++;
            else if (item.parentInventory == null && !StorePlacement.Destroyed(item))
                ProductionOutput.TryAddSeparate(source, item);
        }
        return moved;
    }

    private static GameItem Build()
    {
        var crusher = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) =>
            IsMachine(item, MineralCrusher.Key) || IsMachine(item, MineralCrusher.FeederKey) };
        var sieve = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) => IsMachine(item, PowderSieve.Key) };
        var furnace = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) => IsMachine(item, SinteringStation.Key) };
        var pickup = new GameGridInventory(9, 6) { mayInventoryAddItemFunc = (_, _) => false };
        var summary = new MachineryText(uppercase: false);
        var priority = new MachineryButton();
        var reserve = new MachineryButton();
        var conductive = new MachineryButton();
        var destination = new MachineryButton();
        var window = new PixelWindow(true, null, WindowStyle.Machinery);
        window.Attach(new MachineryPanelRow(
            MachineHelper.Panel(Goods.NameOf(MineralCrusher.Key), crusher, "crusher"),
            MachineHelper.Panel(Goods.NameOf(PowderSieve.Key), sieve, "sieve"),
            MachineHelper.Panel(Goods.NameOf(SinteringStation.Key), furnace, "furnace"),
            MachineHelper.Panel(L.K("workshop.fold.pickup", "提货区", "Pickup"), pickup, "pickup"),
            MachineHelper.Panel(L.K("workshop.fold.control", "夜班调度", "Night scheduling"),
                MachineryStack.Vertical(summary, priority, reserve, conductive, destination), "control"))
            .SetColumns(2).SetDisplayOrder(4, 0, 1, 2, 3));
        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");
        var bays = new Bays(crusher, sieve, furnace, pickup, Refresh);
        Controllers.Add(machine, bays);
        machine.onCycleEndSlotItemFunc = (_, _, _) => HookGuard.Run("折叠工厂夜班", () => RunNight(machine, bays));
        priority.SetCallback(() => Configure(() => machine.ModifyTag(ModeTag, tag => tag.Enable().SetInt(((int)Mode(machine) + 1) % 3))));
        reserve.SetCallback(() => Configure(() => machine.ModifyTag(ReserveTag, tag =>
            tag.Enable().SetInt(Reserves[(Array.IndexOf(Reserves, Reserve(machine)) + 1) % Reserves.Length]))));
        conductive.SetCallback(() => Toggle(ConductiveTag));
        destination.SetCallback(() => Toggle(PickupTag));
        window.openCallback += Refresh;
        return machine;

        void Configure(Action change)
        {
            WorkshopCycle.Run(machine, change);
            Refresh();
        }

        void Toggle(string key) => Configure(() =>
            machine.ModifyTag(key, tag => tag.Enable().SetBool(!machine.GetTagReadonly(key).GetBool())));

        void Refresh()
        {
            priority.SetText(Mode(machine) switch
            {
                Priority.Glass => L.K("workshop.fold.glass", "合格硅粉直送优先", "Direct qualifying silica"),
                Priority.Sort => L.K("workshop.fold.sort", "分类优先", "Sort first"),
                _ => L.K("workshop.fold.paused", "调度暂停", "Scheduling paused")
            });
            reserve.SetText(L.F("workshop.fold.reserve", "每来源保留 {0} 点", "Reserve {0} points per source", Reserve(machine)));
            conductive.SetText(machine.GetTagReadonly(ConductiveTag).GetBool()
                ? L.K("workshop.fold.hold", "保留导电粉", "Reserve conductive powder")
                : L.K("workshop.fold.allow", "允许调度导电粉", "Route conductive powder"));
            destination.SetText(machine.GetTagReadonly(PickupTag).GetBool()
                ? L.K("workshop.fold.toPickup", "成品到提货区", "Products to pickup")
                : L.K("workshop.fold.local", "成品留烧结站", "Products stay at the kiln"));
            string report = machine.GetTagReadonly(ReportTag).GetString();
            summary.SetText(L.K("workshop.fold.setup", "装入原机并备好电池和粉盒。", "Dock existing machines with their own batteries and boxes.")
                + (string.IsNullOrEmpty(report) ? "" : "\n" + L.F("workshop.fold.report",
                    "上夜直送／送筛／粉仓补料／提货  {0}", "Last night: direct / to sieve / bin feed / pickup  {0}", report)));
        }
    }
}
