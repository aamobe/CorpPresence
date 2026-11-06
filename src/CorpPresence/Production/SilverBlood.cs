using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static partial class SilverBlood
{
	public const string CreatureId = "corp_presence_silverblood";

	public const string DrawerId = "corp_presence_blood_drawer";

	public const string BloodId = "corp_presence_silver_blood";

	public const string VialId = "corp_presence_isomer_vial";

	public const string ReactorId = "corp_presence_isomer_reactor";

	public const string RejuvId = "corp_presence_rejuvenation";

	public const string CompatId = "corp_presence_implant_compat";

	public const string GelId = "corp_presence_isomer_gel";

	public const string Creature = "@silverblood";

	public const string Drawer = "@blood_drawer";

	public const string Reactor = "@isomer_reactor";

	public const string Handbook = "@sb_handbook";

	public const string LiquidId = "corp_isomer";

	public const string RawId = "corp_isomer_raw";

	public const string SpeciesId = "FISH_CORP_SILVERBLOOD";

	private const string BloodTag = "CORP_SB_BLOOD_ML";

	private const int BloodPerNight = 40;

	private const int BloodPerTube = 50;

	private const float BloodBodyRatio = 0.08f;

	private const float DrawHealthCost = 0.15f;

	private const float DrawHealthCostEarly = 0.2f;

	private const int IsomerPerDose = 30;

	private const int ReactorPower = 8;

	private const int DosePerNight = 1;

	private const int SpeedScale = 100;

	private const string CarryTag = "CORP_SB_REACTOR_CARRY_INT";

	private const int IsomerValuePerMl = 6;

	internal static readonly Dictionary<string, Goods.Def> Defs = new Dictionary<string, Goods.Def>
	{
		["silverblood"] = new Goods.Def
		{
			Name = "银血三叶蜥",
			NameEn = "Silverblood Trilobite",
			Value = 400L,
			Company = "carmicheal",
			Art = "silverblood_small",
			Fallback = "eel_small1",
			Types = Array.Empty<string>(),
			W = 3,
			H = 2,
			Shell = BuildCreature,
			Setup = SetupCreature,
			Desc = "一种有攻击性的异形生物",
			DescEn = "An aggressive alien creature.",
			Flavor = "卡迈克尔实验室的“医用收容物”。背甲分成三节，甲缝里透着银光。",
			FlavorEn = "A Carmicheal lab \"medical containment specimen\". Its carapace splits into three lobes, silver light leaking from the seams."
		},
		["blood_drawer"] = new Goods.Def
		{
			Name = "采血器",
			NameEn = "Blood Drawer",
			Value = 250L,
			Company = "carmicheal",
			Art = "blood_drawer",
			Fallback = "surgery_toolkit",
			Manufacturer = "carmicheal",
			Types = new string[2] { "MEDICAL", "MEDICAL_TOOL" },
			W = 3,
			H = 1,
			Shape = "###",
			Shell = Empty,
			Setup = InitDrawer,
			Desc = "对着养在循环水养殖系统里的成年银血三叶蜥用，每 50 毫升采出一管银血。会伤到它，血没满时伤得更重。",
			DescEn = "Use on an adult Silverblood Trilobite kept in an aquaculture system: one tube of silver blood per 50 ml. It hurts the creature, more so if the blood hasn't fully built up.",
			Flavor = "粗针管，透明储血仓。",
			FlavorEn = "A thick needle and a clear collection chamber."
		},
		["silver_blood"] = new Goods.Def
		{
			Name = "银血",
			NameEn = "Silver Blood",
			Value = 60L,
			Company = "carmicheal",
			Art = "silver_blood",
			Fallback = "med_bottle_orange",
			Types = new string[1] { "MEDICAL" },
			W = 1,
			H = 2,
			Shell = () => LiquidShell("corp_presence_silver_blood", 50, 3),
			OnNew = FillBlood,
			Desc = "一管 50 毫升。粗异构蛋白约占两成，其余是水、蛋白质、油脂、微生物和有机废物。粗异构蛋白要先加还原剂加热活化，反应釜才认；超过 85°C 会变性。",
			DescEn = "One 50 ml tube. About a fifth is raw isomer protein; the rest is water, protein, fat, microbes and organic waste. The raw isomer must be activated with reducing agent and heat before the reactor will take it; above 85°C it denatures.",
			Flavor = "像液态的汞，晃一晃会挂壁。",
			FlavorEn = "Like liquid mercury. It clings to the glass when you swirl it."
		},
		["isomer_vial"] = new Goods.Def
		{
			Name = "异构蛋白溶液",
			NameEn = "Isomer Solution",
			Value = 50L,
			Company = "carmicheal",
			Art = "isomer_vial",
			Fallback = "med_bottle_small_pink",
			Types = new string[1] { "MEDICAL" },
			W = 1,
			H = 1,
			Shell = () => LiquidShell("corp_presence_isomer_vial", 10, 2),
			Desc = "一小管 10 毫升，也可以从异构修复凝胶里拆出来，纯度不一。",
			DescEn = "A small 10 ml tube. Can also be recovered from isomer repair gel. Purity varies.",
			Flavor = "淡银色，半透明。",
			FlavorEn = "Pale silver, translucent."
		},
		["carapace_small"] = Carapace("异构甲壳（小片）", "Isomer Carapace (Small)", 120L, "carapace_small", 2, 2, "一片单独的侧甲。", "A single side plate."),
		["carapace_medium"] = Carapace("异构甲壳（中片）", "Isomer Carapace (Medium)", 260L, "carapace_medium", 3, 2, "连着两三节的甲壳。", "A few segments still joined."),
		["carapace_full"] = Carapace("异构甲壳（整片背甲）", "Isomer Carapace (Full)", 450L, "carapace_full", 4, 3, "完整的三叶背甲，上层区的收藏家会摆进玻璃柜。", "A complete three-lobed carapace. Upper Levels collectors keep these behind glass."),
		["rejuvenation"] = new Goods.Def
		{
			Name = "回春药剂",
			NameEn = "Rejuvenation Serum",
			Value = 650L,
			Company = "carmicheal",
			Art = "rejuvenation",
			Fallback = "injector_purple_large",
			Manufacturer = "carmicheal",
			Types = new string[2] { "MEDICAL", "LUXURY_ITEM" },
			W = 4,
			H = 1,
			Shell = Empty,
			Usable = true,
			Desc = "患哨兵崩溃综合征时可以注射：每针兼当一针免疫宁，累计 3 针彻底治愈。没有这个病的人用不了。",
			DescEn = "Can be injected if you have Sentinel Collapse Syndrome: each shot also counts as an Immunivax shot, and 3 shots in total cure it for good. Useless to anyone without the disease.",
			Flavor = "药液晃动时泛着一点金属光。卡迈克尔的标签上印着「仅限确诊患者使用」。",
			FlavorEn = "The liquid has a faint metallic sheen when shaken. The Carmicheal label reads “For confirmed cases only.”"
		},
		["implant_compat"] = new Goods.Def
		{
			Name = "义体兼容剂",
			NameEn = "Implant Compatibility Agent",
			Value = 450L,
			Company = "carmicheal",
			Art = "implant_compat",
			Fallback = "injector_white",
			Manufacturer = "carmicheal",
			Types = new string[1] { "MEDICAL" },
			W = 4,
			H = 1,
			Shell = Empty,
			Desc = "卡迈克尔为义体排异配制的药剂。",
			DescEn = "A Carmicheal preparation for implant rejection.",
			Flavor = "针管上有一圈细金属环。",
			FlavorEn = "A thin metal ring around the barrel."
		},
		["isomer_gel"] = new Goods.Def
		{
			Name = "异构修复凝胶",
			NameEn = "Isomer Repair Gel",
			Value = 240L,
			Company = "carmicheal",
			Art = "isomer_gel",
			Fallback = "salve",
			Manufacturer = "carmicheal",
			Types = new string[2] { "MEDICAL", "CHEMICAL_SUPPLIES" },
			W = 2,
			H = 2,
			Shell = Empty,
			Usable = true,
			Desc = "纯度不够的批次做成的凝胶。“使用”后拆出 3 小管异构蛋白溶液，纯度各不相同。",
			DescEn = "Gel made from an under-purity batch. Use it to split it into 3 small tubes of isomer solution of varying purity.",
			Flavor = "银灰色，质地稠密像银色的绸缎。",
			FlavorEn = "Silver-grey and dense, like silver satin."
		},
		["isomer_reactor"] = new Goods.Def
		{
			Name = "反应釜",
			NameEn = "Isomer Reactor",
			Value = 400L,
			Company = "carmicheal",
			Art = "isomer_reactor",
			Fallback = "heating_plate",
			Manufacturer = "carmicheal",
			Types = new string[1] { "MACHINE" },
			W = 3,
			H = 3,
			Shape = "###|###|###",
			Shell = BuildReactor,
			OnNew = (GameItem m) =>
			{
				Tobacco.AddManual(m, ReactorManual);
			},
			Desc = "放进装着活化异构蛋白溶液的容器、插上电池，每晚按纯度做成药：97% 以上回春药剂，88% 以上义体兼容剂，60% 以上异构修复凝胶。每份用 30 毫升异构蛋白，默认每晚 1 份，耗 8 电。",
			DescEn = "Load a container of activated isomer solution and a battery. Each night it turns it into medicine by purity: 97%+ Rejuvenation Serum, 88%+ Implant Compatibility Agent, 60%+ Isomer Repair Gel. 30 ml of isomer per dose, 1 dose per night by default, using 8 charge.",
			Flavor = "实验室的密封反应釜，玻璃釜身里是银色的液体。",
			FlavorEn = "A sealed lab reactor, silver liquid behind the glass."
		},
		["sb_handbook"] = new Goods.Def
		{
			Name = "银血提纯手册",
			NameEn = "Silver Blood Refining Handbook",
			Value = 50L,
			Fallback = "book2",
			Manufacturer = "carmicheal",
			Types = new string[1] { "DOCUMENT" },
			W = 2,
			H = 3,
			Shell = Empty,
			Desc = "卡迈克尔内部流程：从银血到反应釜的每一步。",
			DescEn = "Carmicheal internal procedure: every step from silver blood to the reactor.",
			Flavor = "第1步：杀菌\n- 把银血倒进化学容器，放上加热板\n- 加热到70-85°C，杀掉微生物\n警告：粗异构蛋白超过85°C会变性\n\n第2步：活化\n- 降温到60-75°C\n- 加入粗异构蛋白一半量的还原剂\n- 保持温度，直到全部转化\n警告：75-85°C转化更快但会损失一部分\n\n第3步：去杂\n- 参考常规化学品\n\n第4步：蒸发\n- 放进蒸发器\n- （必需）确保水、酒精和脂合剂全部蒸干\n\n第5步：反应\n- 放进反应釜\n- 纯度97%以上出回春药剂，88%以上出义体兼容剂，60%以上出异构修复凝胶",
			FlavorEn = "Step 1: Sterilize\n- Pour the silver blood into a chemistry container and put it on a heating plate\n- Heat to 70-85C to kill the microbes\nWARNING: raw isomer denatures above 85C\n\nStep 2: Activate\n- Cool to 60-75C\n- Add Reducing Agent equal to half the raw isomer\n- Hold the temperature until it has all converted\nWARNING: 75-85C converts faster but loses some of it\n\nStep 3: Clean up\n- Same as regular chemicals\n\nStep 4: Evaporate\n- Use the Evaporator\n- (REQUIRED) All water, alcohol and Lipobond Agent are evaporated\n\nStep 5: React\n- Load it into the Isomer Reactor\n- 97%+ purity makes Rejuvenation Serum, 88%+ Implant Compatibility Agent, 60%+ Isomer Repair Gel"
		},
		["anon_parcel"] = new Goods.Def
		{
			Name = "匿名包裹",
			NameEn = "Anonymous Parcel",
			Value = 0L,
			Company = "carmicheal",
			Art = "anon_parcel",
			Fallback = "ammo_box",
			Types = Array.Empty<string>(),
			W = 2,
			H = 2,
			Crate = true,
			Desc = "没有寄件人。只能取、不能放，取空后自动消失。",
			DescEn = "No sender. Take items out; nothing can be put in. Disappears once empty.",
			Flavor = "牛皮纸，细绳捆着，角落塞着一张纸条。",
			FlavorEn = "Brown paper tied with string, a note tucked in the corner."
		}
	};

	public static readonly (string Key, string Art)[] Stages = new (string, string)[3]
	{
		("sb_stage_small", "silverblood_small"),
		("sb_stage", "silverblood"),
		("sb_stage_large", "silverblood_large")
	};

	private static FishSpecies _species;

	public const string DryTag = "CORP_SB_DRY_TAG";

	private const double ActivateMin = 60.0;

	private const double ActivateFast = 75.0;

	private const double RawDenature = 85.0;

	private const double ActiveDegrade = 75.0;

	private static bool _raw;

	private static readonly System.Random Rng = new System.Random();

	private static readonly (string Id, int Value)[] ParcelPool = new (string, int)[8]
	{
		("large_purple_injector", 160),
		("salve", 80),
		("common_medical", 40),
		("common_chemical", 50),
		("metal_ingot", 50),
		("phagimycin_pill", 25),
		("energy_credit", 25),
		("pink_injector", 50)
	};

	public static FishSpecies Species
	{
		get
		{
			if (_species != null)
			{
				return _species;
			}
			bool art = Tex.Art("carmicheal", Stages[0].Art) != null;
			FishSpecies fishSpecies = new FishSpecies();
			fishSpecies.speciesID = "FISH_CORP_SILVERBLOOD";
			fishSpecies.displayName = L.T("银血三叶蜥", "Silverblood Trilobite");
			fishSpecies.flavorText = L.T("卡迈克尔实验室的编号标本。背甲分成三节，甲缝里透着银光。", "A numbered specimen from a Carmicheal lab. Its carapace splits into three lobes, silver light leaking from the seams.");
			fishSpecies.fishIdentifier = "corp_presence_silverblood";
			fishSpecies.groupCount = 0;
			fishSpecies.stageStartDays = new int[6] { 0, 0, 0, 6, 12, 22 };
			fishSpecies.stageWeightGrams = new int[6] { 0, 0, 200, 800, 1800, 3000 };
			fishSpecies.stageWaterNeedMl = new int[6] { 0, 0, 300, 800, 1500, 2400 };
			fishSpecies.oldAgeDays = 40;
			fishSpecies.maintenanceKcalPer100gPerNight = 2.5f;
			fishSpecies.growthKcalPerGram = 1.2f;
			fishSpecies.conversionEfficiency = 0.85f;
			fishSpecies.meatItems = AquacultureItems.EEL_MEAT;
			fishSpecies.maxHunger = 900f;
			fishSpecies.maxHealth = 140f;
			fishSpecies.wasteMlPer100gPerNight = 1.5f;
			fishSpecies.diet = FishDiet.Omnivore;
			fishSpecies.breedChancePercent = 0;
			fishSpecies.breedHungerRatio = 1f;
			fishSpecies.stageSprites = new string[6]
			{
				null,
				null,
				S(0),
				S(1),
				S(2),
				S(2)
			};
			_species = fishSpecies;
			return _species;
			string S(int i)
			{
				if (!art)
				{
					return (new string[3] { "eel_small1", "eel1", "eel_large1" })[i];
				}
				return "corp_med_" + Stages[i].Key;
			}
		}
	}

	private static (string Zh, string En) ReactorManual => (Zh: "放入：把装着异构蛋白溶液的容器放进溶液槽，把能量电池放进电池槽。\n反应：每晚按纯度做一份药，每份用 30 毫升异构蛋白。\n97% 以上 → 回春药剂\n88% 以上 → 义体兼容剂\n60% 以上 → 异构修复凝胶\n提纯步骤见《银血提纯手册》。\n[模组影响]：\n效率影响功耗\n性能影响速度\n质量按一半折算成速度", En: "Load: put a container of isomer solution in the solution slot and an energy cell in the battery slot.\nReact: each night it makes one dose by purity, using 30 ml of isomer per dose.\n97%+ → Rejuvenation Serum\n88%+ → Implant Compatibility Agent\n60%+ → Isomer Repair Gel\nFor refining, see the Silver Blood Refining Handbook.\n[Module effects]:\nEfficiency affects power use\nPerformance affects speed\nQuality counts half toward speed");

	private static GameItem Empty()
	{
		return ItemDirectory.CreateEmptyItem();
	}

	private static Goods.Def Carapace(string zh, string en, long value, string art, int w, int h, string flavor, string flavorEn)
	{
		Goods.Def def = new Goods.Def();
		def.Name = zh;
		def.NameEn = en;
		def.Value = value;
		def.Company = "carmicheal";
		def.Art = art;
		def.Fallback = "caviar";
		def.Types = new string[2] { "ART", "LUXURY_ITEM" };
		def.W = w;
		def.H = h;
		def.Shell = Empty;
		def.Desc = "银血三叶蜥的甲壳，带珍珠光泽。工艺品、奢侈品。";
		def.DescEn = "Silverblood Trilobite carapace with a pearly sheen. Art piece and luxury item.";
		def.Flavor = flavor;
		def.FlavorEn = flavorEn;
		return def;
	}
}


