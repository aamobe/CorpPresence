namespace CorpPresence;

internal static class ProductionCompatibility
{
	public static void RemoveLegacyPause(GameItem item)
	{
		if (item != null && item.state != null && item.state.IsTag("CORP_PRODUCTION_COMMIT_PENDING"))
		{
			item.DisableTag("CORP_PRODUCTION_COMMIT_PENDING", skipValidate: true);
		}
	}
}
