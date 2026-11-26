using System;
using System.Linq;

namespace CorpPresence;

internal static partial class Forge
{
    internal static bool IsGoodIngot(GameItem item)
    {
        try
        {
            if (item?.identifier != "metal_ingot") return false;
            string grade = IngotPurityHelper.GetPurity(item);
            return grade == IngotPurityHelper.INGOT_PURITY_HIGH || grade == IngotPurityHelper.INGOT_PURITY_VERY_HIGH
                || grade == IngotPurityHelper.INGOT_PURITY_PERFECT;
        }
        catch { return false; }
    }

    internal static void FinishFurnaceNight(GameItem machine, GameSlotInventory battery, GameGridInventory modules,
        GameInventory input, GameInventory output) => WorkshopCycle.Run(machine, () => HookGuard.Run("精锻熔炉过夜", () =>
    {
        machine.ModifyTag("MACHINERY_DRAWN_POWER_BASE", t => t.Enable().SetInt(FurnacePower));
        ModuleHelper.ComputeModuleEffect(modules.childItems.ToList(), machine);
        ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
        long speed = Math.Max(0, machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt());
        long accumulated = speed + Math.Max(0, machine.GetTagReadonly(CarryTag).GetInt());
        long available = input.childItems.Where(item => IsGoodIngot(item) && GeneralHelper.IsItemOwned(item))
            .Sum(i => (long)Math.Max(0, i.unitCount));
        int limit = (int)Math.Min(accumulated / SpeedScale, available);
        if (limit == 0) return;
        int made = SmeltBillets(input, output, limit, () => WorkshopCycle.DrawPower(machine, battery));
        int carry = made == limit ? (int)Math.Min(99L, accumulated - (long)made * SpeedScale) : 0;
        machine.ModifyTag(CarryTag, t => t.Enable().SetInt(carry));
        if (made > 0) MachineHelper.OnMachineActioned(machine, modules);
    }));

    private static int SmeltBillets(GameInventory input, GameInventory output, int limit, Func<bool> drawPower)
    {
        int made = 0;
        foreach (var ingot in input.childItems.Where(IsGoodIngot).ToArray())
        {
            while (made < limit && ReferenceEquals(ingot.parentInventory, input) && ingot.unitCount > 0 && GeneralHelper.IsItemOwned(ingot))
            {
                var billet = Goods.Make("forged_billet");
                if (billet == null) return made;
                bool committed = false;
                try
                {
                    GeneralHelper.SetItemOwned(billet);
                    if (!ProductionOutput.TryAddSeparate(output, billet) || (made == 0 && !drawPower())) return made;
                    ingot.unitCount--;
                    committed = true;
                    made++;
                    RefreshSpent(ingot);
                }
                finally { if (!committed) billet.Destroy(); }
            }
        }
        return made;
    }

    internal static string StrikeFrame(GameInventory input, GameSlotInventory alloy, GameSlotInventory water, GameSlotInventory battery, GameInventory output)
    {
        var session = StoreSession.Capture();
        string locked = MeridianIndustrial.RecipeError(CorpState.Current);
        if (locked != null || !session.IsCurrent) return locked ?? MeridianIndustrial.Changed;
        bool Owned(GameItem item) => item != null && item.unitCount > 0 && GeneralHelper.IsItemOwned(item);
        var billet = input.childItems.FirstOrDefault(item => Owned(item) && item.identifier == BilletId);
        var powder = alloy.childItem;
        var vessel = water.childItem;
        var cell = battery.childItem;
        if (billet == null || !Owned(powder) || powder.identifier != MeridianIndustrial.AlloyId)
            return L.K("workshop.meridian.forgeFeed", "需要 1 块精锻钢胚和 1 包耐磨合金粉。", "Load one forged billet and one packet of wear-resistant alloy powder.");
        string waterKey = Liquid.GetCurrentPartFromId("water");
        int waterBefore = vessel?.GetTagReadonly(waterKey)?.GetInt() ?? 0;
        int waterAfter = waterBefore - WaterPerBillet * 1000;
        if (!Owned(vessel) || !vessel.IsTag("LIQUID_CONTAINER_TAG") || vessel.IsTag("WINE_CONTAINER_TAG")
            || waterBefore < WaterPerBillet * 1000)
            return L.K("workshop.meridian.forgeWater", "水箱需有 100 毫升水。", "The quench tank needs 100 ml of water.");
        if (!Owned(cell) || !PowerHelper.CanDrawPowerSource(cell, BilletPower))
            return L.K("workshop.meridian.forgePower", "锻工业框架需要 6 电。", "Forging a frame requires 6 charge.");

        var frame = Goods.Make(IndustrialGoods.Frame);
        if (frame == null) return MeridianIndustrial.Changed;
        bool made = false, waterChanged = false;
        try
        {
            GeneralHelper.SetItemOwned(frame);
            if (!ProductionOutput.TryAddSeparate(output, frame))
                return L.K("production.forge.strike.outputTrayFull", "成品槽放不下了", "The output tray is full");
            if (!session.IsCurrent || !ReferenceEquals(billet.parentInventory, input) || !Owned(billet)
                || alloy.childItem != powder || !Owned(powder) || water.childItem != vessel || battery.childItem != cell
                || vessel.GetTagReadonly(waterKey).GetInt() != waterBefore) return MeridianIndustrial.Changed;
            waterChanged = true;
            vessel.ModifyTag(waterKey, t => t.SetInt(waterAfter), skipValidate: true);
            int before = cell.GetTagReadonly("power_source_item_energy").GetInt();
            bool paid;
            try { paid = PowerHelper.DrawPowerSource(cell, BilletPower); }
            catch (Exception error)
            {
                ModLog.Warn("工业锻造电源回调：" + error.Message);
                paid = cell.IsTag("power_source_item") && before - cell.GetTagReadonly("power_source_item_energy").GetInt() == BilletPower;
            }
            if (!paid) return MeridianIndustrial.Changed;
            billet.unitCount--;
            powder.unitCount--;
            made = true;
            RefreshSpent(billet);
            RefreshSpent(powder);
            HookGuard.Run("工业锻造水箱刷新", () => WaterFeatureHelper.OnLiquidChanged(vessel, WaterFeatureHelper.GetWaterFeature(vessel)?.IsExposed() ?? false));
            return null;
        }
        finally
        {
            if (!made)
            {
                if (waterChanged) vessel.ModifyTag(waterKey, t => t.SetInt(waterBefore), skipValidate: true);
                frame.Destroy();
            }
        }
    }

    private static void RefreshSpent(GameItem item) => HookGuard.Run("工业锻造原料刷新", () =>
    {
        if (item.unitCount == 0) item.Destroy();
        else item.SetUnitCount(item.unitCount);
    });
}
