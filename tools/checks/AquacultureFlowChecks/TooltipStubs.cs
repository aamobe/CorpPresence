namespace CorpPresence;

internal static partial class SilverBlood
{
    private static bool IsCreature(GameItem item) => item?.IsCreature == true;
    private static int BloodCap(GameItem item) => item.Capacity;
    private static int Blood(GameItem item) => item.Blood;
}

internal sealed class GameItem
{
    internal bool IsCreature = true;
    internal bool IsDead;
    internal int Blood;
    internal int Capacity;
}

internal static class AquacultureHelper
{
    internal static bool IsDead(GameItem item) => item.IsDead;
}

internal static class RenderHandler
{
    internal enum ColorPalette { LightCyan, VeryDarkCyan }
}

internal sealed class RichTextBuilder
{
    internal readonly List<float> Progress = new();
    internal readonly List<string> Labels = new();

    internal void AddProgressBar(float ratio, int length, RenderHandler.ColorPalette color,
        RenderHandler.ColorPalette background, char[] style, string end) => Progress.Add(ratio);

    internal void AddLine(string label, bool ignoreEmpty, RenderHandler.ColorPalette color) => Labels.Add(label);
}

internal static class L
{
    internal static string F(string key, string zh, string en, params object[] args) => string.Format(en, args);
}
