using System;
using System.Collections.Generic;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static class RodAppearance
{
    private static readonly Dictionary<RodVisual, (Sprite Sprite, Texture2D Texture)> Variants = new();

    internal static Sprite SpriteOverride(GameItem item)
    {
        bool shortStock = RodMaterial.IsShortRod(item?.identifier);
        if ((!shortStock && !RodMaterial.IsRod(item?.identifier)) || RodCondition.Read(item) is not { } condition) return null;
        var visual = RodVisual.From(condition, shortStock);
        if (Variants.TryGetValue(visual, out var cached)) return cached.Sprite;
        var template = Tex.Art("mineral", visual.Art);
        var original = Goods.FindSprite(item.spritePath);
        int height = shortStock ? 16 : 32;
        if (template == null || template.width != 16 || template.height != height || original == null) return null;

        var pixels = template.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            var pixel = pixels[i];
            // The Aseprite source reserves the violet outline. Tint only neutral metal.
            if (pixel.a == 0 || pixel.r != pixel.g || pixel.g != pixel.b) continue;
            int tint = visual.Tint;
            if (visual.JacketTint >= 0)
            {
                int x = i % 16, y = height - 1 - i / 16;
                // Both native templates expose the central end face within these cells.
                bool exposedCore = x is >= 6 and <= 8 && y is >= 2 and <= 4;
                if (!exposedCore) tint = visual.JacketTint;
            }
            int red = tint >> 16, green = (tint >> 8) & 255, blue = tint & 255;
            byte Shade(int channel) => (byte)Math.Min(246, (channel * pixel.r + 90) / 180);
            pixels[i] = new(Shade(red), Shade(green), Shade(blue), pixel.a);
        }
        var texture = new Texture2D(16, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Sprite sprite = null;
        try
        {
            texture.SetPixels32(pixels);
            texture.Apply();
            var pivot = new Vector2(original.pivot.x / original.rect.width, original.pivot.y / original.rect.height);
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, original.pixelsPerUnit);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Variants.Add(visual, (sprite, texture));
            return sprite;
        }
        catch
        {
            UnityEngine.Object.Destroy(sprite);
            UnityEngine.Object.Destroy(texture);
            throw;
        }
    }

    internal static void Release()
    {
        foreach (var variant in Variants.Values)
        {
            UnityEngine.Object.Destroy(variant.Sprite);
            UnityEngine.Object.Destroy(variant.Texture);
        }
        Variants.Clear();
    }
}
