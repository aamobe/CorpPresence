using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static partial class Perfume
{
    private static readonly Dictionary<string, Liquid> OwnedLiquids = new();

    internal static void InstallResources()
    {
        if (!GameResources.Ready) throw new InvalidOperationException("香水资源尚未就绪");
        AddLiquid(MashLiquid, L.K("production.perfume.chemistry.installResources.oilflowerMash", "含油花浆", "Oilflower Mash"), "9d7d98", 200);
        AddLiquid(OilLiquid, L.K("production.perfume.chemistry.installResources.perfumeBaseOil", "香水基底油", "Perfume Base Oil"), "c6a04d", 5);
        RegisterFlower();
    }

    private static void AddLiquid(string id, string name, string color, int size)
    {
        if (Liquid.LiquidDict.TryGetValue(id, out var existing))
        {
            if (!OwnedLiquids.TryGetValue(id, out var owned) || !ReferenceEquals(existing, owned))
                throw new InvalidOperationException("香水液体标识已占用：" + id);
            return;
        }
        var liquid = new Liquid {
            identifier = id, displayName = name, part = "PART_" + id.ToUpperInvariant(),
            currentPart = "CURRENT_PART_" + id.ToUpperInvariant(), particleSize = size,
            // A water purifier must not provide a shortcut for separating the valuable oil.
            canBeRemovedByPurifier = true, baseMaxPurityFromMachine = 12, basePurifierRemoveAmount = 4000,
            color = color, colorMultiplier = 2
        };
        OwnedLiquids.Add(id, liquid);
        Liquid.Liquids.Add(liquid);
        Liquid.LiquidDict.Add(id, liquid);
    }

    internal static void UninstallResources()
    {
        UnregisterFlower();
        foreach (var pair in OwnedLiquids)
        {
            if (Liquid.LiquidDict.TryGetValue(pair.Key, out var current) && ReferenceEquals(current, pair.Value))
                Liquid.LiquidDict.Remove(pair.Key);
            Liquid.Liquids.Remove(pair.Value);
        }
        OwnedLiquids.Clear();
    }

    private static void InitFlower(GameItem item)
    {
        ChemicalHelper.InitChemItem(item, isBlendable: true);
        ChemicalHelper.SetChemContentML(item, OilLiquid, 2);
        ChemicalHelper.SetChemContentML(item, "fat", 5);
        ChemicalHelper.SetChemContentML(item, "protein", 5);
        ChemicalHelper.SetChemContentML(item, "physical_contaminant", 5);
        ChemicalHelper.SetChemContentML(item, "water", 3);
        PlantGeneticsHelper.ApplyFactoryGenetics(item, SeedId);
    }

    private static Goods.Def LegacyOilBottle()
    {
        var definition = Item("旧版基底油液样瓶", "Legacy Base Oil Sample Bottle", 5, LegacyOilBottleKey, 1, 2,
            "旧测试版留下的液体容器，保留原有内容。放入基底油提取器，每10mL基底油成分可转成1份材料；空瓶不再产出。",
            "A liquid container from the earlier test build; contents are preserved. Put it in the extractor to turn each 10 mL of base oil into 1 material. Empty bottles are no longer produced.", null, "CHEMICAL_SUPPLIES");
        definition.Shell = BuildLegacyOilBottle;
        return definition;
    }

    private static GameItem BuildLegacyOilBottle()
    {
        var item = ItemDirectory.CreateEmptyItem();
        // InitLiquidContainerItem immediately refreshes the native bottle sprite,
        // whose dispatch calls identifier.Equals before Goods.Build assigns the ID.
        item.identifier = "corp_presence_" + LegacyOilBottleKey;
        WaterHelper.InitLiquidContainerItem(item, 100, 5, isFilterContainer: false, isGraduated: true);
        AudioHelper.InitGlass(item);
        return item; // Filling belongs to production, never shell construction or load.
    }

    private static int Parts(GameItem item, string id) => item?.GetTagReadonly(Liquid.GetCurrentPartFromId(id)).GetInt() ?? 0;
    private static void SetParts(GameItem item, string id, int amount) =>
        item.ModifyTag(Liquid.GetCurrentPartFromId(id), s => s.Enable().SetInt(Math.Max(0, amount)), skipValidate: true);

    // Same solvent threshold, 15%-per-tick throughput and solvent-ratio penalty as coffee.
    // Only the target yield differs: 16 mL mash -> at most 8 mL oil + 8 mL plant matter.
    internal static void AfterTemperatureReaction(GameItem container)
    {
        if (!GameResources.Ready || container == null) return;
        int mash = Parts(container, MashLiquid);
        if (mash <= 0 || container.GetTagReadonly("CONTAINER_TEMP_TAG").GetDouble() < 50) return;
        int alcohol = Parts(container, "alcohol");
        if (alcohol <= 0) return;
        // Whole flowers contain an even microlitre amount. Process even aliquots so
        // repeated integer rounding cannot turn 3 ideal flowers into less than 30 mL.
        int processed = Math.Min(mash, Math.Max(2, (int)Math.Round(mash * 0.15 / 2) * 2));
        double solvent = Math.Min(1, alcohol / (5.0 * processed));
        int oil = Math.Min(processed / 2, (int)Math.Round(processed * 0.5 * solvent));
        SetParts(container, MashLiquid, mash - processed);
        SetParts(container, OilLiquid, checked(Parts(container, OilLiquid) + oil));
        SetParts(container, "plant_matter", checked(Parts(container, "plant_matter") + processed - oil));
        WaterFeatureHelper.OnLiquidChanged(container, WaterFeatureHelper.GetWaterFeature(container).IsExposed());
    }

    internal static string OilProblem(GameItem container, int neededUl)
    {
        if (container == null || !container.IsTag("LIQUID_CONTAINER_TAG")) return L.K("production.perfume.chemistry.oilProblem.loadBaseOilContainer", "请放入基底油容器。", "Load a base oil container.");
        return Parts(container, OilLiquid) < neededUl
            ? L.K("production.perfume.chemistry.oilProblem.leastMLActualBaseOil", $"至少需要{neededUl / 1000.0:0.##}mL实际基底油成分；不检查纯度。", (FormattableString)$"At least {neededUl / 1000.0:0.##} mL of actual base oil is needed; purity is not checked.")
            : null;
    }

    // This is a component separator, deliberately not a homogeneous liquid transfer.
    // Every other CURRENT_PART tag must remain unchanged, including solvents and dirt.
    private static void ExtractOilComponent(GameItem container, int amount)
    {
        SetParts(container, OilLiquid, Parts(container, OilLiquid) - amount);
        WaterFeatureHelper.OnLiquidChanged(container, WaterFeatureHelper.GetWaterFeature(container).IsExposed());
    }
}
