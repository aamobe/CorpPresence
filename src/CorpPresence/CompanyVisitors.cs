using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.Malice;
using UnityEngine;

namespace CorpPresence;

internal static class CompanyVisitors
{
	private static readonly System.Random Rng = new System.Random();

	private static readonly string[] PastelGoods = new string[8] { "nutrifruit", "galaxy_blend", "shampoo", "toothpaste", "pack_condom", "box_tampon", "kotton_fabric", "@hemingway" };

	private static int Day => StoreStation.GetDayCounter();

	public static void Schedule(StoreClientManager manager)
	{
		if (manager?.clientStack == null)
		{
			return;
		}
		Company[] all = Companies.All;
		foreach (Company company in all)
		{
			CompanyState companyState;
			try
			{
				companyState = Companies.StateOf(company);
			}
			catch (Exception ex)
			{
				WarnScheduling("公司状态未就绪，暂不安排访客：" + ex.Message);
				continue;
			}
			if (companyState == null)
			{
				continue;
			}
			try
			{
				if (companyState.Orders != null && ServiceRetry.Ready(companyState, Time.unscaledTime))
				{
					List<Order> list = companyState.Orders.Where((Order o) => o != null && o.Day <= Day).ToList();
					bool flag = Services.CardDue(company, companyState);
					if (((list.Count > 0) | flag) && !Queued(manager, "corp_" + company.Id + "_courier"))
					{
						if (Insert(manager, Courier(company, companyState, list, flag), front: true))
						{
							foreach (Order item in list)
							{
								item.Day = Day + 1;
							}
							if (flag)
							{
								companyState.CardCourierDay = Day;
							}
							ServiceRetry.Succeeded(companyState);
						}
						else
						{
							ServiceRetry.Failed(companyState, Time.unscaledTime);
						}
					}
				}
			}
			catch (Exception ex2)
			{
				ServiceRetry.Failed(companyState, Time.unscaledTime);
				WarnScheduling("配送安排失败，保留待送订单：" + ex2.Message);
			}
			if (companyState.Visitors == null)
			{
				continue;
			}
			foreach (Visitor item2 in companyState.Visitors.Where((Visitor v) => v != null && v.Day <= Day).ToList())
			{
				if (!ServiceRetry.Ready(item2, Time.unscaledTime))
				{
					continue;
				}
				try
				{
					if (TryScheduleVisitor(manager, company, companyState, item2))
					{
						companyState.Visitors.Remove(item2);
						ServiceRetry.Succeeded(item2);
					}
					else
					{
						ServiceRetry.Failed(item2, Time.unscaledTime);
					}
				}
				catch (Exception ex3)
				{
					ServiceRetry.Failed(item2, Time.unscaledTime);
					WarnScheduling("访客安排失败，保留预约：" + ex3.Message);
				}
			}
		}
	}

