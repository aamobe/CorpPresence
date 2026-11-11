namespace CorpPresence;

// Fixed-price dependencies only. Inventory construction belongs to game checks.
static class Tobacco
{
    public const int StampPrice = 400;
}

static class ProductionWorkBay
{
    public const string OrderId = "@production_work_bay";
    public const int Price = 800;
}

static class CargoWarehouse
{
    public static int Price(string id) => id switch
    {
        "@cargo_warehouse" => 15000,
        "@cargo_warehouse_module" => 2000,
        "@cargo_sort_controller" => 3000,
        _ => 0
    };
}
