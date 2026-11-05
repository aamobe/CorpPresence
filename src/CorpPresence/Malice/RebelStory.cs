using System;
using System.Linq;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence.Malice;

internal static class RebelStory
{
	private const string Odin = "wanted4";

	public const int Ch1Points = 50;

	public const int Ch2Points = 300;

	public const int Ch3Points = 500;

	public const int Ch5Cash = 16000;

	public const int Ch1Value = 100;

	public const int Ch1Reward = 100;

	public const int Ch2BribeLow = 200;

	public const int Ch2BribeHigh = 500;

	public const int Ch2FenceTarget = 1000;

	public const int Ch3MedicalTarget = 1200;

	public const int Ch3Reward = 1500;

	public const int Ch4Target = 3000;

	public const int RescueTarget = 350;

	public const string PickupId = "rebel_pickup";

	public const string OdinBuyerId = "rebel_odin_buyer";

	public const string SellerId = "rebel_meridian_seller";

	public const string BombTag = "CORP_REBEL_BOMB";

	private static bool _orderingBomb;

	public static bool Ch2ReorderOpen => Malice.On && R?.Active == 2 && R.Ch2Route == "bribe" && R.Ch2BombDay < 0;

	private static readonly string[] Corps = new string[5] { "pastel", "solana", "carmicheal", "meridian", "davre" };

	private static readonly string[] Ch1Ids = new string[2] { "dream_dust", "fanta_pill" };

	private static int Day => StoreStation.GetDayCounter();

	private static PlayerStore Store => PlayerStore.Instance;

	private static RebelState R => MaliceState.Get()?.Rebel;

	private static string OdinName => L.T("奥丁", "Odin");

	public static bool Appeased
	{
		get
		{
			if (Malice.On)
			{
				return R?.Appeased ?? false;
			}
			return false;
		}
	}

	public static bool TokenHeld
	{
		get
		{
			if (Malice.On)
			{
				return R?.Joined ?? false;
			}
			return false;
		}
	}

	public static bool PickupOpen
	{
		get
		{
			if (Malice.On)
			{
				if (!Rebels.Arrested)
				{
					RebelState r = R;
					if (r == null)
					{
						return false;
					}
					return r.Active == 1;
				}
				return true;
			}
			return false;
		}
	}

	public static bool Ch2BribeOpen
	{
		get
		{
			if (Malice.On)
			{
				RebelState r = R;
				if (r != null && r.Active == 2)
				{
					return r.Ch2Route == "";
				}
				return false;
			}
			return false;
		}
	}

	public static bool CanCallOdin
	{
		get
		{
			if (Malice.On)
			{
				RebelState r = R;
				if (r != null && r.Active == 4 && r.Invited && !r.Joined)
				{
					return !Envoy.Booked("rebel:invite");
				}
			}
			return false;
		}
	}

