using System;

namespace CorpPresence;

internal static class WorkshopCycle
{
    internal static bool DrawPower(GameItem machine, GameSlotInventory slot)
        => DrawPower(slot, MachineryHelper.GetMachinePowerUsage(machine), () => MachineHelper.TryDrawCyclePower(machine, slot));

    internal static bool CanDrawPower(GameSlotInventory slot, int charge) =>
        slot.childItem is { } cell && PowerHelper.CanDrawPowerSource(cell, charge);

    internal static bool CanDrawPower(GameItem machine, GameSlotInventory slot, int charge) =>
        charge == MachineryHelper.GetMachinePowerUsage(machine) ? MachineHelper.CanPower(machine, slot) : CanDrawPower(slot, charge);

    internal static bool DrawPower(GameItem machine, GameSlotInventory slot, int charge) =>
        charge == MachineryHelper.GetMachinePowerUsage(machine) ? DrawPower(machine, slot) : DrawPower(slot, charge);

    internal static bool DrawPower(GameSlotInventory slot, int charge) =>
        DrawPower(slot, charge, () => PowerHelper.DrawPowerSource(slot.childItem, charge));

    // Native battery validation can change mounted inventory after the debit.
    internal static bool DrawForCurrentStock(GameSlotInventory slot, int cost, Func<bool> stockIsCurrent)
    {
        var session = StoreSession.Capture();
        var cell = slot.childItem;
        if (cell == null || !session.IsCurrent || !stockIsCurrent()) return false;
        const string energy = "power_source_item_energy";
        int before = cell.GetTagReadonly(energy).GetInt();
        bool refundable = cell.IsTag("power_source_item") && !CurrentConverterHelper.IsCurrentConverter(cell)
            && !HypertechBatteryHelper.IsHypertechBattery(cell);
        if (!DrawPower(slot, cost)) return false;
        if (session.IsCurrent && slot.childItem == cell && stockIsCurrent()) return true;
        if (refundable && session.IsCurrent && cost > 0 && before >= cost
            && cell.GetTagReadonly(energy).GetInt() == before - cost
            && GeneralHelper.IsItemOwned(cell) && ShopInventory.Contains(cell) && !StorePlacement.Destroyed(cell))
            HookGuard.Run("退回未开工的普通电池电量", () => PowerHelper.SetPowerSourceAt(cell, before));
        return false;
    }

    private static bool DrawPower(GameSlotInventory slot, int cost, Func<bool> draw)
    {
        var cell = slot.childItem;
        if (cell == null) return false;
        const string energy = "power_source_item_energy";
        int before = cell.GetTagReadonly(energy).GetInt();
        try { return draw(); }
        catch (Exception error)
        {
            ModLog.Warn("工坊电源回调中断，核对原版电池实扣：" + error.GetType().Name);
            return cell.IsTag("power_source_item") && cost > 0 && before >= cost
                && before - cell.GetTagReadonly(energy).GetInt() == cost;
        }
    }

    // A native inventory or power callback can request a save between two changes in one batch.
    internal static void Run(GameItem machine, Action settle)
    {
        var store = SaveSlot.CurrentStore;
        if (store == null || !GeneralHelper.IsItemOwned(machine) || StoreSession.CompanyProgressPaused
            || !SaveSlot.CanWrite("CorpPresence") || !StoreSession.Capture().IsCurrent || !CompanyTransaction.TryEnter(store)) return;
        try { settle(); }
        finally { CompanyTransaction.Exit(); }
    }
}
