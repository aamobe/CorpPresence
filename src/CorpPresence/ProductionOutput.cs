using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class ProductionOutput
{
	private static readonly Func<GameItem, GameInventory, bool> AllowProducedItem = (GameItem item, GameInventory inventory) => true;

	internal static bool TryAdd(GameInventory inventory, GameItem item)
	{
		if (inventory == null || item == null)
		{
			return false;
		}
		Func<GameItem, GameInventory, bool> mayInventoryAddItemFunc = inventory.mayInventoryAddItemFunc;
		inventory.mayInventoryAddItemFunc = AllowProducedItem;
		try
		{
			return Transfer(inventory, item);
		}
		catch (Exception ex)
		{
			ModLog.Warn("成品放置失败，交回生产线回收：" + ex.Message);
			return false;
		}
		finally
		{
			inventory.mayInventoryAddItemFunc = mayInventoryAddItemFunc;
		}
	}

	internal static bool TryAddBatch(GameInventory inventory, List<GameItem> items)
	{
		if (inventory == null || items == null)
		{
			return false;
		}
		Func<GameItem, GameInventory, bool> mayInventoryAddItemFunc = inventory.mayInventoryAddItemFunc;
		inventory.mayInventoryAddItemFunc = AllowProducedItem;
		try
		{
			for (int i = 0; i < items.Count; i++)
			{
				if (!Transfer(inventory, items[i]))
				{
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			ModLog.Warn("成品批次放置失败，交回生产线回收：" + ex.Message);
			return false;
		}
		finally
		{
			inventory.mayInventoryAddItemFunc = mayInventoryAddItemFunc;
		}
	}

	private static bool Transfer(GameInventory inventory, GameItem item)
	{
		if (item == null || !GraphUtils.CanAccept(inventory, item))
		{
			return false;
		}
		GraphUtils.TryAcceptAll(inventory, item);
		return item.parentInventory == inventory;
	}
}
