using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static partial class MineralGrowthStation
{
    internal const string Key = "mineral_growth_station";
    private const string RecipeTag = "CORP_MINERAL_GROWTH_RECIPE";
    private const string ProgressTag = "CORP_MINERAL_GROWTH_PROGRESS";
    private const string LiquidContainer = "LIQUID_CONTAINER_TAG";

    internal static bool HasActiveBatch(GameItem machine) => machine?.identifier == "corp_presence_" + Key
        && (MineralGrowthBatch.Find(machine.GetTagReadonly(RecipeTag).GetString()) != null || HasProfile(machine));

    internal static (bool Active, MineralGrowthBatch.Recipe Recipe, int Nights, int Charge, int Water) ReadQueue(GameItem machine)
    {
        string key = machine.GetTagReadonly(RecipeTag).GetString();
        if (string.IsNullOrEmpty(key)) return HasProfile(machine) ? (true, null, -1, 0, 0) : (false, null, 0, 0, 0);
        var recipe = MineralGrowthBatch.Find(key);
        int progress = machine.GetTagReadonly(ProgressTag).GetInt();
        var rate = LockedRate(machine, recipe);
        var due = ReadDue(machine);
        if (recipe == null || rate == null || progress < 0 || progress > MineralGrowthBatch.RequiredWork
            || !ValidDue(rate, due, progress) || rate.Work <= 0 && progress < MineralGrowthBatch.RequiredWork)
            return (true, null, -1, 0, 0);
        int nights = progress == MineralGrowthBatch.RequiredWork ? 0 :
            1 + (MineralGrowthBatch.RequiredWork - progress - 1) / rate.Work;
        return (true, recipe, nights, (nights - (due == NightDue.None ? 0 : 1)) * rate.Charge, nights * rate.Water);
    }

    internal static readonly Goods.Def Definition = new()
    {
        Name = "矿化机", NameEn = "Mineral Growth Station", Value = 320,
        Company = "mineral", Art = "mineral-growth-station", Fallback = "hydroponic", W = 3, H = 4,
        Types = new[] { "MACHINE" }, Shell = Build
    };

    internal static readonly Goods.Def OreBlueprint = Blueprint("普通矿石蓝图", "Common Ore Blueprint", "mineral-ore-blueprint");
    internal static readonly Goods.Def LightSaltBlueprint = Blueprint("光盐芯蓝图", "Light Salt Core Blueprint", "light-salt-blueprint");

    static MineralGrowthStation()
    {
        LightSaltBlueprint.Desc = "光盐芯培养配方（基础）\n主粉盒：40 点渣料。副粉盒：10 点稀有金属粉。两盒对应成分各占至少 81%。\n供水供电两个有效夜晚，每晚 20 mL 水、6 电，收获 2 枚光盐芯。蓝图可重复使用。";
        LightSaltBlueprint.DescEn = "Light salt core recipe (base values)\nPrimary box: 40 points of residue. Secondary box: 10 points of rare metal powder. Each must be at least 81% pure.\nSupply 20 mL of water and 6 charge per night for two active nights to grow two cores. Reusable blueprint.";
    }

    private static Goods.Def Blueprint(string name, string english, string art) => new()
    {
        Name = name, NameEn = english, Value = 35, Company = "mineral", Art = art, Shape = "#",
        Fallback = "printer_chip_10_mag", W = 1, H = 1, Shell = () => ItemDirectory.CreateEmptyItem()
    };

    private sealed record GrowthInputs(GameItem Water, GameItem Main, GameItem Secondary, int[] MainAfter, int[] SecondaryAfter);

    private static GameItem Build()
    {
        var battery = new GameSlotInventory();
        var water = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) => IsWaterContainer(item) };
        var main = PowderSlot();
        var secondary = PowderSlot();
        var blueprint = new GameSlotInventory(2, 2)
        {
            mayInventoryAddItemFunc = (item, _) => MineralGrowthBatch.Find(Goods.IdOf(item)) != null
        };
        var output = new GameGridInventory(9, 6) { mayInventoryAddItemFunc = (_, _) => false };
        var modules = MineralModuleBay.Create();
        var coil = new GameSlotInventory(2, 2) { mayInventoryAddItemFunc = (item, _) => MachineService.Handbook
            && MachineService.IsStockPart(item, RodMaterial.IndustrialCoil) };
        var guide = new GameSlotInventory(2, 2) { mayInventoryAddItemFunc = (item, _) => MachineService.Handbook && IsGuide(item) };
        var coilPanel = MachineHelper.Panel(Goods.NameOf(RodMaterial.IndustrialCoil), coil, "service_coil").SetHidden(true);
        var guidePanel = MachineHelper.Panel(Goods.NameOf(RodMaterial.GrowthGuide), guide, "service_guide").SetHidden(true);
        var status = new MachineryText(uppercase: false);
        var closeCover = new MachineryButton(L.T("合上检修盖", "Close cover"));
        var window = MachineHelper.CreateMachineryWindow(true, new[] { 0, 6, 8, 9, 1, 2, 3, 4, 5, 7 },
            MachineHelper.PowerPanel(battery),
            MachineHelper.Panel(L.K("production.mineralGrowth.water", "供水", "Water"), water, "water"),
            MachineHelper.Panel(L.K("production.mineralGrowth.main", "主粉盒 · 渣料", "Primary powder · residue"), main, "input"),
            MachineHelper.Panel(L.K("production.mineralGrowth.secondary", "副粉盒", "Secondary powder"), secondary, "secondary"),
            MachineHelper.Panel(L.K("production.mineralGrowth.blueprint", "蓝图", "Blueprint"), blueprint, "blueprint"),
            MachineHelper.Panel(L.K("production.mineralGrowth.output", "出料", "Output"), output, "output"),
            MachineHelper.Panel(LocHelper.GetLocalizedMechanic("mech_furnace_label_module"), modules, "module"),
            MachineHelper.Panel(L.K("production.mineralGrowth.status", "培养", "Growth"), MachineryStack.Vertical(status, closeCover), "control"),
            coilPanel, guidePanel);
        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        MachineService.Register(machine, () => machine.identifier == "corp_presence_" + Key && MachineService.Handbook, visible =>
        {
            coilPanel.SetHidden(!visible);
            guidePanel.SetHidden(!visible);
        });
        closeCover.SetCallback(() => MachineService.Close(machine));
        machine.EnableTag("CONTAINER_TAG");
        machine.EnableTag("STANDARD_MACHINE_TAG");
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineryHelper.InitMachinery(machine, MineralGrowthBatch.BasePower);
        machine.ModifyTag("BASE_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(MineralGrowthBatch.NightWork));
        machine.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(MineralGrowthBatch.NightWork));

        bool Writable() => GeneralHelper.IsItemOwned(machine) && SaveSlot.CanWrite("CorpPresence");
        MineralGrowthBatch.Recipe LockedRecipe() => MineralGrowthBatch.Find(machine.GetTagReadonly(RecipeTag).GetString());
        MineralGrowthBatch.Recipe InsertedRecipe() => !HasProfile(machine) ? MineralGrowthBatch.Find(Goods.IdOf(blueprint.childItem)) : null;
        int Progress() => Math.Clamp(machine.GetTagReadonly(ProgressTag).GetInt(), 0, MineralGrowthBatch.RequiredWork);
        GrowthRate Quote(MineralGrowthBatch.Recipe recipe, bool starting) => starting
            ? DraftRate(machine, recipe, coil.childItem, guide.childItem) : LockedRate(machine, recipe);
        bool HasPower(GrowthRate rate) => machine.GetTagReadonly(MachineryHelper.IS_MACHINERY_POWERED).GetBool()
            && WorkshopCycle.CanDrawPower(machine, battery, rate.Charge);
        bool CanTakePart(int bit) => !HasProfile(machine) || LockedRate(machine, LockedRecipe())?.Uses(bit) == false;
        coil.mayInventoryRemoveItemFunc = (_, _) => CanTakePart(CoilBit);
        guide.mayInventoryRemoveItemFunc = (_, _) => CanTakePart(GuideBit);

        void Refresh()
        {
            var locked = LockedRecipe();
            var recipe = locked ?? InsertedRecipe();
            var rate = Quote(recipe, locked == null);
            var due = ReadDue(machine);
            int progress = locked == null ? 0 : Progress();
            if (rate == null || (recipe == null && HasProfile(machine)) || !ValidDue(rate, due, progress))
            {
                status.SetText(L.K("workshop.growth.unknownRig", "保留未知改装批次", "Unrecognized retrofit batch retained"));
                return;
            }
            if (recipe == null)
            {
                status.SetText(L.K("production.mineralGrowth.needBlueprint", "插入矿化蓝图", "Insert a mineral growth blueprint"));
                return;
            }
            string state;
            if (progress == MineralGrowthBatch.RequiredWork)
                state = L.K("production.mineralGrowth.mature", "批次已成熟，等待出料", "Batch mature, waiting for output");
            else if (due == NightDue.FeedAndWater)
                state = L.K("workshop.growth.prepaid", "本夜电已付，等待水和已排来料", "Night prepaid · waiting for water and planned feed");
            else if (due == NightDue.Water)
                state = L.K("workshop.growth.prepaidWater", "本夜电已付，只待补水", "Night prepaid · waiting for water");
            else if (recipe == MineralGrowthBatch.Ore && AlreadyRan(machine))
                state = L.K("workshop.growth.settled", "本夜已结算", "This night has settled");
            else if (locked == null && !TryPlan(recipe, main.childItem, secondary.childItem, rate.Quality, out _, out _))
                state = L.K("production.mineralGrowth.needPowder", "粉量或纯度不足", "Insufficient powder or purity");
            else if (!HasWater(water.childItem, rate.Water))
                state = L.K("production.mineralGrowth.needWater", "水量不足", "Insufficient water");
            else if (!HasPower(rate))
                state = L.K("production.mineralGrowth.needPower", "电量不足", "Insufficient power");
            else if (locked == null && rate.Retrofitted && !Fits(recipe, output))
                state = MineralNightReport.Reason(MineralStop.Full);
            else
                state = L.K("production.mineralGrowth.ready", "可过夜培养", "Ready to grow overnight");

            string product = recipe == MineralGrowthBatch.Ore
                ? L.K("production.mineralGrowth.ore", "普通矿石", "Common Ore")
                : L.K("production.mineralGrowth.lightSalt", "光盐芯", "Light Salt Core");
            string feed = locked == null ? "\n" + L.F("production.mineralGrowth.feed",
                "渣料 {0} · {1} {2}（各至少 81%）", "Residue {0} · {1} {2} (at least 81% each)",
                recipe.Residue, PowderBox.ComponentName(recipe.Component), MineralGrowthBatch.SecondaryCost(recipe.Secondary, rate.Quality)) : "";
            status.SetText(L.F("production.mineralGrowth.progress",
                "{0} ×{1} · {2}%\n每有效夜晚 {3} mL 水 · {4} 电",
                "{0} ×{1} · {2}%\n{3} mL water · {4} charge per active night",
                product, recipe.Count, progress / 2, rate.Water, rate.Charge) + feed + "\n" + state);
        }

        void DeliverMature()
        {
            var recipe = LockedRecipe();
            var rate = LockedRate(machine, recipe);
            if (!Writable() || recipe == null || rate == null || ReadDue(machine) != NightDue.None || Progress() != MineralGrowthBatch.RequiredWork
                || !TryDeliver(recipe, output)) return;
            machine.ModifyTag(RecipeTag, tag => tag.Enable().SetString(""), skipValidate: true);
            machine.ModifyTag(ProgressTag, tag => tag.Enable().SetInt(0), skipValidate: true);
            if (rate.Retrofitted)
            {
                machine.DisableTag(RigTag, skipValidate: true);
                MarkNight(machine);
            }
        }

        GrowthInputs PlanNight(MineralGrowthBatch.Recipe recipe, GrowthRate rate, bool first)
        {
            var waterItem = water.childItem;
            if (!HasWater(waterItem, rate.Water)) return null;
            if (!first) return new(waterItem, null, null, null, null);
            var primary = main.childItem;
            var second = secondary.childItem;
            return TryPlan(recipe, primary, second, rate.Quality, out var nextMain, out var nextSecondary)
                ? new(waterItem, primary, second, nextMain, nextSecondary) : null;
        }

        void CommitNight(MineralGrowthBatch.Recipe recipe, GrowthRate rate, GrowthInputs plan, bool first)
        {
            if (first)
            {
                PowderBox.CommitComposition(plan.Main, plan.MainAfter);
                PowderBox.CommitComposition(plan.Secondary, plan.SecondaryAfter);
                machine.ModifyTag(RecipeTag, tag => tag.Enable().SetString(recipe.Blueprint), skipValidate: true);
            }
            ConsumeWater(plan.Water, rate.Water);
            int progress = MineralGrowthBatch.Advance(first ? 0 : Progress(), rate.Work);
            machine.ModifyTag(ProgressTag, tag => tag.Enable().SetInt(progress), skipValidate: true);
            if (rate.Retrofitted)
            {
                machine.DisableTag(DueTag, skipValidate: true);
                MarkNight(machine);
            }
            DeliverMature();
            if (first)
            {
                PowderBox.RefreshComposition(plan.Main, plan.MainAfter);
                PowderBox.RefreshComposition(plan.Secondary, plan.SecondaryAfter);
            }
            WaterFeatureHelper.OnLiquidChanged(plan.Water, WaterFeatureHelper.GetWaterFeature(plan.Water)?.IsExposed() ?? false);
            MachineHelper.OnMachineActioned(machine, modules);
        }

        void Grow(MineralGrowthBatch.Recipe recipe, bool starting)
        {
            var rate = Quote(recipe, starting);
            if (recipe == null || rate == null || rate.Work <= 0 || !DirectoryMaster.Has<GameItem>(recipe.Product)) return;
            var due = ReadDue(machine);
            if (!ValidDue(rate, due, starting ? 0 : Progress())) return;
            bool first = starting || due == NightDue.FeedAndWater;
            var plan = PlanNight(recipe, rate, first);
            if (plan == null) return;
            if (due == NightDue.None)
            {
                if (recipe == MineralGrowthBatch.Ore && AlreadyRan(machine) || !HasPower(rate)
                    || starting && rate.Retrofitted && !Fits(recipe, output)
                    || !WorkshopCycle.DrawPower(machine, battery, rate.Charge)) return;
                if (rate.Retrofitted)
                {
                    if (starting)
                    {
                        FreezeRate(machine, rate);
                        machine.ModifyTag(RecipeTag, tag => tag.Enable().SetString(recipe.Blueprint), skipValidate: true);
                    }
                    machine.ModifyTag(DueTag, tag => tag.Enable().SetInt((int)(first ? NightDue.FeedAndWater : NightDue.Water)), skipValidate: true);
                    MarkNight(machine);
                    plan = PlanNight(recipe, rate, first);
                    if (plan == null) return;
                }
            }
            CommitNight(recipe, rate, plan, first);
        }

        void GrowOvernight()
        {
            if (!Writable()) return;
            string previousAppearance = Appearance(machine);
            if (LockedRecipe() != null && Progress() == MineralGrowthBatch.RequiredWork)
            {
                DeliverMature();
                if (previousAppearance != Appearance(machine)) MachineAppearance.Refresh(machine);
                Refresh();
                return;
            }

            ModuleHelper.ComputeModuleEffect(modules, null, machine);
            ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
            var locked = LockedRecipe();
            Grow(locked ?? InsertedRecipe(), starting: locked == null);
            if (previousAppearance != Appearance(machine)) MachineAppearance.Refresh(machine);
            Refresh();
        }
        machine.onCycleEndSlotItemFunc = (_, _, _) => HookGuard.Run("矿化机过夜", () => WorkshopCycle.Run(machine, GrowOvernight));

        foreach (var inventory in new GameInventory[] { battery, water, main, secondary, blueprint, coil, guide })
        {
            inventory.onSlotAddItemEndFunc += (_, _, _) => Refresh();
            inventory.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        }
        output.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        MachineHelper.SetupModuleBay(modules, machine, Refresh);
        window.openCallback += () => { MachineService.OpenWindow(machine); Refresh(); };
        window.closeCallback += () => MachineService.Close(machine);
        machine.onLoaded += _ =>
        {
            MachineAppearance.Refresh(machine);
            Refresh();
        };
        return machine;
    }

    internal static string Appearance(GameItem machine) => MineralGrowthBatch.Appearance(
        machine.GetTagReadonly(RecipeTag).GetString(), machine.GetTagReadonly(ProgressTag).GetInt());

    private static GameSlotInventory PowderSlot() => new(3, 3)
    {
        mayInventoryAddItemFunc = (item, _) => PowderBox.IsOwnedBox(item)
    };

    private static bool TryPlan(MineralGrowthBatch.Recipe recipe, GameItem main, GameItem secondary, int quality,
        out int[] nextMain, out int[] nextSecondary)
    {
        nextMain = nextSecondary = null;
        if (!PowderBox.IsOwnedBox(main) || !PowderBox.IsOwnedBox(secondary) || ReferenceEquals(main, secondary))
            return false;

        nextMain = new int[PowderBox.ComponentCount];
        nextSecondary = new int[PowderBox.ComponentCount];
        return PowderBox.Read(main, nextMain) <= PowderColor.Capacity
            && PowderBox.Read(secondary, nextSecondary) <= PowderColor.Capacity
            && MineralGrowthBatch.TryPlan(recipe, nextMain, nextSecondary, quality);
    }

    private static bool IsWaterContainer(GameItem item) =>
        item != null && GeneralHelper.IsItemOwned(item) && item.IsTag(LiquidContainer);

    private static bool HasWater(GameItem item, int millilitres) => IsWaterContainer(item)
        && item.GetTagReadonly(Liquid.GetCurrentPartFromId("water")).GetInt() >= millilitres * 1000;

    private static void ConsumeWater(GameItem item, int millilitres)
    {
        var key = Liquid.GetCurrentPartFromId("water");
        int remaining = item.GetTagReadonly(key).GetInt() - millilitres * 1000;
        item.ModifyTag(key, tag => tag.SetInt(remaining), skipValidate: true);
    }

    private static bool Fits(MineralGrowthBatch.Recipe recipe, GameGridInventory output) =>
        MachineAppearance.CanFit(output, recipe.Product, recipe.Count, separateUnit: true);

    private static bool TryDeliver(MineralGrowthBatch.Recipe recipe, GameGridInventory output)
    {
        if (!Fits(recipe, output)) return false;

        var products = new List<GameItem>(recipe.Count);
        bool delivered = false;
        try
        {
            for (int i = 0; i < recipe.Count; i++)
            {
                var product = DirectoryMaster.Item(recipe.Product, isOwned: true);
                if (product == null) return false;
                products.Add(product);
            }
            return delivered = ProductionOutput.TryAddSeparateBatch(output, products);
        }
        finally
        {
            if (!delivered) ProductionOutput.DiscardBatch(products);
        }
    }
}
