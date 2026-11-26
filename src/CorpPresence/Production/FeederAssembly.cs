using System;
using System.Runtime.CompilerServices;

namespace CorpPresence;

internal static class FeederAssembly
{
    private sealed class Cover
    {
        internal GameSlotInventory Shaft;
        internal GameSlotInventory Sleeve;
        internal Func<bool> IsEmpty;
    }

    private static readonly ConditionalWeakTable<GameItem, Cover> Covers = new();
    internal const int BatchSize = 2;
    internal const int BaseCharge = 10;
    internal const int ExtraCharge = 6;

    internal static void Register(GameItem machine, GameSlotInventory shaft, GameSlotInventory sleeve, Action<bool> setOpen,
        Func<bool> isEmpty)
    {
        Covers.Add(machine, new Cover { Shaft = shaft, Sleeve = sleeve, IsEmpty = isEmpty });
        MachineService.Register(machine, () => machine.identifier == "corp_presence_" + MineralCrusher.FeederKey, setOpen);
    }

    internal static bool Empty(GameItem machine) => Covers.TryGetValue(machine, out var cover) && cover.IsEmpty();

    internal static GameItem SleeveForSecondBatch(GameItem machine)
    {
        if (!Covers.TryGetValue(machine, out var cover)) return null;
        var shaft = cover.Shaft.childItem;
        var sleeve = cover.Sleeve.childItem;
        if (!MachineService.IsStockPart(shaft, RodMaterial.FeedShaft) || !MachineService.IsStockPart(sleeve, RodMaterial.FeedSleeve)) return null;
        var axle = RodCondition.Read(shaft);
        var bearing = RodCondition.Read(sleeve);
        return axle.Core.Load >= 50 && (axle.Traits & RodTrait.Fatigue) != 0
            && !bearing.Scored && bearing.Surface.Porosity is >= 5 and <= 9 && bearing.Surface.Wear >= 40
            && SleeveOiling.Oil(sleeve) == 1 ? sleeve : null;
    }
}
