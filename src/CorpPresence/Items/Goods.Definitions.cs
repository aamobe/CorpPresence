using System;
using System.Collections.Generic;
using CorpPresence.Malice;

namespace CorpPresence;

internal static partial class Goods
{
    // Pure descriptions and delegates only. Never create items or load textures here.
    internal static class Catalog
    {
        internal static readonly IReadOnlyDictionary<string, Def> Defs;
        internal static readonly IReadOnlyDictionary<string, string> Sources;
        internal static readonly Dictionary<string, (string Company, string Art)> ExtraArt;
        static Catalog()
        {
            var initial = new DefinitionSet<Def>("initial")
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
                    Shape = "####",
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
                    Types = new string[2] { "MEDICAL", "LUXURY_ITEM" },
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
            var Guns = new DefinitionSet<Def>("Guns")
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
            var Spacer = new DefinitionSet<Def>("Spacer")
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
                [Perfume.ExtractorKey + "_running"] = ("solana", Perfume.ExtractorKey + "_running"),
                [Perfume.BloodKey + "_red"] = ("solana", Perfume.BloodKey + "_red"),
                [Perfume.BloodKey + "_blue"] = ("solana", Perfume.BloodKey + "_blue"),
                [Perfume.MixerKey + "_red"] = ("solana", Perfume.MixerKey + "_red"),
                [Perfume.MixerKey + "_blue"] = ("solana", Perfume.MixerKey + "_blue"),
                [Perfume.CollectorKey + "_running"] = ("solana", Perfume.CollectorKey + "_running"),
                ["rolling_machine_running"] = ("pastel", "rolling_machine_running"),
                ["packaging_machine_running"] = ("pastel", "packaging_machine_running"),
                ["meat_grinder_running"] = ("davre", "meat_grinder_running"),
                ["canning_machine_running"] = ("davre", "canning_machine_running"),
                ["seafood_sealer_running"] = ("davre", "seafood_sealer_running"),
                ["forge_furnace_running"] = ("forge", "forge_furnace"),
                ["isomer_reactor_running"] = ("carmicheal", "isomer_reactor_running"),
                ["rat_wheel_generator_occupied"] = ("husbandry", "rat_wheel_generator_retrofit_occupied"),
                ["candle_lit"] = ("solana", "solana_candle_lit"),
                ["hemingway_limited"] = ("pastel", "hemingway_limited"),
                ["sb_stage_small"] = ("carmicheal", "silverblood_small"),
                ["sb_stage"] = ("carmicheal", "silverblood"),
                ["sb_stage_large"] = ("carmicheal", "silverblood_large")
            };
            var set = new DefinitionSet<Def>();
            set.AddRange("Goods", initial);
            set.AddRange("Guns", Guns);
            set.AddRange("Spacer", Spacer);
            set.AddRange("Tobacco", Tobacco.Defs);
            set.AddRange("Perfume", Perfume.Defs);
            set.AddRange("Canning", Canning.Defs);
            set.Add("RatWheelGenerator", RatWheelGenerator.GoodsKey, RatWheelGenerator.Definition());
            set.Add("ProductionWorkBay", ProductionWorkBay.GoodsKey, ProductionWorkBay.Definition());
            set.Add("CargoWarehouse", CargoWarehouse.GoodsKey, CargoWarehouse.Definition());
            set.Add("CargoWarehouse", CargoWarehouse.MangoKey, CargoWarehouse.MangoDefinition());
            set.Add("MangoGift", MangoGift.BackpackKey, MangoGift.BackpackDefinition());
            set.Add("WasteBattery", WasteBattery.Key, WasteBattery.Definition());
            set.Add("CargoWarehouse", CargoWarehouse.ModuleKey, CargoWarehouse.ModuleDefinition());
            set.Add("CargoWarehouse", CargoWarehouse.ControllerKey, CargoWarehouse.ControllerDefinition());
            set.AddRange("Forge", Forge.Defs);
            set.AddRange("SilverBlood", SilverBlood.Defs);
            set.AddRange("MaliceItems", MaliceItems.Defs);
            Defs = set.Values;
            Sources = set.Sources;
            GoodsText.Bind(Defs);
        }

        internal static bool TryGetArt(string key, out Def definition, out string company, out string art)
        {
            definition = null; company = null; art = null;
            if (key == null) return false;
            if (Defs.TryGetValue(key, out definition))
            {
                company = definition.Company; art = definition.Art;
                return true;
            }
            if (ExtraArt.TryGetValue(key, out var extra))
            {
                (company, art) = extra;
                if (key == "candle_lit") Defs.TryGetValue("candle", out definition);
                else if (key.EndsWith("_running", StringComparison.Ordinal))
                    Defs.TryGetValue(key.Substring(0, key.Length - "_running".Length), out definition);
                return true;
            }
            if (!TryBatteryState(key, out definition, out var suffix)) return false;
            company = definition.Company; art = definition.Art + suffix;
            return true;
        }
        internal static bool TryBatteryState(string key, out Def d, out string suffix)
        {
            if (key == null) { d = null; suffix = null; return false; }
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

    }
}
