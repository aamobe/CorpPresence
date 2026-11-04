using System;
using System.Linq;

namespace CorpPresence;

internal sealed class Milo : Character
{
	private const string Hemingway = "packet_red_cigarette";

	private const string GenuineHemingway = "@hemingway_genuine";

	private static readonly string[] Issues = new string[5] { "CIGARETTE_SEAL_ISSUE", "CIGARETTE_LOGO_ISSUE", "CIGARETTE_LETTERING_ISSUE", "CIGARETTE_BOX_ISSUE", "CIGARETTE_COLOR_ISSUE" };

	public override string Id => "milo";

	public override string Name => L.T("米洛", "Milo");

	public override string Faction => "FACTION_LOWER_LEVEL";

	public override int[] Thresholds => new int[8] { 20, 40, 60, 80, 100, 60, 60, 60 };

	public override int CashNeeded => 500;

	public override string PortraitRef => "maleLower_0";

	public override int BuyGoodsNeeded => 1;

	private static string WrongItem => L.T("我只收假海明威。", "I only take fake Hemingways.");

	private static string[] Talk => new string[5]
	{
		L.T("营养果没籽。吃完了，还得找我们买。", "Nutrifruit has no seeds. Finish one, and you buy another from us."),
		L.T("繁星果糊，最健康的。就是没人爱吃。", "Galaxy Blend is the healthiest. Nobody likes it, though."),
		L.T("四合一，洗头刷牙都用它。", "4 In 1 Wash. Hair, teeth, all of it."),
		L.T("帕克斯在下层卖不动。太贵了。", "PAX doesn't sell in the Lower Levels. Too expensive."),
		L.T("我们公司生产的韧棉纤维，小口径手枪完全挡得住。", "The Kotton fiber our company makes stops small-caliber pistol rounds cold.")
	};

	public override bool IsSide(int chapter)
	{
		return chapter >= 6;
	}

	public override int SideAfter(int chapter)
	{
		if (chapter < 6)
		{
			return base.SideAfter(chapter);
		}
		return 3;
	}

	public override bool SideAllowed(int chapter)
	{
		return chapter switch
		{
			6 => Wilde.Hydroponics, 
			7 => PastelCo.CraftDone(1) && (CorpState.Get()?.CompanyOf("pastel").LooseSold ?? 0) >= 10, 
			8 => PastelCo.Bribed && (CorpState.Get()?.CompanyOf("pastel").PacksMade ?? 0) >= 6, 
			_ => true, 
		};
	}

	public override int CashFor(int chapter)
	{
		if (chapter != 7)
		{
			return CashNeeded;
		}
		return PastelCo.Bribe;
	}

	public override bool DailyBuyer(CharacterState s)
	{
		return s.Chapter >= 3;
	}

	public override string Portrait(CharacterState s, string kind)
	{
		if ((kind.StartsWith("ch") ? int.Parse(kind.Substring(2)) : s.Chapter) < 3)
		{
			return "pastel_milo";
		}
		return "pastel_milo_hemingway";
	}

	private static bool IsHemingway(GameItem item)
	{
		try
		{
			return item?.identifier == "packet_red_cigarette";
		}
		catch
		{
			return false;
		}
	}

	private static bool IsFake(GameItem item)
	{
		string[] issues = Issues;
		foreach (string tagName in issues)
		{
			try
			{
				if (item.IsTag(tagName))
				{
					return true;
				}
			}
			catch
			{
			}
		}
		return false;
	}

	public override GameItem MakeItem(string id)
	{
		if (id == "@craft_roller_set")
		{
			return Crates.MakeFilled("crate_pastel", "rolling_machine", "tobacco_seed", "tobacco_seed");
		}
		if (id == "@craft_packer_set")
		{
			return Crates.MakeFilled("crate_pastel", "packaging_machine");
		}
		if (id != "@hemingway_genuine")
		{
			return base.MakeItem(id);
		}
		return PastelCo.GenuineHemingway();
	}

