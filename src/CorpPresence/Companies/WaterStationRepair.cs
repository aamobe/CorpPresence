using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static class WaterStationRepair
{
    internal const string LoanTag = "CORP_WATER_PUMP_REPAIR_RUN";

    internal static bool Unlocked(CorpState root) => root?.Workshop?.FirstOverhaul?.CompletedDay is int completed
        && completed <= StoreStation.GetDayCounter() && root.Workshop.FeederTrial?.Handbook == true
        && MeridianIndustrial.Eligible(root);

    internal static bool EngineeringCleared(CorpState root) => root?.Workshop?.PumpRepair is { CompletedDay: int day } receipt
        && receipt.Valid() && day <= StoreStation.GetDayCounter();

    internal static bool EligibleBuiltPump(GameItem item) => item?.identifier == "corp_presence_" + IndustrialPump.Key
        && item.unitCount == 1 && string.IsNullOrEmpty(item.GetTagReadonly(LoanTag).GetString())
        && GeneralHelper.IsItemOwned(item) && !StorePlacement.Destroyed(item) && ShopInventory.Contains(item);

    private static bool Current(CorpState root, StoreSession session) => session.IsCurrent
        && ReferenceEquals(CorpState.Current, root) && root?.Workshop != null && !string.IsNullOrEmpty(root.RunId)
        && !StoreSession.CompanyProgressPaused && SaveSlot.CanWrite("CorpPresence");

    internal static GameItem Find() => Find(CorpState.Current);

    private static GameItem Find(CorpState root)
    {
        if (root?.Workshop?.PumpRepair is not { PumpId: > 0, ReceivedDay: null } receipt) return null;
        return EmporiumEntry.Instance?.GetAllOwnedItems(includeAfterhour: true)?.FirstOrDefault(item =>
            item?.identifier == "corp_presence_" + IndustrialPump.Key && item.uniqueId == receipt.PumpId && item.unitCount == 1
            && item.GetTagReadonly(LoanTag).GetString() == root.RunId && GeneralHelper.IsItemOwned(item)
            && !StorePlacement.Destroyed(item) && ShopInventory.Contains(item));
    }

    internal static SaleStatus Claim()
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return SaleStatus.Unavailable;
        try
        {
            var root = CorpState.Current;
            var session = StoreSession.Capture();
            if (!Current(root, session) || !Unlocked(root)) return SaleStatus.Unavailable;
            var receipt = root.Workshop.PumpRepair;
            if (receipt != null)
            {
                if (!receipt.Valid() || receipt.InFlight) return SaleStatus.Frozen;
                if (receipt.ReceivedDay.HasValue) return SaleStatus.TrialSettled;
                return Find(root) != null ? SaleStatus.Ready : SaleStatus.Changed;
            }
            if (!WorkshopCounter.HasRoom) return SaleStatus.Short;
            return Issue(root, session, StoreStation.GetDayCounter());
        }
        finally { CompanyTransaction.Exit(); }
    }

    private static SaleStatus Issue(CorpState root, StoreSession session, int day)
    {
        var machine = DirectoryMaster.Item("corp_presence_" + IndustrialPump.Key, isOwned: true);
        if (machine == null) return SaleStatus.Unavailable;
        var staged = new List<GameItem>();
        try
        {
            if (!WorkshopCounter.CanFit(machine)) return SaleStatus.Short;
            int id = machine.GetUniqueID();
            if (id <= 0 || !IndustrialPump.PopulateRepairPump(machine, staged)) return SaleStatus.Changed;
            machine.ModifyTag(LoanTag, tag => tag.Enable().SetString(root.RunId), skipValidate: true);
            Goods.Relabel(machine, IndustrialPump.Key);
            if (!Current(root, session) || root.Workshop.PumpRepair != null) return SaleStatus.Changed;
            bool placed = WorkshopCounter.TryPlaceOnCounter(machine);
            if (!Current(root, session)) return SaleStatus.Changed;
            if (!placed && machine.parentInventory == null) return SaleStatus.Short;
            // If a native callback moved the placed pump, keep its identity instead of issuing a second body.
            root.Workshop.PumpRepair = new WaterPumpRepairState { PumpId = id, IssuedDay = day };
            return placed ? SaleStatus.Ready : SaleStatus.Changed;
        }
        finally
        {
            if (machine.parentInventory == null)
            {
                foreach (var part in staged) HookGuard.Run("撤回未交柜旧泵部件", () => IndustrialWorkbench.Discard(part));
                CompanyVisitors.DiscardUnshipped(machine);
            }
        }
    }

    internal static SaleStatus Check(out int amount, GameItem built = null) =>
        Inspect(CorpState.Current, StoreSession.Capture(), built, out _, out amount);

    private static SaleStatus Inspect(CorpState root, StoreSession session, GameItem built, out GameItem machine, out int amount)
    {
        machine = null;
        amount = 0;
        if (!Current(root, session)) return SaleStatus.Unavailable;
        var receipt = root.Workshop.PumpRepair;
        if (receipt == null && (built == null || !Unlocked(root))) return SaleStatus.Unavailable;
        if (receipt != null && (!receipt.Valid() || receipt.InFlight || StoreStation.GetDayCounter() < (receipt.ReceivedDay ?? receipt.IssuedDay)))
            return SaleStatus.Frozen;
        if (receipt?.CompletedDay.HasValue == true) return SaleStatus.TrialSettled;
        if (receipt?.ReceivedDay.HasValue == true) amount = receipt.Amount;
        else
        {
            var loan = Find(root);
            if (built != null && loan != null) return SaleStatus.Outstanding;
            if (built != null && !EligibleBuiltPump(built)) return SaleStatus.Changed;
            machine = built ?? loan;
            if (machine == null || !machine.MayRemove() || machine.MaxNumRemove() < 1) return SaleStatus.Changed;
            var flow = IndustrialPump.Passed(machine, IndustrialPump.Test.Flow);
            bool pressure = IndustrialPump.Passed(machine, IndustrialPump.Test.Pressure) != null;
            if (flow == null || IndustrialPump.Passed(machine, IndustrialPump.Test.Restart) == null
                || built != null && !pressure) return SaleStatus.Short;
            if (!IndustrialPump.ReadyForCollection(machine)) return SaleStatus.Outstanding;
            amount = built == null ? WaterPumpRepairState.BasePay : WaterPumpRepairState.BaseBuildPay;
            if (flow.Charge <= 9) amount += WaterPumpRepairState.LowLossBonus;
            if (built == null && pressure) amount += WaterPumpRepairState.PressureBonus;
        }
        var store = SaveSlot.CurrentStore;
        if (store == null) return SaleStatus.Unavailable;
        return store.GetCash() > int.MaxValue - (amount - (receipt?.Paid ?? 0)) ? SaleStatus.WalletFull : SaleStatus.Ready;
    }

    internal static SaleStatus Submit(int? quotedAmount = null, GameItem built = null)
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return SaleStatus.Unavailable;
        try
        {
            var root = CorpState.Current;
            var session = StoreSession.Capture();
            var status = Inspect(root, session, built, out var machine, out int amount);
            if (status != SaleStatus.Ready) return status;
            if (quotedAmount.HasValue && quotedAmount != amount) return SaleStatus.Changed;
            var store = SaveSlot.CurrentStore;
            var previous = root.Workshop.PumpRepair;
            int day = StoreStation.GetDayCounter();
            var receipt = previous ?? new WaterPumpRepairState { IssuedDay = day };
            bool SameReceipt() => Current(root, session) && ReferenceEquals(root.Workshop.PumpRepair, receipt);

            if (machine != null)
            {
                var stock = new WorkshopSaleStock(machine, false);
                int? fabricatedId = null;
                if (built != null)
                {
                    int id = built.GetUniqueID();
                    if (id <= 0 || id == receipt.PumpId) return SaleStatus.Changed;
                    fabricatedId = id;
                }
                if (!Current(root, session) || !ReferenceEquals(root.Workshop.PumpRepair, previous)) return SaleStatus.Changed;
                receipt.FabricatedPumpId = fabricatedId;
                receipt.InFlight = true;
                root.Workshop.PumpRepair = receipt;
                try { stock.RemoveUnits(1); }
                catch (Exception error) { ModLog.Warn("水站整泵交接回调中断：" + error.GetType().Name); }
                if (!SameReceipt()) return SaleStatus.Frozen;
                int received = stock.RemovedUnits();
                if (received is not (0 or 1)) return SaleStatus.Frozen;
                receipt.InFlight = false;
                if (received == 0)
                {
                    receipt.FabricatedPumpId = null;
                    root.Workshop.PumpRepair = previous;
                    return SaleStatus.Changed;
                }
                receipt.ReceivedDay = day;
                receipt.Amount = amount;
            }

            int due = receipt.Amount - receipt.Paid;
            int before = store.GetCash();
            if (before > int.MaxValue - due) return SaleStatus.WalletFull;
            receipt.InFlight = true;
            try { store.ModCash(due, playSound: false); }
            catch (Exception error) { ModLog.Warn("水站返修付款回调中断：" + error.GetType().Name); }
            if (!SameReceipt()) return SaleStatus.Frozen;
            long paid = (long)store.GetCash() - before;
            if (paid < 0 || paid > due) return SaleStatus.Frozen;
            receipt.Paid += (int)paid;
            receipt.InFlight = false;
            if (receipt.Paid != receipt.Amount) return SaleStatus.Partial;
            receipt.CompletedDay = day;
            return SaleStatus.Paid;
        }
        finally { CompanyTransaction.Exit(); }
    }
}
