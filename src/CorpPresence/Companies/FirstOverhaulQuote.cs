using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal sealed class FirstOverhaulQuote : OverhaulQuote
{
    internal FirstOverhaulQuote() : base(false) { }
}

internal sealed class RotationQuote : OverhaulQuote
{
    internal RotationQuote() : base(true) { }
    internal new RotationContract Contract => (RotationContract)base.Contract;
}

internal abstract class OverhaulQuote
{
    private readonly bool _rotation;
    private readonly FirstOverhaulState _first;
    private readonly RotationContract _latest;
    internal readonly int Window;
    internal readonly RotationKind Kind;
    internal readonly TruckSpec[] Specs;
    private readonly StoreSession _session = StoreSession.Capture();
    private readonly WorkshopState _workshop;
    private readonly OverhaulTruck _truck;
    private readonly bool _abandoned;
    private readonly int[] _received, _kinds;
    private readonly int _paid;
    internal readonly OverhaulContractState Contract;
    internal readonly int Day = StoreStation.GetDayCounter();
    internal readonly int TruckIndex;
    internal readonly List<WorkshopSaleStock> Stock = new();
    internal readonly string[] Slots = Array.Empty<string>();
    internal readonly int[] Sources = Array.Empty<int>();
    internal int BasePrice => _truck?.BasePrice ?? Specs[0].BasePrice;
    internal int Bonus => _truck?.Bonus ?? Specs[0].Bonus;
    internal int DueDay => _truck?.DueDay ?? Day + Specs[0].Days;
    internal int Amount => _truck?.Quote(Day) ?? BasePrice + Bonus;

    internal static bool Eligible(CorpState root) => root != null && MeridianIndustrial.Eligible(root)
        && WorkshopSaleOffer.GelOrderSettled(root) && CartelShizuka.BlueprintDelivered(root);

    protected OverhaulQuote(bool rotation)
    {
        _workshop = CorpState.Current?.Workshop;
        _rotation = rotation;
        _first = _workshop?.FirstOverhaul;
        _latest = _workshop?.Rotations?.LastOrDefault();
        Window = RotationContract.WindowAt(_first?.CompletedDay, Day);
        var subject = _latest != null && (_latest.Window == Window || !_latest.Abandoned && !_latest.CompletedDay.HasValue)
            ? _latest : null;
        Contract = rotation ? subject : _first;
        Kind = rotation ? subject?.Kind ?? (RotationKind)(Math.Max(0, Window) % 4) : RotationKind.Joint;
        Specs = TruckSpec.For(Kind);
        if (Contract == null || !Contract.Valid()) return;
        _abandoned = Contract.Abandoned;
        TruckIndex = Contract.NextTruck;
        if (TruckIndex == Specs.Length) return;
        _truck = Contract.Trucks[TruckIndex];
        _paid = _truck.Paid;
        _received = (int[])_truck.Received.Clone();
        var ids = Specs[TruckIndex].ItemIds;
        var quantities = Specs[TruckIndex].Quantities;
        _kinds = Enumerable.Range(0, ids.Length)
            .SelectMany(kind => Enumerable.Repeat(kind, quantities[kind] - _received[kind])).ToArray();
        Slots = _kinds.Select(kind => ids[kind]).ToArray();
        Sources = Enumerable.Repeat(-1, Slots.Length).ToArray();
        if (Slots.Length == 0 || Availability() != SaleStatus.Ready) return;

        Stock.AddRange(OwnedStock());
        for (int i = 0; i < Stock.Count; i++)
        {
            int remaining = Stock[i].Removable;
            for (int slot = 0; slot < Slots.Length && remaining > 0; slot++)
            {
                if (Sources[slot] >= 0 || Slots[slot] != Stock[i].Item.identifier) continue;
                Sources[slot] = i;
                remaining--;
            }
        }
    }

    private List<WorkshopSaleStock> OwnedStock()
    {
        var result = new List<WorkshopSaleStock>();
        var owned = EmporiumEntry.Instance?.GetAllOwnedItems(includeAfterhour: false);
        if (owned == null) return result;
        foreach (var item in owned.Distinct())
        {
            if (item == null || !Slots.Contains(item.identifier) || !Accepts(item)) continue;
            var stock = new WorkshopSaleStock(item, false);
            if (stock.Removable > 0) result.Add(stock);
        }
        return result;
    }

