using System;

namespace CorpPresence;

internal sealed class SolanaCo : Company
{
	public const int VipBudgetMin = 3000;

	public const int VipBudgetMax = 5000;

	private const int MaliceVipBudgetMin = 880;
	private const int MaliceVipBudgetMax = 1155;

	private static readonly Random Rng = new Random();

	public static readonly string[] LabPool = new string[10] { "skincare_cream", "neuroactive_perfume", "bottled_water_premium", "black_injector", "large_purple_injector", "phagimycin_pill", "salve", "box_tampon", "cassette1", "cassette4" };

	public override string Id => "solana";

	public override CatalogEntry[] Catalog => Perfume.Catalog;

	public override string Name => L.T("索拉纳生物", "Solana Biogenics");

	public override string CharacterId => "seline";

	public override long PhoneNumber => 76526L;

	public override string[] TierNames => L.T(new string[3] { "银卡会员", "金卡会员", "黑金会员" }, new string[3] { "Silver Member", "Gold Member", "Black Gold Member" });

	public override string CardFlavor => L.T("烫金的字，只有一个号码：76526。", "Gold-foil lettering. Just one number: 76526.");

	public override string DeskName => L.T("索拉纳前台", "Solana Concierge");

	public override string DeskAsk => L.T("请问需要什么服务？", "Which service may I assist you with?");

	public override string[] FrontDesk => new string[2]
	{
		L.T("索拉纳生物。正在为您调取会员档案。", "Solana Biogenics. Retrieving your membership file."),
		L.T("索拉纳。请说。", "Solana. Please, go ahead.")
	};

	public override string FrontDeskDone => L.T("已为您安排，明天专人上门。", "All arranged. Someone will call on you tomorrow.");

	public static int VipFee => Services.Fee("solana", "vip", 300);

	public static int VipGap => Settings.Period("vip", 3);

	public static int LabPeriod => Settings.Period("lab", 7);

	public static int GiftPeriod => Settings.Period("gift", 7);

	public static int VipBudget()
	{
		if (VipBudgetRange(daysUntilVisit: 0) is { } range)
		{
			return Rng.Next(range.Min, range.Max + 1);
		}
		return Settings.VanillaBudget();
	}

	public static string VipBudgetText()
	{
		if (VipBudgetRange(daysUntilVisit: 1) is { } range)
		{
			return $"{range.Min}–{range.Max}₵";
		}
		int visitDay = StoreStation.GetDayCounter() + 1;
		return $"{800 + visitDay}–{1050 + visitDay}₵";
	}

	private static (int Min, int Max)? VipBudgetRange(int daysUntilVisit)
	{
		if (Settings.MaliceMode)
		{
			int visitDay = StoreStation.GetDayCounter() + daysUntilVisit;
			return (MaliceVipBudgetMin + visitDay, MaliceVipBudgetMax + visitDay);
		}
		return Settings.VanillaServices ? null : (VipBudgetMin, VipBudgetMax);
	}

	public static double LabMultiplier(string id)
	{
		if (!Settings.VanillaServices)
		{
			if (!(id == "blue_blood_bag"))
			{
				return 2.0;
			}
			return 3.0;
		}
		return 1.5;
	}
}
