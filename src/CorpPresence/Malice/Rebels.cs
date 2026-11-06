using System;
using System.Collections;
using CorpPresence.UI;

namespace CorpPresence.Malice;

internal static class Rebels
{
	public const string Faction = "FACTION_REVOLUTION";

	public const string FenceId = "malice_fence";

	public const int PointDivisor = 20;

	public const int FenceGap = 5;

	public const int FenceBase = 50;

	public const int FenceStep = 25;

	public const int FenceMax = 150;

	public const int FenceBudgetCut = 200;

	public const double FencePriceFactor = 1.1;

	private static int Day => StoreStation.GetDayCounter();

	public static RebelCo Company => (RebelCo)Companies.Find("rebel");

	public static bool Arrested => MaliceState.Get()?.OdinArrested ?? false;

	internal static bool OdinUnavailable => Arrested || Cut || Blacklisted;

	public static bool Blacklisted
	{
		get
		{
			try
			{
				return StoreReputation.IsPerkUnlocked("REV_BLACKLISTED");
			}
			catch
			{
				return false;
			}
		}
	}

	public static bool Cut
	{
		get
		{
			MaliceState maliceState = MaliceState.Get();
			if (maliceState == null)
			{
				return false;
			}
			return maliceState.Envoy?.RebelCut == true;
		}
	}

	public static bool FenceClosed
	{
		get
		{
			EnvoyState envoyState = MaliceState.Get()?.Envoy;
			if (envoyState != null)
			{
				return envoyState.Ch3 == "joined";
			}
			return false;
		}
	}

	public static void AddPoints(int points, string why)
	{
		MaliceState maliceState = MaliceState.Get();
		if (maliceState != null && points > 0)
		{
			maliceState.RebelPoints += points;
			maliceState.RebelEarned += points;
			ModLog.Info($"恶意巨企：反抗军助力点 +{points}（{why}），余 {maliceState.RebelPoints}，累计 {maliceState.RebelEarned}");
		}
	}

	private static StorePhoneClient RevPhone()
	{
		try
		{
			return StorePhoneClient.GetRevMerchantClient();
		}
		catch
		{
			return null;
		}
	}

	public static void OnDayStart()
	{
		MaliceState maliceState = MaliceState.Get();
		RebelCo company = Company;
		CompanyState companyState = ((company == null) ? null : Companies.StateOf(company));
		if (maliceState == null || companyState == null)
		{
			return;
		}
		StorePhoneClient storePhoneClient = RevPhone();
		if (storePhoneClient != null)
		{
			StorePhoneClient.PhoneState phoneState = storePhoneClient.phoneState;
			if (phoneState == StorePhoneClient.PhoneState.Regular || phoneState == StorePhoneClient.PhoneState.Cooldown)
			{
				maliceState.OdinMet = true;
				maliceState.OdinArrested = false;
			}
			else if (phoneState == StorePhoneClient.PhoneState.NotResponding && maliceState.OdinMet && !maliceState.OdinArrested && !Blacklisted)
			{
				maliceState.OdinArrested = true;
				Notify(L.T("奥丁被捕了。反抗军断了线：订货、地下销赃网络都停了。", "Odin has been arrested. The Resistance has gone dark: orders and the fence network are suspended."));
			}
		}
		if (maliceState.OdinMet && !companyState.CardGiven)
		{
			companyState.CardGiven = true;
			if (companyState.Coop < 20)
			{
				companyState.Coop = 20;
			}
			Phone.Activate(company);
			try
			{
				Notebook.Refresh();
			}
			catch
			{
			}
			Notify(L.T($"反抗军的电话（{company.PhoneNumber}）记在了电话旁的记事簿上。", L.Translate((System.FormattableString)$"The Resistance's number ({company.PhoneNumber}) is now in the notebook by the phone.")));
		}
		int num = 0;
		string[] factions = RebelCo.Factions;
		foreach (string faction in factions)
		{
			num += Math.Max(0, Engine.Reputation(faction));
		}
		if (companyState.RepSum >= 0 && num > companyState.RepSum)
		{
			Services.AddCoop(company, (double)(num - companyState.RepSum) * 0.5, "革命军、下层区声望上涨");
		}
		companyState.RepSum = num;
	}

	public static string OrderBlocked(Company c)
	{
		if (c is RebelCo)
		{
			if (!Arrested)
			{
				if (!Cut)
				{
					if (!Blacklisted)
					{
						return null;
					}
					return L.T("反抗军把你拉黑了。", "The Resistance has blacklisted you.");
				}
				return L.T("你加入了巨企，反抗军不再和你来往。", "You joined the corporations; the Resistance wants nothing to do with you.");
			}
			return L.T("反抗军断线了：奥丁被捕。", "The Resistance has gone dark: Odin was arrested.");
		}
		return null;
	}

	public static int FenceCost()
	{
		MaliceState maliceState = MaliceState.Get();
		if (maliceState == null)
		{
			return 50;
		}
		int num = Day - maliceState.FenceDay;
		if (num < 5)
		{
			return Math.Min(150, 50 + 25 * (5 - num));
		}
		return 50;
	}

