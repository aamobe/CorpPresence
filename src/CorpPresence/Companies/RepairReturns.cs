using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal enum RepairResult { Done, Unavailable, NoRoom, Changed, Frozen }

internal static class RepairReturns
{
    internal static int Unclaimed(RotationContract contract) =>
        contract?.Kind == RotationKind.Repair && !contract.Abandoned && !contract.CompletedDay.HasValue
            ? contract.Shells.Count(s => !s.Issued && !s.InFlight) : 0;

    internal static bool LiningDue(RotationContract contract) => contract?.Kind == RotationKind.Repair
        && contract.CompletedDay.HasValue && !contract.LiningClaimed;

    internal static IReadOnlyList<RotationContract> Pending() =>
        CorpState.Current?.Workshop?.Rotations?.Where(c => c?.Kind == RotationKind.Repair && c.Valid()
            && (Unclaimed(c) > 0 || LiningDue(c) || c.Shells.Any(s => s.InFlight || Open(s)))).ToArray()
        ?? Array.Empty<RotationContract>();

    private static bool Open(ShellReceipt slot) => slot.Issued && !slot.Returned && !slot.Delivered && !slot.InFlight;

    private static bool Current(RotationContract contract, StoreSession session) =>
        contract?.Kind == RotationKind.Repair && session.IsCurrent && !StoreSession.CompanyProgressPaused
        && SaveSlot.CanWrite("CorpPresence") && CorpState.Current?.Workshop?.Rotations?.Contains(contract) == true
        && !string.IsNullOrWhiteSpace(CorpState.Current.RunId) && StoreStation.GetDayCounter() >= contract.SignedDay
        && !(contract.CompletedDay > StoreStation.GetDayCounter());

    private static ShellReceipt Source(RotationContract contract, GameItem item)
    {
        if (item == null || item.unitCount != 1 || StorePlacement.Destroyed(item)
            || !IndustrialGoods.TryReadOrigin(item, out var run, out int instance, out int index)
            || run != CorpState.Current?.RunId || contract?.Serial != instance || index < 0 || index >= contract.Shells.Length)
            return null;
        return contract.Shells[index];
    }

    // The workbench can transform an outstanding shell after retirement as well.
    internal static bool Outstanding(GameItem item)
    {
        if (!IndustrialGoods.TryReadOrigin(item, out var run, out int instance, out _)
            || run != CorpState.Current?.RunId) return false;
        var contract = CorpState.Current?.Workshop?.Rotations?.Find(c => c?.Serial == instance);
        return Current(contract, StoreSession.Capture()) && contract.Valid()
            && Source(contract, item) is ShellReceipt slot && Open(slot);
    }

    private static bool Owned(GameItem item) => item != null && GeneralHelper.IsItemOwned(item)
        && ShopInventory.Contains(item) && item.MayRemove() && item.MaxNumRemove() >= 1;

    internal static GameItem[] Selectable(RotationContract contract)
    {
        if (!Current(contract, StoreSession.Capture()) || !contract.Valid()) return Array.Empty<GameItem>();
        return EmporiumEntry.Instance?.GetAllOwnedItems(includeAfterhour: false)?.Distinct()
            .Where(item => Owned(item) && Source(contract, item) is ShellReceipt slot && Open(slot)).ToArray()
            ?? Array.Empty<GameItem>();
    }

    internal static RepairResult Claim(RotationContract contract) => Issue(contract, lining: false);
    internal static RepairResult ClaimLining(RotationContract contract) => Issue(contract, lining: true);

