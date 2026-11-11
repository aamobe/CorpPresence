using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static partial class SilverBlood
{

	public static bool CanInject()
	{
		if (SentinelRaw())
		{
			CorpState corpState = CorpState.Get();
			if (corpState == null)
			{
				return true;
			}
			return !corpState.Meds.SentinelCured;
		}
		return false;
	}

	public static string Inject()
	{
		MedState meds = CorpState.Get().Meds;
		meds.RejuvShots++;
		try
		{
			PlayerStore.Instance.healthData.daysSinceSentinelInjection = 0;
		}
		catch
		{
		}
		if (meds.RejuvShots >= 3)
		{
			meds.SentinelCured = true;
			RemoveSentinelPerk();
			return L.K("production.silverBlood.medicines.inject.injectRejuvenationSerumSymptomsSentinel", "你注射了回春药剂。（3/3）\n哨兵崩溃综合征的症状消失了。你再也不需要免疫宁了。", "You inject the Rejuvenation Serum. (3/3)\nThe symptoms of Sentinel Collapse Syndrome are gone. You'll never need Immunivax again.");
		}
		if (meds.RejuvShots != 1)
		{
			return L.K("production.silverBlood.medicines.inject.injectRejuvenationSerumCanBarely", "你注射了回春药剂。（2/3）\n症状几乎感觉不到了。这一针也当作打过免疫宁。", "You inject the Rejuvenation Serum. (2/3)\nYou can barely feel the symptoms. This shot also counts as Immunivax.");
		}
		return L.K("production.silverBlood.medicines.inject.injectRejuvenationSerumSymptomsEase", "你注射了回春药剂。（1/3）\n症状减轻了。这一针也当作打过免疫宁。", "You inject the Rejuvenation Serum. (1/3)\nThe symptoms ease. This shot also counts as Immunivax.");
	}

	private static bool SentinelRaw()
	{
		_raw = true;
		try
		{
			return HealthData.IsSentinel();
		}
		catch
		{
			return false;
		}
		finally
		{
			_raw = false;
		}
	}

	private static void RemoveSentinelPerk()
	{
		try
		{
			if ((PlayerStore.Instance?.startingPerks?.RemoveAll((StartingPerk p) => p?.id == "sentinel")).GetValueOrDefault() > 0)
			{
				ModLog.Info("哨兵崩溃综合征已从特性列表移除");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("移除哨兵崩溃综合征特性失败：" + ex.Message);
		}
	}

	public static void AfterIsSentinel(ref bool __result)
	{
		if (__result && !_raw)
		{
			CorpState corpState = CorpState.Get();
			if (corpState != null && corpState.Meds.SentinelCured)
			{
				__result = false;
			}
		}
	}

	public static void AfterIsPerkActive(string perkId, ref bool __result)
	{
		if (__result && perkId == "sentinel" && !_raw)
		{
			CorpState corpState = CorpState.Get();
			if (corpState != null && corpState.Meds.SentinelCured)
			{
				__result = false;
			}
		}
	}

	public static int SplitGel(GameItem gel)
	{
		GameInventory parentInventory = gel.parentInventory;
		int num = 0;
		for (int i = 0; i < 3; i++)
		{
			GameItem gameItem = Goods.Make("isomer_vial");
			if (gameItem != null)
			{
				double num2 = 0.5 + Rng.NextDouble() * 0.35;
				double num3 = 10.0 * (1.0 - num2);
				FillLiquids(gameItem, ("corp_isomer", 10.0 * num2), ("water", num3 * 0.6), ("protein", num3 * 0.25), ("organic_waste", num3 * 0.15));
				GeneralHelper.SetItemOwned(gameItem);
				if (!Placed(parentInventory, gameItem))
				{
					break;
				}
				num++;
			}
		}
		return num;
	}

	public static void AfterIsBuying(StoreClient __instance, GameItem gameItem, ref bool __result, bool __runOriginal)
	{
		if (!__runOriginal) return;
		try
		{
			if (!__result && __instance != null && __instance.isAug && gameItem?.identifier == "corp_presence_implant_compat")
			{
				__result = true;
			}
		}
		catch
		{
		}
		if (Malice.Envoy.IsProtectedBomb(gameItem))
		{
			__result = Malice.Envoy.CanReceiveBomb(__instance);
			return;
		}
		if (!__result && Malice.Malice.IsInstalled && __instance != null && gameItem != null)
		{
			__result = Ext.Ask("ClientBuys", () => Malice.Envoy.ClientBuys(__instance, gameItem) || Malice.RebelStory.ClientBuys(__instance, gameItem), fallback: false);
		}
	}

}


