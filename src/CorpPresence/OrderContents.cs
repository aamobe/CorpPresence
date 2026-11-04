using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class OrderContents
{
	public static void ExpandModBoxes(Order order, Func<List<string>> roll)
	{
		if (!order.Items.Contains("@modbox"))
		{
			return;
		}
		List<string> list = new List<string>();
		foreach (string item in order.Items)
		{
			if (item != "@modbox")
			{
				list.Add(item);
				continue;
			}
			List<string> list2 = roll();
			if (list2 == null || list2.Count == 0 || list2.Exists(string.IsNullOrEmpty))
			{
				list.Add(item);
			}
			else
			{
				list.AddRange(list2);
			}
		}
		order.Items = list;
	}
}
