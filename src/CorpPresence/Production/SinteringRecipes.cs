using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class SinteringRecipes
{
    internal sealed record Recipe(string Id, string Product, string NameKey, bool GradeIngot, SinteringIngredient[] Ingredients);

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
    }

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

    internal static int IndexOf(string id)
    {
        int index = Entries.FindIndex(recipe => recipe.Id == id);
        return Math.Max(0, index);
    }
}
