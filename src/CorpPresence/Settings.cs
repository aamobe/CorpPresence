using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CorpPresence;

internal static class Settings
{
	private const string General = "画面 / Display";

	private const string Unlimited = "不限";

	private static readonly string[] Days = (from n in Enumerable.Range(1, 30)
		select n.ToString()).ToArray();

	private static ConfigEntry<string> _portraits;

	// 0.9.2 加回高清立绘（旧版立绘，只分 2K、4K 两档，不用多级纹理）；放在最后，已存的选项不变
	private static readonly string[] PortraitSets = new string[3] { "平滑新版立绘 / New, smooth", "像素化新版立绘 / New, pixel", "高清立绘 / HD" };

	private static ConfigEntry<string> _restock;

	private static readonly Dictionary<string, ConfigEntry<string>> PeriodEntries = new Dictionary<string, ConfigEntry<string>>();

	private static readonly string[] PeriodDays = (from n in Enumerable.Range(3, 19)
		select n.ToString()).ToArray();

	private static ConfigEntry<string> _panelScale;

	private static readonly string[] Scales = new string[5] { "1", "1.25", "1.5", "1.75", "2" };

	private static ConfigEntry<string> _cap;

	private static ConfigEntry<bool> _limits;

	private static ConfigEntry<bool> _orderEvents;

	private static ConfigEntry<string> _orderFactor;

	private static readonly string[] Factors = new string[8] { "0.8", "0.9", "1.0", "1.1", "1.2", "1.3", "1.4", "1.5" };

	private static ConfigEntry<bool> _coopCap;

	private static ConfigEntry<bool> _malice;

	private static ConfigEntry<string> _budget;

	private static ConfigEntry<bool> _cleanup;

	private static ConfigEntry<bool> _cleanupConfirm;

	private const string Dynamic = "动态 / Dynamic";

	private static readonly string[] Budgets = new string[4] { "动态 / Dynamic", "3000", "10000", "30000" };

	private const string Standard = "标准 / Standard";

	private const string Vanilla = "原版 / Vanilla";

	private static readonly string[] Strengths = new string[2] { "标准 / Standard", "原版 / Vanilla" };

	private static ConfigEntry<string> _services;

	private static ConfigEntry<string> _buyPrices;

	private static ConfigEntry<string> _coopCredits;

	private static readonly string[] CoopRates = new string[5] { "100", "200", "300", "400", "500" };

	private static readonly Random Rng = new Random();

	private static readonly Dictionary<string, (ConfigEntry<string> Min, ConfigEntry<string> Max, ConfigEntry<string> Story)> Visits = new Dictionary<string, (ConfigEntry<string>, ConfigEntry<string>, ConfigEntry<string>)>();

	public const int CoopPerDayMax = 10;

	private static readonly Dictionary<string, (string Zh, string En)> Names = new Dictionary<string, (string, string)>
	{
		["isa"] = ("伊莎", "Isa"),
		["milo"] = ("米洛", "Milo"),
		["seline"] = ("瑟琳", "Seline"),
		["rosen"] = ("罗森", "Rosen"),
		["doreen"] = ("多琳", "Doreen"),
		["mentor"] = ("导师", "The Mentor")
	};

	// 1 = 平滑新版，2 = 像素化新版，3 = 高清立绘
	public static int PortraitSet => _portraits?.Value == PortraitSets[1] ? 2 : _portraits?.Value == PortraitSets[2] ? 3 : 1;

