using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.Malice;
using CorpPresence.UI;

namespace CorpPresence;

internal static class Services
{
	public const string StoryReason = "剧情推进";

	private static int _closedDay = -1;

	[ThreadStatic]
	private static bool _nightlyRunning;

	private const string Warn = "9c3738";

	private const string Good = "5c6f46";

	private static int Day => StoreStation.GetDayCounter();

	public static int Week => Day / (7 * Settings.RestockWeeks);

	public static int NextRestockDay => (Week + 1) * 7 * Settings.RestockWeeks;

	public static string PeriodWord
	{
		get
		{
			if (Settings.RestockWeeks != 1)
			{
				return L.T("本期", "this period");
			}
			return L.T("本周", "this week");
		}
	}

	public static string PeriodKey => Settings.RestockWeeks + "w" + Week;

	public static void AddCoop(Company c, double points, string why)
	{
		CompanyState companyState = Companies.StateOf(c);
		if (companyState == null || points <= 0.0 || companyState.Coop >= 100)
		{
			return;
		}
		if (Settings.CoopDailyCap && why != "剧情推进")
		{
			if (companyState.CoopGainDay != Day)
			{
				companyState.CoopGainDay = Day;
				companyState.CoopGainToday = 0.0;
			}
			points = Math.Min(points, 10.0 - companyState.CoopGainToday);
			if (points <= 0.0)
			{
				return;
			}
			companyState.CoopGainToday += points;
		}
		int coop = companyState.Coop;
		companyState.CoopFraction += points;
		int num = (int)Math.Floor(companyState.CoopFraction);
		if (num <= 0)
		{
			return;
		}
		companyState.CoopFraction -= num;
		companyState.Coop = Math.Min(100, companyState.Coop + num);
		try
		{
			ModLog.Info($"{c.Name}合作度 {coop} → {companyState.Coop}（{why}）");
		}
		catch
		{
		}
	}

	public static void OnSoldToClient(StoreClient client, GameItem item, int amount)
	{
		Company company = CompanyOfClient(client?.identifier ?? "");
		Brand.Record(item);
		PastelCo.CountLoose(item);
		SilverBlood.OnSold(client, item);
		if (company != null)
		{
			AddCoop(company, (double)amount / Settings.CoopCredits, "卖货给公司的人");
		}
		Company[] all = Companies.All;
		foreach (Company company2 in all)
		{
			if (company2.Owns(item))
			{
				if (company2 != company)
				{
					AddCoop(company2, (double)amount / (3.0 * Settings.CoopCredits), "卖出本公司产品");
				}
				CompanyState companyState = Companies.StateOf(company2);
				if (companyState != null && companyState.DealerJoined)
				{
					companyState.WeekSales += amount;
				}
			}
		}
		Action<StoreClient, GameItem, int> sold = Ext.SoldToClient;
		if (sold != null)
		{
			Ext.Fire("SoldToClient", () =>
			{
				sold(client, item, amount);
			});
		}
	}

	public static void OnBoughtFromClient(StoreClient client, GameItem item, int cost)
	{
		Company company = CompanyOfClient(client?.identifier ?? "");
		Brand.Record(item);
		if (company != null)
		{
			AddCoop(company, (double)cost / Settings.CoopCredits, "从公司的人手里进货");
		}
	}

	public static void OnChapterDone(string charId)
	{
		Company company = Companies.ByCharacter(charId);
		if (company != null)
		{
			AddCoop(company, 5.0, "剧情推进");
		}
		if (company != null)
		{
			ChapterFloor(company, Companies.StateOf(company));
		}
		CompanyState companyState = ((company is CarmichaelCo) ? Companies.StateOf(company) : null);
		if (companyState != null && companyState.StaminaLastDay < 0 && CarmichaelCo.StaminaOpen())
		{
			companyState.StaminaLastDay = Day;
		}
		if (company is CarmichaelCo)
		{
			CorpState corpState = CorpState.Get();
			if (corpState != null && corpState.Of(charId).SideDone.Contains(6))
			{
				CarmichaelCo.GrantLicense();
			}
		}
		if (company is SpacerCo)
		{
			SpacerCo.OnMentorChapter(CorpState.Get()?.Of(charId).Chapter ?? 0);
		}
	}

