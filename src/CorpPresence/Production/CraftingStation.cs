using System;

namespace CorpPresence;

internal static class CraftingStation
{
    internal const string Key = "crafting_station";
    internal const string SiliconPlate = "silicon_plate";
    internal const string Substrate = "electronic_substrate";
    internal const string Blueprint = "electronic_blueprint";
    private const string Product = "common_electronic";
    private const string MillRecipeTag = "CORP_CRAFT_MILL_RECIPE";

    internal static readonly Goods.Def Definition = new()
    {
        Name = "综合工艺台", NameEn = "Assembly Workbench", Value = 240,
        Company = "mineral", Art = "crafting-station", Fallback = "furnace",
        W = 3, H = 3, Types = new[] { "MACHINE" }, Shell = Build
    };

    internal static readonly Goods.Def SiliconPlateDefinition = new()
    {
        Name = "硅板", NameEn = "Silicon Plate", Value = 6,
        Company = "mineral", Art = "silicon-plate", Fallback = "steel_sheet",
        W = 2, H = 2, Shape = "##|##", Types = new[] { "MATERIAL" }, Shell = () => ItemDirectory.CreateEmptyItem(),
        Setup = item => item.onLoaded += FoldedFactory.PreservePlate
    };

    internal static readonly Goods.Def SubstrateDefinition = new()
    {
        Name = "电子基材", NameEn = "Electronic Substrate", Value = 3,
        Company = "mineral", Art = "electronic-substrate", Fallback = "common_electronic",
        W = 2, H = 1, Shape = "##", Types = new[] { "MATERIAL" }, Shell = () => ItemDirectory.CreateEmptyItem()
    };

    internal static readonly Goods.Def BlueprintDefinition = new()
    {
        Name = "普通电子元件蓝图", NameEn = "Common Electronics Blueprint", Value = 35,
        Company = "mineral", Art = "electronic-blueprint", Fallback = "printer_chip_10_mag",
        W = 1, H = 1, Shape = "#", Shell = () => ItemDirectory.CreateEmptyItem()
    };