	public static string BookFence()
	{
		MaliceState maliceState = MaliceState.Get();
		RebelCo company = Company;
		CompanyState companyState = ((company == null) ? null : Companies.StateOf(company));
		if (maliceState == null || companyState == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		if (Arrested || Cut || Blacklisted)
		{
			return L.T("反抗军断线了。", "The Resistance has gone dark.");
		}
		if (FenceClosed)
		{
			return L.T("反抗军不再给你安排地下买家。", "The Resistance no longer sends you underground buyers.");
		}
		if (companyState.Visitors.Exists((Visitor v) => v.Kind == "fence"))
		{
			return L.T("已经约好了，明天来。", "Already booked for tomorrow.");
		}
		int num = FenceCost();
		if (maliceState.RebelPoints < num)
		{
			return L.T($"助力点不够（要 {num} 点）。", L.Translate((System.FormattableString)$"Not enough support points (need {num})."));
		}
		maliceState.RebelPoints -= num;
		maliceState.FenceDay = Day;
		companyState.Visitors.Add(new Visitor
		{
			Day = Day + 1,
			Kind = "fence"
		});
		ModLog.Info($"恶意巨企：预约地下买家，{num} 点，第 {Day + 1} 天来");
		return null;
	}

	public static StoreClient FenceClient()
	{
		try
		{
			string text = L.T("地下买家", "Underground Buyer");
			StoreClient storeClient = new StoreClient
			{
				identifier = FenceId,
				displayName = text,
				spriteName = SpriteDict.GetRandomRevSpriteName(),
				clientFaction = "FACTION_REVOLUTION",
				clientIntent = StoreClient.ClientIntent.BUY,
				acceptRetailMarkup = false,
				isNoContributeToEvidence = true
			};
			int num = Math.Max(300, Settings.VanillaBudget() - 200);
			if (RebelStory.TokenHeld)
			{
				num = (int)((double)num * 1.5);
			}
			storeClient.SetBudget(num);
			storeClient.mainDialogue.SetText(text, L.T("奥丁让我来的。", "Odin sent me.")).NextDialogue().SetText(text, L.T("什么都收，现钱。货得经得起查。", "I take anything, cash. Just make sure it checks out."));
			storeClient.clientBuyingTagList.Add("ALL_TYPE");
			storeClient.canClientExposeFeature = ClientCanExposeFunc.TradeBoardExposeCapacity;
			storeClient.AddBasicDialog();
			storeClient.CompleteClientCreation(true);
			return storeClient;
		}
		catch (Exception ex)
		{
			ModLog.Warn("地下买家生成失败：" + ex.Message);
			return null;
		}
	}

	public static bool BeforeClientExposeFeature(StoreClient __instance, GameItem __0)
	{
		if (!Malice.On || __instance.identifier != FenceId)
			return true;

		var store = PlayerStore.Instance;
		var visitor = store?.currentClientInstance;
		if (visitor == null || !ReferenceEquals(visitor.GetClientBlueprint(), __instance)
			|| !__instance.clientBuyingTagList.Contains("ALL_TYPE") || __0 == null)
			return false;

		foreach (ItemFeature feature in __0.itemFeatures)
		{
			if (!__instance.CanClientExposeThisFeature(feature))
				continue;

			// Stop accepting goods immediately, including the rest of a bulk sale.
			// The same buyer can inspect several items before the placement call returns.
			__instance.clientBuyingTagList.Remove("ALL_TYPE");
			DialogUIManager.Instance.StartCoroutine(LeaveAfterInspection(() =>
			{
				if (ReferenceEquals(PlayerStore.Instance, store) && ReferenceEquals(store.currentClientInstance, visitor))
					store.DismissCurrentClient(callNextWhenPossible: false);
			}));

			var state = MaliceState.Get();
			int lost = state == null ? 0 : Math.Min(20, Math.Max(0, state.RebelPoints));
			if (state != null)
				state.RebelPoints -= lost;
			StoreReputation.ModReputationRaw(Faction, -5);
			feature.ExposeFeature(lowkey: true, playBad: true);
			Notify(L.T($"地下买家识破了造假，终止交易。助力点 -{lost}，反抗军声望 -5。",
				L.Translate((FormattableString)$"The underground buyer caught the fraud and called off the deal. Support points -{lost}, Resistance reputation -5.")));
			break;
		}
		// Vanilla exposure charges reputation for each feature and resumes bargaining.
		return false;
	}

	private static IEnumerator LeaveAfterInspection(Action leave)
	{
		// The base game still uses the active visitor after the inspection returns.
		yield return null;
		leave();
	}

	public static StoreClient SpawnVisitor(Company c, string kind)
	{
		if (!Malice.On || !(c is RebelCo) || !(kind == "fence") || Arrested || Cut || Blacklisted)
		{
			return null;
		}
		return FenceClient();
	}

	public static double BuyFactor(GameItem item)
	{
		if (!Malice.On)
		{
			return 1.0;
		}
		if (!((PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint())?.identifier == "malice_fence"))
		{
			return 1.0;
		}
		return 1.1;
	}

	public static void SoldToClient(StoreClient client, GameItem item, int amount)
	{
		if (Malice.On && client != null && !(client.clientFaction != "FACTION_REVOLUTION"))
		{
			AddPoints(amount / PointDivisor, "卖给反抗军");
		}
	}

	public static string Charge(int amount)
	{
		PlayerStore instance = PlayerStore.Instance;
		if (instance == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		if (!instance.CanPay(amount))
		{
			return L.T("现金不够", "Not enough cash");
		}
		CompanyMoney.Pay(instance, amount);
		AddPoints(amount / PointDivisor, "在反抗军订货");
		return "";
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
		ModLog.Info("恶意巨企：" + text);
	}
}
