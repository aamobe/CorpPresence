using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.Malice;
using UnityEngine;

namespace CorpPresence;

internal static partial class CompanyVisitors
{
	private static readonly System.Random Rng = new System.Random();

	private static readonly string[] PastelGoods = new string[8] { "nutrifruit", "galaxy_blend", "shampoo", "toothpaste", "pack_condom", "box_tampon", "kotton_fabric", "@hemingway" };

	private static int Day => StoreStation.GetDayCounter();

	public static void Schedule(StoreClientManager manager)
	{
		if (manager?.clientStack == null) return;
		foreach (Company c in Companies.All)
		{
			CompanyState state;
			try { state = Companies.StateOf(c); }
			catch (Exception error) { WarnScheduling("公司状态未就绪，暂不安排访客：" + error.Message); continue; }
			if (state == null) continue;
			try
			{
				if (state.Orders != null && ServiceRetry.Ready(state, Time.unscaledTime))
				{
					List<Order> due = state.Orders.Where(o => o != null && o.Day <= Day).ToList();
					bool card = Services.CardDue(c, state);
					if ((due.Count > 0 || card) && !Queued(manager, "corp_" + c.Id + "_courier"))
					{
						if (Insert(manager, Courier(c, state, due, card), front: true))
						{
							foreach (Order order in due) order.Day = Day + 1;
							if (card) state.CardCourierDay = Day;
							ServiceRetry.Succeeded(state);
						}
						else ServiceRetry.Failed(state, Time.unscaledTime);
					}
				}
			}
			catch (Exception error) { ServiceRetry.Failed(state, Time.unscaledTime); WarnScheduling("配送安排失败，保留待送订单：" + error.Message); }
			if (state.Visitors == null) continue;
			foreach (Visitor visit in state.Visitors.Where(v => v != null && v.Day <= Day).ToList())
			{
				if (!ServiceRetry.Ready(visit, Time.unscaledTime)) continue;
				try
				{
					if (TryScheduleVisitor(manager, c, state, visit))
					{
						state.Visitors.Remove(visit);
						ServiceRetry.Succeeded(visit);
					}
					else ServiceRetry.Failed(visit, Time.unscaledTime);
				}
				catch (Exception error) { ServiceRetry.Failed(visit, Time.unscaledTime); WarnScheduling("访客安排失败，保留预约：" + error.Message); }
			}
		}
	}

	private static bool TryScheduleVisitor(StoreClientManager manager, Company c, CompanyState state, Visitor visit)
	{
		switch (visit.Kind)
		{
		case "traffic":
			int target = Rng.Next(1, 3);
			int arrived = 0;
			for (int i = 0; i < target; i++)
			{
				try
				{
					StoreClient client = c is SpacerCo ? SpacerClient() : Buyer("FACTION_LOWER_LEVEL", 0);
					if (Insert(manager, client)) arrived++;
				}
				catch (Exception error) { WarnScheduling("介绍顾客生成失败：" + error.Message); }
			}
			if (arrived == 0) return false;
			LogScheduled(c is SpacerCo ? $"街坊互助：今天多来 {arrived} 位星际客" : $"帕斯泰尔介绍来 {arrived} 位下层区顾客");
			return true;
		case "vip":
			int budget = SolanaCo.VipBudget();
			StoreClient vip = Vip(budget);
			if (!Insert(manager, vip)) return false;
			LogScheduled($"索拉纳引荐的贵宾今天来，安排预算 {budget}₵");
			return true;
		case "lab":
			return Insert(manager, LabBuyer(c, state));
		default:
			// Preserve the extension's existing null-as-cancel convention.
			Func<Company, string, StoreClient> spawn = Ext.SpawnVisitor;
			if (spawn == null) return true;
			StoreClient extra = spawn(c, visit.Kind);
			return extra == null || Insert(manager, extra);
		}
	}

	private static void LogScheduled(string text) { try { ModLog.Info(text); } catch { } }
	private static void WarnScheduling(string text) { try { ModLog.Warn(text); } catch { } }

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
		if (m?.clientStack == null || client == null) return false;
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
		bool jackson = c is JacksonContact;
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
			if (jackson)
			{
				text = "Dr. Jackson's Courier";
			}
			else if (flag6)
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
		else if (jackson)
		{
			text = "杰克逊博士送货员";
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
			List<string> list2 = list;
			string item;
			if (L.En)
			{
				if (jackson)
				{
					item = "Machines and custom parts from Jackson's workshop. He tuned them himself. Check the order and sign.";
				}
				else if (flag6)
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
			else if (jackson)
			{
				item = "杰克逊工作间的货。机器和配件都是博士亲手调过的，核对一下，签收吧。";
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
			list2.Add(item);
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
}





