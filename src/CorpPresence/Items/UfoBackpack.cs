using System;
using System.Linq;

namespace CorpPresence;

internal static class UfoBackpack
{
    internal const string Key = "ufo_backpack";
    internal const string ItemId = "corp_presence_ufo_backpack";
    internal const string SlotId = "corp_ufo_toolbox_slot";

    internal static Goods.Def Definition() => new()
    {
        Name = "飞碟背包", NameEn = "UFO Backpack",
        Flavor = "飞碟赠礼", FlavorEn = "A gift from UFO",
        Desc = "与原版大背包相同的8×9主仓，另有独立栏位内置原版工具箱。工具箱初始3槽，可用废金属扩至9槽。",
        DescEn = "The native large backpack's 8 x 9 storage, plus a separate bay with a built-in native toolbox. Starts with 3 tool slots; upgrade to 9 using scrap metal.",
        Company = "jackson_private", Art = Key, Fallback = "simple_backpack_large",
        Value = 650, W = 3, H = 3, Shape = "###|###|###",
        Types = Array.Empty<string>(), Shell = CreateShell
    };

    private static GameItem CreateShell()
    {
        // Use the original factory to preserve equipment, audio and storage rules.
        var bag = DirectoryMaster.Item("backpack_large", isOwned: false);
        try
        {
            var window = bag.contentWindow;
            var main = window.childElement;
            var bay = new GameSlotInventory(3, 3) { identifier = SlotId };
            bay.SetBackgroundFadeSprite("Items/items_tool2", "toolbox_old");
            bay.mayInventoryAddItemFunc = (item, _) => item?.identifier == "toolbox"
                && GeneralHelper.IsItemOwned(item, bag);
            bay.mayInventoryRemoveItemFunc = (_, _) => Settings.CleanupForUninstall;
            window.Detach();
            window.Attach(new MachineryPanelRow(
                MachineHelper.Panel(L.K("ufoBackpack.storage", "主仓", "Storage"), main, "storage"),
                MachineHelper.Panel(L.K("ufoBackpack.toolbox", "内置工具箱", "Built-in Toolbox"), bay, "toolbox")));
            return bag;
        }
        catch { bag.Destroy(); throw; }
    }

    // Only called for a new gift, never from a factory or an onLoaded callback.
    internal static bool InitializeGift(GameItem bag)
    {
        if (bag?.identifier != ItemId) return false;
        var bay = GraphUtils.FindAllChildrenType<GameSlotInventory>(bag)
            .FirstOrDefault(slot => slot.identifier == SlotId);
        if (bay == null) return false;
        if (bay.childItem != null) return bay.childItem.identifier == "toolbox";
        GameItem box = null;
        try
        {
            box = DirectoryMaster.Item("toolbox", isOwned: true);
            bay.UncheckedAccept(box);
            return ReferenceEquals(bay.childItem, box);
        }
        finally
        {
            if (box != null && !ReferenceEquals(bay.childItem, box)) box.Destroy();
        }
    }
}
