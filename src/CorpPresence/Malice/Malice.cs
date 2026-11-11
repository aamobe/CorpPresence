using System;

namespace CorpPresence.Malice;

internal static partial class Malice
{
	private static readonly string[] OurPrefixes = new string[5] { "corp_", "malice_", "envoy_", "rebel_", "solana_vip" };

	public const int FirstSubscription = 500;

	public const int Renewal = 200;

	public const int Reactivation = 100;

	public const int SubscriptionDays = 7;

	public static readonly int[] ApplyFee = new int[4] { 0, 0, 300, 800 };

	public static readonly int[] TierLimit = new int[4] { 0, 10, 5, 3 };

	public const double ServiceMarkup = 1.5;
	private const int ServiceCashCap = 15000;
	private const double ServiceExtraMarkup = 0.5;

	public const int VoucherDivisor = 20;

	private static readonly string[] OneOffServices = new string[5] { "dealer", "stamina", "canning", "forge", "bribe" };

	private static readonly string[] CorpIds = new string[5] { "pastel", "solana", "carmicheal", "meridian", "davre" };



	public static bool On => Settings.MaliceMode;

	private static int Day => StoreStation.GetDayCounter();

	// 巨企惩罚减客流时能删的顾客。保险：原版标成不能请走的重要客人（房东、剧情人物等）不删
	public static bool Cuttable(StoreClient c)
	{
		return OrdinaryVanilla(c) && c.dismissable;
	}

	public static bool OrdinaryVanilla(StoreClient c)
	{
		if (c == null || !string.IsNullOrEmpty(c.sourceModId))
		{
			return false;
		}
		string text = c.identifier ?? "";
		string[] ourPrefixes = OurPrefixes;
		foreach (string value in ourPrefixes)
		{
			if (text.StartsWith(value, StringComparison.Ordinal))
			{
				return false;
			}
		}
		if (c.clientIntent != StoreClient.ClientIntent.DIALOGUE && !c.isWanted && !c.isStealthWanted && !c.isSecurity && !c.isSentenceOfficer)
		{
			return c.hirelingData == null;
		}
		return false;
	}

	public static bool IsCorp(Company c)
	{
		if (c != null)
		{
			return Array.IndexOf(CorpIds, c.Id) >= 0;
		}
		return false;
	}

	public static bool Applies(Company c)
	{
		if (On)
		{
			return IsCorp(c);
		}
		return false;
	}

	private static CorpTerms TermsOf(Company c)
	{
		return MaliceState.Get()?.Of(c.Id);
	}

	public static bool Subscribed(Company c)
	{
		if (!SubscriptionWaived(c))
		{
			CorpTerms corpTerms = TermsOf(c);
			if (corpTerms != null)
			{
				return corpTerms.SubUntil > Day;
			}
			return false;
		}
		return true;
	}

	public static bool SubscriptionWaived(Company c)
	{
		return Applies(c) && Envoy.SubscriptionFree;
	}

	public static bool IsMembership(Company c)
	{
		return c?.Id == "solana";
	}

	public static int SubscriptionPrice(Company c)
	{
		if (SubscriptionWaived(c)) return 0;
		CorpTerms corpTerms = TermsOf(c);
		if (corpTerms == null || !corpTerms.Subscribed)
		{
			if (!IsMembership(c))
			{
				return 500;
			}
			return 200;
		}
		return 200 + (Lapsed(corpTerms) ? 100 : 0);
	}

	public static bool Lapsed(CorpTerms t)
	{
		if (t.Subscribed)
		{
			return Day - t.SubUntil >= 7;
		}
		return false;
	}

