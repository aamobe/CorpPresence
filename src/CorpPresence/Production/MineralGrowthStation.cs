using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class MineralGrowthStation
{
    internal const string Key = "mineral_growth_station";
    private const string RecipeTag = "CORP_MINERAL_GROWTH_RECIPE";
    private const string ProgressTag = "CORP_MINERAL_GROWTH_PROGRESS";
    private const string LiquidContainer = "LIQUID_CONTAINER_TAG";

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
        var output = new GameGridInventory(6, 4) { mayInventoryAddItemFunc = (_, _) => false };
        var modules = MineralModuleBay.Create();
        var status = new MachineryText(uppercase: false);
        var window = MachineHelper.CreateMachineryWindow(true, new[] { 0, 6, 1, 2, 3, 4, 5, 7 },
            MachineHelper.PowerPanel(battery),
            MachineHelper.Panel(L.K("production.mineralGrowth.water", "供水", "Water"), water, "water"),
            MachineHelper.Panel(L.K("production.mineralGrowth.main", "主粉盒 · 渣料", "Primary powder · residue"), main, "input"),
            MachineHelper.Panel(L.K("production.mineralGrowth.secondary", "副粉盒", "Secondary powder"), secondary, "secondary"),
            MachineHelper.Panel(L.K("production.mineralGrowth.blueprint", "蓝图", "Blueprint"), blueprint, "blueprint"),
            MachineHelper.Panel(L.K("production.mineralGrowth.output", "出料", "Output"), output, "output"),
            MachineHelper.Panel(LocHelper.GetLocalizedMechanic("mech_furnace_label_module"), modules, "module"),
            MachineHelper.Panel(L.K("production.mineralGrowth.status", "培养", "Growth"), status, "control"));
        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");
        machine.EnableTag("STANDARD_MACHINE_TAG");
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineryHelper.InitMachinery(machine, MineralGrowthBatch.BasePower);
        machine.ModifyTag("BASE_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(MineralGrowthBatch.NightWork));
        machine.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(MineralGrowthBatch.NightWork));

        bool Writable() => GeneralHelper.IsItemOwned(machine) && SaveSlot.CanWrite("CorpPresence");
        MineralGrowthBatch.Recipe LockedRecipe() => MineralGrowthBatch.Find(machine.GetTagReadonly(RecipeTag).GetString());
        MineralGrowthBatch.Recipe InsertedRecipe() => MineralGrowthBatch.Find(Goods.IdOf(blueprint.childItem));
        int Progress() => Math.Clamp(machine.GetTagReadonly(ProgressTag).GetInt(), 0, MineralGrowthBatch.RequiredWork);
        int Quality() => machine.GetTagReadonly(ModuleHelper.TOTAL_PERCENTAGE_QUALITY_BONUS_INT).GetInt();
        int WaterCost() => MineralGrowthBatch.WaterCost(machine.GetTagReadonly(ModuleHelper.TOTAL_PERCENTAGE_POWER_BONUS_INT).GetInt());
        bool HasPower() => machine.GetTagReadonly(MachineryHelper.IS_MACHINERY_POWERED).GetBool()
            && MachineHelper.CanPower(machine, battery);

        void Refresh()
        {
            var locked = LockedRecipe();
            var recipe = locked ?? InsertedRecipe();
            if (recipe == null)
            {
                status.SetText(L.K("production.mineralGrowth.needBlueprint", "插入矿化蓝图", "Insert a mineral growth blueprint"));
                return;
            }
            int progress = locked == null ? 0 : Progress();
            int waterMl = WaterCost();
            int power = MachineryHelper.GetMachinePowerUsage(machine);
            string state;
            if (progress == MineralGrowthBatch.RequiredWork)
                state = L.K("production.mineralGrowth.mature", "批次已成熟，等待出料", "Batch mature, waiting for output");
            else if (locked == null && !TryPlan(recipe, main.childItem, secondary.childItem, Quality(), out _, out _))
                state = L.K("production.mineralGrowth.needPowder", "粉量或纯度不足", "Insufficient powder or purity");
            else if (!HasWater(water.childItem, waterMl))
                state = L.K("production.mineralGrowth.needWater", "水量不足", "Insufficient water");
            else if (!HasPower())
                state = L.K("production.mineralGrowth.needPower", "电量不足", "Insufficient power");
            else
                state = L.K("production.mineralGrowth.ready", "可过夜培养", "Ready to grow overnight");

            string product = recipe == MineralGrowthBatch.Ore
                ? L.K("production.mineralGrowth.ore", "普通矿石", "Common Ore")
                : L.K("production.mineralGrowth.lightSalt", "光盐芯", "Light Salt Core");
            string feed = locked == null ? "\n" + L.F("production.mineralGrowth.feed",
                "渣料 {0} · {1} {2}（各至少 81%）", "Residue {0} · {1} {2} (at least 81% each)",
                recipe.Residue, PowderBox.ComponentName(recipe.Component), MineralGrowthBatch.SecondaryCost(recipe.Secondary, Quality())) : "";
            status.SetText(L.F("production.mineralGrowth.progress",
                "{0} ×{1} · {2}%\n每有效夜晚 {3} mL 水 · {4} 电",
                "{0} ×{1} · {2}%\n{3} mL water · {4} charge per active night",
                product, recipe.Count, progress / 2, waterMl, power) + feed + "\n" + state);
        }

        void DeliverMature()
        {
            var recipe = LockedRecipe();
            if (!Writable() || recipe == null || Progress() != MineralGrowthBatch.RequiredWork
                || !TryDeliver(recipe, output)) return;
            machine.ModifyTag(RecipeTag, tag => tag.Enable().SetString(""));
            machine.ModifyTag(ProgressTag, tag => tag.Enable().SetInt(0));
        }

        machine.onCycleEndSlotItemFunc = (_, _, _) => HookGuard.Run("矿化机过夜", () =>
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
            var recipe = locked ?? InsertedRecipe();
            int waterMl = WaterCost();
            int speed = machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt();
            var waterItem = water.childItem;
            if (recipe != null && speed > 0 && HasWater(waterItem, waterMl) && HasPower()
                && DirectoryMaster.Has<GameItem>(recipe.Product))
            {
                var mainBox = main.childItem;
                var secondaryBox = secondary.childItem;
                int[] nextMain = null, nextSecondary = null;
                if ((locked != null || TryPlan(recipe, mainBox, secondaryBox, Quality(), out nextMain, out nextSecondary))
                    && MachineHelper.TryDrawCyclePower(machine, battery))
                {
                    if (locked == null)
                    {
                        PowderBox.WriteComposition(mainBox, nextMain);
                        PowderBox.WriteComposition(secondaryBox, nextSecondary);
                        machine.ModifyTag(RecipeTag, tag => tag.Enable().SetString(recipe.Blueprint));
                    }
                    ConsumeWater(waterItem, waterMl);
                    int progress = MineralGrowthBatch.Advance(locked == null ? 0 : Progress(), speed);
                    machine.ModifyTag(ProgressTag, tag => tag.Enable().SetInt(progress));
                    DeliverMature();
                    MachineHelper.OnMachineActioned(machine, modules);
                }
            }
            if (previousAppearance != Appearance(machine)) MachineAppearance.Refresh(machine);
            Refresh();
        });

        foreach (var inventory in new GameInventory[] { battery, water, main, secondary, blueprint })
        {
            inventory.onSlotAddItemEndFunc += (_, _, _) => Refresh();
            inventory.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        }
        output.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        MachineHelper.SetupModuleBay(modules, machine, Refresh);
        window.openCallback += Refresh;
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
        item.ModifyTag(key, tag => tag.SetInt(remaining));
        WaterFeatureHelper.OnLiquidChanged(item, WaterFeatureHelper.GetWaterFeature(item)?.IsExposed() ?? false);
    }

    private static bool TryDeliver(MineralGrowthBatch.Recipe recipe, GameGridInventory output)
    {
        if (!MachineAppearance.CanFit(output, recipe.Product, recipe.Count)) return false;

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
            return delivered = ProductionOutput.TryAddBatch(output, products);
        }
        finally
        {
            if (!delivered)
                foreach (var product in products)
                    product.Destroy();
        }
    }
}