	public static void OnDayStart()
	{
		Func<bool> isCurrent = MaliceGuards.CaptureSession();
		if (!isCurrent()) return;
		RebelState r = R;
		MaliceState maliceState = MaliceState.Get();
		if (r == null || maliceState == null)
		{
			return;
		}
		bool num = maliceState.OdinMet && !Rebels.OdinUnavailable;
		bool flag = DeliverDueBomb(r);
		if (!num || !isCurrent())
		{
			return;
		}
		int rebelEarned = maliceState.RebelEarned;
		if (r.Done == 0 && r.Active == 0 && rebelEarned >= 50)
		{
			Envoy.Book("rebel:ch1", Day);
		}
		if (r.Done == 1 && r.Active == 0 && rebelEarned >= 300)
		{
			Envoy.Book("rebel:ch2", Day);
		}
		if (r.Done == 2 && r.Active == 0 && rebelEarned >= 500)
		{
			StartCh3();
		}
		bool present = default(bool);
		if (flag)
		{
			Envoy.Book("rebel:odinbomb", Day + 1);
		}
		else if (r.Active == 2 && r.Ch2Route == "bribe" && r.Ch2BombDay < 0 && !Envoy.Booked("rebel:odinbomb") && TryFindBomb(out present) && present)
		{
			Envoy.Book("rebel:odinbomb", Day);
		}
		if (r.Active == 3 && r.Ch3Start >= 0 && Day >= r.Ch3Start && Day < r.Ch3Start + 3)
		{
			Envoy.Book("rebel:seller", Day);
		}
		if ((r.Active == 3 || (r.Active == 4 && r.Joined && r.RaidDay < 0)) && r.OdinBuyDue >= 0 && r.OdinBuyDue <= Day)
		{
			Envoy.Book("rebel:buyer", Day);
			r.OdinBuyDue = Day + 7;
		}
		if (r.Done == 3 && r.Active == 0 && !r.Invited && maliceState.Envoy.Ch3 == "refused")
		{
			r.Active = 4;
			Envoy.Book("rebel:invite", Day);
		}
		if (r.Active == 4 && r.RaidDay >= 0 && Day >= r.RaidDay && !r.Appeased)
		{
			Raid();
		}
		if (r.Done == 4 && !r.Ch5 && Store.GetCash() >= 16000)
		{
			r.Ch5 = true;
			r.Active = 5;
			Notify(L.T("导师来了消息：他会替你去和那几家巨企谈。", "Word from the Mentor: he'll speak to the corporations on your behalf."));
		}
		if (r.Active != 5 || r.RepCert)
		{
			return;
		}
		string[] corps = Corps;
		foreach (string text in corps)
		{
			if (!maliceState.Envoy.Coins.Contains(text))
			{
				CompanyState companyState = Companies.StateOf(Companies.Find(text));
				if (companyState != null && companyState.Coop >= 100)
				{
					Envoy.Book("coin:" + text, Day);
				}
			}
		}
	}

	public static void CheckFinale()
	{
		RebelState r = R;
		MaliceState maliceState = MaliceState.Get();
		if (r != null && maliceState != null && r.Active == 5 && !r.RepCert && maliceState.Envoy.Coins.Count >= Corps.Length)
		{
			r.RepCert = true;
			r.Done = 5;
			r.Active = 0;
			Envoy.GiveItem("lower_rep_cert");
			Notify(L.T("下层区的人联名推你做了代表：所有顾客带的钱 +50%。", "The Lower Levels have named you their representative: all customers carry 50% more money."));
		}
	}

