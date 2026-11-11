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
	[ThreadStatic]
	private static int _pendingVendingWater;

	private static void AfterCounterWaterSale(GameItem item, int cost, StoreClient client)
	{
		Guard("柜台售水记录", () => Services.OnItemWaterSold(item, cost, client));
	}

	private static void AfterVendingItemSale(GameItem item, int soldValue)
	{
		// Vanilla calls this for every selected item before destroying it and recording the payout.
		_pendingVendingWater = 0;
		Guard("识别售货机售水", () =>
		{
			if (soldValue > 0 && Services.IsWaterSaleItem(item))
				_pendingVendingWater = soldValue;
		});
	}

	private static void AfterVendingSale(int value)
	{
		int pending = _pendingVendingWater;
		_pendingVendingWater = 0;
		if (pending > 0 && pending == value)
			Guard("售货机售水记录", () => Services.OnWaterSold(value));
	}

	private static void AfterFountainWaterSale(int value)
	{
		Guard("售水机售水记录", () => Services.OnWaterSold(value));
	}

	private static void AfterClientBought(StoreClient __instance, GameItem item, int cost)
	{
		Guard("卖出记录", () =>
		{
			Services.OnSoldToClient(__instance, item, cost);
		});
	}

	private static void AfterClientSold(StoreClient __instance, GameItem item, int soldAmount)
	{
		Guard("买入记录", () =>
		{
			Services.OnBoughtFromClient(__instance, item, soldAmount);
		});
	}

	private static bool IsCarmichael(GameItem item)
	{
		return Companies.Find("carmicheal")?.Owns(item) ?? ManufacturerIs(item, "carmicheal");
	}

	private static bool ManufacturerIs(GameItem item, string id)
	{
		try
		{
			TagState tagState = item?.GetTagReadonly("manufacturer");
			return tagState != null && tagState.GetString() == id;
		}
		catch
		{
			return false;
		}
	}

	private static bool VanillaBuyerAtCounter()
	{
		StoreClient storeClient = PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
		if (storeClient != null && storeClient.clientIntent == StoreClient.ClientIntent.BUY)
		{
			if (storeClient.identifier != null)
			{
				return !storeClient.identifier.StartsWith("corp_");
			}
			return true;
		}
		return false;
	}

	private static void AfterNegociatedValue(GameItem __instance, ref long __result, bool __runOriginal)
	{
		if (!__runOriginal) return;
		try
		{
			long? num = Engine.SellFixedPrice(__instance) ?? Engine.BuyFixedPrice(__instance);
			if (num.HasValue && num.Value >= 0)
			{
				__result = num.Value;
				return;
			}
			double num2 = Engine.BuyMultiplier(__instance);
			if (num2 == 1.0 && !Engine.BuyActive && Isa.Licensed && IsCarmichael(__instance) && VanillaBuyerAtCounter())
			{
				num2 = 1.1;
			}
			Func<GameItem, double> bf = Ext.BuyFactor;
			if (bf != null)
			{
				num2 *= Ext.Ask("BuyFactor", () => bf(__instance), 1.0);
			}
			if (num2 != 1.0)
			{
				double quote = (double)__result * num2;
				if (double.IsNaN(quote) || double.IsInfinity(quote) || quote < 0 || quote >= 9223372036854775808d) return;
				__result = (long)Math.Round(quote);
			}
		}
		catch
		{
		}
	}
}
