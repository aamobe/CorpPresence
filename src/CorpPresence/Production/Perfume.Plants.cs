using System;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static partial class Perfume
{
    private static PlantDefinition _flower;
    private static Sprite _flowerSprite;
    internal const string AromaTrait = "production_corp_flower_mash";
    private static PlantTrait _aroma;
    private const string LegacyGeneticsVersion = "CORP_OILFLOWER_GENETICS_V1_TAG";
    private const string GeneticsVersion = "CORP_OILFLOWER_GENETICS_V2_TAG";

    private static void RegisterFlower()
    {
        if (PlantRegistry.Contains(SeedId))
        {
            if (!ReferenceEquals(PlantRegistry.Get(SeedId), _flower))
                throw new InvalidOperationException("油花植物标识已占用");
            return;
        }
        RegisterAroma();
        _flower = new PlantDefinition(SeedId, "Oilflower", FlowerId, 42, 4)
            .Nutriment(300, 75, 60).Water(100).Contaminant(1.1)
            .Refruit(21).ReturnsSeeds().Innate("infertile", 3)
            .Innate(AromaTrait).Innate("complex_genome").Innate("frail")
            .OnHarvest(ctx => {
                ctx.OutputById(SeedId);
                double health = ctx.health <= 30 ? 0.5 : ctx.health <= 70 ? 0.75 : 1;
                ctx.OutputById(FlowerId, Math.Max(1, (int)Math.Floor(ctx.harvestCount * health)));
            }); // Native infertility controls returned seeds; perennial must be inserted.
        PlantRegistry.Add(_flower);
    }

    internal static void InitFlowerSeed(GameItem seed)
    {
        HydroponicHelper.InitSeedItem(seed, SeedId);
        seed.EnableTag(GeneticsVersion);
    }

    internal static void RepairFlowerGenetics(GameItem seed)
    {
        if (seed?.identifier != SeedId || seed.IsTag(GeneticsVersion)) return;
        // Decode replaces factory state. Older defaults migrate once; an edited
        // V1 seed with infertility removed or set to another rank stays edited.
        bool legacyDefault = !seed.IsTag(LegacyGeneticsVersion);
        if (legacyDefault && PlantGeneticsHelper.GetTraitRank(seed, AromaTrait) <= 0)
            PlantGeneticsHelper.SetTraitRank(seed, AromaTrait, 1);
        int infertility = PlantGeneticsHelper.GetTraitRank(seed, "infertile");
        if (PlantGeneticsHelper.GetTraitRank(seed, "perennial") <= 0 &&
            (infertility == 10 || legacyDefault && infertility == 0))
            PlantGeneticsHelper.SetTraitRank(seed, "infertile", 3);
        seed.EnableTag(GeneticsVersion);
    }

    private static void UnregisterFlower()
    {
        if (_flower != null && ReferenceEquals(PlantRegistry.Get(SeedId), _flower)) PlantRegistry.Remove(SeedId);
        _flower = null;
        if (_aroma != null && ReferenceEquals(PlantTraitRegistry.Get(AromaTrait), _aroma))
            PlantTraitRegistry.Remove(AromaTrait);
        _aroma = null;
        if (_flowerSprite != null) UnityEngine.Object.Destroy(_flowerSprite);
        _flowerSprite = null;
    }

    private static void RegisterAroma()
    {
        if (PlantTraitRegistry.Contains(AromaTrait))
        {
            if (!ReferenceEquals(PlantTraitRegistry.Get(AromaTrait), _aroma))
                throw new InvalidOperationException("芳香物质基因标识已占用");
            return;
        }
        _aroma = new PlantTrait(AromaTrait, "Aromatic Compounds", 4)
            .Named(() => L.T("芳香物质", "Aromatic Compounds"),
                () => L.T("收获物中含有可提取基底油的芳香花浆。", "Produces aromatic mash that can be extracted into base oil."))
            .Infuses(MashLiquid).Unlimited().AppliesToSeedProduce()
            .Describe(rank => L.T($"产生 {16 * PlantGeneticsHelper.RankScale(rank):0.#} mL 含油花浆。",
                $"Produces {16 * PlantGeneticsHelper.RankScale(rank):0.#} mL of oilflower mash."))
            .OnHarvestItem(ctx => {
                if (!ctx.item.IsTag("CHEM_ITEM_TAG")) ChemicalHelper.InitChemItem(ctx.item, isBlendable: true);
                // Native potency hooks precede this mod's registered production trait.
                // Scale only the newly added ingredient; never rescale other native ingredients.
                float multiplier = 1;
                foreach (var id in new[] { "potent", "weak" })
                    multiplier *= 1 + PlantGeneticsHelper.RankedPercent(id == "potent" ? 25 : -25,
                        PlantGeneticsHelper.GetTraitRank(ctx.seed, id)) / 100;
                ChemicalHelper.AddChemContentUL(ctx.item, MashLiquid, Mathf.RoundToInt(16000 * ctx.Scale * multiplier));
            });
        PlantTraitRegistry.Add(_aroma);
    }

    internal static Sprite FlowerSprite()
    {
        if (_flowerSprite != null) return _flowerSprite;
        var texture = Tex.Art("solana", "oilflower_p");
        var reference = RenderHandler.TryLoadFromAtlas("Items/items_hydroponic_plant", "coffee_bean_p");
        if (texture == null) return reference;
        Vector2 pivot = reference == null ? new Vector2(0.5f, 0.5f) : reference.pivot / reference.rect.size;
        _flowerSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, reference?.pixelsPerUnit ?? 100);
        _flowerSprite.name = FlowerId + "_p";
        _flowerSprite.hideFlags = HideFlags.HideAndDontSave;
        return _flowerSprite;
    }
}
