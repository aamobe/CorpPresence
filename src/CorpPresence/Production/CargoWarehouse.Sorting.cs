using System;
using System.Linq;
using System.Runtime.CompilerServices;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static partial class CargoWarehouse
{
    internal const string ControllerKey = "cargo_sort_controller";
    internal const string ControllerOrderId = "@" + ControllerKey;
    internal const int ControllerPrice = 3000;
    internal const string ControllerId = "corp_presence_" + ControllerKey;
    internal const string ControllerSpriteKey = "corp_med_" + ControllerKey;
    internal const string TobaccoAndAlcohol = "corp_presence_tobacco_and_alcohol";
    private static readonly string[] FilterKeys = Enumerable.Range(0, MaxModules + 1)
        .Select(index => "corp_presence_cargo_filter_" + index).ToArray();
    private static readonly ConditionalWeakTable<GameItem, string[][]> Filters = new();
    private static StoreSession _controllerStockSession;
    private static float _controllerStockExpires;
    private static bool _controllerFound;

    internal static bool ControllerAvailable(CompanyState state, bool refresh)
    {
        if (state?.Orders == null || state.Orders.Any(order => order?.Items?.Contains(ControllerOrderId) == true))
            return false;
        var shop = EmporiumEntry.Instance;
        StoreSession session = StoreSession.Capture();
        if (shop == null || !session.IsCurrent) return false;
        float now = Time.unscaledTime;
        if (refresh || !_controllerStockSession.IsCurrent || now >= _controllerStockExpires)
        {
            var drag = ItemMouseDragHandler.current;
            _controllerFound = (drag != null && IsLiveController(drag.GetCurrentItem()))
                || shop.GetAllItems().Any(IsLiveController)
                || (shop.backInvinvElementCounter != null &&
                    GraphUtils.FindAllChildrenType<GameItem>(shop.backInvinvElementCounter).Any(IsLiveController));
            _controllerStockSession = session;
            _controllerStockExpires = now + 0.75f;
        }
        return session.IsCurrent && !_controllerFound;
    }

    internal static void ResetControllerStock() => _controllerStockSession = default;

    private static bool IsLiveController(GameItem item) =>
        item?.identifier == ControllerId && item.unitCount > 0 && !StorePlacement.Destroyed(item);

    internal static Goods.Def ControllerDefinition() => new()
    {
        Name = "仓库分类控制器", NameEn = "Cargo Sorting Controller",
        Desc = "先选中仓库页，再将控制器拖到仓体。勾选允许存入的物品类别。全部不勾选表示不限。",
        DescEn = "Select a cargo warehouse tab, then use the controller on the warehouse. Check the categories to accept. Leave all unchecked for unrestricted storage.",
        Company = "jackson_private", Value = ControllerPrice, Art = ControllerKey, Fallback = "portable_water_purifier",
        W = 1, H = 2, Shape = "#|#", Types = new[] { "TOOL" }, Shell = CreateController
    };

    private static string[][] Rules(GameItem bay) =>
        Filters.GetValue(bay, _ => new string[MaxModules + 1][]);

    private static string[] ParseFilter(string input) => (input ?? "")
        .Split(new[] { ',', '，', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Trim()).Where(part => part.Length > 0).Distinct(StringComparer.Ordinal).ToArray();

    private static void RestoreFilters(GameItem bay, int pageCount)
    {
        string[][] rules = Rules(bay);
        for (int page = 0; page < pageCount; page++)
            rules[page] = ParseFilter(bay.GetTagReadonly(FilterKeys[page]).GetString());
    }

    private static bool Matches(GameItem item, string[] tags)
    {
        foreach (string tag in tags)
        {
            if (tag == TobaccoAndAlcohol)
            {
                if (item.IsTag("CIGARETTE_TAG") || item.IsGameItemType("ALCOHOL") || item.IsTag("ALCOHOL_TAG")) return true;
            }
            else if (item.IsTag(tag) || item.IsGameItemType(tag)) return true;
        }
        return false;
    }

    private static bool Accepts(GameItem item, GameItem bay, int page)
    {
        if (item == null || item.identifier == ControllerId || !IsWarehouse(bay) || item == bay || !GeneralHelper.IsItemOwned(item, bay)
            || item.IsTag("BACKPACK_TAG")
            || ((item.IsTag("CONTAINER_TAG") || item.IsGameItemType("STORAGE"))
                && !ContainerHelper.IsNestingAllowed(item, bay))) return false;
        string[] tags = Rules(bay)[page];
        return tags == null || tags.Length == 0 || Matches(item, tags);
    }

    private sealed class SortingRow(MachineryTabs tabs)
        : MachineryPanelRow(MachineHelper.Panel(null, tabs, "storage")), GraphNodeStorage
    {
        private GameGridInventory DropGrid(GameItem item)
        {
            GameGridInventory fallback = null;
            for (int page = 0; page < tabs.count; page++)
            {
                if (tabs.GetTabInventory(page) is not GameGridInventory grid) continue;
                if (grid == item?.parentInventory) return null;
            }
            for (int page = 0; page < tabs.count; page++)
            {
                if (tabs.GetTabInventory(page) is not GameGridInventory grid) continue;
                GameItem owner = grid.GetParentItem();
                if (!Accepts(item, owner, page)) continue;
                if (Rules(owner)[page]?.Length > 0) return grid;
                fallback ??= grid;
            }
            return fallback;
        }

        bool GraphNodeStorage.MayHaveValidInventorySlot(GameItem item) =>
            DropGrid(item)?.MayHaveValidInventorySlot(item) == true;

        SlotMarker GraphNodeStorage.TryFindOneValidInventorySlot(GameItem item)
        {
            GameGridInventory grid = DropGrid(item);
            if (grid == null || !grid.MayHaveValidInventorySlot(item)) return null;
            SlotMarker slot = grid.TryFindOneValidInventorySlot(item, keepOrientation: false);
            return slot?.inventory == grid && slot.numTransfer > 0 ? slot : null;
        }
    }

    private static GameItem CreateController()
    {
        GameItem controller = ItemDirectory.CreateEmptyItem();
        AudioHelper.InitSolidMetal(controller);
        controller.maySelectSlotItemFunc = (item, _, _) => GeneralHelper.IsItemOwned(item);
        controller.canSelectSlotItemFunc = controller.maySelectSlotItemFunc;
        controller.mayThisTargetItemFunc = CanConfigure;
        controller.canThisTargetItemFunc = CanConfigure;
        controller.onThisTargetItemFunc = Configure;
        return controller;
    }

    private static bool CanConfigure(GameItem source, GameItem bay) =>
        source?.identifier == ControllerId && IsWarehouse(bay)
        && GeneralHelper.IsItemOwned(source, bay) && Inventories.TryGetValue(bay, out _);

    private static void Configure(GameItem source, GameItem bay)
    {
        if (!CanConfigure(source, bay) || !Inventories.TryGetValue(bay, out var tabs)) return;
        int page = tabs.selected;
        if (page < 0 || page >= tabs.count) return;
        CargoSortWindow.Open(page, Rules(bay)[page] ?? Array.Empty<string>(), Set, Rename);

        void Rename()
        {
            if (!StorePlacement.Destroyed(bay) && !StorePlacement.Destroyed(source) && CanConfigure(source, bay))
                tabs.onRenameRequest?.Invoke(page);
        }

        void Set(string[] tags)
        {
            if (StorePlacement.Destroyed(bay) || StorePlacement.Destroyed(source) || !CanConfigure(source, bay)) return;
            bay.ModifyTag(FilterKeys[page], tag => tag.Enable().SetString(string.Join(",", tags)));
            Rules(bay)[page] = tags;
        }
    }
}
