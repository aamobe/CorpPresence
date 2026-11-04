using System;

namespace CorpPresence;

internal sealed class Mentor : Character
{
	public static readonly string[] Factions = new string[3] { "FACTION_BLACK_MARKET", "FACTION_REVOLUTION", "FACTION_LOWER_LEVEL" };

	public override string Id => "mentor";

	public override string Name => L.T("导师", "The Mentor");

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

	private static string Nod => Character.Act(L.T("点点头", "nods"), "");

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
			Visit visit3 = new Visit();
			visit3.Lines = new string[3]
			{
				L.T("码头的货，大半都得过我们的手。", "Most of the cargo at the docks passes through our hands."),
				L.T("这是巢火和解放者，我们手搓的。", "This is the Nestfire and the Liberator. We made them by hand."),
				Character.Act(L.T("便签放在柜台上", "leaves a note on the counter"), L.T("有事，打这个。", "Need something, call this."))
			};
			visit3.Exit = Nod;
			visit3.Discount = 30;
			visit3.Stock = new string[3] { "@handmade_pistol", "small_pistol_ammo", "small_pistol_ammo" };
			return visit3;
		}
		case 2:
		{
			Visit visit2 = new Visit();
			visit2.Lines = new string[2]
			{
				L.T("要加入星际客联盟吗。", "Want to join the Spacer Alliance?"),
				L.T("黑市，革命军，下层区，记住我们是一个整体。", "The Black Market, the Revolution, the Lower Levels. Remember, we are one whole.")
			};
			visit2.Exit = Nod;
			visit2.Discount = 30;
			visit2.Stock = new string[4] { "printed_gun", "unlicensed_phagimycin_pill", "unlicensed_phagimycin_pill", "salve" };
			return visit2;
		}
		default:
		{
			Visit visit = new Visit();
			visit.Lines = new string[2]
			{
				L.T("星际客也有自己的港口。", "Spacers have their own port too."),
				Character.Act(L.T("敲了敲柜台", "knocks on the counter"), L.T("你这儿也算一个。", "Yours counts as one too."))
			};
			visit.Exit = Nod;
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