	public static StoreClient Create(string kind)
	{
		RebelState r = R;
		switch (kind)
		{
		case "rebel:envoybomb":
			if (!MaliceGuards.CanDeliverBomb(MaliceState.Get()?.Envoy.Ch2, Day, MaliceState.Get()?.Envoy.BombDeadline ?? -1, Rebels.OdinUnavailable, Envoy.BombBuyerId)) return null;
			return Buyer(kind, Envoy.BombBuyerId, new string[] { L.T("电话里说的礼物呢？交给我。", "Where is the gift you called about? Hand it over.") }, Array.Empty<string>(), 5000, talkFirst: true);
		case "rebel:ch1":
			return Envoy.Talk(kind, OdinName, "wanted4", new string[3]
			{
				L.T("你帮了我们不少。我想请你再帮一个忙。", "You've helped us a lot. I need one more favor."),
				L.T($"弄一瓶毒药给我们。弄不到的话，价值 {100}₵ 的梦尘和芬太片也行。", $"Get us a bottle of poison. If you can't, Dream Dust and Phanta Pills worth {100}₵ will do."),
				L.T("准备好了就打我们的电话，我会叫人来取。", "When it's ready, call us and I'll send someone.")
			}, null, null, () =>
			{
				r.Active = 1;
			}, "FACTION_REVOLUTION");
		case "rebel:ch2":
			return Envoy.Talk(kind, OdinName, "wanted4", new string[3]
			{
				L.T("我们需要一批炸药，子午线的重火力也勉强可以。", "We need explosives. Meridian's heavy firepower would do in a pinch."),
				L.T($"子午线那边我知道你有关系可以打点：合作关系够的话 {200}₵，不然 {500}₵，他们会送一颗炸弹过来，我来买。", $"I know you have connections at Meridian, so they can be bought: {200}₵ if you're on good terms, {500}₵ otherwise. They'll send a bomb, and I'll buy it."),
				L.T($"不然的话，通过地下买家卖出价值 {1000}₵ 的武器。", $"Otherwise, sell {1000}₵ worth of weapons through the underground buyer.")
			}, null, null, () =>
			{
				r.Active = 2;
			}, "FACTION_REVOLUTION");
		case "rebel:odinbomb":
			return Buyer(kind, "rebel_odin_buyer_bomb", new string[1] { L.T("子午线的货到了？给我。", "Meridian's package arrived? Hand it over.") }, Array.Empty<string>(), 5000, talkFirst: true);
		case "rebel:seller":
			return Seller(kind);
		case "rebel:buyer":
		{
			bool flag = r.Active == 4;
			return Buyer(kind, "rebel_odin_buyer", new string[1] { flag ? L.T("老规矩：武器和医疗品，有多少收多少。", "Same as always: weapons and medical supplies, as much as you've got.") : L.T("大清缴以后伤员太多了。医疗品，有多少收多少。", "Too many wounded since the great crackdown. Medical supplies, as much as you've got.") }, (!flag) ? new string[1] { "MEDICAL" } : new string[2] { "MEDICAL", "WEAPON" }, Settings.VanillaBudget(), talkFirst: false);
		}
		case "rebel:invite":
			return Envoy.Talk(kind, OdinName, "wanted4", new string[2]
			{
				L.T("你拒绝了他们。我听说了。", "You turned them down. I heard."),
				L.T("加入我们吧。", "Join us.")
			}, L.T("怎么样？", "Well?"), new (string, string, Action)[2]
			{
				Envoy.Opt(L.T("我加入。", "I'm in."), L.T("拿着这个。", "Take this."), JoinRebels),
				Envoy.Opt(L.T("让我再想想。", "Let me think about it."), L.T("想好了打电话叫我。", "Call me when you've decided."), () =>
				{
					r.Invited = true;
				})
			}, null, "FACTION_REVOLUTION");
		case "rebel:raid":
			return Envoy.Talk(kind, OdinName, "wanted4", new string[2]
			{
				L.T("够了。我们准备好了。", "That's enough. We're ready."),
				L.T("三天后，我们去拔掉巨企在下层区的代理人站点。", "In three days, we take out the corporations' agent station in the Lower Levels.")
			}, null, null, () =>
			{
				r.RaidDay = Day + 3;
			}, "FACTION_REVOLUTION");
		case "rebel:pickup":
			return Buyer(kind, "rebel_pickup", new string[2]
			{
				L.T("奥丁让我来取东西。", "Odin sent me to pick something up."),
				L.T("放这儿就行。", "Just put it here.")
			}, PickupTags(), 99999, talkFirst: false);
		default:
			return null;
		}
	}

	public static string CallPickup()
	{
		if (!PickupOpen)
		{
			return L.T("现在没有要送的东西。", "Nothing needs picking up right now.");
		}
		if (Envoy.Booked("rebel:pickup"))
		{
			return L.T("已经约好了，明天来人。", "Already booked: someone comes tomorrow.");
		}
		Envoy.Book("rebel:pickup", Day + 1);
		return null;
	}

	private static string[] PickupTags()
	{
		if (Rebels.Arrested)
		{
			return new string[2] { "MEDICAL", "WEAPON" };
		}
		return new string[1] { "POISON" };
	}

	public static double BuyFactor(GameItem item)
	{
		string text = Store?.currentClientInstance?.GetClientBlueprint()?.identifier;
		if (text == "rebel_pickup")
		{
			return 0.0;
		}
		if (text == "rebel_odin_buyer_bomb" && item.IsTag("CORP_REBEL_BOMB"))
		{
			return 1.5;
		}
		return 1.0;
	}

