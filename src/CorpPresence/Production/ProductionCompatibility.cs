namespace CorpPresence;

internal static class ProductionCompatibility
{
    // Retire the checkpoint written by the previous unpublished candidate.
    // This does not attempt to alter ingredients, products, power or recipes.
    public static void RemoveLegacyPause(GameItem item)
    {
        const string tag = "CORP_PRODUCTION_COMMIT_PENDING";
        if (item != null && item.state != null && item.state.IsTag(tag))
            item.DisableTag(tag, skipValidate: true);
    }
}
