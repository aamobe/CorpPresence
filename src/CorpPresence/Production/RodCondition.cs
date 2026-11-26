using System;

namespace CorpPresence;

internal enum RodProcess { Unknown = -1, Cast, Temper, Densify, Align, Harden }

[Flags]
internal enum RodTrait
{
    LoadBearing = 1, Fatigue = 2, Impact = 4, LowLoss = 8, AxialConductor = 16, HardTooth = 32, DimensionStable = 64
}

[Flags]
internal enum RodLabel
{
    HighHardness = 1, HighLoad = 2, HighWear = 4, HighConductivity = 8, HighAxialConductivity = 16,
    HighCorrosion = 32, LowPorosity = 64, OilPores = 128, Brittle = 256
}

internal sealed record RodJacket(RodMaterial Material, RodMaterial Assay, RodProcess Process)
{
    internal const string MassTag = "CORP_ROD_JACKET_MASS";
    internal const string AssayTag = "CORP_ROD_JACKET_ASSAY";
    internal const string ProcessTag = "CORP_ROD_JACKET_PROCESS";

    internal static bool HasTags(GameItem item) => !string.IsNullOrEmpty(item.GetTagReadonly(MassTag).GetString())
        || !string.IsNullOrEmpty(item.GetTagReadonly(AssayTag).GetString())
        || !string.IsNullOrEmpty(item.GetTagReadonly(ProcessTag).GetString());

    internal static RodJacket Read(GameItem item)
    {
        var mass = RodMaterial.ReadTag(item, MassTag);
        var assay = RodMaterial.ReadTag(item, AssayTag);
        var process = RodCondition.Decode(item.GetTagReadonly(ProcessTag).GetString());
        return mass == null || assay == null || process == RodProcess.Unknown ? null : new(mass, assay, process);
    }

    internal void Write(GameItem item)
    {
        Material.Write(item, MassTag);
        Assay.Write(item, AssayTag);
        if (Process != RodProcess.Cast)
            item.ModifyTag(ProcessTag, tag => tag.Enable().SetString(RodCondition.Code(Process)), skipValidate: true);
    }

    internal static void Wear(GameItem item, RodCondition stock, int halfPoints)
    {
        var parts = stock.Jacket.Material.Parts();
        int wear = Math.Min(halfPoints, (int)(stock.Jacket.Material.PowderPoints * 2));
        for (int step = 0; step < wear; step++)
        {
            int largest = 0;
            for (int i = 1; i < parts.Length; i++)
                if (parts[i] > parts[largest]) largest = i;
            parts[largest]--;
        }
        var remaining = new RodMaterial(parts[0], parts[1], parts[2], parts[3], parts[4]);
        stock.CoreMaterial.Add(remaining).Write(item);
        if (remaining.PowderPoints > 0)
        {
            remaining.Write(item, MassTag);
            return;
        }
        item.DisableTag(MassTag, skipValidate: true);
        item.DisableTag(AssayTag, skipValidate: true);
        item.DisableTag(ProcessTag, skipValidate: true);
    }
}

