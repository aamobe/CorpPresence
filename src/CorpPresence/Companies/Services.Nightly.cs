using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class Services
{
	public static void OnEndDay()
	{
		_closedDay = Day;
	}

	public static void ResetNightly()
	{
		_closedDay = -1;
	}

	private static void NightLog(string text, string color = null)
	{
		try
		{
			PlayerStore instance = SaveSlot.CurrentStore;
			if (instance == null) return;
			if (color == null) instance.AddNightLog(text);
			else instance.AddNightLog(text, color);
		}
		catch { }
	}

	public static void OnNightlyServices()
	{
		if (_nightlyRunning || !GameResources.Ready) return;
		_nightlyRunning = true;
		try
		{
			PlayerStore store = SaveSlot.CurrentStore;
			if (store == null) return;
			int day = _closedDay >= 0 ? _closedDay : Day;
			_closedDay = -1;
			CorpState root = CorpState.Get();
			if (root == null) return;
			string run = store.runID;
			long generation = GameResources.Generation;
			bool IsCurrent() => GameResources.Ready && GameResources.Generation == generation && ReferenceEquals(SaveSlot.CurrentStore, store) && store.runID == run && ReferenceEquals(CorpState.Current, root);
			if (Settings.MaliceMode) CorpPresence.Malice.MaliceState.Get();
			if (!SaveSlot.CanWrite("CorpPresence") || (Settings.MaliceMode && !SaveSlot.CanWrite("CorpPresence.Malice")))
			{
				if (IsCurrent()) RescueCrates();
				return;
			}
			foreach (Company company in Companies.All)
			{
				if (!IsCurrent()) break;
				CompanyState state;
				try
				{
					state = Companies.StateOf(company);
					if (state?.Orders == null || state.Visitors == null || state.LimitedBought == null) continue;
					if (!IsCurrent()) break;
					if (!NightSettlement.TryBegin(state, day)) continue;
				}
				catch (Exception error) { WarnNightService("读取夜间公司状态", error); continue; }
				try
				{
					if (state.TrafficOn) Nightly(company, state, company is SpacerCo ? SpacerCo.MutualFee : PastelCo.TrafficFee, "traffic", day);
				}
				catch (Exception error) { WarnNightService("夜间服务扣费", error); }
				if (!IsCurrent()) break;
				state.VipOn = false;
				try { Rebate(company, state, day + 1); }
				catch (Exception error) { WarnNightService("登记公司返点", error); }
			}
			if (IsCurrent()) RescueCrates();
		}
		finally { _nightlyRunning = false; }
	}

	private static void WarnNightService(string stage, Exception error)
	{
		try { ModLog.Warn(stage + "失败：" + error.Message); } catch { }
	}

	private static void RescueCrates()
	{
		try
		{
			GameGridInventory gameGridInventory = EmporiumEntry.Instance?.frontInvinvElement;
			if (gameGridInventory == null)
			{
				return;
			}
			int num = 0;
			int num2 = 0;
			foreach (GameItem item in gameGridInventory.childItems.ToList())
			{
				string text = Goods.IdOf(item);
				if (text != null && text.StartsWith("crate_"))
				{
					GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, item);
					if (item.parentInventory == gameGridInventory)
					{
						num2++;
					}
					else
					{
						num++;
					}
				}
			}
			if (num > 0)
			{
				NightLog(L.T($"柜台上的 {num} 只送货箱已收进背包。", $"{num} delivery crate(s) left on the counter were moved into your bag."));
			}
			if (num2 > 0)
			{
				NightLog(L.T($"背包放不下，柜台上还有 {num2} 只送货箱，过夜会被清掉。", $"Your bag is full: {num2} delivery crate(s) remain on the counter and will be cleared overnight."), "9c3738");
			}
		}
		catch (Exception ex)
		{
			WarnNightService("收起送货箱", ex);
		}
	}

	private static void Nightly(Company c, CompanyState s, int fee, string kind, int day)
	{
		string text;
		if (kind == "vip")
		{
			text = L.T("贵宾引荐", "VIP Referral");
		}
		else
		{
			text = ((c is SpacerCo) ? L.T("街坊互助", "Mutual Aid") : L.T("下层客流", "Lower Foot Traffic"));
		}
		Visitor visitor = new Visitor { Day = day + 1, Kind = kind };
		string paymentError = Pay(c, fee, kind);
		if (paymentError != null)
		{
			if (kind == "traffic")
			{
				s.TrafficOn = false;
			}
			else
			{
				s.VipOn = false;
			}
			NightLog(L.T(c.Name + "的" + text + "未结算，已暂停：" + paymentError, c.Name + " " + text + " paused: " + paymentError), "9c3738");
			try { ModLog.Info(c.Name + "的" + text + "已暂停：" + paymentError); } catch { }
		}
		else
		{
			s.Visitors.Add(visitor);
			string text2;
			if (kind == "vip")
			{
				text2 = L.T("明天会有一位上层区贵宾光顾店铺", "An Upper Levels VIP will visit tomorrow");
			}
			else
			{
				text2 = ((c is SpacerCo) ? L.T("明天会多来几位星际客", "More Spacers will come by tomorrow") : L.T("明天会多来几位下层区顾客", "More Lower Levels customers will come by tomorrow"));
			}
			NightLog(L.T($"{c.Name}{text}已付 {fee} 信用点，{text2}。", $"{c.Name} {text}: paid {fee} credits. {text2}."));
			try { ModLog.Info($"{c.Name}{text}：扣 {fee}₵，第 {day + 1} 天生效"); } catch { }
		}
	}

	private static void Rebate(Company c, CompanyState s, int tomorrow)
	{
		if (s.DealerJoined && tomorrow > s.DealerSinceDay && (tomorrow - s.DealerSinceDay) % PastelCo.RebatePeriod == 0)
		{
			double amount = Math.Round(s.WeekSales * 0.1);
			if (double.IsNaN(amount) || double.IsInfinity(amount) || amount > int.MaxValue || amount < int.MinValue)
				throw new InvalidOperationException("返点金额无效，保留原销售额");
			int num = (int)amount;
			if (num > 0)
			{
				s.Orders.Add(new Order
				{
					Day = tomorrow,
					Kind = "rebate",
					Cash = num
				});
				s.WeekSales = 0.0;
				NightLog(L.T($"{c.Name}返点 {num} 信用点，送货员明天带来。", $"{c.Name} rebate: {num} credits. The courier brings it tomorrow."), "5c6f46");
			}
			else s.WeekSales = 0.0;
			try { ModLog.Info($"{c.Name}本周返点 {num}₵"); } catch { }
		}
	}
}

