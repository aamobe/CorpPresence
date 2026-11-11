using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static partial class SilverBlood
{

	private static GameItem BuildReactor()
	{
		RegisterLiquid();
		GameSlotInventory battery = new GameSlotInventory();
		GameGridInventory modules = new GameGridInventory("10000010001000001110000010001000001", 7);
		GameSlotInventory tank = new GameSlotInventory();
		GameGridInventory output = new GameGridInventory(8, 4);
		GameSlotInventory gameSlotInventory = new GameSlotInventory();
		PixelWindow contentWindow = MachineHelper.CreateMachineryWindow(true, MachineHelper.PowerPanel(battery), MachineHelper.Panel(L.K("production.silverBlood.reactor.buildReactor.modules", "模组", "Modules"), modules, "module"), MachineHelper.Panel(L.K("production.silverBlood.reactor.buildReactor.solution", "溶液", "Solution"), tank, "input"), MachineHelper.Panel(L.K("production.silverBlood.reactor.buildReactor.output", "成品", "Output"), output, "output"), MachineHelper.NotePanel(gameSlotInventory));
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
		MachineAppearance.WatchCycle(machine, output, () =>
		{
			GameItem solution = tank.childItem;
			if (solution == null || IsomerMl(solution) < 30 ||
				!MachineAppearance.CanPowerCycle(machine, battery, "CORP_SB_REACTOR_CARRY_INT",
					Math.Max(0, ModuleHelper.GetAjustedQuality(machine)) / 2))
				return null;
			return ProductFor(Purity(solution), directoryId: true);
		});
		return machine;
	}

	private static string ProductFor(double purity, bool directoryId = false)
	{
		const string Prefix = "corp_presence_";
		const string Rejuvenation = "rejuvenation";
		const string Implant = "implant_compat";
		const string Gel = "isomer_gel";

		if (purity >= 0.97) return directoryId ? Prefix + Rejuvenation : Rejuvenation;
		if (purity >= 0.88) return directoryId ? Prefix + Implant : Implant;
		if (purity >= 0.6) return directoryId ? Prefix + Gel : Gel;
		return null;
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
					bool readyToConsume = false;
					try
					{
						GeneralHelper.SetItemOwned(gameItem);
						if (!ProductionOutput.TryAdd(output, gameItem)) break;
						if (made == 0 && !MachineHelper.TryDrawCyclePower(machine, battery)) break;
						readyToConsume = true;
					}
					finally
					{
						if (!readyToConsume) gameItem.Destroy();
					}
					WaterHelper.RemoveMl(childItem, (int)Math.Ceiling(30.0 / Math.Max(0.01, num4)));
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

}