    private static GameItem Build()
    {
        var blueprint = new GameSlotInventory(2, 2)
        {
            mayInventoryAddItemFunc = (item, _) => Goods.IdOf(item) == Blueprint
        };
        GameItem machine = null;
        bool HasBlueprint() => Goods.IdOf(blueprint.childItem) == Blueprint;
        string MillRecipe() => !HasBlueprint() && machine != null
            ? machine.GetTagReadonly(MillRecipeTag).GetString() : null;
        bool Accept(GameItem item)
        {
            string recipe = MillRecipe();
            if (IndustrialWorkbench.IsIndustrial(recipe)) return IndustrialWorkbench.Learned(recipe) && IndustrialWorkbench.Accept(recipe, item);
            if (!IndustrialWorkbench.Learned(recipe)) return MaterialIndex(item) >= 0;
            if (item.unitCount < 1 || !GeneralHelper.IsItemOwned(item)) return false;
            string key = Goods.IdOf(item);
            return recipe == ClassifierGoods.Screen ? key is SiliconPlate or ClassifierGoods.Coarse
                : key is ClassifierGoods.Sleeve or ClassifierGoods.Crystal;
        }
        var inputs = new GameSlotInventory[3];
        for (int i = 0; i < inputs.Length; i++)
            inputs[i] = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) => item != null && Accept(item) };
        var extra = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) =>
            IndustrialWorkbench.Learned(MillRecipe()) && IndustrialWorkbench.Accept(MillRecipe(), item) };
        var industrialInputs = new[] { inputs[0], inputs[1], inputs[2], extra };
        var liquid = new GameSlotInventory(9, 6) { mayInventoryAddItemFunc = (item, _) =>
            MillRecipe() == IndustrialGoods.CleanShell && IndustrialWorkbench.Learned(IndustrialGoods.CleanShell)
            && AlcoholPreparationStation.IsWaterContainer(item) };
        var output = new GameGridInventory(6, 4) { mayInventoryAddItemFunc = (_, _) => false };
        var assemble = new MachineryButton(L.K("production.crafting.assemble", "装配", "Assemble"));
        var choose = new MachineryButton(L.K("workshop.mill.workRecipe", "切换已学工艺", "Change learned recipe"));
        var status = new MachineryText(uppercase: false);
        var groups = MachineryStack.Horizontal(
            Group(IndustrialWorkbench.BasicCycle, L.T("基础", "Basic")),
            Group(IndustrialWorkbench.FormingCycle, L.T("成形", "Forming")),
            Group(IndustrialWorkbench.PartCycle, L.T("零件", "Parts")));
        var window = MachineHelper.CreateMachineryWindow(true, new[] { 0, 1, 4, 5, 2, 3 },
            MachineHelper.Panel(L.K("production.crafting.blueprint", "蓝图", "Blueprint"), blueprint, "blueprint"),
            MachineHelper.Panel(L.K("production.crafting.materials", "材料", "Materials"), MachineryStack.Horizontal(inputs), "input"),
            MachineHelper.Panel(L.K("production.crafting.assembly", "装配", "Assembly"), MachineryStack.Vertical(status, groups, choose, assemble), "control"),
            MachineHelper.Panel(L.K("production.crafting.output", "出料", "Output"), output, "output"),
            MachineHelper.Panel(L.K("workshop.industrial.fourthSlot", "工业材料位", "Industrial material"), extra, "extra"),
            MachineHelper.Panel(L.K("workshop.industrial.washSlot", "清洗液", "Wash liquid"), liquid, "liquid"));
        machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");

        GameItem Find(string key)
        {
            foreach (var slot in inputs)
                if (slot.childItem?.unitCount > 0 && Goods.IdOf(slot.childItem) == key && GeneralHelper.IsItemOwned(slot.childItem))
                    return slot.childItem;
            return null;
        }

        string MillState(string recipe, out GameItem material, out GameItem other)
        {
            bool screen = recipe == ClassifierGoods.Screen;
            material = Find(screen ? ClassifierGoods.Coarse : ClassifierGoods.Crystal);
            other = Find(screen ? SiliconPlate : ClassifierGoods.Sleeve);
            if (material == null || other == null || (!screen && other.unitCount != 1)) return "feed";
            if (!screen && ClassifierGoods.Charges(other) == 3) return "new";
            if (screen && !MachineAppearance.CanFit(output, "corp_presence_" + ClassifierGoods.Screen, separateUnit: true)) return "full";
            return "ready";
        }

        void CraftMill(string recipe)
        {
            if (!IndustrialWorkbench.Learned(recipe) || MillState(recipe, out var material, out var other) != "ready") return;
            if (recipe == ClassifierGoods.Screen)
            {
                var screen = DirectoryMaster.Item("corp_presence_" + ClassifierGoods.Screen, isOwned: true);
                if (screen == null) return;
                if (!ProductionOutput.WithSeparateUnit(screen, () => ProductionOutput.TryAdd(output, screen)))
                { screen.Destroy(); return; }
                // Commit both unit counts without redraw between them. Native cleanup follows the full receipt.
                other.unitCount--;
            }
            else ClassifierGoods.SetCharges(other, 3);
            material.unitCount--;
            if (recipe == ClassifierGoods.Screen) RefreshSpent(other);
            RefreshSpent(material);
            Refresh();
        }
        void RefreshSpent(GameItem item) => HookGuard.Run("工艺台材料刷新", () =>
        {
            if (item.unitCount <= 0) item.Destroy();
            else item.SetUnitCount(item.unitCount);
        });
        bool HasMaterials()
        {
            int mask = 0;
            foreach (var slot in inputs)
            {
                var item = slot.childItem;
                int index = MaterialIndex(item);
                if (index < 0 || item.unitCount < 1) return false;
                mask |= 1 << index;
            }
            return mask == 7;
        }

        void Refresh()
        {
            string recipe = MillRecipe();
            if (IndustrialWorkbench.Learned(recipe))
            {
                if (IndustrialWorkbench.IsIndustrial(recipe)) { status.SetText(IndustrialWorkbench.Status(recipe, industrialInputs, liquid.childItem, output)); return; }
                string text = recipe == ClassifierGoods.Screen ?
                    L.K("workshop.mill.makeScreen", "制细筛 · 1 粗砂 + 1 硅板", "Make fine screen · 1 coarse grit + 1 silicon plate") :
                    L.K("workshop.mill.refurbish", "翻新 · 1 晶粉 + 1 磨套，恢复 3 批快磨", "Refurbish · 1 crystal powder + 1 sleeve restores 3 fast batches");
                string state = MillState(recipe, out _, out _);
                status.SetText(text + "\n" + (state switch
                {
                    "full" => MineralNightReport.Reason(MineralStop.Full),
                    "new" => L.K("workshop.mill.alreadyNew", "磨套仍有三批快磨", "Sleeve already has three fast batches"),
                    "ready" => L.K("workshop.mill.workReady", "可以装配", "Ready to assemble"),
                    _ => L.K("workshop.mill.workFeed", "将两件材料放入不同材料位", "Insert the two materials in separate slots")
                }));
                return;
            }
            status.SetText(!HasBlueprint()
                ? LowerMachinist.MineralizerUnlocked(CorpState.Current)
                    ? L.K("workshop.mill.chooseOrPrint", "放入普通电子蓝图，或切换已学工艺", "Insert an electronics blueprint or choose a learned recipe")
                    : L.K("production.crafting.needBlueprint", "放入普通电子元件蓝图", "Insert the Common Electronics Blueprint")
                : !HasMaterials()
                    ? L.K("production.crafting.needMaterials", "需要硅板、电线和电子基材各 1 件", "Requires one silicon plate, wire and electronic substrate")
                    : !MachineAppearance.CanFit(output, Product)
                        ? L.K("production.crafting.full", "出料区空间不足", "Insufficient output space")
                        : L.K("production.crafting.ready", "可装配 1 件普通电子元件", "Ready to assemble one common electronic component"));
        }

        MachineryButton Group(string[] cycle, string label) => new MachineryButton(label)
            .SetCallback(() => WorkshopCycle.Run(machine, () => SelectRecipe(cycle, -1)));

        void SelectRecipe(string[] cycle, int start)
        {
            if (HasBlueprint()) return;
            string next = "";
            for (int i = 1; i <= cycle.Length; i++)
            {
                string candidate = cycle[(start + i) % cycle.Length];
                if (IndustrialWorkbench.Learned(candidate)) { next = candidate; break; }
            }
            if (next.Length == 0) return;
            machine.ModifyTag(MillRecipeTag, tag => tag.Enable().SetString(next));
            Refresh();
        }

        choose.SetCallback(() => WorkshopCycle.Run(machine, () =>
        {
            string recipe = MillRecipe();
            string[] cycle = IndustrialWorkbench.CycleFor(recipe);
            SelectRecipe(cycle, Array.IndexOf(cycle, recipe));
        }));
        assemble.SetCallback(() => HookGuard.Run("工艺台装配", () =>
        {
            string recipe = MillRecipe();
            if (IndustrialWorkbench.Learned(recipe))
            {
                WorkshopCycle.Run(machine, () =>
                {
                    if (IndustrialWorkbench.IsIndustrial(recipe)) { IndustrialWorkbench.Craft(recipe, industrialInputs, liquid, output); Refresh(); }
                    else CraftMill(recipe);
                });
                return;
            }
            if (!GeneralHelper.IsItemOwned(machine) || !SaveSlot.CanWrite("CorpPresence")
                || !HasBlueprint() || !HasMaterials() || !DirectoryMaster.Has<GameItem>(Product)
                || !MachineAppearance.CanFit(output, Product)) return;

            var product = DirectoryMaster.Item(Product, isOwned: true);
            if (product == null) return;
            if (!ProductionOutput.TryAdd(output, product))
            {
                product.Destroy();
                return;
            }

            foreach (var slot in inputs)
            {
                var item = slot.childItem;
                if (item.unitCount == 1) item.Destroy();
                else item.SetUnitCount(item.unitCount - 1);
            }
            Refresh();
        }));

        void Watch(GameInventory inventory)
        {
            inventory.onSlotAddItemEndFunc += (_, _, _) => Refresh();
            inventory.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        }
        Watch(blueprint);
        foreach (var input in inputs) Watch(input);
        Watch(extra);
        Watch(liquid);
        Watch(output);
        MachineAppearance.WatchOutput(machine, output);
        window.openCallback += Refresh;
        return machine;
    }

    private static int MaterialIndex(GameItem item) => item?.identifier == "wire" ? 1 : Goods.IdOf(item) switch
    {
        SiliconPlate => 0,
        Substrate => 2,
        _ => -1
    };
}
