using System;
using System.Collections.Generic;
using CorpPresence.Malice;
using CorpPresence.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CorpPresence;

internal static class Goods
{
	internal sealed class Def
	{
		public string Name;

		public string Flavor;

		public string Desc;

		public string Company;

		public string Art;

		public string Manufacturer;

		public string NameEn;

		public string FlavorEn;

		public string DescEn;

		public string[] Types;

		public string Shape;

		public long Value;

		public int W;

		public int H;

		public bool Usable;

		public int[] Gun;

		public int[] Power;

		public int Durability;

		public bool VanillaUse;

		public string Fallback;

		public bool Crate;

		public Func<GameItem> Shell;

		public Action<GameItem> Setup;

		public Action<GameItem> OnNew;

		public string LName => L.T(Name, NameEn ?? Name);

		public string LFlavor => L.T(Flavor, FlavorEn ?? Flavor);

		public string LDesc
		{
			get
			{
				if (Desc != null)
				{
					return L.T(Desc, DescEn ?? Desc);
				}
				return LFlavor;
			}
		}
	}

	public const string Prefix = "corp_med_";

	private const string RefSprite = "injector_pink_alt";

	private static readonly Dictionary<string, Def> Defs;

	private static readonly Dictionary<string, Def> Guns;

	private static readonly Dictionary<string, Def> Spacer;

	private const string CandleLit = "candle_lit";

	private static readonly Dictionary<string, (string Company, string Art)> ExtraArt;

	private static readonly Dictionary<string, Sprite> Sprites;

	private static readonly Dictionary<string, int> NativeWidth;

	private static readonly Dictionary<string, Sprite> VanillaCache;

	private static readonly Dictionary<string, float> VanillaMissAt;

	private static readonly HashSet<StoreClient> Boosted;

	private static int _boostDay;

	private static int Day => StoreStation.GetDayCounter();

	private static MedState M => CorpState.Get()?.Meds;

	public static int ExtraScavAttempts => Math.Min(M?.StaminaShots ?? 0, Settings.VanillaServices ? 2 : int.MaxValue);

	public static bool PainBlockedToday
	{
		get
		{
			if (M != null)
			{
				return M.PainBlockDay == Day;
			}
			return false;
		}
	}

	public static bool PatchActive
	{
		get
		{
			if (M != null)
			{
				return Day <= M.PatchUntil;
			}
			return false;
		}
	}

	public static bool ElixirActive
	{
		get
		{
			if (M != null)
			{
				return Day <= M.ElixirUntil;
			}
			return false;
		}
	}

	public static bool CandleActive
	{
		get
		{
			if (M != null)
			{
				return Day <= M.CandleUntil;
			}
			return false;
		}
	}

