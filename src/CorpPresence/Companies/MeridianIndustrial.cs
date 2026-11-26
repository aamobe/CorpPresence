using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static class MeridianIndustrial
{
    internal const string OrderKind = MeridianCommissionReceipt.OrderKind;
    internal const string AlloyId = "corp_presence_" + IndustrialGoods.Alloy;

    internal static bool Eligible(CorpState root) => root != null && LowerMachinist.MineralizerUnlocked(root) &&
        (root.Companies.TryGetValue("meridian", out var company) && company.ForgeOwned
         || root.Characters.TryGetValue("rosen", out var rosen) && rosen.SideDone.Contains(5));

    internal static string RecipeError(CorpState root) => Eligible(root) ? null :
        L.K("workshop.meridian.locked", "需买下阿岑的第二套设备，并听罗森提出锻造，或已取得他的锻具。",
            "Buy Cen’s second set of machines and hear Rosen’s forging offer or own his forge kit.");

    internal static string Changed => L.K("workshop.meridian.changed", "库存或存档已变化，请重新验料。", "The stock or save has changed. Inspect the materials again.");
    internal static string Frozen => L.K("workshop.meridian.frozen", "委托凭单待核，请先核对实收记录。", "Commission on hold. Check the recorded materials and fee.");
}

internal sealed class MeridianCommission
{
    private sealed record Supply(GameItem Item, GameInventory Inventory, int Before, int Take, bool Ingot);
    private readonly List<Supply> _stock = new();
    private readonly StoreSession _session = StoreSession.Capture();
    private readonly CorpState _root = CorpState.Current;
    private readonly CompanyState _state;
    private readonly WorkshopState _workshop;
    private readonly MeridianCommissionReceipt _previous;
    private readonly int _ingots, _alloy, _paid;
    private readonly Company _company;
    internal readonly int Quantity, Day;
    internal int FeeDue => Quantity * MeridianCommissionReceipt.Fee - _paid;
    internal int NeedIngots => Quantity - _ingots;
    internal int NeedAlloy => Quantity - _alloy;

    internal MeridianCommission(Company company, int quantity)
    {
        _company = company;
        _state = company is MeridianCo ? Companies.StateOf(company) : null;
        _workshop = _root?.Workshop;
        _previous = _workshop?.Commission;
        _ingots = _previous?.Ingots ?? 0;
        _alloy = _previous?.Alloy ?? 0;
        _paid = _previous?.Paid ?? 0;
        Quantity = quantity;
        Day = StoreStation.GetDayCounter();
        if (Access() != null) return;
        int ingots = NeedIngots, alloy = NeedAlloy;
        var stock = EmporiumEntry.Instance?.GetAllOwnedItems(includeAfterhour: false);
        if (stock == null) return;
        foreach (var item in stock.Distinct())
        {
            if (ingots == 0 && alloy == 0) break;
            bool ingot = Forge.IsGoodIngot(item);
            if (!ingot && item?.identifier != MeridianIndustrial.AlloyId) continue;
            int wanted = ingot ? ingots : alloy;
            if (wanted <= 0 || !Available(item)) continue;
            int take = Math.Min(wanted, Math.Min(item.unitCount, item.MaxNumRemove()));
            if (take <= 0) continue;
            _stock.Add(new Supply(item, item.parentInventory, item.unitCount, take, ingot));
            if (ingot) ingots -= take; else alloy -= take;
        }
    }

    private static bool Available(GameItem item) => item != null && item.unitCount > 0
        && GeneralHelper.IsItemOwned(item) && !StorePlacement.Destroyed(item) && ShopInventory.Contains(item) && item.MayRemove();

    private static bool Matches(Supply s) => Available(s.Item) && ReferenceEquals(s.Item.parentInventory, s.Inventory)
        && s.Item.unitCount == s.Before && s.Item.MaxNumRemove() >= s.Take
        && (s.Ingot ? Forge.IsGoodIngot(s.Item) : s.Item.identifier == MeridianIndustrial.AlloyId);

    private bool Current() => _session.IsCurrent && !StoreSession.CompanyProgressPaused && SaveSlot.CanWrite("CorpPresence")
        && (!Settings.MaliceMode || SaveSlot.CanWrite("CorpPresence.Malice")) && Day == StoreStation.GetDayCounter()
        && _state?.Orders != null && ReferenceEquals(_root.Workshop, _workshop) && _workshop != null
        && _root.Companies.TryGetValue("meridian", out var current) && ReferenceEquals(current, _state);