    // 返修总成带回工艺台原料的壳位，按实物移出结果一次销账。
    private ShellReceipt SourceShell(GameItem item)
    {
        if (Contract is not RotationContract { Kind: RotationKind.Repair } repair || item.identifier != "corp_presence_" + IndustrialGoods.Unit || item.unitCount != 1
            || !IndustrialGoods.TryReadOrigin(item, out var run, out int serial, out int slot)
            || run != CorpState.Current?.RunId || serial != repair.Serial || slot < 0 || slot >= repair.Shells.Length) return null;
        var shell = repair.Shells[slot];
        return shell.Issued && !shell.Returned && !shell.Delivered && !shell.InFlight ? shell : null;
    }

    private bool Accepts(GameItem item)
    {
        if (item == null || StorePlacement.Destroyed(item) || item.unitCount < 1 || !GeneralHelper.IsItemOwned(item)
            || !ShopInventory.Contains(item) || !item.MayRemove()) return false;
        if (item.identifier != "corp_presence_" + IndustrialGoods.Unit) return true;
        return IndustrialGoods.TryReadOrigin(item, out _, out _, out _) ? SourceShell(item) != null : IndustrialGoods.IsFreshUnit(item);
    }

    private bool StillHere(WorkshopSaleStock stock, string id, int count) =>
        stock.Item.identifier == id && ReferenceEquals(stock.Inventory, stock.Item.parentInventory)
        && stock.Units == stock.Item.unitCount && Accepts(stock.Item) && stock.Item.MaxNumRemove() >= count;

    private bool Bound() => _session.IsCurrent && !StoreSession.CompanyProgressPaused && SaveSlot.CanWrite("CorpPresence")
        && Day >= 0 && Day == StoreStation.GetDayCounter() && ReferenceEquals(_workshop, CorpState.Current?.Workshop)
        && _workshop != null && ReferenceEquals(_first, _workshop.FirstOverhaul)
        && (_rotation ? Window >= 0 && Window == RotationContract.WindowAt(_first?.CompletedDay, Day)
            && _workshop.Rotations != null && ReferenceEquals(_latest, _workshop.Rotations.LastOrDefault())
            && (_latest == null || _latest.Window <= Window && !(_latest.CompletedDay > Day))
            : Eligible(CorpState.Current));

    private bool SameTruck() => Bound() && Contract.Trucks?.Length == Specs.Length
        && ReferenceEquals(_truck, Contract.Trucks[TruckIndex]);

    private SaleStatus Snapshot()
    {
        if (!Bound()) return SaleStatus.Unavailable;
        if (Contract == null) return SaleStatus.Ready;
        if (!Contract.Valid()) return SaleStatus.Frozen;
        if (Day < Contract.SignedDay || (TruckIndex == 1 && Day < Contract.Trucks[0].CompletedDay)
            || _truck?.StartedDay > Day) return SaleStatus.Unavailable;
        if (Contract.NextTruck != TruckIndex || Contract.Abandoned != _abandoned) return SaleStatus.Changed;
        if (_truck == null) return SaleStatus.Paid;
        if (_truck.InFlight) return SaleStatus.Frozen;
        return ReferenceEquals(Contract.Trucks[TruckIndex], _truck) && _truck.Paid == _paid
            && _truck.Received.SequenceEqual(_received) ? SaleStatus.Ready : SaleStatus.Changed;
    }

    internal SaleStatus Availability()
    {
        var status = Snapshot();
        if (status != SaleStatus.Ready) return status;
        return Contract == null || Contract.Abandoned ? SaleStatus.Unavailable : SaleStatus.Ready;
    }

    internal bool CanSign => Snapshot() == SaleStatus.Ready && (Contract == null || Contract.Abandoned)
        && (Contract != null || Day <= int.MaxValue - Specs.Max(s => s.Days));

