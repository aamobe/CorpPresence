using System;

namespace CorpPresence;

internal static class CenSupplies
{
    internal const string BatteryOrder = "@cen_charge24";
    private const string Scrap = "scrap_metal";
    private const string Glass = "empty_beer_bottle";
    internal const string Pump = "cen_pump_pack";
    internal const string Classifier = "@" + PowderClassifier.Key;
    internal const string LubricantOrder = "@mechanical_lubricant";
    internal const string FeederOrder = "@cen_feeder";
    internal const string ScrewdriverOrder = "screwdriver";
    internal static readonly CatalogEntry[] All =
    {
        Supply("glass", 30, "workshop.cen.glass", "玻璃筐 · 3 个空啤酒瓶", "Glass basket · 3 empty beer bottles", Glass, Glass, Glass),
        Supply("metal", 45, "workshop.cen.metal", "机修筐 · 3 件废金属", "Mechanic’s basket · 3 scrap metal", Scrap, Scrap, Scrap),
        Supply("salvage", 55, "workshop.cen.salvage", "拆旧筐 · 废金属、电子残料、旧炉衬各 1", "Salvage basket · 1 scrap, 1 electronic salvage, 1 spent lining", Scrap, "@" + WorkshopMaterials.Electronics, "@" + WorkshopMaterials.Lining),
        Supply("@" + PowderBox.Key, 20, "workshop.cen.emptyBox", "空接粉盒", "Empty powder box"),
        Supply(BatteryOrder, 16, "workshop.cen.battery", "电池 · 24 电", "Battery · 24 charge"),
        Supply(Pump, 65, "workshop.cen.pump", "旧泵拆料包 · 2 件电子残料、1 件废金属", "Pump pack · 2 electronic salvage, 1 scrap metal", "@" + WorkshopMaterials.Electronics, "@" + WorkshopMaterials.Electronics, Scrap),
        Supply(Classifier, 220, "workshop.cen.classifier", "分级机（含通量筛）", "Powder classifier with throughput screen"),
        Supply(LubricantOrder, 8, "workshop.industrial.lubricant.name", "机修润滑料", "Mechanical Lubricant"),
        Supply(FeederOrder, 220, "workshop.industrial.cenFeeder.name", "阿岑的旧给料机", "Cen's Old Feeder"),
        Supply(ScrewdriverOrder, 25, "workshop.industrial.screwdriver.name", "螺丝刀", "Screwdriver")
    };

    private static CatalogEntry Supply(string id, int price, string key, string zh, string en, params string[] contents) =>
        new() { Id = id, MinTier = 0, FixedBaseValue = price, FixedListValue = price, IgnoreOrderRate = true, NoQuantityQuota = true,
            LabelKey = key, Label = zh, LabelEn = en, Contents = contents.Length == 0 ? null : contents,
            Factory = id == BatteryOrder ? _ => MakeBattery() : null };

    internal static CatalogEntry Kit(string choice) =>
        choice is "glass" or "metal" or "salvage" ? Array.Find(All, e => e.Id == choice) : null;

    internal static GameItem MakeBattery()
    {
        GameItem item = ItemRegistry.Create("energy_credit");
        if (item == null) return null;
        try
        {
            PowerHelper.SetPowerSourceAt(item, 24);
            if (item.GetTagReadonly("power_source_item_energy")?.GetInt() == 24 && PowerHelper.CanDrawPowerSource(item, 24))
                return item;
        }
        catch { item.Destroy(); throw; }
        item.Destroy();
        return null;
    }
}