	public static bool ClientBuys(StoreClient client, GameItem item)
	{
		if (!Malice.On)
		{
			return false;
		}
		if (client.identifier == "rebel_pickup")
		{
			RebelState r = R;
			if (r != null && r.Active == 1 && !Rebels.Arrested)
			{
				return Ch1Ids.Contains(item.identifier);
			}
		}
		if (client.identifier == "rebel_odin_buyer_bomb")
		{
			return item.IsTag("CORP_REBEL_BOMB");
		}
		return false;
	}

	public static void SoldToClient(StoreClient client, GameItem item, int amount)
	{
		RebelState r = R;
		if (r == null || client == null || item == null)
		{
			return;
		}
		long num = ValueOf(item);
		switch (client.identifier)
		{
		case "rebel_pickup":
			if (Rebels.Arrested)
			{
				if (item.IsTag("MEDICAL") || item.IsTag("WEAPON"))
				{
					r.RescueValue += (int)num;
					if (r.RescueValue >= 350)
					{
						Rescue();
					}
				}
			}
			else if (r.Active == 1)
			{
				if (item.IsTag("POISON"))
				{
					r.Ch1Poison = true;
				}
				else if (Ch1Ids.Contains(item.identifier))
				{
					r.Ch1Value += (int)num;
				}
				if (r.Ch1Poison || r.Ch1Value >= 100)
				{
					Complete(1);
				}
			}
			break;
		case "rebel_odin_buyer_bomb":
			if (r.Active == 2 && item.IsTag("CORP_REBEL_BOMB"))
			{
				Complete(2);
			}
			break;
		case "rebel_odin_buyer":
			if (r.Active == 3 && item.IsTag("MEDICAL"))
			{
				r.Ch3Medical += amount;
				if (r.Ch3Medical >= 1200)
				{
					Complete(3);
				}
			}
			else if (r.Active == 4 && r.Joined && (item.IsTag("MEDICAL") || item.IsTag("WEAPON")))
			{
				r.Ch4Sales += amount;
				if (r.Ch4Sales >= 3000 && !Envoy.Booked("rebel:raid") && r.RaidDay < 0)
				{
					Envoy.Book("rebel:raid", Day + 1);
				}
			}
			break;
		case "malice_fence":
			if (r.Active == 2 && item.IsTag("WEAPON"))
			{
				r.Ch2FenceWeapons += amount;
				if (r.Ch2FenceWeapons >= 1000)
				{
					Complete(2);
				}
			}
			break;
		}
	}

	private static long ValueOf(GameItem item)
	{
		try
		{
			return item.GetCurrentValue(useRetailMarkup: false, withChild: true, includeEvents: false, forceMarkup: false, 0L) * Math.Max(1, item.unitCount);
		}
		catch
		{
			return 0L;
		}
	}

	private static void Complete(int ch)
	{
		RebelState r = R;
		if (r.Active == ch)
		{
			r.Done = ch;
			r.Active = 0;
			switch (ch)
			{
			case 1:
				Rebels.AddPoints(100, "反抗军第 1 章");
				Notify(L.T($"奥丁收到了东西。反抗军助力点 +{100}。", $"Odin got what he needed. +{100} support points."));
				break;
			case 2:
				Notify(L.T("奥丁：“谢了。之后武器可能会涨价，囤点货吧。”", "Odin: “Thanks. Weapon prices might go up soon. Stock up.”"));
				QueueEvent("malice_raid_armory", L.T("革命军突袭治安部武器库", "Revolutionaries Raid Security Armory"), L.T("武器库遇袭", "Armory Raided"), L.T("革命军袭击了治安部的一座武器库。站内武器紧缺，价格上涨。", "Revolutionaries struck a Security armory. Weapons are scarce station-wide and prices are up."), 25, 3, 4);
				break;
			case 3:
				GiveMeridianCrate();
				Notify(L.T("奥丁送来一箱子午线的枪：清缴的时候那些治安部身上搜出来的。", "Odin sent a crate of Meridian guns, taken off Security officers during the crackdown."));
				break;
			}
			ModLog.Info($"恶意巨企：反抗军第 {ch} 章完成");
		}
	}

	public static int Ch2BribePrice()
	{
		if ((Companies.StateOf(Companies.Find("meridian"))?.Coop ?? 0) <= 20)
		{
			return 500;
		}
		return 200;
	}

