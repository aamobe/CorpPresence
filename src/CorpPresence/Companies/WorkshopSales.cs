using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal enum SaleStatus { Ready, Unavailable, DailyLimit, Outstanding, Frozen, Short, Changed, WalletFull, Paid, Partial, TrialSettled }

internal sealed class WorkshopSaleStock
{
    internal readonly GameItem Item;
    internal readonly GameInventory Inventory;
    internal readonly int Units;
    internal readonly int Removable;
    internal readonly int[] Parts;
    internal readonly RodCondition Rod;

    internal WorkshopSaleStock(GameItem item, bool powder, bool rod = false)
    {
        Item = item;
        Inventory = item.parentInventory;
        Units = item.unitCount;
        Removable = Math.Min(Units, item.MaxNumRemove());
        if (powder) Parts = ReadParts(item);
        if (rod) Rod = RodCondition.Read(item);
    }

    internal static int[] ReadParts(GameItem item)
    {
        var parts = new int[PowderBox.ComponentCount];
        PowderBox.Read(item, parts);
        return parts;
    }

    internal void RemoveUnits(int count)
    {
        if (count < Units) { Item.SetUnitCount(Units - count); return; }
        try { Inventory.Expel(Item); }
        finally
        {
            if (Item.parentInventory == null && !StorePlacement.Destroyed(Item)) Item.Destroy();
        }
    }

    internal int RemovedUnits()
    {
        // Native Destroy marks the item before its inventory removal callbacks finish.
        // The save graph still encodes an attached child even if its destroyed flag is set.
        bool stillListed = Inventory.childItems.Contains(Item);
        if (Item.parentInventory == null) return stillListed ? -1 : Units;
        return !StorePlacement.Destroyed(Item) && stillListed && ReferenceEquals(Item.parentInventory, Inventory) && ShopInventory.Contains(Item)
            ? Units - Item.unitCount : -1;
    }

    internal bool CanStillSupply(WorkshopSaleOffer offer, int units) =>
        ReferenceEquals(Inventory, Item.parentInventory) && Units == Item.unitCount
        && WorkshopSaleQuote.Eligible(Item, offer)
        && Item.MaxNumRemove() >= units
        && (!offer.IsPowder || Parts.SequenceEqual(ReadParts(Item)))
        && (!offer.IsAlloy || Rod == RodCondition.Read(Item));
}

internal sealed class WorkshopSaleQuote
{
    internal readonly WorkshopSaleOffer Offer;
    internal readonly List<WorkshopSaleStock> Stock = new();
    // Each product unit has a source. A powder order always uses exactly one portable box.
    internal readonly int[] Sources;
    internal readonly WorkshopSaleReceipt Previous;
    internal readonly int Day = StoreStation.GetDayCounter();
    internal readonly int Needed;
    private readonly StoreSession _session = StoreSession.Capture();
    private readonly WorkshopState _workshop;
    private readonly WorkshopSaleState _sales;
    private readonly int _previousReceived, _previousPaid;
    private readonly bool _validReceipt;

    internal WorkshopSaleQuote(WorkshopSaleOffer offer, GameItem selectedRod = null)
    {
        Offer = offer;
        _workshop = CorpState.Current?.Workshop;
        _sales = _workshop?.Sales;
        Previous = CurrentReceipt;
        _validReceipt = Previous == null || Previous.ValidFor(offer.Book);
        _previousReceived = Previous?.Received ?? 0;
        _previousPaid = Previous?.Paid ?? 0;
        Needed = offer.Quantity - (Resuming ? _previousReceived : 0);
        Sources = new int[offer.IsPowder && Needed > 0 ? 1 : Needed];
        if (Availability() != SaleStatus.Ready || Needed == 0) return;
        IEnumerable<GameItem> owned = offer.IsAlloy ? new[] { selectedRod } :
            EmporiumEntry.Instance?.GetAllOwnedItems(includeAfterhour: false);
        if (owned == null) return;
        foreach (var item in owned.Distinct())
        {
            if (!Eligible(item, offer)) continue;
            var stock = new WorkshopSaleStock(item, offer.IsPowder, offer.IsAlloy);
            if (stock.Removable < 1) continue;
            if (offer.IsPowder && !CanSupplyPowder(stock)) continue;
            Stock.Add(stock);
        }
        int slot = 0;
        for (int i = 0; i < Stock.Count && slot < Sources.Length; i++)
            for (int n = 0; n < Stock[i].Removable && slot < Sources.Length; n++) Sources[slot++] = i;
        while (slot < Sources.Length) Sources[slot++] = -1;
    }

