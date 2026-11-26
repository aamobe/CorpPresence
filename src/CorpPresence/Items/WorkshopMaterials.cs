using System.Collections.Generic;

namespace CorpPresence;

internal static class WorkshopMaterials
{
    internal const string Electronics = "salvage_electronics";
    internal const string Lining = "spent_lining";
    internal const string Tile = "hearth_tile";

    internal static readonly Dictionary<string, Goods.Def> Definitions = new()
    {
        [Electronics] = Material("电子残料", "Electronic Salvage", 15, "salvage-electronics", "common_electronic",
            "阿岑拆下的旧电路。每件能破出金属 15、硅质 25、稀有 8、导电 22、渣料 30 点（基础值）。",
            "Discarded circuits. Base yield per piece: 15 metal, 25 silica, 8 rare metal, 22 conductive and 30 residue."),
        [Lining] = Material("旧炉衬碎块", "Spent Furnace Lining", 12, "spent-furnace-lining", "scrap_metal",
            "每件基础产出金属 5、硅质 10、渣料 85 点。破碎机开启保渣后可把渣粉留给烧结站。",
            "Base yield per piece: 5 metal, 10 silica and 85 residue. Use the crusher’s residue setting to keep slag for sintering."),
        [Tile] = Material("炉床垫砖", "Hearth Tile", 18, "hearth-tile", "steel_sheet",
            "用含渣量至少 81% 的粉烧结，耗 40 点渣料。下层互助页收一块付 24 元。",
            "Sinter from powder containing at least 81% residue. Each tile needs 40 points of residue. Cen pays 24 for one tile.")
    };

    private static Goods.Def Material(string name, string english, long value, string art, string fallback,
        string description, string englishDescription) => new()
    {
        Name = name, NameEn = english, Value = value, Company = "mineral", Art = art, Fallback = fallback,
        W = 2, H = 2, Types = new[] { "MATERIAL" }, Desc = description, DescEn = englishDescription,
        Shell = () => ItemDirectory.CreateEmptyItem()
    };
}
