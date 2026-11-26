using System;

namespace CorpPresence;

internal enum CrusherFeed { None, Junk, Scrap, Glass, SalvageElectronics, SpentLining, SpentContact, AlloyBillet, AlloyOffcut }

internal static class CrusherBatch
{
    private static readonly int[] Mixed = { 25, 20, 3, 7, 45 };
    private static readonly int[] MetalRich = { 45, 15, 5, 10, 25 };
    private static readonly int[] SilicaRich = { 15, 45, 2, 8, 30 };
    private static readonly int[] Electronics = { 20, 25, 8, 22, 25 };
    private static readonly int[] Scrap = { 75, 10, 5, 5, 5 };

    private static readonly int[] Glass = { 0, 85, 0, 0, 15 };

    private static readonly int[] Salvage = { 15, 25, 8, 22, 30 };
    private static readonly int[] Lining = { 5, 10, 0, 0, 85 };
    private static readonly int[] HardLining = { 10, 15, 0, 0, 75 };

    private static readonly int[] Contact = { 0, 0, 10, 10, 0 };

    internal static CrusherFeed Classify(string identifier) => identifier switch
    {
        "corp_presence_salvage_electronics" => CrusherFeed.SalvageElectronics,
        "corp_presence_spent_lining" => CrusherFeed.SpentLining,
        "corp_presence_" + IndustrialRecipes.SpentContact => CrusherFeed.SpentContact,
        "corp_presence_" + IndustrialRecipes.Billet => CrusherFeed.AlloyBillet,
        "corp_presence_" + IndustrialRecipes.Offcut => CrusherFeed.AlloyOffcut,
        "junk" => CrusherFeed.Junk,
        "scrap_metal" => CrusherFeed.Scrap,
        "empty_beer_bottle" or "glass_shard" => CrusherFeed.Glass,
        _ => CrusherFeed.None
    };

    internal const int MaxResidueShift = 3;

    internal static ReadOnlySpan<int> HardLiningParts => HardLining;
    internal static int HardLiningMaximum(int quality) => 100 - HardLining[4] + Residue(HardLining[4], quality);

    internal static bool Fits(long total, int batchSize = 100) =>
        total >= 0 && total <= PowderColor.Capacity - batchSize;

    internal static bool CanFitFeed(long total, CrusherFeed feed, int quality)
    {
        int size = MaximumPowder(feed, quality);
        return size > 0 && Fits(total, size);
    }

    internal static int MaximumPowder(CrusherFeed feed, int quality)
    {
        if (feed == CrusherFeed.SpentContact) return 20;
        if (feed is CrusherFeed.AlloyBillet or CrusherFeed.AlloyOffcut or CrusherFeed.None) return 0;
        // The recipe with the least residue produces the largest batch after quality reduction.
        int residue = feed switch
        {
            CrusherFeed.Scrap => Scrap[4],
            CrusherFeed.Glass => Glass[4],
            CrusherFeed.SalvageElectronics => Salvage[4],
            CrusherFeed.SpentLining => Lining[4],
            _ => Math.Min(MetalRich[4], Electronics[4]) - MaxResidueShift
        };
        return 100 - residue + Residue(residue, quality);
    }

    internal static (int[] Min, int[] Max) Range(CrusherFeed feed, int quality)
    {
        var low = Parts(feed, 0).ToArray();
        var high = (int[])low.Clone();
        if (feed == CrusherFeed.Junk)
        {
            int[] spread = { 5, 5 + MaxResidueShift, 1, 1, MaxResidueShift };
            foreach (var recipe in new[] { Mixed, MetalRich, SilicaRich, Electronics })
            {
                for (int i = 0; i < recipe.Length; i++)
                {
                    low[i] = Math.Min(low[i], recipe[i] - spread[i]);
                    high[i] = Math.Max(high[i], recipe[i] + spread[i]);
                }
            }
        }
        low[4] = Residue(low[4], quality);
        high[4] = Residue(high[4], quality);
        return (low, high);
    }

    internal static int NightlyLimit(int speed) => Math.Clamp(speed / 100, 3, 5);

    internal static int EffectiveQuality(int moduleQuality, bool preserveResidue) =>
        preserveResidue ? 0 : Math.Clamp(moduleQuality, 0, 100);

    internal static int Residue(int baseResidue, int quality) =>
        baseResidue - baseResidue * Math.Clamp(quality, 0, 100) / 100;

    internal static ReadOnlySpan<int> Parts(CrusherFeed feed, int roll, int metalShift = 0, int rareShift = 0, int residueShift = 0)
    {
        if (feed == CrusherFeed.SpentContact) return Contact;
        if (feed is CrusherFeed.AlloyBillet or CrusherFeed.AlloyOffcut or CrusherFeed.None) throw new ArgumentOutOfRangeException(nameof(feed));
        if (feed == CrusherFeed.Scrap) return Scrap;
        if (feed == CrusherFeed.Glass) return Glass;
        if (feed == CrusherFeed.SalvageElectronics) return Salvage;
        if (feed == CrusherFeed.SpentLining) return Lining;
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
