using System.Collections.Generic;

namespace CorpPresence;

internal static class CatalogPricing
{
    internal static void AppendDiagnostics(System.Collections.Generic.ICollection<MaintenanceCounter> target)
    {
        target.Add(new MaintenanceCounter("catalog.samples", Samples.Count));
    }

	private static readonly HashSet<CatalogEntry> Samples = new HashSet<CatalogEntry>();

	public static long Epoch { get; private set; }

	public static bool IsResetting { get; private set; }

	internal static void Track(CatalogEntry entry)
	{
		Samples.Add(entry);
	}

	// Failed release stays tracked for the existing cleanup retry path.
	internal static void Release(CatalogEntry entry)
	{
		if (entry == null) return;
		if (entry.ReleaseQuoteSample()) Samples.Remove(entry);
		else Samples.Add(entry);
	}

	internal static void ReleaseExpiredSamples()
	{
		foreach (var entry in new List<CatalogEntry>(Samples))
			if (entry.ReleaseExpiredQuoteSample()) Samples.Remove(entry);
	}

	public static void Reset()
	{
		if (IsResetting)
		{
			return;
		}
		IsResetting = true;
		Epoch++;
		try
		{
			List<CatalogEntry> list = new List<CatalogEntry>(Samples);
			Samples.Clear();
			foreach (CatalogEntry item in list)
			{
				if (!item.ReleaseQuoteSample())
				{
					Samples.Add(item);
				}
			}
		}
		finally
		{
			IsResetting = false;
		}
	}
}
