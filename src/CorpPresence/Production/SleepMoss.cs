using System;
using System.Collections.Generic;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static class SleepMoss
{
    internal const string SeedId = "corp_presence_sleep_moss_seed";
    internal const string BlockId = "corp_presence_sleep_moss_block";
    private const int Maturity = 18;
    private const int Yield = 3;
    private static PlantDefinition _plant;
    private static readonly Sprite[] Stages = new Sprite[3];
    private static readonly string[] StageArt = {
        "sleep-moss-hydroponic-inoculated",
        "sleep-moss-hydroponic-spreading",
        "sleep-moss-hydroponic-harvestable"
    };

    internal static readonly DefinitionSet<Goods.Def> Defs = new("SleepMoss")
    {
        ["sleep_moss_seed"] = new Goods.Def
        {
            Name = "眠苔接种包", NameEn = "Sleep Moss Starter", Value = 20,
            Company = "cartel", Art = "sleep-moss-seed", Fallback = "hydroreed_seed",
            W = 1, H = 1, Types = new[] { "SEED" },
            Shell = () => ItemDirectory.CreateEmptyItem(),
            Setup = seed => HydroponicHelper.InitSeedItem(seed, SeedId),
            Desc = "放入水培槽。成熟后收获三块眠苔菌块并返还一份接种包。",
            DescEn = "Place in a hydroponic planter. Harvest three moss blocks and one starter at maturity."
        },
        ["sleep_moss_block"] = new Goods.Def
        {
            Name = "眠苔菌块", NameEn = "Sleep Moss Block", Value = 10,
            Company = "cartel", Art = "sleep-moss-block", Fallback = "hydroreed_filter",
            W = 1, H = 1, Types = new[] { "CHEMICAL_SUPPLIES" }, Shell = () => ItemDirectory.CreateEmptyItem(),
            Desc = "水培眠苔收获的菌块。", DescEn = "A block harvested from hydroponic sleep moss."
        }
    };

    internal static void Register()
    {
        if (PlantRegistry.Contains(SeedId))
            throw new InvalidOperationException("眠苔植物标识已占用");
        _plant = new PlantDefinition(SeedId, "Sleep Moss", BlockId, Maturity, Yield)
            .Nutriment(100, 100, 25).Water(20).Contaminant(0).ReturnsSeeds().DurableProduce()
            .OnGrowTick(Absorb)
            .OnHarvest(ctx =>
            {
                ctx.OutputById(BlockId, Yield);
                ctx.OutputById(SeedId);
            });
        PlantRegistry.Add(_plant);
    }

    internal static void Unregister()
    {
        if (_plant != null && ReferenceEquals(PlantRegistry.Get(SeedId), _plant))
            PlantRegistry.Remove(SeedId);
        _plant = null;
        for (int i = 0; i < Stages.Length; i++)
        {
            if (Stages[i] != null) UnityEngine.Object.Destroy(Stages[i]);
            Stages[i] = null;
        }
    }

    private static void Absorb(GameItem seed, GameItem machine)
    {
        var container = MachineHydroponic.GetWaterSlotItem(machine);
        if (container == null) return;
        int water = container.GetTagReadonly(Liquid.GetCurrentPartFromId("water")).GetInt();
        int removed = AbsorbPart(container, water, "chemical_contaminant", 4);
        removed += AbsorbPart(container, water, "heavy_metal", 6);
        if (removed > 0) WaterFeatureHelper.UpdateWaterFeature(container, WaterFeatureHelper.GetWaterFeature(container)?.IsExposed() ?? false);
    }

    private static int AbsorbPart(GameItem container, int water, string id, int floor)
    {
        var key = Liquid.GetCurrentPartFromId(id);
        int current = container.GetTagReadonly(key).GetInt();
        int removed = SleepMossRules.Absorb(water, current, floor);
        if (removed > 0) container.ModifyTag(key, tag => tag.SetInt(current - removed));
        return removed;
    }

    internal static void AddStageOverlay(GameItem __result)
    {
        if (__result == null) return;
        var original = __result.getItemSpriteModifierForItemFunc;
        __result.getItemSpriteModifierForItemFunc = machine =>
        {
            if (_plant == null) return original?.Invoke(machine);
            var seed = MachineHydroponic.GetInputSlot(machine).childItem;
            if (seed?.identifier != SeedId) return original?.Invoke(machine);

            int progress = MachineProgressHelper.GetMachineState(machine) == "STATE_WORKING"
                ? seed.GetTagReadonly("GROWING_PROGRESS").GetInt()
                : 0;
            int target = seed.GetTagReadonly("REQUIRED_PROGRESS").GetInt();
            Sprite sprite = StageSprite(SleepMossRules.Stage(progress, target > 0 ? target : Maturity));
            return sprite == null ? original?.Invoke(machine) : new List<ItemSpriteModifier>
            {
                new(sprite, Vector2.zero, 0, isFlipped: false, 0, haveMask: false, Color.white)
            };
        };
    }

    private static Sprite StageSprite(int stage)
    {
        if (Stages[stage] != null) return Stages[stage];
        var reference = RenderHandler.TryLoadFromAtlas(MachineHydroponic.ATLAS, "hydroreed_p");
        var texture = Tex.Art("cartel", StageArt[stage]);
        if (texture == null) return reference;
        var pivot = reference == null ? new Vector2(0.5f, 0.5f) : reference.pivot / reference.rect.size;
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, reference?.pixelsPerUnit ?? 100);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return Stages[stage] = sprite;
    }
}
