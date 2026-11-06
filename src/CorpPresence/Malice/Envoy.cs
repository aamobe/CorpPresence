using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence.Malice;

internal static class Envoy
{
	public const int RichCash = 3000;

	public const int PoorCash = 2500;

	public const int RichWaitDays = 4;

	public const int AidAmount = 2000;

	public const int AidWeeks = 3;

	public const int Installment = 800;

	public const int AidRepayWeeks = 5;

	private const int ReplanFeeInstallments = 1;

	public const int ServiceFee = 1500;

	public const int Ch2Cash = 6000;

	public const int BombDays = 14;

	public const int ChannelWeeks = 2;

	public const double ChannelRate = 0.05;

	public const int ReconcileBribe = 3000;

	public const int Ch4Cash = 20000;

	public const int EvidenceRelief = 10;

	public const int SubsidyMin = 500;

	public const int SubsidyMax = 800;

	public const string BombTag = "CORP_ENVOY_BOMB";
	internal const string BombPending = "bombpending";

	internal const string BombBuyerId = "rebel_odin_envoy_bomb";

	internal static bool CanCallBombOdin => Malice.On && S?.Ch2 == "bomb" && Day < S.BombDeadline && !Rebels.OdinUnavailable;

	private static readonly string[] AidCorps = new string[2] { "pastel", "carmicheal" };

	private static readonly string[] Corps = new string[5] { "pastel", "solana", "carmicheal", "meridian", "davre" };
	private static bool _issuingBomb;

	private static int _luck;

	private static float _luckUntil;

	private static readonly List<(float At, string Text)> Timed = new List<(float, string)>();

	private static readonly List<string> Pending = new List<string>();

	private static int Day => StoreStation.GetDayCounter();

	private static PlayerStore Store => PlayerStore.Instance;

	private static EnvoyState S => MaliceState.Get()?.Envoy;

	public static EnvoyCo Company => (EnvoyCo)Companies.Find("envoy");

	private static string Sprite
	{
		get
		{
			try
			{
				return Portraits.Ensure("malice_envoy", "maleUpper_1");
			}
			catch
			{
				return "maleUpper_1";
			}
		}
	}

	public static bool Punished => S?.Aid == "punished";

	public static bool Joined => S?.Ch3 == "joined";

	public static bool Agent => S?.Agent ?? false;

	public static bool OrdersRefused => S?.Ch2 == "refused";

	public static bool SubscriptionFree
	{
		get
		{
			EnvoyState s = S;
			if (s != null)
			{
				if (!s.Agent && !(s.Ch3 == "joined") && (!s.SubFree || !(s.Ch3 != "refused")))
				{
					return RebelStory.Appeased;
				}
				return true;
			}
			return false;
		}
	}

	public static bool Ch2Reopened => S?.Ch2 == "refused";

	public static double OrderFactor()
	{
		EnvoyState s = S;
		if (s == null)
		{
			return 1.0;
		}
		double num = 1.0;
		if (s.DeclineUntil > Day)
		{
			num *= 1.1;
		}
		num *= 1.0 + LateOrderSurchargePercent(s.LateWeeks) / 100.0;
		return num * (s.Agent ? 0.8 : ((s.Ch3 == "joined" || RebelStory.Appeased) ? 0.9 : 1.0));
	}

	public static int LateOrderSurchargePercent(int lateWeeks) => Math.Clamp(lateWeeks, 0, 3) * 10;

	public static int LuckBonus()
	{
		if (!Malice.On)
		{
			return 0;
		}
		if (Time.unscaledTime < _luckUntil)
		{
			return _luck;
		}
		_luckUntil = Time.unscaledTime + 1f;
		try
		{
			PixelWindow pixelWindow = EmporiumEntry.Instance?.invWindow;
			if (pixelWindow == null)
			{
				return _luck = 0;
			}
			return _luck = GraphUtils.FindAllChildrenType<GameItem>(pixelWindow).Count((GameItem i) =>
			{
				string text = Goods.IdOf(i);
				return text != null && text.StartsWith("fate_coin_") && GeneralHelper.IsItemOwned(i);
			}) * 5;
		}
		catch
		{
			return _luck = 0;
		}
	}

