using System;
using System.Globalization;

namespace CorpPresence;

internal readonly record struct RodProperties(int Load, int Wear, int Conductivity, int Corrosion,
    int Brittleness, int Porosity, int Hardness);

// Each component is stored in half powder points, so the two rods can share an odd input equally.
internal sealed record RodMaterial(int Metal, int Silica, int Rare, int Conductive, int Residue)
{
    internal const string Tag = "CORP_ROD_MATERIAL";
    internal const string AssayTag = "CORP_STOCK_ASSAY";
    internal const string CladRod = "clad_alloy_rod";
    internal const string WetContact = "wet_pump_contact", ToothRoller = "crusher_tooth_roller";
    internal const string CastingDie = "dense_trial_die", ClassifierSupport = "classifier_support";
    internal const string GrowthGuide = "mineral_growth_guide";
    internal const string ShortRod = "short_alloy_rod", Strip = "rolled_alloy_strip", WireBlank = "drawn_wire_blank",
        PorousBlank = "porous_sleeve_blank", Offcut = IndustrialRecipes.Offcut;
    internal const string FeedShaft = "feed_shaft", FeedSleeve = "feed_sleeve", IndustrialCoil = "industrial_coil",
        AxialLead = "axial_lead", ReturnGuide = "sieve_return_guide", FilterBasket = "pump_filter_basket",
        PressureShell = "pump_pressure_shell";
    private const string Version = "1";
    private long Mass => (long)Metal + Silica + Rare + Conductive + Residue;
    internal decimal PowderPoints => Mass / 2m;
    internal int RawHardness => 20 + Score(100L * (2L * Silica + Rare)) - Score(50L * Residue);

    internal RodProperties CastProperties => new(
        Score(80L * (Metal + (long)Rare)),
        Score(200L * (Silica + (long)Rare)),
        Score(100L * (3L * Conductive + Rare)),
        Score(100L * (3L * Rare + Silica - 2L * Residue)),
        Math.Max(0, Score(100L * Silica) - 10),
        Score(300L * Residue),
        Math.Clamp(RawHardness, 0, 100));

    private int Score(long numerator) => (int)Math.Floor((decimal)numerator / Mass);

    internal static bool IsRod(string identifier) => identifier is
        "corp_presence_" + SinteringRecipes.StandardRod or "corp_presence_" + SinteringRecipes.WearRod
        or "corp_presence_" + SinteringRecipes.ConductiveRod or "corp_presence_" + SinteringRecipes.TrialRod
        or "corp_presence_" + CladRod;
    internal static bool IsShortRod(string identifier) => identifier == "corp_presence_" + ShortRod;
    internal static bool IsFormedStock(string identifier) => identifier is
        "corp_presence_" + ShortRod or "corp_presence_" + Strip or "corp_presence_" + WireBlank
        or "corp_presence_" + PorousBlank or "corp_presence_" + Offcut
        or "corp_presence_" + FeedShaft or "corp_presence_" + FeedSleeve or "corp_presence_" + IndustrialCoil
        or "corp_presence_" + AxialLead or "corp_presence_" + ReturnGuide or "corp_presence_" + FilterBasket
        or "corp_presence_" + PressureShell or "corp_presence_" + WetContact or "corp_presence_" + ToothRoller
        or "corp_presence_" + CastingDie or "corp_presence_" + ClassifierSupport or "corp_presence_" + GrowthGuide;

    // The following masses describe the original untagged production, even if a later recipe changes.
    private static RodMaterial Legacy(string identifier) => identifier switch
    {
        "corp_presence_" + SinteringRecipes.StandardRod => new(60, 12, 0, 0, 0),
        "corp_presence_" + SinteringRecipes.WearRod => new(45, 0, 5, 0, 0),
        "corp_presence_" + SinteringRecipes.ConductiveRod => new(0, 8, 4, 24, 0),
        _ => null
    };

    internal static RodMaterial ForOutput(int[] spent, int count)
    {
        if (spent?.Length != 5 || count <= 0) return null;
        var share = new int[spent.Length];
        for (int i = 0; i < share.Length; i++)
        {
            long halfPoints = 2L * spent[i];
            if (halfPoints < 0 || halfPoints % count != 0 || halfPoints / count > int.MaxValue) return null;
            share[i] = (int)(halfPoints / count);
        }
        return FromShare(share);
    }

    internal static RodMaterial Read(GameItem item)
    {
        if (!IsRod(item?.identifier) && !IsFormedStock(item?.identifier)) return null;
        string raw = item.GetTagReadonly(Tag).GetString();
        if (string.IsNullOrEmpty(raw)) return Legacy(item.identifier);
        return Parse(raw);
    }

    internal static RodMaterial ReadAssay(GameItem item) =>
        ReadTag(item, AssayTag);

    internal static RodMaterial ReadTag(GameItem item, string key) =>
        Parse(item.GetTagReadonly(key).GetString());

    internal int[] Parts() => new[] { Metal, Silica, Rare, Conductive, Residue };

    internal RodMaterial Add(RodMaterial other) =>
        new(Metal + other.Metal, Silica + other.Silica, Rare + other.Rare, Conductive + other.Conductive, Residue + other.Residue);

    internal RodMaterial TakeAway(RodMaterial other)
    {
        if (other.Metal > Metal || other.Silica > Silica || other.Rare > Rare || other.Conductive > Conductive || other.Residue > Residue)
            return null;
        return new(Metal - other.Metal, Silica - other.Silica, Rare - other.Rare, Conductive - other.Conductive, Residue - other.Residue);
    }

    private static RodMaterial Parse(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        var fields = raw.Split('|');
        if (fields.Length != 6 || fields[0] != Version) return null;
        var share = new int[5];
        for (int i = 0; i < share.Length; i++)
            if (!int.TryParse(fields[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out share[i])) return null;
        return FromShare(share);
    }

    private static RodMaterial FromShare(int[] share)
    {
        var material = new RodMaterial(share[0], share[1], share[2], share[3], share[4]);
        return material.Mass > 0 ? material : null;
    }

    internal void Write(GameItem item, string key = Tag)
    {
        string record = FormattableString.Invariant($"{Version}|{Metal}|{Silica}|{Rare}|{Conductive}|{Residue}");
        item.ModifyTag(key, tag => tag.Enable().SetString(record), skipValidate: true);
    }
}
