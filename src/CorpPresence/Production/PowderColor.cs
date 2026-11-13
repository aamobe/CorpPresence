using System;

namespace CorpPresence;

internal static class PowderColor
{
    internal const int Capacity = 500;
    internal static readonly int[] Palette = { 0xB8C1CA, 0xF2F0E8, 0xBAADD0, 0xC78F6B, 0x8B7B69 };

    internal static int Level(int amount) => (Math.Clamp(amount, 0, Capacity) + 99) / 100;

    internal static int Mix(ReadOnlySpan<int> parts)
    {
        long total = 0, red = 0, green = 0, blue = 0;
        for (int i = 0; i < Palette.Length; i++)
        {
            long amount = Math.Max(0, parts[i]);
            int color = Palette[i];
            total += amount;
            red += amount * (color >> 16);
            green += amount * ((color >> 8) & 255);
            blue += amount * (color & 255);
        }
        if (total == 0) return 0xFFFFFF;
        int Channel(long sum) => (int)((sum + total / 2) / total);
        return Channel(red) << 16 | Channel(green) << 8 | Channel(blue);
    }
}
