using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class SinteringRecipes
{
    internal sealed record Recipe(string Id, string Product, string NameKey, bool GradeIngot, SinteringIngredient[] Ingredients,
        bool Exact = false, string Solid = null, int OutputCount = 1);

    internal const string StandardRod = "standard_alloy_rod";
    internal const string WearRod = "wear_alloy_rod";
    internal const string ConductiveRod = "conductive_alloy_rod";
    internal const string TrialRod = "trial_alloy_rod";

    private static readonly List<Recipe> Entries = new();
    internal static IReadOnlyList<Recipe> All { get; } = Entries.AsReadOnly();

    static SinteringRecipes()
    {
        Register("metal_ingot", "metal_ingot", "item_metal_ingot_name", true, new SinteringIngredient(PowderComponent.Metal, 120));
        Register("silicon_plate", "corp_presence_silicon_plate", null, false,
            new SinteringIngredient(PowderComponent.Silica, 30));
        Register("wire", "wire", "item_wire_name", false,
            new SinteringIngredient(PowderComponent.Conductive, 12));
        Register("electronic_substrate", "corp_presence_electronic_substrate", null, false,
            new SinteringIngredient(PowderComponent.RareMetal, 4));
        Register("wine_bottle", "wine_bottle", "item_wine_bottle_name", false,
            new SinteringIngredient(PowderComponent.Silica, 20));
        Register("hearth_tile", "corp_presence_hearth_tile", null, false,
            new SinteringIngredient(PowderComponent.Residue, 40));
        Entries.Add(new Recipe(IndustrialRecipes.Billet, "corp_presence_" + IndustrialRecipes.Billet, null, false,
            new[] { new SinteringIngredient(PowderComponent.Metal, IndustrialRecipes.AlloyMetal),
                new SinteringIngredient(PowderComponent.RareMetal, IndustrialRecipes.AlloyRare) }, Exact: true));
        Entries.Add(new Recipe(IndustrialRecipes.Blank, "corp_presence_" + IndustrialRecipes.Blank, null, false,
            Array.Empty<SinteringIngredient>(), Solid: "coarse_silica_grit"));
        RegisterRod(StandardRod, new(PowderComponent.Metal, 60), new(PowderComponent.Silica, 12));
        RegisterRod(WearRod, new(PowderComponent.Metal, 45), new(PowderComponent.RareMetal, 5));
        RegisterRod(ConductiveRod, new(PowderComponent.Conductive, 24), new(PowderComponent.Silica, 8),
            new(PowderComponent.RareMetal, 4));
        RegisterRod(TrialRod);
    }

    private static void RegisterRod(string id, params SinteringIngredient[] ingredients) =>
        Entries.Add(new Recipe(id, "corp_presence_" + id, null, false, ingredients, Exact: true, OutputCount: 2));

    internal static void Register(string id, string product, string nameKey, bool gradeIngot, params SinteringIngredient[] ingredients)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(product))
            throw new ArgumentException("Recipe ID and product ID are required.");
        if (Entries.Exists(recipe => recipe.Id == id)) throw new ArgumentException("Duplicate sintering recipe ID.", nameof(id));
        if (ingredients == null || ingredients.Length == 0) throw new ArgumentException("A recipe needs powder ingredients.", nameof(ingredients));
        var components = new HashSet<PowderComponent>();
        foreach (var ingredient in ingredients)
        {
            if (ingredient == null || ingredient.Points <= 0 || !Enum.IsDefined(typeof(PowderComponent), ingredient.Component)
                || (ingredient.MinimumGrade != PowderGrade.Coarse && ingredient.MinimumGrade != PowderGrade.Pure)
                || !components.Add(ingredient.Component))
                throw new ArgumentException("Use one positive requirement per powder component.", nameof(ingredients));
        }
        if (gradeIngot && (product != "metal_ingot" || ingredients.Length != 1 || !components.Contains(PowderComponent.Metal)))
            throw new ArgumentException("Ingot grading requires metal powder and the native ingot product.");
        Entries.Add(new Recipe(id, product, nameKey, gradeIngot, (SinteringIngredient[])ingredients.Clone()));
    }

    internal sealed record PowderTarget(string Id, Recipe Recipe, SinteringIngredient Ingredient);

    // Keep the original recipe ID for its first powder, including old workshop saves.
    // A secondary target uses the component's immutable powder-box index.
    private static readonly List<PowderTarget> PowderTargets = new();
    private static int indexedRecipes;
    internal static PowderTarget SieveTarget(string id, Func<Recipe,bool> unlocked, bool next = false)
    {
        while (indexedRecipes < Entries.Count)
        {
            var r = Entries[indexedRecipes++];
            for (int i = 0; i < r.Ingredients.Length; i++)
            {
                var ingredient = r.Ingredients[i];
                PowderTargets.Add(new(i == 0 ? r.Id : r.Id + "/" + (int)ingredient.Component, r, ingredient));
            }
        }
        int current = PowderTargets.FindIndex(t => t.Id == id && unlocked(t.Recipe));
        if (current < 0) current = PowderTargets.FindIndex(t => unlocked(t.Recipe));
        if (next)
        {
            do { current = (current + 1) % PowderTargets.Count; } while (!unlocked(PowderTargets[current].Recipe));
        }
        return PowderTargets[current]; // The native metal ingot recipe is always available.
    }

    internal static int IndexOf(string id)
    {
        int index = Entries.FindIndex(recipe => recipe.Id == id);
        return Math.Max(0, index);
    }
}
