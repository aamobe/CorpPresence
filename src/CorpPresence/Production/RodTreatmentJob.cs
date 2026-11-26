namespace CorpPresence;

internal static class RodTreatmentJob
{
    private const string Tag = "CORP_SINTERING_ROD_JOB";

    internal static RodProcess Read(GameItem machine) => RodCondition.Decode(machine.GetTagReadonly(Tag).GetString());
    internal static bool HasQueue(GameItem machine) => Read(machine) != RodProcess.Cast;

    internal static bool TryQueue(GameItem machine, GameItem rod, RodProcess process)
    {
        if (!LowerMachinist.MineralizerUnlocked(CorpState.Current) || RodCondition.Preview(rod, process) == null) return false;
        machine.ModifyTag(Tag, tag => tag.Enable().SetString(RodCondition.Code(process)), skipValidate: true);
        return true;
    }

    internal static void Clear(GameItem machine) => machine.DisableTag(Tag, skipValidate: true);

    internal static bool Ready(GameItem machine, GameItem rod, GameSlotInventory battery, int charge) =>
        LowerMachinist.MineralizerUnlocked(CorpState.Current)
        && machine.GetTagReadonly(MachineryHelper.IS_MACHINERY_POWERED).GetBool()
        && WorkshopCycle.CanDrawPower(machine, battery, charge) && RodCondition.Preview(rod, Read(machine)) != null;

    internal static MineralStop Run(GameItem machine, GameItem rod, GameSlotInventory battery, int charge)
    {
        if (!LowerMachinist.MineralizerUnlocked(CorpState.Current)) return MineralStop.Locked;
        var condition = RodCondition.Preview(rod, Read(machine));
        if (condition == null) return MineralStop.Insufficient;
        if (!machine.GetTagReadonly(MachineryHelper.IS_MACHINERY_POWERED).GetBool()
            || !WorkshopCycle.CanDrawPower(machine, battery, charge) || !WorkshopCycle.DrawPower(machine, battery, charge))
            return MineralStop.NoPower;
        condition.WriteProcess(rod);
        Clear(machine);
        HookGuard.Run("刷新棒料外观", () => Goods.RefreshRodSprite(rod));
        return MineralStop.Ready;
    }
}
