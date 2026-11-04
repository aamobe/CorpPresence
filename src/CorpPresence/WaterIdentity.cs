namespace CorpPresence;

internal static class WaterIdentity
{
    public const string Category = "CATEGORY_WATER_PURITY";
    public static bool IsWater(ItemFeature feature) => feature != null && feature.useCondition && feature.category == Category;
}
