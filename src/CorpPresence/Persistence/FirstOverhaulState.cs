using System;
using System.Linq;

namespace CorpPresence;

internal abstract class OverhaulContractState
{
    public int Serial { get; set; }
    public int SignedDay { get; set; } = -1;
    public bool Abandoned { get; set; }
    public int? CompletedDay { get; set; }
    public OverhaulTruck[] Trucks { get; set; }

    internal int NextTruck => Trucks.TakeWhile(truck => truck.CompletedDay.HasValue).Count();
    internal abstract bool Valid();

    protected bool ValidTrucks(TruckSpec[] specs)
    {
        if (SignedDay < 0 || SignedDay > int.MaxValue - specs.Max(s => s.Days) || Trucks?.Length != specs.Length) return false;
        for (int i = 0; i < specs.Length; i++)
        {
            if (Trucks[i]?.Valid(specs[i], SignedDay) != true) return false;
            if (i > 0 && Trucks[i].StartedDay.HasValue && !(Trucks[i - 1].CompletedDay <= Trucks[i].StartedDay)) return false;
        }
        return CompletedDay == Trucks[Trucks.Length - 1].CompletedDay
            && (!Abandoned || NextTruck < Trucks.Length && !Trucks[NextTruck].StartedDay.HasValue);
    }
}

internal sealed class FirstOverhaulState : OverhaulContractState
{
    internal static FirstOverhaulState Sign(int day) => new()
    {
        Serial = 1, SignedDay = day, Trucks = TruckSpec.Joint.Select(spec => OverhaulTruck.Create(spec, day)).ToArray()
    };

    internal override bool Valid() => Serial == 1 && ValidTrucks(TruckSpec.Joint);
}

internal sealed class OverhaulTruck
{
    public int DueDay { get; set; }
    public int BasePrice { get; set; }
    public int Bonus { get; set; }
    public int? StartedDay { get; set; }
    public int Amount { get; set; }
    public int[] Received { get; set; }
    public int[] SourceIds { get; set; }
    public int Paid { get; set; }
    public bool InFlight { get; set; }
    public int? CompletedDay { get; set; }

    internal static readonly int[][] Quantities = TruckSpec.Joint.Select(spec => spec.Quantities).ToArray();
    internal static int Deadline(int index, int day) => day + TruckSpec.Joint[index].Days;
    internal static int Price(int index) => TruckSpec.Joint[index].BasePrice;
    internal static int Premium(int index) => TruckSpec.Joint[index].Bonus;
    internal int Quote(int day) => StartedDay.HasValue ? Amount : BasePrice + (day <= DueDay ? Bonus : 0);

    internal static OverhaulTruck Create(TruckSpec spec, int day) => new()
    {
        DueDay = day + spec.Days, BasePrice = spec.BasePrice, Bonus = spec.Bonus,
        Received = new int[spec.Quantities.Length], SourceIds = new int[spec.Quantities.Sum()]
    };

    internal bool Valid(TruckSpec spec, int signedDay)
    {
        int[] quantities = spec.Quantities;
        if (DueDay != signedDay + spec.Days || BasePrice != spec.BasePrice || Bonus != spec.Bonus
            || Received?.Length != quantities.Length || SourceIds?.Length != quantities.Sum()) return false;
        for (int i = 0; i < quantities.Length; i++)
            if (Received[i] < 0 || Received[i] > quantities[i]) return false;

        if (StartedDay is not int started)
            return !InFlight && Paid == 0 && Amount == 0 && CompletedDay == null
                && Received.All(n => n == 0) && SourceIds.All(n => n == 0);
        return started >= signedDay && Amount == BasePrice + (started <= DueDay ? Bonus : 0)
            && SourceIds.All(n => n > 0) && (InFlight || Received.Any(n => n > 0))
            && Paid >= 0 && Paid <= Amount && (Paid == 0 || Received.SequenceEqual(quantities))
            && (CompletedDay.HasValue ? !InFlight && Paid == Amount && CompletedDay >= started : Paid < Amount);
    }
}
