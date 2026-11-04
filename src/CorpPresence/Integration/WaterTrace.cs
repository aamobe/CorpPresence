#if WATER_TRACE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CorpPresence;

internal static class WaterTrace
{
    private static long _sequence;
    private static readonly Dictionary<(GameItem, StoreClient, string), string> LastCheck = new();
    private static readonly Dictionary<GameItem, string> LastDisplay = new();

    internal static void Ready()
    {
        LastCheck.Clear();
        LastDisplay.Clear();
        Write("ready", $"公司增强 {Main.Version}；模组版本及启用状态见本日志的 [ModLoader] audit");
    }

    private static void Write(string stage, string message) =>
        ModLog.Info($"[WATER-TRACE {++_sequence:D5}] {stage} {message}");

    private static void Observe(Action action)
    {
        try { action(); }
        catch (Exception e) { Write("probe-error", e.GetType().Name); }
    }

    private static bool Tracked(GameItem item) => item != null && item.IsTag("LIQUID_CONTAINER_TAG");

    private static string Snapshot(GameItem item)
    {
        // GetTagReadonly creates missing tags internally. Read the existing dictionary instead.
        var tags = item.modifiedState.dict;
        long volume = 0, water = 0;
        foreach (var liquid in Liquid.Liquids)
        {
            if (!tags.TryGetValue(liquid.currentPart, out var tag)) continue;
            int amount = tag.GetInt();
            volume += amount;
            if (liquid.identifier == "water") water = amount;
        }
        long purity = volume > 0 ? water * 10000 / volume : 0;
        return $"id={item.uniqueId} object={RuntimeHelpers.GetHashCode(item):x} item={item.identifier} " +
            $"at={item.parentInventory?.identifier ?? "-"} volume_uL={volume} purity_bp={purity} " +
            $"WATER={item.IsTag("WATER")} typeWater={item.IsGameItemType("WATER")} owned={item.IsTag("IS_OWNED_TAG")} value={item.unitValue}";
    }

    private static string Caller() => string.Join(" <- ", Environment.StackTrace
        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Where(line => !line.Contains("WaterTrace.") && !line.Contains("get_StackTrace"))
        .Take(8).Select(line => line.Trim()));

    private static void ItemEvent(string stage, GameItem item, string detail = "") =>
        Observe(() => { if (Tracked(item)) Write(stage, Snapshot(item) + " " + detail); });

    internal static void Created(GameItem __result) => ItemEvent("factory", __result);

    internal static void Drained(GameItem __0, int __1, bool __runOriginal) =>
        ItemEvent("vanilla-remove-ml", __0, $"requested={__1} ran={__runOriginal} caller={Caller()}");

    internal static void LiquidChanged(GameItem __0, bool __runOriginal) =>
        ItemEvent("vanilla-liquid-refresh", __0, $"ran={__runOriginal}");

    internal static void BeforeFill(GameItem __0, int __1, int __2, out string __state)
    {
        __state = null;
        try
        {
            if (Tracked(__0)) __state = $"grade={__1} requested={__2} before=[{Snapshot(__0)}] caller={Caller()}";
        }
        catch (Exception e) { Write("probe-error", e.GetType().Name); }
    }

    internal static void AfterFill(GameItem __0, string __state, bool __runOriginal)
    {
        if (__state != null) ItemEvent("vanilla-fill", __0, $"ran={__runOriginal} {__state}");
    }

    internal static void BeforeType(GameItem __0, out bool __state) =>
        __state = __0?.IsTag("WATER") == true;

    internal static void AfterType(GameItem __0, bool __state, bool __runOriginal) => Observe(() =>
    {
        if (Tracked(__0) && __state != __0.IsTag("WATER"))
            Write("vanilla-water-tag", $"{__state}->{__0.IsTag("WATER")} ran={__runOriginal} {Snapshot(__0)} caller={Caller()}");
    });

    internal static void Decoded(GameItem __result) => Observe(() =>
    {
        LastCheck.Clear();
        LastDisplay.Clear();
        if (__result == null) return;
        ItemEvent("load", __result);
        foreach (var item in GraphUtils.FindAllChildrenType<GameItem>(__result)) ItemEvent("load", item);
    });

    internal static void Moved(GameItem __instance, GraphNodeStorage __0, bool __result) => Observe(() =>
    {
        if (__result && Tracked(__instance))
            Write("move", $"{Snapshot(__instance)} inventory={(__0 as GameInventory)?.identifier ?? "-"} caller={Caller()}");
    });

    internal static void WaterDisplay(GameItem __0, string __result, bool __runOriginal) => Observe(() =>
    {
        if (!Tracked(__0)) return;
        if (LastDisplay.Count >= 1024) LastDisplay.Clear();
        LastDisplay[__0] = $"live='{__result}' base='{WaterFeatureHelper.BASE_WATER_DISPLAY}' " +
            $"high='{WaterFeatureHelper.HIGH_QUALITY_WATER_DISPLAY}' ghost='{WaterFeatureHelper.GHOSTWATER_DISPLAY}' displayRan={__runOriginal}";
    });

    internal static void BeforeCheck(GameItem __0)
    {
        if (__0 != null) LastDisplay.Remove(__0);
    }

    internal static void Checked(StoreClient __instance, GameItem __0, bool __result, bool __runOriginal) =>
        Check("trade-check", __instance, __0, __result, __runOriginal);

    internal static void CheckedInner(StoreClient __instance, GameItem __0, bool __result, bool __runOriginal) =>
        Check("vanilla-trade-check", __instance, __0, __result, __runOriginal);

    private static void Check(string stage, StoreClient client, GameItem item, bool result, bool ran) => Observe(() =>
    {
        if (!Tracked(item)) return;
        LastDisplay.TryGetValue(item, out var display);
        string detail = $"{Snapshot(item)} client={client.identifier} accepted={result} ran={ran} {display ?? "water-display-not-called"}";
        var key = (item, client, stage);
        if (LastCheck.TryGetValue(key, out var previous) && previous == detail) return;
        if (LastCheck.Count >= 1024) LastCheck.Clear();
        LastCheck[key] = detail;
        Write(stage, detail);
    });

    internal static void Sold(StoreClient __instance, GameItem __0, int __1) =>
        ItemEvent("sold", __0, $"client={__instance.identifier} price={__1}");

    internal static void ModLiquid(GameItem item, string liquid, int amount) =>
        ItemEvent("corp-add-liquid", item, $"liquid={liquid} amount_uL={amount}");
}
#endif
