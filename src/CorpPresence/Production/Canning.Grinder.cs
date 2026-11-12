using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class Canning
{
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

	internal static string MinceOf(GameItem meat)
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
					if (inspect) ProductionNotice(L.K("production.canning.grind.firstInputRequiresPowerSource", $"首份原料需要可供 {num6 * num} 电的电源。", (System.FormattableString)$"The first input requires a power source with {num6 * num} charge."));
					break;
				}
				if (inspect)
				{
					if (num6 == 0)
						ProductionNotice(L.K("production.canning.grind.readyGrindFirstInputAdds", "可以绞碎。首份原料不足以产出一份，余量会累计。", "Ready to grind. The first input adds to the stored remainder."));
					else
						CheckOutput(output, () => MakeMince(text, mixed, prion),
							L.K("production.canning.grind.firstInputYieldsPortionsCharge", $"首份原料可产出 {num6} 份，需 {num6 * num} 电。成品槽能放下这些成品。", (System.FormattableString)$"The first input yields {num6} portions for {num6 * num} charge. The tray fits all of them."), num6);
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
				bool complete = list.Count == num6;
				bool placed = complete && ProductionOutput.TryAddBatch(output, list);
				if (complete && !placed)
					MaintenanceDiagnostics.Record("grinder", "placement", "output_rejected", null, text);
				bool paid = placed && (num6 == 0 || PowerHelper.DrawPowerSource(childItem, num6 * num));
				if (placed && !paid)
					MaintenanceDiagnostics.Record("grinder", "power", "payment_failed", null, text);
				if (!paid)
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
				ProductionNotice(L.K("production.canning.grind.loadRawMeatCleanFish", "请放入生肉、合格鱼片或营养果。劣质鱼肉无法绞碎。", "Load raw meat, clean fish fillets, or nutrifruit. Trash-grade fish cannot be ground."));
		}
		catch (Exception ex)
		{
			MaintenanceDiagnostics.Record("grinder", "process", "exception", ex);
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
			ProductionNotice(L.K("production.canning.grindFruit.thisNutrifruitHasNoCalories", "这份营养果没有可加工的热量。", "This nutrifruit has no calories to process."));
			return false;
		}

		long total = stored + added;
		long portions = total / PasteKcal;
		bool prion = (stored > 0 && machine.GetTagReadonly(PoolPrion(PasteId)).GetBool()) || fruit?.IsTag(Prion) == true;
		if (portions > 0 && !PowerHelper.CanDrawPowerSource(battery, power))
		{
			ProductionNotice(L.K("production.canning.grindFruit.insufficientPowerOnePortionPaste", $"电力不足。做一份果糊需 {power} 电。", (System.FormattableString)$"Insufficient power. One portion of paste needs {power} charge."));
			return false;
		}
		if (inspect)
		{
			if (portions == 0)
				ProductionNotice(L.K("production.canning.grindFruit.kcalTotalGrinderStoresAny", $"共 {total:N0} 千卡。未满 240 千卡的热量会储存起来。", (System.FormattableString)$"{total:N0} kcal total. The grinder stores any remainder under 240 kcal."));
			else
				CheckOutput(output, () => MakeMince(PasteId, "", prion),
					L.K("production.canning.grindFruit.caloriesPortionsChargeEachFirst", $"热量够做 {portions:N0} 份果糊，每份耗 {power} 电。成品槽能放下首份，本次产量视空间和电量而定。", (System.FormattableString)$"Calories for {portions:N0} portions at {power} charge each. The first fits. Batch size depends on space and power."));
			return false;
		}

		int made = portions == 0 ? 0 : PlaceFruitPaste(output, battery, portions, power, prion);
		if (portions > 0 && made == 0)
		{
			ProductionNotice(L.K("production.canning.grindFruit.noPasteProducedCheckOutput", "果糊未产出。请检查成品槽和电源。", "No paste produced. Check the output tray and power source."));
			return false;
		}

		long left = total - made * (long)PasteKcal;
		machine.ModifyTag(PoolKcal(PasteId), tag => tag.Enable().SetLong(left).SetInt((int)Math.Min(left, int.MaxValue)));
		machine.ModifyTag(PoolPrion(PasteId), tag => tag.Enable().SetBool(left > 0 && prion));
		fruit?.Destroy();
		if (left >= PasteKcal)
			ProductionNotice(L.K("production.canning.grindFruit.madePortionsPasteStoredKcal", $"本次绞出 {made:N0} 份果糊，剩余 {left:N0} 千卡已储存。", (System.FormattableString)$"Made {made:N0} portions of paste and stored {left:N0} kcal."));
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
				if (received == 0)
				{
					MaintenanceDiagnostics.Record("grinder", "placement", "output_rejected", null, PasteId);
					break;
				}
				if (received != 1)
				{
					MaintenanceDiagnostics.Record("grinder", "placement", "unexpected_quantity", null, PasteId);
					return 0;
				}
				made++;
			}

			paid = made > 0 && TryPayFruitPower(battery, made * power);
			if (made > 0 && !paid)
				MaintenanceDiagnostics.Record("grinder", "power", "payment_failed", null, PasteId);
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
}
