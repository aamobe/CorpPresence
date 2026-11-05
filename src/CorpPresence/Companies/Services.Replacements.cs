using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class Services
{
	public static int SwapNextDay(CompanyState s)
	{
		if (s.SwapLastDay >= 0)
		{
			return s.SwapLastDay + CarmichaelCo.SwapGap;
		}
		return Day;
	}

	public static List<GameItem> FakesOwned()
	{
		List<GameItem> list = new List<GameItem>();
		try
		{
			foreach (GameItem allRepoEligibleItem in PlayerStore.Instance.GetAllRepoEligibleItems())
			{
				if (CarmichaelCo.FakeReason(allRepoEligibleItem) != null)
				{
					list.Add(allRepoEligibleItem);
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("查找赝品失败：" + ex.Message);
		}
		return list;
	}

	public static string RequestSwap(Company c)
	{
		CompanyState companyState = Companies.StateOf(c);
		if (companyState?.Orders == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		if (Day < SwapNextDay(companyState))
		{
			return L.T($"第 {SwapNextDay(companyState)} 天才能再申请", $"You can apply again on day {SwapNextDay(companyState)}");
		}
		List<GameItem> list = FakesOwned().Take(6).ToList();
		if (list.Count == 0)
		{
			return L.T("没有找到卡迈克尔产品的赝品", "No fake Carmicheal products found");
		}
		Order order = new Order
		{
			Day = Day + 1,
			Kind = "swap"
		};
		int num;
		try
		{
			int fee = CarmichaelCo.SwapFee;
			long total = (long)list.Count * fee;
			if (fee < 0 || total > int.MaxValue)
				return L.T("换货费用无效，尚未扣款。", "Invalid exchange fee. You have not been charged.");
			num = (int)total;
			foreach (GameItem item in list)
			{
				string target = CarmichaelCo.SwapTarget(item);
				if (item == null || string.IsNullOrEmpty(target))
					return L.T("换货明细未就绪，尚未扣款。", "Exchange details are not ready. You have not been charged.");
				order.Items.Add(target);
			}
		}
		catch
		{
			return L.T("换货明细未就绪，尚未扣款。", "Exchange details are not ready. You have not been charged.");
		}
		string text = Pay(c, num, "swap");
		if (text != null) return text;
		foreach (GameItem item in list)
		{
			try
			{
				item.Destroy();
			}
			catch (Exception ex)
			{
				try { ModLog.Warn("收走赝品失败：" + ex.Message); } catch { }
			}
		}
		companyState.Orders.Add(order);
		companyState.SwapLastDay = Day;
		ModLog.Info($"赝品换正品：{list.Count} 件，手续费 {num}₵，第 {order.Day} 天送达");
		return null;
	}

	public static int RefurbishNextDay(CompanyState s)
	{
		if (s.RefurbishLastDay >= 0)
		{
			return s.RefurbishLastDay + MeridianCo.RefurbishGap;
		}
		return Day;
	}

	public static List<GameItem> RefurbishCandidates()
	{
		List<GameItem> list = new List<GameItem>();
		try
		{
			foreach (GameItem allRepoEligibleItem in PlayerStore.Instance.GetAllRepoEligibleItems())
			{
				if (MeridianCo.CanRefurbish(allRepoEligibleItem))
				{
					list.Add(allRepoEligibleItem);
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("查找待翻新的枪失败：" + ex.Message);
		}
		return list;
	}

	public static string RequestRefurbish(Company c)
	{
		CompanyState companyState = Companies.StateOf(c);
		if (companyState?.Orders == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		if (c.Tier(companyState) < 2)
		{
			return L.T(c.TierNames[1] + "才开放", "Opens at " + c.TierNames[1]);
		}
		if (Day < RefurbishNextDay(companyState))
		{
			return L.T($"第 {RefurbishNextDay(companyState)} 天才能再送", $"You can send again on day {RefurbishNextDay(companyState)}");
		}
		List<GameItem> list = RefurbishCandidates().Take(3).ToList();
		if (list.Count == 0)
		{
			return L.T("没有能翻新的枪", "No guns to refurbish");
		}
		Order order = new Order
		{
			Day = Day + 1,
			Kind = "refurbish"
		};
		int num;
		try
		{
			long total = 0;
			foreach (GameItem item in list)
			{
				int condition = MeridianCo.ConditionOf(item);
				if (item == null || string.IsNullOrEmpty(item.identifier) || condition < 0 || condition >= 3)
					return L.T("翻新明细未就绪，尚未扣款。", "Refurbishment details are not ready. You have not been charged.");
				int fee = MeridianCo.RefurbishFee(item);
				total += fee;
				if (fee < 0 || total > int.MaxValue)
					return L.T("翻新费用无效，尚未扣款。", "Invalid refurbishment fee. You have not been charged.");
				order.Items.Add($"{item.identifier}|{condition + 1}");
			}
			num = (int)total;
		}
		catch
		{
			return L.T("翻新明细未就绪，尚未扣款。", "Refurbishment details are not ready. You have not been charged.");
		}
		string text = Pay(c, num, "refurbish");
		if (text != null) return text;
		foreach (GameItem item in list)
		{
			try
			{
				item.Destroy();
			}
			catch (Exception ex)
			{
				try { ModLog.Warn("收走待翻新的枪失败：" + ex.Message); } catch { }
			}
		}
		companyState.Orders.Add(order);
		companyState.RefurbishLastDay = Day;
		AddCoop(c, (double)num / Settings.CoopCredits, "返厂翻新");
		ModLog.Info($"返厂翻新：{list.Count} 把，{num}₵，第 {order.Day} 天送回");
		return null;
	}

}

