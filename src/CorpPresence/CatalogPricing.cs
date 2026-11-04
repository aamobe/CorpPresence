using System;
using System.Collections.Generic;
namespace CorpPresence;

// Only sample objects are retained; monetary estimates are never cached.
internal static class CatalogPricing
{
    private static readonly HashSet<CatalogEntry> Samples = new HashSet<CatalogEntry>();
    public static long Epoch { get; private set; }
    public static bool IsResetting { get; private set; }
    internal static void Track(CatalogEntry entry) => Samples.Add(entry);
    public static void Reset()
    {
        if (IsResetting) return;
        IsResetting = true;
        Epoch++;
        try
        {
            var entries = new List<CatalogEntry>(Samples);
            Samples.Clear();
            foreach (var entry in entries)
                if (!entry.ReleaseQuoteSample()) Samples.Add(entry); // Retain failed cleanup for retry.
        }
        finally { IsResetting = false; }
    }
}
