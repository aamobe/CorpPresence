using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class PortraitResources
{
	private static readonly HashSet<string> Names = BuildIndex();

	public static bool Exists(string name)
	{
		if (name != null)
		{
			return Names.Contains(name);
		}
		return false;
	}

	private static HashSet<string> BuildIndex()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		string[] manifestResourceNames = typeof(PortraitResources).Assembly.GetManifestResourceNames();
		foreach (string text in manifestResourceNames)
		{
			if (text.StartsWith("CorpPresence.portraits.", StringComparison.Ordinal) && text.EndsWith(".png", StringComparison.Ordinal))
			{
				hashSet.Add(text.Substring("CorpPresence.portraits.".Length, text.Length - "CorpPresence.portraits.".Length - ".png".Length));
			}
		}
		return hashSet;
	}
}