	public static void OnDayStart()
	{
		EnvoyState s = S;
		PlayerStore store = Store;
		if (s == null || store == null)
		{
			return;
		}
		FlushLater();
		if (s.Ch3 == "joined" && s.RebelCut)
		{
			CheckReconciled();
		}
		if (s.Gone)
		{
			return;
		}
		int cash = store.GetCash();
		TrackCash();
		if (s.Aid == "active" && Day > s.AidStart)
		{
			if (Day >= s.AidStart + AidWeeks * 7)
			{
				s.Aid = "done";
				Book("medal", Day);
			}
			else if (AidCorps.Any((string id) => !Malice.Subscribed(Companies.Find(id))))
			{
				s.Aid = "repay5";
				s.RepayLeft = AidRepayWeeks;
				s.RepayDue = Day + 7;
				Notify(L.T($"帕斯泰尔或卡迈克尔的订阅断了。撒该要求你每周还 {Installment}₵，共 {AidRepayWeeks} 周。", $"Your Pastel or Carmicheal subscription lapsed. Zacchaeus now wants {Installment}₵ a week for {AidRepayWeeks} weeks."));
			}
		}
		if ((s.Aid == "repay5" || s.Aid == "plan6") && s.RepayDue >= 0 && s.RepayDue <= Day && !Booked("repay"))
		{
			Book("repay", Day);
		}
		if (Ch1Over(s) && s.Ch2 == "" && cash > 6000 && !Booked("ch2"))
		{
			Book("ch2", Day);
		}
		if (s.Ch2 == "bomb" && !s.BombIssued) RecheckLegacyBomb(s);
		if (s.BombIssued && MaliceGuards.CanPauseBombDeadline(s.Ch2, Day, s.BombDeadline, s.BombPauseDay, Rebels.OdinUnavailable))
		{
			s.BombDeadline++;
			s.BombPauseDay = Day;
		}
		if (s.Ch2 == "bomb" && s.BombIssued && Day > s.BombDeadline)
		{
			s.Ch2 = "malice";
			s.ChannelUntil = Day + ChannelWeeks * 7;
			s.ChannelDue = Day + 7;
			s.ChannelFee = 0;
			Notify(L.T($"礼物逾期未送达。接下来 {ChannelWeeks * 7} 天，卖出巨企目录商品时按成交额的 {ChannelRate * 100:0}% 计提渠道费。", $"The gift missed its deadline. For the next {ChannelWeeks * 7} days, the corporations charge {ChannelRate * 100:0}% of each catalog sale as a channel fee."));
		}
		if (s.Ch2 == "malice" && s.ChannelDue >= 0 && s.ChannelDue <= Day && !Booked("channel"))
		{
			Book("channel", Day);
		}
		if (s.BombSoldDay >= 0 && s.BombSoldDay == Day - 1 && s.RetaliateFrom < 0)
		{
			try
			{
				StoreReputation.ModReputation("FACTION_REVOLUTION", -20);
			}
			catch (Exception ex)
			{
				ModLog.Warn("扣反抗军声望失败：" + ex.Message);
			}
			s.RetaliateFrom = Day;
			s.RetaliateUntil = Day + 2;
			Notify(L.T("昨天那颗礼物在反抗军的据点爆炸。反抗军开始对你进行报复虽然被治安部拦下，但还是造成了影响：这几天来的顾客少了。", "Yesterday's gift went off in a Resistance safehouse. The Resistance struck back at you; Security stopped them, but the damage is done: fewer customers will come for a few days."));
		}
		if (s.RetaliateUntil >= 0 && Day > s.RetaliateUntil && !s.OdinUnderstood && !Booked("odin"))
		{
			Book("odin", Day);
		}
		if (Ch2Over(s) && s.Ch3 == "" && store.IsPropertyPaid && !Booked("ch3"))
		{
			Book("ch3", Day);
		}
		if (s.Ch3 == "joined")
		{
			try
			{
				store.secData.evidenceLevel = Math.Max(0, store.secData.evidenceLevel - 10);
			}
			catch
			{
			}
			if (s.SubsidyDue >= 0 && s.SubsidyDue <= Day)
			{
				SendSubsidy();
				s.SubsidyDue = Day + 7;
			}
		}
		if (s.Ch3 == "joined" && s.Ch4 == "" && cash >= 20000)
		{
			s.Ch4 = "asked";
			Company c = Companies.Find("spacer");
			CompanyState companyState = Companies.StateOf(c);
			if (companyState != null && !companyState.CardGiven)
			{
				companyState.CardGiven = true;
				if (companyState.Coop < 20)
				{
					companyState.Coop = 20;
				}
				Phone.Activate(c);
				try
				{
					Notebook.Refresh();
				}
				catch
				{
				}
			}
			Notify(L.T("导师留了话，让你回个电话（商人联合）。", "The Mentor left word: call him back (Merchant Union)."));
		}
		if (!(s.Ch4 == "silent") || s.Agent)
		{
			return;
		}
		string[] corps = Corps;
		foreach (string text in corps)
		{
			if (!s.Coins.Contains(text))
			{
				CompanyState companyState2 = Companies.StateOf(Companies.Find(text));
				if (companyState2 != null && companyState2.Coop >= 100 && !Booked("coin:" + text))
				{
					Book("coin:" + text, Day);
				}
			}
		}
		if (s.Coins.Count >= Corps.Length && !Booked("finale"))
		{
			Book("finale", Day);
		}
	}

	private static bool Ch1Over(EnvoyState s)
	{
		if (!(s.Aid == "done"))
		{
			return s.Aid == "declined";
		}
		return true;
	}

	private static bool Ch2Over(EnvoyState s)
	{
		switch (s.Ch2)
		{
		case "paid":
		case "bombdone":
		case "maliceover":
			return true;
		default:
			return false;
		}
	}


	private static bool AidContactsReady()
	{
		string[] aidCorps = AidCorps;
		int num = 0;
		while (num < aidCorps.Length)
		{
			Company company = Companies.Find(aidCorps[num]);
			if (company != null && company.Active)
			{
				CompanyState companyState = Companies.StateOf(company);
				if (companyState != null && companyState.CardGiven)
				{
					num++;
					continue;
				}
			}
			return false;
		}
		return true;
	}

	private static void TrackCash()
	{
		EnvoyState s = S;
		PlayerStore store = Store;
		if (s != null && store != null && !s.Gone && !(s.Aid != "") && !Booked("aid"))
		{
			int cash = store.GetCash();
			if (s.RichDay < 0 && cash > 3000)
			{
				s.RichDay = Day;
				ModLog.Info($"恶意巨企：现金第一次超过 {3000}（第 {Day} 天）");
			}
			if (s.RichDay >= 0 && AidContactsReady() && (cash < 2500 || Day >= s.RichDay + 4))
			{
				Book("aid", Day);
			}
		}
	}

	internal static bool Booked(string kind)
	{
		return S?.Visits.Any((EnvoyVisit v) => v.Kind == kind) ?? false;
	}

	internal static void Book(string kind, int day)
	{
		EnvoyState s = S;
		if (s != null && !Booked(kind))
		{
			s.Visits.Add(new EnvoyVisit
			{
				Kind = kind,
				Day = day
			});
			ModLog.Info($"恶意巨企：{kind} 排在第 {day} 天");
		}
	}

	internal static void Done(string kind)
	{
		S?.Visits.RemoveAll((EnvoyVisit v) => v.Kind == kind);
	}

