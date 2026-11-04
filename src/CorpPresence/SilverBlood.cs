using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static class SilverBlood
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
			Flavor = "（危险描述）",
			FlavorEn = "(Dangerous description)"
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
			Desc = "减轻身体对义体的排异。（卖给改造体应该有彩蛋）",
			DescEn = "Reduces the body's rejection of implants. (Selling it to augmented customers should have an easter egg)",
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

	private static Liquid _ownedRaw;

	private static Liquid _ownedIsomer;

	private static bool _ownsSpecies;

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

	public static void RegisterSpecies()
	{
		if (!GameResources.Ready)
		{
			throw new InvalidOperationException("银血物种资源尚未就绪");
		}
		if (FishSpecies.All.TryGetValue("FISH_CORP_SILVERBLOOD", out var value))
		{
			if (value != Species)
			{
				throw new InvalidOperationException("银血物种标识已由其他内容占用");
			}
		}
		else
		{
			_ownsSpecies = true;
			FishSpecies.All["FISH_CORP_SILVERBLOOD"] = Species;
		}
	}

	public static bool IsCreature(GameItem item)
	{
		try
		{
			return item != null && item.GetTagReadonly("FISH_SPECIES_TAG").GetString() == "FISH_CORP_SILVERBLOOD";
		}
		catch
		{
			return false;
		}
	}

	public static bool BeforeTankFits(int originX, int originY, List<Vector2Int> cells, int ___width, int ___height, ref bool __result)
	{
		foreach (Vector2Int cell in cells)
		{
			long num = (long)originX + (long)cell.x;
			long num2 = (long)originY + (long)cell.y;
			if (num < 0 || num2 < 0 || num >= ___width || num2 >= ___height)
			{
				__result = false;
				return false;
			}
		}
		return true;
	}

	private static GameItem BuildCreature()
	{
		RegisterSpecies();
		GameItem gameItem = Empty().SetName(Species.GetDisplayName()).SetSpriteAndShape(Species.spriteAtlas, Species.stageSprites[2]);
		gameItem.unitValue = 0L;
		AquacultureHelper.InitFishItem(gameItem, Species);
		AudioHelper.InitMeaty(gameItem);
		return gameItem;
	}

	private static void SetupCreature(GameItem item)
	{
		AquacultureHelper.UpdateNameAndSprite(item);
		item.onCycleEndSlotItemFunc = (GameItem cur, GameInventory inv, SlotMarker _) =>
		{
			Nightly(cur, inv);
		};
	}

	private static void Nightly(GameItem item, GameInventory inv)
	{
		try
		{
			if (AquacultureHelper.IsDead(item) || !AquacultureHelper.IsInTank(inv))
			{
				return;
			}
			if (item.IsTag("CORP_SB_DRY_TAG") || AquacultureHelper.GetStage(item) < FishStage.Adult || AquacultureHelper.GetHealth(item) < Species.maxHealth * 0.9f)
			{
				return;
			}
			int cap = BloodCap(item);
			int blood = Blood(item);
			if (blood < cap)
			{
				item.ModifyTag("CORP_SB_BLOOD_ML", (TagState s) =>
				{
					s.Enable().SetInt(Math.Min(cap, blood + 40));
				});
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("银血三叶蜥每晚处理失败：" + ex.Message);
		}
	}

	public static void BeforeApplyWaterChecks(ref List<GameItem> live, bool isPowered)
	{
		try
		{
			live = SilverBloodFlow.WaterChecks(live, IsCreature, ApplyWaterExemption, !isPowered);
		}
		catch (Exception ex)
		{
			ModLog.Warn("三叶蜥养殖缸水量检查失败：" + ex.Message);
		}
	}

	private static void ApplyWaterExemption(GameItem item)
	{
		if (AquacultureHelper.GetHunger(item) >= Species.maxHunger * 0.5f)
		{
			AquacultureHelper.ModifyHealth(item, Species.maxHealth * 0.1f);
		}
	}

	public static void BeforeTickTank(GameGridInventory tankGrid, GameItem waterTank, bool isPowered, out Dictionary<GameItem, float> __state)
	{
		__state = null;
		if (tankGrid == null)
		{
			return;
		}
		try
		{
			int waterMl = AquacultureHelper.GetWaterMl(waterTank);
			foreach (GameItem childItem in tankGrid.childItems)
			{
				if (IsCreature(childItem) && !AquacultureHelper.IsDead(childItem))
				{
					if (waterMl < Species.stageWaterNeedMl[(int)AquacultureHelper.GetStage(childItem)])
					{
						childItem.EnableTag("CORP_SB_DRY_TAG");
					}
					else
					{
						childItem.DisableTag("CORP_SB_DRY_TAG");
					}
					if (!isPowered)
					{
						(__state ?? (__state = new Dictionary<GameItem, float>()))[childItem] = AquacultureHelper.GetHealth(childItem);
					}
				}
			}
		}
		catch
		{
			__state = null;
		}
	}

	public static void AfterTickTank(Dictionary<GameItem, float> __state)
	{
		if (__state == null)
		{
			return;
		}
		foreach (KeyValuePair<GameItem, float> item in __state)
		{
			try
			{
				if (!AquacultureHelper.IsDead(item.Key))
				{
					float health = AquacultureHelper.GetHealth(item.Key);
					if (health < item.Value)
					{
						AquacultureHelper.ModifyHealth(item.Key, item.Value - health);
					}
					if (AquacultureHelper.GetHealth(item.Key) <= 0f)
					{
						AquacultureHelper.OnFishDead(item.Key);
					}
				}
			}
			catch
			{
			}
		}
	}

	public static void AfterWillSuffocate(GameItem stock, ref bool __result)
	{
		try
		{
			if (__result && IsCreature(stock) && AquacultureHelper.GetHostRAS(stock) == null)
			{
				__result = false;
			}
		}
		catch
		{
		}
	}

	public static bool BeforeFillet(GameItem fish, ref List<GameItem> __result)
	{
		try
		{
			if (!IsCreature(fish))
			{
				return true;
			}
			FishStage stage = AquacultureHelper.GetStage(fish);
			string id;
			if (stage < FishStage.Elder)
			{
				id = ((stage >= FishStage.Adult) ? "carapace_medium" : "carapace_small");
			}
			else
			{
				id = "carapace_full";
			}
			__result = new List<GameItem>();
			GameItem gameItem = Goods.Make(id);
			if (gameItem != null)
			{
				GeneralHelper.SetItemOwned(gameItem, GeneralHelper.IsItemOwned(fish));
				__result.Add(gameItem);
			}
			return false;
		}
		catch (Exception ex)
		{
			ModLog.Warn("三叶蜥屠宰失败：" + ex.Message);
			return true;
		}
	}

	private static int Blood(GameItem item)
	{
		return item.GetTagReadonly("CORP_SB_BLOOD_ML").GetInt();
	}

	private static int BloodCap(GameItem item)
	{
		FishStage stage = AquacultureHelper.GetStage(item);
		if (stage >= FishStage.Adult)
		{
			return Mathf.RoundToInt((float)Species.stageWeightGrams[(int)stage] * 0.08f);
		}
		return 0;
	}

	private static void InitDrawer(GameItem tool)
	{
		tool.maySelectSlotItemFunc = (GameItem _, GameInventory _, SlotMarker _) => true;
		tool.canSelectSlotItemFunc = (GameItem _, GameInventory _, SlotMarker _) => true;
		tool.mayThisTargetItemFunc = (Func<GameItem, GameItem, bool>)Delegate.Combine(tool.mayThisTargetItemFunc, (Func<GameItem, GameItem, bool>)((GameItem s, GameItem t) => GeneralHelper.IsItemOwned(s, t) && IsCreature(t)));
		tool.canThisTargetItemFunc = (Func<GameItem, GameItem, bool>)Delegate.Combine(tool.canThisTargetItemFunc, (Func<GameItem, GameItem, bool>)((GameItem s, GameItem t) => GeneralHelper.IsItemOwned(s, t) && CanDraw(t)));
		tool.onThisTargetItemFunc = (Action<GameItem, GameItem>)Delegate.Combine(tool.onThisTargetItemFunc, (Action<GameItem, GameItem>)((GameItem _, GameItem t) =>
		{
			if (IsCreature(t))
			{
				Draw(t);
			}
		}));
	}

	private static float DrawCost(GameItem fish)
	{
		int num = BloodCap(fish);
		float num2 = ((num > 0) ? Mathf.Clamp01((float)Blood(fish) / (float)num) : 1f);
		return Species.maxHealth * (0.15f + 0.2f * (1f - num2));
	}

	private static bool CanDraw(GameItem fish)
	{
		if (IsCreature(fish) && !AquacultureHelper.IsDead(fish) && Blood(fish) >= 50)
		{
			return AquacultureHelper.GetHealth(fish) > DrawCost(fish);
		}
		return false;
	}

	private static void Draw(GameItem fish)
	{
		try
		{
			if (!CanDraw(fish))
			{
				return;
			}
			int num = Blood(fish) / 50;
			float num2 = DrawCost(fish);
			GameInventory parentInventory = fish.parentInventory;
			int tubes = 0;
			for (int i = 0; i < num; i++)
			{
				GameItem gameItem = Goods.Make("silver_blood");
				if (gameItem == null)
				{
					continue;
				}
				bool flag = false;
				try
				{
					GeneralHelper.SetItemOwned(gameItem);
					flag = Placed(parentInventory, gameItem);
				}
				finally
				{
					if (!flag && gameItem.parentInventory == null)
					{
						gameItem.Destroy();
					}
				}
				if (!flag)
				{
					break;
				}
				tubes++;
			}
			if (tubes == 0)
			{
				Tell(L.T("店里放不下了，先腾点地方再采血。", "No room in the shop. Make some space before drawing blood."));
				return;
			}
			fish.ModifyTag("CORP_SB_BLOOD_ML", (TagState s) =>
			{
				s.Enable().SetInt(Blood(fish) - tubes * 50);
			});
			AquacultureHelper.ModifyHealth(fish, 0f - num2);
			CarmichaelCo.OnBloodDrawn(tubes);
			try
			{
				AudioManager.Instance.Play3("click_vintage");
			}
			catch
			{
			}
			ModLog.Info($"采血：{tubes} 管银血，扣健康 {num2:0}");
		}
		catch (Exception ex)
		{
			ModLog.Warn("采血失败：" + ex.Message);
		}
	}

	private static bool Placed(GameInventory where, GameItem item)
	{
		GraphUtils.EmporiumTryAdd(where, item);
		try
		{
			return item.parentInventory != null;
		}
		catch
		{
			return false;
		}
	}

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

	public static void InstallResources()
	{
		if (!GameResources.Ready)
		{
			throw new InvalidOperationException("公司液体资源尚未就绪");
		}
		RegisterLiquid();
		if (FishSpecies.All.TryGetValue("FISH_CORP_SILVERBLOOD", out var value) && value != Species)
		{
			throw new InvalidOperationException("银血物种标识已由其他内容占用");
		}
		RegisterSpecies();
		if (FishSpecies.All["FISH_CORP_SILVERBLOOD"] != Species)
		{
			throw new InvalidOperationException("银血物种注册未完成");
		}
	}

	public static void UninstallResources()
	{
		RemoveOwned("corp_isomer_raw", ref _ownedRaw);
		RemoveOwned("corp_isomer", ref _ownedIsomer);
		if (_ownsSpecies && FishSpecies.All.TryGetValue("FISH_CORP_SILVERBLOOD", out var value) && value == Species)
		{
			FishSpecies.All.Remove("FISH_CORP_SILVERBLOOD");
		}
		_ownsSpecies = false;
	}

	private static void RemoveOwned(string id, ref Liquid owned)
	{
		if (owned != null)
		{
			if (Liquid.LiquidDict.TryGetValue(id, out var value) && value == owned)
			{
				Liquid.LiquidDict.Remove(id);
			}
			Liquid.Liquids.Remove(owned);
			owned = null;
		}
	}

	public static void RegisterLiquid()
	{
		if (!GameResources.Ready)
		{
			throw new InvalidOperationException("公司液体资源尚未就绪");
		}
		AddLiquid("corp_isomer_raw", L.T("粗异构蛋白", "Raw Isomer Protein"), "9aa3b4", ref _ownedRaw);
		AddLiquid("corp_isomer", L.T("异构蛋白", "Isomer Protein"), "c8d0dc", ref _ownedIsomer);
	}

	private static void AddLiquid(string id, string name, string color, ref Liquid owned)
	{
		if (!Liquid.LiquidDict.ContainsKey(id))
		{
			string text = id.ToUpperInvariant();
			Liquid liquid = (owned = new Liquid
			{
				displayName = name,
				identifier = id,
				part = "PART_" + text,
				currentPart = "CURRENT_PART_" + text,
				canBeRemovedByPurifier = false,
				particleSize = 1,
				color = color,
				colorMultiplier = 2
			});
			Liquid.Liquids.Add(liquid);
			Liquid.LiquidDict[id] = liquid;
			ModLog.Info(name + "已加入原版液体表");
		}
	}

	private static int Parts(GameItem c, string id)
	{
		try
		{
			return c.GetTagReadonly(Liquid.GetCurrentPartFromId(id)).GetInt();
		}
		catch
		{
			return 0;
		}
	}

	private static int IsomerMl(GameItem c)
	{
		return Parts(c, "corp_isomer") / 1000;
	}

	private static int RawMl(GameItem c)
	{
		return Parts(c, "corp_isomer_raw") / 1000;
	}

	private static int TotalParts(GameItem c)
	{
		return WaterHelper.GetTotalVolume(c);
	}

	public static double Purity(GameItem c)
	{
		int num = TotalParts(c);
		if (num > 0)
		{
			return (double)Parts(c, "corp_isomer") / (double)num;
		}
		return 0.0;
	}

	public static void FixValue(GameItem c)
	{
		try
		{
			if (c != null && c.IsTag("LIQUID_CONTAINER_TAG") && Liquid.LiquidDict.ContainsKey("corp_isomer"))
			{
				int num = IsomerMl(c) + RawMl(c);
				if (num > 0)
				{
					c.unitValue = WaterFeatureHelper.GetWaterPrice(TotalParts(c)) + num * 6;
				}
			}
		}
		catch
		{
		}
	}

	public static void AfterUpdateWaterFeature(GameItem gameItem)
	{
		FixValue(gameItem);
	}

	public static void AfterLiquidChanged(GameItem target)
	{
		FixValue(target);
	}

	public static void AfterPourInto(GameItem target, GameItem source)
	{
		FixValue(target);
		FixValue(source);
	}

	private static GameItem LiquidShell(string id, int ml, int basePrice)
	{
		RegisterLiquid();
		GameItem gameItem = Empty();
		gameItem.identifier = id;
		WaterHelper.InitLiquidContainerItem(gameItem, ml, basePrice, isFilterContainer: false, isGraduated: true);
		AudioHelper.InitGlass(gameItem);
		return gameItem;
	}

	private static void FillBlood(GameItem item)
	{
		FillLiquids(item, ("corp_isomer_raw", 10.0), ("water", 28.0), ("protein", 5.0), ("fat", 3.0), ("microbe", 2.0), ("organic_waste", 2.0));
	}

	private static void FillLiquids(GameItem item, params (string Id, double Ml)[] parts)
	{
		for (int i = 0; i < parts.Length; i++)
		{
			var (liquidId, num) = parts[i];
			if (num > 0.0)
			{
				WaterHelper.AddLiquid(item, liquidId, (int)Math.Round(num * 1000.0));
			}
		}
		FixValue(item);
	}

	public static void AfterTemperatureReaction(GameItem container)
	{
		try
		{
			if (container == null || WaterHelper.IsEmpty(container) || !Liquid.LiquidDict.ContainsKey("corp_isomer_raw"))
			{
				return;
			}
			int num = Parts(container, "corp_isomer_raw");
			int num2 = Parts(container, "corp_isomer");
			if (num > 0 || num2 > 0)
			{
				double num3 = container.GetTagReadonly("CONTAINER_TEMP_TAG").GetDouble();
				int num4 = Parts(container, "reducing_agent");
				int num5 = Parts(container, "protein");
				int num6 = num;
				int num7 = num2;
				if (num > 0 && num3 > 85.0)
				{
					int num8 = Math.Min(num, Math.Max(200, (int)((double)num * 0.05)));
					num -= num8;
					num5 += num8;
				}
				else if (num > 0 && num4 > 0 && num3 >= 60.0)
				{
					bool flag = num3 >= 75.0;
					int num9 = Math.Min(Math.Min(num, num4 * 2), Math.Max(200, (int)((double)num * (flag ? 0.12 : 0.03))));
					int num10 = (flag ? (num9 / 5) : 0);
					num -= num9;
					num4 -= (num9 + 1) / 2;
					num2 += num9 - num10;
					num5 += num10;
				}
				if (num2 > 0 && num3 > 75.0)
				{
					int num11 = Math.Min(num2, Math.Max(100, (int)((double)num2 * 0.02)));
					num2 -= num11;
					num5 += num11;
				}
				if (num != num6 || num2 != num7)
				{
					SetParts(container, "corp_isomer_raw", num);
					SetParts(container, "corp_isomer", num2);
					SetParts(container, "reducing_agent", num4);
					SetParts(container, "protein", num5);
					WaterFeatureHelper.OnLiquidChanged(container, WaterFeatureHelper.GetWaterFeature(container).IsExposed());
				}
			}
		}
		catch
		{
		}
	}

	private static void SetParts(GameItem c, string id, int value)
	{
		c.ModifyTag(Liquid.GetCurrentPartFromId(id), (TagState s) =>
		{
			s.Enable().SetInt(Math.Max(0, value));
		}, skipValidate: true);
	}

	public static bool CanInject()
	{
		if (SentinelRaw())
		{
			CorpState corpState = CorpState.Get();
			if (corpState == null)
			{
				return true;
			}
			return !corpState.Meds.SentinelCured;
		}
		return false;
	}

	public static string Inject()
	{
		MedState meds = CorpState.Get().Meds;
		meds.RejuvShots++;
		try
		{
			PlayerStore.Instance.healthData.daysSinceSentinelInjection = 0;
		}
		catch
		{
		}
		if (meds.RejuvShots >= 3)
		{
			meds.SentinelCured = true;
			RemoveSentinelPerk();
			return L.T("你注射了回春药剂。（3/3）\n哨兵崩溃综合征的症状消失了。你再也不需要免疫宁了。", "You inject the Rejuvenation Serum. (3/3)\nThe symptoms of Sentinel Collapse Syndrome are gone. You'll never need Immunivax again.");
		}
		if (meds.RejuvShots != 1)
		{
			return L.T("你注射了回春药剂。（2/3）\n症状几乎感觉不到了。这一针也当作打过免疫宁。", "You inject the Rejuvenation Serum. (2/3)\nYou can barely feel the symptoms. This shot also counts as Immunivax.");
		}
		return L.T("你注射了回春药剂。（1/3）\n症状减轻了。这一针也当作打过免疫宁。", "You inject the Rejuvenation Serum. (1/3)\nThe symptoms ease. This shot also counts as Immunivax.");
	}

	private static bool SentinelRaw()
	{
		_raw = true;
		try
		{
			return HealthData.IsSentinel();
		}
		catch
		{
			return false;
		}
		finally
		{
			_raw = false;
		}
	}

	private static void RemoveSentinelPerk()
	{
		try
		{
			if ((PlayerStore.Instance?.startingPerks?.RemoveAll((StartingPerk p) => p?.id == "sentinel")).GetValueOrDefault() > 0)
			{
				ModLog.Info("哨兵崩溃综合征已从特性列表移除");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("移除哨兵崩溃综合征特性失败：" + ex.Message);
		}
	}

	public static void AfterIsSentinel(ref bool __result)
	{
		if (__result && !_raw)
		{
			CorpState corpState = CorpState.Get();
			if (corpState != null && corpState.Meds.SentinelCured)
			{
				__result = false;
			}
		}
	}

	public static void AfterIsPerkActive(string perkId, ref bool __result)
	{
		if (__result && perkId == "sentinel" && !_raw)
		{
			CorpState corpState = CorpState.Get();
			if (corpState != null && corpState.Meds.SentinelCured)
			{
				__result = false;
			}
		}
	}

	public static int SplitGel(GameItem gel)
	{
		GameInventory parentInventory = gel.parentInventory;
		int num = 0;
		for (int i = 0; i < 3; i++)
		{
			GameItem gameItem = Goods.Make("isomer_vial");
			if (gameItem != null)
			{
				double num2 = 0.5 + Rng.NextDouble() * 0.35;
				double num3 = 10.0 * (1.0 - num2);
				FillLiquids(gameItem, ("corp_isomer", 10.0 * num2), ("water", num3 * 0.6), ("protein", num3 * 0.25), ("organic_waste", num3 * 0.15));
				GeneralHelper.SetItemOwned(gameItem);
				if (!Placed(parentInventory, gameItem))
				{
					break;
				}
				num++;
			}
		}
		return num;
	}

	public static void AfterIsBuying(StoreClient __instance, GameItem gameItem, ref bool __result, bool __runOriginal)
	{
		if (CorpPresence.Malice.Envoy.IsProtectedBomb(gameItem))
		{
			__result = __runOriginal && CorpPresence.Malice.Envoy.CanReceiveBomb(__instance);
			return;
		}
		if (!__runOriginal)
		{
			return;
		}
		try
		{
			if (!__result && __instance != null && __instance.isAug && gameItem?.identifier == "corp_presence_implant_compat")
			{
				__result = true;
			}
		}
		catch
		{
		}
		Func<StoreClient, GameItem, bool> f = Ext.ClientBuys;
		if (!__result && f != null && __instance != null && gameItem != null)
		{
			__result = Ext.Ask("ClientBuys", () => f(__instance, gameItem), fallback: false);
		}
	}

	private static GameItem BuildReactor()
	{
		RegisterLiquid();
		GameSlotInventory battery = new GameSlotInventory();
		GameGridInventory modules = new GameGridInventory("10000010001000001110000010001000001", 7);
		GameSlotInventory tank = new GameSlotInventory();
		GameGridInventory output = new GameGridInventory(8, 4);
		GameSlotInventory gameSlotInventory = new GameSlotInventory();
		PixelWindow contentWindow = MachineHelper.CreateMachineryWindow(true, MachineHelper.PowerPanel(battery), MachineHelper.Panel(L.T("模组", "Modules"), modules, "module"), MachineHelper.Panel(L.T("溶液", "Solution"), tank, "input"), MachineHelper.Panel(L.T("成品", "Output"), output, "output"), MachineHelper.NotePanel(gameSlotInventory));
		MachineHelper.SetupManualSlot(gameSlotInventory);
		GameItem machine = Empty().SetContentWindow(contentWindow);
		machine.EnableTag("CONTAINER_TAG");
		machine.EnableTag("STANDARD_MACHINE_TAG");
		MachineHelper.SetupBatterySlot(battery, machine);
		MachineryHelper.InitMachinery(machine, 8);
		machine.ModifyTag("BASE_PROCESSING_SPEED_TAG", (TagState s) =>
		{
			s.Enable().SetInt(100);
		});
		machine.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", (TagState s) =>
		{
			s.Enable().SetInt(100);
		});
		MachineHelper.SetupModuleBay(modules, machine);
		tank.mayInventoryAddItemFunc = (GameItem item, GameInventory _) => GeneralHelper.IsItemOwned(item) && item.IsTag("LIQUID_CONTAINER_TAG") && !item.IsTag("WINE_CONTAINER_TAG");
		tank.SetBackgroundFadeSprite("Items/water_container4", "xl");
		output.mayInventoryAddItemFunc = (GameItem _, GameInventory _) => false;
		machine.onCycleEndSlotItemFunc = (GameItem _, GameInventory _, SlotMarker _) =>
		{
			React(machine, battery, modules, tank, output);
		};
		return machine;
	}

	private static string ProductFor(double purity)
	{
		if (!(purity >= 0.97))
		{
			if (!(purity >= 0.88))
			{
				if (!(purity >= 0.6))
				{
					return null;
				}
				return "isomer_gel";
			}
			return "implant_compat";
		}
		return "rejuvenation";
	}

	private static void React(GameItem machine, GameSlotInventory battery, GameGridInventory modules, GameSlotInventory tank, GameInventory output)
	{
		try
		{
			ModuleHelper.ComputeModuleEffect(modules.childItems.ToList(), machine);
			ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
			int num = Math.Max(0, machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt());
			int num2 = Math.Max(0, ModuleHelper.GetAjustedQuality(machine));
			num += 100 * num2 / 200;
			int num3 = machine.GetTagReadonly("CORP_SB_REACTOR_CARRY_INT").GetInt();
			GameItem childItem = tank.childItem;
			if (childItem == null || IsomerMl(childItem) < 30 || ProductFor(Purity(childItem)) == null)
			{
				machine.ModifyTag("CORP_SB_REACTOR_CARRY_INT", (TagState s) =>
				{
					s.Enable().SetInt(0);
				});
				return;
			}
			int budget = num + num3;
			int n = budget / 100;
			if (n <= 0)
			{
				machine.ModifyTag("CORP_SB_REACTOR_CARRY_INT", (TagState s) =>
				{
					s.Enable().SetInt(budget);
				});
			}
			else
			{
				if (!MachineHelper.TryDrawCyclePower(machine, battery))
				{
					return;
				}
				int made;
				for (made = 0; made < n; made++)
				{
					double num4 = Purity(childItem);
					string text = ProductFor(num4);
					if (text == null || IsomerMl(childItem) < 30)
					{
						break;
					}
					GameItem gameItem = Goods.Make(text);
					if (gameItem == null)
					{
						break;
					}
					GeneralHelper.SetItemOwned(gameItem);
					if (!ProductionOutput.TryAdd(output, gameItem))
					{
						gameItem.Destroy();
						break;
					}
					WaterHelper.RemoveMl(childItem, (int)Math.Ceiling(30.0 / Math.Max(0.01, num4)));
					FixValue(childItem);
				}
				int left = Math.Min(99, Math.Max(0, budget - made * 100));
				machine.ModifyTag("CORP_SB_REACTOR_CARRY_INT", (TagState s) =>
				{
					s.Enable().SetInt((made == n) ? left : 0);
				});
				if (made > 0)
				{
					MachineHelper.OnMachineActioned(machine, modules);
				}
				ModLog.Info($"反应釜：做了 {made} 份（速度 {num}）");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("反应釜失败：" + ex.Message);
		}
	}

	public static void OnSold(StoreClient client, GameItem item)
	{
		if (client != null && client.isAug && !(item?.identifier != "corp_presence_implant_compat"))
		{
			CompanyState companyState = CorpState.Get()?.CompanyOf("carmicheal");
			if (companyState != null)
			{
				companyState.ParcelsDue++;
				ModLog.Info("义体兼容剂卖给了改造体：明天收到匿名包裹");
			}
		}
	}

	public static void OnDayStart()
	{
		RegisterLiquid();
		RegisterSpecies();
		CorpState corpState = CorpState.Get();
		if (corpState != null && corpState.Meds.SentinelCured)
		{
			RemoveSentinelPerk();
		}
		CompanyState companyState = CorpState.Get()?.CompanyOf("carmicheal");
		if (companyState != null && companyState.ParcelsDue > 0)
		{
			int delivered = 0;
			SilverBloodFlow.DeliverPending(companyState.ParcelsDue, TryDeliverParcel, () =>
			{
				companyState.ParcelsDue--;
				delivered++;
			});
			if (delivered > 0)
			{
				Tell(L.T("门口放着一只没有寄件人的包裹。", "There's a parcel at the door with no sender."));
			}
		}
	}

	private static bool TryDeliverParcel()
	{
		GameItem gameItem = null;
		bool flag = false;
		try
		{
			gameItem = Goods.Make("anon_parcel");
			if (gameItem == null)
			{
				return false;
			}
			int num = 0;
			foreach (var (text, num2) in ParcelPool.OrderBy(((string Id, int Value) _) => Rng.Next()))
			{
				if (num >= 300)
				{
					break;
				}
				if (num + num2 > 420)
				{
					continue;
				}
				GameItem gameItem2 = ((text == "large_purple_injector") ? CarmichaelCo.MakeGenuine(text) : DirectoryMaster.Item(text, isOwned: false));
				if (gameItem2 != null)
				{
					if (Crates.Put(gameItem, gameItem2))
					{
						num += num2;
					}
					else
					{
						gameItem2.Destroy();
					}
				}
			}
			GameItem gameItem3 = DirectoryMaster.Item("postit", isOwned: false);
			if (gameItem3 != null)
			{
				gameItem3.name = L.T("没署名的纸条", "Unsigned Note");
				gameItem3.flavorText = L.T("感谢指令，通过，向你致谢。", "Gratitude directive: approved. Thank you.");
				gameItem3.SetGameItemType("DOCUMENT");
				gameItem3.unitValue = 0L;
				if (!Crates.Put(gameItem, gameItem3))
				{
					gameItem3.Destroy();
				}
			}
			if (num <= 0)
			{
				return false;
			}
			return flag = StorePlacement.Place(gameItem);
		}
		catch (Exception ex)
		{
			ModLog.Warn("银血奖励包裹发放失败，保留待发数量：" + ex.Message);
			return flag = gameItem != null && gameItem.parentInventory != null;
		}
		finally
		{
			if (!flag && gameItem != null && gameItem.parentInventory == null && !StorePlacement.Destroyed(gameItem))
			{
				gameItem.Destroy();
			}
		}
	}

	internal static void Tell(string text)
	{
		try
		{
			StoreUIManager.Instance?.Notify(text);
		}
		catch
		{
		}
	}
}