	public static Company CompanyOfClient(string id)
	{
		if (!id.StartsWith("corp_"))
		{
			return null;
		}
		Company[] all = Companies.All;
		foreach (Company company in all)
		{
			if (id.StartsWith("corp_" + company.Id + "_") || id.StartsWith("corp_" + company.CharacterId + "_"))
			{
				return company;
			}
		}
		return null;
	}

	private static int ChapterOf(Company c)
	{
		return CorpState.Get()?.Of(c.CharacterId).Chapter ?? 0;
	}

	public static void ChapterFloor(Company c, CompanyState s)
	{
		if (s != null && s.Coop < 20 && ChapterOf(c) >= 1)
		{
			ModLog.Info($"{c.Name}合作度 {s.Coop} → {20}（第 1 章来访）");
			s.Coop = 20;
		}
	}

	public static bool CardDue(Company c, CompanyState s)
	{
		if (s != null && s.CardCourierDay == Day)
		{
			return false;
		}
		ChapterFloor(c, s);
		if (s != null && !s.CardGiven && s.Coop >= 20)
		{
			return ChapterOf(c) >= 1;
		}
		return false;
	}

	public static void MaybeGiveCard(string charId)
	{
		Company company = Companies.ByCharacter(charId);
		CompanyState companyState = ((company == null) ? null : Companies.StateOf(company));
		if (companyState == null || companyState.CardGiven || companyState.Coop < 20)
		{
			return;
		}
		try
		{
			GameItem gameItem = Cards.Make(company);
			if (gameItem == null)
			{
				ModLog.Warn(company.Name + "的名片没生成出来，下次再给");
				return;
			}
			if (!PlayerStore.Instance.TryAddDirectSellingItemToTable(gameItem, isOwend: true))
			{
				ModLog.Info("柜台放满了，" + company.Name + "的名片明天由跑腿送来");
				return;
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("名片生成失败，下次再给：" + ex.Message);
			return;
		}
		companyState.CardGiven = true;
		Phone.Activate(company);
		try
		{
			Notebook.Refresh();
		}
		catch
		{
		}
		ModLog.Info($"拿到{company.Name}的名片，电话 {company.PhoneNumber}");
	}

	public static int Fee(string companyId, string key, int basePrice)
	{
		Func<Company, string, int, int> f = Ext.ServicePrice;
		if (f == null)
		{
			return basePrice;
		}
		Company c = Companies.Find(companyId);
		if (c != null)
		{
			return Math.Max(0, Ext.Ask("ServicePrice", () => f(c, key, basePrice), basePrice));
		}
		return basePrice;
	}

	public static string Pay(Company c, int amount, string what)
	{
		PlayerStore currentStore = SaveSlot.CurrentStore;
		if (currentStore == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		if (c != null)
		{
			CompanyState companyState = Companies.StateOf(c);
			if (companyState == null || companyState.Orders == null || companyState.Visitors == null || companyState.LimitedBought == null)
			{
				return L.T("公司数据未就绪，尚未付款。", "Company data is not ready. No payment attempted.");
			}
			if (Settings.MaliceMode)
			{
				MaliceState.Get();
			}
			if (!SaveSlot.CanWrite("CorpPresence") || (Settings.MaliceMode && !SaveSlot.CanWrite("CorpPresence.Malice")))
			{
				return L.T("公司数据未就绪，尚未付款。请先检查存档读取错误。", "Company data is not ready. No payment attempted. Check the save-load error first.");
			}
		}
		if (amount <= 0)
		{
			return null;
		}
		Func<Company, int, string, string> charge = Ext.Charge;
		if (charge != null && c != null)
		{
			string text;
			try
			{
				text = charge(c, amount, what);
			}
			catch (Exception ex)
			{
				try
				{
					ModLog.Warn("公司付款回调失败，不再补扣现金：" + ex.Message);
				}
				catch
				{
				}
				return L.T("付款状态异常，请先核对余额，避免重复付款。", "Payment status is uncertain. Check your balance before trying again.");
			}
			if (text != null)
			{
				if (!(text == ""))
				{
					return text;
				}
				return null;
			}
		}
		if (!currentStore.CanPay(amount))
		{
			return L.T("现金不够", "Not enough cash");
		}
		CompanyMoney.Pay(currentStore, amount);
		return null;
	}

	public static string JoinDealer(Company c)
	{
		CompanyState companyState = Companies.StateOf(c);
		if (companyState.DealerJoined)
		{
			return L.T("已经是合作商了", "Already a dealer");
		}
		string text = Pay(c, PastelCo.DealerFee, "dealer");
		if (text != null)
		{
			return text;
		}
		companyState.DealerJoined = true;
		companyState.DealerSinceDay = Day;
		companyState.WeekSales = 0.0;
		AddCoop(c, (double)PastelCo.DealerFee / Settings.CoopCredits, "加盟合作商");
		return null;
	}

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
			PlayerStore currentStore = SaveSlot.CurrentStore;
			if (currentStore != null)
			{
				if (color == null)
				{
					currentStore.AddNightLog(text);
				}
				else
				{
					currentStore.AddNightLog(text, color);
				}
			}
		}
		catch
		{
		}
	}