    private string Access()
    {
        if (!Current() || Day < 0 || Day > int.MaxValue - 2 || Quantity is < 1 or > MeridianCommissionReceipt.Maximum)
            return MeridianIndustrial.Changed;
        if (_previous != null && (!_previous.Valid() || _previous.InFlight)) return MeridianIndustrial.Frozen;
        if (!ReferenceEquals(_previous, _workshop.Commission) || _previous != null
            && (_previous.Quantity != Quantity || _previous.Ingots != _ingots || _previous.Alloy != _alloy || _previous.Paid != _paid))
            return MeridianIndustrial.Changed;
        return MeridianIndustrial.RecipeError(_root) ?? Services.OrderBlocked(_company)
            ?? (_state.LastOrderDay >= Day ? L.K("companies.services.orders.placeOrder.alreadyOrderedToday", "今天已经下过单了", "Already ordered today") : null);
    }

    internal string Check()
    {
        var reason = Access();
        if (reason != null) return reason;
        if (_stock.Where(s => s.Ingot).Sum(s => s.Take) != NeedIngots || _stock.Where(s => !s.Ingot).Sum(s => s.Take) != NeedAlloy)
            return L.K("workshop.meridian.short", "店里可移出的高纯金属锭或合金粉不足。", "Not enough removable high-purity ingots or alloy powder in the shop.");
        if (!_stock.All(Matches)) return MeridianIndustrial.Changed;
        return Services.CanAfford(Services.CashDue(_company, FeeDue)) ? null : L.K("companies.services.pay.notEnoughCash", "现金不够", "Not enough cash");
    }

    internal string Submit()
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return MeridianIndustrial.Changed;
        try
        {
            string error = Check();
            if (error != null) return error;
            var receipt = _previous ?? new MeridianCommissionReceipt { Quantity = Quantity };
            var order = new Order { Day = Day + 2, Kind = MeridianIndustrial.OrderKind,
                Items = Enumerable.Repeat(MeridianFrameDelivery.FrameId, Quantity).ToList() };
            _workshop.Commission = receipt;
            foreach (var supply in _stock)
            {
                if (!Current() || !Matches(supply)) return Retry(receipt);
                receipt.InFlight = true;
                int received = Take(supply);
                if (!Current() || received < 0 || received > supply.Take) return MeridianIndustrial.Frozen;
                if (supply.Ingot) receipt.Ingots += received; else receipt.Alloy += received;
                receipt.InFlight = false;
                if (received != supply.Take) return Retry(receipt);
            }
            if (!Current() || _state.LastOrderDay >= Day || Services.OrderBlocked(_company) != null) return Retry(receipt);
            if (FeeDue > 0)
            {
                var store = SaveSlot.CurrentStore;
                int before = store.GetCash(), cashDue = Services.CashDue(_company, FeeDue);
                int vouchersBefore = int.MaxValue - Services.CashDue(_company, int.MaxValue);
                receipt.InFlight = true;
                try { error = Services.Pay(_company, FeeDue, "frame_commission"); }
                catch (Exception ex) { ModLog.Warn("子午线委托付款回调：" + ex.Message); error = MeridianIndustrial.Changed; }
                if (!Current()) return MeridianIndustrial.Frozen;
                long debit = (long)before - store.GetCash();
                if (debit < 0 || debit > cashDue) return MeridianIndustrial.Frozen;
                if (error == null && debit == cashDue) receipt.Paid += FeeDue;
                else
                {
                    int vouchersAfter = int.MaxValue - Services.CashDue(_company, int.MaxValue);
                    long voucherDebit = (long)vouchersBefore - vouchersAfter;
                    // The extension can also award vouchers. A changed voucher balance on
                    // an interrupted payment is conclusive only if the entire fee is visible.
                    if (voucherDebit != 0 && debit + voucherDebit != FeeDue) return MeridianIndustrial.Frozen;
                    receipt.Paid += (int)(debit + voucherDebit);
                }
                receipt.InFlight = false;
                if (receipt.Paid != Quantity * MeridianCommissionReceipt.Fee) return error ?? MeridianIndustrial.Changed;
            }
            _state.Orders.Add(order);
            _state.LastOrderDay = Day;
            _workshop.Commission = null;
            return null;
        }
        finally { CompanyTransaction.Exit(); }
    }

    private string Retry(MeridianCommissionReceipt receipt)
    {
        if (_previous == null && receipt.Ingots == 0 && receipt.Alloy == 0 && Current()) _workshop.Commission = null;
        return MeridianIndustrial.Changed;
    }

    private static int Take(Supply s)
    {
        try
        {
            if (s.Take < s.Before) s.Item.SetUnitCount(s.Before - s.Take);
            else
            {
                try { s.Inventory.Expel(s.Item); }
                finally { if (s.Item.parentInventory == null && !StorePlacement.Destroyed(s.Item)) s.Item.Destroy(); }
            }
        }
        catch (Exception ex) { ModLog.Warn("子午线委托收料回调：" + ex.Message); }
        if (StorePlacement.Destroyed(s.Item) || s.Item.parentInventory == null) return s.Before;
        return ReferenceEquals(s.Item.parentInventory, s.Inventory) && ShopInventory.Contains(s.Item) ? s.Before - s.Item.unitCount : -1;
    }
}
