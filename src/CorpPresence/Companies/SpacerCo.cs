using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal sealed partial class SpacerCo : Company
{
	public const string ModBox = "@modbox";

	public const int ModBoxPrice = 500;

	public const int ModBoxMin = 300;

	public const int ModBoxMax = 750;

	public const double RepToTrust = 0.5;

	public static readonly CatalogEntry[] Entries = new CatalogEntry[27]
	{
		new CatalogEntry
		{
			Id = "@handmade_pistol",
			Factory = MakeCatalogGun,
			Page = "军械",
			Label = "“巢火”紧凑型手枪"
		},
		new CatalogEntry
		{
			Id = "@heavy_handmade_pistol",
			Factory = MakeCatalogGun,
			Page = "军械",
			Label = "“铁铸10”手枪"
		},
		new CatalogEntry
		{
			Id = "printed_gun",
			Page = "军械"
		},
		new CatalogEntry
		{
			Id = "small_pistol_ammo",
			Page = "军械"
		},
		new CatalogEntry
		{
			Id = "small_pistol_ammo_p",
			Page = "军械",
			MinTier = 2
		},
		new CatalogEntry
		{
			Id = "small_pistol_ammo_nl",
			Page = "军械"
		},
		new CatalogEntry
		{
			Id = "heavy_pistol_ammo",
			Page = "军械"
		},
		new CatalogEntry
		{
			Id = "heavy_pistol_ammo_p",
			Page = "军械",
			MinTier = 2
		},
		new CatalogEntry
		{
			Id = "heavy_pistol_ammo_nl",
			Page = "军械"
		},
		new CatalogEntry
		{
			Id = ModBox,
			Factory = static _ => null,
			FixedBaseValue = ModBoxPrice,
			FixedListValue = ModBoxPrice,
			Page = "军械",
			Label = "改装盲盒（3–5 件随机配件）",
			LabelKey = "companies.catalog.modGrabBox35RandomAttachments.name",
			LabelEn = "Mod Grab Box (3–5 random attachments)"
		},
		new CatalogEntry
		{
			Id = "fanta_pill",
			Page = "药房"
		},
		new CatalogEntry
		{
			Id = "unlicensed_phagimycin_pill",
			Page = "药房"
		},
		new CatalogEntry
		{
			Id = "caffeine_pill",
			Page = "药房"
		},
		new CatalogEntry
		{
			Id = "salve",
			Page = "药房",
			MinTier = 2
		},
		new CatalogEntry
		{
			Id = "@zhuoyue_battery",
			Page = "药房",
			Label = "桌越能量电池"
		},
		new CatalogEntry
		{
			Id = "processed_milk",
			Page = "码头"
		},
		new CatalogEntry
		{
			Id = "processed_juice",
			Page = "码头"
		},
		new CatalogEntry
		{
			Id = "processed_cheese",
			Page = "码头"
		},
		new CatalogEntry
		{
			Id = "li_eat_snackbar",
			Page = "码头"
		},
		new CatalogEntry
		{
			Id = "energy_drink",
			Page = "码头"
		},
		new CatalogEntry
		{
			Id = "water_ration_small",
			Page = "码头"
		},
		new CatalogEntry
		{
			Id = "water_ration",
			Page = "码头"
		},
		new CatalogEntry
		{
			Id = "large_bottled_water",
			Page = "码头"
		},
		new CatalogEntry
		{
			Id = "water_jug",
			Page = "码头",
			MinTier = 2
		},
		new CatalogEntry
		{
			Id = "@mutual_medkit",
			Page = "阵线",
			MinTier = 2,
			Label = "互助药箱"
		},
		new CatalogEntry
		{
			Id = "@freedom_walkman",
			Page = "阵线",
			MinTier = 2,
			Label = "自由频率随身听"
		},
		new CatalogEntry
		{
			Id = "@anchor_cell",
			Page = "阵线",
			MinTier = 2,
			Label = "锚牌高能电池"
		}
	};

	public static readonly string[] MerchantExtras = new string[2] { "@anchor_cell", "@zhuoyue_battery" };

	private static readonly (string Company, string Tech)[] TechCreditsZh = new (string, string)[5]
	{
		("Ashvin Medical", "冷链凝胶"),
		("A.L.I.S.O.N.", "母带"),
		("桌越电力", "电芯"),
		("社会主义阵线", "分发、电台"),
		("星际客", "仿制、组装")
	};

	private static readonly (string Company, string Tech)[] TechCreditsEn = new (string, string)[5]
	{
		("Ashvin Medical", "cold-chain gel"),
		("A.L.I.S.O.N.", "master tape"),
		("Eninence Power", "cells"),
		("Socialist Front", "distribution, radio"),
		("Spacer", "copying, assembly")
	};

	public const string NoteTag = "CORP_SPACER_NOTE";

	public override string Id => "spacer";

	public override string Name
	{
		get
		{
			if (!Settings.MaliceMode)
			{
				return L.K("companies.spacer.name.spacerAlliance", "星际客联盟", "Spacer Alliance");
			}
			return L.K("companies.spacer.name.merchantUnion", "商人联合", "Merchant Union");
		}
	}

	public override string CharacterId => "mentor";

	public override long PhoneNumber => 77223L;

	public override string[] TierNames => new string[3] { L.K("companies.spacer.tierNames.contactPoint", "联络点", "Contact Point"), L.K("companies.spacer.tierNames.relayStation", "中转站", "Relay Station"), L.K("companies.spacer.tierNames.hub", "枢纽", "Hub") };

	public override string CardFlavor => "";

	public override string DeskName => L.K("companies.spacer.deskName.mentor", "导师", "The Mentor");

	public override string DeskAsk => "";

	public override string[] FrontDesk => new string[1] { L.K("companies.spacer.frontDesk.talk", "说。", "Talk.") };

	public override string FrontDeskDone => L.K("companies.spacer.frontDeskDone.tomorrow", "明天到。", "Tomorrow.");

	protected override string ContentOrderError(CatalogEntry entry) => Settings.MaliceMode && !PageEntries("码头").Contains(entry) ? UnavailableOrderEntry() : null;

	public override CatalogEntry[] Catalog => Entries;

	public static int MutualFee => Services.Fee("spacer", "mutual", 30);

	private static readonly (string Company, string Tech)[] TechCreditKeys = { ("companies.spacer.techCredits.ashvinMedical.company", "companies.spacer.techCredits.ashvinMedical.technology"), ("companies.spacer.techCredits.aLISON.company", "companies.spacer.techCredits.aLISON.technology"), ("companies.spacer.techCredits.eninencePower.company", "companies.spacer.techCredits.eninencePower.technology"), ("companies.spacer.techCredits.socialistFront.company", "companies.spacer.techCredits.socialistFront.technology"), ("companies.spacer.techCredits.spacer.company", "companies.spacer.techCredits.spacer.technology") };

	private static (string Company, string Tech)[] TranslateTechCredits()
	{
		var result = new (string Company, string Tech)[TechCreditsEn.Length];
		for (int i = 0; i < result.Length; i++) result[i] = (L.K(TechCreditKeys[i].Company, TechCreditsZh[i].Company, TechCreditsEn[i].Company), L.K(TechCreditKeys[i].Tech, TechCreditsZh[i].Tech, TechCreditsEn[i].Tech));
		return result;
	}

	public static (string Company, string Tech)[] TechCredits
	{
		get
		{
			if (!L.En && !OfficialTranslations.HasLocale(L.Locale))
			{
				return TechCreditsZh;
			}
			return TextTranslations.HasLocale(L.Locale)
				? TranslateTechCredits()
				: TechCreditsEn;
		}
	}

	public static List<string> RollModBox()
	{
		Dictionary<string, long> dictionary = ModValues();
		List<string> list = new List<string>();
		if (dictionary == null)
		{
			return list;
		}
		Random random = new Random();
		int num = random.Next(3, 6);
		long val = Math.Min(750L, dictionary.Values.Max() * num);
		long num2 = random.Next(300, (int)Math.Max(300L, val) + 1);
		long num3 = 0L;
		for (int i = 0; i < num; i++)
		{
			double want = (double)(num2 - num3) / (double)(num - i);
			List<KeyValuePair<string, long>> list2 = dictionary.OrderBy((KeyValuePair<string, long> kv) => Math.Abs((double)kv.Value - want)).Take(4).ToList();
			KeyValuePair<string, long> keyValuePair = list2[random.Next(list2.Count)];
			list.Add(keyValuePair.Key);
			num3 += keyValuePair.Value;
		}
		ModLog.Info(string.Format("改装盲盒：{0} 件，目标 {1}₵，实际 {2}₵（{3}）", num, num2, num3, string.Join("、", list)));
		return list;
	}

	public static IEnumerable<CatalogEntry> PageEntries(string page)
	{
		if (!Settings.MaliceMode || !(page == "码头"))
		{
			return Entries.Where((CatalogEntry e) => e.Page == page);
		}
		return Entries.Where((CatalogEntry e) => e.Page == "码头" || MerchantExtras.Contains(e.Id));
	}

	private static GameItem MakeCatalogGun(string id) => Goods.Has(id.Substring(1))
		? CatalogEntry.MakeDefault(id) : MakeGun(id.Substring(1));

	public static GameItem MakeGun(string id)
	{
		GameItem gameItem = DirectoryMaster.Item(id, isOwned: false);
		try
		{
			ItemFeatureList.EquipmentCondition condition = ((new Random().Next(2) == 0) ? ItemFeatureList.EquipmentCondition.Fair : ItemFeatureList.EquipmentCondition.Good);
			GunHelper.SetEquipmentCondition(gameItem, condition);
		}
		catch
		{
		}
		return gameItem;
	}

	public static GameItem MakeNote()
	{
		GameItem gameItem = DirectoryMaster.Item("tutorial_page", isOwned: false);
		if (gameItem == null)
		{
			return null;
		}
		try
		{
			IntelHelper.InitTutorialPage(gameItem, "", "77223", L.K("companies.spacer.makeNote.mentor", "——导师", "— The Mentor"));
		}
		catch (Exception ex)
		{
			ModLog.Warn("导师便签写字失败：" + ex.Message);
		}
		try
		{
			gameItem.EnableTag("CORP_SPACER_NOTE");
		}
		catch
		{
		}
		return gameItem;
	}

	public static void OnMentorChapter(int chapter)
	{
		CompanyState companyState = Companies.StateOf(Companies.Find("spacer"));
		if (companyState != null)
		{
			int num;
			if (chapter >= 3)
			{
				num = 80;
			}
			else
			{
				num = ((chapter == 2) ? 50 : 20);
			}
			if (companyState.Coop < num)
			{
				companyState.Coop = num;
				ModLog.Info($"星际客联盟信任度 → {num}（导师第 {chapter} 章）");
			}
		}
	}

	public static void OnDayStart()
	{
		Company c = Companies.Find("spacer");
		CompanyState companyState = Companies.StateOf(c);
		if (companyState != null)
		{
			if (companyState.RepMalice != Settings.MaliceMode)
			{
				companyState.RepMalice = Settings.MaliceMode;
				companyState.RepSum = -1;
			}
			int num = 0;
			string[] trustFactions = Mentor.TrustFactions;
			foreach (string faction in trustFactions)
			{
				num += Math.Max(0, Engine.Reputation(faction));
			}
			if (companyState.RepSum >= 0 && num > companyState.RepSum)
			{
				Services.AddCoop(c, (double)(num - companyState.RepSum) * 0.5, Settings.MaliceMode ? "黑市声望上涨" : "黑市、革命军、下层区声望上涨");
			}
			companyState.RepSum = num;
		}
	}
}
