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

	public override string Name => L.T("伊莎", "Isa");

	public override string Faction => "FACTION_UPPER_LEVEL";

	public override int[] Thresholds => new int[7] { 20, 40, 60, 80, 100, 100, 100 };

	public override int CashNeeded => 1000;

	public override string PortraitRef => "femaleUpper_1";

	public override string[] RefuseIds => new string[7] { "large_purple_injector", "phagimycin_pill", "smelling_salt", "zerochew", "pink_injector", "pure_white_injector", "corp_presence_painblock" };

	public override int BuyGoodsNeeded => 1;

	private static string Bye => L.T("本次拜访不构成任何医疗建议（官方腔调）。", "This visit does not constitute medical advice. (in an official tone)");

	private static string[] Talk => new string[6]
	{
		L.T("零嚼只是在上层区改了名，叫“纤体-X”。药是同一种，可是价格在下城区能买一箱零嚼。", "In the Upper Levels, ZeroChew just got renamed \"Slim-X\". Same drug, but what it costs up there buys a whole box of ZeroChew down here."),
		L.T("免疫宁每周都需要一针。回头客真是稳定（讽刺意味）。", "Immunivax, one shot every week. Such reliable repeat customers. (sarcastically)"),
		L.T("序列号前三位是批次。需要召回的批次，手册上有，记得比对一下。", "The first three digits of the serial are the batch. Batches to be recalled are in the manual. Remember to check against it."),
		L.T("奥克西莫是合法药品，手续齐全。", "Oxymore is a legal drug. All the paperwork is in order."),
		L.T("有人说哨兵崩溃是人为设计的。公司对此不予置评（官方腔调）。", "Some say Sentinel Collapse was engineered. The company has no comment. (in an official tone)"),
		L.T("噬菌霉素，专利在卡迈克尔手里，可是大部分人居然都不知道。", "Carmicheal holds the Phagimycin patent. Most people don't even know that.")
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
			if (corpState != null && corpState.CompanyOf("carmicheal").CreatureGiven)
			{
				return CarmichaelCo.HasAdultCreature();
			}
			return false;
		}
		case 7:
			if (CarmichaelCo.Licensed)
			{
				return (CorpState.Get()?.CompanyOf("carmicheal").BloodDrawn ?? 0) > 0;
			}
			return false;
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
			Visit visit7 = new Visit();
			visit7.Lines = new string[5]
			{
				L.T("卡迈克尔制药，市场规范部专员。伊莎·兰登。", "Carmicheal Pharmaceutical, Market Compliance. Isa Langdon."),
				L.T("你能见到的药一大半都是我们在生产，只是目前公司最在意的还是专利期内的免疫宁", "Most of the medicine you'll ever see is ours. Right now, though, the company cares most about Immunivax, still under patent."),
				L.T("专利期内，居然会有大量仿制品，真是荒唐（不快）。", "Still under patent, and yet copies are everywhere. Absurd. (displeased)"),
				L.T("最近大量仿制品已经冲击了正品售卖", "Lately the copies have been cutting into genuine sales."),
				Character.Act(L.T("递过一本手册", "hands over a manual"), L.T("我希望你能帮我们回收一部分仿制品。", "I'd like you to help us take some of the copies off the market."))
			};
			visit7.Exit = Character.Act(L.T("合上文件夹", "closes the folder"), Bye);
			visit7.Discount = 30;
			visit7.Stock = new string[2] { "large_purple_injector", "large_purple_injector" };
			visit7.Gift = new string[1] { "injector_guide" };
			return visit7;
		}
		case 2:
		{
			Visit visit6 = new Visit();
			visit6.Lines = new string[3]
			{
				L.T("召回通知。", "Recall notice. ") + Character.Act(L.T("递过一张单子", "hands over a list"), L.T("单子上的批次我们都会回收。", "We're recalling every batch on this list.")),
				L.T("（官方腔调）你有外售权限", "(in an official tone) You are authorized to resell."),
				Character.Act(L.T("停顿", "pauses"), L.T("这批次的药其实没问题，就是外贸部经理那个糊涂蛋把价格定低了。", "Nothing was actually wrong with these batches. That idiot of a trade manager just priced them too low."))
			};
			visit6.Exit = Character.Act(L.T("在单子上签字", "signs the list"), L.T("已送达。", "Delivered."));
			visit6.Discount = 30;
			visit6.Stock = new string[3] { "large_purple_injector", "large_purple_injector", "large_purple_injector" };
			return visit6;
		}
		case 3:
		{
			Visit visit5 = new Visit();
			visit5.Lines = new string[3]
			{
				L.T("（自言自语）奥克西莫，这个季度下层区销量涨了四成。", "(to herself) Oxymore sales in the Lower Levels are up forty percent this quarter."),
				Character.Act(L.T("翻了一页", "turns a page"), L.T("纳洛通，也涨了四成。", "Naloxgone, also up forty percent.")),
				L.T("哦对不起我们继续谈生意。", "Oh, sorry. Back to business.")
			};
			visit5.Exit = Character.Act(L.T("合上文件夹，犹豫", "closes the folder, hesitates"), L.T("最好别用奥克西莫。", "Better not to use Oxymore."));
			visit5.Discount = 30;
			visit5.Stock = new string[6] { "pink_injector", "pink_injector", "pink_injector", "pure_white_injector", "pure_white_injector", "pure_white_injector" };
			return visit5;
		}
		case 4:
		{
			Visit visit4 = new Visit();
			visit4.Lines = new string[3]
			{
				L.T("我听说你这里有赛纳普斯？就是帕斯泰尔的戒瘾针。", "I hear you have some Synaprest? pastel's addiction shot."),
				L.T("研发部那帮人非要要拆开看看。", "The R&D people insist on taking it apart."),
				Character.Act(L.T("停顿", "pauses"), L.T("戒瘾的生意，我们也想做。", "We'd like a piece of the addiction business too."))
			};
			visit4.Exit = Character.Act(L.T("放进冷藏箱", "puts it in a cooler"), L.T("别跟帕斯泰尔提。", "Don't mention this to pastel."));
			visit4.Buy = true;
			visit4.Wrong = L.T("呃，店长，我是要赛纳普斯。", "Uh, I need Synaprest, boss.");
			return visit4;
		}
		case 6:
		{
			Visit visit3 = new Visit();
			visit3.Lines = new string[3]
			{
				L.T("收容物在你这里吧。真让我头疼。", "The specimen is with you, isn't it. What a headache."),
				L.T("不过公司的繁殖计划才初步完成，你怎么能养得这么好。", "The company's breeding program has barely gotten started, and you've raised it this well?"),
				Character.Act(L.T("放下一份文件和一支采血器", "sets down a document and a blood drawer"), L.T("我帮你申请了养殖许可。以后要养这种未知生物，先跟我确定一下。", "I got you a breeding license. If you want to keep more of these unknown creatures, check with me first."))
			};
			visit3.Exit = Character.Act(L.T("合上文件夹", "closes the folder"), L.T("当然三叶蜥的产物我们也会回收，虽然我现在连产什么都不知道。", "We'll buy back whatever the trilobites produce, of course. Though right now I don't even know what that is."));
			visit3.TalkOnly = true;
			visit3.Gift = new string[1] { "@blood_drawer" };
			return visit3;
		}
		case 7:
		{
			Visit visit2 = new Visit();
			visit2.Lines = new string[3]
			{
				L.T("你采过血了吗。研发部想谈原料供应。", "Have you drawn its blood yet? R&D wants to talk about supply."),
				Character.Act(L.T("让人搬进来一台机器", "has a machine carried in"), L.T("反应釜。纯度够，能做三种药。", "A reactor. With enough purity, it makes three kinds of medicine.")),
				L.T("这台机器不收钱。以后坏了，要按原价订。", "This machine is free. If it breaks, you'll have to order another at list price.")
			};
			visit2.Exit = Character.Act(L.T("合上文件夹", "closes the folder"), L.T("记住，纯度，决定一切。", "Remember: purity is everything."));
			visit2.TalkOnly = true;
			visit2.Gift = new string[2] { "@isomer_reactor", "@sb_handbook" };
			return visit2;
		}
		default:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.T("新品体力增强剂，目前还没上市，老板让我来找几个实验人员。", "A new product, the Stamina Booster. Not on the market yet. My boss wants me to find a few test subjects."),
				L.T("能改善体质，每一支可是都要走实验流程才能申请，请你珍惜，实验员。", "It improves your constitution. Every dose has to go through the trial process, so treat it well, test subject."),
				Character.Act(L.T("放下一支", "sets one down"), L.T("这是本期的，以后要用，直接向我申请就行。", "This one is for this period. If you need more later, just apply to me directly."))
			};
			visit.Exit = Character.Act(L.T("合上文件夹", "closes the folder"), L.T("下个季度，下层区划拨到我的管辖范围，一定记得定期申请体力针剂。（小声嘟囔）这可是好东西。", "Next quarter the Lower Levels fall under my jurisdiction. Be sure to apply for your booster shots regularly. (mutters) It's good stuff."));
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
			source = source.Append(L.T("研发部拆了赛纳普斯。本想做戒瘾的，但是未知成分太多了，只做出来了镇痛针。", "R&D took the Synaprest apart. They wanted an anti-addiction drug, but there were too many unknown compounds. All they got was a painkiller."));
		}
		if (s.Chapter >= 5)
		{
			source = source.Append(L.T("体力增强剂，记得按期申请。", "Stamina Booster: remember to apply on schedule."));
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
			Engine.Any(L.T("市场规范部，例行走访。", "Market Compliance. Routine visit."), L.T("渠道检查，几分钟就好。", "Channel inspection. Just a few minutes."), L.T("伊莎·兰登。照例检查。", "Isa Langdon. The usual check.")),
			Engine.Any(source.ToArray())
		};
		visit.Exit = Character.Act(L.T("合上文件夹", "closes the folder"), Bye);
		visit.Discount = ((s.Chapter >= 3) ? 30 : 20);
		visit.Stock = enumerable2.ToArray();
		visit.Extra = enumerable.ToArray();
		visit.Both = true;
		visit.Wrong = L.T("这次只收化学品。", "Only chemicals this time.");
		return visit;
	}
}
