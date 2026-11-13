using System;

namespace CorpPresence;

internal static class CropHealth
{
    internal static void UpdateMaturityTarget(GameItem seed, GameItem hydroponic)
    {
        // The native grow tick has already applied this cycle's mineral and contaminant effects.
        int baseWork = HydroponicHelper.GetRequiredProgress(seed);
        int health = Math.Max(0, Math.Min(100, HydroponicHelper.GetHealthPercent(seed)));
        int healthTarget = (int)Math.Ceiling(baseWork * (200 - health) / 100d);
        // Work is integral: round the cap down so the increase never exceeds 20%.
        int maximumTarget = baseWork + baseWork / 5;
        int target = Math.Min(healthTarget, maximumTarget);

        // The seed displays progress; the machine checks its own target to decide when to harvest.
        seed.ModifyTag("REQUIRED_PROGRESS", state => state.Enable().SetInt(target));
        seed.ModifyTag("PROGRESS_ITEM_TARGET_TAG", state => state.Enable().SetInt(target));
        hydroponic.ModifyTag("MACHINE_PROGRESS_TARGET_TAG", state => state.Enable().SetInt(target));
    }

    internal static int HarvestCount(int baseCount, int health)
    {
        double multiplier = health <= 30 ? 0.5 : health <= 70 ? 0.75 : 1;
        return Math.Max(1, (int)Math.Floor(baseCount * multiplier));
    }

    internal static void AfterContinueGrowing(GameItem __0, GameItem __2, bool __runOriginal)
    {
        if (!__runOriginal || __0 == null || __2 == null || StorePlacement.Destroyed(__0)) return;
        string plantId = HydroponicHelper.GetPlantId(__0);
        if (plantId != Tobacco.SeedId && plantId != Perfume.SeedId) return;
        if (!ReferenceEquals(MachineHydroponic.GetInputSlot(__2)?.childItem, __0)) return;

        // The original method may have capped the seed at the target from before this tick.
        // The machine owns progress and also resets it when a perennial starts its next cycle.
        var (progress, target) = MachineProgressHelper.GetProgress(__2);
        __0.ModifyTag("GROWING_PROGRESS", state => state.Enable().SetInt(Math.Min(progress, target)));
    }
}
