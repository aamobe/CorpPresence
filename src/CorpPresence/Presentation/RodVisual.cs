namespace CorpPresence;

internal readonly record struct RodVisual(string Art, int Tint, int JacketTint = -1)
{
    internal const string CastArt = "alloy-rod";
    internal const string ShortArt = "alloy-rod-short";
    private static readonly (int Red, int Green, int Blue, int Weight)[] ComponentTones =
    {
        (187, 198, 212, 1),
        (225, 213, 164, 3),
        (145, 133, 190, 3),
        (232, 129, 81, 6),
        (122, 129, 115, 2)
    };

    internal static RodVisual From(RodCondition condition, bool shortStock = false)
    {
        string baseArt = shortStock ? ShortArt : CastArt;
        string art = baseArt + ((condition.Jacket?.Process ?? condition.Process) switch
        {
            RodProcess.Temper => "-temper",
            RodProcess.Densify => "-densify",
            RodProcess.Align => "-align",
            RodProcess.Harden => "-harden",
            _ => ""
        });
        return new(art, ColorOf(condition.Assay), condition.Jacket == null ? -1 : ColorOf(condition.Jacket.Assay));
    }

    private static int ColorOf(RodMaterial alloy)
    {
        System.ReadOnlySpan<int> amounts = stackalloc[] { alloy.Metal, alloy.Silica, alloy.Rare, alloy.Conductive, alloy.Residue };
        long red = 0, green = 0, blue = 0, total = 0;
        for (int i = 0; i < amounts.Length; i++)
        {
            var tone = ComponentTones[i];
            long weight = (long)amounts[i] * tone.Weight;
            red += tone.Red * weight;
            green += tone.Green * weight;
            blue += tone.Blue * weight;
            total += weight;
        }
        // Small composition changes share a color step and the same resident sprite.
        int Channel(long sum) => (int)((sum + 8 * total) / (16 * total)) * 16;
        return Channel(red) << 16 | Channel(green) << 8 | Channel(blue);
    }
}
