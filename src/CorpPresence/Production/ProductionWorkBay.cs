using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CorpPresence;

internal static class ProductionWorkBay
{
    internal const string GoodsKey = "production_work_bay";
    internal const string ItemId = "corp_presence_" + GoodsKey;
    internal const string OrderId = "@" + GoodsKey;
    internal const string SpriteKey = "corp_med_" + GoodsKey;
    internal const string PurchaseKey = OrderId + "@lifetime";
    internal const int Price = 800;
    internal const int PageCount = 5;
    internal const int Width = 11;
    internal const int Height = 14;
    internal const int MaxUpgradeLevel = 4;
    private const int TobaccoPage = 0;
    private const int FoodPage = 1;
    private const int ForgePage = 2;
    private const int SilverBloodPage = 3;
    private const int PerfumePage = 4;
    private const string LevelTag = "CORP_WORK_BAY_LEVEL_INT";
    private static readonly ConditionalWeakTable<GameItem, GameGridInventory[]> Inventories = new();

    // SaveManager restores child inventories by position. Append only.
    private static readonly HashSet<string>[] AllowedIds =
    {
        CompanyIds("tobacco_seed", "tobacco_leaf", "loose_cigarette", "rolling_machine", "packaging_machine", "hemingway_stamp"),
        CompanyIds("meat_grinder", "canning_machine", "seafood_sealer", "fine_mince", "coarse_mince", "fruit_paste",
            "fresh_meat_can", "fish_mince", "crab_stick", "fish_can", "leviathan", RatWheelGenerator.GoodsKey),
        CompanyIds("forge_furnace", "forge_anvil", "forged_billet", "custom_dagger", "custom_saber", "custom_machete"),
        CompanyIds("silverblood", "blood_drawer", "silver_blood", "isomer_vial", "isomer_reactor",
            "carapace_small", "carapace_medium", "carapace_full", "rejuvenation", "implant_compat", "isomer_gel"),
        CompanyIds("oilflower_seed", "oilflower", Perfume.ExtractorKey, Perfume.BloodKey, Perfume.MixerKey, Perfume.CollectorKey,
            Perfume.OilKey, Perfume.LegacyOilBottleKey, Perfume.RedKey, Perfume.BlueKey)
    };

    internal static Goods.Def Definition() => new()
    {
        Name = "专用工作仓",
        NameEn = "Production Work Bay",
        Desc = "五个标签页分别收纳烟草、食品与水产、锻造、银血和香水产线的设备、原料及成品。拖到仓体可自动归类。",
        DescEn = "Five tabs for equipment, ingredients, and products from the tobacco, food and seafood, forging, silverblood, and perfume lines. Drop items onto the bay to sort them.",
        Value = Price,
        Company = "jackson_private",
        Art = GoodsKey,
        W = 4,
        H = 4,
        Shape = "####|####|####|####",
        Types = new[] { "STORAGE" },
        Shell = CreateShell
    };