	public override bool IsBuyChapter(int chapter)
	{
		return false;
	}

	public override int BuyGoodsHeld()
	{
		return Character.CountHeld(IsFakeHemingway);
	}

	public override bool Accepts(GameItem item)
	{
		return IsFakeHemingway(item);
	}

	private static bool IsFakeHemingway(GameItem item)
	{
		if (IsHemingway(item))
		{
			return IsFake(item);
		}
		return false;
	}

	public override double PriceMultiplier(GameItem item, string kind)
	{
		if (!Settings.VanillaBuyPrices)
		{
			return 2.0;
		}
		return 1.5;
	}

	public override Visit Chapter(int chapter)
	{
		switch (chapter)
		{
		case 1:
		{
			Visit visit = new Visit();
			visit.Lines = new string[2]
			{
				L.T("你好！我是米洛，帕斯泰尔公司跑下层区的推销员。", "Hi! I'm Milo, pastel Inc.'s sales rep for the Lower Levels."),
				L.T("帕斯泰尔几乎包揽了整个下层区的民生用品，哈哈，呃当然是除了达夫雷占据之外的部分。", "pastel supplies nearly all the everyday goods in the Lower Levels. Ha. Well, except Davre's share, of course.")
			};
			visit.Exit = Character.Act(L.T("挥了挥手", "waves"), L.T("下周我再来！", "See you next week!"));
			visit.Discount = 50;
			visit.Stock = new string[10] { "nutrifruit", "nutrifruit", "nutrifruit", "galaxy_blend", "galaxy_blend", "galaxy_blend", "shampoo", "shampoo", "toothpaste", "toothpaste" };
			return visit;
		}
		case 2:
		{
			Visit visit = new Visit();
			visit.Lines = new string[2]
			{
				L.T("补贴到了，这箱不要钱。", "The subsidy came through. This box is free."),
				Character.Act(L.T("拍了拍箱子", "pats the box"), L.T("肚子饱了，我们这些“刁民”才不会闹事，上面比谁都清楚。", "Full bellies keep us \"troublemakers\" quiet. Upstairs knows that better than anyone."))
			};
			visit.Exit = Character.Act(L.T("压低声音", "lowers his voice"), L.T("别说是我说的。", "You didn't hear it from me."));
			visit.Discount = 30;
			visit.Gift = new string[4] { "pack_condom", "pack_condom", "pack_condom", "@gloves" };
			visit.Stock = new string[10] { "shampoo", "shampoo", "shampoo", "toothpaste", "toothpaste", "toothpaste", "box_tampon", "box_tampon", "kotton_fabric", "kotton_fabric" };
			return visit;
		}
		case 3:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.T("嘿！我被调去海明威分部了，现在市场安全由我负责，想来点好烟吗？", "Hey! I got transferred to the Hemingway division. Market security is my job now. Want some good smokes?"),
				Character.Act(L.T("掏出一张色卡", "pulls out a color swatch"), Settings.VanillaBuyPrices ? L.T("有假烟就拿出来，我一点五倍收。", "Got fake cigarettes? Bring them out. I pay one and a half times.") : L.T("有假烟就拿出来，我两倍收。", "Got fake cigarettes? Bring them out. I pay double.")),
				L.T("正品我也带了，给你打八折。", "I brought the real stuff too. Twenty percent off for you.")
			};
			visit.Exit = Character.Act(L.T("把色卡收回口袋", "pockets the swatch"), L.T("现在市场上的烟假的比真的还多。", "There are more fake cigarettes on the market than real ones these days."));
			visit.Both = true;
			visit.Discount = 20;
			visit.Stock = new string[8] { "@hemingway_genuine", "@hemingway_genuine", "@hemingway_genuine", "@hemingway_genuine", "box_tampon", "box_tampon", "kotton_fabric", "kotton_fabric" };
			visit.Wrong = WrongItem;
			return visit;
		}
		case 4:
		{
			Visit visit = new Visit();
			visit.Lines = new string[2]
			{
				L.T("托你的福，收上去的假烟够多，我升职了！", "Thanks to you, I turned in enough fakes to get promoted!"),
				Character.Act(L.T("放下两包烟", "sets down two packs"), L.T("正品，不要钱。", "Genuine, on the house."))
			};
			visit.Exit = Character.Act(L.T("看了看自己的新工牌", "glances at his new badge"), L.T("下次见，店长。", "See you, boss."));
			visit.TalkOnly = true;
			visit.Gift = new string[2] { "@hemingway_genuine", "@hemingway_genuine" };
			return visit;
		}
		case 6:
			return ScrapMachines();
		case 7:
			return Caught();
		case 8:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				Character.Act(L.T("回头看了看门口", "glances back at the door"), L.T("分部印限量包装的标签戳，我顺了一个出来。", "I lifted one of the division's Limited Edition label stamps.")),
				L.T("一个标签戳能印八包。外面买不到的，一口价四百。", "One label stamp does eight packs. You can't get these outside. Four hundred, flat."),
				L.T("以后要，打电话找我，内部渠道，一期两个。", "Want more later? Call me. Inside channel, two per period.")
			};
			visit.Exit = Character.Act(L.T("拉了拉衣领", "tugs at his collar"), L.T("我什么都没卖给你。", "I never sold you anything."));
			visit.Stock = new string[1] { "@hemingway_stamp" };
			return visit;
		}
		default:
		{
			Visit visit = new Visit();
			visit.Lines = new string[2]
			{
				L.T("我们的主营业务当然是烟草", "Our main business is tobacco, of course."),
				Character.Act(L.T("放下一个盒子", "sets down a box"), L.T("但其实，我们也在卖戒瘾针（耸肩）。", "But honestly, we also sell the shot that helps you quit. (shrugs)"))
			};
			visit.Exit = Character.Act(L.T("笑了笑", "smiles"), L.T("帕斯泰尔应有尽有。", "pastel has everything."));
			visit.Discount = 30;
			visit.Stock = new string[4] { "black_injector", "black_injector", "@hemingway_genuine", "@hemingway_genuine" };
			visit.Gift = new string[1] { "@sign" };
			return visit;
		}
		}
	}

	private static Visit ScrapMachines()
	{
		Visit v = new Visit
		{
			Lines = new string[3]
			{
				L.T("分部要处理一批旧机器，说是型号淘汰了。", "The division is clearing out some old machines. Says the model's been retired."),
				Character.Act(L.T("压低声音", "lowers his voice"), L.T("其实还能用。拉去回收站之前，我想先问问你。", "They still work, honestly. Before they go to the scrapyard, I wanted to ask you.")),
				L.T("你要是有一台卷烟机，自己就能做散烟（小声）我会给你提供种子。", "With a cigarette roller, you could make loose cigarettes yourself. (quietly) I'll get you the seeds.")
			},
			Exit = Character.Act(L.T("挥了挥手", "waves"), L.T("走了！", "Off I go!")),
			TalkOnly = true,
			Ask = L.T("要不要？", "Want it?")
		};
		v.Choices = new Choice[2]
		{
			new Choice(L.T("我要了。", "I'll take it."), L.T("给你搬进来了。种子我也塞了一包，别说是公司的。", "Brought it in for you. Slipped in a pack of seeds too. Don't say it came from the company."), () =>
			{
				v.Gift = new string[1] { "@craft_roller_set" };
			}),
			new Choice(L.T("不用了。", "No thanks."), L.T("行，那我拉走了。哪天想要，打电话订。", "Fine, I'll haul it off. If you change your mind, call and order one."), null)
		};
		return v;
	}

	private static Visit Caught()
	{
		Visit v = new Visit
		{
			Lines = new string[3]
			{
				L.T("店长我们的香烟生意怕是要黄了，上面追查追到我这儿了。", "Boss, I think our cigarette business is about to fall apart. Upstairs traced it all the way to me."),
				Character.Act(L.T("搓了搓手", "rubs his hands"), L.T($"不过我认识管这片的主管。{PastelCo.Bribe} 块，这事就当没发生。", $"But I know the supervisor for this sector. {PastelCo.Bribe}, and it never happened.")),
				L.T("条件是——以后你的烟，只能装进海明威的盒子里卖。", "The condition: from now on, your cigarettes only sell in Hemingway boxes.")
			},
			Exit = Character.Act(L.T("看了看表", "checks his watch"), L.T("还有三家要跑。", "Three more shops to hit.")),
			TalkOnly = true,
			Ask = L.T("给不给？", "Well?")
		};
		v.Choices = new Choice[2]
		{
			new Choice(L.T($"给你{PastelCo.Bribe}。", $"Here's {PastelCo.Bribe}."), L.T("分装机给你搬进来了。从今往后，你做的就是正品。", "Brought you a packaging machine. From now on, what you make is genuine."), () =>
			{
				if (PastelCo.PayBribe(deliver: false))
				{
					v.Gift = new string[1] { "@craft_packer_set" };
				}
				else
				{
					ModLog.Warn("米洛第 7 章：现金不够付打点费，改到面板补交");
				}
			}),
			new Choice(L.T("我再想想。", "Let me think."), L.T("别拖太久。想好了打电话。", "Don't take too long. Call when you've decided."), null)
		};
		return v;
	}

	public override Visit Daily(CharacterState s)
	{
		string[] array = ((s.Chapter >= 3) ? Append(Talk, L.T("海明威的假烟，比真的还多。", "There are more fake Hemingways than real ones.")) : Talk);
		string[] array2 = new string[6] { "nutrifruit", "galaxy_blend", "shampoo", "toothpaste", "box_tampon", "kotton_fabric" };
		if (s.Chapter >= 4)
		{
			array2 = Append(array2, "@patch");
		}
		Visit visit;
		if (s.Chapter >= 3)
		{
			visit = new Visit();
			visit.Lines = new string[2]
			{
				Engine.Any(L.T("又是我！", "Me again!"), L.T("米洛来了。", "Milo's here."), L.T("假烟留着没？", "Saved any fakes for me?"), L.T("海明威分部，例行回收。", "Hemingway division, routine collection.")),
				Engine.Any(array)
			};
			visit.Exit = Engine.Any(Character.Act(L.T("挥了挥手", "waves"), L.T("走了！", "Off I go!")), Character.Act(L.T("看了看表", "checks his watch"), L.T("还有三家要跑。", "Three more shops to hit.")));
			visit.Both = true;
			visit.Discount = 20;
			visit.Stock = new string[2] { "@hemingway_genuine", "@hemingway_genuine" }.Concat(Engine.Pick(array2, 2, 4)).ToArray();
			visit.Extra = array2;
			visit.Wrong = WrongItem;
			return visit;
		}
		visit = new Visit();
		visit.Lines = new string[2]
		{
			Engine.Any(L.T("又是我！", "Me again!"), L.T("米洛来了。", "Milo's here."), L.T("今天进货吗？", "Stocking up today?")),
			Engine.Any(array)
		};
		visit.Exit = Engine.Any(Character.Act(L.T("挥了挥手", "waves"), L.T("走了！", "Off I go!")), Character.Act(L.T("看了看表", "checks his watch"), L.T("还有三家要跑。", "Three more shops to hit.")));
		visit.Discount = 20;
		visit.Stock = Engine.Pick(array2, 4, 6);
		return visit;
	}

	private static string[] Append(string[] a, string x)
	{
		string[] array = new string[a.Length + 1];
		Array.Copy(a, array, a.Length);
		array[a.Length] = x;
		return array;
	}
}
