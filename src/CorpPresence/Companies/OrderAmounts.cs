using System;
namespace CorpPresence;

internal static class OrderAmounts
{
    public static bool TryAdd(ref int total, int unitPrice, int quantity, int bundle, out int itemCount)
    {
        itemCount = 0;
        if (unitPrice <= 0 || quantity <= 0 || total < 0) return false;
        long items = (long)quantity * Math.Max(1, bundle);
        long amount = (long)total + (long)unitPrice * quantity;
        if (items > int.MaxValue || amount > int.MaxValue) return false;
        itemCount = (int)items;
        total = (int)amount;
        return true;
    }
}
