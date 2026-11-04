using System;
using System.Collections.Generic;
using UnityEngine;
namespace CorpPresence;

internal static partial class WaterNames
{
    private static readonly string[] GradeKeys = { "mech_condition_pure_water", "mech_condition_high_quality_water", "mech_condition_basewater", "mech_condition_ghostwater", "mech_condition_rustwater", "mech_condition_gutterflow" };
    private static string _locale;
    private static int _frame = -1;
    private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();
    public static void Reset() { _locale = null; _frame = -1; Cache.Clear(); }
    private static string Get(string key)
    {
        var locale = LocHelper.GetCurrentLocaleCode();
        if (_locale != locale || _frame != Time.frameCount) { Cache.Clear(); _locale = locale; _frame = Time.frameCount; }
        if (!Cache.TryGetValue(key, out var value))
        {
            value = LocHelper.GetLocalizedMechanic(key);
            if (!string.IsNullOrEmpty(value) && value != key) Cache[key] = value;
        }
        return value;
    }
    public static string ForGrade(WaterGrade grade) => Get(grade == WaterGrade.Unknown ? "mech_unknown_water" : GradeKeys[(int)grade]);
    public static string ForPurity(int purity)
    {
        var grade = WaterGrades.Of(purity);
        return Get(grade == WaterGrade.Unknown ? "mech_unknown_water" : GradeKeys[(int)grade]);
    }
    public static string LiquidDisplay(Liquid liquid)
    {
        if (liquid == null) return "";
        if (string.IsNullOrEmpty(liquid.identifier)) return liquid.displayName;
        if (liquid.identifier == "corp_isomer_raw") return L.T("粗异构蛋白", "Raw Isomer Protein");
        if (liquid.identifier == "corp_isomer") return L.T("异构蛋白", "Isomer Protein");
        return LiquidKeys.TryGetValue(liquid.identifier, out var key) ? Get(key) : liquid.displayName;
    }
    public static void AfterRealDisplay(ItemFeature __instance, ref string __result, bool __runOriginal)
    {
        if (__runOriginal && WaterIdentity.IsWater(__instance) && __instance.realCondition != null)
            __result = ForPurity(__instance.realCondition.customValue);
    }
    public static void AfterFakeDisplay(ItemFeature __instance, ref string __result, bool __runOriginal)
    {
        if (__runOriginal && WaterIdentity.IsWater(__instance) && __instance.fakeCondition != null)
            __result = ForPurity(__instance.fakeCondition.customValue);
    }
    public static void AfterLabeledString(GameItem currentWaterContainer, ref string __result, bool __runOriginal)
    {
        var feature = currentWaterContainer?.FindItemFeatureByCategory(WaterIdentity.Category);
        if (!__runOriginal || !WaterIdentity.IsWater(feature)) return;
        var condition = feature.IsExposed() ? feature.realCondition : feature.fakeCondition;
        if (condition != null) __result = LocHelper.GetLocalizedMechanic("mech_tooltip_labeled") + "\n" + ForPurity(condition.customValue);
    }
    private static readonly string[] StripColors = { "3d823f", "7ab053", "ccb987", "ba8953", "9b4a3d", "74313b" };
    public static void AfterStripOpen(WaterStripUIManager __instance, GameItem testStrip, bool __runOriginal)
    {
        if (!__runOriginal || testStrip == null) return;
        var grade = WaterGrades.Of(testStrip.GetTagReadonly("TEST_WATER_QUALITY").GetInt());
        if (__instance.qualityStrip != null)
            __instance.qualityStrip.color = grade == WaterGrade.Unknown ? Color.white : MetaHelper.HexToColor(StripColors[(int)grade]);
        if (grade != WaterGrade.Unknown) testStrip.SetSpriteAndShape("Items/items_amenities", "strip" + (int)grade);
        var feature = __instance.currentWaterContainer?.FindItemFeatureByCategory(WaterIdentity.Category);
        if (WaterIdentity.IsWater(feature) && __instance.labelTMP != null)
        {
            var condition = feature.IsExposed() ? feature.realCondition : feature.fakeCondition;
            if (condition != null) __instance.labelTMP.text = LocHelper.GetLocalizedMechanic("mech_tooltip_labeled") + "\n" + ForPurity(condition.customValue);
        }
    }
}



