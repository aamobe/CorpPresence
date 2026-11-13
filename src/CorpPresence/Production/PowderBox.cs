using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static class PowderBox
{
    internal const string Key = "powder_box";
    internal const string Id = "corp_presence_powder_box";
    private const string ContainerTag = "CORP_MINERAL_POWDER_TAG";
    private static readonly string[] Parts =
    {
        "CORP_MINERAL_PART_METAL", "CORP_MINERAL_PART_SILICA", "CORP_MINERAL_PART_RARE_METAL",
        "CORP_MINERAL_PART_CONDUCTIVE", "CORP_MINERAL_PART_RESIDUE"
    };
    private static readonly Sprite[] Layers = new Sprite[5];
    private static readonly List<ItemSpriteModifier> EmptyLayer = new(0);
    private sealed record BuiltIn(Func<int[]> Read, Action<int[]> Write);
    private static readonly ConditionalWeakTable<GameItem, BuiltIn> BuiltIns = new();

    internal static readonly Goods.Def Definition = new()
    {
        Name = "接粉盒", NameEn = "Powder Box", Value = PowderPricing.EmptyBoxValue, Company = "mineral",
        Art = "powder-box-body", Fallback = "ammo_box", W = 3, H = 3,
        Shape = "###|###|###", Types = new[] { "MATERIAL" },
        Shell = () => ItemDirectory.CreateEmptyItem(),
        Setup = Init
    };

    private static void Init(GameItem item)
    {
        item.EnableTag(ContainerTag);
        item.SetType("MATERIAL_TAG");
        item.onLoaded += RefreshValue;
        item.getItemSpriteModifierForItemFunc = Overlay;
        item.maySelectSlotItemFunc = (box, _, _) => CanPour(box);
        item.canSelectSlotItemFunc = item.maySelectSlotItemFunc;
        item.mayThisTargetItemFunc = MayPourInto;
        item.canThisTargetItemFunc = MayPourInto;
        item.onThisTargetItemFunc = (source, target) => HookGuard.Run("倒矿粉", () => Pour(source, target));
    }

    internal const int ComponentCount = 5;

    internal static long Read(GameItem item, Span<int> parts)
    {
        var stored = BuiltIns.TryGetValue(item, out var bin) ? bin.Read() : null;
        long total = 0;
        for (int i = 0; i < Parts.Length; i++)
        {
            parts[i] = stored == null ? Math.Max(0, item.GetTagReadonly(Parts[i]).GetInt()) : stored[i];
            total += parts[i];
        }
        return total;
    }

    internal static bool IsOwnedBox(GameItem item) =>
        IsBox(item) && !BuiltIns.TryGetValue(item, out _) && GeneralHelper.IsItemOwned(item);

    private static bool IsBox(GameItem item) => item?.identifier == Id && item.unitCount == 1 && item.IsTag(ContainerTag);

    internal static bool IsBoundBin(GameItem item) => BuiltIns.TryGetValue(item, out _);

    internal static void BindBin(GameItem box, Func<int[]> read, Action<int[]> write)
    {
        BuiltIns.Remove(box);
        BuiltIns.Add(box, new BuiltIn(read, write));
        box.mayInventoryRemoveItemFunc = (_, _) => false;
    }

    private static bool CanPour(GameItem item) => IsBox(item) && GeneralHelper.IsItemOwned(item)
        && Read(item, stackalloc int[ComponentCount]) > 0;

    private static bool MayPourInto(GameItem source, GameItem target) => source != target && CanPour(source)
        && IsOwnedBox(target) && Read(target, stackalloc int[ComponentCount]) < PowderColor.Capacity;

    private static void Pour(GameItem source, GameItem target)
    {
        if (!SaveSlot.CanWrite("CorpPresence") || !MayPourInto(source, target)) return;
        var from = new int[ComponentCount];
        var to = new int[ComponentCount];
        Read(source, from);
        int free = PowderColor.Capacity - (int)Read(target, to);
        var moved = WaterHelper.SplitProportionally(from, free);
        for (int i = 0; i < moved.Length; i++)
        {
            from[i] -= moved[i];
            to[i] += moved[i];
        }
        if (BuiltIns.TryGetValue(source, out var bin)) bin.Write(from);
        else WriteComposition(source, from);
        WriteComposition(target, to);
        Goods.Redraw(source);
    }

    internal static PowderClassification Classify(GameItem item)
    {
        if (item?.identifier != Id || !item.IsTag(ContainerTag)) return default;
        Span<int> parts = stackalloc int[ComponentCount];
        Read(item, parts);
        return PowderClassification.FromParts(parts);
    }

    internal static void AppendClassification(RichTextBuilder builder, GameItem item)
    {
        if (!IsBox(item)) return;
        int capacity = BuiltIns.TryGetValue(item, out _) ? PowderSieveRules.BinCapacity : PowderColor.Capacity;
        long total = Read(item, stackalloc int[ComponentCount]);
        builder.AddLine(L.T($"粉量 {total}/{capacity} 点", $"Powder {total}/{capacity} points"), ignoreEmpty: true);
        var powder = Classify(item);
        if (powder.Grade == PowderGrade.Empty) return;

        string purity = (powder.PurityBasisPoints / 100m).ToString("0.##", CultureInfo.CurrentCulture);
        string label = powder.Grade switch
        {
            PowderGrade.Mixed => L.F("production.powder.mixed", "杂粉 · 最高成分 {0}%", "Mixed powder · largest component {0}%", purity),
            PowderGrade.Coarse => L.F("production.powder.coarse", "{0} · 粗筛 {1}%", "{0} · coarse {1}%", ComponentName(powder.Branch.Value), purity),
            _ => L.F("production.powder.pure", "{0} · 纯粉 {1}%", "{0} · pure {1}%", ComponentName(powder.Branch.Value), purity)
        };
        builder.AddLine(label, ignoreEmpty: true);
    }

    internal static string ComponentName(PowderComponent component) => component switch
    {
        PowderComponent.Metal => L.K("production.powder.metal", "金属粉", "Metal powder"),
        PowderComponent.Silica => L.K("production.powder.silica", "硅质粉", "Silica powder"),
        PowderComponent.RareMetal => L.K("production.powder.rareMetal", "稀有金属粉", "Rare metal powder"),
        PowderComponent.Conductive => L.K("production.powder.conductive", "导电粉", "Conductive powder"),
        _ => L.K("production.powder.residue", "渣粉", "Residue powder")
    };

    internal static void WriteComposition(GameItem item, ReadOnlySpan<int> parts)
    {
        if (!IsOwnedBox(item)) throw new ArgumentException("Expected an owned powder box.", nameof(item));
        if (parts.Length != ComponentCount) throw new ArgumentException("Expected five powder components.", nameof(parts));
        long total = 0;
        foreach (int amount in parts)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(parts));
            total += amount;
        }
        if (total > PowderColor.Capacity) throw new ArgumentOutOfRangeException(nameof(parts));


        for (int i = 0; i < Parts.Length; i++)
        {
            int amount = parts[i];
            item.ModifyTag(Parts[i], tag => tag.Enable().SetInt(amount), skipValidate: true);
        }
        item.unitValue = PowderPricing.BoxValue(parts);
        item.ValidateShapeState();
        Goods.Redraw(item);
    }

    private static void RefreshValue(GameItem item)
    {
        Span<int> parts = stackalloc int[ComponentCount];
        Read(item, parts);
        item.unitValue = PowderPricing.BoxValue(parts);
    }

    private static List<ItemSpriteModifier> Overlay(GameItem item)
    {
        Span<int> parts = stackalloc int[ComponentCount];
        int capacity = BuiltIns.TryGetValue(item, out _) ? PowderSieveRules.BinCapacity : PowderColor.Capacity;
        long total = Read(item, parts);
        int level = PowderColor.Level((int)((Math.Min(capacity, total) * PowderColor.Capacity + capacity - 1) / capacity));
        if (level == 0) return EmptyLayer;
        int index = level - 1;
        if (Layers[index] == null)
        {
            var texture = Tex.Art("mineral", level == 5 ? "powder-box-fill" : "powder-box-fill-" + level * 20);
            if (texture == null) return EmptyLayer;
            Layers[index] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
            Layers[index].hideFlags = HideFlags.HideAndDontSave;
        }
        int rgb = PowderColor.Mix(parts);
        var tint = new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        return new List<ItemSpriteModifier> { new(Layers[index], Vector2.zero, color: tint) };
    }

    internal static void Release()
    {
        for (int i = 0; i < Layers.Length; i++)
        {
            if (Layers[i] != null) UnityEngine.Object.Destroy(Layers[i]);
            Layers[i] = null;
        }
    }
}
