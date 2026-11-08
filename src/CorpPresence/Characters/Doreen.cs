using System;
using System.Linq;

namespace CorpPresence;

internal sealed class Doreen : Character
{
	private const string Peat = "processed_meat";

	private const string Noodle = "cup_noodle";

	public const long HydraPrice = 400L;

	public override string Id => "doreen";

	public override string Name => L.K("characters.doreen.name.doreen", "多琳", "Doreen");

	public override string Faction => "FACTION_LOWER_LEVEL";

	public override int[] Thresholds => new int[4] { 30, 50, 70, 50 };

	public override int CashNeeded => 300;

	public override string PortraitRef => "femaleLower_0";

	public override int BuyGoodsNeeded => 1;

	private static string Bye => L.K("characters.doreen.bye.payTimeGetTime", "按时付款，按时到货。", "Pay on time, get it on time.");

	private static string[] Talk => new string[3]
	{
		L.K("characters.doreen.talk.hmRecipePeatConfidential", "嗯？“泥炭”的配方？保密。", "Hm? The recipe for Peat? Confidential."),
		L.K("characters.doreen.talk.hungryHaveSomeDavreRamen", "饿了就吃达夫雷拉面，加水即可食用。水？当然也找我们买。", "Hungry? Have some Davre ramen. Just add water. Water? Of course you buy that from us too."),
		L.K("characters.doreen.talk.everyTimeSecurityHandsOut", "每次治安部发放口粮，我们的单子总是最多，多到工厂都要炸了。", "Every time Security hands out rations, we get the most orders. So many the factory's about to burst.")
	};

	public override string Portrait(CharacterState s, string kind)
	{
		return "davre_doreen";
	}

	public override bool IsSide(int chapter)
	{
		return chapter >= 3;
	}

	public override bool SideAllowed(int chapter)
	{
		return chapter switch
		{
			3 => DavreCo.IsWaterMerchant(), 
			4 => DavreCo.Coop() >= 60 && Wilde.Rats, 
			_ => true, 
		};
	}

	public override int CashFor(int chapter)
	{
		if (chapter != 4)
		{
			return CashNeeded;
		}
		return DavreCo.CanningFee;
	}

	public override GameItem MakeItem(string id)
	{
		if (!(id == "@canning_set"))
		{
			return base.MakeItem(id);
		}
		return Canning.MakeMachineCrate();
	}

	private static bool IsHydra(GameItem item)
	{
		try
		{
			return item?.identifier == "portable_water_purifier";
		}
		catch
		{
			return false;
		}
	}

	public override bool IsBuyChapter(int chapter)
	{
		return chapter == 2;
	}

	public override int BuyGoodsHeld()
	{
		return Character.CountHeld(IsHydra);
	}

	public override bool Accepts(GameItem item)
	{
		return IsHydra(item);
	}

	public override string[] WantIds(bool daily)
	{
		if (!daily)
		{
			return new string[1] { "portable_water_purifier" };
		}
		return Array.Empty<string>();
	}

	public override double PriceMultiplier(GameItem item, string kind)
	{
		return 1.0;
	}

	public override long? FixedPrice(GameItem item, string kind)
	{
		if (!IsHydra(item))
		{
			return null;
		}
		return 400L;
	}

