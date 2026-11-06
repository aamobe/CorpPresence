using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class Services
{
	public static List<string> LabListThisWeek(CompanyState s)
	{
		int num = PeriodIndex("lab", 7);
		if (s.LabWeek != num || s.LabList.Count == 0)
		{
			s.LabList = CreateLabList(num);
			s.LabWeek = num;
			s.LabBooked = false;
		}
		return s.LabList;
	}

	private static List<string> CreateLabList(int period)
	{
		Random rng = new Random(period * 7919 + 13);
		List<string> list = new List<string> { "blue_blood_bag" };
		list.AddRange(SolanaCo.LabPool.OrderBy(_ => rng.Next()).Take(2));
		return list;
	}

	internal static IReadOnlyList<string> LabListForVisit(CompanyState state, Visitor visit)
	{
		if (visit.LabItems != null) return visit.LabItems;
		// Legacy bookings stored only the scheduled arrival day.
		int period = (Math.Max(1, visit.Day) - 1) / SolanaCo.LabPeriod;
		return visit.LabItems = state.LabWeek == period && state.LabList.Count > 0
			? new List<string>(state.LabList)
			: CreateLabList(period);
	}

	public static int VipNextDay(CompanyState s)
	{
		if (s.VipBookedDay >= 0)
		{
			return s.VipBookedDay + SolanaCo.VipGap;
		}
		return Day;
	}

	public static string BookVip(Company c)
	{
		CompanyState companyState = Companies.StateOf(c);
		if (companyState?.Visitors == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		if (Day < VipNextDay(companyState))
		{
			return L.T($"第 {VipNextDay(companyState)} 天才能再预约", $"You can book again on day {VipNextDay(companyState)}");
		}
		int day = Day;
		int fee = SolanaCo.VipFee;
		Visitor visitor = new Visitor { Day = day + 1, Kind = "vip" };
		string text = Pay(c, fee, "vip");
		if (text != null)
		{
			return text;
		}
		companyState.Visitors.Add(visitor);
		companyState.VipBookedDay = day;
		try { ModLog.Info($"预约索拉纳贵宾：{fee}₵，第 {visitor.Day} 天来"); } catch { }
		return null;
	}

	public static string BookLab(Company c)
	{
		CompanyState companyState = Companies.StateOf(c);
		if (companyState?.Visitors == null || companyState.LabList == null)
			return L.T("存档未就绪", "Save not ready");
		string text = OrderBlocked(c);
		if (text != null)
		{
			return text;
		}
		string readiness = Pay(c, 0, "lab");
		if (readiness != null) return readiness;
		List<string> list;
		try { list = LabListThisWeek(companyState); }
		catch { return L.T("采购清单暂未就绪", "Procurement list not ready"); }
		if (companyState.LabBooked)
		{
			return L.T("这一期已经预约过了", "Already booked for this list");
		}
		companyState.Visitors.Add(new Visitor
		{
			Day = Day + 1,
			Kind = "lab",
			LabItems = new List<string>(list)
		});
		companyState.LabBooked = true;
		return null;
	}

	public static bool GiftReady(CompanyState s)
	{
		return s.GiftWeek != PeriodIndex("gift", 7);
	}

	public static string ClaimGift(Company c)
	{
		CompanyState companyState = Companies.StateOf(c);
		if (companyState?.Orders == null) return L.T("存档未就绪", "Save not ready");
		string text = OrderBlocked(c);
		if (text != null)
		{
			return text;
		}
		string readiness = Pay(c, 0, "gift");
		if (readiness != null) return readiness;
		int period = PeriodIndex("gift", 7);
		if (companyState.GiftWeek == period)
		{
			return L.T("这一期已经领过了", "Already claimed for this period");
		}
		Order order;
		try
		{
			string item = Engine.Any("neuroactive_perfume", "@elixir", "@mask");
			if (string.IsNullOrEmpty(item)) return L.T("赠礼暂未就绪", "Gift not ready");
			order = new Order { Day = Day + 1, Kind = "gift", Items = new List<string> { item } };
		}
		catch { return L.T("赠礼暂未就绪", "Gift not ready"); }
		companyState.Orders.Add(order);
		companyState.GiftWeek = period;
		return null;
	}

	public static int StaminaNextDay(CompanyState s)
	{
		if (s.StaminaLastDay >= 0)
		{
			return s.StaminaLastDay + CarmichaelCo.StaminaGap;
		}
		return Day;
	}

	public static string RequestStamina(Company c)
	{
		CompanyState companyState = Companies.StateOf(c);
		if (companyState?.Orders == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		if (!CarmichaelCo.StaminaOpen())
		{
			return L.T("尚未向贵店配发", "Not yet allocated to your shop");
		}
		if (Day < StaminaNextDay(companyState))
		{
			return L.T($"批次未到，第 {StaminaNextDay(companyState)} 天可申领", $"Batch not ready. Available on day {StaminaNextDay(companyState)}");
		}
		int day = Day;
		int fee = CarmichaelCo.StaminaFee;
		Order order = new Order { Day = day + 1, Kind = "stamina", Items = new List<string> { "@stamina" } };
		string text = Pay(c, fee, "stamina");
		if (text != null)
		{
			return text;
		}
		companyState.Orders.Add(order);
		companyState.StaminaLastDay = day;
		try { ModLog.Info($"体力增强剂申领：手续费 {fee}₵，第 {order.Day} 天送达"); } catch { }
		return null;
	}

	public static string OrderLeviathan(DavreCo c, int price)
	{
		CompanyState companyState = Companies.StateOf(c);
		if (companyState?.Orders == null || companyState.LimitedBought == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		if (!DavreCo.LeviathanOpen)
		{
			return L.T("还没有开通", "Not available yet");
		}
		string quotaKey = "leviathan@" + PeriodKey;
		int used = companyState.LimitedBought.TryGetValue(quotaKey, out int prior) ? prior : 0;
		if (used >= c.LeviathanLimit(companyState))
		{
			return L.T("这周的配额已经订完了", "This week's quota is used up");
		}
		if (price <= 0)
		{
			return L.T("利维坦暂不可用，请稍后再试。尚未扣款。", "The Leviathan is unavailable right now. Try again later. You have not been charged.");
		}
		string text = OrderBlocked(c);
		if (text != null)
		{
			return text;
		}
		Order order = new Order { Day = Day + 1, Items = new List<string> { "@leviathan" } };
		string text2 = Pay(c, price, "leviathan");
		if (text2 != null)
		{
			return text2;
		}
		companyState.Orders.Add(order);
		companyState.LimitedBought[quotaKey] = (companyState.LimitedBought.TryGetValue(quotaKey, out int current) ? current : 0) + 1;
		AddCoop(c, (double)price / Settings.CoopCredits, "订购利维坦");
		try { ModLog.Info($"利维坦净水器订单：{price}₵，第 {order.Day} 天送达"); } catch { }
		return null;
	}

	public static string MakeUpCanning(DavreCo c)
	{
		CompanyState companyState = Companies.StateOf(c);
		if (companyState?.Orders == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		if (companyState.CanningOwned)
		{
			return L.T("已经办过了", "Already done");
		}
		if (!DavreCo.CanningOffered)
		{
			return L.T("还没有开通", "Not available yet");
		}
		Order order;
		try
		{
			List<string> machines = new List<string> { Canning.Grinder, Canning.Canner };
			if (machines.Count == 0 || machines.Any(string.IsNullOrEmpty))
				return L.T("设备明细未就绪，尚未扣款。", "Equipment details not ready. You have not been charged.");
			order = new Order { Day = Day + 1, Items = machines };
		}
		catch { return L.T("设备明细未就绪，尚未扣款。", "Equipment details not ready. You have not been charged."); }
		int fee = DavreCo.CanningFee;
		string text = Pay(c, fee, "canning");
		if (text != null)
		{
			return text;
		}
		companyState.Orders.Add(order);
		companyState.CanningOwned = true;
		try { ModLog.Info($"罐装设备补办：{fee}₵，第 {order.Day} 天送达两台机器"); } catch { }
		return null;
	}

	public static string OrderCanningMachine(DavreCo c, string id, int price)
	{
		CompanyState companyState = Companies.StateOf(c);
		if (companyState?.Orders == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		if (!companyState.CanningOwned)
		{
			return L.T("还没有开通", "Not available yet");
		}
		if (price <= 0 || string.IsNullOrEmpty(id) || DavreCo.CanningMachines == null || !DavreCo.CanningMachines.Any(m => m.Id == id))
			return L.T("设备暂不可用，尚未扣款。", "Equipment unavailable. You have not been charged.");
		string text = OrderBlocked(c);
		if (text != null)
		{
			return text;
		}
		Order order = new Order { Day = Day + 1, Items = new List<string> { id } };
		string text2 = Pay(c, price, "canning_machine");
		if (text2 != null)
		{
			return text2;
		}
		companyState.Orders.Add(order);
		AddCoop(c, (double)price / Settings.CoopCredits, "订购罐装设备");
		try { ModLog.Info($"罐装设备订单：{id}，{price}₵，第 {order.Day} 天送达"); } catch { }
		return null;
	}

}