	public static string BribeMeridian()
	{
		return OrderBomb(replacement: false);
	}

	public static string ReorderBomb()
	{
		return OrderBomb(replacement: true);
	}

	private static string OrderBomb(bool replacement)
	{
		Func<bool> isCurrent = MaliceGuards.CaptureSession();
		RebelState r = R;
		if (!isCurrent() || _orderingBomb || !(replacement ? Ch2ReorderOpen : Ch2BribeOpen))
		{
			return L.T("现在不需要。", "Not needed right now.");
		}
		if (replacement)
		{
			if (!TryFindBomb(out var present))
			{
				return L.T("暂时无法检查库存，尚未扣款。", "Inventory could not be checked. You have not been charged.");
			}
			if (present)
			{
				return L.T("库存里还有任务炸弹，无需补订。", "The mission bomb is still in storage.");
			}
		}
		if (!isCurrent() || !ReferenceEquals(r, R))
			return L.T("存档已变化，预约已停止，尚未扣款。", "The save changed; booking stopped before payment.");
		int num = Ch2BribePrice();
		PlayerStore store = Store;
		if (store.GetCash() < num)
		{
			return L.T("现金不够", "Not enough cash");
		}
		int num2 = Day + 1;
		_orderingBomb = true;
		try
		{
			if (!isCurrent() || !ReferenceEquals(r, R))
				return L.T("存档已变化，预约已停止，尚未扣款。", "The save changed; booking stopped before payment.");
			store.PayMoney(num, playRegisterSound: true);
			if (!isCurrent() || !ReferenceEquals(r, R))
				return L.T("存档已变化，已停止旧预约。", "The save changed; the old booking was stopped.");
			r.Ch2Route = "bribe";
			r.Ch2BombDay = num2;
			ModLog.Info($"子午线炸弹已付款 {num}₵，预定第 {num2} 天开店送达。");
			return null;
		}
		finally
		{
			_orderingBomb = false;
		}
	}