	static Goods()
	{
		Defs = new Dictionary<string, Def>
		{
			["stamina"] = new Def
			{
				Name = "体力增强剂",
				NameEn = "Stamina Booster",
				Value = 1500L,
				Company = "carmicheal",
				Art = "stamina_booster",
				Fallback = "injector_pink_alt",
				Shell = CreateMedicineShell,
				Manufacturer = "carmicheal",
				Types = new string[1] { "MEDICAL" },
				W = 4,
				H = 1,
				Usable = true,
				Flavor = "一种长效体能改善药剂，能使人体质略微改善。卡迈克尔尚未上市的新品，只向部门经理级别的内部人员开放申请通道。注射后，每天在垃圾场的拾荒次数上限永久 +1（设置“服务强度”为原版时累计最多 +2）。",
				FlavorEn = "A long-acting performance drug that slightly improves your constitution. An unreleased Carmicheal product; only insiders at department-manager level can apply for it. After injecting, your daily scavenging attempts in the Dumping Grounds permanently increase by 1 (at most +2 in total when Service strength is set to Vanilla)."
			},
			["painblock"] = new Def
			{
				Name = "痛觉屏蔽剂",
				NameEn = "Pain Blocker",
				Value = 600L,
				Company = "carmicheal",
				Art = "pain_blocker",
				Fallback = "injector_pink_alt",
				Shell = CreateMedicineShell,
				Manufacturer = "carmicheal",
				Types = new string[1] { "MEDICAL" },
				W = 4,
				H = 1,
				Usable = true,
				Flavor = "卡迈克尔研发部的新品，脱胎于一种戒瘾疗法。可以暂时屏蔽痛觉。注射后，当天拾荒受伤的几率减半。",
				FlavorEn = "A new product from Carmicheal R&D, born out of an addiction treatment. It temporarily blocks pain. After injecting, your chance of getting hurt while scavenging is halved for the day."
			},
			["gloves"] = new Def
			{
				Name = "韧棉工装手套",
				NameEn = "Kotton Work Gloves",
				Value = 400L,
				Company = "pastel",
				Art = "pastel_gloves",
				Manufacturer = "pastel",
				Types = new string[1] { "TOOL" },
				W = 2,
				H = 2,
				Flavor = "韧棉织的厚手套，是工程人员的好帮手。夜里带着它去垃圾场，拾荒受轻伤的几率降低 30%。",
				FlavorEn = "Thick Kotton gloves, an engineer's best friend. Take them to the Dumping Grounds at night and your chance of minor injury while scavenging drops by 30%."
			},
			["patch"] = new Def
			{
				Name = "海明威戒烟贴",
				NameEn = "Hemingway Quit Patch",
				Value = 120L,
				Company = "pastel",
				Art = "hemingway_patch",
				Manufacturer = "pastel",
				Types = new string[1] { "MEDICAL" },
				W = 1,
				H = 2,
				Usable = true,
				Flavor = "海明威出品。贴一片，3 天内不受尼古丁戒断的交易惩罚。",
				FlavorEn = "Made by Hemingway. Apply one and you take no nicotine withdrawal trading penalty for 3 days."
			},
			["sign"] = new Def
			{
				Name = "帕斯泰尔特约店灯牌",
				NameEn = "pastel Partner Store Sign",
				Value = 300L,
				Company = "pastel",
				Art = "pastel_sign",
				Manufacturer = "pastel",
				Types = Array.Empty<string>(),
				W = 1,
				H = 1,
				Flavor = "一块小小的营养果灯牌：“帕斯泰尔特约店”。手里有帕斯泰尔货的人，看到这个就知道手里货物的归属在哪。放进展示柜，每天会有一位卖帕斯泰尔货的人上门。",
				FlavorEn = "A small Nutrifruit light-up sign: \"pastel Partner Outlet\". Anyone holding pastel goods sees it and knows where they belong. Put it in a display case and a pastel goods seller will come by every day."
			},
			["elixir"] = new Def
			{
				Name = "名流精华",
				NameEn = "Socialite Essence",
				Value = 900L,
				Company = "solana",
				Art = "solana_elixir",
				Manufacturer = "solana",
				Types = new string[1] { "LUXURY_ITEM" },
				W = 1,
				H = 2,
				Usable = true,
				Flavor = "涂上它，人人都会觉得你是上层区来的“贵族”。使用后 3 天内，交易得到的声望 +50%。",
				FlavorEn = "Put it on and everyone will take you for Upper Levels \"nobility\". For 3 days after use, reputation gained from trades +50%."
			},
			["mask"] = new Def
			{
				Name = "永恒·夜间修护面膜",
				NameEn = "Eternal Night Repair Mask",
				Value = 450L,
				Company = "solana",
				Art = "solana_mask",
				Manufacturer = "solana",
				Types = new string[1] { "LUXURY_ITEM" },
				W = 2,
				H = 1,
				Usable = true,
				Flavor = "敷上后，下一次夜间外出，第二天不会开店迟到、错过顾客。",
				FlavorEn = "Put it on, and after your next night out you won't open late or miss customers the next day."
			},
			["candle"] = new Def
			{
				Name = "索拉纳香薰蜡烛",
				NameEn = "Solana Scented Candle",
				Value = 800L,
				Company = "solana",
				Art = "solana_candle",
				Manufacturer = "solana",
				Types = new string[1] { "LUXURY_ITEM" },
				W = 2,
				H = 2,
				Usable = true,
				Shape = "##|##",
				Flavor = "闻起来像上层区的大堂。来这儿的有钱人会觉得宾至如归，钱包的扣子也松一点。点燃后 3 天内，上层区顾客的预算 +15%，烧完就没了。",
				FlavorEn = "Smells like an Upper Levels lobby. Rich visitors feel right at home, and their purse strings loosen a little. For 3 days after lighting, Upper Levels customers' budget +15%. Gone once it burns out."
			}
		};
		Guns = new Dictionary<string, Def>
		{
			["rosen_revolver"] = new Def
			{
				Name = "罗森手作左轮",
				NameEn = "Rosen's Handmade Revolver",
				Value = 400L,
				Company = "meridian",
				Art = "rosen_revolver",
				Types = new string[3] { "WEAPON", "FIREARM", "CONTRABAND" },
				W = 4,
				H = 3,
				Shape = "####|###.|#...",
				Gun = new int[7] { 55, 85, 80, 40, 60, 65, 45 },
				Desc = "罗森手工打造的左轮手枪，.45 口径。",
				DescEn = "A revolver handcrafted by Rosen. .45 caliber.",
				Flavor = "没有编号，没有商标。握把内侧刻着一个 R。",
				FlavorEn = "No serial number, no trademark. An R is carved inside the grip."
			},
			["rosen_rifle"] = new Def
			{
				Name = "罗森手作步枪",
				NameEn = "Rosen's Handmade Rifle",
				Value = 650L,
				Company = "meridian",
				Art = "rosen_rifle",
				Types = new string[4] { "WEAPON", "FIREARM", "CONTRABAND", "MODDABLE" },
				W = 8,
				H = 2,
				Shape = "########|########",
				Gun = new int[7] { 75, 80, 80, 35, 45, 90, 10 },
				Desc = "罗森手工打造的步枪，10 毫米口径。",
				DescEn = "A rifle handcrafted by Rosen. 10mm caliber.",
				Flavor = "子午线的产线做不出这种枪",
				FlavorEn = "Meridian's line can't make a gun like this."
			}
		};
		Spacer = new Dictionary<string, Def>
		{
			["zhuoyue_battery"] = new Def
			{
				Name = "桌越能量电池",
				NameEn = "Eninence Energy Cell",
				Value = 25L,
				Company = "spacer",
				Art = "zhuoyue_battery",
				Fallback = "battery100",
				Manufacturer = "eninence",
				Types = new string[1] { "ENERGY_CELL" },
				W = 1,
				H = 2,
				Power = new int[3] { 80, 10, 0 },
				Desc = "可充电的能量电池，出厂不带电。电量上限是正品的八成。",
				DescEn = "A rechargeable energy cell, shipped empty. Holds 80% of the genuine article's charge.",
				Flavor = "桌越电力，品质不值得保证",
				FlavorEn = "Eninence Power. Quality not guaranteed."
			},
			["mutual_medkit"] = new Def
			{
				Name = "互助药箱",
				NameEn = "Mutual Aid Medkit",
				Value = 180L,
				Company = "spacer",
				Art = "mutual_medkit",
				Fallback = "medical_pouch",
				Manufacturer = "spacer",
				Types = new string[1] { "MEDICAL" },
				W = 2,
				H = 2,
				Desc = "一箱常用药。Ashvin 的冷链凝胶，星际客仿制，阵线分发。",
				DescEn = "A box of everyday medicine. Ashvin's cold-chain gel, copied by Spacers, handed out by the Front.",
				Flavor = "没有商标，上面只画着一只锚。",
				FlavorEn = "No trademark. Just an anchor drawn on the lid."
			},
			["anchor_cell"] = new Def
			{
				Name = "锚牌高能电池",
				NameEn = "Anchor High-Capacity Cell",
				Value = 260L,
				Company = "spacer",
				Art = "anchor_cell",
				Fallback = "battery100",
				Manufacturer = "spacer",
				Types = new string[2] { "ENERGY_CELL", "CONTRABAND" },
				W = 1,
				H = 2,
				Power = new int[2] { 500, 10 },
				Setup = (GameItem item) =>
				{
					PowerHelper.InitDisposable(item);
					ContrabandHelper.InitContrabandItem(item);
				},
				Desc = "一次性能量电池。电量是 2 型电池的两倍。不能充电。",
				DescEn = "A single-use energy cell. Twice the charge of a Mk. II cell. Cannot be recharged.",
				Flavor = "桌越提供的电芯，加上一些下层人民的智慧。",
				FlavorEn = "Eninence cells, plus a little Lower Levels ingenuity."
			},
			["freedom_walkman"] = new Def
			{
				Name = "自由频率随身听",
				NameEn = "Free Frequency Walkman",
				Value = 300L,
				Company = "spacer",
				Art = "freedom_walkman",
				Fallback = "cassette10",
				Manufacturer = "spacer",
				Types = new string[1] { "LUXURY_ITEM" },
				W = 2,
				H = 2,
				Desc = "一台随身听，里面只有一盘带子。A.L.I.S.O.N. 的母带，桌越的电芯，星际客组装。",
				DescEn = "A walkman with one tape inside. A.L.I.S.O.N. master tape, Eninence cells, Spacer-assembled.",
				Flavor = "七张专辑的间奏剪在一起。没有曲目表。",
				FlavorEn = "The interludes from seven albums, spliced together. No tracklist."
			},
			["meridian_cert"] = new Def
			{
				Name = "子午线授权经销证书",
				NameEn = "Meridian Authorized Dealer Certificate",
				Value = 50L,
				Company = "meridian",
				Art = "meridian_cert",
				Fallback = "cert1",
				Manufacturer = "meridian",
				Types = new string[1] { "DOCUMENT" },
				W = 3,
				H = 2,
				Desc = "子午线已经停发的旧证书，没有任何效果。枪械是否合法以武器许可证为准。",
				DescEn = "An old certificate Meridian no longer issues. It has no effect. Whether a firearm is legal depends on your weapon permits.",
				Flavor = "子午线兵工颁发给合作店铺的授权证书。黑底红字，盖着经线钢印，落款是驻站负责人罗森。",
				FlavorEn = "An authorization Meridian Ballistics issued to partner shops. Red on black, stamped with the meridian seal, signed by Rosen, the station manager."
			},
			["leviathan"] = new Def
			{
				Name = "利维坦净水器",
				NameEn = "Leviathan Water Purifier",
				Value = 180L,
				Company = "davre",
				Art = "leviathan",
				Fallback = "portable_water_purifier",
				Manufacturer = "davre",
				Types = new string[2] { "HOUSEHOLD_GOOD", "WATER_PURIFICATION_SUPPLY" },
				W = 2,
				H = 2,
				Durability = 600,
				VanillaUse = true,
				Desc = "对水容器使用，将铁锈水或幽灵水提升至基础水级别。每净化 100 毫升消耗 1 点耐久度。",
				DescEn = "Use on a water container to bring Rustwater or Ghostwater up to Basewater purity. It will use 1 durability per 100ml.",
				Flavor = "达夫雷照着海德拉做的。沟渠水还对付不了，但胜在耐用。",
				FlavorEn = "Davre modeled it on the HydraTech. It can't handle Gutterflow yet, but it lasts."
			},
			["crate_pastel"] = new Def
			{
				Name = "帕斯泰尔送货箱",
				NameEn = "pastel Delivery Crate",
				Value = 0L,
				Company = "pastel",
				Art = "crate",
				Fallback = "ammo_box",
				Types = Array.Empty<string>(),
				W = 3,
				H = 2,
				Crate = true,
				Desc = "电话订的货装在里面。只能取、不能放，取空后箱子自动消失。",
				DescEn = "Your phone order is inside. Take items out; nothing can be put in. The crate disappears once empty.",
				Flavor = "送货员留下的箱子。",
				FlavorEn = "A crate the courier left behind."
			},
			["crate_davre"] = new Def
			{
				Name = "达夫雷送货箱",
				NameEn = "Davre Delivery Crate",
				Value = 0L,
				Company = "davre",
				Art = "crate",
				Fallback = "ammo_box",
				Types = Array.Empty<string>(),
				W = 3,
				H = 2,
				Crate = true,
				Desc = "电话订的货装在里面。只能取、不能放，取空后箱子自动消失。",
				DescEn = "Your phone order is inside. Take items out; nothing can be put in. The crate disappears once empty.",
				Flavor = "送货员留下的箱子。",
				FlavorEn = "A crate the courier left behind."
			},
			["crate_solana"] = new Def
			{
				Name = "索拉纳送货箱",
				NameEn = "Solana Delivery Crate",
				Value = 0L,
				Company = "solana",
				Art = "crate",
				Fallback = "ammo_box",
				Types = Array.Empty<string>(),
				W = 3,
				H = 2,
				Crate = true,
				Desc = "电话订的货装在里面。只能取、不能放，取空后箱子自动消失。",
				DescEn = "Your phone order is inside. Take items out; nothing can be put in. The crate disappears once empty.",
				Flavor = "送货员留下的箱子。",
				FlavorEn = "A crate the courier left behind."
			},
			["crate_carmicheal"] = new Def
			{
				Name = "卡迈克尔送货箱",
				NameEn = "Carmicheal Delivery Crate",
				Value = 0L,
				Company = "carmicheal",
				Art = "crate",
				Fallback = "ammo_box",
				Types = Array.Empty<string>(),
				W = 3,
				H = 2,
				Crate = true,
				Desc = "电话订的货装在里面。只能取、不能放，取空后箱子自动消失。",
				DescEn = "Your phone order is inside. Take items out; nothing can be put in. The crate disappears once empty.",
				Flavor = "送货员留下的箱子。",
				FlavorEn = "A crate the courier left behind."
			},
			["crate_meridian"] = new Def
			{
				Name = "子午线送货箱",
				NameEn = "Meridian Delivery Crate",
				Value = 0L,
				Company = "meridian",
				Art = "crate",
				Fallback = "ammo_box",
				Types = Array.Empty<string>(),
				W = 3,
				H = 2,
				Crate = true,
				Desc = "电话订的货装在里面。只能取、不能放，取空后箱子自动消失。",
				DescEn = "Your phone order is inside. Take items out; nothing can be put in. The crate disappears once empty.",
				Flavor = "送货员留下的箱子。",
				FlavorEn = "A crate the courier left behind."
			},
			["crate_jackson_private"] = new Def
			{
				Name = "杰克逊博士送货箱",
				NameEn = "Dr. Jackson's Delivery Crate",
				Value = 0L,
				Company = "jackson_private",
				Art = "crate",
				Fallback = "ammo_box",
				Types = Array.Empty<string>(),
				W = 3,
				H = 2,
				Crate = true,
				Desc = "杰克逊工作间寄来的机器与改装配件。只能取、不能放，取空后箱子自动消失。",
				DescEn = "Machines and custom parts from Jackson's workshop. Take items out; nothing can be put in. The crate disappears once empty.",
				Flavor = "箱面印着齿轮标记。博士做的东西，也由他自己调试。",
				FlavorEn = "A gear marks the front. Jackson builds his own equipment and tunes it himself."
			},
			["crate_spacer"] = new Def
			{
				Name = "星际客联盟货箱",
				NameEn = "Spacer Alliance Crate",
				Value = 0L,
				Company = "spacer",
				Art = "crate",
				Fallback = "ammo_box",
				Types = Array.Empty<string>(),
				W = 3,
				H = 2,
				Crate = true,
				Desc = "电话订的货装在里面。只能取、不能放，取空后箱子自动消失。",
				DescEn = "Your phone order is inside. Take items out; nothing can be put in. The crate disappears once empty.",
				Flavor = "送货员留下的箱子。",
				FlavorEn = "A crate the courier left behind."
			}
		};
		ExtraArt = new Dictionary<string, (string, string)>
		{
			["rat_wheel_generator_occupied"] = ("husbandry", "rat_wheel_generator_retrofit_occupied"),
			["candle_lit"] = ("solana", "solana_candle_lit"),
			["hemingway_limited"] = ("pastel", "hemingway_limited"),
			["sb_stage_small"] = ("carmicheal", "silverblood_small"),
			["sb_stage"] = ("carmicheal", "silverblood"),
			["sb_stage_large"] = ("carmicheal", "silverblood_large")
		};
		Sprites = new Dictionary<string, Sprite>();
		NativeWidth = new Dictionary<string, int>
		{
			["sb_stage_small"] = 48,
			["sb_stage"] = 64,
			["sb_stage_large"] = 80
		};
		VanillaCache = new Dictionary<string, Sprite>();
		VanillaMissAt = new Dictionary<string, float>();
		Boosted = new HashSet<StoreClient>();
		_boostDay = -1;
		foreach (KeyValuePair<string, Def> gun in Guns)
		{
			Defs[gun.Key] = gun.Value;
		}
		foreach (KeyValuePair<string, Def> item in Spacer)
		{
			Defs[item.Key] = item.Value;
		}
		foreach (KeyValuePair<string, Def> def in Tobacco.Defs)
		{
			Defs[def.Key] = def.Value;
		}
		foreach (KeyValuePair<string, Def> def2 in Canning.Defs)
		{
			Defs[def2.Key] = def2.Value;
		}
		Defs[RatWheelGenerator.GoodsKey] = RatWheelGenerator.Definition();
		foreach (KeyValuePair<string, Def> def3 in Forge.Defs)
		{
			Defs[def3.Key] = def3.Value;
		}
		foreach (KeyValuePair<string, Def> def4 in SilverBlood.Defs)
		{
			Defs[def4.Key] = def4.Value;
		}
		foreach (KeyValuePair<string, Def> def5 in MaliceItems.Defs)
		{
			Defs[def5.Key] = def5.Value;
		}
	}