	public override Visit Chapter(int chapter)
	{
		switch (chapter)
		{
		case 1:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.K("characters.doreen.chapter.doreenISetsDownCan", "多琳。<i>（放下一罐“泥炭”）</i>", "Doreen. <i>(sets down a can of Peat)</i>"),
				L.K("characters.doreen.chapter.iMDavreFoodS", "我是达夫雷食品的下层区供应专员。达夫雷食品，便宜管饱。", "I'm Davre Food's supply rep for the Lower Levels. Davre Food—cheap and filling."),
				Character.Act(L.K("characters.doreen.chapter.glancesWaterJug", "看了看你的水桶", "glances at your water jug"), L.K("characters.doreen.chapter.usingHydraTechPurifierISee", "在用海德拉净水器啊。用旧了别扔，找我回收。", "Using a HydraTech purifier, I see. Don't throw it away when it wears out—I'll buy it back."))
			};
			visit.Exit = Character.Act(L.K("characters.doreen.chapter.pressesBusinessCardOntoCounter", "把名片压在柜台上", "presses a business card onto the counter"), Bye);
			visit.Discount = 30;
			visit.Stock = new string[6] { "processed_meat", "processed_meat", "processed_meat", "cup_noodle", "cup_noodle", "cup_noodle" };
			return visit;
		}
		case 2:
		{
			Visit visit = new Visit();
			visit.Lines = new string[2]
			{
				L.K("characters.doreen.chapter.weReBuyingHydraTechPurifiers", "我们在大批量收海德拉净水器，新旧都要。", "We're buying HydraTech purifiers in bulk. New or used."),
				Character.Act(L.K("characters.doreen.chapter.pauses", "顿了顿", "pauses"), L.K("characters.doreen.chapter.rDWantsTakeThem", "研发部想拆开看看。", "R&D wants to take them apart."), showAction: false)
			};
			visit.Exit = L.K("characters.doreen.chapter.remember", "记住，", "Remember: ") + Bye;
			visit.Buy = true;
			visit.Wrong = L.K("characters.doreen.chapter.hydraTechPurifiersOnly", "只收海德拉的净水器。", "HydraTech purifiers only.");
			return visit;
		}
		case 4:
			return CanningDeal();
		default:
		{
			Visit visit = new Visit();
			visit.Lines = new string[5]
			{
				Character.Act(L.K("characters.doreen.chapter.setsDownPurifier", "放下一台净水器", "sets down a purifier"), L.K("characters.doreen.chapter.heardStartedSellingWater", "听说你开始卖水了？", "Heard you started selling water?")),
				Character.Act(L.K("characters.doreen.chapter.patsPurifier", "拍拍净水器", "pats the purifier"), L.K("characters.doreen.chapter.thisLeviathanDavreProduct", "这东西叫利维坦。达夫雷的产品。", "This is the Leviathan. A Davre product."), showAction: false),
				L.K("characters.doreen.chapter.mightNotFilterQuiteAs", "渗透度可能比海德拉差点，但铁锈水、幽灵水都能过滤。", "It might not filter quite as well as a HydraTech, but it handles Rustwater and Ghostwater."),
				L.K("characters.doreen.chapter.lastsLongerThanHydraTech", "而且比海德拉耐用。", "And it lasts longer than a HydraTech."),
				L.K("characters.doreen.chapter.onePerWeekJustCall", "每周限订一台，打电话就行。", "One per week. Just call to order.")
			};
			visit.Exit = Character.Act(L.K("characters.doreen.chapter.quietly", "小声", "quietly"), L.K("characters.doreen.chapter.howMuchWaterThereDoesn", "其实水多水少并不重要，谁掌握净化工艺才重要。", "How much water there is doesn't really matter. What matters is who controls the purification process."));
			visit.TalkOnly = true;
			visit.Gift = new string[1] { "@leviathan" };
			return visit;
		}
		}
	}

	private static Visit CanningDeal()
	{
		Visit v = new Visit
		{
			Lines = new string[4]
			{
				L.K("characters.doreen.canningDeal.davreExpandingProduction", "达夫雷正在扩产。", "Davre is expanding production."),
				Character.Act(L.K("characters.doreen.canningDeal.lowersHerVoice", "压低声音", "lowers her voice"), L.K("characters.doreen.canningDeal.pastelLookingMoveIntoFood", "帕斯泰尔要往食品这块伸手，总部决定先占住市场。", "Pastel is looking to move into food, so HQ has decided to secure the market first.")),
				L.K("characters.doreen.canningDeal.theyVeApprovedBatchEquipment", "他们批了一批设备，专门下发给你这种小店。", "They've approved a batch of equipment specifically for small shops like yours."),
				L.K("characters.doreen.canningDeal.oneGrinderOneCannerPay", $"一台绞肉机，一台罐装机。手续费{DavreCo.CanningFee}₵，机器就归你。原料得你自己找。", (System.FormattableString)$"One grinder, one canner. Pay the {DavreCo.CanningFee}₵ handling fee and they're yours. You'll have to find your own raw materials.")
			},
			Exit = L.K("characters.doreen.canningDeal.goodbye", "再见。", "Goodbye. ") + Bye,
			TalkOnly = true,
			Ask = L.K("characters.doreen.canningDeal.orOut", "做不做？", "In or out?")
		};
		v.Choices = new Choice[2]
		{
			new Choice(L.K("characters.doreen.canningDeal.iM", "我做。", "I'm in."), L.K("characters.doreen.canningDeal.ifMachineBreaksOrNeed", "以后机器坏了或者要添新的，直接打电话。", "If a machine breaks or you need another one, just call."), () =>
			{
				if (DavreCo.PayCanningFee())
				{
					v.Gift = new string[1] { "@canning_set" };
				}
				else
				{
					ModLog.Warn("多琳第 4 章：现金不够付手续费，改到面板补办");
				}
			}),
			new Choice(L.K("characters.doreen.canningDeal.letMeThink", "再想想。", "Let me think."), L.K("characters.doreen.canningDeal.slotsWonTWaitCall", "名额可不等人。想好了就打电话，手续费不变。", "The slots won't wait. Call when you've made up your mind. The fee stays the same."), null)
		};
		return v;
	}

	public override Visit Daily(CharacterState s)
	{
		string[] array = (s.SideDone.Contains(3) ? Talk.Append(L.K("characters.doreen.daily.leviathanWornOutDonT", "利维坦用完了？别担心，这是量产品。", "Leviathan worn out? Don't worry, it's mass-produced.")).ToArray() : Talk);
		if (DavreCo.CanningOpen)
		{
			array = array.Append(L.K("characters.doreen.daily.howAreCansSelling", "罐头卖的怎么样？", "How are the cans selling?")).ToArray();
		}
		Visit visit = new Visit();
		visit.Lines = new string[2]
		{
			Engine.Any(L.K("characters.doreen.daily.davreRoutineSupply", "达夫雷，例行供货。", "Davre. Routine supply."), L.K("characters.doreen.daily.doreenDeliveringPerOrder", "多琳。照单送货。", "Doreen. Delivering per the order."), Character.Act(L.K("characters.doreen.daily.glancesWaterTank", "瞧了一眼水箱", "glances at the water tank"), L.K("characters.doreen.daily.needPurificationService", "需要净水服务吗？", "Need purification service?"))),
			Engine.Any(array)
		};
		visit.Exit = Engine.Any(Bye, Character.Act(L.K("characters.doreen.daily.checksList", "看了看清单", "checks the list"), L.K("characters.doreen.daily.nextStop", "下一家。", "Next stop.")));
		visit.Discount = 20;
		visit.Stock = Engine.Pick(new string[2] { "processed_meat", "cup_noodle" }, 4, 6);
		visit.Extra = new string[2] { "processed_meat", "cup_noodle" };
		return visit;
	}
}
