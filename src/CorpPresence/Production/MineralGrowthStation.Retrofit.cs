using System;
using System.Globalization;

namespace CorpPresence;

internal static partial class MineralGrowthStation
{
    private const string RigTag = "CORP_MINERAL_GROWTH_RIG", DueTag = "CORP_MINERAL_GROWTH_DUE";
    private const string RigNightTag = "CORP_MINERAL_GROWTH_RIG_NIGHT";
    private const int CoilBit = 1, GuideBit = 2;
    private const int GuideWaterSaving = 6;

    private enum NightDue { None, FeedAndWater, Water }

    private sealed record GrowthRate(int Charge, int Water, int Work, int Quality, int Parts = 0)
    {
        internal bool Retrofitted => Parts != 0;
        internal bool Uses(int bit) => (Parts & bit) != 0;
    }

    private static bool HasProfile(GameItem machine) => !string.IsNullOrEmpty(machine.GetTagReadonly(RigTag).GetString());
    private static NightDue ReadDue(GameItem machine) => (NightDue)machine.GetTagReadonly(DueTag).GetInt();

    private static GrowthRate LiveRate(GameItem machine) => new(
        MachineryHelper.GetMachinePowerUsage(machine),
        MineralGrowthBatch.WaterCost(machine.GetTagReadonly(ModuleHelper.TOTAL_PERCENTAGE_POWER_BONUS_INT).GetInt()),
        machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt(),
        Math.Clamp(machine.GetTagReadonly(ModuleHelper.TOTAL_PERCENTAGE_QUALITY_BONUS_INT).GetInt(), 0, 25));

    private static GrowthRate LockedRate(GameItem machine, MineralGrowthBatch.Recipe recipe)
    {
        string text = machine.GetTagReadonly(RigTag).GetString();
        if (string.IsNullOrEmpty(text)) return LiveRate(machine);
        if (text.Length > 32 || machine.GetTagReadonly(ProgressTag).GetInt() is < 0 or > MineralGrowthBatch.RequiredWork) return null;
        var fields = text.Split('|');
        return recipe == MineralGrowthBatch.Ore && fields.Length == 6 && fields[0] == "1"
            && Read(1, out int charge) && charge is >= 1 and <= int.MaxValue / MineralGrowthBatch.RequiredWork
            && Read(2, out int water) && water is >= 1 and <= 20
            && Read(3, out int work) && work is >= 1 and <= MineralGrowthBatch.RequiredWork
            && Read(4, out int quality) && quality is >= 0 and <= 25
            && Read(5, out int parts) && parts is >= CoilBit and <= (CoilBit | GuideBit)
            ? new(charge, water, work, quality, parts) : null;

        bool Read(int index, out int value) => int.TryParse(fields[index], NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private static void FreezeRate(GameItem machine, GrowthRate rate) => machine.ModifyTag(RigTag,
        tag => tag.Enable().SetString(FormattableString.Invariant($"1|{rate.Charge}|{rate.Water}|{rate.Work}|{rate.Quality}|{rate.Parts}")),
        skipValidate: true);

    private static bool ValidDue(GrowthRate rate, NightDue due, int progress) => due == NightDue.None || rate.Retrofitted &&
        (due == NightDue.FeedAndWater && progress == 0 || due == NightDue.Water && progress is > 0 and < MineralGrowthBatch.RequiredWork);

    private static long NightNumber => (long)StoreStation.GetDayCounter() + 1;

    private static bool AlreadyRan(GameItem machine) => NightNumber > 0 && machine.GetTagReadonly(RigNightTag).GetLong() == NightNumber;

    private static void MarkNight(GameItem machine) =>
        machine.ModifyTag(RigNightTag, tag => tag.Enable().SetLong(NightNumber), skipValidate: true);

    internal static bool GuideStock(RodCondition stock) => stock is { Process: RodProcess.Densify, Jacket: null,
        Surface: { Corrosion: >= 45, Porosity: <= 1 } };

    private static bool IsGuide(GameItem item) => MachineService.IsStockPart(item, RodMaterial.GrowthGuide)
        && GuideStock(RodCondition.Read(item));

    private static GrowthRate DraftRate(GameItem machine, MineralGrowthBatch.Recipe recipe, GameItem coil, GameItem guide)
    {
        var rate = LiveRate(machine);
        if (recipe != MineralGrowthBatch.Ore || !MachineService.Handbook) return rate;
        bool lowLoss = MachineService.HasLowLossCoil(coil) && rate.Charge > 1;
        bool savesWater = IsGuide(guide) && rate.Water > 1;
        int parts = (lowLoss ? CoilBit : 0) | (savesWater ? GuideBit : 0);
        return parts == 0 ? rate : new(lowLoss ? Math.Max(1, rate.Charge - 3) : Math.Max(1, rate.Charge),
            savesWater ? Math.Max(1, rate.Water - GuideWaterSaving) : rate.Water,
            Math.Min(rate.Work, MineralGrowthBatch.RequiredWork), rate.Quality, parts);
    }
}