	public static void Schedule(StoreClientManager manager)
	{
		EnvoyState s = S;
		if (!Malice.On || s == null || manager?.clientStack == null)
		{
			return;
		}
		FlushLater();
		if (s.Gone)
		{
			s.Visits.RemoveAll((EnvoyVisit v) => !v.Kind.StartsWith("rebel:") && !v.Kind.StartsWith("coin:") && v.Kind != "odin" && v.Kind != "finale");
		}
		foreach (EnvoyVisit item in s.Visits.Where((EnvoyVisit v) => v.Day <= Day && v.QueuedDay != Day).ToList())
		{
			if ((item.Kind != "aid" || AidContactsReady()) && (!OdinComes(item.Kind) || !Rebels.OdinUnavailable))
			{
				StoreClient storeClient = Create(item.Kind);
				if (storeClient == null)
				{
					Done(item.Kind);
					continue;
				}
				item.QueuedDay = Day;
				int count = manager.clientStack.Count;
				manager.clientStack.Insert((count > 1) ? new System.Random().Next(0, count / 2 + 1) : 0, storeClient);
				ModLog.Info("恶意巨企：" + item.Kind + " 已排进今天的顾客队列");
			}
		}
		if (s.CutDay != Day)
		{
			int num = ((s.Aid == "punished") ? 1 : 0) + ((s.RetaliateFrom >= 0 && Day >= s.RetaliateFrom && Day <= s.RetaliateUntil) ? 2 : 0);
			if (num > 0)
			{
				s.CutDay = Day;
				int num2 = manager.clientStack.Count - 1;
				while (num2 >= 0 && num > 0)
				{
					if (Malice.Cuttable(manager.clientStack[num2]))
					{
						manager.clientStack.RemoveAt(num2);
						num--;
					}
					num2--;
				}
			}
		}
		if (s.BoostDay == Day || !MedalShown())
		{
			return;
		}
		s.BoostDay = Day;
		foreach (StoreClient item2 in manager.clientStack)
		{
			try
			{
				if (Malice.OrdinaryVanilla(item2) && item2.clientFaction == "FACTION_UPPER_LEVEL" && item2.GetBudget() > 0)
				{
					item2.OverrideBudget((int)Math.Round((double)item2.GetBudget() * 1.1));
				}
			}
			catch
			{
			}
		}
	}

	private static bool OdinComes(string kind)
	{
		if (!(kind == "odin"))
		{
			if (kind.StartsWith("rebel:") && kind != "rebel:pickup")
			{
				return kind != "rebel:seller";
			}
			return false;
		}
		return true;
	}

	private static bool MedalShown()
	{
		try
		{
			return (EmporiumEntry.Instance?.showcaseElement)?.childItems.Any((GameItem i) => Goods.IdOf(i) == "friend_medal") ?? false;
		}
		catch
		{
			return false;
		}
	}

