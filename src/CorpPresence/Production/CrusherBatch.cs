using System;

namespace CorpPresence;

internal enum CrusherFeed { None, Junk, Scrap, Glass }

internal static class CrusherBatch
{
    private static readonly int[] Mixed = { 25, 20, 3, 7, 45 };
    private static readonly int[] MetalRich = { 45, 15, 5, 10, 25 };
    private static readonly int[] SilicaRich = { 15, 45, 2, 8, 30 };
    private static readonly int[] Electronics = { 20, 25, 8, 22, 25 };
    private static readonly int[] Scrap = { 75, 10, 5, 5, 5 };

    private static readonly int[] Glass = { 0, 85, 0, 0, 15 };

    internal static CrusherFeed Classify(string identifier) => identifier switch
    {
        "junk" => CrusherFeed.Junk,
        "scrap_metal" => CrusherFeed.Scrap,
        "empty_beer_bottle" or "glass_shard" => CrusherFeed.Glass,
        _ => CrusherFeed.None
    };

    internal const int MaxResidueShift = 3;

    internal static bool Fits(long total, int batchSize = 100) =>
        total >= 0 && total <= PowderColor.Capacity - batchSize;

    internal static bool CanFitFeed(long total, CrusherFeed feed, int quality)
    {
        // The recipe with the least residue produces the largest batch after quality reduction.
        int residue = feed switch
        {
            CrusherFeed.Scrap => Scrap[4],
            CrusherFeed.Glass => Glass[4],
            _ => Math.Min(MetalRich[4], Electronics[4]) - MaxResidueShift
        };
        return Fits(total, 100 - residue + Residue(residue, quality));
    }

    internal static int NightlyLimit(int speed) => Math.Clamp(speed / 100, 3, 5);

    internal static int EffectiveQuality(int moduleQuality, bool preserveResidue) =>
        preserveResidue ? 0 : Math.Clamp(moduleQuality, 0, 100);

    internal static int Residue(int baseResidue, int quality) =>
        baseResidue - baseResidue * Math.Clamp(quality, 0, 100) / 100;

    internal static ReadOnlySpan<int> Parts(CrusherFeed feed, int roll, int metalShift = 0, int rareShift = 0, int residueShift = 0)
    {
        if (feed == CrusherFeed.Scrap) return Scrap;
        if (feed == CrusherFeed.Glass) return Glass;
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