	public static bool Has(string id)
	{
		return Defs.ContainsKey(id);
	}

	private static void ApplyText(GameItem item, Def d)
	{
		try
		{
			item.SetName(d.LName);
		}
		catch
		{
		}
		try
		{
			item.flavorText = d.Desc == null ? "" : d.LFlavor ?? "";
		}
		catch
		{
		}
		try
		{
			item.shortDescription = d.LDesc + MachineUpgrade.Status(item);
		}
		catch
		{
		}
	}

	public static void Relabel(GameItem item, string goodsId)
	{
		if (item != null && goodsId != null && Defs.TryGetValue(goodsId, out var value))
		{
			ApplyText(item, value);
		}
	}

	public static long ValueOf(string id)
	{
		if (id == null || !Defs.TryGetValue(id, out var value))
		{
			return 0L;
		}
		return value.Value;
	}

	public static string[] TypesOf(string id)
	{
		if (id == null || !Defs.TryGetValue(id, out var value))
		{
			return null;
		}
		return value.Types;
	}

	public static bool VanillaUse(GameItem item)
	{
		string text = IdOf(item);
		if (text != null && Defs.TryGetValue(text, out var value))
		{
			return value.VanillaUse;
		}
		return false;
	}

	public static GameItem MakeSpecial(string id)
	{
		if (id == null || !id.StartsWith("@") || !Has(id.Substring(1)))
		{
			return null;
		}
		return Make(id.Substring(1));
	}

