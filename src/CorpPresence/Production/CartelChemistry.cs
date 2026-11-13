using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class CartelChemistry
{
    private const string Moss = "corp_cartel_moss";
    private const string Crystal = "corp_cartel_crystal";
    private const string Salt = "corp_cartel_salt";
    private const string IceActive = "corp_cartel_ice_active";
    private static readonly List<Liquid> Liquids = new();
    private static readonly List<ChemFinisherRecipe> Recipes = new();

    internal static void Register()
    {
        AddLiquid(Moss, L.T("眠苔胶成分", "Moss gel fraction"), "648957");
        AddLiquid(Crystal, L.T("光盐析晶成分", "Light salt fraction"), "90bac4");
        AddLiquid(Salt, L.T("光盐芯成分", "Light salt core fraction"), "d2c89c");

        AddLiquid(IceActive, L.T("冰棱活性物", "Ice Shard active compound"), "a2d9e1");

        AddRecipe(CartelReactor.Tablets, Moss, 5000);
        AddRecipe(CartelReactor.Tins, Crystal, 5000);
        AddRecipe(CartelReactor.Ice, IceActive, 15000);
    }

    private static void AddLiquid(string id, string name, string color)
    {
        if (Liquid.LiquidDict.ContainsKey(id)) throw new InvalidOperationException("卡尔特成分标识已占用：" + id);
        var liquid = new Liquid
        {
            identifier = id, displayName = name, part = "PART_" + id.ToUpperInvariant(),
            currentPart = "CURRENT_PART_" + id.ToUpperInvariant(), particleSize = 5,
            color = color, colorMultiplier = 2
        };
        Liquids.Add(liquid);
        Liquid.Liquids.Add(liquid);
        Liquid.LiquidDict.Add(id, liquid);
    }

    private static void AddRecipe(string key, string ingredient, int volume)
    {
        string id = "corp_presence_" + key;
        if (ChemFinisherRecipe.GetRecipeById(id) != null) throw new InvalidOperationException("卡尔特配方标识已占用：" + id);
        var recipe = new ChemFinisherRecipe
        {
            identifier = id, displayName = CartelReactor.Defs[key].LName,
            requiredVolume = volume, isRequireDry = true, activeIngredientId = ingredient
        };
        Recipes.Add(recipe);
        ChemFinisherRecipe.chemFinisherRecipes.Add(recipe);
    }

    internal static void Unregister()
    {
        foreach (var recipe in Recipes) ChemFinisherRecipe.chemFinisherRecipes.Remove(recipe);
        Recipes.Clear();
        foreach (var liquid in Liquids)
        {
            if (Liquid.LiquidDict.TryGetValue(liquid.identifier, out var current) && ReferenceEquals(current, liquid))
                Liquid.LiquidDict.Remove(liquid.identifier);
            Liquid.Liquids.Remove(liquid);
        }
        Liquids.Clear();
    }

    internal static void Initialize(GameItem item, string art)
    {
        string component = art switch
        {
            "sleep-moss-gel" => Moss,
            "light-salt-precipitate" => Crystal,
            "light-salt-core" => Salt,
            _ => null
        };
        if (component != null)
        {
            ChemicalHelper.InitChemItem(item, isBlendable: true);
            ChemicalHelper.SetChemContentUL(item, component, 8000);
            ChemicalHelper.SetChemContentUL(item, "physical_contaminant", 1000);
            return;
        }
        string active = art switch
        {
            "zol-tablets" => Moss,
            "ice-shard-pouch" => IceActive,
            "nicotine-tin" => Crystal,
            _ => null
        };
        if (active == null) return;
        ChemicalHelper.InitChemItem(item);
        item.EnableTag("CHEMICAL_PRODUCT_TAG");
        item.ModifyTag("ACTIVATE_INGREDIENT_TAG", tag => tag.Enable().SetString(active));
    }

    internal static void CheckOutputSpace(GameItem machine, ref bool __result)
    {
        if (!__result) return;
        var recipe = ChemFinisherRecipe.GetRecipeById(machine.GetTagReadonly("SELECTED_CHEM_ID_TAG").GetString());
        if (!Recipes.Contains(recipe)) return;

        __result = SaveSlot.CanWrite("CorpPresence")
            && MachineHelper.GetBodyStorage(machine, 2) is GameGridInventory output
            && output.HasFreeCell();
    }

    internal static void CheckIngredients(ChemFinisherRecipe __instance, GameItem container, ref bool __result)
    {
        if (!__result || !Recipes.Contains(__instance)) return;

        int[] portion = WaterHelper.SplitProportionally(WaterHelper.GetLiquidParts(container), __instance.requiredVolume);
        int Part(string id)
        {
            int index = Liquid.Liquids.FindIndex(liquid => liquid.identifier == id);
            return index < 0 ? 0 : portion[index];
        }

        int minimumPurity = __instance.activeIngredientId == IceActive ? 80 : 60;
        __result = (long)Part(__instance.activeIngredientId) * 100 >= (long)__instance.requiredVolume * minimumPurity;
    }

    internal static void ReactIce(GameItem container)
    {
        if (Liquids.Count == 0 || container == null || !container.IsTag("LIQUID_CONTAINER_TAG")
            || container.GetTagReadonly("CONTAINER_TEMP_TAG").GetDouble() < 50
            || !SaveSlot.CanWrite("CorpPresence")) return;

        int Read(string id) => container.GetTagReadonly(Liquid.GetCurrentPartFromId(id)).GetInt();
        void Write(string id, int amount) => container.ModifyTag(
            Liquid.GetCurrentPartFromId(id), tag => tag.Enable().SetInt(amount), skipValidate: true);

        int moss = Read(Moss);
        int crystal = Read(Crystal);
        int salt = Read(Salt);
        int reacted = Math.Min(moss, Math.Min(crystal, salt));
        if (reacted <= 0) return;

        long consumed = 3L * reacted;
        long product = consumed * 95 / 100;
        long active = Read(IceActive) + product;
        long waste = Read("physical_contaminant") + consumed - product;
        if (active > int.MaxValue || waste > int.MaxValue) return;

        Write(Moss, moss - reacted);
        Write(Crystal, crystal - reacted);
        Write(Salt, salt - reacted);
        Write(IceActive, (int)active);
        Write("physical_contaminant", (int)waste);
        WaterFeatureHelper.OnLiquidChanged(container, WaterFeatureHelper.GetWaterFeature(container)?.IsExposed() ?? false);
    }
}