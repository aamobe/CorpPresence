using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal sealed class Rosen : Character
{
	private const string Sentinel = "@glock_receiver";

	private const string Peacemaker = "@smg";

	private const string Bulwark = "@shotgun";

	private const string Ammo = "heavy_pistol_ammo";

	private const string AmmoAP = "heavy_pistol_ammo_p";

	private const string AmmoNL = "heavy_pistol_ammo_nl";

	public const string Revolver = "@rosen_revolver";

	public const string Rifle = "@rosen_rifle";

	public override string Id => "rosen";

	public override string Name => L.T("罗森", "Rosen");

	public override string Faction => "FACTION_SECURITY";

	public override int[] Thresholds => new int[5] { 20, 40, 60, 80, 80 };

	public override int CashNeeded => 1000;

	public override string PortraitRef => "maleSecOff_0";

	public override string[] RefuseIds => new string[3] { "heavy_pistol_ammo", "heavy_pistol_ammo_p", "heavy_pistol_ammo_nl" };

	public override (int Min, int Max) DefaultDailyGap => (Min: 5, Max: 5);

	public override int BuyGoodsNeeded => 1;

	private static string Bye => L.T("保重", "Take care.");

	private static int Disc
	{
		get
		{
			if (!Settings.VanillaServices)
			{
				return 30;
			}
			return 20;
		}
	}

	private static string[] Talk => new string[6]
	{
		L.T("哨兵3型不会卡壳，至少宣传上是这样。", "The Sentinel doesn't jam. At least, that's what the ads say."),
		L.T("非致命弹，就数治安部用得最多。报告上写着好看。", "Non-lethal rounds are what Security uses most. They look good in reports."),
		L.T("穿甲弹贵一点，毕竟明面上不让卖。", "AP rounds cost more. They're not supposed to be sold openly, after all."),
		Character.Act(L.T("敲了敲右臂", "taps his right arm"), L.T("每年都得回厂保养一次。", "Back to the factory once a year for maintenance.")),
		L.T("这道疤？演习。谁想到演习也用实弹。", "This scar? A drill. Who knew drills used live rounds."),
		L.T("子午线可从来不卖给黑市，我们的活计只是损耗大一点。", "Meridian never sells to the Black Market. Our stock just has a higher loss rate.")
	};

	public override bool IsSide(int chapter)
	{
		return chapter == 5;
	}

	public override bool SideAllowed(int chapter)
	{
		if (chapter == 5)
		{
			return MeridianCo.Coop() >= 60;
		}
		return true;
	}

	public override int CashFor(int chapter)
	{
		if (chapter != 5)
		{
			return CashNeeded;
		}
		return MeridianCo.ForgeFee;
	}

	public override string Portrait(CharacterState s, string kind)
	{
		return "meridian_rosen";
	}

	public override bool DailyBuyer(CharacterState s)
	{
		return true;
	}

	public override bool DailyAccepts(GameItem item)
	{
		return Character.HasTag(item, "MATERIAL");
	}

	public override string[] WantTags(bool daily)
	{
		if (!daily)
		{
			return Array.Empty<string>();
		}
		return new string[1] { "MATERIAL" };
	}

	public override GameItem MakeItem(string id)
	{
		bool flag;
		switch (id)
		{
		case "@glock_receiver":
		case "@smg":
		case "@shotgun":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return MeridianCo.MakeNew(id);
		}
		if (id == "@forge_set")
		{
			return Crates.MakeFilled("crate_meridian", "forge_furnace", "forge_anvil", "forged_billet", "forged_billet", "forged_billet");
		}
		return base.MakeItem(id);
	}

	public override bool IsBuyChapter(int chapter)
	{
		return false;
	}

	public override int BuyGoodsHeld()
	{
		return 0;
	}

	public override bool Accepts(GameItem item)
	{
		return false;
	}

	public override double PriceMultiplier(GameItem item, string kind)
	{
		return 1.0;
	}

	public override Visit Chapter(int chapter)
	{
		switch (chapter)
		{
		case 1:
		{
			Visit visit4 = new Visit();
			visit4.Lines = new string[3]
			{
				L.T("子午线兵工，罗森。", "Meridian Ballistics. Rosen."),
				L.T("子午线致力于生产标配军工（官方腔调）。", "Meridian is dedicated to standard-issue arms. (in an official tone)"),
				Character.Act(L.T("把一把哨兵3型放在柜台上", "sets a Sentinel Mk. III on the counter"), "")
			};
			visit4.Exit = Bye;
			visit4.Discount = Disc;
			visit4.Stock = new string[3] { "@glock_receiver", "heavy_pistol_ammo", "heavy_pistol_ammo" };
			return visit4;
		}
		case 2:
		{
			Visit visit3 = new Visit();
			visit3.Lines = new string[3]
			{
				L.T("和平使者，标准军备。", "The Peacemaker. Standard armament."),
				Character.Act(L.T("指了指左脸那道疤", "points at the scar on his left cheek"), L.T("我身上绝大部分疤痕都是它留下的。", "Most of the scars on me came from one of these.")),
				L.T("所以我知道它好用。", "So I know it works.")
			};
			visit3.Exit = Bye;
			visit3.Discount = Disc;
			visit3.Stock = new string[3] { "@smg", "heavy_pistol_ammo", "heavy_pistol_ammo" };
			return visit3;
		}
		case 3:
		{
			Visit visit2 = new Visit();
			visit2.Lines = new string[3]
			{
				L.T("“壁垒”，双动式霰弹枪，嘭！就结束了。", "The Rampart. Dual-action shotgun. Boom! And it's over."),
				L.T("产线一天出几百把，每一把都一样（撇嘴）。", "The line turns out hundreds a day, every one the same. (curls his lip)"),
				Character.Act(L.T("拍了拍枪托", "pats the stock"), L.T("当然这使维修很方便我承认。", "It does make repairs easy, I'll admit."))
			};
			visit2.Exit = Bye;
			visit2.Discount = Disc;
			visit2.Stock = new string[1] { "@shotgun" };
			return visit2;
		}
		case 5:
			return ForgeLesson();
		default:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.T("公司的枪当然都有编号，这两把显然没有。", "Company guns all have serial numbers, of course. These two clearly don't."),
				Character.Act(L.T("打开一个旧木盒", "opens an old wooden case"), L.T("左轮，步枪。我自己做的。", "A revolver. A rifle. I made them myself.")),
				L.T("治安部用不上它们。相信你会找到用得上的人。", "Security has no use for them. I trust you'll find someone who does.")
			};
			visit.Exit = Character.Act(L.T("合上木盒", "closes the case"), L.T("保养好它们。", "Take care of them."));
			visit.Discount = Disc;
			visit.Stock = new string[2] { "@rosen_revolver", "@rosen_rifle" };
			return visit;
		}
		}
	}

	private static Visit ForgeLesson()
	{
		Visit v = new Visit
		{
			Lines = new string[4]
			{
				L.T("店长，对锻造有兴趣吗？", "Interested in forging, boss?"),
				Character.Act(L.T("把一块钢胚放在柜台上", "sets a billet on the counter"), L.T("高纯度的金属锭，在炉子里烧一夜，就是这个。", "A high-purity metal ingot, a night in the forge, and you get this.")),
				L.T("我最近买了一套。。。呃不是，我最近希望传播这门手艺。", "I recently bought a set... er, no. I've been wanting to pass on the craft."),
				L.T($"我手里的整套家伙 {MeridianCo.ForgeFee}。钢胚还送你三块，练手用足够了。", $"The whole kit I've got is {MeridianCo.ForgeFee}. I'll throw in three billets, plenty for practice.")
			},
			Exit = Bye,
			TalkOnly = true,
			Ask = L.T("学不学？", "Well?")
		};
		v.Choices = new Choice[2]
		{
			new Choice(L.T("我学。", "Teach me."), L.T("炉子坏了，找我订。", "If the forge breaks, order from me."), () =>
			{
				if (MeridianCo.PayForgeFee())
				{
					v.Gift = new string[1] { "@forge_set" };
				}
				else
				{
					ModLog.Warn("罗森第 5 章：现金不够，改到面板补办");
				}
			}),
			new Choice(L.T("改天。", "Another time."), L.T("想好了打电话。", "Call when you're ready."), null)
		};
		return v;
	}

	public override Visit Daily(CharacterState s)
	{
		string[] talk = Talk;
		List<string> list = new List<string>();
		list.Add(Engine.Any(L.T("子午线。例行补货。", "Meridian. Routine restock."), L.T("罗森。照旧。", "Rosen. As usual."), L.T("补货。几分钟。", "Restock. A few minutes.")));
		list.Add(Engine.Any(talk));
		List<string> list2 = list;
		List<string> list3 = Engine.Pick(new string[4] { "heavy_pistol_ammo", "heavy_pistol_ammo", "heavy_pistol_ammo_p", "heavy_pistol_ammo_nl" }, 3, 5).ToList();
		list3.Add((s.Chapter >= 3) ? Engine.Any("@glock_receiver", "@smg", "@shotgun") : Engine.Any("@glock_receiver", "@smg"));
		Visit visit = new Visit
		{
			Exit = Bye,
			Discount = Disc,
			Both = true,
			Wrong = L.T("只收材料。", "Materials only.")
		};
		if (s.Order != null)
		{
			list3.Add(s.Order);
			list2.Add(Character.Act(L.T("放下一个木盒", "sets down a wooden case"), L.T("这是你订的“手工品”。", "Your \"handmade\" order.")));
		}
		if (s.Chapter >= 4)
		{
			visit.Ask = L.T("要订一把吗？", "Want to order one?");
			visit.Choices = new Choice[3]
			{
				new Choice(L.T("左轮", "Revolver"), L.T("七天，手工制作，快不了。", "Seven days. Handmade. Can't rush it."), () =>
				{
					s.Order = "@rosen_revolver";
				}),
				new Choice(L.T("步枪", "Rifle"), L.T("七天，手工制作，快不了。", "Seven days. Handmade. Can't rush it."), () =>
				{
					s.Order = "@rosen_rifle";
				}),
				new Choice(L.T("不用", "No thanks"), L.T("好。", "Fine."), () =>
				{
					s.Order = null;
				})
			};
		}
		visit.Lines = list2.ToArray();
		visit.Stock = list3.ToArray();
		visit.Extra = new string[4] { "heavy_pistol_ammo", "heavy_pistol_ammo", "heavy_pistol_ammo_p", "heavy_pistol_ammo_nl" };
		return visit;
	}
}
