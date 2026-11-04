using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static partial class ItemRegistry
{
	public static bool IsAvailable(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return false;
		}
		try
		{
			return DirectoryMaster.Has<GameItem>(id);
		}
		catch (Exception ex)
		{
			Warn("available:" + id, "检查 " + id + " 失败：" + ex.Message);
			return false;
		}
	}

	public static GameItem Create(string id)
	{
		if (id == null)
		{
			return null;
		}
		if (!IsAvailable(id))
		{
			if (ItemIdentity.Find(id, null) != null)
			{
				Warn("missing:" + id, "物品目录未就绪，暂不生成：" + id);
			}
			return null;
		}
		GameItem gameItem = DirectoryMaster.Item(id, isOwned: false);
		if (gameItem != null && gameItem.identifier == id)
		{
			return gameItem;
		}
		// A rejected factory result will never reach the caller's normal cleanup.
		// Never destroy an item that another participant has already placed.
		if (gameItem != null && gameItem.parentInventory == null && !StorePlacement.Destroyed(gameItem))
		{
			try
			{
				gameItem.Destroy();
			}
			catch (Exception ex)
			{
				Warn("discard:" + id, "回收错误身份的临时物品失败：" + id + "；" + ex.Message);
			}
		}
		Warn("create:" + id, "物品工厂返回了不匹配的身份：" + id);
		return null;
	}

}

