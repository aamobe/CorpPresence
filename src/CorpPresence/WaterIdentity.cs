using System.Collections.Generic;
namespace CorpPresence;

internal static class WaterIdentity
{
    public const string Category = "CATEGORY_WATER_PURITY";
    public static bool IsWater(ItemFeature feature) => feature != null && feature.useCondition && feature.category == Category;
    public static string RealKey(ItemFeature feature) => IsWater(feature) ? ConditionKey(feature.realCondition) : feature.GetBaseRealDisplay();
    public static string FakeKey(ItemFeature feature) => IsWater(feature) ? ConditionKey(feature.fakeCondition) : feature.GetBaseFakeDisplay();
    public static string ConditionKey(ItemCondition condition) => condition == null ? "" : WaterGrades.Key(WaterGrades.Of(condition.customValue));
    public static string CurrentKey(GameItem item)
    {
        if (item == null || !item.IsTag("LIQUID_CONTAINER_TAG")) return "";
        var feature = item.FindItemFeatureByCategory(Category);
        if (!IsWater(feature)) return "";
        return feature.IsExposed() ? WaterGrades.Key(WaterGrades.Of(WaterHelper.GetWaterPurity(item))) : ConditionKey(feature.fakeCondition);
    }
    public static string AcceptanceKey(GameItem item)
    {
        var key = CurrentKey(item);
        // Preserve vanilla handling for unknown/missing water features.
        return key.Length != 0 ? key : WaterFeatureHelper.GetCurrentWaterDisplay(item);
    }
    public static bool AcceptContains(List<string> accepted, string key)
    {
        if (accepted.Contains(key)) return true;
        var grade = WaterGrades.FromKey(key);
        // The original helper still runs, including any other MOD's patches.
        // Foreign lists containing display names keep vanilla dynamic-name semantics.
        return grade != WaterGrade.Unknown && accepted.Contains(WaterNames.ForGrade(grade));
    }
    public static bool BeforeFeatureMatch(ItemFeature __instance, ref bool __result, bool __runOriginal)
    {
        if (!__runOriginal || !IsWater(__instance) || __instance.realCondition == null || __instance.fakeCondition == null) return true;
        var real = WaterGrades.Of(__instance.realCondition.customValue);
        var fake = WaterGrades.Of(__instance.fakeCondition.customValue);
        if (real == WaterGrade.Unknown || fake == WaterGrade.Unknown) return true;
        __result = real == fake;
        return false;
    }
}

