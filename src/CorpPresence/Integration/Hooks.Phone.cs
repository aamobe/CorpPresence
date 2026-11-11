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
	private static void AfterInitPhones(Dictionary<long, StorePhoneClient> __result)
	{
		Guard("电话登记", () =>
		{
			Phone.Register(__result);
		});
	}

	private static void AfterShownInPhoneBook(StorePhoneClient __instance, ref bool __result)
	{
		if (__instance?.locID == MangoGift.PhoneId) { __result = false; return; }
		try
		{
			Notebook.AfterShownInPhoneBook(__instance, ref __result);
		}
		catch
		{
		}
	}

	private static void AfterCloseAllPaper()
	{
		Guard("关名片", CardView.CloseAll);
	}

	private static void AfterPhoneOpen()
	{
		Guard("记事簿", () =>
		{
			Phone.RegisterCurrent();
			Notebook.Refresh();
		});
	}

	private static void AfterGetCallDialog(StorePhoneClient __instance, ref Dialogue __result)
	{
		try
		{
			if (__instance?.locID == MangoGift.PhoneId) { __result = MangoGift.Call(); return; }
			Company company = Companies.ByLocId(__instance?.locID);
			if (company != null)
			{
				__result = Phone.CallDialog(company);
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("公司电话出错：" + ex.Message);
		}
	}

	private static void AfterPhoneName(StorePhoneClient __instance, ref string __result)
	{
		try
		{
			if (__instance?.locID == MangoGift.PhoneId || Companies.ByLocId(__instance?.locID) != null)
			{
				__result = __instance.displayName;
			}
		}
		catch
		{
		}
	}

	private static void BeforeHandleCall(long currentNumber)
	{
		Guard("公司电话拒接提示", () =>
		{
			Phone.WarnIfShunned(currentNumber);
		});
	}

	private static void AfterWillAnswer(long number, ref bool __result)
	{
		try
		{
			Company company = Companies.ByNumber(number);
			if (company != null)
			{
				__result = Companies.StateOf(company)?.CardGiven ?? false;
			}
		}
		catch
		{
		}
	}
}
