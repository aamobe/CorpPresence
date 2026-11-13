using System;

namespace CorpPresence;

internal static partial class SilverBlood
{
    internal static void AppendBloodProgress(RichTextBuilder builder, GameItem item)
    {
        if (!IsCreature(item) || AquacultureHelper.IsDead(item)) return;

        int capacity = BloodCap(item);
        if (capacity <= 0) return;

        int blood = Math.Clamp(Blood(item), 0, capacity);
        var color = RenderHandler.ColorPalette.LightCyan;
        builder.AddProgressBar((float)blood / capacity, 10, color, RenderHandler.ColorPalette.VeryDarkCyan, null, " ");
        builder.AddLine(L.F("production.silverBlood.tooltip.bloodReserve", "银血储量：{0}/{1} 毫升", "Blood reserve: {0}/{1} mL", blood, capacity), ignoreEmpty: true, color);
    }
}