	private static bool TryFindBomb(out bool present)
	{
		present = false;
		try
		{
			EmporiumEntry instance = EmporiumEntry.Instance;
			if (SaveSlot.CurrentStore == null || instance == null)
			{
				return false;
			}
			present = Store.FindAllItem(isOwn: false).Any(IsMissionBomb) || instance.GetAllItems().Any(IsMissionBomb);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMissionBomb(GameItem item)
	{
		if (item != null && !StorePlacement.Destroyed(item))
		{
			return item.IsTag("CORP_REBEL_BOMB");
		}
		return false;
	}

	private static bool DeliverDueBomb(RebelState state)
	{
		Func<bool> isCurrent = MaliceGuards.CaptureSession();
		if (!isCurrent() || !ReferenceEquals(state, R)) return false;
		if (state.Active != 2 || state.Ch2BombDay < 0 || Day < state.Ch2BombDay)
		{
			return false;
		}
		if (!TryFindBomb(out var present))
		{
			ModLog.Warn("子午线炸弹暂无法核对库存，保留已付款订单。");
			return false;
		}
		if (!isCurrent() || (!present && !GiveBomb()))
		{
			return false;
		}
		if (!isCurrent() || !ReferenceEquals(state, R)) return false;
		state.Ch2BombDay = -1;
		ModLog.Info(present ? "子午线炸弹订单已结清，库存中已有任务炸弹。" : "子午线炸弹已确认入库。");
		if (!present)
		{
			Notify(L.T("子午线送来的炸弹已入库。", "The bomb from Meridian has been delivered."));
		}
		return true;
	}

	private static bool GiveBomb()
	{
		Func<bool> isCurrent = MaliceGuards.CaptureSession();
		GameItem gameItem = null;
		try
		{
			gameItem = ItemRegistry.Create("c4");
			if (gameItem == null)
			{
				ModLog.Warn("子午线炸弹物品工厂不可用，保留已付款订单。");
				return false;
			}
			gameItem.EnableTag("CORP_REBEL_BOMB");
			gameItem.SetName(L.T("子午线的“样品”", "Meridian “Sample”"));
			gameItem.shortDescription = L.T("子午线私下送来的炸药。奥丁会来买。", "Explosives Meridian sent on the quiet. Odin will come for it.");
			if (isCurrent() && StorePlacement.Place(gameItem))
			{
				return true;
			}
			ModLog.Warn("子午线炸弹未能入库，保留订单，下次开店重试。");
		}
		catch (Exception ex)
		{
			ModLog.Warn("子午线炸弹生成失败：" + ex.Message);
		}
		if (StorePlacement.Attached(gameItem))
		{
			return true;
		}
		try
		{
			if (gameItem != null && !StorePlacement.Destroyed(gameItem))
			{
				gameItem.Destroy();
			}
		}
		catch (Exception ex2)
		{
			ModLog.Warn("未送达的炸弹清理失败：" + ex2.Message);
		}
		return false;
	}

	private static void StartCh3()
	{
		RebelState r = R;
		r.Active = 3;
		r.Ch3Start = Day + 1;
		r.OdinBuyDue = Day + 4;
		QueueEvent("malice_crackdown", L.T("巨企协助治安部清缴反抗军", "Corporations Back Security Crackdown"), L.T("清缴", "Crackdown"), L.T("巨企协助治安部清缴反抗军：反抗军损失惨重，同时治安部得到大量补给。贪腐之下，大量武器流入市场。", "The corporations are backing a Security crackdown on the Resistance: heavy losses for the rebels, while Security gets a flood of supplies. Thanks to graft, plenty of weapons are leaking onto the market."), -25, 3, 1);
	}

	private static StoreClient Seller(string kind)
	{
		Func<bool> isCurrent = MaliceGuards.CaptureSession();
		try
		{
			string text = L.T("子午线的人", "Meridian Man");
			StoreClient client = new StoreClient
			{
				identifier = "rebel_meridian_seller",
				displayName = text,
				spriteName = SpriteDict.GetRandomSecSpriteName(),
				clientFaction = "FACTION_SECURITY",
				clientIntent = StoreClient.ClientIntent.SELL,
				dismissable = true
			};
			client.AddBasicDialog();
			client.CompleteClientCreation();
			client.SetWholesale(20);
			bool placed = false;
			Dialogue dialogue = new Dialogue().SetText(text, L.T("清缴“多出来”的货。你要的话便宜给你。", "Surplus from the crackdown. Cheap, if you want it."));
			dialogue.SetEndAction(() =>
			{
				if (!isCurrent()) return;
				if (!placed)
				{
					placed = true;
					Envoy.Done(kind);
					string[] array = new string[3] { "glock_receiver", "smg", "shotgun" };
					System.Random random = new System.Random();
					for (int i = 0; i < 3; i++)
					{
						if (!isCurrent()) return;
						try
						{
							GameItem gameItem = DirectoryMaster.Item(array[random.Next(array.Length)], isOwned: false);
							GunHelper.SetEquipmentCondition(gameItem, (random.Next(2) == 0) ? ItemFeatureList.EquipmentCondition.Good : ItemFeatureList.EquipmentCondition.Fair);
							if (!Store.TryAddDirectSellingItemToTable(gameItem))
							{
								gameItem.Destroy();
							}
						}
						catch
						{
						}
					}
				}
				client.OnIntroduced();
			});
			client.mainDialogue = dialogue;
			client.allDoneDialogue = new Dialogue().SetText(text, L.T("回见。", "Later.")).SetEndAction(() =>
			{
				if (!isCurrent()) return;
				try
				{
					Store.RemoveNotOwnedItemFromTable();
				}
				catch
				{
				}
				try
				{
					UnityEngine.Object.FindObjectOfType<DialogUIManager>()?.CurrentClientLeave();
				}
				catch
				{
				}
			});
			client.acceptDealDialogue = client.allDoneDialogue;
			return client;
		}
		catch (Exception ex)
		{
			ModLog.Warn("子午线卖枪的人生成失败：" + ex.Message);
			return null;
		}
	}

	private static void GiveMeridianCrate()
	{
		try
		{
			GameItem gameItem = Goods.Make("crate_meridian");
			if (gameItem == null)
			{
				return;
			}
			GeneralHelper.SetItemOwned(gameItem);
			string[] array = new string[3] { "glock_receiver", "smg", "shotgun" };
			System.Random random = new System.Random();
			long num = 0L;
			for (int i = 0; i < 12; i++)
			{
				if (num >= 1500)
				{
					break;
				}
				GameItem gameItem2 = DirectoryMaster.Item(array[random.Next(array.Length)]);
				GunHelper.SetEquipmentCondition(gameItem2, (random.Next(2) == 0) ? ItemFeatureList.EquipmentCondition.Good : ItemFeatureList.EquipmentCondition.Fair);
				long num2 = ValueOf(gameItem2);
				if (!Crates.Put(gameItem, gameItem2))
				{
					gameItem2.Destroy();
					break;
				}
				num += num2;
			}
			if (!StorePlacement.Place(gameItem))
			{
				ModLog.Warn("恶意巨企：店里所有库存都放满了，" + gameItem.identifier + " 没送到");
			}
			ModLog.Info($"恶意巨企：子午线武器箱 {num}₵");
		}
		catch (Exception ex)
		{
			ModLog.Warn("子午线武器箱失败：" + ex.Message);
		}
	}

	public static string CallOdin()
	{
		if (!CanCallOdin)
		{
			return L.T("现在不需要。", "Not needed right now.");
		}
		Envoy.Book("rebel:invite", Day + 1);
		return null;
	}

	private static void JoinRebels()
	{
		RebelState r = R;
		r.Invited = true;
		r.Joined = true;
		r.OdinBuyDue = Day + 7;
		Envoy.GiveItem("rebel_token");
	}

	private static void Raid()
	{
		RebelState r = R;
		EnvoyState envoy = MaliceState.Get().Envoy;
		r.Appeased = true;
		r.Done = 4;
		r.Active = 0;
		envoy.Gone = true;
		try
		{
			Notebook.Refresh();
		}
		catch
		{
		}
		Notify(L.T("巨企意识到下层脱离掌控开始安抚下层区：所有订货九折，不再收订阅费。", "Realizing the Lower Levels are slipping out of their grip, the corporations are placating them: all orders 10% off, no more subscription fees."));
		QueueEvent("malice_devil_dies", L.T("恶魔之死", "Death of a Devil"), L.T("恶魔之死", "Death of a Devil"), L.T("联合站区管理处专员撒该·索恩在下层区遇袭身亡。治安部封锁了现场，武器价格上涨。", "Joint District Administration officer Zacchaeus Thorne was killed in an attack in the Lower Levels. Security has sealed the area; weapon prices are up."), 30, 3, 0);
	}

	public static void OnReported(string clientId, int revDelta)
	{
		RebelState r = R;
		if (Malice.On && r != null && clientId != null && clientId.StartsWith("wanted4"))
		{
			r.OdinRepLost = Math.Max(0, -revDelta);
			r.RescueValue = 0;
			ModLog.Info($"恶意巨企：奥丁被举报，反抗军声望 {revDelta}");
		}
	}

	private static void Rescue()
	{
		RebelState r = R;
		MaliceState maliceState = MaliceState.Get();
		try
		{
			StorePhoneClient.GetRevMerchantClient().phoneState = StorePhoneClient.PhoneState.Regular;
		}
		catch
		{
		}
		if (r.OdinRepLost > 0)
		{
			try
			{
				StoreReputation.ModReputation("FACTION_REVOLUTION", r.OdinRepLost);
			}
			catch
			{
			}
		}
		maliceState.OdinArrested = false;
		r.OdinRepLost = 0;
		r.RescueValue = 0;
		Notify(L.T("奥丁被救出来了。反抗军恢复了联系。", "Odin has been rescued. The Resistance is back in touch."));
	}

	public static void Schedule(StoreClientManager manager)
	{
		RebelState r = R;
		if (!Malice.On || r == null || !r.RepCert || r.BoostDay == Day || manager?.clientStack == null)
		{
			return;
		}
		r.BoostDay = Day;
		foreach (StoreClient item in manager.clientStack)
		{
			try
			{
				if (Malice.OrdinaryVanilla(item) && item.GetBudget() > 0)
				{
					item.OverrideBudget((int)Math.Round((double)item.GetBudget() * 1.5));
				}
			}
			catch
			{
			}
		}
	}

	private static StoreClient Buyer(string kind, string id, string[] lines, string[] tags, int budget, bool talkFirst)
	{
		Func<bool> isCurrent = MaliceGuards.CaptureSession();
		try
		{
			StoreClient client = new StoreClient
			{
				identifier = id,
				displayName = ((id == "rebel_pickup") ? L.T("反抗军联络员", "Resistance Runner") : OdinName),
				spriteName = ((id == "rebel_pickup") ? SpriteDict.GetRandomRevSpriteName() : "wanted4"),
				clientFaction = "FACTION_REVOLUTION",
				clientIntent = StoreClient.ClientIntent.BUY,
				acceptRetailMarkup = (id != "rebel_pickup"),
				dismissable = true
			};
			foreach (string item in tags)
			{
				client.clientBuyingTagList.Add(item);
			}
			client.AddBasicDialog();
			client.CompleteClientCreation();
			client.SetBudget(budget);
			Dialogue dialogue = new Dialogue().SetText(client.displayName, lines[0]);
			Dialogue dialogue2 = dialogue;
			for (int j = 1; j < lines.Length; j++)
			{
				dialogue2 = dialogue2.NextDialogue().SetText(client.displayName, lines[j]);
			}
			dialogue2.SetEndAction(() =>
			{
				if (!isCurrent()) return;
				Envoy.Done(kind);
				client.OnIntroduced();
			});
			client.mainDialogue = dialogue;
			return client;
		}
		catch (Exception ex)
		{
			ModLog.Warn("恶意巨企：" + kind + " 生成失败：" + ex.Message);
			return null;
		}
	}

	public static string DebugEvent(int i)
	{
		switch (i % 3)
		{
		case 0:
			QueueEvent("malice_raid_armory", L.T("革命军突袭治安部武器库", "Revolutionaries Raid Security Armory"), L.T("武器库遇袭", "Armory Raided"), L.T("（测试）武器价格上涨。", "(Test) Weapon prices are up."), 25, 3, 0);
			return "武器库遇袭 +25%";
		case 1:
			QueueEvent("malice_crackdown", L.T("巨企协助治安部清缴反抗军", "Corporations Back Security Crackdown"), L.T("清缴", "Crackdown"), L.T("（测试）武器流入市场。", "(Test) Weapons flood the market."), -25, 3, 0);
			return "清缴 -25%";
		default:
			QueueEvent("malice_devil_dies", L.T("恶魔之死", "Death of a Devil"), L.T("恶魔之死", "Death of a Devil"), L.T("（测试）武器价格上涨。", "(Test) Weapon prices are up."), 30, 3, 0);
			return "恶魔之死 +30%";
		}
	}

	private static void QueueEvent(string id, string news, string display, string desc, int weaponPct, int days, int daysAhead)
	{
		try
		{
			StoreEvent storeEvent = new StoreEvent
			{
				identifier = id,
				newsName = news,
				displayName = display,
				newsDescription = desc,
				duration = days,
				importance = 8,
				eventType = StoreEvent.EventType.NORMALE,
				eventArea = StoreEvent.EventArea.ALL
			};
			storeEvent.negociationDatas.Add(new NegociationData("WEAPON", weaponPct, storeEvent.newsName, storeEvent.displayName));
			if (daysAhead <= 0)
			{
				StoreStation.Instance.storeEventManager.ActivateEvent(storeEvent, debugLog: false, force: true);
			}
			else
			{
				StoreStation.Instance.storeEventManager.QueueFuturEvent(storeEvent, daysAhead, IsKnown: true);
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("恶意巨企：事件 " + id + " 失败：" + ex.Message);
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