	private static StoreClient Create(string kind)
	{
		EnvoyState s = S;
		string name = L.T("撒该", "Zacchaeus");
		switch (kind)
		{
		case "aid":
			return Talk(kind, name, Sprite, new string[3]
			{
				L.T("您好。我是撒该·索恩，联合站区管理处站长。", "Good day. I'm Zacchaeus Thorne, director of the Joint District Administration."),
				L.T("我们注意到您的生意最近不错，想必很需要一笔扶持金来扩大规模。", "We've noticed business has been good lately. Surely you could use a grant to expand."),
				L.T($"管理处可以提供一笔 {2000}₵ 扶持金，甚至不用您偿还。条件很简单：连续三周订阅帕斯泰尔和卡迈克尔的订购服务，我的朋友们最近需要扩大市场。", $"The Administration can offer a {2000}₵ grant, and you needn't even repay it. One simple condition: keep Pastel and Carmicheal's ordering services subscribed for three weeks straight. My friends are looking to expand their market.")
			}, L.T("您意下如何？", "What do you say?"), new (string, string, Action)[2]
			{
				Opt(L.T("我接受。", "I accept."), L.T("明智。钱已经放在柜台上了。", "Wise. The money is on your counter."), AcceptAid),
				Opt(L.T("不用了。", "No thanks."), L.T("可惜。站区的物价最近恐怕会有些波动。", "A pity. Prices in the district may become somewhat volatile."), DeclineAid)
			}, () =>
			{
				s.Met = true;
				MeetPhone();
			});
		case "medal":
			return Talk(kind, name, Sprite, new string[2]
			{
				L.T("三周，一天不落。联合管理处很满意您信守承诺。", "Three weeks, not a day missed. The Administration appreciates someone who keeps their word."),
				L.T("这枚奖牌请收下。摆在显眼的地方，上层区的客人会注意到。", "Please accept this medal. Display it somewhere visible; Upper Levels clients will notice.")
			}, null, null, () =>
			{
				s.Medal = true;
				GiveItem("friend_medal");
			});
		case "repay":
			return Talk(kind, name, Sprite, new string[1] { L.T($"这周的 {Installment}₵。", $"This week's {Installment}₵.") }, L.T($"还剩 {s.RepayLeft} 期。", $"{s.RepayLeft} installment(s) left."), new (string, string, Action)[2]
			{
				Opt(L.T($"给你 {Installment}₵。", $"Here's {Installment}₵."), L.T("合作愉快。", "A pleasure."), PayInstallment),
				Opt(L.T("这周拿不出来。", "I can't pay this week."), L.T("我会如实记下的。", "I'll note that accurately."), MissInstallment)
			}, null);
		case "ch2":
			return Talk(kind, name, Sprite, new string[3]
			{
				L.T("生意兴隆。管理处其实一直在为您的店铺提供看不见的服务，我觉得您需要做出一些表示。", "Business is thriving. The Administration has quietly been providing your shop with services you never see. I think a gesture of thanks is in order."),
				L.T($"这些服务的费用是 {1500}₵。", $"Those services come to {1500}₵."),
				L.T("或者……您可以帮我们一个小忙。有一件东西，需要送到一位叫奥丁的人手里，我当然知道您和他很熟。", "Or... you could do us a small favor. There's a package that needs to reach a man named Odin. I know you two are well acquainted.")
			}, L.T("您选哪一样？", "Which will it be?"), new (string, string, Action)[3]
			{
				Opt(L.T($"我付 {1500}₵。", $"I'll pay {1500}₵."), L.T("感谢您的配合。", "Thank you for your cooperation."), () =>
				{
					Say(PayServiceFee());
				}),
				Opt(L.T("东西给我吧。", "Give me the gift."), L.T("拿到礼物后，您有两周。通过反抗军电话预约奥丁，再把礼物卖给他。办成了，订购服务的订阅费我想我可以帮您免去。", "Once you receive the gift, you have two weeks. Book Odin through the Resistance phone and sell it to him. Do this, and I believe I can waive your subscription fees."), () => Say(AcceptBomb())),
				Opt(L.T("都不干。", "Neither."), L.T("那么巨企的订货通道，暂时可能就不对您开放了。当然想通了打我电话。", "Then the corporate ordering channel may be closed to you for a while. Do call me when you've reconsidered."), RefuseCh2)
			}, null);
		case "channel":
			return Talk(kind, name, Sprite, new string[1] { L.T("我来结算目前已记的渠道费。未交的房租会给您留出来。", "I'm here to settle the channel fees accrued so far. I'll leave enough for any rent you still owe.") }, null, null, CollectChannel);
		case "odin":
			return Talk(kind, L.T("奥丁", "Odin"), "wanted4", new string[3]
			{
				L.T("别紧张。我不是来算账的。", "Relax. I'm not here to settle scores."),
				L.T("我知道那颗炸弹是谁给你的。我也知道你是被逼的。", "I know who gave you that bomb. I also know you were forced."),
				L.T("下层区的人都一样，脖子上都套着他们的绳子。", "Everyone down here is the same. Their noose around every neck.")
			}, null, null, () =>
			{
				s.OdinUnderstood = true;
			}, "FACTION_REVOLUTION");
		case "ch3":
			return Talk(kind, name, Sprite, new string[3]
			{
				L.T("恭喜您买下了这间店。从今天起，您是这家店真正的店主了。", "Congratulations on buying the shop. As of today, it's truly yours."),
				L.T("不过店主理所应当的应该和管理处站在一方我们会为您争取新的利益：订货九折，每周一份物资补贴，甚至治安部那边我们也替您打招呼，不会过分追究您做了什么。", "But an owner naturally belongs on the Administration's side, and we'll secure new benefits for you: 10% off orders, a weekly supply subsidy, and we'll even have a word with Security so they won't look too closely at what you've done."),
				L.T("当然，您得和那些老鼠断干净。", "Naturally, you'd cut ties with those rats.")
			}, L.T("加入我们吗？", "Will you join us?"), new (string, string, Action)[2]
			{
				Opt(L.T("加入。", "I'm in."), L.T("欢迎。", "Welcome."), JoinCorps),
				Opt(L.T("不了。", "No."), L.T("您会后悔的。", "You'll regret this."), RefuseCorps)
			}, null);
		case "finale":
			return Talk(kind, name, Sprite, new string[2]
			{
				"……",
				L.T("……", "...")
			}, null, null, Finale);
		default:
			if (kind.StartsWith("coin:"))
			{
				return CoinVisit(kind.Substring(5));
			}
			if (kind.StartsWith("rebel:"))
			{
				return RebelStory.Create(kind);
			}
			return null;
		}
	}

	private static StoreClient CoinVisit(string companyId)
	{
		Company company = Companies.Find(companyId);
		Character character = ((company == null) ? null : Engine.Find(company.CharacterId));
		if (company == null || character == null)
		{
			return null;
		}
		string sprite;
		try
		{
			sprite = Portraits.Ensure(character.Portrait(CorpState.Get().Of(character.Id), "daily"), character.PortraitRef);
		}
		catch
		{
			sprite = character.PortraitRef;
		}
		return Talk("coin:" + companyId, character.Name, sprite, new string[2]
		{
			L.T(company.Name + "让我把这个交给你。", company.Name + " asked me to give you this."),
			L.T("他们说，你是被选中的人。", "They say you're one of the chosen.")
		}, null, null, () =>
		{
			S.Coins.Add(companyId);
			GiveItem(MaliceItems.Coin(companyId));
			RebelStory.CheckFinale();
		}, character.Faction);
	}

	private static void AcceptAid()
	{
		EnvoyState s = S;
		s.Aid = "active";
		s.AidStart = Day;
		s.Met = true;
		Store.playerCash += 2000;
		MeetPhone();
		string[] aidCorps = AidCorps;
		foreach (string id in aidCorps)
		{
			if (!Malice.Subscribed(Companies.Find(id)))
			{
				Notify(L.T("记得开通" + Companies.Find(id).Name + "的订购服务（援助条件）。", "Remember to subscribe to " + Companies.Find(id).Name + "'s ordering service (aid condition)."));
			}
		}
		ModLog.Info($"恶意巨企：接受援助 {2000}₵");
	}

	private static void DeclineAid()
	{
		EnvoyState s = S;
		s.Aid = "declined";
		s.DeclineUntil = Day + 14;
		s.Met = true;
		MeetPhone();
	}

	private static void PayInstallment()
	{
		EnvoyState s = S;
		PlayerStore store = Store;
		if (s.Aid != "repay5" && s.Aid != "plan6")
		{
			Done("repay");
			return;
		}
		if (store.GetCash() < Installment)
		{
			MissInstallment();
			Notify(L.T("现金不够，这期没交上。", "Not enough cash: this installment is missed."));
			return;
		}
		store.PayMoney(Installment, playRegisterSound: true);
		s.RepayLeft--;
		s.RepayDue = Day + 7;
		if (s.RepayLeft <= 0)
		{
			FinishRepayment();
		}
		Done("repay");
	}