	public static string Subscribe(Company c)
	{
		// Entitlement also guards stale UI callbacks. Do not spend cash or
		// vouchers, extend paid terms, or award cooperation for a waived fee.
		if (SubscriptionWaived(c)) return null;
		CorpTerms corpTerms = TermsOf(c);
		if (corpTerms == null)
		{
			return L.K("malice.malice.subscribe.saveNotReady", "存档未就绪", "Save not ready");
		}
		if (corpTerms.SubUntil - Day >= 7)
		{
			return L.K("malice.malice.subscribe.canOnlyRenewOneWeek", "最多提前续一周。", "You can only renew one week ahead.");
		}
		int num = SubscriptionPrice(c);
		string text = Services.Pay(c, num, "subscription");
		if (text != null)
		{
			return text;
		}
		corpTerms.SubUntil = Math.Max(Day, corpTerms.SubUntil) + 7;
		corpTerms.Subscribed = true;
		Services.AddCoop(c, (double)num / Settings.CoopCredits, "订阅订购服务");
		// Payment and entitlement have committed; logging must not report failure.
		try { ModLog.Info($"恶意巨企：{c.Name}订购服务续到第 {corpTerms.SubUntil} 天，{num}₵"); } catch { }
		return null;
	}

	internal static string OrderBlocked(Company c)
	{
		if (On && c is RebelCo)
		{
			return Rebels.OrderBlocked(c);
		}
		if (On && Envoy.Punished)
		{
			return L.K("malice.malice.orderBlocked.zacchaeusSPenaltyAllCompany", "撒该的惩罚：所有公司服务暂停。打他的电话答应还款计划。", "Zacchaeus's penalty: all company services are suspended. Call him to agree to a repayment plan.");
		}
		if (Applies(c) && Envoy.OrdersRefused)
		{
			return L.K("malice.malice.orderBlocked.turnedZacchaeusDownCorporateOrdering", "你拒绝了撒该，巨企的订货暂不开放。打他的电话回心转意。", "You turned Zacchaeus down: corporate ordering is closed. Call him if you change your mind.");
		}
		if (!Applies(c) || Subscribed(c))
		{
			return null;
		}
		if (IsMembership(c))
		{
			return L.K("malice.malice.orderBlocked.requiresBasicMembershipWeekSign", $"需要{c.Name}的基础会员（{SubscriptionPrice(c)}₵ 一周），在“巨企合约”页开通。", (System.FormattableString)$"Requires a {c.Name} basic membership ({SubscriptionPrice(c)}₵ a week). Sign up on the Corporate Terms page.");
		}
		return L.K("malice.malice.orderBlocked.sOrderingServiceIsnT", $"{c.Name}的订购服务未开通或已到期，请在“巨企合约”页开通（{SubscriptionPrice(c)}₵）。", (System.FormattableString)$"{c.Name}'s ordering service isn't active. Subscribe on the Corporate Terms page ({SubscriptionPrice(c)}₵).");
	}

	internal static int CatalogTier(Company c, CompanyState s, int coopTier)
	{
		// Solana uses membership tiers; its terms page has no catalog applications.
		// Subscription and story restrictions are still enforced by OrderBlocked.
		if (Applies(c) && !IsMembership(c))
		{
			CorpTerms corpTerms = TermsOf(c);
			if (corpTerms != null)
			{
				return Math.Min(coopTier, corpTerms.Unlocked);
			}
		}
		return coopTier;
	}

	public static string ApplyUnlock(Company c)
	{
		CorpTerms corpTerms = TermsOf(c);
		CompanyState companyState = Companies.StateOf(c);
		if (corpTerms == null || companyState == null)
		{
			return L.K("malice.malice.applyUnlock.saveNotReady", "存档未就绪", "Save not ready");
		}
		int num = corpTerms.Unlocked + 1;
		if (num >= ApplyFee.Length)
		{
			return L.K("malice.malice.applyUnlock.everythingUnlocked", "已经全部解锁", "Everything is unlocked");
		}
		if (c.Tier(companyState) < num)
		{
			return L.K("composed.malice.applyUnlock.reachFirst", "合作度要先到“" + c.TierNames[num - 1] + "”", System.Runtime.CompilerServices.FormattableStringFactory.Create("Reach \u201C{0}\u201D first", new object[] { c.TierNames[num - 1] }));
		}
		string text = Services.Pay(c, ApplyFee[num], "unlock");
		if (text != null)
		{
			return text;
		}
		corpTerms.Unlocked = num;
		ModLog.Info($"恶意巨企：{c.Name}目录解锁到第 {num} 档，{ApplyFee[num]}₵");
		return null;
	}

