using System;
using System.Linq;

namespace CorpPresence;

internal sealed class Milo : Character
{
	private const string Hemingway = "packet_red_cigarette";

	private const string GenuineHemingway = "@hemingway_genuine";

	private static readonly string[] Issues = new string[5] { "CIGARETTE_SEAL_ISSUE", "CIGARETTE_LOGO_ISSUE", "CIGARETTE_LETTERING_ISSUE", "CIGARETTE_BOX_ISSUE", "CIGARETTE_COLOR_ISSUE" };

	public override string Id => "milo";

	public override string Name => L.K("characters.milo.name.milo", "米洛", "Milo");

	public override string Faction => "FACTION_LOWER_LEVEL";

	public override int[] Thresholds => new int[8] { 20, 40, 60, 80, 100, 60, 60, 60 };

	public override int CashNeeded => 500;

	public override string PortraitRef => "maleLower_0";

	public override int BuyGoodsNeeded => 1;

	private static string WrongItem => L.K("characters.milo.wrongItem.iOnlyTakeFakeCigarettes", "……我只收假烟。", "...I only take fake cigarettes.");

	private static string[] Talk => new string[5]
	{
		L.K("characters.milo.talk.nutrifruitDoesnTProduceSeeds", "营养果不产籽，这样吃完了还得找我们买。", "Nutrifruit doesn't produce seeds. That way, once you've eaten it, you have to buy more from us."),
		L.K("characters.milo.talk.galaxyBlendHealthiestSJust", "繁星果糊是最健康的，就是没人爱吃。", "Galaxy Blend is the healthiest. It's just that nobody likes it."),
		L.K("characters.milo.talk.washHairBodyTeethLaundry", "四合一，洗头沐浴刷牙洗衣都用它。", "4 In 1 Wash. Hair, body, teeth, laundry. Does it all."),
		L.K("characters.milo.talk.pAXDoesnTSellToo", "帕克斯在下层卖不太动。太贵了。", "PAX doesn't sell too well in the Lower Levels. Too expensive."),
		L.K("characters.milo.talk.kottonFiberWeMakeCan", "我们公司产的韧棉纤维甚至能挡住小口径手枪弹。", "The Kotton fiber we make can even stop small-caliber pistol rounds.")
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
			visit.Lines = new string[3]
			{
				L.K("characters.milo.chapter.hiIMMiloPastel", "你好！我是米洛，帕斯泰尔公司的下层区推销员。", "Hi! I'm Milo, pastel Inc.'s sales rep for the Lower Levels."),
				L.K("characters.milo.chapter.pastelSuppliesNearlyAllEveryday", "帕斯泰尔几乎包揽了整个下层区的民生用品。", "pastel supplies nearly all the everyday goods in the Lower Levels."),
				L.K("characters.milo.chapter.uhExceptDavreSShare", "呃，达夫雷那片不算，哈哈。", "Uh, except Davre's share. Ha ha.")
			};
			visit.Exit = Character.Act(L.K("characters.milo.chapter.waves", "挥了挥手", "waves"), L.K("characters.milo.chapter.seeNextWeek", "下周我再来！", "See you next week!"));
			visit.Discount = 50;
			visit.Stock = new string[10] { "nutrifruit", "nutrifruit", "nutrifruit", "galaxy_blend", "galaxy_blend", "galaxy_blend", "shampoo", "shampoo", "toothpaste", "toothpaste" };
			return visit;
		}
		case 2:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.K("characters.milo.chapter.longTimeNoSeeS", "好久不见！又是我，米洛！", "Long time no see! It's me again, Milo!"),
				L.K("characters.milo.chapter.pastelSAnnouncedNewSubsidy", "帕斯泰尔公布了新的补贴政策。这几件送你了。", "pastel's announced a new subsidy policy. These are yours, free of charge."),
				Character.Act(L.K("characters.milo.chapter.patsBox", "拍了拍箱子", "pats the box"), L.K("characters.milo.chapter.higherUpsKnowFullWell", "上面清楚得很，日子舒坦了，我们这些“刁民”才不会闹事。", "The higher-ups know full well: keep us “troublemakers” comfortable, and we won't cause trouble."))
			};
			visit.Exit = Character.Act(L.K("characters.milo.chapter.lowersHisVoice", "压低声音", "lowers his voice"), L.K("characters.milo.chapter.donTTellAnyoneI", "别跟别人讲我这么说过。", "Don't tell anyone I said that."));
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
				L.K("characters.milo.chapter.heyIGotTransferredHemingway", "嘿！我被调去海明威分部了。", "Hey! I got transferred to the Hemingway division."),
				Character.Act(L.K("characters.milo.chapter.pullsOutColorSwatch", "掏出一张色卡", "pulls out a color swatch"), Settings.VanillaBuyPrices ? L.K("characters.milo.chapter.iMChargeMarketSecurity", "负责市场安全。有假烟就拿出来，我加价收。", "I'm in charge of market security. Got fake cigarettes? Bring them out. I'll pay extra.") : L.K("characters.milo.chapter.iMChargeMarketSecurity", "负责市场安全。有假烟就拿出来，我加价收。", "I'm in charge of market security. Got fake cigarettes? Bring them out. I'll pay extra.")),
				L.K("characters.milo.chapter.iBroughtRealStuffToo", "正品我也带了，想来点吗？给你打八折。", "I brought the real stuff too. Want some? Twenty percent off for you.")
			};
			visit.Exit = Character.Act(L.K("characters.milo.chapter.pocketsSwatch", "把色卡收回口袋", "pockets the swatch"), L.K("characters.milo.chapter.honestlyIKnowThereAre", "其实我知道，现在市场上的假烟比真的还多。", "Honestly, I know there are more fake cigarettes on the market than real ones these days."));
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
				L.K("characters.milo.chapter.thanksBusinessLowerLevelsHas", "托你的福，近期的下层区业务开展得不错，我升职了！", "Thanks to you, business in the Lower Levels has been going well lately. I got promoted!"),
				Character.Act(L.K("characters.milo.chapter.setsDownTwoPacks", "放下两包烟", "sets down two packs"), L.K("characters.milo.chapter.genuineNoChargeMyTreat", "正品，不要钱，算我请你的。", "Genuine. No charge. My treat."))
			};
			visit.Exit = Character.Act(L.K("characters.milo.chapter.glancesHisNewBadge", "看了看自己的新工牌", "glances at his new badge"), L.K("characters.milo.chapter.seeBoss", "下次见，店长。", "See you, boss."));
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
			visit.Lines = new string[4]
			{
				L.K("characters.milo.chapter.heyPalSMeMilo", "嘿伙计，是我，米洛。", "Hey, pal. It's me, Milo."),
				Character.Act(L.K("characters.milo.chapter.glancesBehindHim", "回头看了看", "glances behind him"), L.K("characters.milo.chapter.thisLabelStampDivisionUses", "这是分部印限量包装的标签戳，我顺了一个出来。", "This is a label stamp the division uses for Limited Edition packaging. I slipped one out.")),
				L.K("characters.milo.chapter.oneStampDoesEightPacks", "一个标签戳能印八包。四百块——外面买不到的。", "One stamp does eight packs. Four hundred. You can't get these outside."),
				L.K("characters.milo.chapter.callMeAnytimeIfWant", "以后要的话随时打电话找我。内部渠道，一期最多两个。", "Call me anytime if you want more. I've got an inside source. Two per period, max.")
			};
			visit.Exit = Character.Act(L.K("characters.milo.chapter.tugsHisCollar", "拉了拉衣领", "tugs at his collar"), L.K("characters.milo.chapter.iNeverSoldAnything", "我什么都没卖给你。", "I never sold you anything."));
			visit.Stock = new string[1] { "@hemingway_stamp" };
			return visit;
		}
		default:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.K("characters.milo.chapter.hiISetsDownBox", "嗨！<i>（放下一个盒子）</i>", "Hi! <i>(sets down a box)</i>"),
				L.K("characters.milo.chapter.ohTobaccoOurMainBusiness", "哦，烟草当然是我们的主营业务。", "Oh, tobacco is our main business, of course."),
				L.K("characters.milo.chapter.butWeActuallySellShots", "但其实我们也在卖戒瘾针<i>（耸肩）</i>。", "But we actually sell the shots that help you quit, too. <i>(shrugs)</i>")
			};
			visit.Exit = Character.Act(L.K("characters.milo.chapter.smiles", "笑了笑", "smiles"), L.K("characters.milo.chapter.pastelWeVeGotAll", "帕斯泰尔，应有尽有。", "pastel. We've got it all."));
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
			Lines = new string[5]
			{
				L.K("characters.milo.scrapMachines.hiBoss", "嗨，店长！", "Hi, boss!"),
				L.K("characters.milo.scrapMachines.divisionClearingOutSomeOld", "分部要处理一批旧机器，说是型号淘汰了。", "The division is clearing out some old machines. Says the model's been retired."),
				Character.Act(L.K("characters.milo.scrapMachines.lowersHisVoice", "压低声音", "lowers his voice"), L.K("characters.milo.scrapMachines.theyStillWorkThough", "不过其实还能用。", "They still work, though.")),
				L.K("characters.milo.scrapMachines.soBeforeTheyGoScrapyard", "所以在拉去回收站之前，我想先问问你。", "So before they go to the scrapyard, I wanted to ask you."),
				L.K("characters.milo.scrapMachines.withThisCigaretteRollerCan", "有了这台卷烟机，你就能自己做散烟。<i>（小声）</i>我会给你提供种子。", "With this cigarette roller, you can make your own loose cigarettes. <i>(quietly)</i> I'll get you the seeds.")
			},
			Exit = Character.Act(L.K("characters.milo.scrapMachines.waves", "挥了挥手", "waves"), L.K("characters.milo.scrapMachines.offIGo", "走了！", "Off I go!")),
			TalkOnly = true,
			Ask = L.K("characters.milo.scrapMachines.want", "要不要？", "Want it?")
		};
		v.Choices = new Choice[2]
		{
			new Choice(L.K("characters.milo.scrapMachines.iLlTake", "我要了。", "I'll take it."), L.K("characters.milo.scrapMachines.allRightILlBring", "好，这就给你搬进来，种子也给你一包。别说是公司的。", "All right, I'll bring it in. A pack of seeds for you, too. Don't say it came from the company."), () =>
			{
				v.Gift = new string[1] { "@craft_roller_set" };
			}),
			new Choice(L.K("characters.milo.scrapMachines.noThanks", "不用了。", "No thanks."), L.K("characters.milo.scrapMachines.thatSShameGiveMe", "真可惜。哪天要是改主意了，打我电话。", "That's a shame. Give me a call if you ever change your mind."), null)
		};
		return v;
	}

	private static Visit Caught()
	{
		Visit v = new Visit
		{
			Lines = new string[5]
			{
				L.K("characters.milo.caught.sMeMilo", "是我，米洛。", "It's me, Milo."),
				Character.Act(L.K("characters.milo.caught.quietly", "小声", "quietly"), L.K("characters.milo.caught.bossIMAfraidOur", "店长，我们的香烟生意怕是要黄了。", "Boss, I'm afraid our cigarette business might be finished.")),
				L.K("characters.milo.caught.higherUpsHaveTracedBack", "上面追查到我这儿了。", "The higher-ups have traced it back to me."),
				Character.Act(L.K("characters.milo.caught.rubsHisHands", "搓了搓手", "rubs his hands"), L.K("characters.milo.caught.butIKnowSupervisorThis", $"不过，我认识这片的主管。{PastelCo.Bribe}块，这事就当没发生。", (System.FormattableString)$"But I know the supervisor for this area. {PastelCo.Bribe} credits, and it's like none of this ever happened.")),
				L.K("characters.milo.caught.thereSConditionThoughCigarettes", "不过有条件，之后你的烟也得装进海明威的盒子里。", "There's a condition, though. Your cigarettes will have to go in Hemingway boxes from now on, too.")
			},
			Exit = Character.Act(L.K("characters.milo.caught.checksHisWatch", "看了看表", "checks his watch"), L.K("characters.milo.caught.threeMoreShopsHit", "还有三家要跑。", "Three more shops to hit.")),
			TalkOnly = true,
			Ask = L.K("characters.milo.caught.whatDoSay", "怎么说？", "What do you say?")
		};
		v.Choices = new Choice[2]
		{
			new Choice(L.K("characters.milo.caught.here", $"给你，{PastelCo.Bribe}。", (System.FormattableString)$"Here, {PastelCo.Bribe}."), L.K("characters.milo.caught.allRightIVeBrought", "行。分装机我给你带来了，从今往后，你做的就是正品。", "All right. I've brought you a packaging machine. From now on, what you make is genuine."), () =>
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
			new Choice(L.K("characters.milo.caught.letMeThink", "我再想想。", "Let me think."), L.K("characters.milo.caught.donTTakeTooLong", "别拖太久。想好了给我打电话。", "Don't take too long. Give me a call when you've decided."), null)
		};
		return v;
	}

	public override Visit Daily(CharacterState s)
	{
		string[] array = ((s.Chapter >= 3) ? Append(Talk, L.K("characters.milo.daily.yesYesThereAreMore", "是，是，海明威的假烟比真的还多。", "Yes, yes, there are more fake Hemingways than real ones.")) : Talk);
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
				Engine.Any(L.K("characters.milo.daily.meAgain", "又是我！", "Me again!"), L.K("characters.milo.daily.miloSHere", "米洛来了。", "Milo's here."), L.K("characters.milo.daily.gotAnyFakeCigarettes", "有假烟嘛？", "Got any fake cigarettes?"), L.K("characters.milo.daily.hemingwayDivisionRoutineCollection", "海明威分部，例行回收。", "Hemingway division, routine collection.")),
				Engine.Any(array)
			};
			visit.Exit = Engine.Any(Character.Act(L.K("characters.milo.daily.waves", "挥了挥手", "waves"), L.K("characters.milo.daily.offIGo", "走了！", "Off I go!")), Character.Act(L.K("characters.milo.daily.checksHisWatch", "看了看表", "checks his watch"), L.K("characters.milo.daily.threeMoreShopsHit", "还有三家要跑。", "Three more shops to hit.")));
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
			Engine.Any(L.K("characters.milo.daily.meAgain", "又是我！", "Me again!"), L.K("characters.milo.daily.miloSHere", "米洛来了。", "Milo's here."), L.K("characters.milo.daily.stockingUpToday", "今天进货吗？", "Stocking up today?")),
			Engine.Any(array)
		};
		visit.Exit = Engine.Any(Character.Act(L.K("characters.milo.daily.waves", "挥了挥手", "waves"), L.K("characters.milo.daily.offIGo", "走了！", "Off I go!")), Character.Act(L.K("characters.milo.daily.checksHisWatch", "看了看表", "checks his watch"), L.K("characters.milo.daily.threeMoreShopsHit", "还有三家要跑。", "Three more shops to hit.")));
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