	public static void OnNightlyServices()
	{
		if (_nightlyRunning || !GameResources.Ready)
		{
			return;
		}
		_nightlyRunning = true;
		try
		{
			PlayerStore store = SaveSlot.CurrentStore;
			if (store == null)
			{
				return;
			}
			int num = ((_closedDay >= 0) ? _closedDay : Day);
			_closedDay = -1;
			CorpState root = CorpState.Get();
			if (root == null)
			{
				return;
			}
			string run = store.runID;
			long generation = GameResources.Generation;
			if (Settings.MaliceMode)
			{
				MaliceState.Get();
			}
			if (!SaveSlot.CanWrite("CorpPresence") || (Settings.MaliceMode && !SaveSlot.CanWrite("CorpPresence.Malice")))
			{
				// Moving existing crates is allowed even when company progression is protected.
				if (StoreSession.Matches(store, root, run, generation))
				{
					RescueCrates();
				}
				return;
			}
			Company[] all = Companies.All;
			foreach (Company company in all)
			{
				if (!IsCurrent())
				{
					break;
				}
				CompanyState companyState;
				try
				{
					companyState = Companies.StateOf(company);
					if (companyState?.Orders != null && companyState.Visitors != null && companyState.LimitedBought != null)
					{
						if (!IsCurrent())
						{
							break;
						}
						if (NightSettlement.TryBegin(companyState, num))
						{
							goto IL_013e;
						}
					}
				}
				catch (Exception error)
				{
					WarnNightService("读取夜间公司状态", error);
				}
				continue;
				IL_013e:
				try
				{
					if (companyState.TrafficOn)
					{
						Nightly(company, companyState, (company is SpacerCo) ? SpacerCo.MutualFee : PastelCo.TrafficFee, "traffic", num);
					}
				}
				catch (Exception error2)
				{
					WarnNightService("夜间服务扣费", error2);
				}
				if (!IsCurrent())
				{
					break;
				}
				companyState.VipOn = false;
				try
				{
					Rebate(company, companyState, num + 1);
				}
				catch (Exception error3)
				{
					WarnNightService("登记公司返点", error3);
				}
			}
			if (IsCurrent())
			{
				RescueCrates();
			}
			bool IsCurrent()
			{
				return StoreSession.Matches(store, root, run, generation)
					&& SaveSlot.CanWrite("CorpPresence")
					&& (!Settings.MaliceMode || SaveSlot.CanWrite("CorpPresence.Malice"));
			}
		}
		finally
		{
			_nightlyRunning = false;
		}
	}

