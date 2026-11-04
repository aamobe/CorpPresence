using System;

namespace CorpPresence;

internal static class OrderAmounts
{
	public static bool TryAdd(ref int total, int unitPrice, int quantity, int bundle, out int itemCount)
	{
		itemCount = 0;
		if (unitPrice <= 0 || quantity <= 0 || total < 0)
		{
			return false;
		}
		long num = (long)quantity * (long)Math.Max(1, bundle);
		long num2 = total + (long)unitPrice * (long)quantity;
		if (num > int.MaxValue || num2 > int.MaxValue)
		{
			return false;
		}
		itemCount = (int)num;
		total = (int)num2;
		return true;
	}
}
