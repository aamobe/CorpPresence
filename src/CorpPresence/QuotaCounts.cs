using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class QuotaCounts
{
	public static string Key(string id, int windowDays, int day, string period)
	{
		if (windowDays <= 0)
		{
			return id + "@" + period;
		}
		return id + "@d" + day;
	}

	public static bool TryAdd(Dictionary<string, int> records, string key, int amount, out int next)
	{
		next = 0;
		if (records == null || string.IsNullOrEmpty(key) || amount <= 0)
		{
			return false;
		}
		int num = (records.TryGetValue(key, out var value) ? value : 0);
		long num2 = (long)num + (long)amount;
		if (num < 0 || num2 > int.MaxValue)
		{
			return false;
		}
		next = (int)num2;
		return true;
	}

	public static int Used(Dictionary<string, int> records, string id, int windowDays, int day, string period)
	{
		if (records == null || string.IsNullOrEmpty(id))
		{
			return int.MaxValue;
		}
		if (windowDays <= 0)
		{
			int num = (records.TryGetValue(Key(id, windowDays, day, period), out var value) ? value : 0);
			if (num >= 0)
			{
				return num;
			}
			return int.MaxValue;
		}
		long num2 = 0L;
		string text = id + "@d";
		if (windowDays <= records.Count)
		{
			for (int i = 0; i < windowDays; i++)
			{
				long num3 = (long)day - (long)i;
				if (num3 < int.MinValue)
				{
					break;
				}
				if (records.TryGetValue(text + (int)num3, out var value2))
				{
					if (value2 < 0)
					{
						return int.MaxValue;
					}
					num2 += value2;
					if (num2 >= int.MaxValue)
					{
						return int.MaxValue;
					}
				}
			}
		}
		else
		{
			long num4 = (long)day - (long)windowDays + 1;
			foreach (KeyValuePair<string, int> record in records)
			{
				if (record.Key.StartsWith(text, StringComparison.Ordinal) && int.TryParse(record.Key.Substring(text.Length), out var result) && result >= num4 && result <= day)
				{
					if (record.Value < 0)
					{
						return int.MaxValue;
					}
					num2 += record.Value;
					if (num2 >= int.MaxValue)
					{
						return int.MaxValue;
					}
				}
			}
		}
		return (int)num2;
	}
}