    private static HashSet<string> CompanyIds(params string[] keys)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (string key in keys) ids.Add("corp_presence_" + key);
        return ids;
    }

    internal static bool Allows(int page, string itemId) =>
        page >= 0 && page < AllowedIds.Length && itemId != null &&
        (itemId == "powerblock" || AllowedIds[page].Contains(itemId) || (page switch
        {
            TobaccoPage => itemId is "hydroponic" or "packet_red_cigarette",
            FoodPage => itemId is "animal_cage" or "rat" or "livestock_feed_dispenser" or "auto_butcher"
                or "ras" or "ras_water_tank" or "tilapia" or "tilapia_roe" or "tilapia_fry"
                or "industrial_fish_feed" or "gutterflow_pellet" or "printer_plastic" or "processed_meat" or "galaxy_blend",
            ForgePage => itemId == "metal_ingot",
            SilverBloodPage => itemId is "ras" or "ras_water_tank" or "livestock_feed_dispenser"
                or "industrial_fish_feed" or "gutterflow_pellet"
                or "heating_plate" or "water_purifier" or "evaporator",
            PerfumePage => itemId is "hydroponic" or "blender" or "heating_plate" or "blood_bag" or "blue_blood_bag"
                or "neuroactive_perfume" or "pheromone_perfume",
            _ => false
        }));

    internal static bool AllowsItem(int page, GameItem item)
    {
        if (item == null || page < 0 || page >= PageCount) return false;
        if (Allows(page, item.identifier)) return true;
        if (item.IsTag("power_source_item") || item.IsTag("MODULE_TYPE_UNIVERSAL")) return true;
        if ((page is TobaccoPage or PerfumePage) && item.IsTag("MODULE_TYPE_HYDROPONIC")) return true;
        if ((page is FoodPage or SilverBloodPage) && item.IsTag("WATER_FILTER")) return true;
        if (page == FoodPage && Canning.MinceOf(item) != null) return true;
        if (!item.IsTag("LIQUID_CONTAINER_TAG") || item.IsTag("WINE_CONTAINER_TAG")) return false;
        if (IsRecipeWater(item)) return true;
        return page switch
        {
            SilverBloodPage => HasPart(item, SilverBlood.RawId) || HasPart(item, SilverBlood.LiquidId)
                || HasPart(item, "reducing_agent") || HasPart(item, "lipobond_agent"),
            PerfumePage => HasPart(item, Perfume.OilLiquid) || HasPart(item, Perfume.MashLiquid) || HasPart(item, "alcohol"),
            _ => false
        };
    }

    private static bool HasPart(GameItem item, string liquidId) =>
        item.GetTagReadonly(Liquid.GetCurrentPartFromId(liquidId)).GetInt() > 0;

    private static bool IsRecipeWater(GameItem item) =>
        item.IsTag("LIQUID_CONTAINER_TAG") && !item.IsTag("WINE_CONTAINER_TAG") && item.IsGameItemType("WATER")
        && HasPart(item, "water") && !HasPart(item, "alcohol")
        && !HasPart(item, SilverBlood.RawId) && !HasPart(item, SilverBlood.LiquidId)
        && !HasPart(item, Perfume.OilLiquid) && !HasPart(item, Perfume.MashLiquid)
        && !HasPart(item, "reducing_agent") && !HasPart(item, "lipobond_agent");

    // Only this bay's window routes body drops. Keep the native row's inventory graph and tab layout.
    private sealed class ProductionRow(GameGridInventory[] grids, MachineryTabs tabs)
        : MachineryPanelRow(MachineHelper.Panel(null, tabs, "storage")), GraphNodeStorage
    {
        private GameGridInventory DropGrid(GameItem item)
        {
            if (item == null || Array.IndexOf(grids, item.parentInventory) >= 0) return null;
            // Dropping an ingot on the shell is the upgrade action. Use the Forging tab to store it before max level.
            if (item.identifier == "metal_ingot" && CanUpgrade(grids[ForgePage].GetParentItem())) return null;
            if (IsRecipeWater(item)) return grids[FoodPage];
            for (int page = 0; page < grids.Length; page++)
                if (AllowsItem(page, item)) return grids[page];
            return null;
        }

        bool GraphNodeStorage.MayHaveValidInventorySlot(GameItem item) =>
            DropGrid(item)?.MayHaveValidInventorySlot(item) == true;

        SlotMarker GraphNodeStorage.TryFindOneValidInventorySlot(GameItem item)
        {
            var grid = DropGrid(item);
            if (grid == null || !grid.MayHaveValidInventorySlot(item)) return null;
            SlotMarker slot = grid.TryFindOneValidInventorySlot(item, keepOrientation: false);
            return slot?.inventory == grid && slot.numTransfer > 0 ? slot : null;
        }
    }

    private static GameItem CreateShell()
    {
        var tabs = new MachineryTabs();
        var grids = new GameGridInventory[PageCount];
        for (int page = 0; page < PageCount; page++)
        {
            int inventoryIndex = page;
            var grid = new GameGridInventory(Width, Height).SetMachineryStyle();
            grids[page] = grid;
            grid.mayInventoryAddItemFunc = (item, inventory) =>
            {
                if (!AllowsItem(inventoryIndex, item) || !GeneralHelper.IsItemOwned(item)) return false;
                GameItem owner = inventory.GetParentItem();
                return owner?.identifier == ItemId && GeneralHelper.IsItemOwned(owner);
            };
            tabs.AddTab(Caption(page), MachineHelper.WrapInIntake(grid));
        }

        var window = new PixelWindow(true, null, WindowStyle.Machinery);
        window.Attach(new ProductionRow(grids, tabs));
        GameItem bay = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        ContainerHelper.InitContainerItemTagOnly(bay);
        bay.EnableTag("SYSTEM_TAG");
        bay.EnableTag("SYSTEM_TAG_UTILITY");
        Inventories.Add(bay, grids);
        return bay;
    }

    internal static bool AlreadyPurchased(CompanyState state)
    {
        if (state.LimitedBought.TryGetValue(PurchaseKey, out int used) && used != 0) return true;
        foreach (var order in state.Orders)
            if (order?.Items?.Contains(OrderId) == true) return true;
        // Earlier local builds recorded the order under the normal period key.
        foreach (var entry in state.LimitedBought)
            if (entry.Value != 0 && entry.Key.StartsWith(OrderId + "@", StringComparison.Ordinal)) return true;
        return false;
    }

    private static int Level(GameItem bay) =>
        Math.Max(0, Math.Min(MaxUpgradeLevel, bay.GetTagReadonly(LevelTag).GetInt()));

    internal static bool CanUpgrade(GameItem bay) =>
        bay?.identifier == ItemId && Inventories.TryGetValue(bay, out _) && Level(bay) < MaxUpgradeLevel;

    internal static string UpgradeStatus(GameItem bay)
    {
        int level = Level(bay);
        string capacity = L.F("production.workBay.upgrade.status", "容量 {0}/{1} 级，每页 {2}×{3} 格。",
            "Capacity {0}/{1}: {2}×{3} cells per tab.", level, MaxUpgradeLevel, Width + level, Height + level);
        return level < MaxUpgradeLevel
            ? capacity + "\n" + L.K("production.workBay.upgrade.hint", "拖 1 个金属锭到工作仓上，五页各加 1 列、1 行。",
                "Drop one metal ingot onto the bay to add one column and one row to every tab.")
            : capacity;
    }

    // Native DecodeNodes calls this entry after restoring item tags and before attaching stored items.
    internal static bool BeforeRestoreNativeTabs(GameItem __0, bool __runOriginal)
    {
        if (!__runOriginal) return true;
        if (CargoWarehouse.IsWarehouse(__0) && CargoWarehouse.RestoreTabs(__0)) return false;
        if (__0?.identifier != ItemId || !Inventories.TryGetValue(__0, out var grids)) return true;
        int level = Level(__0);
        if (level > 0) Resize(__0, grids, level);
        Goods.Relabel(__0, GoodsKey);
        // Native storage initialization replaces the factory's line captions with generic numbered tabs.
        return false;
    }

    internal static void Upgrade(GameItem ingot, GameItem bay)
    {
        if (ingot?.identifier != "metal_ingot" || ingot.unitCount < 1 ||
            !CanUpgrade(bay) || !GeneralHelper.IsItemOwned(ingot, bay)) return;
        int level = Level(bay);
        int count = ingot.unitCount;
        if (!Inventories.TryGetValue(bay, out var grids)) return;
        try
        {
            Resize(bay, grids, level + 1);
            SetLevel(bay, level + 1);
            if (count == 1) ingot.Destroy();
            else ingot.SetUnitCount(count - 1);
        }
        catch (Exception error)
        {
            if (ingot.unitCount == count && !StorePlacement.Destroyed(ingot))
            {
                try
                {
                    SetLevel(bay, level);
                    Resize(bay, grids, level);
                }
                catch (Exception rollback)
                {
                    ModLog.Warn("专用工作仓扩容回退失败：" + rollback.Message);
                }
            }
            ModLog.Warn("专用工作仓扩容失败：" + error.Message);
            return;
        }
        Goods.Relabel(bay, GoodsKey);
        try { EmporiumAudioPlayer.Instance?.ratchet?.Play(); } catch { }
        StoreUIManager.Instance?.Notify(UpgradeStatus(bay));
    }

    private static void SetLevel(GameItem bay, int level) =>
        bay.ModifyTag(LevelTag, state => state.Enable().SetInt(level));

    private static void Resize(GameItem bay, GameGridInventory[] grids, int level)
    {
        foreach (GameGridInventory grid in grids)
            grid.SetShape(Width + level, Height + level);
        bay.contentWindow?.Validate();
    }

    private static string Caption(int page) => page switch
    {
        0 => L.K("production.workBay.tab.tobacco", "烟草", "Tobacco"),
        FoodPage => L.K("production.workBay.tab.food", "食品与水产", "Food & Fish"),
        2 => L.K("production.workBay.tab.forging", "锻造", "Forging"),
        3 => L.K("production.workBay.tab.silverblood", "银血", "Silverblood"),
        4 => L.K("production.workBay.tab.perfume", "香水", "Perfume"),
        _ => throw new ArgumentOutOfRangeException(nameof(page))
    };
}