	public static float PanelScale
	{
		get
		{
			if (_panelScale == null || !float.TryParse(_panelScale.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				return 1.5f;
			}
			return result;
		}
	}

	public static int DailyCap => Number(_cap, 0);

	public static bool OrderLimits
	{
		get
		{
			ConfigEntry<bool> limits = _limits;
			if (limits == null || !limits.Value)
			{
				if (Ext.ForceOrderLimits != null)
				{
					return Ext.Ask("ForceOrderLimits", Ext.ForceOrderLimits, fallback: false);
				}
				return false;
			}
			return true;
		}
	}

	public static bool OrderEvents => _orderEvents?.Value ?? true;

	public static int RestockWeeks
	{
		get
		{
			int num = Forced(Ext.ForceRestockWeeks, "ForceRestockWeeks");
			return Math.Max(1, Math.Min(3, (num > 0) ? num : Number(_restock, 1)));
		}
	}

	public static double OrderMarkup
	{
		get
		{
			if (_orderFactor == null || !double.TryParse(_orderFactor.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				return 1.1;
			}
			return result;
		}
	}

	public static bool CoopDailyCap => _coopCap?.Value ?? true;

	public static int AgentBudget
	{
		get
		{
			if (!(_budget?.Value == "动态 / Dynamic") && !LockedVanilla)
			{
				return Number(_budget, 10000);
			}
			return VanillaBudget();
		}
	}

	public static bool VanillaServices
	{
		get
		{
			if (!(_services?.Value == "原版 / Vanilla"))
			{
				return LockedVanilla;
			}
			return true;
		}
	}

	public static bool VanillaBuyPrices
	{
		get
		{
			if (!(_buyPrices?.Value == "原版 / Vanilla"))
			{
				return LockedVanilla;
			}
			return true;
		}
	}

	private static bool LockedVanilla
	{
		get
		{
			if (Ext.LockVanillaDifficulty != null)
			{
				return Ext.Ask("LockVanillaDifficulty", Ext.LockVanillaDifficulty, fallback: false);
			}
			return false;
		}
	}

	public static double CoopCredits => Math.Max(100, Math.Min(500, Number(_coopCredits, 100)));

	public static bool MaliceMode => _malice?.Value ?? false;

	public static bool CleanupForUninstall
	{
		get
		{
			ConfigEntry<bool> cleanup = _cleanup;
			if (cleanup != null && cleanup.Value)
			{
				return _cleanupConfirm?.Value ?? false;
			}
			return false;
		}
	}

	public static int Period(string key, int fallback)
	{
		if (!PeriodEntries.TryGetValue(key, out var value))
		{
			return fallback;
		}
		return Math.Max(3, Math.Min(21, Number(value, fallback)));
	}

	public static int VanillaBudget()
	{
		return 800 + Rng.Next(0, 251) + StoreStation.GetDayCounter();
	}

	private static int Forced(Func<int> f, string name)
	{
		if (f != null)
		{
			return Ext.Ask(name, f, 0);
		}
		return 0;
	}

	public static void Init(string modId)
	{
		ModConfig config = ModConfig.For(modId);
		_cap = config.Define("DailyCap", "不限", "来访 / Visits", () => L.T("每天最多几位公司人物", "Max company characters per day"), new string[1] { "不限" }.Concat(from n in Enumerable.Range(1, 6)
			select n.ToString()).ToArray(), () => L.T("超出的人顺延到第二天：剧情优先，其次是最久没来的。花钱请来的客流、贵宾、实验室访客不算在内。", "Extra visitors are pushed to the next day: story first, then whoever has waited longest. Paid foot traffic, VIPs and lab buyers don't count."));
		_limits = config.Define("OrderLimits", defaultValue: false, "订货 / Orders", () => L.T("订货每周限量", "Weekly order limits"), () => L.T("打开后，各公司电话订货的每样货每周限量，按一份的基础价值分阶：\n", "When on, each item ordered by phone has a weekly limit, tiered by its base value:\n") + L.T("50₵ 以下 20 份，200₵ 以下 10 份，300₵ 以下 5 份，300₵ 及以上 3 份。\n", "under 50₵: 20, under 200₵: 10, under 300₵: 5, 300₵ and up: 3.\n") + L.T("海明威限量包装、免疫宁整箱这些独家货不管开不开都按原来的限量。", "Exclusives like Hemingway Limited Edition and the Immunivax case keep their own limits either way."));
		_orderEvents = config.Define("OrderEvents", defaultValue: true, "订货 / Orders", () => L.T("事件影响订货", "Events affect orders"), () => L.T("打开后，原版市场事件（某类物品涨价、降价）也会影响电话订货的单价：事件让这类物品涨多少、跌多少，订货价就跟着涨跌多少，和店里同类物品的估价一致。\n", "When on, vanilla market events (a type of item going up or down in price) also change phone order prices: the order price rises or falls by as much as the event moves that type of item, matching how the shop values it.\n") + L.T("下单时按当时的价格结算，之后事件结束也不退差价。星际客改装盲盒、各公司的固定价设备不受影响。\n关闭：订货价不看事件（以前的做法）。", "You pay the price at the time of ordering; nothing is refunded when the event ends. The Spacer grab box and fixed-price machines are not affected.\nOff: order prices ignore events (the old behaviour)."));
		_orderFactor = config.Define("OrderMarkup", "1.1", "订货 / Orders", () => L.T("订购溢价（倍）", "Order markup (x)"), Factors, () => L.T("电话订货的价格 = 物品价值 × 这个倍数（默认 1.1）。合作档位越高每档便宜 0.05：签约 ×1.1、长期客户 ×1.05、合作伙伴 ×1.0（按默认值）。\n子午线高端配件、利维坦也按这个倍数（不看档位）。星际客改装盲盒固定 500₵。", "Phone order price = item value × this (default 1.1). Each higher cooperation tier is 0.05 cheaper: Signed ×1.1, Regular ×1.05, Partner ×1.0 (at the default).\nMeridian's high-end attachments and the Leviathan use this multiplier too (regardless of tier). The Spacer grab box is a fixed 500₵."));
		_budget = config.Define("AgentBudget", "10000", "来访 / Visits", () => L.T("公司人物携带信用点", "Company character credits"), Budgets, () => L.T("公司人物来收货时最多花多少信用点（六位人物、索拉纳采购员）。\n动态：800 + 随机 0～250 + 当前天数，参照原版最有钱的顾客（750～1000 + 天数）。\n索拉纳贵宾带多少钱看“服务强度”。", "The most a company character will spend when buying from you (the six characters and the Solana lab buyer).\nDynamic: 800 + random 0–250 + the current day, based on the richest vanilla customers (750–1000 + day).\nHow much Solana's VIPs bring depends on “Service strength”."));
		_services = config.Define("ServiceStrength", "标准 / Standard", "来访 / Visits", () => L.T("服务强度", "Service strength"), Strengths, () => L.T("标准：现在的数值。\n原版：按原版数值校准——\n· 索拉纳贵宾带 800 + 随机 0～250 + 天数（标准是 3000～5000）；原版最有钱的顾客带 750～1000 + 天数；\n· 实验室采购员收单上的货一律 1.5 倍（标准：2 倍，蓝血 3 倍）；原版涨价事件最常见 +50%；\n· 体力增强剂累计最多 +2 次拾荒（标准：每针 +1，不封顶）；原版开局特长“熟练拾荒者”是 +2；\n· 罗森批发八折（标准：七折）；原版供货商八折。", "Standard: the current values.\nVanilla: tuned to the base game—\n· Solana VIPs bring 800 + random 0–250 + the day (Standard: 3000–5000); the richest vanilla customers bring 750–1000 + day;\n· the lab buyer pays 1.5x for everything on the list (Standard: 2x, Blue Blood 3x); vanilla price events are most often +50%;\n· Stamina Boosters add at most +2 scavenging attempts in total (Standard: +1 per shot, no cap); the vanilla Proficient Scavenger perk gives +2;\n· Rosen sells wholesale at 20% off (Standard: 30%); vanilla suppliers give 20% off."));
		_buyPrices = config.Define("BuyPriceStrength", "标准 / Standard", "来访 / Visits", () => L.T("人物收货价", "Character buying prices"), Strengths, () => L.T("标准：现在的数值。\n原版：日常收货不加价，特殊需求最多 1.5 倍（原版顾客按成交价付钱，涨价事件最常见 +50%）——\n· 米洛收海明威假烟 2 倍 → 1.5 倍；\n· 瑟琳日常收货 1.2 倍 → 1 倍，收蓝血 3 倍 → 1.5 倍；\n· 伊莎收赛纳普斯 1.5 倍、瑟琳章节里收奢侈品 1.3 倍、多琳收海德拉 400₵ 不变；伊莎、罗森日常收货本来就不加价。", "Standard: the current values.\nVanilla: no premium on everyday purchases, at most 1.5x for special demand (vanilla customers pay the negotiated price; price events are most often +50%)—\n· Milo's fake Hemingways: 2x → 1.5x;\n· Seline's everyday purchases: 1.2x → 1x, Blue Blood: 3x → 1.5x;\n· unchanged: Isa's Synapse 1.5x, Seline's story luxury purchases 1.3x, Doreen's Hydra 400₵; Isa's and Rosen's everyday purchases never had a premium."));
		_malice = config.Define("MaliceMode", defaultValue: false, "难度 / Difficulty", () => L.T("恶意巨企模式", "Corporate Malice mode"), () => L.T("打开后：巨企的订货要按周订阅，服务全面涨价，合作档位的货要花钱申请解锁；星际客联盟拆成商人联合和反抗军；巨企使者撒该会找上门。\n建议从新档开始。中途打开：合作档位保留，已解锁的货要重新申请。中途关闭（第二天开店生效）：两边并回星际客联盟，订阅、代金券、助力点、撒该的账全部清空。", "When on: corporate phone orders need a weekly subscription, services cost more, and tier goods must be unlocked by paid application; the Spacer Alliance splits into the Merchant Union and the Resistance; the corporate envoy Zacchaeus comes calling.\nStarting a new save is recommended. Turned on mid-game: cooperation tiers are kept, but unlocked goods must be applied for again. Turned off mid-game (from the next day's opening): both sides merge back into the Spacer Alliance, and subscriptions, vouchers, support points and Zacchaeus's accounts are cleared."));
		_coopCap = config.Define("CoopDailyCap", defaultValue: true, "合作度 / Cooperation", () => L.T("合作度每天上限", "Daily cooperation cap"), () => L.T("打开：每家公司的合作度（星际客联盟叫信任度）每天最多涨 ", "On: each company's cooperation (Trust for the Spacer Alliance) rises by at most ") + 10 + L.T(" 点，超出的不累计。\n", " per day; the excess is lost.\n") + L.T("买卖、订货、服务、声望上涨都算在内；剧情推进的奖励和剧情保底不受限制。\n", "Trades, orders, services and reputation gains all count; story rewards and story minimums are not capped.\n") + L.T("关闭：不设上限。", "Off: no cap."));
		_coopCredits = config.Define("CoopCredits", "100", "合作度 / Cooperation", () => L.T("合作度：每多少信用点涨 1 点", "Cooperation: credits per point"), CoopRates, () => L.T("和公司的人买卖、电话订货（含利维坦）、帕斯泰尔加盟费、子午线翻新费，每这么多信用点涨 1 点合作度（默认 100）；卖出本公司产品按 3 倍算（默认每 300₵ 涨 1 点）。\n原版势力声望是每 400₵ 涨 1 点。剧情奖励和星际客的声望折算不受影响。", "Trading with company people, phone orders (including the Leviathan), the pastel dealer fee and Meridian refurbishment fees give 1 cooperation point per this many credits (default 100); selling the company's products counts at 3x that (default 1 point per 300₵).\nVanilla faction reputation rises 1 point per 400₵. Story rewards and the Spacer reputation conversion are not affected."));
		_panelScale = config.Define("PanelScale", "1.5", "画面 / Display", () => L.T("电话服务界面大小（倍）", "Phone service window size (x)"), Scales, () => L.T("公司电话打开的服务界面放大几倍。1 = 以前的大小。屏幕放不下时会自动缩小到刚好放得下。", "How much to enlarge the service window opened from company phone calls. 1 = the old size. Shrinks automatically if it doesn't fit the screen."));
		_cleanup = config.Define("CleanupForUninstall", defaultValue: false, "卸载 / Uninstall", () => L.T("卸载前清理", "Clean up before uninstalling"), () => L.T("要卸载本模组时打开，并同时勾选下面的“确认清理”。两项都打开后存一次档（睡觉或手动存档）：\n· 公司电话从电话簿和存档里移除；\n· 本模组的物品全部删除（名片、新物品、罗森的手作枪、利维坦等）。\n存完档就可以卸载。不卸载的话请保持关闭。", "Turn on when you want to uninstall, together with “Confirm cleanup” below. With both on, save once (sleep or save manually):\n· company phone numbers are removed from the phone book and the save;\n· every item from this mod is deleted (cards, new items, Rosen's guns, the Leviathan, etc.).\nThen you can uninstall. Keep it off if you're not uninstalling."));
		_cleanupConfirm = config.Define("CleanupConfirm", defaultValue: false, "卸载 / Uninstall", () => L.T("确认清理（会删除本模组的物品）", "Confirm cleanup (deletes this mod's items)"), () => L.T("和“卸载前清理”一起打开才会生效。删除的物品无法恢复。", "Only takes effect together with “Clean up before uninstalling”. Deleted items cannot be restored."));
		_restock = config.Define("RestockWeeks", "1", "周期 / Periods", () => L.T("订货补货周期（周）", "Order restock period (weeks)"), new string[3] { "1", "2", "3" }, () => L.T("每周限量的货、达夫雷利维坦的配额，多少周补一次货。1 = 每周（原来的做法）。", "How many weeks until weekly order limits and Davre's Leviathan quota refill. 1 = every week (the original behavior)."));
		P("vip", 3, "索拉纳贵宾：预约间隔（天）", "Solana VIP: booking interval (days)", "预约一次贵宾后，隔几天才能再约。", "Days before you can book another VIP.");
		P("lab", 7, "索拉纳实验室采购单：周期（天）", "Solana lab order: period (days)", "采购单多少天换一张，每张可以预约一次采购员。", "How often the lab's purchase list changes; one buyer booking per list.");
		P("gift", 7, "索拉纳会员礼遇：周期（天）", "Solana member gift: period (days)", "黑金会员多少天可以领一次礼遇。", "How often Black Gold Members can claim the gift.");
		P("stamina", 10, "卡迈克尔体力增强剂：申领间隔（天）", "Carmicheal Stamina Booster: request interval (days)", "申领一支后，隔几天才能再申领。", "Days between Stamina Booster requests.");
		P("swap", 3, "卡迈克尔赝品换正品：间隔（天）", "Carmicheal fake-for-genuine swap: interval (days)", "申请一次换货后，隔几天才能再申请。", "Days between swap requests.");
		P("refurbish", 3, "子午线返厂翻新：间隔（天）", "Meridian refurbishment: interval (days)", "送一次枪翻新后，隔几天才能再送。以前固定 2 天。", "Days between refurbishment drop-offs. Used to be a fixed 2 days.");
		P("rebate", 7, "帕斯泰尔合作商返点：周期（天）", "pastel dealer rebate: period (days)", "多少天结一次返点，由送货员带来。", "How often the dealer rebate is settled and brought by the courier.");
		_portraits = config.Define("PortraitSet", PortraitSets[0], "画面 / Display", () => L.T("立绘", "Portraits"), PortraitSets, () => L.T("选公司人物用哪一套立绘。\n平滑新版立绘：分三档，1080p 起是平滑的高清图。\n像素化新版立绘：像素画，4K 用 4K 图，其余都用 2K 图。\n高清立绘：原来的高清立绘，4K 用 4K 图，其余都用 2K 图（撒该没有这一套，用平滑新版）。\n都按屏幕分辨率用不同清晰度的图；1080p 及以上会关掉游戏的低分辨率中转（会改游戏的像素完美相机）。", "Which set of company portraits to use.\nNew, smooth: three tiers, smooth HD images from 1080p up.\nNew, pixel: pixel art; 4K image at 4K, 2K image otherwise.\nHD: the original HD portraits; 4K image at 4K, 2K image otherwise (Zacchaeus uses New, smooth).\nAll use sharper images at higher resolutions; at 1080p and above the game's low-resolution pass is skipped (changes the game's pixel-perfect camera)."));
		try
		{
			if (Array.IndexOf(PortraitSets, _portraits.Value) < 0)
			{
				_portraits.Set(PortraitSets[0]);
			}
		}
		catch
		{
		}
		Character[] all = Engine.All;
		foreach (Character character in all)
		{
			string text;
			string text2;
			if (Names.TryGetValue(character.Id, out (string, string) value))
			{
				(text, text2) = value;
			}
			else
			{
				string name = character.Name;
				string name2 = character.Name;
				text2 = name2;
				text = name;
			}
			string section = "来访：" + text + " / Visits: " + text2;
			ConfigEntry<string> item = null;
			ConfigEntry<string> item2 = null;
			if (character.HasDaily)
			{
				(int Min, int Max) defaultDailyGap = character.DefaultDailyGap;
				int item3 = defaultDailyGap.Min;
				int item4 = defaultDailyGap.Max;
				item = config.Define(character.Id + ".DailyGapMin", item3.ToString(), section, () => L.T("日常来访间隔·最少（天）", "Daily visit gap, min (days)"), Days, () => L.T("这次日常来访之后，最少隔几天再来。改了从下一次来访起生效。", "Minimum days before the next daily visit. Takes effect from the next visit."));
				item2 = config.Define(character.Id + ".DailyGapMax", item4.ToString(), section, () => L.T("日常来访间隔·最多（天）", "Daily visit gap, max (days)"), Days, () => L.T("最多隔几天再来，在最少和最多之间随机。比最少还小时按最少算。", "Maximum days before the next visit; picked at random between min and max. If lower than min, min is used."));
			}
			ConfigEntry<string> item5 = config.Define(character.Id + ".StoryGap", "1", section, () => L.T("剧情章节间隔（天）", "Gap between story chapters (days)"), Days, () => L.T("上一章之后，至少隔几天下一章才来（1 = 条件够了第二天就来，原来的做法）。", "Minimum days between story chapters (1 = the next day once conditions are met, the original behavior)."));
			Visits[character.Id] = (item, item2, item5);
		}
		void P(string key, int def, string zh, string en, string zhTip, string enTip)
		{
			PeriodEntries[key] = config.Define("Period." + key, def.ToString(), "周期 / Periods", () => L.T(zh, en), PeriodDays, () => L.T(zhTip, enTip));
		}
	}

	private static int Number(ConfigEntry<string> e, int fallback)
	{
		if (e == null || !int.TryParse(e.Value, out var result))
		{
			return fallback;
		}
		return result;
	}

	public static int DailyGap(Character c)
	{
		var (num, num2) = c.DefaultDailyGap;
		if (Visits.TryGetValue(c.Id, out (ConfigEntry<string>, ConfigEntry<string>, ConfigEntry<string>) value) && value.Item1 != null)
		{
			num = Math.Max(1, Number(value.Item1, num));
			num2 = Math.Max(num, Number(value.Item2, num2));
		}
		return new Random().Next(num, num2 + 1);
	}

	public static int StoryGap(Character c)
	{
		if (!Visits.TryGetValue(c.Id, out (ConfigEntry<string>, ConfigEntry<string>, ConfigEntry<string>) value))
		{
			return 1;
		}
		return Math.Max(1, Number(value.Item3, 1));
	}
}
