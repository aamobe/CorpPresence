namespace CorpPresence;

// Internal rules selected by stable item identity, never supplied by the caller's catalog entry.
internal sealed class EquipmentOrderRule
{
    private enum Kind { WorkBay, Warehouse, Module, Controller }
    private readonly Kind _kind;
    internal string CounterKey { get; }
    internal bool UsesCounter => _kind != Kind.Controller;
    internal bool RejectDuplicateLines => _kind != Kind.WorkBay;
    internal int FixedPrice { get; }
    private readonly int _limit;

    private EquipmentOrderRule(Kind kind, string id, int price, int limit)
    {
        _kind = kind;
        CounterKey = kind == Kind.WorkBay ? ProductionWorkBay.PurchaseKey : id + "@lifetime";
        FixedPrice = price;
        _limit = limit;
    }

    // Constants avoid initializing CargoWarehouse's company-dependent state during catalog construction.
    private static readonly EquipmentOrderRule WorkBay = new(Kind.WorkBay, ProductionWorkBay.OrderId, 0, 1);
    private static readonly EquipmentOrderRule Warehouse = new(Kind.Warehouse, CargoWarehouse.OrderId, CargoWarehouse.WarehousePrice, 1);
    private static readonly EquipmentOrderRule Module = new(Kind.Module, CargoWarehouse.ModuleOrderId, CargoWarehouse.ModulePrice, CargoWarehouse.MaxModules);
    private static readonly EquipmentOrderRule Controller = new(Kind.Controller, CargoWarehouse.ControllerOrderId, CargoWarehouse.ControllerPrice, 1);

    internal static EquipmentOrderRule For(string id) => id switch
    {
        ProductionWorkBay.OrderId => WorkBay,
        CargoWarehouse.OrderId => Warehouse,
        CargoWarehouse.ModuleOrderId => Module,
        CargoWarehouse.ControllerOrderId => Controller,
        _ => null
    };

    internal string AccessError(Company company) => _kind != Kind.WorkBay &&
        (company is not JacksonContact || !CargoWarehouse.Unlocked()) ? CargoWarehouse.UnlockHint() : null;

    internal int Remaining(CompanyState state, bool refreshControllerStock)
    {
        if (state?.LimitedBought == null) return 0;
        if (_kind == Kind.WorkBay)
            return state.Orders == null || ProductionWorkBay.AlreadyPurchased(state) ? 0 : 1;
        if (!CargoWarehouse.Unlocked()) return 0;
        if (_kind == Kind.Controller)
            return CargoWarehouse.ControllerAvailable(state, refreshControllerStock) ? 1 : 0;
        state.LimitedBought.TryGetValue(CounterKey, out int used);
        return used < 0 || used >= _limit ? 0 : _limit - used;
    }

    internal string RemainingText(int remaining)
    {
        if (_kind == Kind.WorkBay)
            return remaining == 0
                ? L.K("production.workBay.order.purchased", "本存档已购买", "Already purchased in this save")
                : L.K("production.workBay.order.once", "每个存档限购 1 件", "Limit 1 per save");
        return L.F("production.cargoWarehouse.order.remaining", "本存档还可购买 {0} 件", "{0} remaining in this save", remaining);
    }

    internal void RecordPurchase(CompanyState state, int count)
    {
        // The queued order reserves a controller; it does not consume a lifetime counter.
        if (!UsesCounter) return;
        if (_kind == Kind.WorkBay) state.LimitedBought[CounterKey] = 1;
        else state.LimitedBought[CounterKey] = state.LimitedBought.TryGetValue(CounterKey, out int used) ? used + count : count;
    }
}
