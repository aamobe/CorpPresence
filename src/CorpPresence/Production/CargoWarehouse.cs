using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CorpPresence;

internal static partial class CargoWarehouse
{
    internal const string GoodsKey = "cargo_warehouse";
    internal const string MangoKey = "mango_warehouse";
    internal const string MangoId = "corp_presence_" + MangoKey;
    internal static bool IsWarehouse(GameItem item) => item?.identifier == ItemId || item?.identifier == MangoId;
    internal const string ModuleKey = "cargo_warehouse_module";
    internal const string ItemId = "corp_presence_" + GoodsKey;
    internal const string ModuleId = "corp_presence_" + ModuleKey;
    internal const string OrderId = "@" + GoodsKey;
    internal const string ModuleOrderId = "@" + ModuleKey;
    internal const string SpriteKey = "corp_med_" + GoodsKey;
    internal const string ModuleSpriteKey = "corp_med_" + ModuleKey;
    internal const int WarehousePrice = 15000;
    internal const int ModulePrice = 2000;
    internal const int MaxModules = 4;
    private const string ExtraTabsTag = "corp_presence_cargo_extra_tabs";
    private static readonly ConditionalWeakTable<GameItem, MachineryTabs> Inventories = new();
    private static readonly Company[] Partners = Companies.All
        .Where(c => c is PastelCo or SolanaCo or CarmichaelCo or SpacerCo or MeridianCo or DavreCo).ToArray();

    internal static bool Unlocked() => Partners.All(c => Companies.StateOf(c)?.Coop >= 100);
    internal static string UnlockHint() => L.K("production.cargoWarehouse.unlock",
        "六家公司的合作度均达到 100 后开放", "Requires 100 cooperation with all six companies");

    internal static Goods.Def Definition() => new()
    {
        Name = "大型集装箱仓库", NameEn = "Cargo Warehouse",
        Desc = "通用仓库。初始一页，每页 24×24 格。使用集装箱扩页模块可扩至五页。",
        DescEn = "General storage. Starts with one 24 × 24 tab and expands to five with cargo expansion modules.",
        Company = "jackson_private", Value = WarehousePrice, Art = GoodsKey, Fallback = "hypertech_bay",
        W = 4, H = 4, Shape = "####|####|####|####", Types = new[] { "STORAGE" }, Shell = CreateWarehouse
    };

    internal static Goods.Def MangoDefinition() => new()
    {
        Name = "芒果箱", NameEn = "Mango Box",
        Flavor = "芒果开门", FlavorEn = "Open, mango",
        Desc = "尝试打开魔法芒果", DescEn = "Try opening the magic mango",
        Company = "jackson_private", Value = 0, Art = MangoKey, Fallback = "hypertech_bay",
        W = 1, H = 1, Shape = "#", Types = new[] { "STORAGE" }, Shell = CreateWarehouse
    };

    internal static Goods.Def ModuleDefinition() => new()
    {
        Name = "集装箱扩页模块", NameEn = "Cargo Expansion Module",
        Desc = "拖到大型集装箱仓库上，消耗一件并追加一页。",
        DescEn = "Use on a Cargo Warehouse to consume one module and add one tab.",
        Company = "jackson_private", Value = ModulePrice, Art = ModuleKey, Fallback = "storage_bay_expansion_kit",
        W = 1, H = 1, Shape = "#", Types = new[] { "TOOL" }, Shell = CreateModule
    };

    private static GameItem CreateWarehouse()
    {
        var tabs = new MachineryTabs();
        AppendTab(tabs);
        var window = new PixelWindow(true, null, WindowStyle.Machinery);
        window.Attach(new SortingRow(tabs));
        GameItem bay = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        ContainerHelper.InitContainerItemTagOnly(bay);
        bay.EnableTag("SYSTEM_TAG");
        bay.EnableTag("SYSTEM_TAG_UTILITY");
        Inventories.Add(bay, tabs);
        BindTabNames(bay, tabs);
        return bay;
    }

    private static void AppendTab(MachineryTabs tabs)
    {
        int page = tabs.count;
        var grid = new GameGridInventory(24, 24).SetMachineryStyle();
        grid.mayInventoryAddItemFunc = (item, inventory) => Accepts(item, inventory.GetParentItem(), page);
        tabs.AddTab(LocHelper.GetLocalizedMechanic("mech_storage_bay_tab", tabs.count + 1),
            MachineHelper.WrapInIntake(grid));
    }

    private static GameItem CreateModule()
    {
        GameItem item = ItemDirectory.CreateEmptyItem();
        AudioHelper.InitSolidMetal(item);
        item.maySelectSlotItemFunc = (source, _, _) => GeneralHelper.IsItemOwned(source);
        item.canSelectSlotItemFunc = item.maySelectSlotItemFunc;
        item.mayThisTargetItemFunc = CanExpand;
        item.canThisTargetItemFunc = CanExpand;
        item.onThisTargetItemFunc = Expand;
        return item;
    }

    private static bool CanExpand(GameItem source, GameItem target) =>
        source?.identifier == ModuleId && source.unitCount > 0 && IsWarehouse(target)
        && GeneralHelper.IsItemOwned(source, target)
        && Inventories.TryGetValue(target, out var tabs) && tabs.count <= MaxModules;

    private static void Expand(GameItem source, GameItem target)
    {
        if (!CanExpand(source, target) || !Inventories.TryGetValue(target, out var tabs)) return;
        AppendTab(tabs);
        target.ModifyTag(ExtraTabsTag, tag => tag.Enable().SetInt(tabs.count - 1));
        if (source.unitCount == 1) source.Destroy();
        else source.SetUnitCount(source.unitCount - 1);
        target.contentWindow.Validate();
        EmporiumAudioPlayer.Instance?.ratchet?.Play();
    }

    internal static bool RestoreTabs(GameItem bay)
    {
        if (!IsWarehouse(bay) || !Inventories.TryGetValue(bay, out var tabs)) return false;
        int count = 1 + Math.Max(0, Math.Min(MaxModules, bay.GetTagReadonly(ExtraTabsTag).GetInt()));
        while (tabs.count < count) AppendTab(tabs);
        RestoreFilters(bay, tabs.count);
        BindTabNames(bay, tabs);
        bay.contentWindow.Validate();
        return true;
    }

    private static void BindTabNames(GameItem bay, MachineryTabs tabs)
    {
        StorageExpansionHelper.InitTabNames(bay);
        tabs.onRenameRequest = page => UI.CargoTabRename.Open(bay, page);
    }
}
