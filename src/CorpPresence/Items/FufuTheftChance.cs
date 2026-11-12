using System;

namespace CorpPresence;

internal static class FufuTheftChance
{
    internal static float Apply(int chance, bool owned)
        => owned ? Math.Min(100, Math.Max(0, chance)) * 0.5f : chance;
}
