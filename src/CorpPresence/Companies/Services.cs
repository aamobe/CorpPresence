using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.UI;

namespace CorpPresence;

internal static partial class Services
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
		if (num > 0)
		{
			companyState.CoopFraction -= num;
			companyState.Coop = Math.Min(100, companyState.Coop + num);
			try { ModLog.Info($"{c.Name}合作度 {coop} → {companyState.Coop}（{why}）"); } catch { }
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

	public static bool IsWaterSaleItem(GameItem item)
	{
		return item?.IsGameItemType("WATER") == true && WaterHelper.GetWaterPurity(item) > 0;
	}

	public static void OnItemWaterSold(GameItem item, int amount, StoreClient client)
	{
		if (amount <= 0 || !IsWaterSaleItem(item)) return;
		// Sales to Davre's own visitors already receive the direct company trade award.
		if (CompanyOfClient(client?.identifier ?? "")?.Id == "davre") return;
		OnWaterSold(amount);
	}

	public static void OnWaterSold(int amount)
	{
		Company davre = Companies.Find("davre");
		if (davre != null)
			AddCoop(davre, amount / (3.0 * Settings.CoopCredits), "卖出水");
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

	public static int CashDue(Company c, int amount)
	{
		return Math.Max(0, amount) - CorpPresence.Malice.Malice.VoucherDeduction(c, amount);
	}

	public static bool CanAfford(int cashDue)
	{
		PlayerStore store = SaveSlot.CurrentStore;
		return store != null && (cashDue <= 0 || store.CanPay(cashDue));
	}

	public static string Pay(Company c, int amount, string what)
	{
		PlayerStore instance = SaveSlot.CurrentStore;
		if (instance == null) return L.T("存档未就绪", "Save not ready");
		if (c != null)
		{
			CompanyState companyState = Companies.StateOf(c);
			if (companyState == null || companyState.Orders == null || companyState.Visitors == null || companyState.LimitedBought == null)
				return L.T("公司数据未就绪，尚未付款。", "Company data is not ready. No payment attempted.");
			if (Settings.MaliceMode) CorpPresence.Malice.MaliceState.Get();
			if (!SaveSlot.CanWrite("CorpPresence") || (Settings.MaliceMode && !SaveSlot.CanWrite("CorpPresence.Malice")))
				return L.T("公司数据未就绪，尚未付款。请先检查存档读取错误。", "Company data is not ready. No payment attempted. Check the save-load error first.");
		}
		// Free services still consume items or create orders: protect their save data too.
		if (amount <= 0) return null;
		Func<Company, int, string, string> f = Ext.Charge;
		if (f != null && c != null)
		{
			string text;
			try { text = f(c, amount, what); }
			catch (Exception error)
			{
				try { ModLog.Warn("公司付款回调失败，不再补扣现金：" + error.Message); } catch { }
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
		if (!instance.CanPay(amount))
		{
			return L.T("现金不够", "Not enough cash");
		}
		// Vanilla debits first, then plays audio. Sound failure must not lose the order.
		CompanyMoney.Pay(instance, amount);
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

}





