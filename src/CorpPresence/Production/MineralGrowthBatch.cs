using System;

namespace CorpPresence;

internal static class MineralGrowthBatch
{
    internal const int NightWork = 100;
    internal const int RequiredWork = 2 * NightWork;
    internal const int BasePower = 6;
    internal const string OreBlueprint = "mineral_ore_blueprint";
    internal const string LightSaltBlueprint = "light_salt_blueprint";

    internal sealed record Recipe(string Blueprint, int Residue, PowderComponent Component, int Secondary,
        string Product, int Count);

    internal static readonly Recipe Ore = new(OreBlueprint, 60, PowderComponent.Metal, 15, "common_ore", 1);
    internal static readonly Recipe LightSalt = new(LightSaltBlueprint, 40, PowderComponent.RareMetal, 10,
        "corp_presence_light_salt_core", 2);

    internal static Recipe Find(string blueprint) => blueprint switch
    {
        OreBlueprint => Ore,
        LightSaltBlueprint => LightSalt,
        _ => null
    };

    internal static string Appearance(string blueprint, int progress) => blueprint switch
    {
        OreBlueprint => progress >= RequiredWork ? "_ore-mature" : "_ore-growing",
        LightSaltBlueprint => progress >= RequiredWork ? "_light-salt-core-mature" : "_light-salt-core-growing",
        _ => null
    };

    internal static int SecondaryCost(int points, int quality) =>
        (points * (100 - Math.Clamp(quality, 0, 25)) + 99) / 100;

    internal static int WaterCost(int efficiency) => Math.Max(1, 20 - 20 * Math.Clamp(efficiency, 0, 100) / 100);

    internal static int Advance(int progress, int speed) =>
        (int)Math.Min(RequiredWork, Math.Clamp(progress, 0, RequiredWork) + Math.Max(0L, speed));

    // The buffers are local snapshots in PowderBox component order.
    internal static bool TryPlan(Recipe recipe, Span<int> main, Span<int> secondary, int quality)
    {
        int residueIndex = (int)PowderComponent.Residue;
        int secondaryIndex = (int)recipe.Component;
        int cost = SecondaryCost(recipe.Secondary, quality);
        if (main[residueIndex] < recipe.Residue || secondary[secondaryIndex] < cost
            || PowderClassification.FromParts(main).Branch != PowderComponent.Residue
            || PowderClassification.FromParts(secondary).Branch != recipe.Component)
            return false;

        main[residueIndex] -= recipe.Residue;
        secondary[secondaryIndex] -= cost;
        return true;
    }
}
