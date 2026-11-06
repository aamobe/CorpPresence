using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class Canning
{
	private sealed record Recipe(string Mince, int MinceN, int FruitN, Func<GameItem> Make, int OutN);

	public const string FineId = "corp_presence_fine_mince";

	public const string CoarseId = "corp_presence_coarse_mince";

	public const string PasteId = "corp_presence_fruit_paste";

	// 营养果糊：每 240 热量 1 份（1 个普通营养果），每份耗 1 电
	public const int PasteKcal = 240;

	public const int PastePower = 1;

	public const string CanId = "corp_presence_fresh_meat_can";

	public const string GrinderId = "corp_presence_meat_grinder";

	public const string CannerId = "corp_presence_canning_machine";

	public const string Grinder = "@meat_grinder";

	public const string Canner = "@canning_machine";

	public const int MinceKcal = 275;

	public const int CoarseKcal = 200;

	public const int MincePower = 2;

	public const int CannerPower = 8;

	public const int BatchesPerNight = 2;

	private const int SpeedScale = 100;

	private const string Peat = "processed_meat";

	private const string Fruit = "nutrifruit";

	private static readonly Recipe Synthetic = new Recipe("corp_presence_coarse_mince", 1, 3, () => DirectoryMaster.Item("processed_meat", isOwned: false), 4);

	private static readonly Recipe Fresh = new Recipe("corp_presence_fine_mince", 2, 1, () => Goods.Make("fresh_meat_can"), 1);

	private const string FreshTag = "CORP_CAN_FRESH_BOOL";

	private const string CarryTag = "CORP_CAN_CARRY_INT";

	private const string MeatType = "MEAT_TYPE_TAG";

	private const string Prion = "ANIMAL_PRION_TAG";

	internal static readonly Dictionary<string, Goods.Def> Defs = new Dictionary<string, Goods.Def>
	{
		["fish_mince"] = SeafoodFood("鱼肉糜", "Fish Mince", 29, "fish_mince", 200, false),
		["crab_stick"] = SeafoodFood("风味蟹肉棒", "Crab-flavored Stick", 195, "crab_stick", 800, false, luxury: true),
		["fish_can"] = SeafoodFood("达夫雷鱼罐头", "Davre Canned Fish", 125, "fish_can", 600, true, luxury: true),
		["seafood_sealer"] = SeafoodSealerDef(),
		["fine_mince"] = new Goods.Def
		{
			Name = "精肉糜",
			NameEn = "Prime Mince",
			Value = 32L,
			Company = "davre",
			Art = "minced_meat",
			Fallback = "morsel",
			Types = new string[1] { "FOOD" },
			W = 2,
			H = 1,
			Shell = Empty,
			Setup = InitFine,
			Desc = "标准和小块生肉绞成。放进罐装机的鲜肉档，2 份加 1 份营养果糊封成 1 罐鲜肉罐头。",
			DescEn = "Ground from standard and small cuts of raw meat. In a canning machine on Fresh mode, 2 portions and 1 portion of nutrifruit paste make 1 can of fresh meat.",
			Flavor = "颜色鲜红，带一点白色的脂肪。",
			FlavorEn = "Bright red, flecked with a little white fat."
		},
		["coarse_mince"] = new Goods.Def
		{
			Name = "杂肉糜",
			NameEn = "Scrap Mince",
			Value = 30L,
			Company = "davre",
			Art = "coarse_minced_meat",
			Fallback = "morsel",
			Types = new string[1] { "FOOD" },
			W = 2,
			H = 1,
			Shell = Empty,
			Setup = InitCoarse,
			Desc = "碎块肉绞成。放进罐装机的合成档，1 份加 3 份营养果糊封成 4 罐“泥炭”。",
			DescEn = "Ground from meat morsels. In a canning machine on Synthetic mode, 1 portion and 3 portions of nutrifruit paste make 4 cans of Peat.",
			Flavor = "颜色发暗，里面混着筋膜和碎渣。",
			FlavorEn = "Dark, with gristle and bits mixed in."
		},
		["fruit_paste"] = new Goods.Def
		{
			Name = "营养果糊",
			NameEn = "Nutrifruit Paste",
			Value = 12L,
			Company = "davre",
			Art = "nutrifruit_paste",
			Fallback = "morsel",
			Types = new string[1] { "FOOD" },
			W = 2,
			H = 1,
			Shell = Empty,
			Setup = InitPaste,
			Desc = "营养果放进绞肉机绞成，按热量折算：每 240 热量 1 份，大小果都能绞。罐装机只收果糊，不收整个的营养果。",
			DescEn = "Made by putting nutrifruit through a meat grinder, by calories: 1 portion per 240 calories, any size of fruit. Canning machines take paste only, not whole nutrifruit.",
			Flavor = "黄乎乎的一团，闻起来像煮过头的土豆。",
			FlavorEn = "A yellowish lump that smells like overboiled potatoes."
		},
		["fresh_meat_can"] = new Goods.Def
		{
			Name = "达夫雷鲜肉罐头",
			NameEn = "Davre Fresh Meat",
			Value = 125L,
			Company = "davre",
			Art = "fresh_meat_can",
			Fallback = "processed_meat",
			Manufacturer = "davre",
			Types = new string[3] { "FOOD", "PROCESSED_FOOD", "LUXURY_ITEM" },
			W = 2,
			H = 2,
			Shell = Empty,
			Setup = InitCan,
			Desc = "据说每个人吃下的第一口都会（太空脏话）",
			DescEn = "They say everyone's first bite makes them go (space expletive).",
			Flavor = "和“泥炭”同一条罐装线，装的是真肉。达夫雷可不往下层区卖这种“宝贝”。",
			FlavorEn = "Same canning line as Peat, but this one has real meat in it. Davre doesn't sell this kind of \"treasure\" in the Lower Levels."
		},
		["meat_grinder"] = new Goods.Def
		{
			Name = "绞肉机",
			NameEn = "Meat Grinder",
			Value = 250L,
			Company = "davre",
			Art = "meat_grinder",
			Fallback = "gibber",
			Manufacturer = "davre",
			Types = new string[2] { "MACHINE", "LOWTECH" },
			W = 2,
			H = 3,
			Shell = BuildGrinder,
			OnNew = (GameItem m) =>
			{
				Tobacco.AddManual(m, GrinderManual);
			},
			Desc = "放进生肉或营养果、插上电池，按“绞碎”当场绞好：标准、小块生肉出精肉糜，碎块、小碎块出杂肉糜，营养果出营养果糊。精肉糜每 275 热量 1 份，杂肉糜每 200 热量 1 份，每份耗 2 电；营养果糊每 240 热量 1 份，每份耗 1 电。",
			DescEn = "Load raw meat or nutrifruit and a battery, then press Grind: standard and small cuts become prime mince, morsels become scrap mince, nutrifruit becomes nutrifruit paste. Prime mince takes 275 calories per portion and scrap mince 200, 2 charge each; paste takes 240 calories per portion, 1 charge each.",
			Flavor = "达夫雷下发的商用绞肉机。刀口上还有没擦干净的油。",
			FlavorEn = "A commercial grinder issued by Davre. There's still grease on the blades."
		},
		["canning_machine"] = new Goods.Def
		{
			Name = "罐装机",
			NameEn = "Canning Machine",
			Value = CannerBaseValue,
			Company = "davre",
			Art = "canning_machine",
			Fallback = "furnace",
			Manufacturer = "davre",
			Types = new string[1] { "MACHINE" },
			W = 3,
			H = 3,
			// Keep the pre-remaster footprint when art alpha changes.
			Shape = "##.|###|###",
			Shell = BuildCanner,
			OnNew = (GameItem m) =>
			{
				Tobacco.AddManual(m, CannerManual);
			},
			Desc = "放进肉糜和营养果糊、插上电池，每晚封罐，默认每晚 2 批，耗 8 电。合成档：3 营养果糊 + 1 杂肉糜 → 4 罐“泥炭”。鲜肉档：2 精肉糜 + 1 营养果糊 → 1 罐鲜肉罐头。性能模组多做批次（也多用原料），效率模组省电。",
			DescEn = "Load mince, nutrifruit paste and a battery; it cans overnight, 2 batches per night by default, using 8 charge. Synthetic: 3 nutrifruit paste + 1 scrap mince → 4 cans of Peat. Fresh: 2 prime mince + 1 nutrifruit paste → 1 can of fresh meat. Performance modules run more batches (using more ingredients); efficiency modules save power.",
			Flavor = "达夫雷为了和帕斯泰尔抢食品市场，下发给小店的商用封罐设备。",
			FlavorEn = "Commercial canning equipment Davre hands out to small shops in its food war with Pastel."
		}
	};

	private static (string Zh, string En) GrinderManual => (
		Zh: "把生肉或营养果放进原料槽，接上电源，再按「绞碎」。标准、小块生肉出精肉糜，碎块、小碎块出杂肉糜。精肉糜每 275 千卡出 1 份，杂肉糜每 200 千卡出 1 份，各耗 2 电。\n营养果糊每 240 千卡出 1 份，耗 1 电。槽位或电量不足时，果糊分批产出，剩余热量保存在机器里，可在「果糊热量储存」查看。取走成品或补充电量后，再按「绞碎」即可继续。不足 240 千卡的零头会与下次放入的果子合算。\n带朊病毒的肉绞出的肉糜也带朊病毒。",
		En: "Load raw meat or nutrifruit and connect a power source, then press Grind. Standard and small cuts make prime mince, and morsels make scrap mince. Prime mince needs 275 kcal per portion and scrap mince needs 200 kcal. Both cost 2 charge per portion.\nNutrifruit paste needs 240 kcal and 1 charge per portion. When space or power runs short, the grinder makes a partial batch and shows the remaining calories under Paste Reserve. Clear the output or restore power and press Grind again. Any reserve under 240 kcal carries over to the next fruit.\nMince from prion-infected meat carries prions too.");

	private static (string Zh, string En) CannerManual => (Zh: "放入：把肉糜放进肉糜槽、营养果糊放进果糊槽，把能量电池放进电池槽。整个的营养果要先用绞肉机绞成果糊。\n档位：\n合成档：3 营养果糊 + 1 杂肉糜 → 4 罐“泥炭”\n鲜肉档：2 精肉糜 + 1 营养果糊 → 1 罐鲜肉罐头\n每晚封罐，默认 2 批，耗 8 电。不对档的肉糜留在槽里不动。成品槽放满就停。\n[模组影响]：\n效率影响功耗\n性能影响每晚批数（也多用原料）\n质量无影响", En: "Load: put mince in the mince tray, nutrifruit paste in the paste tray, and an energy cell in the battery slot. Whole nutrifruit must be ground into paste first.\nModes:\nSynthetic: 3 nutrifruit paste + 1 scrap mince → 4 cans of Peat\nFresh: 2 prime mince + 1 nutrifruit paste → 1 can of fresh meat\nCans overnight, 2 batches by default, using 8 charge. Mince for the other mode stays in the tray. Stops when the output is full.\n[Module effects]:\nEfficiency affects power use\nPerformance affects batches per night (and uses more ingredients)\nQuality has no effect");

	public static int KcalOf(string minceId)
	{
		if (minceId == FishMinceId)
		{
			return 200;
		}
		if (minceId == PasteId)
		{
			return PasteKcal;
		}
		if (!(minceId == "corp_presence_coarse_mince"))
		{
			return 275;
		}
		return 200;
	}

	private static string PoolKcal(string mince)
	{
		return "CORP_GRIND_" + Grade(mince) + "_KCAL_INT";
	}

	private static string PoolSpecies(string mince)
	{
		return "CORP_GRIND_" + Grade(mince) + "_SPECIES";
	}

	private static string PoolPrion(string mince)
	{
		return "CORP_GRIND_" + Grade(mince) + "_PRION_BOOL";
	}

	private static string Grade(string mince)
	{
		if (mince == FishMinceId)
		{
			return "FISH";
		}
		if (mince == PasteId)
		{
			return "FRUIT";
		}
		if (!(mince == "corp_presence_fine_mince"))
		{
			return "COARSE";
		}
		return "FINE";
	}

	private static GameItem Empty()
	{
		return ItemDirectory.CreateEmptyItem();
	}

	private static void InitFine(GameItem item)
	{
		InitMince(item, 275);
	}

	private static void InitCoarse(GameItem item)
	{
		InitMince(item, 200);
	}

	private static void InitMince(GameItem item, int kcal)
	{
		AudioHelper.InitFoodAudio(item);
		FoodItemHelper.InitFoodItem(item, kcal, isCanned: false, isMeat: true);
	}

	private static void InitPaste(GameItem item)
	{
		AudioHelper.InitFoodAudio(item);
		FoodItemHelper.InitFoodItem(item, PasteKcal, isCanned: false, isMeat: false);
	}

	private static void InitCan(GameItem item)
	{
		AudioHelper.InitCannedFood(item);
		FoodItemHelper.InitFoodItem(item, 800, isCanned: true, isMeat: true);
	}

	private static string MinceOf(GameItem meat)
	{
		switch (meat?.identifier)
		{
		case "raw_meat":
		case "small_raw_meat":
			return "corp_presence_fine_mince";
		case "morsel":
		case "small_morsel":
			return "corp_presence_coarse_mince";
		case "nutrifruit":
			return PasteId;
		case "tiny_fish_fillet":
		case "small_fish_fillet":
		case "fish_fillet":
		case "large_fish_fillet":
			return GutterflowPelletHelper.IsTainted(meat) ? null : FishMinceId;
		default:
			return null;
		}
	}

	private static GameItem BuildGrinder()
	{
		GameSlotInventory battery = new GameSlotInventory();
		GameGridInventory input = new GameGridInventory(6, 4);
		GameGridInventory output = new GameGridInventory(6, 4);
		GameSlotInventory gameSlotInventory = new GameSlotInventory();
		MachineryButton machineryButton = new MachineryButton(L.T("绞碎", "Grind"));
		var check = new MachineryButton(L.T("检查生产条件", "Check Production"));
		var reserve = new MachineryText(uppercase: false);
		var resume = new MachineryText(L.T("清出空间或补电后\n按「绞碎」继续", "Clear space or restore power\nthen press Grind to resume"), uppercase: false);
		PixelWindow contentWindow = MachineHelper.CreateMachineryWindow(true,
			MachineHelper.PowerPanel(battery),
			MachineHelper.Panel(L.T("原料", "Ingredients"), input, "input"),
			MachineHelper.Panel(L.T("成品", "Output"), output, "output"),
			MachineHelper.Panel(L.T("操作", "Control"), MachineryStack.Vertical(machineryButton, check), "control"),
			MachineHelper.Panel(L.T("果糊热量储存", "Paste Reserve"), MachineryStack.Vertical(reserve, resume), "control"),
			MachineHelper.NotePanel(gameSlotInventory));
		MachineHelper.SetupManualSlot(gameSlotInventory);
		GameItem machine = Empty().SetContentWindow(contentWindow);
		machine.EnableTag("CONTAINER_TAG");
		MachineHelper.SetupBatterySlot(battery, machine);
		MachineryHelper.InitMachinery(machine);
		input.mayInventoryAddItemFunc = (GameItem item, GameInventory _) => GeneralHelper.IsItemOwned(item) && MinceOf(item) != null;
		output.mayInventoryAddItemFunc = (GameItem _, GameInventory _) => false;
		MachineUpgrade.Attach(machine, 1, input, output);
		void RefreshReserve()
		{
			long kcal = StoredFruitKcal(machine);
			reserve.SetText(L.T($"已存 {kcal:N0} 千卡\n可再加工 {kcal / PasteKcal:N0} 份", $"Stored {kcal:N0} kcal\n{kcal / PasteKcal:N0} portions ready"));
		}
		machine.onLoaded += _ => RefreshReserve();
		contentWindow.openCallback += RefreshReserve;
		RefreshReserve();
		check.SetCallback(() => Grind(machine, battery, input, output, inspect: true));
		machineryButton.SetCallback(() =>
		{
			Grind(machine, battery, input, output);
			RefreshReserve();
		});
		return machine;
	}

	private static void Grind(GameItem machine, GameSlotInventory battery, GameInventory input, GameInventory output, bool inspect = false)
	{
		try
		{
			GameItem childItem = battery.childItem;
			int cut = MachineUpgrade.PowerCut(machine);
			int pastePower = Math.Max(0, PastePower - cut);
			if (StoredFruitKcal(machine) >= PasteKcal)
			{
				GrindFruit(machine, childItem, output, null, pastePower, inspect);
				return;
			}
			int num2 = 0;
			foreach (GameItem item in input.childItems.ToList())
			{
				string text = MinceOf(item);
				if (text == null)
				{
					continue;
				}
				if (text == PasteId)
				{
					bool processed = GrindFruit(machine, childItem, output, item, pastePower, inspect);
					if (inspect) return;
					if (!processed || StoredFruitKcal(machine) >= PasteKcal) break;
					continue;
				}
				int num3 = item.GetTagReadonly("CALORIE_VALUE_TAG").GetInt();
				if (text == FishMinceId)
				{
					if (num3 <= 0 || item.unitCount <= 0) continue;
					num3 = checked(num3 * item.unitCount);
				}
				int num4 = machine.GetTagReadonly(PoolKcal(text)).GetInt();
				string text2 = item.GetTagReadonly("MEAT_TYPE_TAG").GetString() ?? "";
				string text3 = ((num4 > 0) ? (machine.GetTagReadonly(PoolSpecies(text)).GetString() ?? "") : text2);
				string mixed = ((text3 == text2) ? text2 : "*");
				bool prion = item.IsTag("ANIMAL_PRION_TAG") || (num4 > 0 && machine.GetTagReadonly(PoolPrion(text)).GetBool());
				int num5 = KcalOf(text);
				int num6 = (num4 + num3) / num5;
				int powerPerPortion = text == FishMinceId ? 1 : MincePower;
				int num = Math.Max(0, powerPerPortion - cut);
				if (num6 > 0 && (childItem == null || !PowerHelper.CanDrawPowerSource(childItem, num6 * num)))
				{
					if (inspect) ProductionNotice(L.T($"首份原料需要可供 {num6 * num} 电的电源。", $"The first input requires a power source with {num6 * num} charge."));
					break;
				}
				if (inspect)
				{
					if (num6 == 0)
						ProductionNotice(L.T("可以绞碎。首份原料不足以产出一份，余量会累计。", "Ready to grind. The first input adds to the stored remainder."));
					else
						CheckFirstOutput(output, () => MakeMince(text, mixed, prion),
							L.T($"首份原料可产出 {num6} 份，需 {num6 * num} 电。已确认成品槽能放入首件。", $"The first input yields {num6} portions for {num6 * num} charge. The tray fits the first item."));
					return;
				}
				List<GameItem> list = new List<GameItem>();
				for (int i = 0; i < num6; i++)
				{
					GameItem gameItem = MakeMince(text, mixed, prion);
					if (gameItem == null)
					{
						break;
					}
					list.Add(gameItem);
				}
				if (list.Count < num6 || !Put(output, list) ||
					(num6 > 0 && !PowerHelper.DrawPowerSource(childItem, num6 * num)))
				{
					foreach (GameItem item2 in list)
					{
						item2.Destroy();
					}
					break;
				}
				int left = num4 + num3 - num6 * num5;
				machine.ModifyTag(PoolKcal(text), (TagState s) =>
				{
					s.Enable().SetInt(left);
				});
				machine.ModifyTag(PoolSpecies(text), (TagState s) =>
				{
					s.Enable().SetString((left > 0) ? mixed : "");
				});
				machine.ModifyTag(PoolPrion(text), (TagState s) =>
				{
					s.Enable().SetBool((left > 0) & prion);
				});
				item.Destroy();
				num2 += num6;
			}
			if (num2 > 0)
			{
				ModLog.Info($"绞肉机：绞出 {num2} 份");
			}
			else if (inspect && !input.childItems.Any(item => MinceOf(item) != null))
				ProductionNotice(L.T("请放入生肉、合格鱼片或营养果。劣质鱼肉无法绞碎。", "Load raw meat, clean fish fillets, or nutrifruit. Trash-grade fish cannot be ground."));
		}
		catch (Exception ex)
		{
			ModLog.Warn("绞肉失败：" + ex.Message);
		}
	}

	private static long StoredFruitKcal(GameItem machine)
	{
		TagState stored = machine.GetTagReadonly(PoolKcal(PasteId));
		return Math.Max(0, Math.Max(stored.GetInt(), stored.GetLong()));
	}

	private static bool GrindFruit(GameItem machine, GameItem battery, GameInventory output, GameItem fruit, int power, bool inspect)
	{
		long stored = StoredFruitKcal(machine);
		long added = fruit == null ? 0 : (long)fruit.GetTagReadonly("CALORIE_VALUE_TAG").GetInt() * fruit.unitCount;
		if (fruit != null && added <= 0)
		{
			ProductionNotice(L.T("这份营养果没有可加工的热量。", "This nutrifruit has no calories to process."));
			return false;
		}

		long total = stored + added;
		long portions = total / PasteKcal;
		bool prion = (stored > 0 && machine.GetTagReadonly(PoolPrion(PasteId)).GetBool()) || fruit?.IsTag(Prion) == true;
		if (portions > 0 && !PowerHelper.CanDrawPowerSource(battery, power))
		{
			ProductionNotice(L.T($"电力不足。做一份果糊需 {power} 电。", $"Insufficient power. One portion of paste needs {power} charge."));
			return false;
		}
		if (inspect)
		{
			if (portions == 0)
				ProductionNotice(L.T($"共 {total:N0} 千卡。未满 240 千卡的热量会储存起来。", $"{total:N0} kcal total. The grinder stores any remainder under 240 kcal."));
			else
				CheckFirstOutput(output, () => MakeMince(PasteId, "", prion),
					L.T($"热量够做 {portions:N0} 份果糊，每份耗 {power} 电。成品槽能放下首份，本次产量视空间和电量而定。",
						$"Calories for {portions:N0} portions at {power} charge each. The first fits. Batch size depends on space and power."));
			return false;
		}

		int made = portions == 0 ? 0 : PlaceFruitPaste(output, battery, portions, power, prion);
		if (portions > 0 && made == 0)
		{
			ProductionNotice(L.T("果糊未产出。请检查成品槽和电源。", "No paste produced. Check the output tray and power source."));
			return false;
		}

		long left = total - made * (long)PasteKcal;
		machine.ModifyTag(PoolKcal(PasteId), tag => tag.Enable().SetLong(left).SetInt((int)Math.Min(left, int.MaxValue)));
		machine.ModifyTag(PoolPrion(PasteId), tag => tag.Enable().SetBool(left > 0 && prion));
		fruit?.Destroy();
		if (left >= PasteKcal)
			ProductionNotice(L.T($"本次绞出 {made:N0} 份果糊，剩余 {left:N0} 千卡已储存。", $"Made {made:N0} portions of paste and stored {left:N0} kcal."));
		return true;
	}

	private static int PlaceFruitPaste(GameInventory output, GameItem battery, long portions, int power, bool prion)
	{
		var originalStacks = output.childItems.Where(item => item.identifier == PasteId).ToDictionary(item => item, item => item.unitCount);
		long originalUnits = originalStacks.Values.Sum(count => (long)count);
		var products = new List<GameItem>();
		int made = 0;
		bool paid = false;
		try
		{
			while (made < portions && PowerHelper.CanDrawPowerSource(battery, checked((made + 1) * power)))
			{
				GameItem paste = MakeMince(PasteId, "", prion);
				if (paste == null) break;
				products.Add(paste);
				ProductionOutput.TryAdd(output, paste);
				// A stacking mod can transfer the portion without parenting its source.
				long units = output.childItems.Where(item => item.identifier == PasteId).Sum(item => (long)item.unitCount);
				long received = units - originalUnits - made;
				if (received == 0) break;
				if (received != 1) return 0;
				made++;
			}

			paid = made > 0 && TryPayFruitPower(battery, made * power);
			return paid ? made : 0;
		}
		finally
		{
			if (!paid)
			{
				foreach (var stack in originalStacks)
					if (stack.Key.unitCount != stack.Value)
						stack.Key.SetUnitCount(stack.Value);
			}
			foreach (GameItem product in products)
				if ((!paid || product.parentInventory != output) && !StorePlacement.Destroyed(product))
				{
					try { product.Destroy(); }
					catch (Exception error) { ModLog.Warn("回收未入槽果糊失败：" + error.Message); }
				}
		}
	}

	private static bool TryPayFruitPower(GameItem battery, int power)
	{
		if (power == 0) return true;
		if (!battery.IsTag("power_source_item") || CurrentConverterHelper.IsCurrentConverter(battery) ||
			HypertechBatteryHelper.IsHypertechBattery(battery))
			return PowerHelper.DrawPowerSource(battery, power);

		int before = battery.GetTagReadonly(BatteryEnergyTag).GetInt();
		bool paid = false;
		try
		{
			paid = PowerHelper.DrawPowerSource(battery, power) && before - battery.GetTagReadonly(BatteryEnergyTag).GetInt() == power;
			return paid;
		}
		finally
		{
			if (!paid && battery.GetTagReadonly(BatteryEnergyTag).GetInt() != before)
				PowerHelper.SetPowerSourceAt(battery, before);
		}
	}

	private static GameItem MakeMince(string id, string species, bool prion)
	{
		GameItem gameItem = Goods.Make(id switch
		{
			FishMinceId => "fish_mince",
			PasteId => "fruit_paste",
			"corp_presence_fine_mince" => "fine_mince",
			_ => "coarse_mince"
		});
		if (gameItem == null)
		{
			return null;
		}
		GeneralHelper.SetItemOwned(gameItem);
		if (species != "" && species != "*")
		{
			gameItem.ModifyTag("MEAT_TYPE_TAG", (TagState s) =>
			{
				s.Enable().SetString(species);
			});
		}
		if (prion)
		{
			gameItem.EnableTag("ANIMAL_PRION_TAG");
		}
		return gameItem;
	}

	private static GameItem BuildCanner()
	{
		GameSlotInventory battery = new GameSlotInventory();
		GameGridInventory modules = new GameGridInventory("10000010001000001110000010001000001", 7);
		GameGridInventory mince = new GameGridInventory(4, 4);
		GameGridInventory fruit = new GameGridInventory(4, 6);
		GameGridInventory output = new GameGridInventory(8, 8);
		GameSlotInventory gameSlotInventory = new GameSlotInventory();
		GameItem machine = null;
		MachineryRadioButton synthetic = new MachineryRadioButton(L.T("合成档", "Synthetic"), on: true, (bool on) =>
		{
			if (on)
			{
				SetFresh(machine, on: false);
			}
		}).SetDescription(L.T("3 营养果糊 + 1 杂肉糜 → 4 罐“泥炭”", "3 nutrifruit paste + 1 scrap mince → 4 cans of Peat"));
		MachineryRadioButton fresh = new MachineryRadioButton(L.T("鲜肉档", "Fresh"), on: false, (bool on) =>
		{
			if (on)
			{
				SetFresh(machine, on: true);
			}
		}).SetDescription(L.T("2 精肉糜 + 1 营养果糊 → 1 罐鲜肉罐头", "2 prime mince + 1 nutrifruit paste → 1 can of fresh meat"));
		new MachineryRadioGroup().Add(synthetic).Add(fresh);
		string lastResult = L.T("本次载入后尚未进行夜间结算。", "No nightly cycle since this load.");
		var lastCycle = new MachineryButton(L.T("最近结算", "Last cycle"));
		lastCycle.SetCallback(() => GenericConfirmationUIManager.Instance?.OpenPanelSingleButton(
			lastResult, L.T("确定", "OK"), () => GenericConfirmationUIManager.Instance.ClosePanel()));
		var check = new MachineryButton(L.T("检查生产条件", "Check Production"));
		PixelWindow pixelWindow = MachineHelper.CreateMachineryWindow(true, MachineHelper.PowerPanel(battery), MachineHelper.Panel(L.T("模组", "Modules"), modules, "module"), MachineHelper.Panel(L.T("肉糜", "Mince"), mince, "input"), MachineHelper.Panel(L.T("营养果糊", "Nutrifruit paste"), fruit, "input"), MachineHelper.Panel(L.T("档位", "Mode"), MachineryStack.Vertical(synthetic, fresh, check, lastCycle), "control"), MachineHelper.Panel(L.T("成品", "Output"), output, "output"), MachineHelper.NotePanel(gameSlotInventory));
		MachineHelper.SetupManualSlot(gameSlotInventory);
		machine = Empty().SetContentWindow(pixelWindow);
		machine.EnableTag("CONTAINER_TAG");
		machine.EnableTag("STANDARD_MACHINE_TAG");
		MachineHelper.SetupBatterySlot(battery, machine);
		MachineryHelper.InitMachinery(machine, 8);
		machine.ModifyTag("BASE_PROCESSING_SPEED_TAG", (TagState s) =>
		{
			s.Enable().SetInt(200);
		});
		machine.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", (TagState s) =>
		{
			s.Enable().SetInt(200);
		});
		MachineHelper.SetupModuleBay(modules, machine);
		mince.mayInventoryAddItemFunc = (GameItem item, GameInventory _) => GeneralHelper.IsItemOwned(item) && (item.identifier == "corp_presence_fine_mince" || item.identifier == "corp_presence_coarse_mince");
		fruit.mayInventoryAddItemFunc = (GameItem item, GameInventory _) => GeneralHelper.IsItemOwned(item) && item.identifier == PasteId;
		output.mayInventoryAddItemFunc = (GameItem _, GameInventory _) => false;
		Action sync = () =>
		{
			(IsFresh(machine) ? fresh : synthetic).SetOn(on: true);
		};
		machine.onLoaded = (Action<GameItem>)Delegate.Combine(machine.onLoaded, (Action<GameItem>)((GameItem _) =>
		{
			sync();
		}));
		pixelWindow.openCallback = (Action)Delegate.Combine(pixelWindow.openCallback, sync);
		MachineUpgrade.Attach(machine, 2, mince, fruit, output);
		check.SetCallback(() => Can(machine, battery, modules, mince, fruit, output, inspect: true));
		machine.onCycleEndSlotItemFunc = (GameItem _, GameInventory _, SlotMarker _) =>
		{
			Can(machine, battery, modules, mince, fruit, output, report: text => lastResult = text);
		};
		return machine;
	}

	private static bool IsFresh(GameItem machine)
	{
		return machine?.GetTagReadonly("CORP_CAN_FRESH_BOOL").GetBool() ?? false;
	}

	private static void SetFresh(GameItem machine, bool on)
	{
		machine?.ModifyTag("CORP_CAN_FRESH_BOOL", (TagState s) =>
		{
			s.Enable().SetBool(on);
		});
	}

	private static void Can(GameItem machine, GameSlotInventory battery, GameGridInventory modules, GameInventory mince, GameInventory fruit, GameInventory output, bool inspect = false, Action<string> report = null)
	{
		try
		{
			ModuleHelper.ComputeModuleEffect(modules.childItems.ToList(), machine);
			ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
			Recipe r = (IsFresh(machine) ? Fresh : Synthetic);
			int num = Math.Max(0, machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt());
			int num2 = machine.GetTagReadonly("CORP_CAN_CARRY_INT").GetInt();
			List<GameItem> list = mince.childItems.Where((GameItem i) => i?.identifier == r.Mince).ToList();
			List<GameItem> list2 = fruit.childItems.Where((GameItem i) => i?.identifier == PasteId).ToList();
			int num3 = num + num2;
			int batches = Math.Min(num3 / 100, Math.Min(list.Count / r.MinceN, list2.Count / r.FruitN));
			if (inspect)
			{
				int power = MachineryHelper.GetMachinePowerUsage(machine);
				if (list.Count < r.MinceN || list2.Count < r.FruitN)
					ProductionNotice(r == Fresh
						? L.T("鲜肉档需要 2 份精肉糜和 1 份营养果糊。", "Fresh mode needs 2 prime mince and 1 nutrifruit paste.")
						: L.T("合成档需要 1 份杂肉糜和 3 份营养果糊。", "Synthetic mode needs 1 scrap mince and 3 nutrifruit paste."));
				else if (batches <= 0)
					ProductionNotice(L.T("当前加工进度不足一批。", "Current processing progress is below one batch."));
				else if (!PowerHelper.CanDrawPowerSource(battery.childItem, power))
					ProductionNotice(L.T($"需要可供 {power} 电的电源。", $"A power source with {power} charge is required."));
				else
					CheckFirstOutput(output, r.Make, L.T($"原料和速度支持 {batches} 批，需 {power} 电。已确认成品槽能放入首件。", $"Inputs and speed support {batches} batches for {power} charge. The tray fits the first item."));
				return;
			}
			if (batches <= 0)
			{
				report?.Invoke(L.T($"未加工：当前档位肉糜 {list.Count}/{r.MinceN}，果糊 {list2.Count}/{r.FruitN}，加工速度 {num}。请核对档位、原料数量和模组。", $"Stopped: matching mince {list.Count}/{r.MinceN}, paste {list2.Count}/{r.FruitN}, speed {num}. Check mode, ingredients and modules."));
				return;
			}
			if (!PowerHelper.CanDrawPowerSource(battery.childItem, MachineryHelper.GetMachinePowerUsage(machine)))
			{
				report?.Invoke(L.T("未加工：电源无法提供本次所需电量。", "Stopped: the power source cannot supply this cycle."));
				return;
			}
			int done = 0;
			string failure = null;
			for (int num4 = 0; num4 < batches; num4++)
			{
				List<GameItem> list3 = new List<GameItem>();
				for (int num5 = 0; num5 < r.OutN; num5++)
				{
					GameItem gameItem = r.Make();
					if (gameItem == null)
					{
						break;
					}
					GeneralHelper.SetItemOwned(gameItem);
					list3.Add(gameItem);
				}
				if (list3.Count < r.OutN || !Put(output, list3) ||
					(done == 0 && !MachineHelper.TryDrawCyclePower(machine, battery)))
				{
					failure = L.T("成品创建、放置或首次扣电失败；本批原料已保留。", "Output creation, placement or initial power draw failed; this batch's inputs were kept.");
					foreach (GameItem item in list3)
					{
						item.Destroy();
					}
					break;
				}
				for (int num6 = 0; num6 < r.MinceN; num6++)
				{
					list[done * r.MinceN + num6].Destroy();
				}
				for (int num7 = 0; num7 < r.FruitN; num7++)
				{
					list2[done * r.FruitN + num7].Destroy();
				}
				done++;
			}
			report?.Invoke(L.T($"完成 {done}/{batches} 批。", $"Completed {done}/{batches} batches.") + failure);
			int left = Math.Min(99, Math.Max(0, num3 - done * 100));
			machine.ModifyTag("CORP_CAN_CARRY_INT", (TagState s) =>
			{
				s.Enable().SetInt((done == batches) ? left : 0);
			});
			if (done > 0)
			{
				MachineHelper.OnMachineActioned(machine, modules);
			}
			ModLog.Info(string.Format("罐装机（{0}档）：封了 {1} 批（速度 {2}，耗电 {3}）", (r == Fresh) ? "鲜肉" : "合成", done, num, MachineryHelper.GetMachinePowerUsage(machine)));
		}
		catch (Exception ex)
		{
			report?.Invoke(L.T("加工异常，请查看游戏日志：", "Production error; check game log: ") + ex.Message);
			ModLog.Warn("罐装失败：" + ex.Message);
		}
	}

	private static void ProductionNotice(string message) => StoreUIManager.Instance?.Notify(message);

	private static void CheckFirstOutput(GameInventory output, Func<GameItem> make, string ready)
	{
		var product = make();
		if (product == null)
		{
			ProductionNotice(L.T("成品尚未就绪。", "The product is unavailable."));
			return;
		}
		var previous = output.mayInventoryAddItemFunc;
		try
		{
			GeneralHelper.SetItemOwned(product);
			output.mayInventoryAddItemFunc = (_, _) => true;
			ProductionNotice(GraphUtils.CanAccept(output, product) ? ready : L.T("成品槽空间不足。", "The output tray is full."));
		}
		finally
		{
			output.mayInventoryAddItemFunc = previous;
			product.Destroy();
		}
	}

	private static bool Put(GameInventory inv, List<GameItem> items)
	{
		return Tobacco.Put(inv, items);
	}

	public static GameItem MakeMachineCrate()
	{
		return Crates.MakeFilled("crate_davre", "meat_grinder", "canning_machine");
	}
}


