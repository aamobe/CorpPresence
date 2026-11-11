using System;
using System.Collections.Generic;
using System.Reflection;
using CorpPresence.Runtime;
using CorpPresence.UI;
using HarmonyLib;
using UnityEngine;

namespace CorpPresence;

internal static partial class Hooks
{
	private static string _reportedId;
	private static int _revBefore;

	private static void BeforeReported(PlayerStore __instance)
	{
		try
		{
			_reportedId = __instance.currentClientInstance?.GetClientBlueprint()?.identifier;
			_revBefore = Engine.Reputation("FACTION_REVOLUTION");
		}
		catch
		{
			_reportedId = null;
		}
	}

	private static void AfterReported()
	{
		if (Malice.Malice.IsInstalled && _reportedId != null)
		{
			int delta = Engine.Reputation("FACTION_REVOLUTION") - _revBefore;
			string id = _reportedId;
			_reportedId = null;
			Ext.Fire("CustomerReported", () =>
			{
				Malice.RebelStory.OnReported(id, delta);
			});
		}
	}

	private static void AfterGetLuck(ref int __result)
	{
		if (Malice.Malice.IsInstalled)
		{
			__result += Ext.Ask("LuckBonus", Malice.Envoy.LuckBonus, 0);
		}
	}

	private static void AfterMaxScav(ref int __result)
	{
		try
		{
			__result += Goods.ExtraScavAttempts;
		}
		catch
		{
		}
	}

	private static float MinorFactor()
	{
		return (Goods.PainBlockedToday ? 0.5f : 1f) * (Goods.GlovesCarried() ? 0.7f : 1f);
	}

	private static float MajorFactor()
	{
		if (!Goods.PainBlockedToday)
		{
			return 1f;
		}
		return 0.5f;
	}

	private static void AfterMinorWoundChance(ref float __result)
	{
		try
		{
			__result *= MinorFactor();
		}
		catch
		{
		}
	}

	private static void AfterWoundChance(ref float __result)
	{
		try
		{
			__result *= MajorFactor();
		}
		catch
		{
		}
	}

	private static void AfterAddictionPenalty(HealthData __instance, ref int __result)
	{
		try
		{
			if (Goods.PatchActive && HealthData.IsNicotineAddicted() && __instance.daysSinceNicotineConsumption >= 3)
			{
				__result = Math.Max(0, __result - 8);
			}
		}
		catch
		{
		}
	}

	private static void AfterNicotineDebuff(ref bool __result)
	{
		try
		{
			if (Goods.PatchActive)
			{
				__result = false;
			}
		}
		catch
		{
		}
	}

	private static void AfterDisplayCase(ref List<StoreClient> __result)
	{
		try
		{
			if (!Goods.SignInShowcase())
			{
				return;
			}
			StoreClient storeClient = CompanyVisitors.PastelSeller();
			if (storeClient != null)
			{
				if (__result == null)
				{
					__result = new List<StoreClient>();
				}
				__result.Add(storeClient);
				ModLog.Info("特约店灯牌：今天会有一位帕斯泰尔货商上门");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("灯牌卖家失败：" + ex.Message);
		}
	}

	private static void AfterTradeRepMultiplier(ref double __result)
	{
		try
		{
			if (Goods.ElixirActive)
			{
				__result *= 1.5;
			}
		}
		catch
		{
		}
	}

	private static void BeforeAfterhourDebuff(PlayerStore __instance, bool __runOriginal)
	{
		if (!__runOriginal) return;
		try
		{
			Goods.TryUseMask(__instance);
		}
		catch
		{
		}
	}
}
