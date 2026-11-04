using System;

namespace CorpPresence;

internal static class Wilde
{
	public static bool Hydroponics
	{
		get
		{
			if (!Start((PlayerStore p) => p.IsFarmerStart()))
			{
				return Unlocked("RETIRED_FARMER");
			}
			return true;
		}
	}

	public static bool Rats
	{
		get
		{
			if (!Start((PlayerStore p) => p.IsRancherStart()))
			{
				return Unlocked("RETIRED_RANCHER");
			}
			return true;
		}
	}

	public static bool Aquaculture
	{
		get
		{
			if (!Start((PlayerStore p) => p.IsAquafarmerStart()))
			{
				return Unlocked("AQUACULTURE");
			}
			return true;
		}
	}

	private static bool Start(Func<PlayerStore, bool> check)
	{
		try
		{
			return PlayerStore.Instance != null && check(PlayerStore.Instance);
		}
		catch
		{
			return false;
		}
	}

	private static bool Unlocked(string id)
	{
		try
		{
			return PlayerStore.Instance != null && NetworkUpgrade.IsUnlocked(id);
		}
		catch
		{
			return false;
		}
	}
}
