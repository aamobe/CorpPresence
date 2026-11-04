using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class QuotaCounts
{
    public static string Key(string id, int windowDays, int day, string period) => windowDays > 0 ? id + "@d" + day : id + "@" + period;

    public static bool TryAdd(Dictionary<string, int> records, string key, int amount, out int next)
    {
        next = 0;
        if (records == null || string.IsNullOrEmpty(key) || amount <= 0) return false;
        int previous = records.TryGetValue(key, out int value) ? value : 0;
        long sum = (long)previous + amount;
        if (previous < 0 || sum > int.MaxValue) return false;
        next = (int)sum;
        return true;
    }

    public static int Used(Dictionary<string, int> records, string id, int windowDays, int day, string period)
    {
        if (records == null || string.IsNullOrEmpty(id)) return int.MaxValue;
        if (windowDays <= 0)
        {
            int value = records.TryGetValue(Key(id, windowDays, day, period), out int found) ? found : 0;
            return value < 0 ? int.MaxValue : value;
        }
        long total = 0;
        string prefix = id + "@d";
        if (windowDays <= records.Count)
        {
            for (int i = 0; i < windowDays; i++)
            {
                long at = (long)day - i;
                if (at < int.MinValue) break;
                if (!records.TryGetValue(prefix + (int)at, out int value)) continue;
                if (value < 0) return int.MaxValue;
                total += value;
                if (total >= int.MaxValue) return int.MaxValue;
            }
        }
        else
        {
            // Bound malformed huge windows by existing data, not billions of dates.
            long first = (long)day - windowDays + 1;
            foreach (var record in records)
            {
                if (!record.Key.StartsWith(prefix, StringComparison.Ordinal) || !int.TryParse(record.Key.Substring(prefix.Length), out int at) || at < first || at > day) continue;
                if (record.Value < 0) return int.MaxValue;
                total += record.Value;
                if (total >= int.MaxValue) return int.MaxValue;
            }
        }
        return (int)total;
    }
}