internal sealed record RodCondition(RodMaterial Material, RodProcess Process, RodProperties Core, RodProperties Surface,
    int? AxialConductivity, RodMaterial Assay)
{
    internal const string ProcessTag = "CORP_ROD_PROCESS";
    internal const string ScoredSleeveTag = "CORP_SLEEVE_SCORED";
    private const string TruedTag = "CORP_STOCK_TRUED";
    internal bool Scored { get; init; }
    internal bool Trued { get; init; }
    internal RodJacket Jacket { get; init; }
    internal RodMaterial CoreMaterial => Jacket == null ? Material : Material.TakeAway(Jacket.Material);

    internal static bool IsScoredSleeve(GameItem item) => item?.identifier == "corp_presence_" + RodMaterial.FeedSleeve
        && item.GetTagReadonly(ScoredSleeveTag).GetInt() == 1;

    internal static bool CanTrue(RodCondition stock) => stock is { Process: RodProcess.Temper, Jacket: null,
        Core: { Load: >= 60, Porosity: <= 3, Brittleness: <= 3 } };

    private static bool IsTruedPart(GameItem item) => item.identifier == "corp_presence_" + RodMaterial.ClassifierSupport;

    internal RodTrait Traits
    {
        get
        {
            RodTrait traits = 0;
            if (Core.Load >= 64 && Core.Brittleness <= 4) traits |= RodTrait.LoadBearing;
            if (Process == RodProcess.Temper)
            {
                if (Core.Wear >= 50 && Core.Brittleness <= 5) traits |= RodTrait.Fatigue;
                if (Core.Load >= 60 && Core.Brittleness <= 2) traits |= RodTrait.Impact;
            }
            if (Process == RodProcess.Densify && Core.Conductivity >= 60 && Core.Porosity <= 3) traits |= RodTrait.LowLoss;
            if (AxialConductivity >= 75 && Core.Brittleness <= 4) traits |= RodTrait.AxialConductor;
            if ((Jacket?.Process ?? Process) == RodProcess.Harden && Surface.Wear >= 70) traits |= RodTrait.HardTooth;
            if (Trued && CanTrue(this)) traits |= RodTrait.DimensionStable;
            return traits;
        }
    }

    internal RodLabel Labels(RodProperties part)
    {
        RodLabel labels = 0;
        if (part.Hardness >= 80) labels |= RodLabel.HighHardness;
        if (part.Load >= 64) labels |= RodLabel.HighLoad;
        if (part.Wear >= 65) labels |= RodLabel.HighWear;
        if (part.Conductivity >= 70) labels |= RodLabel.HighConductivity;
        if (AxialConductivity >= 70) labels |= RodLabel.HighAxialConductivity;
        if (part.Corrosion >= 45) labels |= RodLabel.HighCorrosion;
        if (part.Porosity <= 3) labels |= RodLabel.LowPorosity;
        if (part.Porosity is >= 5 and <= 9) labels |= RodLabel.OilPores;
        if (part.Brittleness >= 10) labels |= RodLabel.Brittle;
        return labels;
    }

    internal static RodProcess Decode(string code) => code switch
    {
        null or "" => RodProcess.Cast,
        "temper" => RodProcess.Temper,
        "densify" => RodProcess.Densify,
        "align" => RodProcess.Align,
        "harden" => RodProcess.Harden,
        _ => RodProcess.Unknown
    };

    internal static string Code(RodProcess process) => process switch
    {
        RodProcess.Temper => "temper",
        RodProcess.Densify => "densify",
        RodProcess.Align => "align",
        RodProcess.Harden => "harden",
        _ => throw new ArgumentOutOfRangeException(nameof(process))
    };

    internal static RodCondition Read(GameItem item)
    {
        var material = RodMaterial.Read(item);
        if (material == null) return null;
        bool cladRod = item.identifier == "corp_presence_" + RodMaterial.CladRod;
        var assay = cladRod || RodMaterial.IsFormedStock(item.identifier) ? RodMaterial.ReadAssay(item) : material;
        if (assay == null) return null;
        var process = Decode(item.GetTagReadonly(ProcessTag).GetString());
        RodCondition condition;
        if (cladRod || RodJacket.HasTags(item))
        {
            if (!cladRod && !RodMaterial.IsShortRod(item.identifier)
                && item.identifier is not ("corp_presence_" + RodMaterial.FeedShaft or "corp_presence_" + RodMaterial.WetContact
                    or "corp_presence_" + RodMaterial.ToothRoller))
                return null;
            var jacket = RodJacket.Read(item);
            var core = jacket == null ? null : material.TakeAway(jacket.Material);
            if (core == null || core.PowderPoints <= 0 || material.PowderPoints > PowderColor.Capacity) return null;
            condition = WithJacket(Evaluate(core, process, assay), jacket);
        }
        else condition = Evaluate(material, process, assay);
        if (IsTruedPart(item) && CanTrue(condition) && item.GetTagReadonly(TruedTag).GetInt() == 1)
            condition = condition with { Trued = true };
        return condition != null && IsScoredSleeve(item) ? condition with { Scored = true } : condition;
    }

    internal static RodCondition WithJacket(RodCondition core, RodJacket jacket)
    {
        if (core == null || core.Jacket != null || jacket == null) return null;
        var surface = Evaluate(jacket.Material, jacket.Process, jacket.Assay);
        return surface == null ? null : core with { Material = core.Material.Add(jacket.Material), Surface = surface.Surface, Jacket = jacket };
    }

    internal static RodCondition Evaluate(RodMaterial material, RodProcess process, RodMaterial assay = null)
    {
        assay ??= material;
        var cast = assay.CastProperties;
        switch (process)
        {
            case RodProcess.Cast: return new(material, process, cast, cast, null, assay);
            case RodProcess.Temper:
                var softened = cast with { Load = Math.Max(0, cast.Load - 4), Brittleness = Math.Max(0, cast.Brittleness - 5),
                    Hardness = Math.Clamp(assay.RawHardness - 6, 0, 100) };
                return new(material, process, softened, softened, null, assay);
            case RodProcess.Densify:
                var dense = cast with { Porosity = Math.Max(0, cast.Porosity - 4) };
                return new(material, process, dense, dense, null, assay);
            case RodProcess.Align when cast.Conductivity >= 60:
                var oriented = cast with { Brittleness = cast.Brittleness + 3 };
                return new(material, process, oriented, oriented, cast.Conductivity + 8, assay);
            case RodProcess.Harden:
                var skin = cast with { Wear = cast.Wear + 6, Brittleness = cast.Brittleness + 3,
                    Hardness = Math.Clamp(assay.RawHardness + 12, 0, 100) };
                return new(material, process, cast, skin, null, assay);
            default: return null;
        }
    }

    internal static RodCondition Preview(GameItem item, RodProcess process)
    {
        if (item?.unitCount != 1 || !GeneralHelper.IsItemOwned(item) || process <= RodProcess.Cast) return null;
        if (!RodMaterial.IsRod(item.identifier) && !RodMaterial.IsShortRod(item.identifier)) return null;
        var current = Read(item);
        return current?.Process == RodProcess.Cast && current.Jacket == null ? Evaluate(current.Material, process, current.Assay) : null;
    }

    internal void WriteFormed(GameItem item)
    {
        Material.Write(item);
        Assay.Write(item, RodMaterial.AssayTag);
        if (Process != RodProcess.Cast) WriteProcess(item);
        Jacket?.Write(item);
        if (Trued && IsTruedPart(item)) item.ModifyTag(TruedTag, tag => tag.Enable().SetInt(1), skipValidate: true);
    }

    internal void WriteProcess(GameItem rod) =>
        rod.ModifyTag(ProcessTag, tag => tag.Enable().SetString(Code(Process)), skipValidate: true);
}
