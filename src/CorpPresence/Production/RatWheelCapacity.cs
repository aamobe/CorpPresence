using System;

namespace CorpPresence;

// 跑轮发电机笼格的容量恢复规则。宽高各取“已保存的值、旧版尺寸标签换算值、当前格子尺寸、已占用范围”里的最大值，只增不减。
internal static class RatWheelCapacity
{
    internal static (int Width, int Height) Restore(int width, int height, int legacySize, int gridWidth, int gridHeight, int occupiedWidth, int occupiedHeight)
    {
        int legacyWidth = legacySize >= 16 ? 20 : legacySize >= 12 ? 15 : 10;
        return (
            Width: Math.Max(Math.Max(10, width), Math.Max(legacyWidth, Math.Max(gridWidth, occupiedWidth))),
            Height: Math.Max(Math.Max(3, height), Math.Max(legacySize, Math.Max(gridHeight, occupiedHeight))));
    }

    internal static bool CanUpgrade(int height) => height < 8;
}
