using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static class Cleanup
{
	public static void BeforeSave()
	{
		if (!Settings.CleanupForUninstall)
		{
			return;
		}
		PlayerStore currentStore = SaveSlot.CurrentStore;
		if (currentStore == null)
		{
			return;
		}
		int num = Phone.RemoveAll();
		int num2 = 0;
		int num3 = 0;
		IEnumerable<GameItem> enumerable = currentStore.FindAllItem(isOwn: false);
		GameItem[] array = (enumerable ?? Enumerable.Empty<GameItem>()).ToArray();
		foreach (GameItem gameItem in array)
		{
			if (!IsOurs(gameItem))
			{
				continue;
			}
			bool flag = false;
			foreach (GameInventory item2 in GraphUtils.FindChildrenType<GameInventory>(Compat.Cast<GraphNodeStorage>(gameItem)))
			{
				GameItem[] array2 = item2.childItems.ToArray();
				foreach (GameItem item in array2)
				{
					try
					{
						GameGridInventory gameGridInventory = EmporiumEntry.Instance?.invElement;
						if (gameGridInventory != null && GraphUtils.CanAccept(gameGridInventory, item))
						{
							GraphUtils.TryAcceptAll(gameGridInventory, item);
						}
					}
					catch
					{
					}
				}
				if (item2.childItems.Count > 0)
				{
					flag = true;
				}
			}
			if (flag)
			{
				num3++;
				try
				{
					ModLog.Warn("卸载清理保留了未清空容器，请腾出空间后重试；暂不要卸载。");
				}
				catch
				{
				}
				continue;
			}
			try
			{
				gameItem.Destroy();
				num2++;
			}
			catch (Exception ex)
			{
				ModLog.Warn("卸载前清理：删除物品失败：" + ex.Message);
			}
		}
		try
		{
			ModLog.Info($"卸载前清理：移除号码 {num} 个，删除物品 {num2} 件，未清空容器 {num3} 个。");
		}
		catch
		{
		}
		try
		{
			StoreUIManager.Instance?.Notify((num3 > 0) ? L.T("公司增强：有未清空的容器，请腾出库存空间后重试，暂不要卸载。", "CorpPresence: containers still hold items. Free inventory space and retry before uninstalling.") : L.T($"公司增强：已清理（电话 {num} 个，物品 {num2} 件），存档后即可卸载。", $"CorpPresence: cleaned up ({num} phone numbers, {num2} items). You can uninstall after this save."));
		}
		catch
		{
		}
	}

	private static bool IsOurs(GameItem item)
	{
		try
		{
			if (item == null)
			{
				return false;
			}
			if (ItemIdentity.Find(item.identifier, item.spritePath) != null)
			{
				return true;
			}
			return item.IsTag("CORP_SPACER_NOTE");
		}
		catch
		{
			return false;
		}
	}
}