	public static GameItem Make(string id)
	{
		ItemIdentity.Entry entry = ItemIdentity.ForGoods(id);
		GameItem gameItem = (((object)entry != null && entry.Goods != null) ? ItemRegistry.Create(entry.Id) : null);
		if (gameItem != null && Defs.TryGetValue(id, out var value) && value.OnNew != null)
		{
			try
			{
				value.OnNew(gameItem);
			}
			catch (Exception ex)
			{
				ModLog.Warn(value.Name + "出厂设置失败：" + ex.Message);
			}
		}
		return gameItem;
	}

	internal static GameItem Build(ItemIdentity.Entry entry)
	{
		if ((object)entry == null || entry.Goods == null || !Defs.TryGetValue(entry.Goods, out var value))
		{
			return null;
		}
		GameItem gameItem;
		if (value.Crate)
		{
			gameItem = Crates.CreateShell();
		}
		else
		{
			gameItem = ((value.Shell != null) ? value.Shell() : DirectoryMaster.Item(entry.LegacyId, isOwned: false));
		}
		if (gameItem == null)
		{
			return null;
		}
		gameItem.identifier = entry.Id;
		ApplyText(gameItem, value);
		try
		{
			gameItem.SetValue(value.Value);
		}
		catch (Exception ex)
		{
			ModLog.Warn(value.Name + "定价失败：" + ex.Message);
		}
		ApplyAppearance(gameItem, entry, value);
		try
		{
			ItemTags.Apply(gameItem, value.Types, Array.Empty<string>());
		}
		catch
		{
		}
		if (value.Manufacturer != null)
		{
			try
			{
				gameItem.EnableManufacturer(value.Manufacturer);
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				gameItem.DisableTag("manufacturer");
			}
			catch
			{
			}
		}
		if (value.Power != null)
		{
			try
			{
				bool flag = gameItem.IsTag("power_source_item");
				Func<GameItem, GameItem, bool> mayThisTargetItemFunc = gameItem.mayThisTargetItemFunc;
				Func<GameItem, GameItem, bool> canThisTargetItemFunc = gameItem.canThisTargetItemFunc;
				Action<GameItem, GameItem> onThisTargetItemFunc = gameItem.onThisTargetItemFunc;
				PowerHelper.InitPowerSourceItem(gameItem, value.Power[0], value.Power[1]);
				if (flag)
				{
					gameItem.mayThisTargetItemFunc = mayThisTargetItemFunc;
					gameItem.canThisTargetItemFunc = canThisTargetItemFunc;
					gameItem.onThisTargetItemFunc = onThisTargetItemFunc;
				}
				if (value.Power.Length > 2)
				{
					PowerHelper.SetPowerSourceAt(gameItem, value.Power[2]);
				}
				gameItem.SetValue(value.Value);
				long num = gameItem.GetCurrentValue(useRetailMarkup: false, withChild: true, includeEvents: false, forceMarkup: false, 0L) - value.Value;
				if (num > 0)
				{
					gameItem.SetValue(Math.Max(1L, value.Value - num));
				}
				RefreshBattery(gameItem);
			}
			catch (Exception ex4)
			{
				ModLog.Warn(value.Name + "设置电量失败：" + ex4.Message);
			}
		}
		if (value.Durability > 0)
		{
			try
			{
				DurabilityHelper.SetDurability(gameItem, value.Durability, value.Durability, value.Durability);
			}
			catch (Exception ex5)
			{
				ModLog.Warn(value.Name + "设置耐久失败：" + ex5.Message);
			}
		}
		if (value.Gun != null)
		{
			try
			{
				int[] gun = value.Gun;
				GunHelper.InitGun(gameItem, gun[0], gun[1], gun[2], gun[3], gun[4], gun[5], gun[6], 70, Array.IndexOf(value.Types, "MODDABLE") >= 0);
				if (entry.Goods == "rosen_rifle")
				{
					RifleMods.Attach(gameItem);
				}
				GunHelper.SetEquipmentCondition(gameItem, ItemFeatureList.EquipmentCondition.New);
				gameItem.SetValue(value.Value);
			}
			catch (Exception ex6)
			{
				ModLog.Warn(value.Name + "设置枪械属性失败：" + ex6.Message);
			}
		}
		if (value.Setup != null)
		{
			try
			{
				value.Setup(gameItem);
			}
			catch (Exception ex7)
			{
				ModLog.Warn(value.Name + "初始化失败：" + ex7.Message);
			}
		}
		return gameItem;
	}