	private static void WarnNightService(string stage, Exception error)
	{
		try
		{
			ModLog.Warn(stage + "失败：" + error.Message);
		}
		catch
		{
		}
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
		catch (Exception error)
		{
			WarnNightService("收起送货箱", error);
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
		Visitor item = new Visitor
		{
			Day = day + 1,
			Kind = kind
		};
		string text2 = Pay(c, fee, kind);
		if (text2 != null)
		{
			if (kind == "traffic")
			{
				s.TrafficOn = false;
			}
			else
			{
				s.VipOn = false;
			}
			NightLog(L.T(c.Name + "的" + text + "未结算，已暂停：" + text2, c.Name + " " + text + " paused: " + text2), "9c3738");
			try
			{
				ModLog.Info(c.Name + "的" + text + "已暂停：" + text2);
				return;
			}
			catch
			{
				return;
			}
		}
		s.Visitors.Add(item);
		string text3;
		if (kind == "vip")
		{
			text3 = L.T("明天会有一位上层区贵宾光顾店铺", "An Upper Levels VIP will visit tomorrow");
		}
		else
		{
			text3 = ((c is SpacerCo) ? L.T("明天会多来几位星际客", "More Spacers will come by tomorrow") : L.T("明天会多来几位下层区顾客", "More Lower Levels customers will come by tomorrow"));
		}
		NightLog(L.T($"{c.Name}{text}已付 {fee} 信用点，{text3}。", $"{c.Name} {text}: paid {fee} credits. {text3}."));
		try
		{
			ModLog.Info($"{c.Name}{text}：扣 {fee}₵，第 {day + 1} 天生效");
		}
		catch
		{
		}
	}

	private static void Rebate(Company c, CompanyState s, int tomorrow)
	{
		if (s.DealerJoined && tomorrow > s.DealerSinceDay && (tomorrow - s.DealerSinceDay) % PastelCo.RebatePeriod == 0)
		{
			double num = Math.Round(s.WeekSales * 0.1);
			if (double.IsNaN(num) || double.IsInfinity(num) || num > 2147483647.0 || num < -2147483648.0)
			{
				throw new InvalidOperationException("返点金额无效，保留原销售额");
			}
			int num2 = (int)num;
			if (num2 > 0)
			{
				s.Orders.Add(new Order
				{
					Day = tomorrow,
					Kind = "rebate",
					Cash = num2
				});
				s.WeekSales = 0.0;
				NightLog(L.T($"{c.Name}返点 {num2} 信用点，送货员明天带来。", $"{c.Name} rebate: {num2} credits. The courier brings it tomorrow."), "5c6f46");
			}
			else
			{
				s.WeekSales = 0.0;
			}
			try
			{
				ModLog.Info($"{c.Name}本周返点 {num2}₵");
			}
			catch
			{
			}
		}
	}

	public static bool CanOrderToday(Company c)
	{
		return Companies.StateOf(c)?.LastOrderDay != Day;
	}

	public static string OrderBlocked(Company c)
	{
		Func<Company, string> f = Ext.OrderBlocked;
		if (f != null && c != null)
		{
			return Ext.Ask("OrderBlocked", () => f(c), null);
		}
		return null;
	}

	public static string PlaceOrder(Company c, Dictionary<CatalogEntry, int> cart, Func<CatalogEntry, int> unitPrice)
	{
		CompanyState s = Companies.StateOf(c);
		if (s == null || s.Orders == null || s.LimitedBought == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		if (s.LastOrderDay == Day)
		{
			return L.T("今天已经下过单了", "Already ordered today");
		}
		string text = OrderBlocked(c);
		if (text != null)
		{
			return text;
		}
		if (cart == null || unitPrice == null)
		{
			return L.T("订单未就绪，尚未扣款。", "Order not ready. You have not been charged.");
		}
		KeyValuePair<CatalogEntry, int>[] array = cart.ToArray();
		if (array.Any((KeyValuePair<CatalogEntry, int> kv) => kv.Key == null || kv.Value <= 0 || kv.Key.BaseValue() < 0))
		{
			return L.T("有货品暂不可用，请稍后再试。尚未扣款。", "Some items are unavailable right now. Try again later. You have not been charged.");
		}
		CatalogEntry key = array.FirstOrDefault((KeyValuePair<CatalogEntry, int> kv) => kv.Value > Left(s, kv.Key)).Key;
		if (key != null)
		{
			return L.T($"{key.Name()}超出限购，还能订 {Left(s, key)} 份", $"{key.Name()} is over the limit. You can order {Left(s, key)} more");
		}
		int total = 0;
		int day = Day;
		string periodKey = PeriodKey;
		(string, int, int, string)[] array2 = new (string, int, int, string)[array.Length];
		int num = 0;
		Order order = new Order
		{
			Day = day + 1
		};
		KeyValuePair<CatalogEntry, int>[] array3 = array;
		for (int num2 = 0; num2 < array3.Length; num2++)
		{
			KeyValuePair<CatalogEntry, int> keyValuePair = array3[num2];
			string id = keyValuePair.Key.Id;
			int windowDays = keyValuePair.Key.WindowDays;
			int bundle = keyValuePair.Key.Bundle;
			if (string.IsNullOrEmpty(id))
			{
				return L.T("货品标识未就绪，尚未扣款。", "Item identifier not ready. You have not been charged.");
			}
			string text2 = QuotaCounts.Key(id, windowDays, day, periodKey);
			if (!QuotaCounts.TryAdd(s.LimitedBought, text2, keyValuePair.Value, out var _))
			{
				return L.T("历史订购计数异常或超出范围，尚未扣款。", "Purchase history is invalid or out of range. You have not been charged.");
			}
			if (!OrderAmounts.TryAdd(ref total, unitPrice(keyValuePair.Key), keyValuePair.Value, bundle, out var itemCount))
			{
				return L.T("订单数量或金额无效，尚未扣款。", "Invalid order quantity or amount. You have not been charged.");
			}
			for (int num3 = 0; num3 < itemCount; num3++)
			{
				order.Items.Add(id);
			}
			array2[num++] = (id, windowDays, keyValuePair.Value, text2);
		}
		if (total <= 0)
		{
			return L.T("还没有选货", "Nothing selected yet");
		}
		string text3 = Pay(c, total, "order");
		if (text3 != null)
		{
			return text3;
		}
		s.Orders.Add(order);
		s.LastOrderDay = day;
		(string, int, int, string)[] array4 = array2;
		for (int num2 = 0; num2 < array4.Length; num2++)
		{
			(string, int, int, string) tuple = array4[num2];
			RecordLimited(s, tuple.Item1, tuple.Item2, tuple.Item3, day, periodKey, tuple.Item4);
		}
		AddCoop(c, (double)total / Settings.CoopCredits, "电话订货");
		try
		{
			ModLog.Info($"{c.Name}订单：{order.Items.Count} 件，{total}₵，第 {order.Day} 天送达");
		}
		catch
		{
		}
		return null;
	}

	private static int PeriodIndex(string key, int fallback)
	{
		return Day / Settings.Period(key, fallback);
	}

	public static int LimitedUsed(CompanyState s, CatalogEntry e)
	{
		if (e != null)
		{
			return QuotaCounts.Used(s?.LimitedBought, e.Id, e.WindowDays, Day, (e.WindowDays <= 0) ? PeriodKey : null);
		}
		return int.MaxValue;
	}

	public static int Left(CompanyState s, CatalogEntry e)
	{
		if (s?.LimitedBought == null || e == null)
		{
			return 0;
		}
		int num = e.Limit();
		if (num <= 0)
		{
			return int.MaxValue;
		}
		return (int)Math.Max(0L, (long)num - (long)LimitedUsed(s, e));
	}

	public static string LeftText(CompanyState s, CatalogEntry e)
	{
		int num = Left(s, e);
		if (num != int.MaxValue)
		{
			if (e.WindowDays <= 0)
			{
				return L.T($"\u3000{PeriodWord}剩 {num}", $"  {num} left {PeriodWord}");
			}
			return L.T($"\u3000剩余 {num}", $"  {num} left");
		}
		return "";
	}

	private static void RecordLimited(CompanyState s, string id, int windowDays, int count, int day, string period, string preparedKey = null)
	{
		bool flag = windowDays > 0;
		string text = id + "@d";
		string text2 = (flag ? text : (id + "@"));
		string text3 = preparedKey ?? QuotaCounts.Key(id, windowDays, day, period);
		if (!QuotaCounts.TryAdd(s.LimitedBought, text3, count, out var next))
		{
			try
			{
				ModLog.Warn("限购计数在付款期间发生异常，保留已登记订单及原记录：" + text3);
				return;
			}
			catch
			{
				return;
			}
		}
		s.LimitedBought[text3] = next;
		List<string> list = null;
		foreach (string key in s.LimitedBought.Keys)
		{
			bool num;
			if (!flag)
			{
				if (!key.StartsWith(text2) || !(key != text3))
				{
					continue;
				}
				num = !key.StartsWith(text);
			}
			else
			{
				if (!key.StartsWith(text2) || !int.TryParse(key.Substring(text2.Length), out var result))
				{
					continue;
				}
				num = result <= (long)day - (long)windowDays;
			}
			if (num)
			{
				(list ?? (list = new List<string>())).Add(key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (string item in list)
		{
			s.LimitedBought.Remove(item);
		}
	}

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
		int num2;
		try
		{
			int swapFee = CarmichaelCo.SwapFee;
			long num = (long)list.Count * (long)swapFee;
			if (swapFee < 0 || num > int.MaxValue)
			{
				return L.T("换货费用无效，尚未扣款。", "Invalid exchange fee. You have not been charged.");
			}
			num2 = (int)num;
			foreach (GameItem item in list)
			{
				string text = CarmichaelCo.SwapTarget(item);
				if (item == null || string.IsNullOrEmpty(text))
				{
					return L.T("换货明细未就绪，尚未扣款。", "Exchange details are not ready. You have not been charged.");
				}
				order.Items.Add(text);
			}
		}
		catch
		{
			return L.T("换货明细未就绪，尚未扣款。", "Exchange details are not ready. You have not been charged.");
		}
		string text2 = Pay(c, num2, "swap");
		if (text2 != null)
		{
			return text2;
		}
		foreach (GameItem item2 in list)
		{
			try
			{
				item2.Destroy();
			}
			catch (Exception ex)
			{
				try
				{
					ModLog.Warn("收走赝品失败：" + ex.Message);
				}
				catch
				{
				}
			}
		}
		companyState.Orders.Add(order);
		companyState.SwapLastDay = Day;
		ModLog.Info($"赝品换正品：{list.Count} 件，手续费 {num2}₵，第 {order.Day} 天送达");
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
		int num4;
		try
		{
			long num = 0L;
			foreach (GameItem item in list)
			{
				int num2 = MeridianCo.ConditionOf(item);
				if (item == null || string.IsNullOrEmpty(item.identifier) || num2 < 0 || num2 >= 3)
				{
					return L.T("翻新明细未就绪，尚未扣款。", "Refurbishment details are not ready. You have not been charged.");
				}
				int num3 = MeridianCo.RefurbishFee(item);
				num += num3;
				if (num3 < 0 || num > int.MaxValue)
				{
					return L.T("翻新费用无效，尚未扣款。", "Invalid refurbishment fee. You have not been charged.");
				}
				order.Items.Add($"{item.identifier}|{num2 + 1}");
			}
			num4 = (int)num;
		}
		catch
		{
			return L.T("翻新明细未就绪，尚未扣款。", "Refurbishment details are not ready. You have not been charged.");
		}
		string text = Pay(c, num4, "refurbish");
		if (text != null)
		{
			return text;
		}
		foreach (GameItem item2 in list)
		{
			try
			{
				item2.Destroy();
			}
			catch (Exception ex)
			{
				try
				{
					ModLog.Warn("收走待翻新的枪失败：" + ex.Message);
				}
				catch
				{
				}
			}
		}
		companyState.Orders.Add(order);
		companyState.RefurbishLastDay = Day;
		AddCoop(c, (double)num4 / Settings.CoopCredits, "返厂翻新");
		ModLog.Info($"返厂翻新：{list.Count} 把，{num4}₵，第 {order.Day} 天送回");
		return null;
	}

	public static List<string> LabListThisWeek(CompanyState s)
	{
		int num = PeriodIndex("lab", 7);
		if (s.LabWeek != num || s.LabList.Count == 0)
		{
			Random rng = new Random(num * 7919 + 13);
			List<string> list = new List<string> { "blue_blood_bag" };
			foreach (string item in SolanaCo.LabPool.OrderBy((string _) => rng.Next()).Take(2))
			{
				list.Add(item);
			}
			s.LabList = list;
			s.LabWeek = num;
			s.LabBooked = false;
		}
		return s.LabList;
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
		int vipFee = SolanaCo.VipFee;
		Visitor visitor = new Visitor
		{
			Day = day + 1,
			Kind = "vip"
		};
		string text = Pay(c, vipFee, "vip");
		if (text != null)
		{
			return text;
		}
		companyState.Visitors.Add(visitor);
		companyState.VipBookedDay = day;
		try
		{
			ModLog.Info($"预约索拉纳贵宾：{vipFee}₵，第 {visitor.Day} 天来");
		}
		catch
		{
		}
		return null;
	}

	public static string BookLab(Company c)
	{
		CompanyState companyState = Companies.StateOf(c);
		if (companyState?.Visitors == null || companyState.LabList == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		string text = OrderBlocked(c);
		if (text != null)
		{
			return text;
		}
		string text2 = Pay(c, 0, "lab");
		if (text2 != null)
		{
			return text2;
		}
		try
		{
			LabListThisWeek(companyState);
		}
		catch
		{
			return L.T("采购清单暂未就绪", "Procurement list not ready");
		}
		if (companyState.LabBooked)
		{
			return L.T("这一期已经预约过了", "Already booked for this list");
		}
		companyState.Visitors.Add(new Visitor
		{
			Day = Day + 1,
			Kind = "lab"
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
		if (companyState?.Orders == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		string text = OrderBlocked(c);
		if (text != null)
		{
			return text;
		}
		string text2 = Pay(c, 0, "gift");
		if (text2 != null)
		{
			return text2;
		}
		int num = PeriodIndex("gift", 7);
		if (companyState.GiftWeek == num)
		{
			return L.T("这一期已经领过了", "Already claimed for this period");
		}
		Order item;
		try
		{
			string text3 = Engine.Any("neuroactive_perfume", "@elixir", "@mask");
			if (string.IsNullOrEmpty(text3))
			{
				return L.T("赠礼暂未就绪", "Gift not ready");
			}
			item = new Order
			{
				Day = Day + 1,
				Kind = "gift",
				Items = new List<string> { text3 }
			};
		}
		catch
		{
			return L.T("赠礼暂未就绪", "Gift not ready");
		}
		companyState.Orders.Add(item);
		companyState.GiftWeek = num;
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
		int staminaFee = CarmichaelCo.StaminaFee;
		Order order = new Order
		{
			Day = day + 1,
			Kind = "stamina",
			Items = new List<string> { "@stamina" }
		};
		string text = Pay(c, staminaFee, "stamina");
		if (text != null)
		{
			return text;
		}
		companyState.Orders.Add(order);
		companyState.StaminaLastDay = day;
		try
		{
			ModLog.Info($"体力增强剂申领：手续费 {staminaFee}₵，第 {order.Day} 天送达");
		}
		catch
		{
		}
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
		string key = "leviathan@" + PeriodKey;
		if ((companyState.LimitedBought.TryGetValue(key, out var value) ? value : 0) >= c.LeviathanLimit(companyState))
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
		Order order = new Order
		{
			Day = Day + 1,
			Items = new List<string> { "@leviathan" }
		};
		string text2 = Pay(c, price, "leviathan");
		if (text2 != null)
		{
			return text2;
		}
		companyState.Orders.Add(order);
		companyState.LimitedBought[key] = (companyState.LimitedBought.TryGetValue(key, out var value2) ? value2 : 0) + 1;
		AddCoop(c, (double)price / Settings.CoopCredits, "订购利维坦");
		try
		{
			ModLog.Info($"利维坦净水器订单：{price}₵，第 {order.Day} 天送达");
		}
		catch
		{
		}
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
			List<string> list = DavreCo.CanningMachines.Select(((string Id, string Goods) m) => m.Id).ToList();
			if (list.Count == 0 || list.Any(string.IsNullOrEmpty))
			{
				return L.T("设备明细未就绪，尚未扣款。", "Equipment details not ready. You have not been charged.");
			}
			order = new Order
			{
				Day = Day + 1,
				Items = list
			};
		}
		catch
		{
			return L.T("设备明细未就绪，尚未扣款。", "Equipment details not ready. You have not been charged.");
		}
		int canningFee = DavreCo.CanningFee;
		string text = Pay(c, canningFee, "canning");
		if (text != null)
		{
			return text;
		}
		companyState.Orders.Add(order);
		companyState.CanningOwned = true;
		try
		{
			ModLog.Info($"罐装设备补办：{canningFee}₵，第 {order.Day} 天送达两台机器");
		}
		catch
		{
		}
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
		if (price <= 0 || string.IsNullOrEmpty(id) || DavreCo.CanningMachines == null || !DavreCo.CanningMachines.Any(((string Id, string Goods) m) => m.Id == id))
		{
			return L.T("设备暂不可用，尚未扣款。", "Equipment unavailable. You have not been charged.");
		}
		string text = OrderBlocked(c);
		if (text != null)
		{
			return text;
		}
		Order order = new Order
		{
			Day = Day + 1,
			Items = new List<string> { id }
		};
		string text2 = Pay(c, price, "canning_machine");
		if (text2 != null)
		{
			return text2;
		}
		companyState.Orders.Add(order);
		AddCoop(c, (double)price / Settings.CoopCredits, "订购罐装设备");
		try
		{
			ModLog.Info($"罐装设备订单：{id}，{price}₵，第 {order.Day} 天送达");
		}
		catch
		{
		}
		return null;
	}
}
