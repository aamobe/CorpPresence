using System;

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
		L.T("“同步”喷一点，它会帮你解开顾客钱包的扣子。", "A spritz of SYNC, and it'll unclasp your customers' wallets for you."),
		L.T("在上层区，只有一家顶尖奢侈品公司。", "In the Upper Levels, there is only one top luxury house."),
		L.T("没人会拒绝索拉纳。", "Nobody turns down Solana."),
		L.T("下层的人用不起这些。也用不着。", "People down here can't afford these. Or need them."),
		L.T("科技展上那些新品化妆品，大半是索拉纳的下游实验室产出的。", "Most of the new cosmetics at the tech expo come out of Solana's downstream labs."),
		L.T("实验室的事，我不过问。", "I don't ask about the lab.")
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
			Visit visit3 = new Visit();
			visit3.Lines = new string[3]
			{
				L.T("瑟琳，索拉纳生物，品牌顾问。", "Seline. Solana Biogenics, brand consultant."),
				L.T("“永恒”“同步”“本源”，还用我继续介绍吗？（轻哼）。", "ETERNAL, SYNC, L'ESSENCE. Do I really need to go on? (scoffs)"),
				L.T("我们一般不跟下层做生意，不过听说你还算有能力。", "We don't usually do business down here. But I hear you're fairly capable.")
			};
			visit3.Exit = Character.Act(L.T("上下打量", "looks you up and down"), L.T("还不错。", "Not bad."));
			visit3.Discount = 30;
			visit3.Stock = new string[2] { "skincare_cream", "skincare_cream" };
			return visit3;
		}
		case 2:
		{
			Visit visit2 = new Visit();
			visit2.Lines = new string[2]
			{
				L.T("索拉纳在做市场调研，要收购一批奢侈品，看看别家在卖什么。", "Solana is doing market research. We're buying luxury goods to see what the competition sells."),
				Character.Act(L.T("看了看柜台", "glances at the counter"), L.T("按采购标准付钱，不用谈。次品就不必拿出来了。", "We pay by our purchasing standards, no haggling. Don't bother bringing out anything second-rate."))
			};
			visit2.Exit = Character.Act(L.T("点点头", "nods"), L.T("好东西，记得先留给我。", "Remember to save the good stuff for me."));
			visit2.Buy = true;
			visit2.Wrong = L.T("别糊弄我。", "Don't try to fool me.");
			return visit2;
		}
		default:
		{
			Visit visit = new Visit();
			visit.Lines = new string[2]
			{
				L.T("上层区最近都在找这个。", "Everyone in the Upper Levels is looking for this lately."),
				Character.Act(L.T("打开盒子", "opens a box"), L.T("我猜你用得上。", "I suspect you'll find a use for it."))
			};
			visit.Exit = Character.Act(L.T("看了眼门外", "glances at the door"), L.T("收好。", "Keep it safe."));
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
		if (s.Chapter >= 3)
		{
			array = new string[6] { "skincare_cream", "neuroactive_perfume", "pheromone_perfume", "@mask", "@candle", "@elixir" };
		}
		else
		{
			array = ((s.Chapter >= 2) ? new string[4] { "skincare_cream", "neuroactive_perfume", "@mask", "@candle" } : new string[3] { "skincare_cream", "neuroactive_perfume", "@mask" });
		}
		Visit visit = new Visit();
		visit.Lines = new string[2]
		{
			Engine.Any(L.T("又见面了。", "We meet again."), L.T("路过。", "Just passing by."), L.T("生意还行？", "Business going well?")),
			Engine.Any(Talk)
		};
		visit.Exit = Engine.Any(Character.Act(L.T("上下打量", "looks you up and down"), L.T("。", "")), Character.Act(L.T("看了看表", "checks her watch"), L.T("我该走了。", "I should go.")));
		visit.Both = true;
		visit.Discount = 20;
		visit.Stock = Engine.Pick(array, 1, 2);
		visit.Extra = array;
		visit.Wrong = L.T("索拉纳的东西，我不收回。", "I don't buy back Solana products.");
		return visit;
	}
}
