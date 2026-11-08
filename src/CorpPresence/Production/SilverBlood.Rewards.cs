using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static partial class SilverBlood
{

	public static void OnSold(StoreClient client, GameItem item)
	{
		if (client != null && client.isAug && !(item?.identifier != "corp_presence_implant_compat"))
		{
			CompanyState companyState = CorpState.Get()?.CompanyOf("carmicheal");
			if (companyState != null)
			{
				companyState.ParcelsDue++;
				ModLog.Info("义体兼容剂卖给了改造体：明天收到匿名包裹");
			}
		}
	}

	public static void OnDayStart()
	{
		RegisterLiquid();
		RegisterSpecies();
		CorpState corpState = CorpState.Get();
		if (corpState != null && corpState.Meds.SentinelCured)
		{
			RemoveSentinelPerk();
		}
		CompanyState companyState = CorpState.Get()?.CompanyOf("carmicheal");
		if (companyState == null || companyState.ParcelsDue <= 0)
		{
			return;
		}
		int delivered = 0;
		SilverBloodFlow.DeliverPending(companyState.ParcelsDue, TryDeliverParcel, () =>
		{
			companyState.ParcelsDue--;
			delivered++;
		});
		if (delivered > 0)
			Tell(L.K("production.silverBlood.rewards.onDayStart.thereSParcelDoorWith", "门口放着一只没有寄件人的包裹。", "There's a parcel at the door with no sender."));
	}

	private static bool TryDeliverParcel()
	{
		GameItem gameItem = null;
		bool delivered = false;
		try
		{
			gameItem = Goods.Make("anon_parcel");
			if (gameItem == null) return false;
			int num = 0;
			foreach (var (text, num2) in ParcelPool.OrderBy(((string Id, int Value) _) => Rng.Next()))
			{
				if (num >= 300)
				{
					break;
				}
				if (num + num2 > 420)
				{
					continue;
				}
				GameItem gameItem2 = ((text == "large_purple_injector") ? CarmichaelCo.MakeGenuine(text) : DirectoryMaster.Item(text, isOwned: false));
				if (gameItem2 != null)
				{
					if (Crates.Put(gameItem, gameItem2))
					{
						num += num2;
					}
					else
					{
						gameItem2.Destroy();
					}
				}
			}
			GameItem gameItem3 = DirectoryMaster.Item("postit", isOwned: false);
			if (gameItem3 != null)
			{
				gameItem3.name = L.K("production.silverBlood.rewards.tryDeliverParcel.unsignedNote", "没署名的纸条", "Unsigned Note");
				gameItem3.flavorText = L.K("production.silverBlood.rewards.tryDeliverParcel.gratitudeDirectiveApprovedThank", "感谢指令，通过，向你致谢。", "Gratitude directive: approved. Thank you.");
				gameItem3.SetGameItemType("DOCUMENT");
				gameItem3.unitValue = 0L;
				if (!Crates.Put(gameItem, gameItem3)) gameItem3.Destroy();
			}
			if (num <= 0) return false;
			return delivered = StorePlacement.Place(gameItem);
		}
		catch (Exception error)
		{
			ModLog.Warn("银血奖励包裹发放失败，保留待发数量：" + error.Message);
			// An inventory operation may have attached the parcel before throwing.
			return delivered = gameItem != null && gameItem.parentInventory != null;
		}
		finally
		{
			if (!delivered && gameItem != null && gameItem.parentInventory == null && !StorePlacement.Destroyed(gameItem)) gameItem.Destroy();
		}
	}

	internal static void Tell(string text)
	{
		try
		{
			StoreUIManager.Instance?.Notify(text);
		}
		catch
		{
		}
	}

}