    private bool Resuming => _validReceipt && Previous != null && !Previous.Complete && Previous.Offer == Offer.Id;
    private WorkshopSaleReceipt CurrentReceipt => Offer.Book switch
    {
        WorkshopSaleBook.Contacts => _sales?.Contacts,
        WorkshopSaleBook.Rods => _sales?.Rods,
        WorkshopSaleBook.MeridianFeeder => _workshop?.FeederTrial?.Spec,
        WorkshopSaleBook.FirstGel => _workshop?.GelOrder,
        WorkshopSaleBook.CartelDaily => _workshop?.CartelDaily,
        _ => _sales?.Get(Offer.IsPowder)
    };
    private void PutReceipt(WorkshopSaleReceipt receipt)
    {
        switch (Offer.Book)
        {
            case WorkshopSaleBook.Contacts: _sales.Contacts = receipt; break;
            case WorkshopSaleBook.Rods: _sales.Rods = receipt; break;
            case WorkshopSaleBook.MeridianFeeder: _workshop.FeederTrial.Spec = receipt; break;
            case WorkshopSaleBook.FirstGel: _workshop.GelOrder = receipt; break;
            case WorkshopSaleBook.CartelDaily: _workshop.CartelDaily = receipt; break;
            default: _sales.Put(Offer.IsPowder, receipt); break;
        }
    }

    internal static bool Unlocked(WorkshopSaleOffer offer, CorpState root) => offer.Book switch
    {
        WorkshopSaleBook.Product => LowerMachinist.StarterUnlocked(root),
        WorkshopSaleBook.Powder or WorkshopSaleBook.Contacts or WorkshopSaleBook.Rods => LowerMachinist.MineralizerUnlocked(root),
        WorkshopSaleBook.MeridianFeeder => root?.Workshop?.FeederTrial is { Passed: true } trial && trial.Valid()
            && MeridianIndustrial.Eligible(root),
        _ => CartelShizuka.Introduced(root) && (offer.Id switch
        {
            "cartel_precipitate" => CartelShizuka.BlueprintDelivered(root),
            "cartel_fine_grit" => LowerMachinist.MineralizerUnlocked(root),
            _ => true
        })
    };
    internal int Amount
    {
        get
        {
            if (Resuming) return Previous.Amount;
            var stock = Selected(0);
            if (Offer.IsPowder && stock != null && Grade(stock).Grade == PowderGrade.Pure) return Offer.PremiumPrice;
            if (Offer == WorkshopSaleOffer.FatigueRod && stock?.Rod is { Core: { Wear: >= 52, Brittleness: <= 3 } })
                return WorkshopSaleOffer.FrequentStartRodQuote;
            return Offer.Price;
        }
    }

    internal WorkshopSaleStock Selected(int slot) =>
        slot >= 0 && slot < Sources.Length && Sources[slot] >= 0 && Sources[slot] < Stock.Count
            ? Stock[Sources[slot]] : null;

    internal static bool Eligible(GameItem item, WorkshopSaleOffer offer) =>
        item != null && item.unitCount > 0 && !StorePlacement.Destroyed(item)
        && (offer.IsPowder ? PowderBox.IsOwnedBox(item) : GeneralHelper.IsItemOwned(item)
            && (offer.IsAlloy ? item.unitCount == 1
                && (offer.ItemId == null ? RodMaterial.IsRod(item.identifier) : item.identifier == offer.ItemId)
                && RodQualifies(offer, RodCondition.Read(item))
                : item.identifier == offer.ItemId))
        && ShopInventory.Contains(item) && item.MayRemove();

