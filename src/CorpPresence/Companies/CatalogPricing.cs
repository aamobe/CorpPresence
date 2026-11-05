using System.Collections.Generic;

namespace CorpPresence;

internal static class CatalogPricing
{
	private static readonly HashSet<CatalogEntry> Samples = new HashSet<CatalogEntry>();

	public static long Epoch { get; private set; }

	public static bool IsResetting { get; private set; }

	internal static void Track(CatalogEntry entry)
	{
		Samples.Add(entry);
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
