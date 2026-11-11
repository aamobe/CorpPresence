using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CorpPresence;

internal static class MachineUpgrade
{
	private sealed class Rig
	{
		public (GameGridInventory Grid, int W, int H)[] Grids;

		public int RowsPerLevel;
	}

	private const string Ingot = "metal_ingot";

	private const string Electronic = "common_electronic";

	private const string CapLevel = "CORP_UPG_CAP_INT";

	private const string CapPaid = "CORP_UPG_CAP_PAID_INT";

	private const string PowerLevel = "CORP_UPG_PWR_INT";

	private const string PowerPaid = "CORP_UPG_PWR_PAID_INT";

	private static readonly int[] CapCost = new int[2] { 3, 6 };

	private const int PowerCost = 3;

	private static readonly string[] CapMachines = { "rolling_machine", "meat_grinder", "packaging_machine", "canning_machine", "seafood_sealer", "forge_furnace",
		Perfume.ExtractorKey, Perfume.BloodKey, Perfume.MixerKey, Perfume.CollectorKey };

	private static readonly string[] PowerMachines = new string[3] { "rolling_machine", "meat_grinder", "forge_anvil" };

	private static readonly ConditionalWeakTable<GameItem, Rig> Rigs = new ConditionalWeakTable<GameItem, Rig>();

	public static void Attach(GameItem machine, int rowsPerLevel, params GameGridInventory[] grids)
	{
		// Preserve original base dimensions and avoid duplicate load callbacks.
		if (machine == null) throw new ArgumentNullException(nameof(machine));
		if (Rigs.TryGetValue(machine, out _)) return;
		Rigs.Add(machine, new Rig
		{
			Grids = grids.Select((GameGridInventory g) => (g: g, width: g.inventoryShape.width, height: g.inventoryShape.height)).ToArray(),
			RowsPerLevel = rowsPerLevel
		});
		machine.onLoaded = (Action<GameItem>)Delegate.Combine(machine.onLoaded, new Action<GameItem>(Resize));
	}

	public static int PowerCut(GameItem machine)
	{
		if (machine == null || machine.GetTagReadonly("CORP_UPG_PWR_INT").GetInt() <= 0)
		{
			return 0;
		}
		return 1;
	}

	private static int Level(GameItem m, string tag)
	{
		return m.GetTagReadonly(tag).GetInt();
	}

	private static bool Is(GameItem m, string[] keys)
	{
		return Array.IndexOf(keys, Goods.IdOf(m)) >= 0;
	}

	private static bool CanCap(GameItem m)
	{
		if (m != null && Is(m, CapMachines) && Rigs.TryGetValue(m, out var _))
		{
			return Level(m, "CORP_UPG_CAP_INT") < CapCost.Length;
		}
		return false;
	}

	private static bool CanPower(GameItem m)
	{
		if (m != null && Is(m, PowerMachines))
		{
			return Level(m, "CORP_UPG_PWR_INT") < 1;
		}
		return false;
	}

