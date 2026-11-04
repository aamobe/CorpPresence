using System;
using System.Linq;

namespace CorpPresence;

internal sealed class Doreen : Character
{
	private const string Peat = "processed_meat";

	private const string Noodle = "cup_noodle";

	public const long HydraPrice = 400L;

	public override string Id => "doreen";

	public override string Name => L.T("多琳", "Doreen");

	public override string Faction => "FACTION_LOWER_LEVEL";

	public override int[] Thresholds => new int[4] { 30, 50, 70, 50 };

	public override int CashNeeded => 300;

	public override string PortraitRef => "femaleLower_0";

	public override int BuyGoodsNeeded => 1;

	private static string Bye => L.T("按时付款，按时到货。", "Pay on time, get it on time.");

	private static string[] Talk => new string[3]
	{
		L.T("“泥炭”是什么肉？配方保密。", "What meat is in Peat? The recipe is confidential."),
		L.T("饿了有我们达夫雷拉面，当然要加水才能吃。水？也找我们买。", "Hungry? There's Davre ramen. It needs water, of course. Water? Buy that from us too."),
		L.T("治安部发口粮的时候，我们的单子最多，工厂都要炸了。", "When Security hands out rations, our orders are the biggest. The factory's about to burst.")
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
				L.T("多琳，达夫雷食品，下层区供应专员。", "Doreen. Davre Food, Lower Levels supply rep."),
				Character.Act(L.T("放下一罐“泥炭”", "sets down a can of Peat"), L.T("便宜，管饱。", "Cheap and filling.")),
				Character.Act(L.T("看了眼你的水桶", "eyes your water jug"), L.T("海德拉净水器，用旧了别扔，我们回收。", "Don't throw out worn HydraTech purifiers. We buy them back."))
			};
			visit.Exit = Character.Act(L.T("把名片压在柜台上", "pins a card under something on the counter"), Bye);
			visit.Discount = 30;
			visit.Stock = new string[6] { "processed_meat", "processed_meat", "processed_meat", "cup_noodle", "cup_noodle", "cup_noodle" };
			return visit;
		}
		case 2:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.T("达夫雷在收海德拉的净水器。", "Davre is buying HydraTech purifiers."),
				L.T("新的旧的都要。", "New or used."),
				Character.Act(L.T("停顿", "pauses"), L.T("研发部要拆开看看。", "R&D wants to take them apart."))
			};
			visit.Exit = Bye;
			visit.Buy = true;
			visit.Wrong = L.T("只收海德拉的净水器。", "HydraTech purifiers only.");
			return visit;
		}
		case 4:
			return CanningDeal();
		default:
		{
			Visit visit = new Visit();
			visit.Lines = new string[5]
			{
				L.T("听说你开始卖水了。", "Heard you started selling water."),
				Character.Act(L.T("放下一台净水器", "sets down a purifier"), L.T("利维坦。达夫雷自己做的。", "The Leviathan. Davre makes it in-house.")),
				L.T("渗透度比海德拉差点，但是铁锈水、幽灵水都能过滤。", "It filters a bit worse than a HydraTech, but it handles Rustwater and Ghostwater."),
				L.T("而且比海德拉耐用。", "And it lasts longer than a HydraTech."),
				L.T("每周可以订一台。打电话就行。", "You can order one a week. Just call.")
			};
			visit.Exit = L.T("水多少其实不重要。净化工艺谁手里，才重要。", "How much water there is doesn't really matter. Who holds the purification process does.");
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
				L.T("达夫雷要扩产。", "Davre is expanding production."),
				Character.Act(L.T("压低声音", "lowers her voice"), L.T("帕斯泰尔在往食品这块伸手。达夫雷决定下放一些设备。", "Pastel is reaching into food. Davre has decided to hand out some equipment.")),
				L.T("总部批了一批商用机器，下发给你这种小店。绞肉机一台，罐装机一台。", "HQ approved a batch of commercial machines for small shops like yours. One grinder, one canner."),
				L.T($"手续费 {DavreCo.CanningFee}₵。机器归你，原料自己找。", $"{DavreCo.CanningFee}₵ handling fee. The machines are yours; source your own meat.")
			},
			Exit = Bye,
			TalkOnly = true,
			Ask = L.T("做不做？", "In or out?")
		};
		v.Choices = new Choice[2]
		{
			new Choice(L.T("我做。", "I'm in."), L.T("以后坏了、要添，打电话。", "If one breaks or you want more, call."), () =>
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
			new Choice(L.T("再想想。", "Let me think."), L.T("名额不等人。想好了打电话，手续费照旧。", "The slots won't wait. Call when you've decided. Same fee."), null)
		};
		return v;
	}

	public override Visit Daily(CharacterState s)
	{
		string[] array = (s.SideDone.Contains(3) ? Talk.Append(L.T("利维坦用完了？别担心这是量产品。", "Leviathan worn out? Don't worry, it's mass-produced.")).ToArray() : Talk);
		if (DavreCo.CanningOpen)
		{
			array = array.Append(L.T("罐头卖得怎么样？", "How are the cans selling?")).ToArray();
		}
		Visit visit = new Visit();
		visit.Lines = new string[2]
		{
			Engine.Any(L.T("达夫雷，例行供货。", "Davre. Routine supply."), L.T("多琳。照单送货。", "Doreen. Delivering per the order."), L.T("（瞧了一眼水箱）需要净水服务吗？", "(glances at the water tank) Need purification service?")),
			Engine.Any(array)
		};
		visit.Exit = Engine.Any(Bye, Character.Act(L.T("看了看清单", "checks the list"), L.T("下一家。", "Next stop.")));
		visit.Discount = 20;
		visit.Stock = Engine.Pick(new string[2] { "processed_meat", "cup_noodle" }, 4, 6);
		visit.Extra = new string[2] { "processed_meat", "cup_noodle" };
		return visit;
	}
}
