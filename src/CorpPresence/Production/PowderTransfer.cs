using System;

namespace CorpPresence;

internal sealed record PowderTransfer(int[] SourceAfter, int[] TargetAfter, int[] Moved)
{
    internal bool Qualified(SinteringIngredient goal)
    {
        var powder = PowderClassification.FromParts(TargetAfter);
        return powder.Branch == goal.Component && powder.Grade >= goal.MinimumGrade;
    }

    internal int Missing(SinteringIngredient goal) => Math.Max(0, goal.Points - TargetAfter[(int)goal.Component]);

    private static int Total(int[] parts, int capacity)
    {
        if (parts == null || parts.Length != 5) return -1;
        long total = 0;
        foreach (int part in parts)
        {
            if (part < 0) return -1;
            total += part;
        }
        return total > capacity ? -1 : (int)total;
    }

    internal static int Limit(int[] source, int[] target, int sourceCapacity)
    {
        int from = Total(source, sourceCapacity);
        int to = Total(target, PowderColor.Capacity);
        return from < 0 || to < 0 ? 0 : Math.Min(from, PowderColor.Capacity - to);
    }

    internal static PowderTransfer Plan(int[] source, int[] target, int sourceCapacity, int amount,
        Func<int[], int, int[]> split)
    {
        if (amount <= 0 || amount > Limit(source, target, sourceCapacity)) return null;
        var moved = split(source, amount);
        var sourceAfter = new int[source.Length];
        var targetAfter = new int[target.Length];
        for (int i = 0; i < moved.Length; i++)
        {
            sourceAfter[i] = source[i] - moved[i];
            targetAfter[i] = target[i] + moved[i];
        }
        return new PowderTransfer(sourceAfter, targetAfter, moved);
    }

    // Integer apportionment can cross a purity boundary for one draw size and fall below it at the next.
    internal static int ToGoal(int[] source, int[] target, int sourceCapacity, SinteringIngredient goal,
        Func<int[], int, int[]> split)
    {
        int limit = Limit(source, target, sourceCapacity);
        if (limit == 0) return 0;
        var current = new PowderTransfer(source, target, null);
        if (current.Qualified(goal) && current.Missing(goal) == 0) return 0;
        for (int i = Math.Max(1, current.Missing(goal)); i <= limit; i++)
        {
            var plan = Plan(source, target, sourceCapacity, i, split);
            if (plan.Qualified(goal) && plan.Missing(goal) == 0) return i;
        }
        return -1;
    }
}
