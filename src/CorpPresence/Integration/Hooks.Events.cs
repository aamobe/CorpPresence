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
	private static Action _nightly;
	private static bool _customerHookInstalled;

	public static void InstallEvents()
	{
		if (!_customerHookInstalled)
		{
			ModHook.OnGenerateCustomerVeryLate += OnGenerateCustomer;
			_customerHookInstalled = true;
		}
		if (_nightly == null)
		{
			_nightly = () =>
			{
				Guard("每晚服务", Services.OnNightlyServices);
			};
			ModHook.OnHandlingNightlyServicesEarly += _nightly;
		}
	}

	public static void UninstallEvents()
	{
		if (_customerHookInstalled)
		{
			ModHook.OnGenerateCustomerVeryLate -= OnGenerateCustomer;
			_customerHookInstalled = false;
		}
		if (_nightly != null)
		{
			ModHook.OnHandlingNightlyServicesEarly -= _nightly;
			_nightly = null;
		}
	}

	private static void Guard(string what, Action action)
	{
		HookGuard.Run(what, action);
	}

	private static void AfterBeginDay()
	{
		Guard("清理过期估价样品", CatalogPricing.ReleaseExpiredSamples);
		Guard("开店检查", () =>
		{
			Engine.OnDayStart();
			SpacerCo.OnDayStart();
			Phone.RegisterCurrent();
			Goods.OnDayStart();
			Brand.ScanOwned();
			SilverBlood.OnDayStart();
			CarmichaelCo.OnDayStart();
			Ext.DayStart.Publish();
		});
	}

	private static void BeforeEndDay()
	{
		Guard("打烊记录", Services.OnEndDay);
		Guard("银血线", CarmichaelCo.OnEndDay);
		Guard("预约来访", Engine.OnDayEnd);
	}

	private static void OnGenerateCustomer(StoreClientManager manager)
	{
		Guard("芒果彩蛋来访", () => MangoGift.Schedule(manager));
		Guard("安排来访", () =>
		{
			Engine.Schedule(manager);
			Goods.ApplyCandle(manager);
		});
	}

	private static void BeforeNextClient(PlayerStore __instance, bool __runOriginal)
	{
		if (!__runOriginal) return;
		Guard("芒果彩蛋来访", () => MangoGift.Schedule(__instance?.storeClientManager));
		Guard("安排来访", () =>
		{
			Engine.BuyActive = false;
			Engine.Schedule(__instance?.storeClientManager);
		});
	}
}