	private static void MissInstallment()
	{
		EnvoyState s = S;
		Done("repay");
		if (!(s.Aid != "repay5") || !(s.Aid != "plan6"))
		{
			if (s.Aid == "repay5")
			{
				s.Aid = "punished";
				s.RepayDue = -1;
				Notify(L.T("你没交上撒该的钱。所有公司服务暂停（反抗军除外），撒该开始“问候”你的客人。打他的电话可以答应还款计划。", "You missed Zacchaeus's payment. All company services are suspended (except the Resistance), and he's started “visiting” your customers. Call him to agree to a repayment plan."));
			}
			else if (s.Aid == "plan6")
			{
				s.LateWeeks++;
				s.RepayDue = Day + 7;
				int surcharge = LateOrderSurchargePercent(s.LateWeeks);
				Notify(L.T($"这期没交，顺延一周。巨企订货价格累计 +{surcharge}%，还完恢复。", $"Missed: pushed back a week. Corporate order prices are now +{surcharge}% until you finish paying."));
			}
		}
	}

	private static void FinishRepayment()
	{
		EnvoyState s = S;
		s.Aid = "done";
		s.RepayDue = -1;
		s.LateWeeks = 0;
		if (!s.Medal)
		{
			Book("medal", Day + 1);
		}
		Notify(L.T("撒该的钱还清了。", "Zacchaeus has been paid in full."));
	}

	public static int RepaymentPlanWeeks(EnvoyState state) => Math.Clamp(state.RepayLeft, 0, AidRepayWeeks) + ReplanFeeInstallments;

	public static string AgreePlan()
	{
		EnvoyState s = S;
		if (s == null || s.Aid != "punished")
		{
			return L.T("现在不需要。", "Not needed right now.");
		}
		s.Aid = "plan6";
		s.RepayLeft = RepaymentPlanWeeks(s);
		s.RepayDue = Day + 7;
		s.LateWeeks = 0;
		ModLog.Info($"恶意巨企：答应 {s.RepayLeft} 周还款计划");
		return null;
	}

	public static string PayServiceFee()
	{
		EnvoyState s = S;
		if (s.Ch2 != "" && s.Ch2 != "refused")
		{
			return L.T("现在不需要。", "Not needed right now.");
		}
		if (Store.GetCash() < 1500)
		{
			return L.T("现金不够", "Not enough cash");
		}
		Store.PayMoney(1500, playRegisterSound: true);
		s.Ch2 = "paid";
		Done("ch2");
		return null;
	}

	public static string AcceptBomb()
	{
		EnvoyState s = S;
		Func<bool> isCurrent = MaliceGuards.CaptureSession();
		if (!isCurrent() || s == null)
			return L.T("存档未就绪。", "Save not ready.");
		if (_issuingBomb || s.Ch2 is not ("" or "refused" or BombPending))
			return L.T("现在无法领取礼物。", "The gift cannot be claimed right now.");

		_issuingBomb = true;
		try
		{
			s.Ch2 = BombPending;
			Done("ch2");
			if (!TryFindEnvoyBomb(out bool present) || !isCurrent())
				return L.T("暂时无法核对库存，礼物仍可稍后领取。", "Inventory could not be checked. You can claim the gift later.");
			if (!present && !TryIssueBomb(isCurrent))
				return L.T("礼物暂未入库。请留出库存空间，再打撒该的电话领取。", "The gift could not be placed. Make room in the shop and call Zacchaeus to claim it.");
			if (!isCurrent()) return L.T("存档已切换。", "The active save has changed.");
			s.BombIssued = true;
			s.BombDeadline = Day + BombDays;
			s.BombPauseDay = -1;
			s.Ch2 = "bomb";
			return null;
		}
		finally
		{
			_issuingBomb = false;
		}
	}

	private static bool TryFindEnvoyBomb(out bool present)
	{
		present = false;
		PlayerStore store = Store;
		EmporiumEntry emporium = EmporiumEntry.Instance;
		if (store == null || emporium == null) return false;
		bool Match(GameItem item) => !StorePlacement.Destroyed(item) && IsProtectedBomb(item);
		try
		{
			present = store.FindAllItem(isOwn: false).Any(Match) || emporium.GetAllItems().Any(Match);
			return true;
		}
		catch (Exception error)
		{
			ModLog.Warn("核对撒该的礼物失败：" + error.Message);
			return false;
		}
	}

	private static bool TryIssueBomb(Func<bool> isCurrent)
	{
		GameItem gameItem = null;
		try
		{
			gameItem = ItemRegistry.Create("c4");
			if (gameItem == null || !isCurrent()) return false;
			gameItem.EnableTag(BombTag);
			gameItem.SetName(L.T("撒该的礼物", "Zacchaeus's Gift"));
			gameItem.shortDescription = L.T("只能交给通过反抗军电话预约来收礼物的奥丁。", "Deliver only to Odin booked through the Resistance phone to collect this gift.");
			return isCurrent() && StorePlacement.Place(gameItem);
		}
		catch (Exception error)
		{
			ModLog.Warn("发放撒该的礼物失败：" + error.Message);
			return StorePlacement.Attached(gameItem);
		}
		finally
		{
			if (gameItem != null && !StorePlacement.Attached(gameItem) && !StorePlacement.Destroyed(gameItem))
				try { gameItem.Destroy(); } catch (Exception error) { ModLog.Warn("回收未发放的礼物失败：" + error.Message); }
		}
	}

	private static void RecheckLegacyBomb(EnvoyState s)
	{
		Func<bool> isCurrent = MaliceGuards.CaptureSession();
		if (!isCurrent() || !TryFindEnvoyBomb(out bool present) || !isCurrent()) return;
		s.BombIssued = present;
		if (present) return;
		s.Ch2 = BombPending;
		s.BombDeadline = -1;
		s.BombPauseDay = -1;
		Done("rebel:envoybomb");
		Notify(L.T("店里未找到撒该的礼物。请打他的电话重新领取，入库后才开始计时。", "Zacchaeus's gift was not found in the shop. Call him to claim it and start the timer once it is placed."));
	}