    internal static bool RodQualifies(WorkshopSaleOffer offer, RodCondition rod)
    {
        if (rod == null || rod.Scored) return false;
        if (offer.ItemId != null)
        {
            return offer.Id switch
            {
                "part_feed_shaft" => rod.Core.Load >= 50 && (rod.Traits & RodTrait.Fatigue) != 0,
                "meridian_feeder_spec" => rod.Core.Load >= 50 && rod.Surface.Wear >= 40 && rod.Core.Brittleness <= 10,
                "part_feed_sleeve" => rod.Surface.Porosity is >= 5 and <= 9 && rod.Surface.Wear >= 40,
                "part_industrial_coil" => (rod.Traits & RodTrait.LowLoss) != 0 && rod.Core.Brittleness <= 2,
                "part_axial_lead" => (rod.Traits & RodTrait.AxialConductor) != 0,
                "part_return_guide" => rod.Surface.Wear >= 40 && rod.Surface.Brittleness <= 8,
                "part_filter_basket" => rod.Core.Brittleness <= 2,
                _ => false
            };
        }
        if (rod.Material.PowderPoints < 18) return false;
        if (offer == WorkshopSaleOffer.BatchRod) return true;
        if (offer == WorkshopSaleOffer.FatigueRod) return (rod.Traits & RodTrait.Fatigue) != 0;
        if (offer == WorkshopSaleOffer.HardRod) return (rod.Labels(rod.Surface) & RodLabel.HighHardness) != 0;
        return offer == WorkshopSaleOffer.ConductorRod && (rod.Traits & (RodTrait.LowLoss | RodTrait.AxialConductor)) != 0;
    }

    private static PowderClassification Grade(WorkshopSaleStock box) => PowderClassification.FromParts(box.Parts);
    private bool CanSupplyPowder(WorkshopSaleStock box)
    {
        var grade = Grade(box);
        return box.Parts.Sum(points => (long)points) <= PowderColor.Capacity
            && (int?)grade.Branch == Offer.Component && box.Parts[Offer.Component] >= Needed
            && (!Resuming || Previous.Amount == Offer.Price || grade.Grade == PowderGrade.Pure);
    }

    internal int[] RemainingParts()
    {
        var box = Selected(0);
        if (!Offer.IsPowder || box == null) return null;
        var after = (int[])box.Parts.Clone();
        after[Offer.Component] -= Needed;
        return after;
    }

    private bool Current() => _session.IsCurrent && !StoreSession.CompanyProgressPaused
        && SaveSlot.CanWrite("CorpPresence") && Day == StoreStation.GetDayCounter()
        && ReferenceEquals(_workshop, CorpState.Current?.Workshop)
        && ReferenceEquals(_sales, _workshop?.Sales) && _sales != null
        && Unlocked(Offer, CorpState.Current);

    internal SaleStatus Availability()
    {
        if (!Current()) return SaleStatus.Unavailable;
        if (!_validReceipt) return SaleStatus.Frozen;
        if (!ReferenceEquals(Previous, CurrentReceipt)
            || Previous != null && (Previous.Paid != _previousPaid || Previous.Received != _previousReceived))
            return SaleStatus.Changed;
        if (Previous == null) return SaleStatus.Ready;
        if (Previous.InFlight) return SaleStatus.Frozen;
        if (Previous.QuotaDay > Day) return SaleStatus.Unavailable;
        if (!Previous.Complete) return Resuming ? SaleStatus.Ready : SaleStatus.Outstanding;
        if (Offer.Book is WorkshopSaleBook.FirstGel or WorkshopSaleBook.MeridianFeeder) return SaleStatus.TrialSettled;
        return Previous.QuotaDay == Day ? SaleStatus.DailyLimit : SaleStatus.Ready;
    }

