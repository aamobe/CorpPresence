using System.Collections.Generic;

namespace CorpPresence.UI;

internal sealed class SwapPageState
{
    internal float ScannedAt = -1f;
    internal int TotalCount;
    internal readonly List<(string Name, string Reason, string Target)> Rows = new();

    internal void Reset()
    {
        ScannedAt = -1f;
        TotalCount = 0;
        Rows.Clear();
    }
}

internal sealed class RefurbishmentPageState
{
    internal float ScannedAt = -1f;
    internal int TotalCount;
    internal readonly List<(string Name, string From, string To, int Fee)> Rows = new();

    internal void Reset()
    {
        ScannedAt = -1f;
        TotalCount = 0;
        Rows.Clear();
    }
}

internal sealed class BrandPageState
{
    internal string Selected;
    internal Theme Theme;
    internal readonly Dictionary<string, (float w, float h, bool fits)> Fits = new();

    internal void Reset()
    {
        Selected = null;
        Theme = null;
        Fits.Clear();
    }
}
