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
		CorpState root = CorpState.Get();
		var workshop = root?.Workshop;
		if (workshop?.CenKitRemaining > 0 || workshop?.Sales?.Product is { Complete: false } ||
			workshop?.Sales?.Powder is { Complete: false } || workshop?.Sales?.Contacts is { Complete: false } ||
			(root?.Companies != null && root.Companies.TryGetValue("lower_mutual", out var mutual) && mutual.Orders?.Count > 0))
		{
			string pending = L.K("workshop.cen.cleanupWait", "下层互助还有待领物资、在途订单或未结收购。请先关闭卸载清理，到互助页领料或结清收购，并签收送货。", "Lower Levels Mutual Aid has unclaimed supplies, a delivery or an unsettled sale. Turn off uninstall cleanup, collect or settle at the contact page, and receive outstanding deliveries.");
			ModLog.Warn(pending);
			StoreUIManager.Instance?.Notify(pending);
			return;
		}
		if (CartelShizuka.Introduced(root) && workshop?.ShizukaKitRemaining > 0 ||
			workshop?.GelOrder is { Complete: false } || workshop?.CartelDaily is { Complete: false } ||
			(root?.Companies != null && root.Companies.TryGetValue("cartel_supplies", out var cartel) && cartel.Orders?.Count > 0))
		{
			string pending = L.K("workshop.shizuka.cleanupWait", "黑羽静处还有待领试料、在途订单或未结货款。请先在联系页取货、结清并签收。", "Shizuka still has starter supplies, a delivery or a payment for you. Collect and settle at her contact page, then receive any delivery.");
			ModLog.Warn(pending);
			StoreUIManager.Instance?.Notify(pending);
			return;
		}
		var contract = workshop?.FirstOverhaul;
		if (workshop?.Commission != null || root?.Companies != null && root.Companies.TryGetValue("meridian", out var meridian)
			&& meridian.Orders.Any(o => o.Kind == MeridianCommissionReceipt.OrderKind)
			|| contract is { CompletedDay: null, Abandoned: false })
		{
			string pending = L.K("workshop.overhaul.cleanup","请先签收子午线在途框架并结清代工，到下层互助结清或中止大修工单，再执行卸载清理。",
				"Collect Meridian frame deliveries and settle the commission, then settle or suspend the overhaul at Mutual Aid before uninstall cleanup.");
			ModLog.Warn(pending);
			StoreUIManager.Instance?.Notify(pending);
			return;
		}
		if (workshop?.Rotations?.Any(c => c.HasOpenObligations) == true)
		{
			string pending = L.K("workshop.rotation.cleanup","请在联合工单结清或中止在办合同，归还领出的旧件，并签收应得的炉衬，再执行卸载清理。",
				"Settle or suspend active contracts, return outstanding shells and collect any recovered lining at the shell desk before uninstall cleanup.");
			ModLog.Warn(pending);
			StoreUIManager.Instance?.Notify(pending);
			return;
		}
		var storeItems = (store.FindAllItem(isOwn: false) ?? Enumerable.Empty<GameItem>()).ToArray();
		if (storeItems.Any(item => PowderClassifier.HasActiveBatch(item) || MineralGrowthStation.HasActiveBatch(item)
			|| CartelReactor.HasLegacyBatch(item)))
		{
			string pending = L.K("workshop.mill.cleanupBatch", "机器里还有已投料的批次。请关闭卸载清理，让批次完成并取走成品，再重试。", "A machine still has a paid batch. Turn off uninstall cleanup, finish and collect the batch, then retry.");
			ModLog.Warn(pending);
			StoreUIManager.Instance?.Notify(pending);
			return;
		}
		int num = Phone.RemoveAll();
		int num2 = 0;
		int retained = 0;
		foreach (GameItem item2 in storeItems)
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
			}
			try
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
			StoreUIManager.Instance?.Notify(retained > 0 ? L.K("persistence.cleanup.beforeSave.corpPresenceContainersStillHoldItems", "公司增强：有未清空的容器，请腾出库存空间后重试，暂不要卸载。", "CorpPresence: containers still hold items. Free inventory space and retry before uninstalling.") : L.K("persistence.cleanup.beforeSave.corpPresenceCleanedUpPhoneNumbers", $"公司增强：已清理（电话 {num} 个，物品 {num2} 件），存档后即可卸载。", (System.FormattableString)$"CorpPresence: cleaned up ({num} phone numbers, {num2} items). You can uninstall after this save."));
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



