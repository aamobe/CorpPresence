using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal sealed class Isa : Character
{
	private const string Immunin = "large_purple_injector";

	private const string Oxymore = "pink_injector";

	private const string Naloxone = "pure_white_injector";

	private const string Synapse = "black_injector";

	private const string Stamina = "@stamina";

	private const string PainBlock = "@painblock";

	public override string Id => "isa";

	public override string Name => L.K("characters.isa.name.isa", "伊莎", "Isa");

	public override string Faction => "FACTION_UPPER_LEVEL";

	public override int[] Thresholds => new int[7] { 20, 40, 60, 80, 100, 100, 100 };

	public override int CashNeeded => 1000;

	public override string PortraitRef => "femaleUpper_1";

	public override string[] RefuseIds => new string[7] { "large_purple_injector", "phagimycin_pill", "smelling_salt", "zerochew", "pink_injector", "pure_white_injector", "corp_presence_painblock" };

	public override int BuyGoodsNeeded => 1;

	private static string Bye => L.K("characters.isa.bye.rememberThisVisitDoesNot", "记住，本次拜访不构成任何医疗建议<i>（官方腔调）</i>。", "Remember, this visit does not constitute medical advice. <i>(in an official tone)</i>");

	private static string[] Talk => new string[6]
	{
		L.K("characters.isa.talk.upUpperLevelsZeroChewGoes", "零嚼在上层改了个名字，叫「纤体-X」。药还是同一种，但价格能买一整箱零嚼。", "Up in the Upper Levels, ZeroChew goes by a different name: “Slim-X”. Same drug, but the price would buy you a whole box of ZeroChew."),
		L.K("characters.isa.talk.immunivaxOneShotEveryWeek", "免疫宁每周都需要一针，回头客真是稳定呢<i>（讽刺意味）</i>。", "Immunivax, one shot every week. Such a steady stream of repeat customers. <i>(sarcastically)</i>"),
		L.K("characters.isa.talk.firstThreeDigitsSerialNumber", "序列号的前三位是批次。手册上有写需要召回的批次，记得对比一下。", "The first three digits of the serial number identify the batch. The recalled batches are listed in the manual. Remember to check against it."),
		L.K("characters.isa.talk.oxymorePerfectlyLegalKnowAll", "奥克西莫可是合法药品，手续齐全。", "Oxymore is perfectly legal, you know. All the paperwork is in order."),
		L.K("characters.isa.talk.someSaySentinelCollapseWas", "有人说哨兵崩溃是人为设计的，公司对此不予置评<i>（官方腔调）</i>。", "Some say Sentinel Collapse was engineered. The company has no comment. <i>(in an official tone)</i>"),
		L.K("characters.isa.talk.carmichealHoldsPatentPhagimycinYet", "噬菌霉素的专利在卡迈克尔手里，可大部分人居然毫不知情。", "Carmicheal holds the patent for Phagimycin, yet most people have absolutely no idea.")
	};

	public static bool Licensed
	{
		get
		{
			CorpState corpState = CorpState.Get();
			if (corpState == null)
			{
				return false;
			}
			return corpState.Of("isa").Chapter >= 5;
		}
	}

	public override string Portrait(CharacterState s, string kind)
	{
		return "carmicheal_isa";
	}

	public override bool IsSide(int chapter)
	{
		if (chapter != 4)
		{
			return chapter >= 6;
		}
		return true;
	}

	public override bool SideAllowed(int chapter)
	{
		switch (chapter)
		{
		case 6:
		{
			CorpState corpState = CorpState.Get();
			return corpState != null && corpState.CompanyOf("carmicheal").CreatureGiven && CarmichaelCo.HasAdultCreature();
		}
		case 7:
			return CarmichaelCo.Licensed && (CorpState.Get()?.CompanyOf("carmicheal").BloodDrawn ?? 0) > 0;
		default:
			return true;
		}
	}

	public override int CashFor(int chapter)
	{
		if (chapter < 6)
		{
			return CashNeeded;
		}
		return 0;
	}

	public override bool DailyBuyer(CharacterState s)
	{
		return true;
	}

	public override bool DailyAccepts(GameItem item)
	{
		return Character.HasTag(item, "CHEMICAL_SUPPLIES");
	}

	public override string[] WantTags(bool daily)
	{
		if (!daily)
		{
			return Array.Empty<string>();
		}
		return new string[1] { "CHEMICAL_SUPPLIES" };
	}

	public override string[] WantIds(bool daily)
	{
		if (!daily)
		{
			return new string[1] { "black_injector" };
		}
		return Array.Empty<string>();
	}

	public override GameItem MakeItem(string id)
	{
		if (!(id == "large_purple_injector"))
		{
			return base.MakeItem(id);
		}
		return CarmichaelCo.MakeGenuine(id);
	}

	private static bool IsSynapse(GameItem item)
	{
		try
		{
			return item?.identifier == "black_injector";
		}
		catch
		{
			return false;
		}
	}

	public override bool IsBuyChapter(int chapter)
	{
		return chapter == 4;
	}

	public override int BuyGoodsHeld()
	{
		return Character.CountHeld(IsSynapse);
	}

	public override bool Accepts(GameItem item)
	{
		return IsSynapse(item);
	}

	public override double PriceMultiplier(GameItem item, string kind)
	{
		return 1.5;
	}

	public override Visit Chapter(int chapter)
	{
		switch (chapter)
		{
		case 1:
		{
			Visit visit = new Visit();
			visit.Lines = new string[5]
			{
				Character.Act(L.K("characters.isa.chapter.setsDownBusinessCard", "放下名片", "sets down a business card"), L.K("characters.isa.chapter.isaLangdonMarketComplianceOfficer", "伊莎·兰登，卡迈克尔制药，市场规范部专员。", "Isa Langdon, Market Compliance Officer, Carmicheal Pharmaceutical.")),
				L.K("characters.isa.chapter.weMakeMostMedicineLl", "你能见到的药大部分都是我们在生产。尤其是免疫宁，医药化工的杰作。", "We make most of the medicine you'll come across. Immunivax in particular—a masterpiece of pharmaceutical chemistry."),
				L.K("characters.isa.chapter.butRecentFloodCopiesHas", "不过最近的大量仿制品已经冲击了正品售卖，劣质假药还害死了一些潜在买家。", "But the recent flood of copies has been cutting into genuine sales. Some shoddy counterfeits have even killed potential customers."),
				L.K("characters.isa.chapter.thisHappenWhilePatentStill", "专利期内发生这种事，公司高层非常不快。<i>（微微蹙眉）</i>", "For this to happen while the patent is still in effect... The higher-ups are very displeased. <i>(frowns slightly)</i>"),
				Character.Act(L.K("characters.isa.chapter.handsOverManual", "递过一本手册", "hands over a manual"), L.K("characters.isa.chapter.iHopeCanHelpUs", "希望你能帮我们回收一些仿制品。", "I hope you can help us take some of these copies off the market."))
			};
			visit.Exit = Character.Act(L.K("characters.isa.chapter.closesFolder", "合上文件夹", "closes the folder"), Bye, showAction: false);
			visit.Discount = 30;
			visit.Stock = new string[2] { "large_purple_injector", "large_purple_injector" };
			visit.Gift = new string[1] { "injector_guide" };
			return visit;
		}
		case 2:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.K("characters.isa.chapter.helloIHandsOverList", "你好，<i>（递过一张单子）</i>，召回通知，回收单子上列出的批次。", "Hello. <i>(hands over a list)</i> A recall notice. We're recalling the batches listed here."),
				Character.Act(L.K("characters.isa.chapter.officialTone", "官方腔调", "in an official tone"), L.K("characters.isa.chapter.alsoVeBeenAuthorizedResell", "还有，你获得了外售权限。这是本期的。", "Also, you've been authorized to resell. Here's this period's allocation."), showAction: false),
				Character.Act(L.K("characters.isa.chapter.pausesLowersHerVoice", "停顿，压低声音", "pauses, lowers her voice"), L.K("characters.isa.chapter.thereSActuallyNothingWrong", "其实这批次的药没什么问题。就是外贸部经理那个糊涂蛋把价格定低了。", "There's actually nothing wrong with this batch. That idiot of a trade manager just priced it too low."))
			};
			visit.Exit = Character.Act(L.K("characters.isa.chapter.signsList", "在单子上签字", "signs the list"), L.K("characters.isa.chapter.delivered", "已送达。", "Delivered."));
			visit.Discount = 30;
			visit.Stock = new string[3] { "large_purple_injector", "large_purple_injector", "large_purple_injector" };
			return visit;
		}
		case 3:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				Character.Act(L.K("characters.isa.chapter.herself", "自言自语", "to herself"), L.K("characters.isa.chapter.oxymoreSalesLowerLevelsAre", "下层区这个季度的奥克西莫销量涨了四成。", "Oxymore sales in the Lower Levels are up forty percent this quarter.")),
				Character.Act(L.K("characters.isa.chapter.turnsPage", "翻了一页", "turns a page"), L.K("characters.isa.chapter.naloxgoneUpFortyPercentToo", "纳洛通也涨了四成。", "Naloxgone is up forty percent too."), showAction: false),
				Character.Act(L.K("characters.isa.chapter.looksUpNotices", "抬头看到你", "looks up and notices you"), L.K("characters.isa.chapter.ohSorryLetSGet", "哦，对不起。我们继续谈生意。", "Oh, sorry. Let's get back to business."))
			};
			visit.Exit = Character.Act(L.K("characters.isa.chapter.closesFolderHesitates", "合上文件夹，犹豫", "closes the folder, hesitates"), L.K("characters.isa.chapter.dBestStayOffOxymore", "……你自己最好别用奥克西莫。", "...You'd best stay off Oxymore yourself."));
			visit.Discount = 30;
			visit.Stock = new string[6] { "pink_injector", "pink_injector", "pink_injector", "pure_white_injector", "pure_white_injector", "pure_white_injector" };
			return visit;
		}
		case 4:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.K("characters.isa.chapter.iHearHaveSynaprestThat", "听说你这里有赛纳普斯？就是帕斯泰尔的那种戒瘾药。", "I hear you have Synaprest? That addiction treatment from pastel."),
				L.K("characters.isa.chapter.rDPeopleInsistTaking", "研发部那帮人非要拆开看看，要我收购一些。", "The R&D people insist on taking it apart, so they've asked me to buy some."),
				Character.Act(L.K("characters.isa.chapter.pauses", "停顿", "pauses"), L.K("characters.isa.chapter.weDLikeGetInto", "我们也想做戒瘾的生意。", "We'd like to get into the addiction treatment business too."))
			};
			visit.Exit = L.K("characters.isa.chapter.donTMentionThisAnyone", "别跟帕斯泰尔的人提。", "Don't mention this to anyone at pastel.");
			visit.Buy = true;
			visit.Wrong = L.K("characters.isa.chapter.uhBossSSynaprestI", "呃，店长，我要的是赛纳普斯。", "Uh, boss, it's Synaprest I'm after.");
			return visit;
		}
		case 6:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.K("characters.isa.chapter.iHearSpecimenHereWith", "听说收容物在你这里？诶，真让我头疼。<i>（扶额）</i>", "I hear the specimen is here with you? Oh, what a headache. <i>(puts a hand to her forehead)</i>"),
				Character.Act(L.K("characters.isa.chapter.looksLizard", "看看蜥蜴", "looks at the lizard"), L.K("characters.isa.chapter.evenCompanySBreedingProgram", "就连公司的繁殖计划都才初步完成诶，你怎么能养得这么好。", "Even the company's breeding program is only through its initial phase. How have you managed to raise it so well?")),
				Character.Act(L.K("characters.isa.chapter.setsDownDocumentBloodDrawer", "放下文件和一支采血器", "sets down a document and a blood drawer"), L.K("characters.isa.chapter.allRightIGotBreeding", "好吧，我帮你申请了养殖许可。以后要养这种未知生物，请先跟我确认一下。", "All right, I got you a breeding license. Next time you want to keep an unknown creature like this, please check with me first."))
			};
			visit.Exit = Character.Act(L.K("characters.isa.chapter.closesFolder", "合上文件夹", "closes the folder"), L.K("characters.isa.chapter.courseWeLlBuyWhatever", "当然，三叶蜥的产物我们也会回收。虽然我现在连产什么都不知道。", "Of course, we'll buy whatever the trilobites produce too. Though I don't even know what that is yet."), showAction: false);
			visit.TalkOnly = true;
			visit.Gift = new string[1] { "@blood_drawer" };
			return visit;
		}
		case 7:
		{
			Visit visit = new Visit();
			visit.Lines = new string[4]
			{
				L.K("characters.isa.chapter.heyIGreetsI", "嘿。<i>（打招呼）</i>", "Hey. <i>(greets you)</i>"),
				L.K("characters.isa.chapter.haveDrawnItsBloodYet", "你采过血了没？研发部需要一些原料。", "Have you drawn its blood yet? R&D needs some raw materials."),
				Character.Act(L.K("characters.isa.chapter.hasMachineCarried", "让人搬进来一台机器", "has a machine carried in"), L.K("characters.isa.chapter.reactorAsLongAsPurity", "反应釜，只要纯度够就能制药。", "A reactor. As long as the purity is high enough, it can make medicine.")),
				L.K("characters.isa.chapter.thisOneSFreeIf", "这台不收钱，以后坏了可就要按原价订了哦。", "This one's free. If it breaks, though, you'll have to order another at full price.")
			};
			visit.Exit = Character.Act(L.K("characters.isa.chapter.closesFolder", "合上文件夹", "closes the folder"), L.K("characters.isa.chapter.rememberPurityEverything", "记住——纯度，决定一切。", "Remember—purity is everything."), showAction: false);
			visit.TalkOnly = true;
			visit.Gift = new string[2] { "@isomer_reactor", "@sb_handbook" };
			return visit;
		}
		default:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.K("characters.isa.chapter.hereStaminaBoosterNewProduct", "喏，体力增强剂。新产品，目前还没上市，需要先找几个实验人员。", "Here, a Stamina Booster. A new product, not on the market yet. We need a few test subjects first."),
				L.K("characters.isa.chapter.permanentlyImprovesConstitutionEveryDose", "能永久改善体质，每一支都要申请，还要走很长的审批流程。你要珍惜哦，实验员。", "It permanently improves your constitution. Every dose needs an application and a lengthy approval process. Make it count, test subject."),
				Character.Act(L.K("characters.isa.chapter.setsOneDown", "放下一支", "sets one down"), L.K("characters.isa.chapter.thisOneSThisPeriod", "这是本期的，以后要用给我打电话。", "This one's for this period. Give me a call when you need more."))
			};
			visit.Exit = Character.Act(L.K("characters.isa.chapter.closesFolderRemindsQuietly", "合上文件夹，小声叮嘱", "closes the folder, reminds you quietly"), L.K("characters.isa.chapter.lowerLevelsWillComeUnder", "下个季度，下层区会划拨到我的管辖范围。一定要记得定期申请哦，这可是好东西。", "The Lower Levels will come under my jurisdiction next quarter. Do remember to apply regularly, all right? This is good stuff."));
			visit.TalkOnly = true;
			visit.Gift = new string[1] { "@stamina" };
			return visit;
		}
		}
	}

	public override Visit Daily(CharacterState s)
	{
		IEnumerable<string> source = Talk.AsEnumerable();
		if (s.SideDone.Contains(4))
		{
			source = source.Append(L.K("characters.isa.daily.rDTookApartLot", "研发部拆了很多赛纳普斯。本来想做戒瘾药的，但是未知成分太多了，只做出来了镇痛剂。", "R&D took apart a lot of Synaprest. They were trying to make an addiction treatment, but there were too many unknown compounds. All they managed was a painkiller."));
		}
		if (s.Chapter >= 5)
		{
			source = source.Append(L.K("characters.isa.daily.rememberApplyStaminaBoostersTime", "记得按时申请体力增强剂。", "Remember to apply for your Stamina Boosters on time."));
		}
		IEnumerable<string> enumerable = new string[4] { "large_purple_injector", "phagimycin_pill", "smelling_salt", "zerochew" }.AsEnumerable();
		if (s.Chapter >= 3)
		{
			enumerable = enumerable.Concat(new string[2] { "pink_injector", "pure_white_injector" });
		}
		IEnumerable<string> enumerable2 = Engine.Pick(enumerable.ToArray(), 3, 5).AsEnumerable();
		if (s.SideDone.Contains(4))
		{
			enumerable2 = enumerable2.Concat(Engine.Pick(new string[1] { "@painblock" }, 1, 2));
		}
		Visit visit = new Visit();
		visit.Lines = new string[2]
		{
			Engine.Any(L.K("characters.isa.daily.carmichealMarketComplianceRoutineVisit", "卡迈克尔市场规范部，例行走访。", "Carmicheal Market Compliance. Routine visit."), L.K("characters.isa.daily.channelInspectionJustFewMinutes", "渠道检查，几分钟就好。", "Channel inspection. Just a few minutes."), L.K("characters.isa.daily.isaLangdonUsualCheck", "伊莎·兰登。照例检查。", "Isa Langdon. The usual check.")),
			Engine.Any(source.ToArray())
		};
		visit.Exit = Character.Act(L.K("characters.isa.daily.closesFolder", "合上文件夹", "closes the folder"), Bye, showAction: false);
		visit.Discount = ((s.Chapter >= 3) ? 30 : 20);
		visit.Stock = enumerable2.ToArray();
		visit.Extra = enumerable.ToArray();
		visit.Both = true;
		visit.Wrong = L.K("characters.isa.daily.gotAnyChemicals", "有化学品吗？", "Got any chemicals?");
		return visit;
	}
}