	private static GameItem CreateMedicineShell()
	{
		var item = ItemDirectory.CreateEmptyItem();
		item.EnableTag("injector");
		item.EnableTag("ITEM_MEDICAL_TAG");
		item.EnableProficiency("MEDICAL_PROFICIENCY");
		AudioHelper.InitInjectorAudio(item);
		return item;
	}

	private static void ApplyAppearance(GameItem gameItem, ItemIdentity.Entry entry, Def value)
	{
		// InitFishItem and the native load/growth callbacks own fish sprites and shapes.
		if (entry.Id == SilverBlood.CreatureId) return;
		try
		{
			gameItem.SetSprite("", entry.LegacySprite);
		}
		catch (Exception ex2)
		{
			ModLog.Warn(value.Name + "换图失败：" + ex2.Message);
		}
		try
		{
			gameItem.shape = ShapeOf(value);
			gameItem.modifiedShape = gameItem.shape.Clone();
		}
		catch (Exception ex3)
		{
			ModLog.Warn(value.Name + "改形状失败：" + ex3.Message);
		}
	}

	public static string IdOf(GameItem item)
	{
		try
		{
			return (item == null) ? null : ItemIdentity.GoodsKey(item.identifier, item.spritePath);
		}
		catch
		{
			return null;
		}
	}

	public static bool IsLitCandle(GameItem item)
	{
		try
		{
			return item != null && ItemIdentity.ResolveId(item.identifier, item.spritePath) == "corp_presence_candle_lit";
		}
		catch
		{
			return false;
		}
	}

