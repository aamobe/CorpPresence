using System;
using System.Linq;

namespace CorpPresence;

internal enum FeederTrialResult { Done, Waiting, Unavailable, Changed, Frozen }

internal static class CenFeederTrial
{
    private const string LoanTag = "CORP_FEEDER_LOAN_RUN";

    internal static FeederTrialResult Claim()
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return FeederTrialResult.Unavailable;
        try
        {
            var root = CorpState.Current;
            var session = StoreSession.Capture();
            if (!Current(root, session)) return FeederTrialResult.Unavailable;
            var trial = root.Workshop.FeederTrial ??= new FeederTrialState();
            if (!trial.Valid() || trial.ReturnInFlight) return FeederTrialResult.Frozen;
            if (trial.Returned) return FeederTrialResult.Done;
            bool SameTrial() => Current(root, session) && ReferenceEquals(root.Workshop.FeederTrial, trial);
            if (!trial.LoanId.HasValue && !TryIssueLoan(root, trial, SameTrial)) return FeederTrialResult.Waiting;
            if (!SameTrial()) return FeederTrialResult.Unavailable;
            int delivered = WorkshopCounter.DeliverCrate(FeederTrialState.ToolCount, trial.ToolsRemaining, index =>
                DirectoryMaster.Item(index == 0 ? "screwdriver" : "corp_presence_" + SleeveOiling.Lubricant, isOwned: true), SameTrial);
            if (!SameTrial()) return FeederTrialResult.Unavailable;
            trial.ToolsRemaining -= delivered;
            return trial.ToolsRemaining == 0 ? FeederTrialResult.Done : FeederTrialResult.Waiting;
        }
        finally { CompanyTransaction.Exit(); }
    }

    private static bool TryIssueLoan(CorpState root, FeederTrialState trial, Func<bool> current)
    {
        if (!WorkshopCounter.HasRoom) return false;
        var machine = DirectoryMaster.Item("corp_presence_" + MineralCrusher.FeederKey, isOwned: true);
        if (machine == null) return false;
        try
        {
            int id = machine.GetUniqueID();
            machine.ModifyTag(LoanTag, tag => tag.Enable().SetString(root.RunId), skipValidate: true);
            if (id <= 0 || !current() || !WorkshopCounter.TryPlaceOnCounter(machine) || !current()) return false;
            trial.LoanId = id;
            return true;
        }
        finally { CompanyVisitors.DiscardUnshipped(machine); }
    }

    internal static GameItem FindLoan()
    {
        var root = CorpState.Current;
        if (root?.Workshop?.FeederTrial is not { LoanId: > 0, Returned: false }) return null;
        return EmporiumEntry.Instance?.GetAllOwnedItems(includeAfterhour: false)?.FirstOrDefault(item => IsLoan(root, item));
    }

    private static bool IsLoan(CorpState root, GameItem item) => item?.identifier == "corp_presence_" + MineralCrusher.FeederKey
        && item.unitCount == 1 && !StorePlacement.Destroyed(item) && GeneralHelper.IsItemOwned(item)
        && ShopInventory.Contains(item) && item.GetTagReadonly(LoanTag).GetString() == root.RunId
        && item.GetUniqueID() == root.Workshop.FeederTrial?.LoanId;

    internal static void Record(GameItem machine, int partsProcessed)
    {
        var root = CorpState.Current;
        var trial = root?.Workshop?.FeederTrial;
        int day = StoreStation.GetDayCounter();
        if (trial == null || trial.Passed || trial.Returned || trial.ReturnInFlight || trial.LastNight >= day
            || day < 0 || partsProcessed < FeederAssembly.BatchSize || !IsLoan(root, machine)) return;
        trial.Batches = Math.Min(FeederTrialState.RequiredBatches, trial.Batches + partsProcessed / FeederAssembly.BatchSize);
        trial.LastNight = day;
    }

    internal static FeederTrialResult Return(GameItem machine)
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return FeederTrialResult.Unavailable;
        try
        {
            var root = CorpState.Current;
            var session = StoreSession.Capture();
            var trial = root?.Workshop?.FeederTrial;
            if (!Current(root, session) || trial is not { Passed: true, Returned: false }) return FeederTrialResult.Unavailable;
            if (trial.ReturnInFlight || !trial.Valid()) return FeederTrialResult.Frozen;
            if (!IsLoan(root, machine) || !FeederAssembly.Empty(machine) || !machine.MayRemove())
                return FeederTrialResult.Changed;
            var stock = new WorkshopSaleStock(machine, false);
            trial.ReturnInFlight = true;
            try { stock.RemoveUnits(1); }
            catch (Exception error) { ModLog.Warn("归还给料机时回调中断：" + error.GetType().Name); }
            if (!Current(root, session) || !ReferenceEquals(root.Workshop.FeederTrial, trial)) return FeederTrialResult.Frozen;
            int removed = stock.RemovedUnits();
            if (removed is not (0 or 1)) return FeederTrialResult.Frozen;
            trial.ReturnInFlight = false;
            trial.Returned = removed == 1;
            return trial.Returned ? FeederTrialResult.Done : FeederTrialResult.Changed;
        }
        finally { CompanyTransaction.Exit(); }
    }

    private static bool Current(CorpState root, StoreSession session) =>
        root?.Workshop != null && !string.IsNullOrWhiteSpace(root.RunId)
        && session.IsCurrent && ReferenceEquals(CorpState.Current, root) && !StoreSession.CompanyProgressPaused
        && SaveSlot.CanWrite("CorpPresence") && LowerMachinist.MineralizerUnlocked(root);
}