	private static void RefuseCh2()
	{
		if (S is not { Ch2: "" } s) return;
		s.Ch2 = "refused";
		Done("ch2");
	}

	public static void SoldToClient(StoreClient client, GameItem item, int amount)
	{
		EnvoyState s = S;
		if (s != null && item != null)
		{
			if (IsProtectedBomb(item) && CanReceiveBomb(client))
			{
				s.Ch2 = "bombdone";
				s.SubFree = true;
				s.BombSoldDay = Day;
				Done("rebel:envoybomb");
				Notify(L.T("礼物送到了。撒该说，巨企的订购服务以后不收订阅费了。", "The gift was delivered. Zacchaeus says corporate ordering services are now subscription-free."));
			}
			if (s.Ch2 == "malice" && Day < s.ChannelUntil && IsCorpProduct(item))
			{
				s.ChannelFee += (int)Math.Round(amount * ChannelRate);
			}
		}
	}

	public static bool ClientBuys(StoreClient client, GameItem item)
	{
		if (IsProtectedBomb(item))
		{
			return CanReceiveBomb(client);
		}
		return false;
	}


	internal static bool IsProtectedBomb(GameItem item)
	{
		if (Malice.On && item != null)
		{
			return item.IsTag(BombTag);
		}
		return false;
	}


	internal static bool CanReceiveBomb(StoreClient client)
	{
		return MaliceGuards.CanDeliverBomb(S?.Ch2, Day, S?.BombDeadline ?? (-1), Rebels.OdinUnavailable, client?.identifier);
	}


	internal static string CallBombOdin()
	{
		if (!CanCallBombOdin)
		{
			return L.T("现在无法预约奥丁，或已来不及在期限内到达。", "Odin cannot be booked, or cannot arrive before the deadline.");
		}
		if (Booked("rebel:envoybomb"))
		{
			return L.T("已经约好了。", "Already booked.");
		}
		Book("rebel:envoybomb", Day + 1);
		return null;
	}

	private static bool IsCorpProduct(GameItem item)
	{
		foreach (string companyId in Corps)
		{
			CatalogEntry[] catalog = Companies.Find(companyId)?.Catalog;
			if (catalog != null && catalog.Any(e => MatchesCatalogItem(e?.Id, item)))
			{
				return true;
			}
		}
		return false;
	}

	private static bool MatchesCatalogItem(string catalogId, GameItem item)
	{
		if (string.IsNullOrEmpty(catalogId)) return false;
		string itemId = item.identifier;
		if (catalogId == itemId) return true;
		if (catalogId[0] != '@') return false;
		string key = catalogId.Substring(1);
		// Catalog factories can deliver a native item under a different identifier.
		return key switch
		{
			"tablets" => itemId == "water_tablet_container",
			"strips" => itemId == "water_test_container",
			"immunin_case" => itemId == CarmichaelCo.Immunin,
			"hemingway_limited" => itemId == "packet_red_cigarette" && item.IsTag(PastelCo.LimitedTag),
			"hemingway_genuine" => itemId == "packet_red_cigarette"
				&& item.FindItemFeatureByCategory("CATEGORY_GENUINE_CIGARETTE")?.realCondition?.identifier == "genuineCigarette",
			_ => itemId == (ItemIdentity.ForGoods(key)?.Id ?? key)
		};
	}

	private static void CollectChannel()
	{
		EnvoyState s = S;
		PlayerStore store = Store;
		if (s?.Ch2 != "malice" || store == null || s.ChannelDue < 0 || Day < s.ChannelDue) return;
		int rent = store.IsPropertyPaid ? 0 : store.GetRentValue();
		int balance = s.ChannelFee;
		int paid = Math.Max(0, Math.Min(balance, store.GetCash() - rent));
		if (paid > 0) CompanyMoney.Pay(store, paid);
		s.ChannelFee = 0;
		Done("channel");
		if (Day >= s.ChannelUntil)
		{
			s.Ch2 = "maliceover";
			s.ChannelDue = -1;
		}
		else
		{
			s.ChannelDue = s.ChannelUntil;
		}
		Notify(L.T($"本期渠道费 {balance}₵，实收 {paid}₵。", $"Channel fees for this collection: {balance}₵ due, {paid}₵ collected."));
	}

	private static void JoinCorps()
	{
		EnvoyState s = S;
		s.Ch3 = "joined";
		s.RebelCut = true;
		s.SubsidyDue = Day + 7;
		Done("ch3");
		Notify(L.T("你加入了巨企。反抗军很失望断了和你的联系。", "You've joined the corporations. The Resistance, disappointed, has cut ties with you."));
	}

	private static void RefuseCorps()
	{
		S.Ch3 = "refused";
		Done("ch3");
		ModLog.Info("恶意巨企：第三章拒绝加入巨企（之后进入反抗军第 4 章）");
	}

	public static string BribeForReconcile()
	{
		EnvoyState s = S;
		if (s.Ch3 != "joined" || s.BribedEnvoy)
		{
			return L.T("现在不需要。", "Not needed right now.");
		}
		if (Store.GetCash() < 3000)
		{
			return L.T("现金不够", "Not enough cash");
		}
		Store.PayMoney(3000, playRegisterSound: true);
		s.BribedEnvoy = true;
		CheckReconciled();
		return null;
	}

	public static string ReconcileWithRebels()
	{
		EnvoyState s = S;
		if (s.Ch3 != "joined" || s.ReconciledRebels)
		{
			return L.T("现在不需要。", "Not needed right now.");
		}
		if (Store.GetCash() < 1000)
		{
			return L.T("现金不够", "Not enough cash");
		}
		Store.PayMoney(1000, playRegisterSound: true);
		s.ReconciledRebels = true;
		CheckReconciled();
		return null;
	}