    private static RepairResult Issue(RotationContract contract, bool lining)
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return RepairResult.Unavailable;
        GameItem product = null;
        try
        {
            var session = StoreSession.Capture();
            if (!Current(contract, session)) return RepairResult.Unavailable;
            if (!contract.Valid()) return RepairResult.Frozen;
            int index = Array.FindIndex(contract.Shells, s => !s.Issued && !s.InFlight);
            if (lining ? !LiningDue(contract) : Unclaimed(contract) == 0) return RepairResult.Unavailable;
            if (lining && contract.LiningInFlight) return RepairResult.Frozen;
            var counter = EmporiumEntry.Instance?.backInvinvElement;
            if (counter == null || !WorkshopCounter.HasRoom) return RepairResult.NoRoom;
            product = Goods.Make(lining ? "spent_lining" : IndustrialGoods.OldShell);
            if (product == null) return RepairResult.Unavailable;
            if (!lining) IndustrialGoods.MarkOrigin(product, CorpState.Current.RunId, contract.Serial, index);
            if (!Current(contract, session)) return RepairResult.Unavailable;

            void InFlight(bool busy)
            {
                if (lining) contract.LiningInFlight = busy;
                else contract.Shells[index].InFlight = busy;
            }
            InFlight(true);
            bool placed = Place(product, counter);
            if (!Current(contract, session)) return RepairResult.Frozen;
            if (!placed && (product.parentInventory != null || counter.childItems.Contains(product) || StorePlacement.Destroyed(product)))
                return RepairResult.Frozen;
            if (placed)
            {
                if (product.unitCount != 1 || !GeneralHelper.IsItemOwned(product)
                    || !lining && Source(contract, product) != contract.Shells[index]) return RepairResult.Frozen;
                if (lining) contract.LiningClaimed = true;
                else contract.Shells[index].Issued = true;
            }
            InFlight(false);
            return placed ? RepairResult.Done : RepairResult.NoRoom;
        }
        catch (Exception error) { Warn(error); return RepairResult.Frozen; }
        finally
        {
            try { CompanyVisitors.DiscardUnshipped(product); }
            finally { CompanyTransaction.Exit(); }
        }
    }

    internal static RepairResult Return(RotationContract contract, GameItem item)
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return RepairResult.Unavailable;
        try
        {
            var session = StoreSession.Capture();
            if (!Current(contract, session)) return RepairResult.Unavailable;
            if (!contract.Valid()) return RepairResult.Frozen;
            var slot = Source(contract, item);
            if (slot == null || !Open(slot) || !Owned(item)) return RepairResult.Changed;
            var stock = new WorkshopSaleStock(item, false);
            var counter = EmporiumEntry.Instance.backInvinvElement;
            if (counter == null) return RepairResult.Unavailable;
            var refund = new List<GameItem>();
            slot.InFlight = true;

            RepairResult Undo(RepairResult reason, GameItem unplaced = null)
            {
                foreach (var product in refund)
                    if (product.unitCount != 1 || StorePlacement.Destroyed(product)
                        || !ReferenceEquals(product.parentInventory, product == unplaced ? null : counter))
                        return RepairResult.Frozen;
                ProductionOutput.DiscardBatch(refund);
                // Native Destroy can set its destroyed flag before Expel calls inventory callbacks.
                // A remaining graph child will be serialized even when that flag is already set.
                if (!Current(contract, session) || refund.Any(p => p.parentInventory != null || counter.childItems.Contains(p)))
                    return RepairResult.Frozen;
                slot.InFlight = false;
                return reason;
            }

            // Stage all three owned parts at distinct counter positions before accepting the customer's assembly.
            if (Goods.IdOf(item) == IndustrialGoods.Unit)
            {
                foreach (string key in new[] { IndustrialGoods.Blank, IndustrialGoods.Liner, CartelReactor.Gel })
                {
                    if (!Current(contract, session)) return RepairResult.Frozen;
                    var part = Goods.Make(key);
                    if (part == null) return Undo(RepairResult.Unavailable);
                    refund.Add(part);
                    if (!Place(part, counter))
                        return Current(contract, session) ? Undo(RepairResult.NoRoom, part) : RepairResult.Frozen;
                }
            }
            if (!Current(contract, session)) return RepairResult.Frozen;
            if (!ReferenceEquals(stock.Inventory, item.parentInventory) || !Owned(item) || Source(contract, item) != slot)
                return Undo(RepairResult.Changed);
            if (refund.Any(p => !OnCounter(p, counter)))
                return RepairResult.Frozen;
            try { stock.RemoveUnits(1); } catch (Exception error) { Warn(error); }
            if (!Current(contract, session)) return RepairResult.Frozen;
            int removed = stock.RemovedUnits();
            if (removed == 0) return Undo(RepairResult.Changed);
            if (removed != 1 || refund.Any(p => !OnCounter(p, counter)))
                return RepairResult.Frozen;
            slot.Returned = true;
            slot.InFlight = false;
            return RepairResult.Done;
        }
        catch (Exception error) { Warn(error); return RepairResult.Frozen; }
        finally { CompanyTransaction.Exit(); }
    }

    private static bool Place(GameItem item, GameInventory counter)
    {
        try { WorkshopCounter.TryPlaceOnCounter(item); } catch (Exception error) { Warn(error); }
        // Slot lookup and placement can both throw from native callbacks before or after attachment.
        return OnCounter(item, counter);
    }

    private static bool OnCounter(GameItem item, GameInventory counter) => counter != null
        && ReferenceEquals(counter, EmporiumEntry.Instance?.backInvinvElement) && ReferenceEquals(item.parentInventory, counter)
        && counter.childItems.Contains(item) && item.unitCount == 1 && !StorePlacement.Destroyed(item);

    private static void Warn(Exception error)
    {
        try { ModLog.Warn("返修实物交割中断，保留凭单：" + error.Message); } catch { }
    }
}
