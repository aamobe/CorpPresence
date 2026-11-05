using System;
using System.Collections.Generic;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static class Ext
{
	public static Action BeforeSave;

	public static Action DayStart;

	public static Action StateReset;

	public static Func<Company, string> OrderBlocked;

	public static Func<Company, double> OrderFactor;

	public static Func<Company, CompanyState, int, int> CatalogTier;

	public static Func<CatalogEntry, int?> Limit;

	public static Func<Company, string, int, int> ServicePrice;

	public static Func<Company, int, string, string> Charge;

	public static Func<Company, string> ExtraModule;

	public static Action<Company, Rect, Theme> DrawExtraModule;

	public static Func<Company, string, StoreClient> SpawnVisitor;

	public static Action<StoreClient, GameItem, int> SoldToClient;

	public static Func<GameItem, double> BuyFactor;

	public static Action<StoreClientManager> ScheduleExtra;

	public static Func<Company, Dialogue> CallDialog;

	public static Func<StoreClient, GameItem, bool> ClientBuys;

	public static Action<string, int> CustomerReported;

	public static Func<int> LuckBonus;

	public static Func<bool> LockVanillaDifficulty;

	public static Func<bool> ForceOrderLimits;

	public static Func<int> ForceRestockWeeks;

	private static readonly HashSet<string> Reported = new HashSet<string>();

	public static void Fire(string name, Action a)
	{
		if (a == null)
		{
			return;
		}
		try
		{
			a();
		}
		catch (Exception ex)
		{
			if (Reported.Add(name))
			{
				ModLog.Warn("子模组 " + name + " 出错：" + ex.Message);
			}
		}
	}

	public static T Ask<T>(string name, Func<T> call, T fallback)
	{
		try
		{
			return call();
		}
		catch (Exception ex)
		{
			if (Reported.Add(name))
			{
				ModLog.Warn("子模组扩展点 " + name + " 出错，已退回默认：" + ex.Message);
			}
			return fallback;
		}
	}
}
