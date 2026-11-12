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

	private static void NightLog(string text, string color = null, Func<bool> isCurrent = null)
	{
		try
		{
			if (isCurrent != null && !isCurrent()) return;
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
			StoreSession session = StoreSession.Capture(store, root);
			bool IsCurrent() => session.IsCurrent;
			bool CanSettle() => !StoreSession.CompanyProgressPaused && IsCurrent() && SaveSlot.CanWrite("CorpPresence") && (!Settings.MaliceMode || SaveSlot.CanWrite("CorpPresence.Malice"));
			if (Settings.MaliceMode) CorpPresence.Malice.MaliceState.Get();
			if (!SaveSlot.CanWrite("CorpPresence") || (Settings.MaliceMode && !SaveSlot.CanWrite("CorpPresence.Malice")))
			{
				if (IsCurrent()) RescueCrates(IsCurrent);
				return;
			}
			foreach (Company company in Companies.All)
			{
				if (!CanSettle()) break;
				CompanyState state;
				try
				{
					state = Companies.StateOf(company);
					if (state?.Orders == null || state.Visitors == null || state.LimitedBought == null) continue;
					if (!CanSettle() || !root.Companies.TryGetValue(company.Id, out var current) || !ReferenceEquals(current, state)) break;
					if (!NightSettlement.TryBegin(state, day)) continue;
				}
				catch (Exception error) { WarnNightService("读取夜间公司状态", error); continue; }
				bool CanSettleCompany() => CanSettle() && root.Companies != null
					&& root.Companies.TryGetValue(company.Id, out var current) && ReferenceEquals(current, state);
				try
				{
					if (CanSettleCompany() && state.TrafficOn) Nightly(company, state, company is SpacerCo ? SpacerCo.MutualFee : PastelCo.TrafficFee, "traffic", day, CanSettleCompany);
				}
				catch (Exception error) { WarnNightService("夜间服务扣费", error); }
				if (!CanSettleCompany()) break;
				state.VipOn = false;
				try { Rebate(company, state, day + 1, CanSettleCompany); }
				catch (Exception error) { WarnNightService("登记公司返点", error); }
			}
			if (IsCurrent()) RescueCrates(IsCurrent);
		}
		finally { _nightlyRunning = false; }
	}

	private static void WarnNightService(string stage, Exception error)
	{
		try { ModLog.Warn(stage + "失败：" + error.Message); } catch { }
	}

	private static void RescueCrates(Func<bool> isCurrent)
	{
		try
		{
			if (!isCurrent()) return;
			var emporium = EmporiumEntry.Instance;
			GameGridInventory gameGridInventory = emporium?.frontInvinvElement;
			if (gameGridInventory == null)
			{
				return;
			}
			int num = 0;
			int num2 = 0;
			foreach (GameItem item in gameGridInventory.childItems.ToList())
			{
				if (!isCurrent() || !ReferenceEquals(EmporiumEntry.Instance, emporium)) return;
				string text = Goods.IdOf(item);
				if (text != null && text.StartsWith("crate_"))
				{
					if (!isCurrent()) return;
					GraphUtils.EmporiumTryAdd(emporium.invElement, item);
					if (!isCurrent()) return;
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
			if (!isCurrent()) return;
			if (num > 0)
			{
				NightLog(L.K("companies.services.nightly.rescueCrates.deliveryCrateSLeftCounter", $"柜台上的 {num} 只送货箱已收进背包。", (System.FormattableString)$"{num} delivery crate(s) left on the counter were moved into your bag."), isCurrent: isCurrent);
			}
			if (num2 > 0)
			{
				NightLog(L.K("companies.services.nightly.rescueCrates.bagFullDeliveryCrateS", $"背包放不下，柜台上还有 {num2} 只送货箱，过夜会被清掉。", (System.FormattableString)$"Your bag is full: {num2} delivery crate(s) remain on the counter and will be cleared overnight."), "9c3738", isCurrent);
			}
		}
		catch (Exception ex)
		{
			WarnNightService("收起送货箱", ex);
		}
	}

	private static void Nightly(Company c, CompanyState s, int fee, string kind, int day, Func<bool> isCurrent)
	{
		if (!isCurrent()) return;
		string text;
		if (kind == "vip")
		{
			text = L.K("companies.services.nightly.nightly.vIPReferral", "贵宾引荐", "VIP Referral");
		}
		else
		{
			text = ((c is SpacerCo) ? L.K("companies.services.nightly.nightly.mutualAid", "街坊互助", "Mutual Aid") : L.K("companies.services.nightly.nightly.lowerFootTraffic", "下层客流", "Lower Foot Traffic"));
		}
		Visitor visitor = new Visitor { Day = day + 1, Kind = kind };
		string paymentError = Pay(c, fee, kind);
		if (!isCurrent()) return;
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
			NightLog(L.K("companies.services.nightly.nightly.servicePaused", c.Name + "的" + text + "未结算，已暂停：" + paymentError, (System.FormattableString)$"{c.Name} {text} paused: {paymentError}"), "9c3738", isCurrent);
			try { ModLog.Info(c.Name + "的" + text + "已暂停：" + paymentError); } catch { }
		}
		else
		{
			s.Visitors.Add(visitor);
			string text2;
			if (kind == "vip")
			{
				text2 = L.K("companies.services.nightly.nightly.upperLevelsVIPWillVisit", "明天会有一位上层区贵宾光顾店铺", "An Upper Levels VIP will visit tomorrow");
			}
			else
			{
				text2 = ((c is SpacerCo) ? L.K("companies.services.nightly.nightly.moreSpacersWillComeBy", "明天会多来几位星际客", "More Spacers will come by tomorrow") : L.K("companies.services.nightly.nightly.moreLowerLevelsCustomersWill", "明天会多来几位下层区顾客", "More Lower Levels customers will come by tomorrow"));
			}
			NightLog(L.K("companies.services.nightly.nightly.paidCredits", $"{c.Name}{text}已付 {fee} 信用点，{text2}。", (System.FormattableString)$"{c.Name} {text}: paid {fee} credits. {text2}."), isCurrent: isCurrent);
			try { ModLog.Info($"{c.Name}{text}：扣 {fee}₵，第 {day + 1} 天生效"); } catch { }
		}
	}

	private static void Rebate(Company c, CompanyState s, int tomorrow, Func<bool> isCurrent)
	{
		if (!isCurrent()) return;
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
				NightLog(L.K("companies.services.nightly.rebate.rebateCreditsCourierBringsTomorrow", $"{c.Name}返点 {num} 信用点，送货员明天带来。", (System.FormattableString)$"{c.Name} rebate: {num} credits. The courier brings it tomorrow."), "5c6f46", isCurrent);
			}
			else s.WeekSales = 0.0;
			try { ModLog.Info($"{c.Name}本周返点 {num}₵"); } catch { }
		}
	}
}