	public static bool BeforeBuildShape(string spritePath, ref GridShapeBuilder __result)
	{
		try
		{
			if (spritePath == null || !spritePath.StartsWith("corp_med_", StringComparison.Ordinal))
			{
				return true;
			}
			string text = spritePath.Substring("corp_med_".Length);
			// State variants must keep the explicit machine footprint, not infer it from alpha.
			if (text == RatWheelGenerator.GoodsKey + "_occupied") text = RatWheelGenerator.GoodsKey;
			if (Defs.TryGetValue(text, out var value) || BatteryState(text, out value, out var _))
			{
				__result = ShapeBuilderOf(value);
				return false;
			}
			if (!ExtraArt.TryGetValue(text, out (string, string) value2))
			{
				return true;
			}
			Texture2D texture2D = Tex.Art(value2.Item1, value2.Item2);
			if (texture2D == null)
			{
				return true;
			}
			int num;
			if (NativeWidth.TryGetValue(text, out var value3))
			{
				num = value3;
			}
			else
			{
				num = ((text == "candle_lit" && Defs.TryGetValue("candle", out var value4)) ? (value4.W * 16) : texture2D.width);
			}
			int num2 = Math.Max(1, num / 16);
			int num3 = Math.Max(1, texture2D.width / num2);
			int h = Math.Max(1, texture2D.height / num3);
			__result = new GridShapeBuilder().SetData(Coverage(texture2D, num2, h), num2);
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static void AfterRaycastValid(Image __instance, Vector2 screenPoint, Camera eventCamera, ref bool __result)
	{
		try
		{
			// Most UI graphics already accept their full rectangle; avoid owner lookups.
			if (__instance.alphaHitTestMinimumThreshold <= 0f) return;
			// Only the actual inventory image of one of our items uses occupied cells.
			// Local-space conversion already undoes the parent rotation/flip.
			TreeNodeRender node = __instance.GetComponent<TreeNodeRender>();
			if (node != null && node.FindParentElement() is GameItemElement item
				&& item.background != null && item.background.image == __instance
				&& ItemIdentity.Find(item.identifier, item.spritePath) != null)
			{
				if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(__instance.rectTransform, screenPoint, eventCamera, out var point))
				{
					__result = false;
					return;
				}
				Rect rect = __instance.GetPixelAdjustedRect();
				GridShape shape = item.shape;
				__result = rect.width > 0f && rect.height > 0f && shape != null
					&& ItemHitArea.Cell((point.x - rect.x) / rect.width, (point.y - rect.y) / rect.height,
						shape.width, shape.height, out int x, out int y) && shape.GetLocal(x, y) != 0;
				return;
			}
			Sprite sprite = __instance.sprite;
			if (sprite == null || __instance.alphaHitTestMinimumThreshold <= 0f || sprite.name == null || !sprite.name.StartsWith("corp_med_", StringComparison.Ordinal))
			{
				return;
			}
			Canvas canvas = __instance.canvas;
			float num = ((canvas != null) ? canvas.referencePixelsPerUnit : 100f);
			if (sprite.pixelsPerUnit <= num * 1.01f)
			{
				return;
			}
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(__instance.rectTransform, screenPoint, eventCamera, out var localPoint))
			{
				__result = false;
				return;
			}
			Rect pixelAdjustedRect = __instance.GetPixelAdjustedRect();
			float num2 = (localPoint.x - pixelAdjustedRect.x) / pixelAdjustedRect.width;
			float num3 = (localPoint.y - pixelAdjustedRect.y) / pixelAdjustedRect.height;
			if (num2 < 0f || num2 > 1f || num3 < 0f || num3 > 1f)
			{
				__result = false;
				return;
			}
			Rect textureRect = sprite.textureRect;
			__result = sprite.texture.GetPixelBilinear((textureRect.x + num2 * textureRect.width) / (float)sprite.texture.width, (textureRect.y + num3 * textureRect.height) / (float)sprite.texture.height).a >= __instance.alphaHitTestMinimumThreshold;
		}
		catch
		{
		}
	}

	private static byte[] Coverage(Texture2D tex, int w, int h)
	{
		int num = tex.width / w;
		Color32[] pixels = tex.GetPixels32();
		byte[] array = new byte[w * h];
		for (int i = 0; i < h; i++)
		{
			for (int j = 0; j < w; j++)
			{
				int num2 = 0;
				int num3 = (h - 1 - i) * num;
				int num4 = j * num;
				for (int k = 0; k < num; k++)
				{
					for (int l = 0; l < num; l++)
					{
						if (pixels[(num3 + k) * tex.width + num4 + l].a > 128)
						{
							num2++;
						}
					}
				}
				array[i * w + j] = ((num2 * 50 >= num * num) ? ((byte)1) : ((byte)0));
			}
		}
		return array;
	}

	private static GridShape ShapeOf(Def d)
	{
		return ShapeBuilderOf(d).Build();
	}

	private static GridShapeBuilder ShapeBuilderOf(Def d)
	{
		if (d.Shape != null)
		{
			string[] array = d.Shape.Split('|');
			byte[] array2 = new byte[d.W * d.H];
			for (int i = 0; i < d.H; i++)
			{
				for (int j = 0; j < d.W; j++)
				{
					array2[i * d.W + j] = (byte)((i < array.Length && j < array[i].Length && array[i][j] == '#') ? 1u : 0u);
				}
			}
			return new GridShapeBuilder().SetData(array2, d.W);
		}
		Texture2D texture2D = ((d.Art != null) ? Tex.Art(d.Company, d.Art) : null);
		if (texture2D == null || texture2D.width % d.W != 0 || texture2D.height % d.H != 0 || texture2D.width / d.W != texture2D.height / d.H || texture2D.width / d.W < 16)
		{
			return new GridShapeBuilder(d.W, d.H).SetDataFill(d.W, d.H, 1);
		}
		int num = texture2D.width / d.W;
		Color32[] pixels = texture2D.GetPixels32();
		byte[] array3 = new byte[d.W * d.H];
		for (int k = 0; k < d.H; k++)
		{
			for (int l = 0; l < d.W; l++)
			{
				int num2 = 0;
				int num3 = (d.H - 1 - k) * num;
				int num4 = l * num;
				for (int m = 0; m < num; m++)
				{
					for (int n = 0; n < num; n++)
					{
						if (pixels[(num3 + m) * texture2D.width + num4 + n].a > 128)
						{
							num2++;
						}
					}
				}
				array3[k * d.W + l] = ((num2 * 4 >= num * num) ? ((byte)1) : ((byte)0));
			}
		}
		return new GridShapeBuilder().SetData(array3, d.W);
	}

	private static float HdScale(Def d, Texture2D tex)
	{
		if (d == null || d.W <= 0 || tex.width <= d.W * 16)
		{
			return 1f;
		}
		return (float)tex.width / ((float)d.W * 16f);
	}

