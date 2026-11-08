using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static class Tobacco
{
	public const string SeedId = "corp_presence_tobacco_seed";

	public const string LeafId = "corp_presence_tobacco_leaf";

	public const string LooseId = "corp_presence_loose_cigarette";

	public const string RollerId = "corp_presence_rolling_machine";

	public const string PackerId = "corp_presence_packaging_machine";

	public const string StampId = "corp_presence_hemingway_stamp";

	public const string PlantSprite = "corp_presence_tobacco_leaf_p";

	public const int LeafPower = 5;

	public const int CigsPerLeaf = 2;

	public const int CigsPerPack = 3;

	public const int PackerPower = 8;

	public const int PacksPerNight = 2;

	public const int StampUses = 8;

	public const int StampMaxUses = 24;

	public const int StampPrice = 400;

	private const int SpeedScale = 100;

	private const string CarryTag = "CORP_PACK_CARRY_INT";

	internal static readonly Dictionary<string, Goods.Def> Defs = new Dictionary<string, Goods.Def>
	{
		["tobacco_seed"] = new Goods.Def
		{
			Name = "烟草种子",
			NameEn = "Tobacco Seeds",
			Value = 20L,
			Company = "pastel",
			Art = "tobacco_seed",
			Fallback = "kotton_plant_seed",
			Manufacturer = "pastel",
			Types = new string[1] { "SEED" },
			W = 1,
			H = 1,
			Shell = Empty,
			Setup = InitSeed,
			Desc = "种在水培机里，大约4晚收一茬烟叶，默认返还1颗种子。可移入多年生基因，收获后保留植株；再次结果基础进度14，产量、生长速度和返种数量遵循原版基因效果。",
			DescEn = "Grow in hydroponics: about 4 nights for leaves and 1 seed by default. Supports Perennial: the plant survives harvest, with refruit base work 14. Native genes modify yield, growth speed and seed returns.",
			Flavor = "帕斯泰尔的经济作物。娇气、吃水。",
			FlavorEn = "A pastel cash crop. Fussy and thirsty."
		},
		["tobacco_leaf"] = new Goods.Def
		{
			Name = "烟叶",
			NameEn = "Tobacco Leaves",
			Value = 30L,
			Company = "pastel",
			Art = "tobacco_leaf",
			Fallback = "fillerweed",
			Types = new string[1] { "SUBSTANCE" },
			W = 1,
			H = 2,
			Shell = Empty,
			Desc = "放进卷烟机，每片可以卷成 2 支私制散烟。",
			DescEn = "Put it in a cigarette roller: each leaf can be rolled into 2 loose cigarettes.",
			Flavor = "刚收下来的一捆烟叶。",
			FlavorEn = "A fresh bundle of tobacco leaves."
		},
		["loose_cigarette"] = new Goods.Def
		{
			Name = "私制散烟",
			NameEn = "Loose Cigarettes",
			Value = 20L,
			Company = "pastel",
			Art = "loose_cigarette",
			Fallback = "cig_red",
			Types = new string[3] { "SUBSTANCE", "MAKESHIFT", "CONTRABAND" },
			W = 1,
			H = 1,
			Shell = Empty,
			Setup = InitLoose,
			VanillaUse = true,
			Desc = "能直接抽。放进分装机，3 支封成 1 包海明威。私人作坊的货，没交税，按违禁品算。",
			DescEn = "Can be smoked. A packaging machine seals 3 into a pack of Hemingways. Untaxed backroom goods, treated as contraband.",
			Flavor = "没有牌子的手卷烟，卷纸发黄，粗细不一。",
			FlavorEn = "An unbranded hand-rolled cigarette in yellowed paper."
		},
		["rolling_machine"] = new Goods.Def
		{
			Name = "卷烟机",
			NameEn = "Cigarette Roller",
			Value = 300L,
			Company = "pastel",
			Art = "rolling_machine",
			Fallback = "bottle_printer",
			Manufacturer = "pastel",
			Types = new string[2] { "MACHINE", "LOWTECH" },
			W = 3,
			H = 2,
			Shell = BuildRoller,
			Manual = RollerManual,
			Desc = "放进烟叶、插上电池，按“卷烟”当场卷好：每片烟叶卷成 2 支私制散烟，耗 5 电。",
			DescEn = "Load tobacco leaves and a battery, then press Roll: each leaf becomes 2 loose cigarettes on the spot and uses 5 charge.",
			Flavor = "海明威分部淘汰下来的旧卷烟机。漆面磨得发白，侧面还留着半个红标。",
			FlavorEn = "An old roller retired by the Hemingway division. The paint is worn pale, with half a red logo left on the side."
		},
		["packaging_machine"] = new Goods.Def
		{
			Name = "分装机",
			NameEn = "Packaging Machine",
			Value = 350L,
			Company = "pastel",
			Art = "packaging_machine",
			Fallback = "furnace",
			Manufacturer = "pastel",
			Types = new string[1] { "MACHINE" },
			W = 3,
			H = 3,
			Shell = BuildPacker,
			Manual = PackerManual,
			Desc = "放进私制散烟、插上电池，每晚把 3 支散烟封成 1 包海明威正品，默认每晚 2 包，耗 8 电。性能模组多封（也多用散烟），效率模组省电。标签槽里放着标签戳时，封出来的是限量包装。",
			DescEn = "Load loose cigarettes and a battery. Each night it seals 3 cigarettes into 1 genuine Hemingway pack, 2 packs by default, using 8 charge. Performance modules seal more (using more cigarettes); efficiency modules save power. With a label stamp in the label slot, packs come out as Limited Edition.",
			Flavor = "帕斯泰尔正规的分装设备。用了它，你的烟就是海明威。",
			FlavorEn = "Proper pastel packaging equipment. Run your smokes through it and they're Hemingways."
		},
		["hemingway_stamp"] = new Goods.Def
		{
			Name = "海明威标签戳",
			NameEn = "Hemingway Label Stamp",
			Value = StampPrice,
			Company = "pastel",
			Art = "hemingway_stamp",
			Fallback = "food_stamp",
			Manufacturer = "pastel",
			Types = new string[3] { "TOOL", "CONTRABAND", "NOT_FOR_RESALE" },
			W = 1,
			H = 2,
			Shell = Empty,
			Setup = InitStamp,
			Desc = "放进分装机的标签槽。全新标签戳可用 8 次。把自己的标签戳拖到另一个上可合并剩余次数，合计最多 24 次；超过上限不能合并。只在分装机里管用。",
			DescEn = "Put it in a packaging machine's label slot. A new stamp has 8 uses. Drag one owned stamp onto another to combine their remaining uses, up to 24. They cannot be combined if the total exceeds 24. Only works in the packaging machine.",
			Flavor = "分部印限量包装用的标签戳，手柄上缠着一圈胶带。外面买不到。",
			FlavorEn = "The division's stamp for Limited Edition packaging, tape wrapped around the handle. You can't buy one outside."
		}
	};

	private static Sprite _overlay;

	private static (string Zh, string En) RollerManual => (Zh: "把烟叶放进烟叶槽，接好电源，按「卷烟」。每片当场卷成 2 支私制散烟，默认耗 5 电。电量或成品空间不足时停止。\n私制散烟是违禁品。", En: "Load tobacco leaves, connect power, then press Roll. Each leaf makes 2 loose cigarettes on the spot for 5 charge by default. Rolling stops when power or output space runs out.\nLoose cigarettes are contraband.");

	private static (string Zh, string En) PackerManual => (Zh: "把私制散烟放进散烟槽，接好电源，等待过夜。每 3 支散烟封成 1 包海明威，默认每晚最多 2 包，启动耗 8 电。\n标签槽放入海明威标签戳时可封限量包装，每包消耗 1 次使用次数。\n性能模组增加封装数量，效率模组降低耗电。", En: "Load loose cigarettes, connect power and wait overnight. Every 3 loose cigarettes make 1 pack of Hemingways. It makes up to 2 packs per night by default, using 8 charge per run.\nA Hemingway stamp in the label slot makes Limited Edition packs and uses 1 stamp charge per pack.\nPerformance modules add packs. Efficiency modules reduce power use.");

	private static GameItem Empty()
	{
		return ItemDirectory.CreateEmptyItem();
	}

	private static PlantDefinition _ownedPlant;

	public static void RegisterPlant()
	{
		if (!GameResources.Ready) throw new InvalidOperationException("烟草资源尚未就绪");
		if (PlantRegistry.Contains("corp_presence_tobacco_seed")) return;
		_ownedPlant = new PlantDefinition("corp_presence_tobacco_seed", "Tobacco", "corp_presence_tobacco_leaf", 28, 10).Nutriment(300, 100, 60).Contaminant(1.1).Water(150)
			.Refruit(14).ReturnsSeeds().Innate("sparse").Innate("thirsty").Innate("frail").Innate("complex_genome").OnHarvest(Harvest);
		PlantRegistry.Add(_ownedPlant);
	}

	public static void UnregisterPlant()
	{
		if (_ownedPlant != null && ReferenceEquals(PlantRegistry.Get("corp_presence_tobacco_seed"), _ownedPlant))
			PlantRegistry.Remove("corp_presence_tobacco_seed");
		_ownedPlant = null;
	}
	private static void Harvest(PlantHarvestContext ctx)
	{
		ctx.OutputById("corp_presence_tobacco_seed");
		ctx.OutputById("corp_presence_tobacco_leaf", Math.Max(1, ctx.harvestCount));
	}

	private static void InitSeed(GameItem item)
	{
		RegisterPlant();
		HydroponicHelper.InitSeedItem(item, "corp_presence_tobacco_seed");
	}

	private static void InitLoose(GameItem item)
	{
		ContrabandHelper.InitContrabandItem(item, 1);
		item.EnableTag("CIGARETTE_TAG");
		ConsumableHelper.InitNicotine(item, 1);
		item.AddAmenities("amenities_substance", 1, 7);
	}

	private static void InitStamp(GameItem item)
	{
		ContrabandHelper.InitContrabandItem(item, 3);
		item.EnableTag("NOT_FOR_RESALE");
		DurabilityHelper.InitItemWithDurability(item, StampUses);
		item.mayThisTargetItemFunc += CanMergeStamps;
		item.canThisTargetItemFunc += CanMergeStamps;
		item.onThisTargetItemFunc += MergeStamps;
	}

	public static Sprite PlantOverlay()
	{
		if (_overlay != null)
		{
			return _overlay;
		}
		Sprite sprite = RenderHandler.TryLoadFromAtlas("Items/items_hydroponic_plant", "coffee_bean_p");
		Texture2D texture2D = Tex.Art("pastel", "tobacco_leaf_p");
		if (texture2D == null)
		{
			return sprite;
		}
		float pixelsPerUnit = ((sprite != null) ? sprite.pixelsPerUnit : 100f);
		Vector2 pivot = ((sprite != null) ? new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height) : new Vector2(0.5f, 0.5f));
		_overlay = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), pivot, pixelsPerUnit);
		_overlay.name = "corp_presence_tobacco_leaf_p";
		_overlay.hideFlags = HideFlags.HideAndDontSave;
		return _overlay;
	}

	private static GameItem BuildRoller()
	{
		GameSlotInventory battery = new GameSlotInventory();
		GameGridInventory input = new GameGridInventory(4, 4);
		GameGridInventory output = new GameGridInventory(6, 4);
		GameSlotInventory gameSlotInventory = new GameSlotInventory();
		MachineryButton machineryButton = new MachineryButton(L.T("卷烟", "Roll"));
		PixelWindow contentWindow = MachineHelper.CreateMachineryWindow(true, MachineHelper.PowerPanel(battery), MachineHelper.Panel(L.T("烟叶", "Leaves"), input, "input"), MachineHelper.Panel(L.T("散烟", "Cigarettes"), output, "output"), MachineHelper.Panel(L.T("操作", "Control"), MachineryStack.Vertical(machineryButton), "control"), MachineHelper.NotePanel(gameSlotInventory));
		MachineHelper.SetupManualSlot(gameSlotInventory);
		GameItem machine = Empty().SetContentWindow(contentWindow);
		machine.EnableTag("CONTAINER_TAG");
		MachineHelper.SetupBatterySlot(battery, machine);
		MachineryHelper.InitMachinery(machine);
		input.mayInventoryAddItemFunc = (GameItem item, GameInventory _) => GeneralHelper.IsItemOwned(item) && item.identifier == "corp_presence_tobacco_leaf";
		output.mayInventoryAddItemFunc = (GameItem _, GameInventory _) => false;
		MachineUpgrade.Attach(machine, 1, input, output);
		MachineAppearance.WatchOutput(machine, output);
		machineryButton.SetCallback(() =>
		{
			Roll(machine, battery, input, output);
		});
		return machine;
	}

	private static void Roll(GameItem machine, GameSlotInventory battery, GameInventory input, GameInventory output)
	{
		try
		{
			GameItem childItem = battery.childItem;
			int amount = 5 - MachineUpgrade.PowerCut(machine);
			int num = 0;
			foreach (GameItem item in input.childItems.ToList())
			{
				if (item?.identifier != "corp_presence_tobacco_leaf")
				{
					continue;
				}
				if (childItem == null || !PowerHelper.CanDrawPowerSource(childItem, amount))
				{
					break;
				}
				List<GameItem> list = new List<GameItem>();
				for (int i = 0; i < 2; i++)
				{
					GameItem gameItem = Goods.Make("loose_cigarette");
					if (gameItem == null)
					{
						break;
					}
					GeneralHelper.SetItemOwned(gameItem);
					list.Add(gameItem);
				}
				if (list.Count < 2 || !Put(output, list))
				{
					foreach (GameItem item2 in list)
					{
						item2.Destroy();
					}
					break;
				}
				PowerHelper.DrawPowerSource(childItem, amount);
				item.Destroy();
				num++;
			}
			if (num > 0)
			{
				ModLog.Info($"卷烟机：卷了 {num} 片烟叶，出 {num * 2} 支散烟");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("卷烟失败：" + ex.Message);
		}
	}

	private static GameItem BuildPacker()
	{
		GameSlotInventory battery = new GameSlotInventory();
		GameGridInventory modules = new GameGridInventory("10000010001000001110000010001000001", 7);
		GameGridInventory input = new GameGridInventory(6, 2);
		GameSlotInventory label = new GameSlotInventory(1, 2);
		GameGridInventory output = new GameGridInventory(6, 4);
		GameSlotInventory gameSlotInventory = new GameSlotInventory();
		PixelWindow contentWindow = MachineHelper.CreateMachineryWindow(true, MachineHelper.PowerPanel(battery), MachineHelper.Panel(L.T("模组", "Modules"), modules, "module"), MachineHelper.Panel(L.T("散烟", "Cigarettes"), input, "input"), MachineHelper.Panel(L.T("标签戳", "Label stamp"), label, "input"), MachineHelper.Panel(L.T("成品", "Output"), output, "output"), MachineHelper.NotePanel(gameSlotInventory));
		MachineHelper.SetupManualSlot(gameSlotInventory);
		GameItem machine = Empty().SetContentWindow(contentWindow);
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
		input.mayInventoryAddItemFunc = (GameItem item, GameInventory _) => GeneralHelper.IsItemOwned(item) && item.identifier == "corp_presence_loose_cigarette";
		label.mayInventoryAddItemFunc = (GameItem item, GameInventory _) => GeneralHelper.IsItemOwned(item) && item.identifier == "corp_presence_hemingway_stamp";
		output.mayInventoryAddItemFunc = (GameItem _, GameInventory _) => false;
		MachineUpgrade.Attach(machine, 1, input, output);
		machine.onCycleEndSlotItemFunc = (GameItem _, GameInventory _, SlotMarker _) =>
		{
			Pack(machine, battery, modules, input, label, output);
		};
		MachineAppearance.WatchCycle(machine, output, () =>
			input.childItems.Count(item => item?.identifier == "corp_presence_loose_cigarette") >= 3 &&
			MachineAppearance.CanPowerCycle(machine, battery, "CORP_PACK_CARRY_INT")
				? "packet_red_cigarette" : null);
		return machine;
	}

	private static void Pack(GameItem machine, GameSlotInventory battery, GameGridInventory modules, GameInventory input, GameSlotInventory label, GameInventory output)
	{
		try
		{
			ModuleHelper.ComputeModuleEffect(modules.childItems.ToList(), machine);
			ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
			int num = Math.Max(0, machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt());
			int num2 = (machine.IsTag("CORP_PACK_CARRY_INT") ? machine.GetTagReadonly("CORP_PACK_CARRY_INT").GetInt() : 0);
			List<GameItem> list = input.childItems.Where((GameItem i) => i?.identifier == "corp_presence_loose_cigarette").ToList();
			int num3 = num + num2;
			int packs = Math.Min(num3 / 100, list.Count / 3);
			if (packs <= 0 || !MachineHelper.TryDrawCyclePower(machine, battery))
			{
				return;
			}
			int made = 0;
			for (int num4 = 0; num4 < packs; num4++)
			{
				GameItem childItem = label.childItem;
				bool flag = childItem != null && DurabilityHelper.GetRealDurability(childItem) > 0;
				GameItem gameItem = PastelCo.GenuineHemingway(flag);
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
				for (int num5 = 0; num5 < 3; num5++)
				{
					list[made * 3 + num5].Destroy();
				}
				made++;
				if (flag)
				{
					DurabilityHelper.ChangeDurability(childItem, -1);
					if (DurabilityHelper.GetRealDurability(childItem) <= 0)
					{
						childItem.Destroy();
					}
				}
			}
			int left = Math.Min(99, Math.Max(0, num3 - made * 100));
			machine.ModifyTag("CORP_PACK_CARRY_INT", (TagState s) =>
			{
				s.Enable().SetInt((made == packs) ? left : 0);
			});
			if (made > 0)
			{
				MachineHelper.OnMachineActioned(machine, modules);
			}
			PastelCo.CountPacks(made);
			ModLog.Info($"分装机：封了 {made} 包（速度 {num}，耗电 {MachineryHelper.GetMachinePowerUsage(machine)}）");
		}
		catch (Exception ex)
		{
			ModLog.Warn("分装失败：" + ex.Message);
		}
	}

	internal static void AddManual(GameItem machine, (string Zh, string En) text)
	{
		GameItem gameItem = DirectoryMaster.Item("postit", isOwned: false);
		if (gameItem != null)
		{
			string displayName = machine.GetDisplayName();
			gameItem.name = L.T(displayName + "说明", L.Translate((FormattableString)$"{displayName} Instructions"));
			gameItem.shortDescription = L.T(displayName + "使用方法。", L.Translate((FormattableString)$"How to use the {displayName}."));
			gameItem.flavorText = ManualText(machine, text);
			gameItem.SetGameItemType("DOCUMENT");
			gameItem.EnableTag("IMPORTANT_TAG");
			gameItem.EnableTag("MACHINE_MANUAL_TAG");
			gameItem.unitValue = 0L;
			GraphUtils.TryAcceptAll(machine, gameItem);
		}
	}

	internal static void RefreshManual(GameItem machine, Goods.Def definition)
	{
		if (machine.contentWindow?.childElement is not MachineryPanelRow row) return;
		var storage = new List<GraphNodeStorage>();
		row.GetPanel("note")?.CollectStorage(storage);
		if (storage.Count != 1 || storage[0] is not GameSlotInventory slot) return;
		var note = slot.childItem;
		if (note?.identifier != "postit" || !note.IsTag("MACHINE_MANUAL_TAG")) return;

		bool hasMatchingTitle = note.name == definition.Name + "说明" ||
			note.name == definition.NameEn + " Instructions" ||
			note.name == LocalizedManualTitle(definition.NameEn, "ru") ||
			note.name == LocalizedManualTitle(definition.NameEn, "fr");
		if (hasMatchingTitle) note.flavorText = ManualText(machine, definition.Manual);
	}

	private static string ManualText(GameItem machine, (string Zh, string En) text)
	{
		string description = L.T(text.Zh, text.En);
		string perfumeEffects = Perfume.EffectText(machine);
		if (perfumeEffects != null) description += "\n\n" + perfumeEffects;
		if (Goods.IdOf(machine) is "meat_grinder" or "rolling_machine")
			description += "\n" + L.T("成品槽有成品时显示运行图，取空后恢复静态图。", "The active sprite stays visible until the output tray is emptied.");
		return description;
	}

	private static string LocalizedManualTitle(string machineName, string locale) =>
		string.Format(TextTranslations.Select("{0} Instructions", locale), TextTranslations.Select(machineName, locale));

	internal static bool Put(GameInventory inv, List<GameItem> items)
	{
		return ProductionOutput.TryAddBatch(inv, items);
	}

	private static bool CanMergeStamps(GameItem source, GameItem target)
	{
		if (source == null || target == null || source == target ||
			source.identifier != StampId || target.identifier != StampId ||
			source.unitCount != 1 || target.unitCount != 1 ||
			StorePlacement.Destroyed(source) || StorePlacement.Destroyed(target) ||
			!GeneralHelper.IsItemOwned(source, target)) return false;

		int incoming = DurabilityHelper.GetRealDurability(source);
		int remaining = DurabilityHelper.GetRealDurability(target);
		return incoming > 0 && incoming <= StampMaxUses && remaining > 0 &&
			remaining <= StampMaxUses - incoming;
	}

	private static void MergeStamps(GameItem source, GameItem target)
	{
		if (!CanMergeStamps(source, target)) return;
		int uses = DurabilityHelper.GetRealDurability(source) + DurabilityHelper.GetRealDurability(target);
		int capacity = Math.Max(StampUses, uses);
		DurabilityHelper.SetDurability(target, uses, capacity, capacity);
		source.Destroy();
		AudioManager.Instance?.Play3("click_vintage");
	}
}