	private static void Resize(GameItem machine)
	{
		try
		{
			if (Rigs.TryGetValue(machine, out var value))
			{
				int num = Level(machine, "CORP_UPG_CAP_INT") * value.RowsPerLevel;
				(GameGridInventory, int, int)[] grids = value.Grids;
				for (int i = 0; i < grids.Length; i++)
				{
					var (gameGridInventory, width, num2) = grids[i];
					gameGridInventory.SetShape(width, num2 + num);
					gameGridInventory.Validate();
				}
				machine.contentWindow?.Validate();
				machine.Validate();
				MachineAppearance.Rearm(machine);
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("机器容量重设失败：" + ex.Message);
		}
	}

	public static void AfterMakeIngot(GameItem __result)
	{
		Wrap(__result, target => ProductionWorkBay.CanUpgrade(target) || CanCap(target), "metal_ingot");
	}

	public static void AfterMakeElectronic(GameItem __result)
	{
		Wrap(__result, CanPower, "common_electronic");
	}

	private static void Wrap(GameItem material, Func<GameItem, bool> can, string kind)
	{
		if (material == null)
		{
			return;
		}
		Func<GameItem, GameItem, bool> may = material.mayThisTargetItemFunc;
		Func<GameItem, GameItem, bool> canFn = material.canThisTargetItemFunc;
		Action<GameItem, GameItem> on = material.onThisTargetItemFunc;
		material.mayThisTargetItemFunc = (GameItem s, GameItem t) => Ours(s, t) || (may != null && may(s, t));
		material.canThisTargetItemFunc = (GameItem s, GameItem t) => Ours(s, t) || (canFn != null && canFn(s, t));
		material.onThisTargetItemFunc = (GameItem s, GameItem t) =>
		{
			if (Ours(s, t))
			{
				if (kind == Ingot && t.identifier == ProductionWorkBay.ItemId) ProductionWorkBay.Upgrade(s, t);
				else Apply(s, t, kind);
			}
			else
			{
				on?.Invoke(s, t);
			}
		};
		bool Ours(GameItem s, GameItem t)
		{
			try
			{
				return t != null && GeneralHelper.IsItemOwned(s, t) && can(t);
			}
			catch
			{
				return false;
			}
		}
	}

	private static void Apply(GameItem material, GameItem machine, string kind)
	{
		try
		{
			bool flag = kind == "metal_ingot";
			string text = (flag ? "CORP_UPG_CAP_INT" : "CORP_UPG_PWR_INT");
			string text2 = (flag ? "CORP_UPG_CAP_PAID_INT" : "CORP_UPG_PWR_PAID_INT");
			int level = Level(machine, text);
			int num = (flag ? CapCost[level] : 3);
			int paid = Level(machine, text2) + 1;
			machine.unitValue += material.unitValue;
			StartingPerkContent.ConsumeUpgradeMaterial(material);
			string name = machine.name;
			if (paid >= num)
			{
				level++;
				paid = 0;
				machine.ModifyTag(text, (TagState s) =>
				{
					s.Enable().SetInt(level);
				});
				if (flag)
				{
					Resize(machine);
				}
				try
				{
					EmporiumAudioPlayer.Instance.ratchet.Play();
				}
				catch
				{
				}
				int num2 = ((!Rigs.TryGetValue(machine, out var value)) ? 1 : value.RowsPerLevel);
				Notify(flag ? L.K("production.machineUpgrade.apply.capacityUpgradedLevelMoreRow", $"{name}容量升到 {level} 级：可扩容的仓储格各多 {num2} 行", (System.FormattableString)$"{name} capacity upgraded to level {level}: {num2} more row(s) in each expandable storage grid") : L.K("production.machineUpgrade.apply.powerSaverInstalledLessCharge", name + "装好省电件：每次加工少耗 1 电", (FormattableString)$"{name} power saver installed: 1 less charge per run"));
				ModLog.Info(string.Format("机器升级：{0} {1} {2} 级", machine.identifier, flag ? "容量" : "省电", level));
			}
			else
			{
				string upgrade = flag ? L.K("production.machineUpgrade.apply.capacityLevel", (FormattableString)$"capacity level {level + 1}") : L.K("production.machineUpgrade.apply.powerSaver", "power saver");
				Notify(L.K("production.machineUpgrade.apply.text", string.Format("{0}{1}：{2}/{3}", name, flag ? $"容量 {level + 1} 级" : "省电", paid, num), (FormattableString)$"{name} {upgrade}: {paid}/{num}"));
			}
			machine.ModifyTag(text2, (TagState s) =>
			{
				s.Enable().SetInt(paid);
			});
			Goods.Relabel(machine, Goods.IdOf(machine));
		}
		catch (Exception ex)
		{
			ModLog.Warn("机器升级失败：" + ex.Message);
		}
	}

	public static string Status(GameItem m)
	{
		try
		{
			if (m == null)
			{
				return "";
			}
			if (m.identifier == ProductionWorkBay.ItemId) return "\n\n" + ProductionWorkBay.UpgradeStatus(m);
			List<string> list = new List<string>();
			if (Is(m, CapMachines))
			{
				int num = Level(m, "CORP_UPG_CAP_INT");
				int num2 = Level(m, "CORP_UPG_CAP_PAID_INT");
				list.Add((num >= CapCost.Length) ? L.K("production.machineUpgrade.status.capacityLevelMax", $"容量：{num}/{CapCost.Length} 级（已满）", (System.FormattableString)$"Capacity: level {num}/{CapCost.Length} (max)") : L.K("production.machineUpgrade.status.capacityLevelDragMetalIngots", $"容量：{num}/{CapCost.Length} 级，拖金属锭上来升级（这级 {num2}/{CapCost[num]} 块，纯度不限）", (System.FormattableString)$"Capacity: level {num}/{CapCost.Length}. Drag metal ingots onto it to upgrade ({num2}/{CapCost[num]} for this level, any purity)"));
			}
			if (Is(m, PowerMachines))
			{
				list.Add(Level(m, "CORP_UPG_PWR_INT") > 0
					? L.K("production.machineUpgrade.status.powerSaverInstalledLessCharge", "省电：已装，每次加工少耗 1 电", "Power saver: installed, 1 less charge per run")
					: L.F("production.machineUpgrade.status.powerSaverDragElectronicComponents", "省电：拖电子元件上来安装（{0}/{1} 个），每次加工少耗 1 电", "Power saver: drag electronic components onto it ({0}/{1}) for 1 less charge per run",
						Level(m, "CORP_UPG_PWR_PAID_INT"), 3));
			}
			return (list.Count == 0) ? "" : ("\n\n" + string.Join("\n", list));
		}
		catch
		{
			return "";
		}
	}

	private static void Notify(string text)
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