	public static Sprite FindSprite(string name)
	{
		if (name == "corp_presence_tobacco_leaf_p")
		{
			return Tobacco.PlantOverlay();
		}
		if (string.IsNullOrEmpty(name) || !name.StartsWith("corp_med_", StringComparison.Ordinal))
		{
			return null;
		}
		if (Sprites.TryGetValue(name, out var value) && value != null)
		{
			return value;
		}
		string text = name.Substring("corp_med_".Length);
		string company;
		string name2;
		(string, string) value3;
		if (Defs.TryGetValue(text, out var value2))
		{
			company = value2.Company;
			name2 = value2.Art;
		}
		else if (ExtraArt.TryGetValue(text, out value3))
		{
			(company, name2) = value3;
			if (text == "candle_lit")
			{
				Defs.TryGetValue("candle", out value2);
			}
		}
		else
		{
			if (!BatteryState(text, out var d, out var suffix))
			{
				return null;
			}
			company = d.Company;
			name2 = d.Art + suffix;
			value2 = d;
		}
		Texture2D texture2D = Tex.Art(company, name2);
		if (texture2D == null)
		{
			if (value2 == null || value2.Fallback == null)
			{
				return null;
			}
			return VanillaByName(value2.Fallback);
		}
		if (value2 == null && NativeWidth.TryGetValue(text, out var value4) && texture2D.width > value4)
		{
			value2 = new Def
			{
				W = value4 / 16
			};
		}
		Sprite sprite = VanillaSprite();
		float num = HdScale(value2, texture2D);
		float pixelsPerUnit = ((sprite != null) ? sprite.pixelsPerUnit : 100f) * num;
		Vector2 pivot = ((sprite != null) ? new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height) : new Vector2(0.5f, 0.5f));
		value = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), pivot, pixelsPerUnit);
		value.name = name;
		value.hideFlags = HideFlags.HideAndDontSave;
		return Sprites[name] = value;
	}

	private static bool BatteryState(string key, out Def d, out string suffix)
	{
		string[] array = new string[2] { "_half", "_empty" };
		foreach (string text in array)
		{
			if (key.EndsWith(text) && Defs.TryGetValue(key.Substring(0, key.Length - text.Length), out d) && d.Power != null)
			{
				suffix = text;
				return true;
			}
		}
		d = null;
		suffix = null;
		return false;
	}

	internal static void Redraw(GameItem item)
	{
		try
		{
			Compat.As<GameItemElement>(item)?.Validate();
		}
		catch (Exception ex)
		{
			ModLog.Warn("物品重画失败：" + ex.Message);
		}
	}

	public static void RefreshBattery(GameItem item)
	{
		try
		{
			string text = IdOf(item);
			if (text != null && Defs.TryGetValue(text, out var value) && value.Power != null)
			{
				int num = item.GetTagReadonly("power_source_item_energy")?.GetInt() ?? 0;
				int num2 = item.GetTagReadonly("power_source_item_max_energy")?.GetInt() ?? value.Power[0];
				string text2;
				if (num <= 0)
				{
					text2 = "_empty";
				}
				else
				{
					text2 = ((num * 3 < num2 * 2) ? "_half" : "");
				}
				string text3 = "corp_med_" + text + text2;
				if (item.spritePath != text3)
				{
					item.SetSprite("", text3);
					Redraw(item);
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("电池换图失败：" + ex.Message);
		}
	}

	internal static Sprite VanillaByName(string name)
	{
		if (name == null)
		{
			return null;
		}
		if (VanillaCache.TryGetValue(name, out var value) && value != null)
		{
			return value;
		}
		if (VanillaMissAt.TryGetValue(name, out var value2) && Time.unscaledTime - value2 < 10f)
		{
			return null;
		}
		try
		{
			Sprite[] array = Resources.FindObjectsOfTypeAll<Sprite>();
			foreach (Sprite sprite in array)
			{
				if (sprite != null && sprite.name == name)
				{
					value = sprite;
					break;
				}
			}
		}
		catch
		{
		}
		if (value == null)
		{
			VanillaMissAt[name] = Time.unscaledTime;
		}
		return VanillaCache[name] = value;
	}

	private static Sprite VanillaSprite()
	{
		return VanillaByName("injector_pink_alt");
	}

	public static bool CanUse(GameItem item)
	{
		string text = IdOf(item);
		if (text == null || !Defs[text].Usable || IsLitCandle(item))
		{
			return false;
		}
		if (text == "rejuvenation" && !SilverBlood.CanInject())
		{
			return false;
		}
		try
		{
			return item.IsTag("IS_OWNED_TAG");
		}
		catch
		{
			return false;
		}
	}

	public static void Use(GameItem item, string id)
	{
		ItemRegistry.Migrate(item);
		CorpState corpState = CorpState.Get();
		if (corpState == null)
		{
			return;
		}
		MedState meds = corpState.Meds;
		bool flag = true;
		if (id == null)
		{
			return;
		}
		string text;
		switch (id.Length)
		{
		default:
			return;
		case 6:
			switch (id[0])
			{
			default:
				return;
			case 'e':
				if (!(id == "elixir"))
				{
					return;
				}
				meds.ElixirUntil = Math.Max(meds.ElixirUntil, Day + 2);
				text = L.T("你抹上了名流精华。\n3 天内交易得到的声望 +50%。", "You put on the Socialite Essence.\nReputation gained from trades +50% for 3 days.");
				break;
			case 'c':
			{
				if (!(id == "candle"))
				{
					return;
				}
				string id2 = ItemIdentity.ForGoods("candle_lit").Id;
				if (!ItemRegistry.IsAvailable(id2))
				{
					return;
				}
				item.identifier = id2;
				flag = false;
				meds.CandleUntil = Math.Max(meds.CandleUntil, Day + 2);
				try
				{
					item.SetSprite("", "corp_med_candle_lit");
					Redraw(item);
				}
				catch
				{
				}
				ApplyCandle(PlayerStore.Instance?.storeClientManager);
				text = L.T("你点燃了索拉纳香薰蜡烛。\n3 天内上层区顾客的预算 +15%。", "You light the Solana Scented Candle.\nUpper Levels customers' budget +15% for 3 days.");
				break;
			}
			}
			break;
		case 7:
			if (!(id == "stamina"))
			{
				return;
			}
			meds.StaminaShots++;
			text = ((Settings.VanillaServices && meds.StaminaShots > 2) ? L.T($"你注射了体力增强剂，但身体已经适应了。\n拾荒次数上限最多 +{2}，这一针没有再增加（设置“服务强度”为原版）。", $"You inject the Stamina Booster, but your body has adapted.\nScavenging attempts are capped at +{2}; this shot adds nothing more (Service strength is set to Vanilla).") : L.T($"你注射了体力增强剂。\n每天在垃圾场的拾荒次数上限 +1（已注射 {meds.StaminaShots} 针）。", $"You inject the Stamina Booster.\nDaily scavenging attempts in the Dumping Grounds +1 ({meds.StaminaShots} injected so far)."));
			break;
		case 9:
			if (!(id == "painblock"))
			{
				return;
			}
			meds.PainBlockDay = Day;
			text = L.T("你注射了痛觉屏蔽剂。\n今天拾荒受伤的几率减半。", "You inject the Pain Blocker.\nYour chance of getting hurt while scavenging is halved today.");
			break;
		case 5:
			if (!(id == "patch"))
			{
				return;
			}
			meds.PatchUntil = Math.Max(meds.PatchUntil, Day + 2);
			text = L.T("你贴上了海明威戒烟贴。\n3 天内不受尼古丁戒断的交易惩罚。", "You apply the Hemingway Quit Patch.\nNo nicotine withdrawal trading penalty for 3 days.");
			break;
		case 4:
			if (!(id == "mask"))
			{
				return;
			}
			meds.MaskReady = true;
			text = L.T("你敷上了夜间修护面膜。\n下一次夜间外出，第二天不会开店迟到。", "You put on the Night Repair Mask.\nAfter your next night out, you won't open late the next day.");
			break;
		case 12:
			if (!(id == "rejuvenation") || !SilverBlood.CanInject())
			{
				return;
			}
			text = SilverBlood.Inject();
			break;
		case 10:
			if (!(id == "isomer_gel"))
			{
				return;
			}
			if (SilverBlood.SplitGel(item) == 0)
			{
				try
				{
					StoreUIManager.Instance?.Notify(L.T("店里放不下了，先腾点地方再拆。", "No room in the shop. Make some space first."));
					return;
				}
				catch
				{
					return;
				}
			}
			text = L.T("你拆开了异构修复凝胶，得到 3 小管异构蛋白溶液。纯度各不相同，可以用原版的化学检测仪看。", "You split the isomer repair gel into 3 small tubes of isomer solution. Their purity varies; check them with a chemical scanner.");
			break;
		case 8:
		case 11:
			return;
		}
		try
		{
			if (!string.IsNullOrEmpty(item.soundUse))
			{
				AudioManager.Instance?.Play(item.soundUse);
			}
		}
		catch
		{
		}
		if (flag)
		{
			try
			{
				item.Destroy();
			}
			catch (Exception ex)
			{
				ModLog.Warn("消耗物品失败：" + ex.Message);
			}
		}
		ModLog.Info(text.Replace("\n", " "));
		try
		{
			GenericConfirmationUIManager.Instance?.OpenPanelSingleButton(text, L.T("确定", "OK"));
		}
		catch
		{
		}
	}

	public static bool GlovesCarried()
	{
		try
		{
			List<GameItem> list = EmporiumEntry.Instance?.GetAllAfterhourOwnedItems();
			if (list == null)
			{
				return false;
			}
			foreach (GameItem item in list)
			{
				if (IdOf(item) == "gloves")
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	public static bool SignInShowcase()
	{
		try
		{
			GameGridInventory gameGridInventory = EmporiumEntry.Instance?.showcaseElement;
			if (gameGridInventory?.childItems == null)
			{
				return false;
			}
			foreach (GameItem childItem in gameGridInventory.childItems)
			{
				if (IdOf(childItem) == "sign")
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	public static bool TryUseMask(PlayerStore store)
	{
		MedState m = M;
		HealthData healthData = store?.healthData;
		if (m == null || !m.MaskReady || healthData == null || !healthData.wentToAfterhoursLastNight)
		{
			return false;
		}
		healthData.wentToAfterhoursLastNight = false;
		m.MaskReady = false;
		ModLog.Info("夜间修护面膜生效：今天不会开店迟到");
		return true;
	}

	public static void ApplyCandle(StoreClientManager manager)
	{
		if (!CandleActive || manager?.clientStack == null)
		{
			return;
		}
		if (_boostDay != Day)
		{
			Boosted.Clear();
			_boostDay = Day;
		}
		foreach (StoreClient item in manager.clientStack)
		{
			try
			{
				if (item != null && !(item.clientFaction != "FACTION_UPPER_LEVEL") && (item.identifier == null || !item.identifier.StartsWith("corp_")) && Boosted.Add(item))
				{
					int budget = item.GetBudget();
					if (budget > 0)
					{
						item.OverrideBudget((int)Math.Round((double)budget * 1.15));
					}
				}
			}
			catch
			{
			}
		}
	}

	public static void OnDayStart()
	{
		MedState m = M;
		if (m == null || m.CandleUntil < 0 || Day <= m.CandleUntil)
		{
			return;
		}
		try
		{
			List<GameItem> list = new List<GameItem>();
			foreach (GameItem allRepoEligibleItem in PlayerStore.Instance.GetAllRepoEligibleItems())
			{
				if (IsLitCandle(allRepoEligibleItem))
				{
					list.Add(allRepoEligibleItem);
				}
			}
			foreach (GameItem item in list)
			{
				try
				{
					item.Destroy();
				}
				catch
				{
				}
			}
			if (list.Count > 0)
			{
				ModLog.Info($"香薰蜡烛烧完了（{list.Count} 支）");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("清理蜡烛失败：" + ex.Message);
		}
		m.CandleUntil = -1;
	}
}