	private static bool TryScheduleVisitor(StoreClientManager manager, Company c, CompanyState state, Visitor visit)
	{
		switch (visit.Kind)
		{
		case "traffic":
		{
			int num = Rng.Next(1, 3);
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				try
				{
					StoreClient client = ((c is SpacerCo) ? SpacerClient() : Buyer("FACTION_LOWER_LEVEL", 0));
					if (Insert(manager, client))
					{
						num2++;
					}
				}
				catch (Exception ex)
				{
					WarnScheduling("介绍顾客生成失败：" + ex.Message);
				}
			}
			if (num2 == 0)
			{
				return false;
			}
			LogScheduled((c is SpacerCo) ? $"街坊互助：今天多来 {num2} 位星际客" : $"帕斯泰尔介绍来 {num2} 位下层区顾客");
			return true;
		}
		case "vip":
		{
			int num3 = SolanaCo.VipBudget();
			StoreClient client2 = Vip(num3);
			if (!Insert(manager, client2))
			{
				return false;
			}
			LogScheduled($"索拉纳引荐的贵宾今天来，安排预算 {num3}₵");
			return true;
		}
		case "lab":
			return Insert(manager, LabBuyer(c, state));
		default:
		{
			Func<Company, string, StoreClient> spawnVisitor = Ext.SpawnVisitor;
			if (spawnVisitor == null)
			{
				return true;
			}
			StoreClient storeClient = spawnVisitor(c, visit.Kind);
			if (storeClient != null)
			{
				return Insert(manager, storeClient);
			}
			return true;
		}
		}
	}

	private static void LogScheduled(string text)
	{
		try
		{
			ModLog.Info(text);
		}
		catch
		{
		}
	}

	private static void WarnScheduling(string text)
	{
		try
		{
			ModLog.Warn(text);
		}
		catch
		{
		}
	}

	private static StoreClient SpacerClient()
	{
		try
		{
			return Rng.Next(6) switch
			{
				0 => StoreClientListTier.CreateHungrySpacer(), 
				1 => StoreClientListTier.CreateThirstySpacer(), 
				2 => StoreClientListTier.CreateInjuredSpacer(), 
				3 => StoreClientListTier.CreateYoungSpacer(), 
				4 => StoreClientListTierSubstance.CreateWornOutSpacer(), 
				_ => StoreClientListTier.CreateSpacerChef(), 
			};
		}
		catch (Exception ex)
		{
			ModLog.Warn("星际客顾客生成失败：" + ex.Message);
			return null;
		}
	}

	private static bool Queued(StoreClientManager m, string prefix)
	{
		foreach (StoreClient item in m.clientStack)
		{
			if (item != null && item.identifier != null && item.identifier.StartsWith(prefix))
			{
				return true;
			}
		}
		return false;
	}

	private static bool Insert(StoreClientManager m, StoreClient client, bool front = false)
	{
		if (m?.clientStack == null || client == null)
		{
			return false;
		}
		int count = m.clientStack.Count;
		int index = ((!front && count > 1) ? Rng.Next(0, count / 2 + 1) : 0);
		m.clientStack.Insert(index, client);
		return true;
	}

	private static StoreClient Vip(int budget)
	{
		try
		{
			string text = L.T("索拉纳贵宾", "Solana VIP");
			StoreClient storeClient = new StoreClient
			{
				identifier = "solana_vip",
				displayName = text,
				spriteName = SpriteDict.GetRandomUpperSpriteName(),
				clientFaction = "FACTION_UPPER_LEVEL",
				clientIntent = StoreClient.ClientIntent.BUY
			};
			storeClient.SetBudget(budget);
			storeClient.mainDialogue.SetText(text, L.T("索拉纳的顾问推荐了你的店。", "A Solana consultant recommended your shop.")).NextDialogue().SetText(text, L.T("听说你这儿有好东西。奢侈品、好烟、好酒，拿出来看看。", "I hear you carry fine things. Luxury goods, good smokes, good liquor. Let's see them."));
			string[] array = new string[3] { "LUXURY_ITEM", "CIGARETTE_TAG", "ALCOHOL" };
			foreach (string item in array)
			{
				storeClient.clientBuyingTagList.Add(item);
			}
			storeClient.AddBasicDialog();
			storeClient.CompleteClientCreation();
			return storeClient;
		}
		catch (Exception ex)
		{
			ModLog.Warn("贵宾生成失败：" + ex.Message);
			return null;
		}
	}

	private static StoreClient Buyer(string faction, int budget)
	{
		try
		{
			StoreClientManager storeClientManager = PlayerStore.Instance.storeClientManager;
			for (int i = 0; i < 12; i++)
			{
				StoreClient storeClient = storeClientManager.PickRandomClient(faction);
				if (storeClient != null && storeClient.clientIntent == StoreClient.ClientIntent.BUY)
				{
					if (budget > 0)
					{
						storeClient.OverrideBudget(budget);
					}
					return storeClient;
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("介绍顾客失败：" + ex.Message);
		}
		return null;
	}

	private static StoreClient Courier(Company c, CompanyState s, List<Order> orders, bool card = false)
	{
		bool flag = c is SolanaCo;
		bool flag2 = c is CarmichaelCo;
		bool flag3 = c is SpacerCo;
		bool flag4 = c is MeridianCo;
		bool flag5 = c is DavreCo;
		bool flag6 = c is RebelCo;
		bool flag7 = flag | flag2;
		string text;
		if (L.En)
		{
			if (flag6)
			{
				text = "Resistance Runner";
			}
			else if (flag5)
			{
				text = "Davre Courier";
			}
			else if (flag4)
			{
				text = "Meridian Courier";
			}
			else if (flag3)
			{
				text = "Spacer Runner";
			}
			else if (flag2)
			{
				text = "Carmicheal Courier";
			}
			else
			{
				text = (flag ? "Solana Courier" : "pastel Courier");
			}
		}
		else if (flag6)
		{
			text = "反抗军联络员";
		}
		else if (flag5)
		{
			text = "达夫雷送货员";
		}
		else if (flag4)
		{
			text = "子午线专人配送";
		}
		else if (flag3)
		{
			text = "星际客跑腿";
		}
		else if (flag2)
		{
			text = "卡迈克尔专人配送";
		}
		else
		{
			text = (flag ? "索拉纳专人配送" : "帕斯泰尔送货员");
		}
		string spriteName;
		if (flag6)
		{
			spriteName = SpriteDict.GetRandomRevSpriteName();
		}
		else if (flag4)
		{
			spriteName = SpriteDict.GetRandomSecSpriteName();
		}
		else
		{
			spriteName = (flag7 ? SpriteDict.GetRandomUpperSpriteName() : SpriteDict.GetRandomLowerLevelSpriteName());
		}
		int num = orders.Sum((Order o) => o.Cash);
		bool flag8 = orders.Any((Order o) => o.Items.Count > 0 && o.Kind != "stamina" && o.Kind != "swap" && o.Kind != "refurbish");
		List<string> list = new List<string>();
		if (flag8)
		{
			string item;
			if (L.En)
			{
				if (flag6)
				{
					item = "Odin sent this. Don't ask where it came from.";
				}
				else if (flag5)
				{
					item = "Davre delivery. Check the count and sign.";
				}
				else if (flag4)
				{
					item = "Meridian delivery. Check the serials.";
				}
				else if (flag3)
				{
					item = "From the docks.";
				}
				else if (flag2)
				{
					item = "Carmicheal Pharmaceutical delivery. Please check the lot numbers and sign.";
				}
				else
				{
					item = (flag ? "Solana Biogenics, personal delivery. Please sign." : "pastel delivery! I'll leave it right here.");
				}
			}
			else if (flag6)
			{
				item = "奥丁让我送来的。别问从哪儿来的。";
			}
			else if (flag5)
			{
				item = "达夫雷送货。核对数量，签收。";
			}
			else if (flag4)
			{
				item = "子午线配送。核对编号。";
			}
			else if (flag3)
			{
				item = "码头来的。";
			}
			else if (flag2)
			{
				item = "卡迈克尔制药配送。请核对批号后签收。";
			}
			else
			{
				item = (flag ? "索拉纳生物，专人配送。请签收。" : "帕斯泰尔送货！东西给你放这儿了。");
			}
			list.Add(item);
		}
		if (orders.Any((Order o) => o.Kind == "swap"))
		{
			list.Add(L.T("问题批次已替换为正品。请核对。", "The problem batch has been replaced with genuine product. Please check."));
		}
		if (orders.Any((Order o) => o.Kind == "refurbish"))
		{
			list.Add(L.T("返厂的枪。翻新好了。", "Your guns from the factory. Refurbished."));
		}
		if (orders.Any((Order o) => o.Kind == "stamina"))
		{
			list.Add(L.T("伊莎·兰登经办的特别申请。请签收。", "A special request handled by Isa Langdon. Please sign."));
		}
		if (num > 0)
		{
			list.Add(L.T($"这是这一期的返点，{num}₵。", $"Here's your rebate: {num}₵."));
		}
		if (card)
		{
			list.Add(flag3 ? L.T("导师让我带张便签。", "The Mentor asked me to bring you a note.") : L.T((Engine.Find(c.CharacterId)?.Name ?? c.Name) + "让我把名片带给你。", (Engine.Find(c.CharacterId)?.Name ?? c.Name) + " asked me to bring you a card."));
		}
		string text2;
		if (L.En)
		{
			if (flag5)
			{
				text2 = "<i>(wheels the empty cart away)</i>";
			}
			else if (flag4)
			{
				text2 = "<i>(salutes)</i>";
			}
			else if (flag3)
			{
				text2 = "<i>(nods)</i>";
			}
			else if (flag2)
			{
				text2 = "<i>(takes back the receipt)</i>Thank you for your cooperation.";
			}
			else
			{
				text2 = (flag ? "<i>(bows slightly)</i>Have a pleasant day." : "<i>(nods)</i>See you next time.");
			}
		}
		else if (flag5)
		{
			text2 = "<i>（推着空推车走了）</i>";
		}
		else if (flag4)
		{
			text2 = "<i>（敬了个礼）</i>";
		}
		else if (flag3)
		{
			text2 = "<i>（点点头）</i>";
		}
		else if (flag2)
		{
			text2 = "<i>（收回签收单）</i>谢谢配合。";
		}
		else
		{
			text2 = (flag ? "<i>（微微欠身）</i>祝您愉快。" : "<i>（点点头）</i>下回见。");
		}
		StoreClient storeClient = new StoreClient
		{
			identifier = $"corp_{c.Id}_courier_{Day}",
			displayName = text,
			clientFaction = (flag4 ? "FACTION_SECURITY" : (flag7 ? "FACTION_UPPER_LEVEL" : "FACTION_LOWER_LEVEL")),
			spriteName = spriteName,
			clientIntent = StoreClient.ClientIntent.DIALOGUE,
			dismissable = true
		};
		storeClient.AddBasicDialog();
		storeClient.CompleteClientCreation();
		if (list.Count == 0)
		{
			list.Add(L.T("送货。请签收。", "Delivery. Please sign."));
		}
		Dialogue dialogue = new Dialogue().SetText(text, list[0]);
		Dialogue dialogue2 = dialogue;
		for (int num2 = 1; num2 < list.Count; num2++)
		{
			dialogue2 = dialogue2.NextDialogue().SetText(text, list[num2]);
		}
		Dialogue dialogue3 = dialogue2.NextDialogue().SetText(text, text2);
		dialogue2.SetEndAction(CreateDeliveryReceipt(c, s, orders, card));
		dialogue3.SetEndAction(CreateCourierLeave(storeClient));
		storeClient.mainDialogue = dialogue;
		return storeClient;
	}

	public static StoreClient PastelSeller()
	{
		string text = L.T("帕斯泰尔货商", "pastel Trader");
		StoreClient client = new StoreClient
		{
			identifier = $"corp_pastel_seller_{Day}",
			displayName = text,
			clientFaction = "FACTION_LOWER_LEVEL",
			spriteName = SpriteDict.GetRandomLowerLevelSpriteName(),
			clientIntent = StoreClient.ClientIntent.SELL,
			dismissable = true
		};
		client.AddBasicDialog();
		client.CompleteClientCreation();
		client.SetWholesale(20);
		string[] stock = Engine.Pick(PastelGoods, 3, 5);
		Dialogue dialogue = new Dialogue().SetText(text, L.T("看到你的灯牌了。", "Saw your sign."));
		Dialogue dialogue2 = dialogue.NextDialogue().SetText(text, L.T("帕斯泰尔的货，要吗？", "pastel goods. Interested?"));
		bool placed = false;
		dialogue2.SetEndAction(() =>
		{
			if (!placed)
			{
				placed = true;
				try
				{
					string[] array = stock;
					foreach (string text2 in array)
					{
						GameItem gameItem = ((!(text2 == "@hemingway")) ? ItemRegistry.Create(text2) : PastelCo.GenuineHemingway());
						if (gameItem != null && !PlayerStore.Instance.TryAddDirectSellingItemToTable(gameItem))
						{
							ModLog.Info("货商摆货：柜台放满了，少摆了 " + text2);
						}
					}
				}
				catch (Exception ex)
				{
					ModLog.Warn("货商摆货失败：" + ex.Message);
				}
			}
			client.OnIntroduced();
		});
		client.mainDialogue = dialogue;
		Dialogue dialogue3 = new Dialogue().SetText(text, L.T("<i>（点点头）</i>下回再来。", "<i>(nods)</i>I'll be back.")).SetEndAction(() =>
		{
			try
			{
				PlayerStore.Instance.RemoveNotOwnedItemFromTable();
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
		client.acceptDealDialogue = dialogue3;
		client.allDoneDialogue = dialogue3;
		return client;
	}

	private static StoreClient LabBuyer(Company c, CompanyState s)
	{
		string text = L.T("索拉纳采购员", "Solana Buyer");
		HashSet<string> list = new HashSet<string>(s.LabList);
		StoreClient client = new StoreClient
		{
			identifier = $"corp_{c.Id}_lab_{Day}",
			displayName = text,
			clientFaction = "FACTION_UPPER_LEVEL",
			spriteName = SpriteDict.GetRandomSciSpriteName(),
			clientIntent = StoreClient.ClientIntent.BUY,
			dismissable = true
		};
		client.AddBasicDialog();
		client.CompleteClientCreation();
		client.SetBudget(Settings.AgentBudget);
		client.willAcceptItem = (GameItem item) =>
		{
			try
			{
				return list.Contains(item.identifier);
			}
			catch
			{
				return false;
			}
		};
		client.placedWrongItemWhenSellingToDialogue = new Dialogue().SetText(text, L.T("单子上没有这个。", "That's not on the list."));
		Dialogue dialogue = new Dialogue().SetText(text, L.T("实验室的采购单。东西放台上。", "Purchase order from the lab. Put the items on the counter."));
		dialogue.SetEndAction(() =>
		{
			Engine.BuyActive = true;
			client.OnIntroduced();
		});
		client.mainDialogue = dialogue;
		Dialogue allDoneDialogue = new Dialogue().SetText(text, L.T("<i>（在单子上打了个勾）</i>收齐了。", "<i>(ticks the list)</i>That's everything.")).SetEndAction(() =>
		{
			Engine.BuyActive = false;
			try
			{
				UnityEngine.Object.FindObjectOfType<DialogUIManager>()?.CurrentClientLeave();
			}
			catch
			{
			}
		});
		client.acceptDealDialogue = new Dialogue().SetText(text, L.T("收下了。单子上还有的，接着放。", "Got it. If there's more from the list, keep it coming."));
		client.allDoneDialogue = allDoneDialogue;
		return client;
	}

	public static double LabMultiplier(StoreClient client, GameItem item)
	{
		CompanyState companyState = Companies.StateOf(Companies.Find("solana"));
		if (companyState == null)
		{
			return 1.0;
		}
		try
		{
			return companyState.LabList.Contains(item.identifier) ? SolanaCo.LabMultiplier(item.identifier) : 1.0;
		}
		catch
		{
			return 1.0;
		}
	}

	private static Action CreateCourierLeave(StoreClient client)
	{
		StoreSession session = StoreSession.Capture();
		bool done = false;
		return () =>
		{
			if (done || !session.IsCurrent || SaveSlot.CurrentStore.currentClientInstance?.GetClientBlueprint() != client)
			{
				return;
			}
			try
			{
				DialogUIManager dialogUIManager = UnityEngine.Object.FindObjectOfType<DialogUIManager>();
				if (!(dialogUIManager == null))
				{
					done = true;
					dialogUIManager.CurrentClientLeave();
				}
			}
			catch
			{
			}
		};
	}

	private static Action CreateDeliveryReceipt(Company c, CompanyState s, List<Order> orders, bool card)
	{
		PlayerStore store = SaveSlot.CurrentStore;
		CorpState root = CorpState.Current;
		string run = store?.runID;
		long generation = GameResources.Generation;
		bool done = false;
		return () =>
		{
			if (!done && IsCurrent())
			{
				done = true;
				foreach (Order order in orders)
				{
					if (!IsCurrent())
					{
						return;
					}
					if (order != null && s.Orders.Contains(order))
					{
						try
						{
							Deliver(c, s, order);
						}
						catch (Exception ex)
						{
							try
							{
								ModLog.Warn("本次订单配送中断，保留未完成明细：" + ex.Message);
							}
							catch
							{
							}
						}
					}
				}
				if (card && IsCurrent())
				{
					try
					{
						Services.MaybeGiveCard(c.CharacterId);
					}
					catch (Exception ex2)
					{
						try
						{
							ModLog.Warn("补送名片失败：" + ex2.Message);
						}
						catch
						{
						}
					}
				}
			}
		};
		bool IsCurrent()
		{
			if (StoreSession.Matches(store, root, run, generation) && c != null && s?.Orders != null && orders != null && root.Companies != null && root.Companies.TryGetValue(c.Id, out var value) && value == s)
			{
				return SaveSlot.CanWrite("CorpPresence");
			}
			return false;
		}
	}

	public static bool CounterFull()
	{
		try
		{
			GameGridInventory gameGridInventory = EmporiumEntry.Instance?.frontInvinvElement;
			return gameGridInventory != null && !gameGridInventory.CanTakeAnything();
		}
		catch
		{
			return false;
		}
	}

	private static void Deliver(Company c, CompanyState s, Order o)
	{
		o.Day = Day + 1;
		int num = 0;
		if (o.Items.Count > 0)
		{
			bool flag = CounterFull();
			if (!flag)
			{
				OrderContents.ExpandModBoxes(o, SpacerCo.RollModBox);
			}
			GameItem gameItem = (flag ? null : Goods.Make(Crates.GoodsKey(c)));
			if (gameItem == null)
			{
				if (!flag)
				{
					ModLog.Warn(c.Name + "送货箱生成失败，明天再送");
				}
				num = o.Items.Count;
			}
			else
			{
				List<string> list = new List<string>();
				foreach (string item in o.Items)
				{
					if (item == "@modbox")
					{
						num++;
						continue;
					}
					try
					{
						GameItem gameItem2 = MakeOrdered(c, item);
						if (gameItem2 == null)
						{
							ModLog.Warn("订货没生成出来，明天再送：" + item);
							continue;
						}
						GeneralHelper.SetItemOwned(gameItem2);
						if (Crates.Put(gameItem, gameItem2))
						{
							list.Add(item);
							continue;
						}
						try
						{
							gameItem2.Destroy();
						}
						catch
						{
						}
						num++;
					}
					catch (Exception ex)
					{
						ModLog.Warn("送货失败，明天再送：" + item + "：" + ex.Message);
					}
				}
				if (list.Count > 0 && TryPlaceDelivery(gameItem))
				{
					foreach (string item2 in list)
					{
						o.Items.Remove(item2);
					}
				}
				else if (list.Count > 0)
				{
					num += list.Count;
					try
					{
						gameItem.Destroy();
					}
					catch
					{
					}
				}
				else
				{
					try
					{
						gameItem.Destroy();
					}
					catch
					{
					}
				}
			}
		}
		if (num > 0)
		{
			ModLog.Info($"{c.Name}送货：{num} 件这次没送成（柜台放满或箱子装不下），明天再送");
			try
			{
				StoreUIManager.Instance?.Notify(L.T($"{c.Name}还有 {num} 件货这次没送成（柜台放满或箱子装满），明天再送来。", $"{c.Name} couldn't deliver {num} items this time (counter or crate full). They'll come tomorrow."));
			}
			catch
			{
			}
		}
		if (o.Cash > 0)
		{
			try
			{
				CompanyMoney.DeliverCash(PlayerStore.Instance, o);
			}
			catch (Exception ex2)
			{
				ModLog.Warn("返点发放失败，明天再送：" + ex2.Message);
			}
		}
		if (o.Items.Count == 0 && o.Cash == 0)
		{
			s.Orders.Remove(o);
		}
	}

	private static bool TryPlaceDelivery(GameItem crate)
	{
		if (CounterFull())
		{
			return false;
		}
		try
		{
			return PlayerStore.Instance.TryAddDirectSellingItemToTable(crate, isOwend: true) || crate.parentInventory != null;
		}
		catch
		{
			return crate.parentInventory != null;
		}
	}

	private static GameItem MakeOrdered(Company c, string id)
	{
		if (id.Contains('|'))
		{
			return MeridianCo.MakeRefurbished(id);
		}
		CatalogEntry catalogEntry = c.Catalog?.FirstOrDefault((CatalogEntry e) => e.Id == id);
		if (catalogEntry != null)
		{
			return catalogEntry.Make();
		}
		if (id.StartsWith("@", StringComparison.Ordinal))
		{
			Character character = Engine.Find(c.CharacterId);
			if (character != null)
			{
				return character.MakeItem(id);
			}
		}
		if (c is CarmichaelCo)
		{
			return CarmichaelCo.MakeGenuine(id);
		}
		return Goods.MakeSpecial(id) ?? ItemRegistry.Create(id);
	}
}
