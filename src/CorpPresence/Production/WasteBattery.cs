using System;

namespace CorpPresence;

internal static class WasteBattery
{
    internal const string Key = "waste_battery";
    internal const string ItemId = "corp_presence_waste_battery";
    internal const int Capacity = 2000;
    internal const int PerJunk = 10;
    private const string Energy = "power_source_item_energy";
    private static bool _feeding;

    internal static Goods.Def Definition() => new()
    {
        Name = "废料电池", NameEn = "Waste Battery",
        Flavor = "感谢久的大力支持", FlavorEn = "Thank you, Jiu, for your generous support.",
        Desc = "容量2000电。将原版“垃圾”拖到电池上，每次消耗1个，补充10电。剩余容量不足10电时不消耗垃圾。无法常规充电，出厂不带电。",
        DescEn = "Capacity: 2000. Drag native Junk onto the battery: one unit adds 10 energy. Requires room for all 10 energy. Cannot recharge normally. Delivered empty.",
        Company = "jackson_private", Art = Key, Fallback = "battery100",
        Value = 0, W = 1, H = 2, Types = new[] { "ENERGY_CELL" },
        Power = new[] { Capacity, 10, 0 }, Shell = () => ItemDirectory.CreateEmptyItem(), Setup = Setup
    };

    // Native junk only permits reactor repair. Extend its OR predicate group;
    // keep native ownership/slot checks and the guarded repair action untouched.
    internal static void AfterMakeJunk(GameItem __result)
    {
        if (__result == null) return;
        __result.mayThisTargetItemFunc -= CanFeed;
        __result.canThisTargetItemFunc -= CanFeed;
        __result.mayThisTargetItemFunc += CanFeed;
        __result.canThisTargetItemFunc += CanFeed;
    }

    internal static bool BlocksCharging(GameItem item) => item?.identifier == ItemId;

    internal static void Setup(GameItem item)
    {
        item.DisableTag("power_source_item_recharge");
        item.DisableTag("BREEDER_POWER_SOURCE_ITEM_TAG");
        // Keep the native battery's output/swap callbacks. Only add our input side.
        item.mayItemTargetThisFunc -= AcceptTarget;
        item.canItemTargetThisFunc -= AcceptTarget;
        item.onItemTargetThisFunc -= Feed;
        item.mayItemTargetThisFunc += AcceptTarget;
        item.canItemTargetThisFunc += AcceptTarget;
        item.onItemTargetThisFunc += Feed;
    }

    private static bool AcceptTarget(GameItem source, GameItem target) => source?.identifier == "junk"
        ? CanFeed(source, target)
        // Native MayBatterySwap still decides whether this exchange is legal.
        : BlocksCharging(target) && source != null && source.IsTag("power_source_item");

    private static bool CanFeed(GameItem source, GameItem target)
    {
        if (_feeding || Settings.CleanupForUninstall || !SaveSlot.CanWrite("CorpPresence")
            || StorePlacement.Destroyed(source) || StorePlacement.Destroyed(target)
            || source.identifier != "junk" || source.unitCount <= 0 || !BlocksCharging(target)
            || target.parentInventory == null || !GeneralHelper.IsItemOwned(source, target)) return false;
        int energy = target.GetTagReadonly(Energy)?.GetInt() ?? -1;
        return energy >= 0 && energy <= Capacity - PerJunk;
    }

    private static void Feed(GameItem source, GameItem target)
    {
        if (!CanFeed(source, target)) return;
        _feeding = true;
        int before = source.unitCount;
        int next = target.GetTagReadonly(Energy).GetInt() + PerJunk;
        try
        {
            try
            {
                if (before == 1) source.Destroy();
                else source.SetUnitCount(before - 1);
            }
            finally
            {
                // A native callback can throw after consumption: credit only observed consumption.
                bool consumed = before == 1 ? StorePlacement.Destroyed(source)
                    : !StorePlacement.Destroyed(source) && source.unitCount == before - 1;
                if (consumed)
                {
                    target.ModifyTag(Energy, tag => tag.SetInt(next));
                    Goods.RefreshBattery(target);
                    Goods.Redraw(target);
                }
            }
        }
        finally { _feeding = false; }
    }
}
