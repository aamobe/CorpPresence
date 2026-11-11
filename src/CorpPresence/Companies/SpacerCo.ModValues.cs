using System.Collections.Generic;

namespace CorpPresence;

internal sealed partial class SpacerCo
{
	private static readonly string[] ModPool = new string[30]
	{
		"internal_springs", "trigger_group", "match_barrel", "pistol_switch", "ammo_display", "grip_gel", "rds", "rds_makeshift", "silencer", "silencer2",
		"silencer_makeshift", "barrel", "compensator", "laser", "laser2", "laser_toy", "bipod", "gl", "grip", "mag_10mm",
		"mag_10mm_high", "mag_10mm_ext", "mag_10mm_smg", "mag_10mm_smg_high", "mag_10mm_smg_box", "mag_22", "mag_22_high", "mag_22_ext", "stock", "short_stock"
	};

	private static Dictionary<string, long> ModValues()
	{
		// One snapshot per roll: an unavailable factory must be retried next time.
		// unitValue is the factory's base value, not an event-adjusted quote.
		Dictionary<string, long> dictionary = new Dictionary<string, long>();
		string[] modPool = ModPool;
		foreach (string text in modPool)
		{
			GameItem gameItem = null;
			try
			{
				gameItem = DirectoryMaster.Item(text, isOwned: false);
				if (gameItem != null && gameItem.unitValue > 0)
				{
					dictionary[text] = gameItem.unitValue;
				}
			}
			catch
			{
			}
			finally
			{
				// Every returned sample is ours, including zero/invalid-value samples.
				try { gameItem?.Destroy(); } catch { }
			}
		}
		return dictionary.Count > 0 ? dictionary : null;
	}

}
