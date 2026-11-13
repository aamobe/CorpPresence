using System;

namespace CorpPresence;

internal static class CrusherBatch
{
    private static readonly int[] Mixed = { 25, 20, 3, 7, 45 };
    private static readonly int[] MetalRich = { 45, 15, 5, 10, 25 };
    private static readonly int[] SilicaRich = { 15, 45, 2, 8, 30 };
    private static readonly int[] Electronics = { 20, 25, 8, 22, 25 };
    private static readonly int[] Scrap = { 75, 10, 5, 5, 5 };

    internal const int MaxResidueShift = 3;

    internal static bool Fits(long total, int batchSize = 100) =>
        total >= 0 && total <= PowderColor.Capacity - batchSize;

    internal static bool CanFitFeed(long total, bool scrap, int quality)
    {
        // The recipe with the least residue produces the largest batch after quality reduction.
        int residue = scrap ? Scrap[4] : Math.Min(MetalRich[4], Electronics[4]) - MaxResidueShift;
        return Fits(total, 100 - residue + Residue(residue, quality));
    }

    internal static int NightlyLimit(int speed) => Math.Clamp(speed / 100, 3, 5);

    internal static int EffectiveQuality(int moduleQuality, bool preserveResidue) =>
        preserveResidue ? 0 : Math.Clamp(moduleQuality, 0, 100);

    internal static int Residue(int baseResidue, int quality) =>
        baseResidue - baseResidue * Math.Clamp(quality, 0, 100) / 100;

    internal static ReadOnlySpan<int> Parts(bool scrap, int roll, int metalShift = 0, int rareShift = 0, int residueShift = 0)
    {
        if (scrap) return Scrap;
        var recipe = roll switch
        {
            < 40 => Mixed,
            < 65 => MetalRich,
            < 90 => SilicaRich,
            _ => Electronics
        };
        return new[]
        {
            recipe[0] + metalShift,
            recipe[1] - metalShift - residueShift,
            recipe[2] + rareShift,
            recipe[3] - rareShift,
            recipe[4] + residueShift
        };
    }
}
