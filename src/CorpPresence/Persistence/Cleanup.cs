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
		PlayerStore store = SaveSlot.CurrentStore;
		if (store == null) return;
		int num = Phone.RemoveAll();
		int num2 = 0;
		int retained = 0;
		IEnumerable<GameItem> enumerable = store.FindAllItem(isOwn: false);
		foreach (GameItem item2 in (enumerable ?? Enumerable.Empty<GameItem>()).ToArray())
		{
			if (!IsOurs(item2))
			{
				continue;
			}
			bool occupied = false;
			// Machines can have multiple input, output, battery and module inventories.
			foreach (GameInventory inventory in GraphUtils.FindChildrenType<GameInventory>(Compat.Cast<GraphNodeStorage>(item2)))
			{
				foreach (GameItem item in inventory.childItems.ToArray())
				{
					try
					{
						var target = EmporiumEntry.Instance?.invElement;
						if (target != null && GraphUtils.CanAccept(target, item)) GraphUtils.TryAcceptAll(target, item);
					}
					catch { }
				}
				if (inventory.childItems.Count > 0) occupied = true;
			}
			if (occupied)
			{
				retained++;
				try { ModLog.Warn("卸载清理保留了未清空容器，请腾出空间后重试；暂不要卸载。"); } catch { }
				continue;
			}			try
			{
				item2.Destroy();
				num2++;
			}
			catch (Exception ex)
			{
				ModLog.Warn("卸载前清理：删除物品失败：" + ex.Message);
			}
		}
		try { ModLog.Info($"卸载前清理：移除号码 {num} 个，删除物品 {num2} 件，未清空容器 {retained} 个。"); } catch { }
		try
		{
			StoreUIManager.Instance?.Notify(retained > 0 ? L.T("公司增强：有未清空的容器，请腾出库存空间后重试，暂不要卸载。", "CorpPresence: containers still hold items. Free inventory space and retry before uninstalling.") : L.T($"公司增强：已清理（电话 {num} 个，物品 {num2} 件），存档后即可卸载。", $"CorpPresence: cleaned up ({num} phone numbers, {num2} items). You can uninstall after this save."));
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



