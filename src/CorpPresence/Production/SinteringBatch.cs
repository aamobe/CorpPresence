using System;

namespace CorpPresence;

internal enum SinteringIngotGrade { None, High, VeryHigh, Perfect }

internal sealed record SinteringIngredient(PowderComponent Component, int Points, PowderGrade MinimumGrade = PowderGrade.Coarse);

internal sealed record SinteringBatch(int[][] Remaining, bool[] Used, int Metal, int Total, int[] Spent,
    int[] Cast = null, int Retained = 0)
{
    internal static SinteringIngotGrade GetIngotGrade(int metal, int total)
    {
        if (total <= 0 || metal * 100L < total * 81L) return SinteringIngotGrade.None;
        if (metal * 100L >= total * 99L) return SinteringIngotGrade.Perfect;
        return metal * 100L > total * 90L ? SinteringIngotGrade.VeryHigh : SinteringIngotGrade.High;
    }

    internal static SinteringBatch Plan(int[][] boxes, SinteringIngredient[] ingredients, Func<int[], int, int[]> split, bool exact = false)
    {
        var remaining = new int[boxes.Length][];
        var used = new bool[boxes.Length];
        var classifications = new PowderClassification[boxes.Length];
        for (int slot = 0; slot < boxes.Length; slot++)
        {
            remaining[slot] = (int[])boxes[slot].Clone();
            classifications[slot] = PowderClassification.FromParts(boxes[slot]);
        }

        int metal = 0;
        int total = 0;
        var spent = new int[5];
        foreach (var ingredient in ingredients)
        {
            int needed = ingredient.Points;
            int component = (int)ingredient.Component;
            for (int slot = 0; slot < remaining.Length && needed > 0; slot++)
            {
                var classification = classifications[slot];
                var box = remaining[slot];
                int available = box[component];
                if (classification.Branch != ingredient.Component || classification.Grade < ingredient.MinimumGrade || available == 0) continue;

                int target = Math.Min(needed, available);
                if (exact)
                {
                    box[component] -= target;
                    spent[component] += target;
                    needed -= target;
                    used[slot] = true;
                    total += target;
                    if (ingredient.Component == PowderComponent.Metal) metal += target;
                    continue;
                }
                int boxTotal = 0;
                foreach (int points in box) boxTotal += points;
                // Below this draw size, even rounding up cannot supply the requested component points.
                int amount = (target - 1) * boxTotal / available + 1;
                var taken = split(box, amount);
                while (taken[component] < target)
                {
                    amount++;
                    taken = split(box, amount);
                }

                for (int i = 0; i < box.Length; i++)
                {
                    box[i] -= taken[i];
                    spent[i] += taken[i];
                }
                used[slot] = true;
                needed -= taken[component];
                metal += taken[(int)PowderComponent.Metal];
                total += amount;
            }
            if (needed > 0) return null;
        }
        return new SinteringBatch(remaining, used, metal, total, spent);
    }
}
