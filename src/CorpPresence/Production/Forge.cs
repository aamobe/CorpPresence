using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static class Forge
{
	private sealed record Blade(string Goods, int Billets);

	public const string BilletId = "corp_presence_forged_billet";

	public const string FurnaceId = "corp_presence_forge_furnace";

	public const string AnvilId = "corp_presence_forge_anvil";

	public const string Furnace = "@forge_furnace";

	public const string Anvil = "@forge_anvil";

	public const int FurnacePower = 15;

	public const int BilletsPerNight = 2;

	public const int BilletPower = 6;

	public const int WaterPerBillet = 100;

	public const float DaggerButcherBonus = 1.15f;

	private const int SpeedScale = 100;

	private const string CarryTag = "CORP_FORGE_CARRY_INT";

	private const string BladeTag = "CORP_FORGE_BLADE_INT";

	private const string Ingot = "metal_ingot";

	private static readonly string[] GoodPurity = new string[3] { "INGOT_PURITY_HIGH", "INGOT_PURITY_VERY_HIGH", "INGOT_PURITY_PERFECT" };

	private static readonly Blade[] Blades = new Blade[3]
	{
		new Blade("custom_dagger", 1),
		new Blade("custom_saber", 2),
		new Blade("custom_machete", 3)
	};

	internal static readonly Dictionary<string, Goods.Def> Defs = new Dictionary<string, Goods.Def>
	{
		["forged_billet"] = new Goods.Def
		{
			Name = "精锻钢胚",
			NameEn = "Forged Billet",
			Value = 90L,
			Company = "forge",
			Art = "forged_billet",
			Fallback = "metal_ingot",
			Types = new string[1] { "MATERIAL" },
			W = 2,
			H = 1,
			Shell = Empty,
			Setup = InitBillet,
			Desc = "高纯度的金属锭在精锻熔炉里烧一夜炼成。放上锻台：短刀 1 块，军刀 2 块，砍刀 3 块。",
			DescEn = "A high-purity metal ingot fired overnight in a precision forge. On the anvil: 1 for a dagger, 2 for a saber, 3 for a machete.",
			Flavor = "表面还留着锤痕。",
			FlavorEn = "Hammer marks still on the surface."
		},
		["custom_dagger"] = new Goods.Def
		{
			Name = "定制短刀",
			NameEn = "Custom Dagger",
			Value = 130L,
			Company = "forge",
			Art = "custom_dagger",
			Fallback = "skinning_knife",
			Types = new string[2] { "WEAPON", "MELEE_WEAPON" },
			W = 4,
			H = 1,
			Shell = Empty,
			Setup = InitDagger,
			Desc = "手工锻的短刀。拿来宰牲畜，按体重多出 15% 的肉。",
			DescEn = "A hand-forged dagger. Butchering with it yields 15% more meat by weight.",
			Flavor = "窄长的单刃，弧形收尖，握柄是收腰的工程塑料。",
			FlavorEn = "A narrow single edge curving to a point, with a waisted polymer grip."
		},
		["custom_saber"] = new Goods.Def
		{
			Name = "定制军刀",
			NameEn = "Custom Saber",
			Value = 280L,
			Company = "forge",
			Art = "custom_saber",
			Fallback = "combat_knife_2",
			Types = new string[2] { "WEAPON", "MELEE_WEAPON" },
			W = 7,
			H = 1,
			Shell = Empty,
			Setup = InitBlade,
			Desc = "手工锻的刺刀式军刀，刀背带锯齿，有环形护手。",
			DescEn = "A hand-forged bayonet-style saber with a sawback spine and a ring guard.",
			Flavor = "截尖的宽刀身，握柄是一节节的模压分段。",
			FlavorEn = "A broad clipped blade on a segmented molded grip."
		},
		["custom_machete"] = new Goods.Def
		{
			Name = "定制砍刀",
			NameEn = "Custom Machete",
			Value = 450L,
			Company = "forge",
			Art = "custom_machete",
			Fallback = "combat_machete",
			Types = new string[2] { "WEAPON", "MELEE_WEAPON" },
			W = 9,
			H = 2,
			Shell = Empty,
			Setup = InitBlade,
			Desc = "手工锻的长砍刀，直刀身，长柄。",
			DescEn = "A hand-forged long machete with a straight blade and a long handle.",
			Flavor = "三块钢胚打成的，挥起来带风。",
			FlavorEn = "Three billets went into this one. You can hear it cut the air."
		},
		["forge_furnace"] = new Goods.Def
		{
			Name = "精锻熔炉",
			NameEn = "Precision Forge",
			Value = 400L,
			Company = "forge",
			Art = "forge_furnace",
			Fallback = "furnace",
			Types = new string[1] { "MACHINE" },
			W = 2,
			H = 2,
			Shape = "##|##",
			Shell = BuildFurnace,
			OnNew = (GameItem m) =>
			{
				Tobacco.AddManual(m, FurnaceManual);
			},
			Desc = "放进高纯度以上的金属锭、插上电池，每晚把 1 块锭炼成 1 块精锻钢胚，默认每晚 2 块，耗 15 电。性能模组多炼（也多用锭），效率模组省电。",
			DescEn = "Load high-purity or better metal ingots and a battery. Each night it turns 1 ingot into 1 forged billet, 2 per night by default, using 15 charge. Performance modules make more (using more ingots); efficiency modules save power.",
			Flavor = "罗森的旧炉子。炉膛里的火一夜都不灭。",
			FlavorEn = "Rosen's old forge. The fire in it burns all night."
		},
		["forge_anvil"] = new Goods.Def
		{
			Name = "锻台",
			NameEn = "Forging Anvil",
			Value = 300L,
			Company = "forge",
			Art = "forge_anvil",
			Fallback = "printer_off",
			Types = new string[2] { "MACHINE", "LOWTECH" },
			W = 3,
			H = 2,
			Shape = "###|###",
			Shell = BuildAnvil,
			OnNew = (GameItem m) =>
			{
				Tobacco.AddManual(m, AnvilManual);
			},
			Desc = "放进精锻钢胚、水箱里放装水的容器，选好款式按“锻造”：短刀 1 块、军刀 2 块、砍刀 3 块钢胚，每块用水 100 毫升、耗 6 电。水越干净，刀的成色越好。",
			DescEn = "Load forged billets and a water container, pick a style and press Forge: 1 billet for a dagger, 2 for a saber, 3 for a machete; 100 ml of water and 6 charge per billet. The cleaner the water, the better the blade.",
			Flavor = "机械锻锤压着铁砧，旁边挂着一只淬火水槽。",
			FlavorEn = "A power hammer over an anvil, with a quench tank hanging off the side."
		}
	};

	[ThreadStatic]
	private static bool _dagger;

	public static bool DaggerActive => _dagger;

	private static (string Zh, string En) FurnaceManual => (Zh: "放入：把金属锭放进锭槽（纯度要“高”以上，可用放大镜、手电查看），把能量电池放进电池槽。\n精锻：每晚把 1 块锭炼成 1 块精锻钢胚，默认每晚 2 块，耗 15 电。\n[模组影响]：\n效率影响功耗\n性能影响每晚块数（也多用锭）\n质量无影响", En: "Load: put metal ingots in the ingot tray (High purity or better; check with a magnifier or flashlight) and an energy cell in the battery slot.\nForging: each night 1 ingot becomes 1 forged billet, 2 per night by default, using 15 charge.\n[Module effects]:\nEfficiency affects power use\nPerformance affects billets per night (and uses more ingots)\nQuality has no effect");

	private static (string Zh, string En) AnvilManual => (Zh: "放入：把精锻钢胚放进钢胚槽，把装水的容器放进水箱，把能量电池放进电池槽。\n锻造：选好款式按“锻造”。短刀 1 块、军刀 2 块、砍刀 3 块钢胚；每块用水 100 毫升、耗 6 电。\n淬火：水的等级决定成色。\n纯水 → 全新\n优质水 → 优秀\n基础水 → 良好\n幽灵水、铁锈水 → 一般\n沟渠水 → 差\n优秀以上的刀算奢侈品。", En: "Load: put forged billets in the billet tray, a water container in the quench tank, and an energy cell in the battery slot.\nForge: pick a style and press Forge. Dagger 1 billet, saber 2, machete 3; 100 ml of water and 6 charge per billet.\nQuench: the water grade sets the condition.\nPure → New\nPremium → Excellent\nBasewater → Good\nGhostwater, Rustwater → Fair\nGutterflow → Poor\nExcellent or better counts as a luxury item.");

	private static GameItem Empty()
	{
		return ItemDirectory.CreateEmptyItem();
	}

	private static void InitBillet(GameItem item)
	{
		AudioHelper.InitSolidMetal(item);
	}

	private static void InitBlade(GameItem item)
	{
		HusbandryHelper.InitBladeItem(item);
	}

	private static void InitDagger(GameItem item)
	{
		InitBlade(item);
		Action<GameItem, GameItem> inner = item.onThisTargetItemFunc;
		item.onThisTargetItemFunc = (GameItem s, GameItem t) =>
		{
			_dagger = true;
			try
			{
				inner?.Invoke(s, t);
			}
			finally
			{
				_dagger = false;
			}
		};
	}

	public static void BeforeButcherCuts(GameItem gameItem, ref float __state)
	{
		__state = -1f;
		if (!_dagger || gameItem == null)
		{
			return;
		}
		try
		{
			float w = gameItem.GetTagReadonly("ANIMAL_WEIGHT_TAG").GetFloat();
			__state = w;
			gameItem.ModifyTag("ANIMAL_WEIGHT_TAG", (TagState s) =>
			{
				s.Enable().SetFloat(w * 1.15f);
			});
		}
		catch (Exception ex)
		{
			ModLog.Warn("定制短刀屠宰加成失败：" + ex.Message);
		}
	}

	public static void AfterButcherCuts(GameItem gameItem, float __state)
	{
		if (__state < 0f || gameItem == null)
		{
			return;
		}
		try
		{
			gameItem.ModifyTag("ANIMAL_WEIGHT_TAG", (TagState s) =>
			{
				s.Enable().SetFloat(__state);
			});
		}
		catch
		{
		}
	}

	public static void ApplyLuxury(GameItem item)
	{
		try
		{
			if (item != null && Blades.Any((Blade b) => ItemIdentity.GoodsKey(item.identifier, item.spritePath) == b.Goods))
			{
				string text = item.FindItemFeatureByCategory("CATEGORY_EQUIPMENT_CONDITION")?.GetCurrentItemCondition()?.identifier;
				if (text == "equipment_condition_new" || text == "equipment_condition_excellent")
				{
					item.SetGameItemType("LUXURY_ITEM");
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("定制刀奢侈品分类失败：" + ex.Message);
		}
	}

	public static void AfterStoreLoaded()
	{
		if (SaveSlot.CurrentStore == null || EmporiumEntry.Instance == null)
		{
			return;
		}
		foreach (GameItem allItem in EmporiumEntry.Instance.GetAllItems())
		{
			ApplyLuxury(allItem);
		}
	}

	private static bool IsGoodIngot(GameItem item)
	{
		try
		{
			return item?.identifier == "metal_ingot" && GoodPurity.Contains(IngotPurityHelper.GetPurity(item));
		}
		catch
		{
			return false;
		}
	}

	private static GameItem BuildFurnace()
	{
		GameSlotInventory battery = new GameSlotInventory();
		GameGridInventory modules = new GameGridInventory("10000010001000001110000010001000001", 7);
		GameGridInventory input = new GameGridInventory(6, 4);
		GameGridInventory output = new GameGridInventory(6, 4);
		GameSlotInventory gameSlotInventory = new GameSlotInventory();
		PixelWindow contentWindow = MachineHelper.CreateMachineryWindow(true, MachineHelper.PowerPanel(battery), MachineHelper.Panel(L.T("模组", "Modules"), modules, "module"), MachineHelper.Panel(L.T("金属锭", "Ingots"), input, "input"), MachineHelper.Panel(L.T("钢胚", "Billets"), output, "output"), MachineHelper.NotePanel(gameSlotInventory));
		MachineHelper.SetupManualSlot(gameSlotInventory);
		GameItem machine = Empty().SetContentWindow(contentWindow);
		machine.EnableTag("CONTAINER_TAG");
		machine.EnableTag("STANDARD_MACHINE_TAG");
		MachineHelper.SetupBatterySlot(battery, machine);
		MachineryHelper.InitMachinery(machine, 15);
		machine.ModifyTag("BASE_PROCESSING_SPEED_TAG", (TagState s) =>
		{
			s.Enable().SetInt(200);
		});
		machine.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", (TagState s) =>
		{
			s.Enable().SetInt(200);
		});
		MachineHelper.SetupModuleBay(modules, machine);
		input.mayInventoryAddItemFunc = (GameItem item, GameInventory _) => GeneralHelper.IsItemOwned(item) && IsGoodIngot(item);
		output.mayInventoryAddItemFunc = (GameItem _, GameInventory _) => false;
		MachineUpgrade.Attach(machine, 2, input, output);
		machine.onCycleEndSlotItemFunc = (GameItem _, GameInventory _, SlotMarker _) =>
		{
			Smelt(machine, battery, modules, input, output);
		};
		return machine;
	}

	private static void Smelt(GameItem machine, GameSlotInventory battery, GameGridInventory modules, GameInventory input, GameInventory output)
	{
		try
		{
			machine.ModifyTag("MACHINERY_DRAWN_POWER_BASE", (TagState s) =>
			{
				s.Enable().SetInt(15);
			});
			ModuleHelper.ComputeModuleEffect(modules.childItems.ToList(), machine);
			ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
			int num = Math.Max(0, machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt());
			int num2 = machine.GetTagReadonly("CORP_FORGE_CARRY_INT").GetInt();
			List<GameItem> list = input.childItems.Where(IsGoodIngot).ToList();
			int num3 = num + num2;
			int n = Math.Min(num3 / 100, list.Count);
			if (n <= 0 || !MachineHelper.TryDrawCyclePower(machine, battery))
			{
				return;
			}
			int made;
			for (made = 0; made < n; made++)
			{
				GameItem gameItem = Goods.Make("forged_billet");
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
				list[made].Destroy();
			}
			int left = Math.Min(99, Math.Max(0, num3 - made * 100));
			machine.ModifyTag("CORP_FORGE_CARRY_INT", (TagState s) =>
			{
				s.Enable().SetInt((made == n) ? left : 0);
			});
			if (made > 0)
			{
				MachineHelper.OnMachineActioned(machine, modules);
			}
			ModLog.Info($"精锻熔炉：炼了 {made} 块钢胚（速度 {num}，耗电 {MachineryHelper.GetMachinePowerUsage(machine)}）");
		}
		catch (Exception ex)
		{
			ModLog.Warn("精锻失败：" + ex.Message);
		}
	}

	private static GameItem BuildAnvil()
	{
		GameSlotInventory battery = new GameSlotInventory();
		GameGridInventory input = new GameGridInventory(6, 3);
		GameSlotInventory water = new GameSlotInventory();
		GameGridInventory output = new GameGridInventory(9, 4);
		GameSlotInventory gameSlotInventory = new GameSlotInventory();
		GameItem machine = null;
		MachineryRadioButton[] radios = new MachineryRadioButton[Blades.Length];
		string[] array = L.T(new string[3] { "短刀", "军刀", "砍刀" }, new string[3] { "Dagger", "Saber", "Machete" });
		MachineryRadioGroup machineryRadioGroup = new MachineryRadioGroup();
		for (int i = 0; i < Blades.Length; i++)
		{
			int k = i;
			radios[i] = new MachineryRadioButton(array[i], i == 0, (bool on) =>
			{
				if (on)
				{
					machine?.ModifyTag("CORP_FORGE_BLADE_INT", (TagState s) =>
					{
						s.Enable().SetInt(k);
					});
				}
			}).SetDescription(L.T($"{Blades[i].Billets} 块钢胚，{Blades[i].Billets * 100} 毫升水", L.Translate((System.FormattableString)$"{Blades[i].Billets} billet(s), {Blades[i].Billets * 100} ml of water")));
			machineryRadioGroup.Add(radios[i]);
		}
		MachineryButton machineryButton = new MachineryButton(L.T("锻造", "Forge"));
		PixelWindow pixelWindow = MachineHelper.CreateMachineryWindow(true, MachineHelper.PowerPanel(battery), MachineHelper.Panel(L.T("钢胚", "Billets"), input, "input"), MachineHelper.Panel(L.T("水箱", "Quench tank"), water, "input"), MachineHelper.Panel(L.T("款式", "Style"), MachineryStack.Vertical(radios[0], radios[1], radios[2], machineryButton), "control"), MachineHelper.Panel(L.T("成品", "Output"), output, "output"), MachineHelper.NotePanel(gameSlotInventory));
		MachineHelper.SetupManualSlot(gameSlotInventory);
		machine = Empty().SetContentWindow(pixelWindow);
		machine.EnableTag("CONTAINER_TAG");
		MachineHelper.SetupBatterySlot(battery, machine);
		MachineryHelper.InitMachinery(machine);
		input.mayInventoryAddItemFunc = (GameItem item, GameInventory _) => GeneralHelper.IsItemOwned(item) && item.identifier == "corp_presence_forged_billet";
		water.mayInventoryAddItemFunc = (GameItem item, GameInventory _) => GeneralHelper.IsItemOwned(item) && item.IsTag("LIQUID_CONTAINER_TAG") && !item.IsTag("WINE_CONTAINER_TAG");
		water.SetBackgroundFadeSprite("Items/water_container4", "xl");
		output.mayInventoryAddItemFunc = (GameItem _, GameInventory _) => false;
		Action sync = () =>
		{
			radios[Math.Max(0, Math.Min(Blades.Length - 1, machine.GetTagReadonly("CORP_FORGE_BLADE_INT").GetInt()))].SetOn(on: true);
		};
		machine.onLoaded = (Action<GameItem>)Delegate.Combine(machine.onLoaded, (Action<GameItem>)((GameItem _) =>
		{
			sync();
		}));
		pixelWindow.openCallback = (Action)Delegate.Combine(pixelWindow.openCallback, sync);
		machineryButton.SetCallback(() =>
		{
			Strike(machine, battery, input, water, output);
		});
		return machine;
	}

	private static ItemFeatureList.EquipmentCondition ConditionOf(int grade)
	{
		switch (grade)
		{
		case 0:
			return ItemFeatureList.EquipmentCondition.New;
		case 1:
			return ItemFeatureList.EquipmentCondition.Excellent;
		case 2:
			return ItemFeatureList.EquipmentCondition.Good;
		case 3:
		case 4:
			return ItemFeatureList.EquipmentCondition.Fair;
		default:
			return ItemFeatureList.EquipmentCondition.Poor;
		}
	}

	private static void Strike(GameItem machine, GameSlotInventory battery, GameInventory input, GameSlotInventory water, GameInventory output)
	{
		try
		{
			Blade blade = Blades[Math.Max(0, Math.Min(Blades.Length - 1, machine.GetTagReadonly("CORP_FORGE_BLADE_INT").GetInt()))];
			List<GameItem> list = input.childItems.Where((GameItem i) => i?.identifier == "corp_presence_forged_billet").ToList();
			if (list.Count < blade.Billets)
			{
				Tell(L.T($"钢胚不够，要 {blade.Billets} 块", L.Translate((System.FormattableString)$"Not enough billets: need {blade.Billets}")));
				return;
			}
			GameItem childItem = water.childItem;
			int num = blade.Billets * 100;
			if (childItem == null || WaterHelper.GetCurrentCapacityML(childItem) < num)
			{
				Tell(L.T($"水箱里的水不够，要 {num} 毫升", L.Translate((System.FormattableString)$"Not enough water in the tank: need {num} ml")));
				return;
			}
			GameItem childItem2 = battery.childItem;
			int num2 = blade.Billets * (6 - MachineUpgrade.PowerCut(machine));
			if (childItem2 == null || !PowerHelper.CanDrawPowerSource(childItem2, num2))
			{
				Tell(L.T($"电量不够，要 {num2} 电", L.Translate((System.FormattableString)$"Not enough charge: need {num2}")));
				return;
			}
			ItemFeatureList.EquipmentCondition equipmentCondition = ConditionOf(WaterFeatureHelper.GetPurityArrayIndex(WaterHelper.GetWaterPurity(childItem)));
			GameItem gameItem = Goods.Make(blade.Goods);
			if (gameItem == null)
			{
				return;
			}
			gameItem.RemoveFeatureByCategory("CATEGORY_EQUIPMENT_CONDITION");
			gameItem.AddItemFeature(ItemFeatureList.EquipementConditionFeature(equipmentCondition));
			ApplyLuxury(gameItem);
			GeneralHelper.SetItemOwned(gameItem);
			if (!ProductionOutput.TryAdd(output, gameItem))
			{
				gameItem.Destroy();
				Tell(L.T("成品槽放不下了", "The output tray is full"));
				return;
			}
			for (int num3 = 0; num3 < blade.Billets; num3++)
			{
				list[num3].Destroy();
			}
			WaterHelper.RemoveMl(childItem, num);
			PowerHelper.DrawPowerSource(childItem2, num2);
			ModLog.Info($"锻台：锻了一把 {blade.Goods}（{equipmentCondition}），用水 {num} 毫升");
		}
		catch (Exception ex)
		{
			ModLog.Warn("锻造失败：" + ex.Message);
		}
	}

	private static void Tell(string text)
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



