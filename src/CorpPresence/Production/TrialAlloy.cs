using System;
using System.Globalization;

namespace CorpPresence;

internal static class TrialAlloy
{
    internal const string BlendTag = "CORP_SINTERING_TRIAL_BLEND";
    internal const int BatchPoints = 100;
    internal const int MinMetal = 55;
    internal const int MaxResidue = 10;
    internal const int MaxOverdraw = 8;
    private static readonly int[] ReturnOrder = { 2, 3, 0, 1, 4 };

    internal static int[] DefaultBlend() => new[] { 64, 18, 8, 8, 2 };

    internal static bool Valid(int[] blend)
    {
        if (blend?.Length != 5 || blend[0] < MinMetal || blend[4] > MaxResidue) return false;
        int sum = 0;
        foreach (int amount in blend)
        {
            if (amount < 0 || amount > BatchPoints) return false;
            sum += amount;
        }
        return sum == BatchPoints;
    }

    internal static int[] Read(GameItem machine)
    {
        string saved = machine.GetTagReadonly(BlendTag).GetString();
        if (string.IsNullOrEmpty(saved)) return DefaultBlend();
        var fields = saved.Split('|');
        if (fields.Length != 5) return null;
        var blend = new int[fields.Length];
        for (int i = 0; i < blend.Length; i++)
            if (!int.TryParse(fields[i], NumberStyles.None, CultureInfo.InvariantCulture, out blend[i])) return null;
        return Valid(blend) ? blend : null;
    }

    internal static void Save(GameItem machine, int[] blend)
    {
        if (!Valid(blend)) throw new ArgumentException("Invalid trial blend.", nameof(blend));
        string saved = FormattableString.Invariant($"{blend[0]}|{blend[1]}|{blend[2]}|{blend[3]}|{blend[4]}");
        machine.ModifyTag(BlendTag, tag => tag.Enable().SetString(saved), skipValidate: true);
    }

    internal static SinteringBatch Plan(int[][] boxes, int[] blend, out int[] missing, bool allowTrim = false, int trimRetention = 0)
    {
        missing = null;
        if (boxes == null || !Valid(blend)) return null;
        missing = (int[])blend.Clone();
        foreach (var box in boxes)
            for (int part = 0; part < missing.Length; part++)
                missing[part] = Math.Max(0, missing[part] - box[part]);
        if (Array.Exists(missing, amount => amount != 0)) return null;

        // Precompute proportional draws for up to three physical boxes and one casting.
        int maxExtra = allowTrim ? MaxOverdraw : 0;
        var choices = new int[boxes.Length][][];
        var capacity = new int[boxes.Length + 1];
        for (int slot = boxes.Length - 1; slot >= 0; slot--)
        {
            int total = 0;
            foreach (int amount in boxes[slot]) total += amount;
            int limit = Math.Min(BatchPoints + maxExtra, total);
            choices[slot] = new int[limit + 1][];
            choices[slot][0] = new int[blend.Length];
            for (int amount = 1; amount <= limit; amount++)
                choices[slot][amount] = WaterHelper.SplitProportionally(boxes[slot], amount);
            capacity[slot] = capacity[slot + 1] + limit;
        }
        var required = (int[])blend.Clone();
        var selected = new int[boxes.Length][];
        int extra = 0;
        while (!Match(0, BatchPoints + extra, extra))
            if (++extra > maxExtra) return null;

        int retained = 0;
        foreach (int part in ReturnOrder)
            for (int slot = boxes.Length - 1; slot >= 0 && retained < trimRetention && required[part] < 0; slot--)
            {
                int returned = Math.Min(Math.Min(-required[part], trimRetention - retained), selected[slot][part]);
                selected[slot][part] -= returned;
                required[part] += returned;
                retained += returned;
            }

        var remaining = new int[boxes.Length][];
        var used = new bool[boxes.Length];
        var spent = new int[blend.Length];
        for (int slot = 0; slot < boxes.Length; slot++)
        {
            var box = remaining[slot] = (int[])boxes[slot].Clone();
            for (int part = 0; part < box.Length; part++)
            {
                int amount = selected[slot][part];
                if (amount == 0) continue;
                box[part] -= amount;
                spent[part] += amount;
                used[slot] = true;
            }
        }
        return new SinteringBatch(remaining, used, spent[0], BatchPoints + extra - retained, spent,
            extra > 0 ? (int[])blend.Clone() : null, retained);

        bool Match(int slot, int points, int extraLeft)
        {
            if (slot == boxes.Length) return points == 0 && extraLeft == 0;
            int min = Math.Max(0, points - capacity[slot + 1]);
            for (int amount = Math.Min(points, choices[slot].Length - 1); amount >= min; amount--)
            {
                var draw = choices[slot][amount];
                int excess = 0;
                for (int part = 0; part < draw.Length && excess <= extraLeft; part++)
                    excess += Math.Max(0, draw[part] - Math.Max(0, required[part]));
                if (excess > extraLeft) continue;
                for (int part = 0; part < draw.Length; part++) required[part] -= draw[part];
                if (Match(slot + 1, points - amount, extraLeft - excess))
                {
                    selected[slot] = draw;
                    return true;
                }
                for (int part = 0; part < draw.Length; part++) required[part] += draw[part];
            }
            return false;
        }
    }
}
