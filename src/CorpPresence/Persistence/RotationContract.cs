using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal enum RotationKind { Expansion, Supplies, Repair, Joint }

internal sealed class TruckSpec
{
    internal readonly int Days, BasePrice, Bonus;
    internal readonly string[] ItemIds;
    internal readonly int[] Quantities;

    private TruckSpec(int days, int price, int bonus, params (string Id, int Count)[] load)
    {
        Days = days;
        BasePrice = price;
        Bonus = bonus;
        ItemIds = load.Select(part => "corp_presence_" + part.Id).ToArray();
        Quantities = load.Select(part => part.Count).ToArray();
    }

    internal static readonly TruckSpec[] Joint = {
        new(6, 320, 40, ("pump_frame", 2), ("pump_contact", 4)),
        new(10, 420, 60, ("filter_unit", 2))
    };

    private static readonly TruckSpec[][] Rotation = {
        new TruckSpec[] { new(8, 610, 70, ("pump_frame", 4), ("heavy_screen_pad", 2)) },
        new TruckSpec[] { new(6, 290, 40, ("fine_silica_grit", 3), ("filter_liner", 2), ("sleep_moss_gel", 2)) },
        new TruckSpec[] { new(4, 90, 15, ("pump_contact", 2)), new(9, 320, 45, ("filter_unit", 2)) },
        Joint
    };

    internal static TruckSpec[] For(RotationKind kind) => Rotation[(int)kind];
}

internal sealed class ShellReceipt
{
    public bool Issued { get; set; }
    public bool Returned { get; set; }
    public bool Delivered { get; set; }
    public bool InFlight { get; set; }

    internal bool Valid() => (!Returned && !Delivered || Issued) && !(Returned && Delivered)
        && (!InFlight || !Returned && !Delivered);
}

internal sealed class RotationContract : OverhaulContractState
{
    public int Window { get; set; } = -1;
    public RotationKind Kind { get; set; } = (RotationKind)(-1);
    public ShellReceipt[] Shells { get; set; }
    public bool LiningClaimed { get; set; }
    public bool LiningInFlight { get; set; }

    internal static int WindowAt(int? completedDay, int day) =>
        completedDay is int first && day > first ? (day - first - 1) / 3 : -1;

    internal static RotationContract Sign(int window, int day)
    {
        var kind = (RotationKind)(window % 4);
        return new RotationContract {
            Window = window, Serial = window + 2, Kind = kind, SignedDay = day,
            Trucks = TruckSpec.For(kind).Select(spec => OverhaulTruck.Create(spec, day)).ToArray(),
            Shells = kind == RotationKind.Repair ? new[] { new ShellReceipt(), new ShellReceipt() } : Array.Empty<ShellReceipt>()
        };
    }

    internal bool HasOpenObligations => !Abandoned && !CompletedDay.HasValue
        || Shells.Any(s => s.InFlight || s.Issued && !s.Returned && !s.Delivered)
        || Kind == RotationKind.Repair && CompletedDay.HasValue && !LiningClaimed;

    internal override bool Valid()
    {
        if (Window < 0 || Serial != (long)Window + 2 || (int)Kind != Window % 4 || !ValidTrucks(TruckSpec.For(Kind))) return false;
        if (Kind != RotationKind.Repair) return Shells?.Length == 0 && !LiningClaimed && !LiningInFlight;
        return Shells?.Length == 2 && Shells.All(s => s?.Valid() == true)
            && Shells.Count(s => s.Delivered) <= Trucks[1].Received[0]
            && (!(LiningClaimed || LiningInFlight) || CompletedDay.HasValue) && !(LiningClaimed && LiningInFlight);
    }

    internal static bool ValidHistory(List<RotationContract> history, int? firstPaidDay)
    {
        if (history == null) return false;
        RotationContract previous = null;
        foreach (var entry in history)
        {
            if (entry?.Valid() != true || WindowAt(firstPaidDay, entry.SignedDay) != entry.Window) return false;
            if (previous != null && (previous.Window >= entry.Window || previous.CompletedDay > entry.SignedDay
                || !previous.Abandoned && !previous.CompletedDay.HasValue)) return false;
            previous = entry;
        }
        return true;
    }
}