    internal SaleStatus Sign()
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return SaleStatus.Unavailable;
        try
        {
            var status = Snapshot();
            if (status != SaleStatus.Ready) return status;
            if (Contract == null)
            {
                if (Day > int.MaxValue - Specs.Max(s => s.Days)) return SaleStatus.Unavailable;
                if (_rotation) _workshop.Rotations.Add(RotationContract.Sign(Window, Day));
                else _workshop.FirstOverhaul = FirstOverhaulState.Sign(Day);
            }
            else
            {
                if (_rotation && (!Contract.Abandoned || ((RotationContract)Contract).Window != Window)) return SaleStatus.Unavailable;
                Contract.Abandoned = false;
            }
            return SaleStatus.Ready;
        }
        finally { CompanyTransaction.Exit(); }
    }

    internal bool CanAbandon => Availability() == SaleStatus.Ready && !_truck.StartedDay.HasValue;

    internal SaleStatus Abandon()
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return SaleStatus.Unavailable;
        try
        {
            var status = Availability();
            if (status != SaleStatus.Ready) return status;
            if (_truck.StartedDay.HasValue) return SaleStatus.Outstanding;
            Contract.Abandoned = true;
            return SaleStatus.Ready;
        }
        finally { CompanyTransaction.Exit(); }
    }

    internal SaleStatus Check()
    {
        var status = Availability();
        if (status != SaleStatus.Ready) return status;
        if (SaveSlot.CurrentStore.GetCash() > int.MaxValue - (Amount - _paid)) return SaleStatus.WalletFull;
        for (int slot = 0; slot < Slots.Length; slot++)
        {
            int index = Sources[slot];
            if (index < 0 || index >= Stock.Count || Slots[slot] != Stock[index].Item.identifier) return SaleStatus.Short;
        }
        var chosenShells = new HashSet<ShellReceipt>();
        foreach (var group in Sources.GroupBy(i => i))
        {
            var stock = Stock[group.Key];
            if (stock.Removable < group.Count()) return SaleStatus.Short;
            if (!StillHere(stock, stock.Item.identifier, group.Count())) return SaleStatus.Changed;
            var shell = SourceShell(stock.Item);
            if (shell != null && !chosenShells.Add(shell)) return SaleStatus.Changed;
        }
        return SaleStatus.Ready;
    }

    internal SaleStatus Submit()
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return SaleStatus.Unavailable;
        try
        {
            var status = Check();
            if (status != SaleStatus.Ready) return status;
            var store = SaveSlot.CurrentStore;
            int amount = Amount;
            // 单价、奖金和全部所选实物在首次原生回调前锁定。后续重试只能改写未收件的原生 ID。
            var groups = Enumerable.Range(0, Slots.Length).GroupBy(slot => Sources[slot])
                .Select(group => (Stock: Stock[group.Key], Kind: _kinds[group.First()], Count: group.Count())).ToArray();
            var sourceIds = (int[])_truck.SourceIds.Clone();
            var offsets = (int[])_received.Clone();
            for (int i = 1; i < offsets.Length; i++) offsets[i] += Specs[TruckIndex].Quantities.Take(i).Sum();
            foreach (var group in groups)
            {
                int id = group.Stock.Item.GetUniqueID();
                if (id <= 0) return SaleStatus.Changed;
                for (int unit = 0; unit < group.Count; unit++) sourceIds[offsets[group.Kind]++] = id;
            }

            // 剩余货物可以重新选择，已收部分的记录与第一次锁定的价格保留。
            _truck.SourceIds = sourceIds;
            _truck.StartedDay ??= Day;
            _truck.Amount = amount;
            foreach (var group in groups)
            {
                if (!SameTruck() || !StillHere(group.Stock, Specs[TruckIndex].ItemIds[group.Kind], group.Count)) break;
                var shell = SourceShell(group.Stock.Item);
                _truck.InFlight = true;
                if (shell != null) shell.InFlight = true;
                try { group.Stock.RemoveUnits(group.Count); }
                catch (Exception ex) { Warn(ex); }
                if (!SameTruck()) return SaleStatus.Frozen;
                int removed = group.Stock.RemovedUnits();
                if (removed < 0 || removed > group.Count) return SaleStatus.Frozen;
                _truck.Received[group.Kind] += removed;
                if (shell != null)
                {
                    shell.Delivered = removed > 0;
                    shell.InFlight = false;
                }
                _truck.InFlight = false;
                if (removed != group.Count) break;
            }
            if (!_truck.Received.SequenceEqual(Specs[TruckIndex].Quantities))
            {
                if (_truck.Received.Any(n => n > 0)) return SaleStatus.Partial;
                _truck.StartedDay = null;
                _truck.Amount = 0;
                Array.Clear(_truck.SourceIds, 0, _truck.SourceIds.Length);
                return SaleStatus.Changed;
            }
            if (!SameTruck()) return SaleStatus.Unavailable;
            int due = amount - _truck.Paid;
            int before = store.GetCash();
            if (before > int.MaxValue - due) return SaleStatus.WalletFull;
            _truck.InFlight = true;
            try { store.ModCash(due, playSound: false); }
            catch (Exception ex) { Warn(ex); }
            if (!SameTruck()) return SaleStatus.Frozen;
            long paid = (long)store.GetCash() - before;
            if (paid < 0 || paid > due) return SaleStatus.Frozen;
            _truck.Paid += (int)paid;
            _truck.InFlight = false;
            if (_truck.Paid != amount) return SaleStatus.Partial;
            _truck.CompletedDay = Day;
            if (TruckIndex == Specs.Length - 1) Contract.CompletedDay = Day;
            return SaleStatus.Paid;
        }
        finally { CompanyTransaction.Exit(); }
    }

    private static void Warn(Exception error)
    {
        try { ModLog.Warn("水站大修原生交割回调异常，凭单按实收核对：" + error.Message); } catch { }
    }
}
