using System;

namespace CorpPresence;

internal static class ClassifierBatch
{
    internal const int Power = 8;
    internal const string Screen = "fine_screen";
    internal const string Sleeve = "crystal_sleeve";
    internal sealed record Recipe(string Id, int Nights, int Coarse, int Fine, int Crystal, int BasePower = Power,
        bool LowLoss = false, bool Precision = false)
    {
        internal int Silica => (Coarse + Fine) * 20;
        internal int NightPower => BasePower - (LowLoss ? 3 : 0);
        internal int SilicaPurity => Precision ? 7000 : 8100;
        internal bool Retrofitted => LowLoss || Precision;
    }
    private static readonly Recipe Flow = new("flow.v1", 1, 2, 1, 0);
    private static readonly Recipe Double = new("double.v1", 1, 4, 2, 0, 16);
    private static readonly Recipe Fine = new("fine.v1", 2, 1, 2, 0);
    private static readonly Recipe Crystal = new("crystal.v1", 2, 0, 0, 2);
    internal static readonly Recipe Fast = new("fast.v1", 1, 0, 0, 2);

    internal static Recipe Plan(int mode, string tool, int sleeveCharges, bool hasPad = false) => mode switch
    {
        0 => Flow,
        1 when tool == Screen => Fine,
        2 when tool == Sleeve => sleeveCharges > 0 ? Fast : Crystal,
        3 when hasPad => Double,
        _ => null
    };

    internal static Recipe Restore(string id) => id switch
    {
        "flow.v1" => Flow, "fine.v1" => Fine,
        "crystal.v1" => Crystal, "fast.v1" => Fast,
        "double.v1" => Double,
        _ => null
    };

    internal static Recipe WithService(Recipe recipe, bool lowLoss, bool precision)
    {
        if (recipe == null || recipe.Crystal > 0) return recipe;
        precision &= recipe.Id == Fine.Id;
        return recipe.LowLoss == lowLoss && recipe.Precision == precision ? recipe :
            recipe with { LowLoss = lowLoss, Precision = precision };
    }

    internal static bool TakeSilica(int[] parts, int amount = 60, int purity = 8100)
    {
        int index = (int)PowderComponent.Silica;
        if (parts == null || parts.Length != 5 || amount <= 0 || parts[index] < amount) return false;
        long total = 0;
        foreach (int part in parts)
        {
            if (part < 0) return false;
            total += part;
        }
        if (total > PowderColor.Capacity || parts[index] * 10000L < total * purity)
            return false;
        parts[index] -= amount;
        return true;
    }
}
