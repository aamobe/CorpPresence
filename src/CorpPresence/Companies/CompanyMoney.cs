namespace CorpPresence;

internal static class CompanyMoney
{
    // Keep the native debit. Optional register audio must not abort bookkeeping.
    public static void Pay(PlayerStore store, int amount)
    {
        store.PayMoney(amount, playRegisterSound: false);
        try { EmporiumAudioPlayer.Instance?.PlayRegister(); } catch { }
    }

    public static void DeliverCash(PlayerStore store, Order order)
    {
        if (order.Cash <= 0) return;
        store.ModCash(order.Cash, playSound: false);
        order.Cash = 0;
        try { NegociationUIManager.Instance?.audioSourceCashRegister?.Play(); } catch { }
    }
}
