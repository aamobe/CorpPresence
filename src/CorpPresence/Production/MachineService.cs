using System;
using System.Runtime.CompilerServices;

namespace CorpPresence;

internal static class MachineService
{
    private sealed class Cover
    {
        internal Func<bool> Unlocked;
        internal Action<bool> SetOpen;
        internal bool OpeningFromTool;
    }

    private static readonly ConditionalWeakTable<GameItem, Cover> Covers = new();

    internal static bool Handbook => CorpState.Current?.Workshop?.FeederTrial?.Handbook == true;
    internal static bool IsStockPart(GameItem item, string part) =>
        item?.identifier == "corp_presence_" + part && item.unitCount == 1
        && GeneralHelper.IsItemOwned(item) && !StorePlacement.Destroyed(item) && RodCondition.Read(item) != null;

    internal static bool HasLowLossCoil(GameItem coil) => Handbook && IsStockPart(coil, RodMaterial.IndustrialCoil)
        && (RodCondition.Read(coil).Traits & RodTrait.LowLoss) != 0;

    internal static int CoilCharge(GameItem machine, GameItem coil)
    {
        int charge = MachineryHelper.GetMachinePowerUsage(machine);
        return HasLowLossCoil(coil) ? Math.Max(1, charge - 3) : charge;
    }

    internal static void Register(GameItem machine, Func<bool> unlocked, Action<bool> setOpen) =>
        Covers.Add(machine, new Cover { Unlocked = unlocked, SetOpen = setOpen });

    internal static bool CanOpen(GameItem tool, GameItem target) => FindCover(tool, target) != null;

    private static Cover FindCover(GameItem tool, GameItem target) =>
        tool?.identifier == "screwdriver" && GeneralHelper.IsItemOwned(tool) && target != null
        && Covers.TryGetValue(target, out var cover) && cover.Unlocked() && !target.IsTag("BROKEN_MACHINE_TAG")
        && GeneralHelper.IsItemOwned(target) && ShopInventory.Contains(target)
        && !StoreSession.CompanyProgressPaused && SaveSlot.CanWrite("CorpPresence") ? cover : null;

    internal static bool TryOpen(GameItem tool, GameItem machine)
    {
        var cover = FindCover(tool, machine);
        if (cover == null) return false;
        cover.OpeningFromTool = true;
        try
        {
            cover.SetOpen(true);
            machine.contentWindow.Show();
            return true;
        }
        finally { cover.OpeningFromTool = false; }
    }

    internal static void OpenWindow(GameItem machine)
    {
        if (Covers.TryGetValue(machine, out var cover) && !cover.OpeningFromTool) cover.SetOpen(false);
    }

    internal static void Close(GameItem machine)
    {
        if (Covers.TryGetValue(machine, out var cover)) cover.SetOpen(false);
    }
}