    internal SaleStatus Check()
    {
        var status = Availability();
        if (status != SaleStatus.Ready) return status;
        if (SaveSlot.CurrentStore.GetCash() > int.MaxValue - (Amount - (Resuming ? _previousPaid : 0)))
            return SaleStatus.WalletFull;
        foreach (var group in Sources.GroupBy(i => i))
        {
            if (group.Key < 0 || group.Key >= Stock.Count || Stock[group.Key].Removable < group.Count())
                return SaleStatus.Short;
            if (!Stock[group.Key].CanStillSupply(Offer, group.Count())) return SaleStatus.Changed;
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
            var receipt = Resuming ? Previous : new WorkshopSaleReceipt { Day = Day, Offer = Offer.Id, Amount = Amount };
            var groups = Sources.GroupBy(i => i).ToArray();
            PutReceipt(receipt);
            foreach (var group in groups)
            {
                var stock = Stock[group.Key];
                if (!stock.CanStillSupply(Offer, group.Count()) || !Current()) break;
                int quantity = Offer.IsPowder ? Needed : group.Count();
                receipt.InFlight = true;
                try { Collect(stock, quantity); }
                catch (Exception ex) { Warn(ex); }
                // 只认从原库存移出的实数；回调改变其他成分、挪到未知库存或切档时冻结。
                if (!Current() || !ReferenceEquals(receipt, CurrentReceipt)) return SaleStatus.Frozen;
                int collected = Collected(stock);
                if (collected < 0 || collected > quantity) return SaleStatus.Frozen;
                receipt.Received += collected;
                receipt.InFlight = false;
                if (collected < quantity) break;
            }
            if (receipt.Received < Offer.Quantity)
            {
                if (receipt.Received == 0 && !Resuming) PutReceipt(Previous);
                return receipt.Received == 0 ? SaleStatus.Changed : SaleStatus.Partial;
            }
            if (!Current()) return SaleStatus.Unavailable;
            int due = receipt.Amount - receipt.Paid;
            int before = store.GetCash();
            if (before > int.MaxValue - due) return SaleStatus.WalletFull;
            receipt.InFlight = true;
            try { store.ModCash(due, playSound: false); }
            catch (Exception ex) { Warn(ex); }
            if (!Current() || !ReferenceEquals(receipt, CurrentReceipt)) return SaleStatus.Frozen;
            long paid = (long)store.GetCash() - before;
            if (paid < 0 || paid > due) return SaleStatus.Frozen;
            receipt.Paid += (int)paid;
            receipt.InFlight = false;
            if (receipt.Complete) receipt.CompletedDay = Day;
            return receipt.Complete ? SaleStatus.Paid : SaleStatus.Partial;
        }
        finally { CompanyTransaction.Exit(); }
    }

    private void Collect(WorkshopSaleStock stock, int count)
    {
        if (Offer.IsPowder) { PowderBox.WriteComposition(stock.Item, RemainingParts()); return; }
        stock.RemoveUnits(count);
    }

    private int Collected(WorkshopSaleStock stock)
    {
        if (!Offer.IsPowder) return stock.RemovedUnits();
        if (StorePlacement.Destroyed(stock.Item) || stock.Item.parentInventory == null)
            return -1;
        if (!ReferenceEquals(stock.Item.parentInventory, stock.Inventory) || !ShopInventory.Contains(stock.Item)) return -1;
        var after = WorkshopSaleStock.ReadParts(stock.Item);
        for (int i = 0; i < after.Length; i++)
            if (i != Offer.Component && after[i] != stock.Parts[i]) return -1;
        return stock.Parts[Offer.Component] - after[Offer.Component];
    }

    private static void Warn(Exception error)
    {
        try { ModLog.Warn("工坊验货／入账回调异常，按实收核账：" + error.Message); } catch { }
    }
}
