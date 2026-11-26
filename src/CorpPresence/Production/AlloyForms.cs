using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class AlloyForms
{
    private sealed record Recipe(string Key, int YieldPercent, int Cells, string Zh, string En, string Feed = null);
    private sealed record FormPlan(GameSlotInventory Slot, GameItem Input, RodCondition Stock, RodCondition Product, RodCondition Remainder);

    private static readonly Recipe[] Recipes =
    {
        new(RodMaterial.ShortRod, 50, 1, "一根完整棒切成两段，保留工艺与组织", "Cut one full rod into two segments with the same material treatment"),
        new(RodMaterial.Strip, 80, 2, "完整棒轧成连续板带，棒料不可带硬化面，脆裂 ≤10", "Roll a whole rod into strip. Requires brittleness ≤10 and no hardened surface"),
        new(RodMaterial.WireBlank, 90, 2, "完整棒拉粗线坯，导电 ≥60 且脆裂 ≤4", "Draw a whole rod into coarse wire. Conductivity ≥60 and brittleness ≤4"),
        new(RodMaterial.PorousBlank, 75, 1, "短棒镗成多孔坯，孔隙 5–9 且耐磨 ≥40",
            "Bore a short rod into a porous blank. Porosity 5–9 and wear resistance ≥40", RodMaterial.ShortRod),
        new(RodMaterial.FeedShaft, 90, 1, "车给料轴 · 短棒承力 ≥50、耐磨 ≥40、脆裂 ≤10",
            "Turn a feed shaft. Short stock with load ≥50, wear resistance ≥40 and brittleness ≤10", RodMaterial.ShortRod),
        new(RodMaterial.FeedSleeve, 95, 1, "精整多孔坯 · 保留孔隙 5–9，耐磨 ≥40",
            "Finish a porous blank. Retains porosity of 5–9 and wear resistance ≥40", RodMaterial.PorousBlank),
        new(RodMaterial.IndustrialCoil, 90, 1, "绕工业线圈 · 粗线坯导电 ≥60、脆裂 ≤2",
            "Wind an industrial coil. Coarse wire with conductivity ≥60 and brittleness ≤2", RodMaterial.WireBlank),
        new(RodMaterial.AxialLead, 90, 2, "制定向引线 · 粗线坯须具定向导电组织",
            "Make an axial lead. Coarse wire with axial conductor structure", RodMaterial.WireBlank),
        new(RodMaterial.ReturnGuide, 85, 2, "折回料导槽 · 连续板带耐磨 ≥40、脆裂 ≤8",
            "Bend a return guide. Continuous strip with wear resistance ≥40 and brittleness ≤8", RodMaterial.Strip),
        new(RodMaterial.FilterBasket, 85, 1, "编泵前滤篮 · 粗线坯脆裂 ≤2",
            "Weave a pump filter basket from coarse wire with brittleness ≤2", RodMaterial.WireBlank),
        new(RodMaterial.PressureShell, 85, 2, "精整承压壳 · 板带承力 ≥50、耐蚀 ≥35、孔隙 ≤3",
            "Finish a pressure shell. Strip with load ≥50, corrosion resistance ≥35 and porosity ≤3", RodMaterial.Strip),
        new(RodMaterial.CastingDie, 85, 2, "精整致密铸模 · 板带须致密，承力 ≥55、耐磨 ≥45、孔隙 ≤3",
            "Finish a dense trial die. Densified strip with load ≥55, wear resistance ≥45 and porosity ≤3", RodMaterial.Strip),
        new(RodMaterial.ClassifierSupport, 85, 2, "校直精密支承 · 回火板带承力 ≥60、孔隙和脆裂均 ≤3",
            "True a precision support. Tempered strip with load ≥60, porosity and brittleness ≤3", RodMaterial.Strip),
        new(RodMaterial.GrowthGuide, 85, 2, "精整耐蚀导流件 · 致密板带耐蚀 ≥45、孔隙 ≤1",
            "Finish a corrosion-resistant guide. Densified strip with corrosion resistance ≥45 and porosity ≤1", RodMaterial.Strip),
        new(RodMaterial.WetContact, 80, 1, "复合短棒制触点 · 芯导电 ≥60、脆裂 ≤4；套耐蚀 ≥45、孔隙 ≤3",
            "Trim a clad segment into a contact. Core conductivity ≥60 and brittleness ≤4; jacket corrosion resistance ≥45 and porosity ≤3", RodMaterial.ShortRod)
    };

    private static Recipe Find(string key) => Array.Find(Recipes, recipe => recipe.Key == key);
    internal static bool IsRecipe(string key) => Find(key) != null;
    internal static bool QualifiesReturnGuide(RodCondition stock) => stock?.Surface is { Wear: >= 40, Brittleness: <= 8 };

    internal static RodMaterial TakePercent(RodMaterial source, int percent)
    {
        int halfPoints = (int)(source.PowderPoints * 2);
        var parts = WaterHelper.SplitProportionally(source.Parts(), halfPoints * percent / 100);
        return new(parts[0], parts[1], parts[2], parts[3], parts[4]);
    }

    internal static bool Accept(string key, GameItem item)
    {
        var recipe = Find(key);
        return recipe != null && item?.unitCount == 1 && GeneralHelper.IsItemOwned(item)
            && (recipe.Feed == null
                ? RodMaterial.IsRod(item.identifier) && (key == RodMaterial.ShortRod || item.identifier != "corp_presence_" + RodMaterial.CladRod)
                : item.identifier == "corp_presence_" + recipe.Feed);
    }

    private static bool Qualifies(Recipe recipe, RodCondition stock)
    {
        if (stock == null || stock.Material.PowderPoints < 2) return false;
        if (stock.Jacket != null && recipe.Key is not (RodMaterial.ShortRod or RodMaterial.FeedShaft or RodMaterial.WetContact)) return false;
        return recipe.Key switch
        {
            RodMaterial.Strip => stock.Process != RodProcess.Harden && stock.Surface.Brittleness <= 10,
            RodMaterial.WireBlank => stock.Core.Conductivity >= 60 && stock.Surface.Brittleness <= 4,
            RodMaterial.PorousBlank or RodMaterial.FeedSleeve => stock.Surface.Porosity is >= 5 and <= 9 && stock.Surface.Wear >= 40,
            RodMaterial.FeedShaft => stock.Core.Load >= 50 && stock.Surface.Wear >= 40 && stock.Core.Brittleness <= 10,
            RodMaterial.IndustrialCoil => stock.Core.Conductivity >= 60 && stock.Core.Brittleness <= 2,
            RodMaterial.AxialLead => (stock.Traits & RodTrait.AxialConductor) != 0,
            RodMaterial.ReturnGuide => QualifiesReturnGuide(stock),
            RodMaterial.FilterBasket => stock.Core.Brittleness <= 2,
            RodMaterial.PressureShell => IndustrialPump.PressureStock(stock),
            RodMaterial.CastingDie => SinteringStation.DieStock(stock),
            RodMaterial.ClassifierSupport => RodCondition.CanTrue(stock),
            RodMaterial.GrowthGuide => MineralGrowthStation.GuideStock(stock),
            RodMaterial.WetContact => WetPumpContact.Qualified(stock),
            _ => true
        };
    }

    private static FormPlan Plan(Recipe recipe, GameSlotInventory[] slots)
    {
        foreach (var slot in slots)
        {
            var input = slot.childItem;
            if (!Accept(recipe.Key, input)) continue;
            var stock = RodCondition.Read(input);
            if (!Qualifies(recipe, stock)) continue;
            // Production bars are far smaller; reject unrepresentable or foreign save values before using native integer math.
            if (stock.Material.PowderPoints > PowderColor.Capacity) continue;
            var core = stock.CoreMaterial;
            var takeCore = TakePercent(core, recipe.YieldPercent);
            if (stock.Jacket == null)
                return new(slot, input, stock, stock with { Material = takeCore, Trued = recipe.Key == RodMaterial.ClassifierSupport },
                    stock with { Material = core.TakeAway(takeCore) });

            var jacket = stock.Jacket;
            var takeJacket = TakePercent(jacket.Material, recipe.YieldPercent);
            if (takeCore.PowderPoints <= 0 || takeJacket.PowderPoints <= 0) continue;
            var leaveCore = core.TakeAway(takeCore);
            var leaveJacket = jacket.Material.TakeAway(takeJacket);
            var product = stock with { Material = takeCore.Add(takeJacket), Jacket = jacket with { Material = takeJacket } };
            var remains = leaveCore.Add(leaveJacket);
            if (recipe.Key != RodMaterial.ShortRod)
                return new(slot, input, stock, product, RodCondition.Evaluate(remains, RodProcess.Cast));
            if (leaveCore.PowderPoints > 0 && leaveJacket.PowderPoints > 0)
                return new(slot, input, stock, product, stock with { Material = remains, Jacket = jacket with { Material = leaveJacket } });
        }
        return null;
    }

    private static bool CanFit(Recipe recipe, GameGridInventory output)
    {
        if (recipe.Key == RodMaterial.ShortRod)
            return MachineAppearance.CanFit(output, "corp_presence_" + recipe.Key, 2, separateUnit: true);
        // The main product takes one or two cells, and the offcut takes any remaining single cell.
        return MachineAppearance.CanFit(output, "corp_presence_" + recipe.Key, separateUnit: true)
            && MachineAppearance.CanFit(output, "corp_presence_" + RodMaterial.Offcut, recipe.Cells + 1, separateUnit: true);
    }

    internal static string Status(string key, GameSlotInventory[] slots, GameGridInventory output)
    {
        var recipe = Find(key);
        var plan = Plan(recipe, slots);
        string description = Goods.NameOf(key) + "\n" + L.T(recipe.Zh, recipe.En);
        if (plan == null) return description;
        string amounts = key == RodMaterial.ShortRod
            ? L.T($"短段各 {plan.Product.Material.PowderPoints:0.#} / {plan.Remainder.Material.PowderPoints:0.#} 点",
                $"Segments of {plan.Product.Material.PowderPoints:0.#} / {plan.Remainder.Material.PowderPoints:0.#} points")
            : L.T($"成品 {plan.Product.Material.PowderPoints:0.#} 点，边料 {plan.Remainder.Material.PowderPoints:0.#} 点",
                $"Product {plan.Product.Material.PowderPoints:0.#}, offcut {plan.Remainder.Material.PowderPoints:0.#} points");
        if (!CanFit(recipe, output)) amounts += "\n" + MineralNightReport.Reason(MineralStop.Full);
        return description + "\n" + amounts;
    }

    internal static void Craft(string key, GameSlotInventory[] slots, GameGridInventory output)
    {
        var recipe = Find(key);
        var plan = Plan(recipe, slots);
        if (plan == null || !CanFit(recipe, output)) return;
        var session = StoreSession.Capture();
        var products = new List<GameItem>(2);
        bool committed = false;
        try
        {
            string remainder = key == RodMaterial.ShortRod ? key : RodMaterial.Offcut;
            if (!Stage(key, plan.Product) || !Stage(remainder, plan.Remainder)
                || !ProductionOutput.TryAddSeparateBatch(output, products)) return;
            if (!session.IsCurrent || !IndustrialWorkbench.Learned(key) || !ReferenceEquals(plan.Slot.childItem, plan.Input)
                || !Accept(key, plan.Input) || RodCondition.Read(plan.Input) != plan.Stock
                || products.Exists(item => item.parentInventory != output || !output.childItems.Contains(item)
                    || item.unitCount != 1 || StorePlacement.Destroyed(item))
                || RodCondition.Read(products[0]) != plan.Product || RodCondition.Read(products[1]) != plan.Remainder) return;
            plan.Input.unitCount = 0;
            committed = true;
            HookGuard.Run("工艺台棒料出库", () => IndustrialWorkbench.Discard(plan.Input));
        }
        finally
        {
            if (!committed)
                foreach (var item in products) HookGuard.Run("撤回棒料加工批次", () => IndustrialWorkbench.Discard(item));
        }

        bool Stage(string id, RodCondition condition)
        {
            var item = DirectoryMaster.Item("corp_presence_" + id, isOwned: true);
            if (item == null) return false;
            products.Add(item);
            condition.WriteFormed(item);
            return true;
        }
    }
}
