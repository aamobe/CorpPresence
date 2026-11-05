using System;
namespace CorpPresence;

internal static class ItemHitArea
{
    // UV is in the item's unrotated image space; GridShape rows start at the top.
    public static bool Cell(float u, float v, int width, int height, out int x, out int y)
    {
        x = y = 0;
        if (width <= 0 || height <= 0 || !(u >= 0f && u < 1f && v >= 0f && v < 1f)) return false;
        x = Math.Min(width - 1, (int)(u * width));
        y = Math.Min(height - 1, (int)((1f - v) * height));
        return true;
    }
}
