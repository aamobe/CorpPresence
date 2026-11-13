using System;

namespace CorpPresence;

internal static class SleepMossRules
{
    private static readonly double AbsorptionShare = 0.1 * Math.Pow(4, 0.75);

    internal static int Absorb(int water, int contaminant, int floorBasisPoints)
    {
        if (water <= 0 || contaminant <= 0) return 0;
        int floor = (int)((long)water * floorBasisPoints / 10000);
        int available = Math.Max(0, contaminant - floor);
        int target = Math.Max(1000, (int)Math.Round(contaminant * AbsorptionShare));
        return Math.Min(available, target);
    }

    internal static int Stage(int progress, int target) =>
        (int)Math.Clamp((long)progress * 3 / Math.Max(1, target), 0, 2);
}
