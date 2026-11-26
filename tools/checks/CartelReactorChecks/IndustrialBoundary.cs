namespace CorpPresence;

internal sealed class SlotMarker { }
internal static class SaveManager { internal static bool IsDecoding { get; set; } }
internal static class RNG { internal static int GetRandomIntExculsive(int min, int max) => min; }
internal static partial class IngotPurityHelper
{
    internal const string INGOT_PURITY_HIGH = "high", INGOT_PURITY_VERY_HIGH = "very_high", INGOT_PURITY_PERFECT = "perfect";
    internal static void SetPurity(GameItem item, string grade) => item.ModifyTag("purity", tag => tag.SetString(grade));
}
internal static partial class MachineHelper
{
    internal static PixelWindow CreateMachineryWindow(bool value, params Panel[] panels) =>
        CreateMachineryWindow(value, Array.Empty<int>(), panels);
}
internal static partial class MachineAppearance
{
    internal static void Rearm(GameItem machine) { }
    internal static void WatchCycle(GameItem m, Func<bool> ready) { }
    internal static void WatchCycle(GameItem m, GameInventory output, Func<string> ready, Func<int> count=null,
        bool separateUnit=false, Func<bool?> inPlaceReady=null) { }
}
internal static partial class WaterHelper
{
    internal static int[] SplitProportionally(int[] parts, int points)
    {
        long sum = parts.Sum(part => (long)part);
        var taken = parts.Select(part => (int)((long)part * points / sum)).ToArray();
        foreach (int i in Enumerable.Range(0, parts.Length).OrderByDescending(i => (long)parts[i] * points % sum).Take(points - taken.Sum()))
            taken[i]++;
        return taken;
    }
}