	private static void CheckReconciled()
	{
		EnvoyState s = S;
		if ((s.BribedEnvoy || s.Gone) && s.ReconciledRebels)
		{
			s.RebelCut = false;
			Notify(L.T("各办各事。反抗军恢复了联系，但不再给你安排地下买家。", "Each to their own. The Resistance is back in touch, but no longer sends you underground buyers."));
		}
	}

	private static void SendSubsidy()
	{
		try
		{
			System.Random random = new System.Random();
			int num = random.Next(500, 801);
			int num2 = 0;
			Company company = Companies.Find(Corps[random.Next(Corps.Length)]);
			CatalogEntry[] array = company.Catalog.Where((CatalogEntry e) => e.BaseValue() > 0 && e.WeeklyLimit == 0).ToArray();
			Order order = new Order
			{
				Day = Day,
				Kind = "gift"
			};
			for (int num3 = 0; num3 < 40; num3++)
			{
				if (num2 >= num)
				{
					break;
				}
				if (array.Length == 0)
				{
					break;
				}
				CatalogEntry catalogEntry = array[random.Next(array.Length)];
				if (num2 + catalogEntry.BaseValue() <= 800)
				{
					order.Items.Add(catalogEntry.Id);
					num2 += catalogEntry.BaseValue();
				}
			}
			if (order.Items.Count != 0)
			{
				Companies.StateOf(company).Orders.Add(order);
				ModLog.Info($"恶意巨企：巨企物资补贴 {num2}₵，{company.Name}送来 {order.Items.Count} 件");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("巨企补贴失败：" + ex.Message);
		}
	}

	public static Dialogue MentorCall(Company c)
	{
		EnvoyState s = S;
		if (!Malice.On || s == null || s.Ch4 != "asked" || !(c is SpacerCo))
		{
			return null;
		}
		string title = L.T("导师", "The Mentor");
		Dialogue dialogue = new Dialogue().SetText(title, L.T("你甘心做巨企的看门人吗？", "Are you content to be the corporations' gatekeeper?"));
		Dialogue dialogue2 = new Dialogue().SetText("PLAYER", L.T("当然，我很愿意。", "Of course. Gladly."));
		Dialogue dialogue3 = new Dialogue().SetText("PLAYER", L.T("（沉默）", "(silence)"));
		dialogue2.NextDialogue().SetText(title, L.T("……那就这样吧。", "...So be it.")).SetEndAction(Phone.GuardCall(c, () =>
		{
			s.Ch4 = "willing";
			try
			{
				PhoneUIManager.Instance?.OnPhoneDialogEnd();
			}
			catch
			{
			}
		}));
		dialogue3.NextDialogue().SetText(title, L.T("我明白了。我去替你联络。", "I understand. I'll make contact for you.")).SetEndAction(Phone.GuardCall(c, () =>
		{
			s.Ch4 = "silent";
			try
			{
				PhoneUIManager.Instance?.OnPhoneDialogEnd();
			}
			catch
			{
			}
		}));
		dialogue.AddChoice(Compat.ChoiceIcon(), L.T("我愿意。", "I am."), "", dialogue2);
		dialogue.AddChoice(Compat.ChoiceIcon(), L.T("（沉默）", "(silence)"), "", dialogue3);
		return dialogue;
	}

	private static void Finale()
	{
		EnvoyState s = S;
		s.Agent = true;
		s.Gone = true;
		GiveItem("agent_cert");
		try
		{
			Notebook.Refresh();
		}
		catch
		{
		}
		After(2f, L.T("你听到一声枪响。", "You hear a gunshot."));
		Later(L.T("你成了下层区的巨企代理人：所有公司服务免费，订货八折。", "You are now the corporations' agent for the Lower Levels: all company services are free, and orders are 20% off."));
	}

	public static string DebugNext()
	{
		EnvoyState s = S;
		if (s == null)
		{
			return null;
		}
		OnDayStart();
		EnvoyVisit envoyVisit = s.Visits.FirstOrDefault();
		if (envoyVisit == null)
		{
			return null;
		}
		StoreClient storeClient = Create(envoyVisit.Kind);
		if (storeClient == null)
		{
			return null;
		}
		envoyVisit.QueuedDay = Day;
		Store.storeClientManager.AddNextClient(storeClient);
		return envoyVisit.Kind;
	}

	public static bool DebugQueue(string kind)
	{
		EnvoyState s = S;
		if (s == null)
		{
			return false;
		}
		StoreClient storeClient = Create(kind);
		if (storeClient == null)
		{
			return false;
		}
		if (!Booked(kind))
		{
			s.Visits.Add(new EnvoyVisit
			{
				Kind = kind,
				Day = Day,
				QueuedDay = Day
			});
		}
		Store.storeClientManager.AddNextClient(storeClient);
		return true;
	}

	public static bool DebugGreet()
	{
		StoreClient storeClient = Talk("greet", L.T("撒该", "Zacchaeus"), Sprite, new string[2]
		{
			L.T("（测试）只是路过，看看您的店。", "(Test) Just passing by to see your shop."),
			L.T("（测试）生意兴隆。", "(Test) Business is booming.")
		}, null, null, null);
		if (storeClient == null)
		{
			return false;
		}
		Store.storeClientManager.AddNextClient(storeClient);
		return true;
	}

	public static void DebugWeek()
	{
		EnvoyState s = S;
		if (s == null)
		{
			return;
		}
		int d = s.AidStart;
		Shift(ref d);
		s.AidStart = d;
		int d2 = s.RepayDue;
		Shift(ref d2);
		s.RepayDue = d2;
		int d3 = s.BombDeadline;
		Shift(ref d3);
		s.BombDeadline = d3;
		int d4 = s.ChannelDue;
		Shift(ref d4);
		s.ChannelDue = d4;
		int d5 = s.ChannelUntil;
		Shift(ref d5);
		s.ChannelUntil = d5;
		int d6 = s.SubsidyDue;
		Shift(ref d6);
		s.SubsidyDue = d6;
		int d7 = s.RichDay;
		Shift(ref d7);
		s.RichDay = d7;
		int d8 = s.DeclineUntil;
		Shift(ref d8);
		s.DeclineUntil = d8;
		foreach (CorpTerms value in MaliceState.Get().Corps.Values)
		{
			if (value.SubUntil >= 0)
			{
				value.SubUntil -= 7;
			}
		}
		static void Shift(ref int reference)
		{
			if (reference >= 0)
			{
				reference -= 7;
			}
		}
	}

	private static void MeetPhone()
	{
		EnvoyCo company = Company;
		CompanyState companyState = ((company == null) ? null : Companies.StateOf(company));
		if (companyState != null)
		{
			bool flag = !companyState.CardGiven;
			companyState.CardGiven = true;
			Phone.Activate(company);
			try
			{
				Notebook.Refresh();
			}
			catch
			{
			}
			if (flag)
			{
				Notify(L.T($"撒该的电话（{company.PhoneNumber}）记在了电话旁的记事簿上。", $"Zacchaeus's number ({company.PhoneNumber}) is now in the notebook by the phone."));
			}
		}
	}

	internal static void GiveItem(string goodsKey)
	{
		try
		{
			GameItem gameItem = Goods.Make(goodsKey);
			if (gameItem == null)
			{
				ModLog.Warn("恶意巨企：" + goodsKey + " 生成失败");
				return;
			}
			GeneralHelper.SetItemOwned(gameItem);
			if (!StorePlacement.Place(gameItem))
			{
				ModLog.Warn("恶意巨企：店里所有库存都放满了，" + gameItem.identifier + " 没送到");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("恶意巨企：发 " + goodsKey + " 失败：" + ex.Message);
		}
	}

	internal static (string Label, string Reply, Action Then) Opt(string label, string reply, Action then)
	{
		return (Label: label, Reply: reply, Then: then);
	}

	private static void Say(string err)
	{
		if (err != null)
		{
			Notify(err);
		}
	}

	internal static StoreClient Talk(string kind, string name, string sprite, string[] lines, string ask, (string Label, string Reply, Action Then)[] choices, Action onEnd, string faction = "FACTION_UPPER_LEVEL")
	{
		Func<bool> isCurrent = MaliceGuards.CaptureSession();
		try
		{
			StoreClient storeClient = new StoreClient
			{
				identifier = "envoy_" + kind.Replace(':', '_'),
				displayName = name,
				spriteName = sprite,
				clientFaction = faction,
				clientIntent = StoreClient.ClientIntent.DIALOGUE,
				dismissable = true
			};
			storeClient.AddBasicDialog();
			storeClient.CompleteClientCreation();
			Action endAction = () =>
			{
				if (!isCurrent()) return;
				try
				{
					onEnd?.Invoke();
				}
				catch (Exception ex2)
				{
					ModLog.Warn("恶意巨企：" + kind + " 收尾失败：" + ex2.Message);
				}
				if (!isCurrent()) return;
				Done(kind);
				try
				{
					UnityEngine.Object.FindObjectOfType<DialogUIManager>()?.CurrentClientLeave();
				}
				catch
				{
				}
			};
			Dialogue dialogue = new Dialogue().SetText(name, lines[0]);
			Dialogue dialogue2 = dialogue;
			for (int num = 1; num < lines.Length; num++)
			{
				dialogue2 = dialogue2.NextDialogue().SetText(name, lines[num]);
			}
			if (!string.IsNullOrEmpty(ask) && choices != null && choices.Length != 0)
			{
				Dialogue dialogue3 = dialogue2.NextDialogue().SetText(name, ask);
				for (int num2 = 0; num2 < choices.Length; num2++)
				{
					(string, string, Action) tuple = choices[num2];
					string label = tuple.Item1;
					string item = tuple.Item2;
					Action item2 = tuple.Item3;
					Dialogue dialogue4 = new Dialogue().SetText("PLAYER", label);
					dialogue4.NextDialogue().SetText(name, item).SetEndAction(endAction);
					Action act = item2;
					dialogue3.AddChoice(Compat.ChoiceIcon(), label, "", dialogue4, () =>
					{
						if (!isCurrent()) return;
						try
						{
							act?.Invoke();
							ModLog.Info("恶意巨企：" + kind + " 选了“" + label + "”");
						}
						catch (Exception ex2)
						{
							ModLog.Warn("恶意巨企：选项处理失败：" + ex2.Message);
						}
					});
				}
			}
			else
			{
				dialogue2.SetEndAction(endAction);
			}
			storeClient.mainDialogue = dialogue;
			return storeClient;
		}
		catch (Exception ex)
		{
			ModLog.Warn("恶意巨企：" + kind + " 来访生成失败：" + ex.Message);
			return null;
		}
	}

	private static void After(float seconds, string text)
	{
		Timed.Add((Time.unscaledTime + seconds, text));
		ModLog.Info("恶意巨企：" + text);
	}

	public static void Tick()
	{
		if (Timed.Count == 0)
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		for (int num = Timed.Count - 1; num >= 0; num--)
		{
			if (!(Timed[num].At > unscaledTime))
			{
				try
				{
					StoreUIManager.Instance?.Notify(Timed[num].Text);
				}
				catch
				{
				}
				Timed.RemoveAt(num);
			}
		}
	}

	private static void Later(string text)
	{
		Pending.Add(text);
		ModLog.Info("恶意巨企：" + text);
	}

	private static void FlushLater()
	{
		if (Pending.Count == 0)
		{
			return;
		}
		string message = string.Join(" ", Pending);
		Pending.Clear();
		try
		{
			StoreUIManager.Instance?.Notify(message);
		}
		catch
		{
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
		ModLog.Info("恶意巨企：" + text);
	}
}

