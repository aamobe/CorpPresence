using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class Perfume
{
    private const string BlueMode = "CORP_PERFUME_BLUE_MODE";
    private sealed class PerfumeRig
    {
        internal GameItem Machine;
        internal int Kind;
        internal GameSlotInventory Battery = new();
        internal GameGridInventory Modules = new("10000010001000001110000010001000001", 7);
        internal GameGridInventory Input = new(4, 4);
        internal GameSlotInventory Liquid = new();
        internal GameGridInventory Output = new(6, 4);
        internal MachineryPanel InputPanel;
        internal MachineryPanel LiquidPanel;
        internal bool OutputDirty = true;
        internal bool OutputAvailable;
        internal string CheckedProduct;
        internal int CheckedCount;
        internal int CheckedOutputHeight;
        internal bool Blue => Machine.IsTag(BlueMode);
        internal string Ingredient => Kind == 1 ? Blue ? "blue_blood_bag" : "blood_bag" : "corp_presence_" + (Blue ? BlueKey : RedKey);
        internal int Count => Kind == 1 && !Blue ? 2 : 1;
        internal int OilMaterialsNeeded => Blue ? 2 : 3;
        internal string Product => Kind == 0 ? "corp_presence_" + OilKey : Kind == 1 ? "corp_presence_" + (Blue ? BlueKey : RedKey) : Blue ? "pheromone_perfume" : "neuroactive_perfume";
    }

    private static GameItem BuildMachine(int kind)
    {
        var rig = new PerfumeRig { Kind = kind };
        var note = new GameSlotInventory();
        var red = new MachineryRadioButton(L.K("production.perfume.machines.buildMachine.ordinaryBlood", "普通血路线", "Ordinary Blood"), true, on => {
            if (on && rig.Machine != null) rig.Machine.DisableTag(BlueMode);
        });
        var blue = new MachineryRadioButton(L.K("production.perfume.machines.buildMachine.blueBlood", "蓝血路线", "Blue Blood"), false, on => {
            if (on && rig.Machine != null) rig.Machine.EnableTag(BlueMode);
        });
        new MachineryRadioGroup().Add(red).Add(blue);
        var check = new MachineryButton(L.K("production.perfume.machines.buildMachine.checkProduction", "检查生产条件", "Check Production"));
        check.SetCallback(() => Tell(ProductionProblem(rig) ?? L.K("production.perfume.machines.buildMachine.ingredientsReadyWaitOvernightOr", "原料合格，等待过夜或加速加工。", "Ingredients ready. Wait overnight or turbo boost.")));
        var control = kind == 0 ? MachineryStack.Vertical(check) : MachineryStack.Vertical(red, blue, check);
        // Keep the saved storage order/types from the preceding test build. Old
        // input flowers or mixer liquid bottles remain retrievable, never discarded.
        rig.InputPanel = MachineHelper.Panel(kind == 0 ? L.K("production.perfume.machines.buildMachine.itemsRetrieve", "待取回物品", "Items to Retrieve") : L.K("production.perfume.machines.buildMachine.materials", "材料", "Materials"), rig.Input, "input");
        rig.LiquidPanel = kind == 1 ? null : MachineHelper.Panel(kind == 0 ? L.K("production.perfume.machines.buildMachine.oilContainingLiquid", "含基底油的液体", "Oil-containing Liquid") : L.K("production.perfume.machines.buildMachine.liquidRetrieve", "待取回液体", "Liquid to Retrieve"), rig.Liquid, "liquid");
        if (kind == 0) rig.InputPanel.SetHidden(true);
        if (kind == 2) rig.LiquidPanel.SetHidden(true);
        var window = kind == 1 ? MachineHelper.CreateMachineryWindow(true,
            MachineHelper.PowerPanel(rig.Battery),
            MachineHelper.Panel(L.K("production.perfume.machines.buildMachine.modules", "模组", "Modules"), rig.Modules, "module"),
            rig.InputPanel,
            MachineHelper.Panel(L.K("production.perfume.machines.buildMachine.recipe", "配方", "Recipe"), control, "control"),
            MachineHelper.Panel(L.K("production.perfume.machines.buildMachine.output", "成品", "Output"), rig.Output, "output"), MachineHelper.NotePanel(note)) : MachineHelper.CreateMachineryWindow(true,
            MachineHelper.PowerPanel(rig.Battery),
            MachineHelper.Panel(L.K("production.perfume.machines.buildMachine.modules", "模组", "Modules"), rig.Modules, "module"),
            rig.InputPanel,
            rig.LiquidPanel,
            MachineHelper.Panel(L.K("production.perfume.machines.buildMachine.recipe", "配方", "Recipe"), control, "control"),
            MachineHelper.Panel(L.K("production.perfume.machines.buildMachine.output", "成品", "Output"), rig.Output, "output"), MachineHelper.NotePanel(note));
        rig.Machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        rig.Machine.EnableTag("CONTAINER_TAG");
        rig.Machine.EnableTag("STANDARD_MACHINE_TAG");
        MachineHelper.SetupManualSlot(note);
        MachineHelper.SetupBatterySlot(rig.Battery, rig.Machine);
        MachineryHelper.InitMachinery(rig.Machine, kind == 0 ? 12 : kind == 1 ? 8 : 6);
        rig.Machine.ModifyTag("BASE_PROCESSING_SPEED_TAG", s => s.Enable().SetInt(kind == 0 ? 200 : 100));
        rig.Machine.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", s => s.Enable().SetInt(kind == 0 ? 200 : 100));
        MachineHelper.SetupModuleBay(rig.Modules, rig.Machine);
        if (kind == 0) MachineUpgrade.Attach(rig.Machine, 2, rig.Output);
        else MachineUpgrade.Attach(rig.Machine, 2, rig.Input, rig.Output);
        rig.Input.mayInventoryAddItemFunc = (item, _) => kind != 0 && GeneralHelper.IsItemOwned(item) &&
            (kind == 1 ? item.identifier is "blood_bag" or "blue_blood_bag" :
                item.identifier == "corp_presence_" + RedKey || item.identifier == "corp_presence_" + BlueKey || item.identifier == "corp_presence_" + OilKey);
        rig.Liquid.mayInventoryAddItemFunc = (item, _) => kind == 0 && GeneralHelper.IsItemOwned(item) && item.IsTag("LIQUID_CONTAINER_TAG") && !item.IsTag("WINE_CONTAINER_TAG");
        rig.Output.mayInventoryAddItemFunc = (_, _) => false;
        rig.Machine.onCycleEndSlotItemFunc = (_, _, _) => RunMachine(rig);
        // Radio selection is visual state; restore it from the machine's saved recipe tag.
        rig.Machine.onLoaded += _ => { rig.OutputDirty = true; if (rig.Blue) blue.SetOn(true); else red.SetOn(true); RefreshRig(rig); };
        rig.Machine.onOneSecondItemFunc += (_, _, _) => RefreshRig(rig);
        rig.Output.onSlotAddItemEndFunc += (_, _, _) => { rig.OutputDirty = true; RefreshRig(rig); };
        rig.Output.onSlotRemoveItemEndFunc += (_, _, _) => { rig.OutputDirty = true; RefreshRig(rig); };
        return rig.Machine;
    }

    private static GameItem[] Ingredients(PerfumeRig rig, string id = null) => rig.Input.childItems
        .Where(i => i != null && GeneralHelper.IsItemOwned(i) && i.identifier == (id ?? rig.Ingredient) && i.unitCount > 0).ToArray();

    private static int IngredientCount(PerfumeRig rig) => Ingredients(rig).Sum(i => i.unitCount);

    private static string IngredientProblem(PerfumeRig rig)
    {
        if (rig.Kind == 0) return OilProblem(rig.Liquid.childItem, OilCost(rig.Machine));
        if (IngredientCount(rig) < rig.Count) return L.K("production.perfume.machines.ingredientProblem.notEnoughIngredientsSelectedRecipe", "所选配方的原料不足。", "Not enough ingredients for the selected recipe.");
        if (rig.Kind == 2 && Ingredients(rig, "corp_presence_" + OilKey).Sum(i => i.unitCount) < MaterialCost(rig))
            return L.K("production.perfume.machines.ingredientProblem.thisBatchNeedsBaseOil", $"当前配方需要{MaterialCost(rig)}份基底油材料。", (FormattableString)$"This batch needs {MaterialCost(rig)} base oil materials.");
        return null;
    }

    private static bool HasPower(PerfumeRig rig) => PowerHelper.CanDrawPowerSource(rig.Battery.childItem, MachineryHelper.GetMachinePowerUsage(rig.Machine));

    private static string ProductionProblem(PerfumeRig rig)
    {
        string reason = IngredientProblem(rig);
        if (reason != null) return reason;
        if (!HasPower(rig)) return L.K("production.perfume.machines.productionProblem.connectSufficientPower", "请连接足够的电力。", "Connect sufficient power.");
        return OutputHasSpace(rig) ? null : L.K("production.perfume.machines.productionProblem.outputFullOrProductDirectory", "成品空间不足或成品目录暂不可用。", "Output is full or the product directory is unavailable.");
    }

    private static bool OutputHasSpace(PerfumeRig rig)
    {
        string product = rig.Product;
        int count = ProductCount(rig);
        if (!rig.OutputDirty && rig.CheckedProduct == product && rig.CheckedCount == count &&
            rig.CheckedOutputHeight == rig.Output.inventoryShape.height) return rig.OutputAvailable;
        var sample = ItemRegistry.Create(product);
        if (sample == null) return false;
        try
        {
            rig.OutputAvailable = MachineAppearance.CanFitOutput(rig.Output, sample, count);
            rig.CheckedProduct = product;
            rig.CheckedCount = count;
            rig.CheckedOutputHeight = rig.Output.inventoryShape.height;
            rig.OutputDirty = false;
            return rig.OutputAvailable;
        }
        finally { sample.Destroy(); }
    }

    private static void RunMachine(PerfumeRig rig)
    {
        try
        {
            if (!GameResources.Ready || SaveSlot.CurrentStore == null || !GeneralHelper.IsItemOwned(rig.Machine)) return;
            ModuleHelper.ComputeModuleEffect(rig.Modules.childItems.ToList(), rig.Machine);
            ModuleHelper.ApplyBasicModuleEffect(rig.Modules, null, rig.Machine);
            if (IngredientProblem(rig) != null) return;
            int budget = checked(Math.Max(0, rig.Machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt()) +
                Math.Clamp(rig.Machine.GetTagReadonly(CarryTag).GetInt(), 0, 99));
            int batches = budget / 100;
            if (batches == 0) { SetCarry(rig.Machine, budget); return; }
            if (!HasPower(rig)) return;
            int made = 0;
            for (; made < batches; made++)
            {
                if (IngredientProblem(rig) != null) break;
                var products = new List<GameItem>();
                bool committed = false;
                try
                {
                    int count = ProductCount(rig);
                    for (int index = 0; index < count; index++)
                    {
                        var product = ItemRegistry.Create(rig.Product);
                        if (product == null) break;
                        GeneralHelper.SetItemOwned(product);
                        products.Add(product);
                    }
                    if (products.Count != count || !ProductionOutput.TryAddBatch(rig.Output, products)) break;
                    // Output is reserved before paying or consuming inputs. Failed power
                    // draw destroys the reserved product and preserves all ingredients.
                    if (made == 0 && !MachineHelper.TryDrawCyclePower(rig.Machine, rig.Battery)) break;
                    if (rig.Kind == 0) ExtractOilComponent(rig.Liquid.childItem, OilCost(rig.Machine));
                    else
                    {
                        ConsumeIngredients(rig, rig.Count);
                        if (rig.Kind == 2) ConsumeIngredients(rig, MaterialCost(rig), "corp_presence_" + OilKey);
                    }
                    CommitQuality(rig);
                    committed = true;
                }
                finally { if (!committed) foreach (var product in products) product.Destroy(); }
            }
            SetCarry(rig.Machine, made == batches ? budget % 100 : 0);
            if (made > 0) MachineHelper.OnMachineActioned(rig.Machine, rig.Modules);
            RefreshRig(rig);
        }
        catch (Exception error) { ModLog.Warn("香水生产失败：" + error.Message); }
    }

    private static void SetCarry(GameItem machine, int carry) => machine.ModifyTag(CarryTag, s => s.Enable().SetInt(carry));

    private static void ConsumeIngredients(PerfumeRig rig, int count, string id = null)
    {
        foreach (GameItem item in Ingredients(rig, id))
        {
            int used = Math.Min(count, item.unitCount);
            count -= used;
            if (used == item.unitCount) item.Destroy(); else item.unitCount -= used;
            if (count == 0) return;
        }
        throw new InvalidOperationException("生产原料在提交时改变");
    }

    private static void RefreshRig(PerfumeRig rig)
    {
        if (rig.Kind == 0)
        {
            bool empty = rig.Input.childItems.Count == 0;
            if (rig.InputPanel.isHidden != empty) rig.InputPanel.SetHidden(empty);
        }
        if (rig.Kind == 2)
        {
            bool empty = rig.Liquid.childItem == null;
            if (rig.LiquidPanel.isHidden != empty) rig.LiquidPanel.SetHidden(empty);
        }
        string key = rig.Kind == 0 ? ExtractorKey : rig.Kind == 1 ? BloodKey : MixerKey;
        bool ready = IngredientProblem(rig) == null && HasPower(rig) && OutputHasSpace(rig);
        string state = ready ? rig.Kind == 0 ? "_running" : rig.Blue ? "_blue" : "_red" : "";
        string path = "corp_med_" + key + state;
        if (rig.Machine.spritePath == path) return;
        rig.Machine.SetSprite(rig.Machine.spriteAtlasPath, path);
        Goods.Redraw(rig.Machine);
    }
}