	internal static int? Limit(CatalogEntry e)
	{
		if (!On || e == null || e.Id == null || !IsCorpEntry(e))
		{
			return null;
		}
		int num = Math.Max(1, Math.Min(3, e.MinTier));
		return TierLimit[num];
	}

	private static bool IsCorpEntry(CatalogEntry e)
	{
		string[] corpIds = CorpIds;
		for (int i = 0; i < corpIds.Length; i++)
		{
			CatalogEntry[] array = Companies.Find(corpIds[i])?.Catalog;
			if (array != null && Array.IndexOf(array, e) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	internal static int ServicePrice(Company c, string key, int basePrice)
	{
		if (!Applies(c))
		{
			return basePrice;
		}
		if (Envoy.Agent)
		{
			return 0;
		}
		if (c is SolanaCo && key == "vip")
		{
			int cash = PlayerStore.Instance?.GetCash() ?? 0;
			return basePrice + 50 + Math.Clamp(cash / 100, 0, 100);
		}
		double markup = ServiceMarkup;
		if (Array.IndexOf(OneOffServices, key) >= 0)
		{
			int cash = PlayerStore.Instance?.GetCash() ?? 0;
			markup += ServiceExtraMarkup * Math.Clamp((double)cash / ServiceCashCap, 0.0, 1.0);
		}
		return (int)Math.Round(basePrice * markup);
	}

	public static int VoucherDeduction(Company c, int amount)
	{
		if (amount <= 0 || !IsInstalled || !Applies(c)) return 0;
		var terms = MaliceState.Get()?.Corps;
		return terms != null && terms.TryGetValue(c.Id, out var balance) ? VoucherDeduction(balance, amount) : 0;
	}

	private static int VoucherDeduction(CorpTerms terms, int amount)
	{
		return Math.Min(terms.Vouchers, amount);
	}

	// null: use ordinary cash; empty: paid; nonempty: refused. Exceptions are uncertain.
	internal static string Charge(Company c, int amount, string what)
	{
		if (On && c is RebelCo)
		{
			return Rebels.Charge(amount);
		}
		if (On && Envoy.Punished && !(c is EnvoyCo))
		{
			return L.K("malice.malice.charge.zacchaeusSPenaltyCompanyServices", "撒该的惩罚：公司服务暂停。", "Zacchaeus's penalty: company services are suspended.");
		}
		if (!Applies(c))
		{
			return null;
		}
		if (IsMembership(c) && what != "subscription" && !Subscribed(c))
		{
			return OrderBlocked(c);
		}
		CorpTerms corpTerms = TermsOf(c);
		PlayerStore instance = PlayerStore.Instance;
		if (corpTerms == null || instance == null)
		{
			return null;
		}
		int voucherDeduction = VoucherDeduction(corpTerms, amount);
		int cashDue = amount - voucherDeduction;
		if (cashDue > 0 && !instance.CanPay(cashDue))
		{
			return L.K("malice.malice.charge.notEnoughCash", "现金不够", "Not enough cash");
		}
		if (cashDue > 0)
		{
			CompanyMoney.Pay(instance, cashDue);
		}
		corpTerms.Vouchers -= voucherDeduction;
		int earnedVouchers = cashDue / VoucherDivisor;
		corpTerms.Vouchers += earnedVouchers;
		if (voucherDeduction > 0 || earnedVouchers > 0)
		{
			// Payment has committed; optional logging must not change its result.
			try { ModLog.Info($"恶意巨企：{c.Name} {what} {amount}₵（代金券 {voucherDeduction}，现金 {cashDue}，返券 {earnedVouchers}）"); } catch { }
		}
		return "";
	}

	private static void OnDayStart()
	{
		MaliceState maliceState = MaliceState.Get();
		if (maliceState == null)
		{
			return;
		}
		if (maliceState.WasOn != On)
		{
			if (On)
			{
				Enabled(maliceState);
			}
			else
			{
				Disabled(maliceState);
			}
			maliceState = MaliceState.Get();
		}
		if (!On)
		{
			return;
		}
		Rebels.OnDayStart();
		string[] corpIds = CorpIds;
		foreach (string text in corpIds)
		{
			Company company = Companies.Find(text);
			if (SubscriptionWaived(company)) continue;
			if (company != null && maliceState.Corps.TryGetValue(text, out var value) && value.Subscribed)
			{
				if (value.SubUntil == Day && value.AutoRenew)
				{
					TryAutoRenew(company, value);
				}
				else if (value.SubUntil == Day + 1 && value.RemindedDay != Day)
				{
					value.RemindedDay = Day;
					Notify(L.K("malice.malice.onDayStart.sOrderingServiceExpiresTomorrow", $"{company.Name}的订购服务明天到期，续费 {200}₵。", (System.FormattableString)$"{company.Name}'s ordering service expires tomorrow. Renewal is {200}₵."));
				}
				else if (value.SubUntil == Day)
				{
					Notify(L.K("composed.malice.onDayStart.sOrderingServiceIsPausedFromToday", company.Name + "的订购服务今天起暂停。", System.Runtime.CompilerServices.FormattableStringFactory.Create("{0}\u0027s ordering service is paused from today.", new object[] { company.Name })));
				}
			}
		}
		Envoy.OnDayStart();
		RebelStory.OnDayStart();
	}

	private static void Enabled(MaliceState state)
	{
		state.WasOn = true;
		CompanyState companyState = Companies.StateOf(Companies.Find("rebel"));
		if (companyState != null)
		{
			companyState.RepSum = -1;
		}
		ModLog.Info("恶意巨企：模式打开（中途打开时，已解锁的货要重新申请）");
	}

	private static void Disabled(MaliceState state)
	{
		CompanyState companyState = Companies.StateOf(Companies.Find("spacer"));
		CompanyState companyState2 = Companies.StateOf(Companies.Find("rebel"));
		if (companyState != null && companyState2 != null && companyState2.Coop > companyState.Coop)
		{
			ModLog.Info($"恶意巨企：反抗军信任度 {companyState2.Coop} 并回星际客联盟（原 {companyState.Coop}）");
			companyState.Coop = companyState2.Coop;
		}
		MaliceState.Current = new MaliceState
		{
			RunId = state.RunId,
			WasOn = false
		};
		Notify(L.K("malice.malice.disabled.corporateMaliceOffMerchantUnion", "恶意巨企模式已关闭：商人联合和反抗军并回星际客联盟，订阅、代金券、助力点、撒该的账都清掉了。", "Corporate Malice is off: the Merchant Union and the Resistance are the Spacer Alliance again, and subscriptions, vouchers, support points and Zacchaeus's accounts are cleared."));
	}

	private static void TryAutoRenew(Company c, CorpTerms t)
	{
		if (SubscriptionWaived(c)) return;
		PlayerStore instance = PlayerStore.Instance;
		int num = SubscriptionPrice(c);
		int num2 = ((!instance.IsPropertyPaid) ? instance.GetRentValue() : 0);
		if (instance.GetCash() - Math.Max(0, num - t.Vouchers) < num2)
		{
			Notify(L.K("composed.malice.tryAutoRenew.sOrderingServiceExpiredNotRenewedAutomatically", c.Name + "的订购服务到期：现金扣完不够交房租，没有自动续费。", System.Runtime.CompilerServices.FormattableStringFactory.Create("{0}\u0027s ordering service expired: not renewed automatically, it would leave you short for rent.", new object[] { c.Name })));
			return;
		}
		string text = Subscribe(c);
		if (text != null)
		{
			Notify(L.K("composed.malice.tryAutoRenew.autoRenewalFailed", c.Name + "自动续费失败：", System.Runtime.CompilerServices.FormattableStringFactory.Create("{0} auto-renewal failed: ", new object[] { c.Name })) + text);
		}
		else
		{
			Notify(L.K("malice.malice.tryAutoRenew.sOrderingServiceWasRenewed", $"{c.Name}的订购服务已自动续费 {num}₵。", (System.FormattableString)$"{c.Name}'s ordering service was renewed automatically for {num}₵."));
		}
	}

	private static void Notify(string text)
	{
		try
		{
			StoreUIManager.Instance?.Notify(text);
		}
		catch
		{
		}
		try { ModLog.Info("恶意巨企：" + text); } catch { }
	}
}

