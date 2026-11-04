namespace CorpPresence;

internal static class StorePlacement
{
	public static bool Place(GameItem item)
	{
		if (item == null)
		{
			return false;
		}
		EmporiumEntry instance = EmporiumEntry.Instance;
		if (instance == null || (instance.frontInvinvElement == null && instance.invElement == null))
		{
			return false;
		}
		GeneralHelper.SetItemOwned(item);
		if (instance.frontInvinvElement != null && GraphUtils.CanAccept(instance.frontInvinvElement, item))
		{
			GraphUtils.TryAcceptAll(instance.frontInvinvElement, item);
		}
		if (Attached(item))
		{
			return true;
		}
		if (Destroyed(item))
		{
			return false;
		}
		GraphUtils.EmporiumTryAdd(instance.invElement ?? instance.frontInvinvElement, item);
		return Attached(item);
	}

	public static bool Destroyed(GameItem item)
	{
		if (item is GameItemElement gameItemElement)
		{
			return gameItemElement.IsDestroyed();
		}
		return false;
	}

	public static bool Attached(GameItem item)
	{
		if (item != null && !Destroyed(item))
		{
			return item.parentInventory != null;
		}
		return false;
	}
}
