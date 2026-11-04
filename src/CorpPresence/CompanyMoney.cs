namespace CorpPresence;

internal static class CompanyMoney
{
	public static void Pay(PlayerStore store, int amount)
	{
		store.PayMoney(amount);
		try
		{
			EmporiumAudioPlayer.Instance?.PlayRegister();
		}
		catch
		{
		}
	}

	public static void DeliverCash(PlayerStore store, Order order)
	{
		if (order.Cash <= 0)
		{
			return;
		}
		store.ModCash(order.Cash, playSound: false);
		order.Cash = 0;
		try
		{
			NegociationUIManager.Instance?.audioSourceCashRegister?.Play();
		}
		catch
		{
		}
	}
}
