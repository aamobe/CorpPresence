using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class SilverBloodFlow
{
	public static List<T> WaterChecks<T>(List<T> original, Predicate<T> exempt, Action<T> onExempt, bool deferDeath)
	{
		if (original == null)
		{
			return null;
		}
		List<T> list = null;
		for (int i = 0; i < original.Count; i++)
		{
			T val = original[i];
			if (exempt(val))
			{
				if (list == null)
				{
					list = original.GetRange(0, i);
				}
				onExempt(val);
			}
			else
			{
				list?.Add(val);
			}
		}
		if ((list != null) & deferDeath)
		{
			original.Clear();
			original.AddRange(list);
			return original;
		}
		return list ?? original;
	}

	public static void DeliverPending(int pending, Func<bool> deliver, Action commit)
	{
		for (int i = 0; i < pending; i++)
		{
			if (!deliver())
			{
				break;
			}
			commit();
		}
	}
}
