using System;
using System.Linq;

namespace CorpPresence;

internal sealed class Seline : Character
{
	public override string Id => "seline";

	public override string Name => L.T("瑟琳", "Seline");

	public override string Faction => "FACTION_UPPER_LEVEL";

	public override int[] Thresholds => new int[3] { 20, 40, 80 };

	public override int CashNeeded => 1000;

	public override string PortraitRef => "femaleUpper_1";

	public override int BuyGoodsNeeded => 2;

	public override string[] RefuseIds => new string[7] { "skincare_cream", "neuroactive_perfume", "pheromone_perfume", "corp_presence_mask", "corp_presence_candle", "corp_presence_candle_lit", "corp_presence_elixir" };

	private static string[] Talk => new string[6]
	{
		L.T("喷一点「同步」，它会帮你打开顾客的钱包。", "A spritz of SYNC, and it'll open your customers' wallets for you."),
		L.T("全星系只有一家真正的奢侈品公司。", "There's only one true luxury house in the entire galaxy."),
		L.T("没人会拒绝索拉纳。", "Nobody turns down Solana."),
		L.T("下层人用不起这些，也用不着。", "People down here can't afford these, and they have no use for them."),
		L.T("科技展上那些新锐的化妆品，大半都是索拉纳的下游实验室产的。", "Most of those innovative cosmetics at the tech expo come from Solana's downstream labs."),
		L.T("我从不过问实验室的事。", "I never ask about the lab.")
	};

	public override string Portrait(CharacterState s, string kind)
	{
		return "solana_seline";
	}

	public override bool IsBuyChapter(int chapter)
	{
		return chapter == 2;
	}

	public override int BuyGoodsHeld()
	{
		return Character.CountHeld(Accepts);
	}

	public override bool Accepts(GameItem item)
	{
		if (Character.IsLuxury(item))
		{
			return !Array.Exists(RefuseIds, (string id) => id == item.identifier);
		}
		return false;
	}

	public override string[] WantTags(bool daily)
	{
		return new string[1] { "LUXURY_ITEM" };
	}

	public override double PriceMultiplier(GameItem item, string kind)
	{
		if (!(item.identifier == "blue_blood_bag"))
		{
			if (!kind.StartsWith("daily"))
			{
				return 1.3;
			}
			if (!Settings.VanillaBuyPrices)
			{
				return 1.2;
			}
			return 1.0;
		}
		if (!Settings.VanillaBuyPrices)
		{
			return 3.0;
		}
		return 1.5;
	}

	public override bool DailyBuyer(CharacterState s)
	{
		return true;
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
				Character.Act(L.T("拍下名片，眯眼轻睨", "slaps down a business card and gives you a sidelong glance, eyes narrowed"), L.T("瑟琳，索拉纳生物品牌顾问与市场专员。", "Seline. Brand consultant and marketing specialist at Solana Biogenics.")),
				L.T("「永恒」、「同步」、「本源」……还用我继续介绍吗？", "“ETERNAL”, “SYNC”, “L'ESSENCE”… Do I really need to go on?"),
				L.T("我们一般不在下层做生意，只是有人推荐说你有点潜力。<i>（轻哼）</i>", "We don't usually do business in the Lower Levels. But someone recommended you, saying you have a little potential. <i>(scoffs)</i>")
			};
			visit.Exit = Character.Act(L.T("扬起下巴", "lifts her chin"), L.T("嗯，不错。", "Hmm. Not bad."));
			visit.Discount = 30;
			visit.Stock = new string[2] { "skincare_cream", "skincare_cream" };
			return visit;
		}
		case 2:
		{
			Visit visit = new Visit();
			visit.Lines = new string[2]
			{
				L.T("我们在做市场调研，所以要收购一批奢侈品，看看别家在卖什么。", "We're doing market research, so we're buying a batch of luxury goods to see what the competition is selling."),
				Character.Act(L.T("看了看柜台", "glances at the counter"), L.T("我会按采购标准付钱，不要次品。索拉纳自家的也不必拿出来了。", "I'll pay according to our purchasing standards. Nothing second-rate. Don't bother bringing out Solana's own products either."))
			};
			visit.Exit = Character.Act(L.T("点点头", "nods"), L.T("以后精品都留给我。", "Save all the finest pieces for me from now on."));
			visit.Buy = true;
			visit.Wrong = L.T("别糊弄我——只收奢侈品！", "Don't try to fool me—luxury goods only!");
			return visit;
		}
		default:
		{
			Visit visit = new Visit();
			visit.Lines = new string[2]
			{
				L.T("这些可在上层都是稀罕物，不过我猜你用得上。<i>（打开盒子）</i>", "These are rare even in the Upper Levels, but I thought you could use them. <i>(opens a box)</i>"),
				Character.Act(L.T("顿了顿", "pauses"), L.T("那瓶送你了，算我请客。", "That bottle's yours. My treat."))
			};
			visit.Exit = Character.Act(L.T("笑了一下", "smiles briefly"), L.T("收好，我走了。", "Keep it safe. I'm off."));
			visit.Discount = 50;
			visit.Stock = new string[2] { "pheromone_perfume", "pheromone_perfume" };
			visit.Gift = new string[1] { "neuroactive_perfume" };
			return visit;
		}
		}
	}

	public override Visit Daily(CharacterState s)
	{
		string[] array;
		if (s.Chapter < 3)
		{
			array = ((s.Chapter < 2) ? new string[3] { "skincare_cream", "neuroactive_perfume", "@mask" } : new string[4] { "skincare_cream", "neuroactive_perfume", "@mask", "@candle" });
		}
		else
		{
			array = new string[6] { "skincare_cream", "neuroactive_perfume", "pheromone_perfume", "@mask", "@candle", "@elixir" };
		}
		Visit visit = new Visit();
		visit.Lines = new string[2]
		{
			Engine.Any(L.T("你好，又见面了。", "Hello. We meet again."), L.T("别误会，只是路过。", "Don't get the wrong idea. I'm just passing by."), L.T("最近生意还行？", "How's business lately?")),
			Engine.Any(Talk)
		};
		visit.Exit = Engine.Any(Character.Act(L.T("轻轻点头", "gives a slight nod"), L.T("嗯。", "Mm.")), Character.Act(L.T("看了看表", "checks her watch"), L.T("我该走了。", "I should go.")));
		visit.Both = true;
		visit.Discount = 20;
		visit.Stock = Engine.Pick(array, 1, 2);
		visit.Extra = array;
		visit.Wrong = L.T("我不是收破烂的。", "I'm not a junk dealer.");
		return visit;
	}
}
