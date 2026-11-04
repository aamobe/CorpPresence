using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class SilverBloodFlow
{
    // The caller still needs its original stock list for final death checks.
    // Ordinary tanks reuse their existing list; only exempt tanks allocate.
    public static List<T> WaterChecks<T>(List<T> original, Predicate<T> exempt, Action<T> onExempt, bool deferDeath)
    {
        if (original == null) return null;
        List<T> filtered = null;
        for (int i = 0; i < original.Count; i++)
        {
            T item = original[i];
            if (exempt(item))
            {
                if (filtered == null) filtered = original.GetRange(0, i);
                onExempt(item);
            }
            else if (filtered != null) filtered.Add(item);
        }
        if (filtered != null && deferDeath)
        {
            original.Clear();
            original.AddRange(filtered);
            return original;
        }
        return filtered ?? original;
    }

    // Commit each success immediately; never erase the unfulfilled remainder.
    public static void DeliverPending(int pending, Func<bool> deliver, Action commit)
    {
        for (int i = 0; i < pending; i++)
        {
            if (!deliver()) break;
            commit();
        }
    }
}
