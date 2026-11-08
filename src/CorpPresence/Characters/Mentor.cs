using System;

namespace CorpPresence;

internal sealed class Mentor : Character
{
	public static readonly string[] Factions = new string[3] { "FACTION_BLACK_MARKET", "FACTION_REVOLUTION", "FACTION_LOWER_LEVEL" };

	public override string Id => "mentor";

	public override string Name => L.K("characters.mentor.name.mentor", "导师", "The Mentor");

	public override string Faction => "FACTION_LOWER_LEVEL";

	public override int[] Thresholds => new int[3] { 20, 40, 80 };

	public override int CashNeeded => 300;

	public override string PortraitRef => "mentor";

	public override bool HasDaily => false;

	public static string[] TrustFactions
	{
		get
		{
			if (!Settings.MaliceMode)
			{
				return Factions;
			}
			return new string[1] { Factions[0] };
		}
	}

	public override int BuyGoodsNeeded => 1;

	private static string Nod => Character.Act(L.K("characters.mentor.nod.nods", "点点头", "nods"), "");

	public override int Rep()
	{
		if (!Settings.MaliceMode)
		{
			return Math.Max(Engine.Reputation(Factions[0]), Math.Max(Engine.Reputation(Factions[1]), Engine.Reputation(Factions[2])));
		}
		return Engine.Reputation(Factions[0]);
	}

	public override string Portrait(CharacterState s, string kind)
	{
		try
		{
			return SpriteDict.Instance.spriteDictionary.ContainsKey("mentor") ? "mentor" : "Mentor";
		}
		catch
		{
			return "mentor";
		}
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
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.K("characters.mentor.chapter.longTimeNoSee", "好久不见。", "Long time no see."),
				L.K("characters.mentor.chapter.broughtSomeHardwareNestfireLiberator", "我给你带了些家伙什——巢火和解放者，本地产，纯手搓。", "Brought you some hardware—the Nestfire and the Liberator. Locally made, all by hand."),
				Character.Act(L.K("characters.mentor.chapter.leavesNoteCounter", "便签放在柜台上", "leaves a note on the counter"), L.K("characters.mentor.chapter.mostCargoDocksPassesThrough", "码头的货大半都得过我们的手，有事打这个。", "Most of the cargo at the docks passes through our hands. Need anything, call this number."))
			};
			visit.Exit = Nod;
			visit.Discount = 30;
			visit.Stock = new string[3] { "@handmade_pistol", "small_pistol_ammo", "small_pistol_ammo" };
			return visit;
		}
		case 2:
		{
			Visit visit = new Visit();
			visit.Lines = new string[4]
			{
				L.K("characters.mentor.chapter.iVeHeardPeopleRecommending", "最近听到有别人在推荐你这里。", "I've heard people recommending your shop lately."),
				L.K("characters.mentor.chapter.goodWorkKeepUp", "做的不错，继续保持。", "Good work. Keep it up."),
				L.K("characters.mentor.chapter.text", "......", "......"),
				L.K("characters.mentor.chapter.iBroughtSomeNewStock", "我带来一批新货。", "I brought some new stock.")
			};
			visit.Exit = Nod;
			visit.Discount = 30;
			visit.Stock = new string[4] { "printed_gun", "unlicensed_phagimycin_pill", "unlicensed_phagimycin_pill", "salve" };
			return visit;
		}
		default:
		{
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.K("characters.mentor.chapter.thingsHaveBeenGettingWorse", "最近局势更严峻了。", "Things have been getting worse lately."),
				L.K("characters.mentor.chapter.rememberBlackMarketRevolutionLower", "记得么？黑市、革命军、下层区是一个整体。", "Remember? The Black Market, the Revolution and the Lower Levels stand as one."),
				L.K("characters.mentor.chapter.ifReDangerCallHelp", "遇到危险立刻求援，我们会帮你。", "If you're in danger, call for help right away. We'll help you.")
			};
			visit.Exit = L.K("characters.mentor.chapter.takeCare", "保重。", "Take care.");
			visit.TalkOnly = true;
			return visit;
		}
		}
	}

	public override Visit Daily(CharacterState s)
	{
		return Chapter(3);
	}

	public override GameItem MakeItem(string id)
	{
		if (!id.StartsWith("@") || Goods.Has(id.Substring(1)))
		{
			return base.MakeItem(id);
		}
		return SpacerCo.MakeGun(id.Substring(1));
	}
}
