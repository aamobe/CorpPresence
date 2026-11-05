using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static partial class SilverBlood
{
	private static Liquid _ownedRaw;
	private static Liquid _ownedIsomer;
	private static bool _ownsSpecies;

	public static void InstallResources()
	{
		if (!GameResources.Ready) throw new InvalidOperationException("公司液体资源尚未就绪");
		RegisterLiquid();
		if (FishSpecies.All.TryGetValue("FISH_CORP_SILVERBLOOD", out var existing) && !ReferenceEquals(existing, Species))
			throw new InvalidOperationException("银血物种标识已由其他内容占用");
		RegisterSpecies();
		if (!ReferenceEquals(FishSpecies.All["FISH_CORP_SILVERBLOOD"], Species)) throw new InvalidOperationException("银血物种注册未完成");
	}

	public static void UninstallResources()
	{
		RemoveOwned("corp_isomer_raw", ref _ownedRaw);
		RemoveOwned("corp_isomer", ref _ownedIsomer);
		if (_ownsSpecies && FishSpecies.All.TryGetValue("FISH_CORP_SILVERBLOOD", out var current) && ReferenceEquals(current, Species))
			FishSpecies.All.Remove("FISH_CORP_SILVERBLOOD");
		_ownsSpecies = false;
	}

	private static void RemoveOwned(string id, ref Liquid owned)
	{
		if (owned == null) return;
		if (Liquid.LiquidDict.TryGetValue(id, out var current) && ReferenceEquals(current, owned)) Liquid.LiquidDict.Remove(id);
		Liquid.Liquids.Remove(owned);
		owned = null;
	}

	public static void RegisterLiquid()
	{
		if (!GameResources.Ready) throw new InvalidOperationException("公司液体资源尚未就绪");
		AddLiquid("corp_isomer_raw", L.T("粗异构蛋白", "Raw Isomer Protein"), "9aa3b4", ref _ownedRaw);
		AddLiquid("corp_isomer", L.T("异构蛋白", "Isomer Protein"), "c8d0dc", ref _ownedIsomer);
	}

	private static void AddLiquid(string id, string name, string color, ref Liquid owned)
	{
			if (!Liquid.LiquidDict.ContainsKey(id))
			{
				string text = id.ToUpperInvariant();
				Liquid liquid = new Liquid
				{
					displayName = name,
					identifier = id,
					part = "PART_" + text,
					currentPart = "CURRENT_PART_" + text,
					canBeRemovedByPurifier = false,
					particleSize = 1,
					color = color,
					colorMultiplier = 2
				};
				owned = liquid;
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

	private static GameItem LiquidShell(string id, int ml, int basePrice)
	{
		RegisterLiquid();
		GameItem gameItem = Empty();
		gameItem.identifier = id;
		WaterHelper.InitLiquidContainerItem(gameItem, ml, basePrice, isFilterContainer: false, isGraduated: true);
		AudioHelper.InitGlass(gameItem);
		return gameItem;
	}

	private static readonly (string Id, double Ml)[] BloodRecipe =
	{
		(RawId, 10), ("water", 28), ("protein", 5), ("fat", 3), ("microbe", 2), ("organic_waste", 2)
	};

	private static void FillBlood(GameItem item) => FillLiquids(item, BloodRecipe);

	internal static bool IsRecoverySample(GameItem item)
	{
		if (item is not GameItemElement element || element.IsDestroyed() || item.identifier != BloodId ||
			item.parentInventory == null || item.unitCount != 1 || !item.IsTag("LIQUID_CONTAINER_TAG") ||
			!GeneralHelper.IsItemOwned(item))
			return false;

		var tags = item.modifiedState.dict;
		foreach (var (id, ml) in BloodRecipe)
		{
			if (!tags.TryGetValue(Liquid.GetCurrentPartFromId(id), out var part) || part.GetInt() != (int)(ml * 1000))
				return false;
		}
		// Reject any extra constituent, including liquids registered by another mod.
		foreach (var liquid in Liquid.Liquids)
		{
			if (tags.TryGetValue(liquid.currentPart, out var part) && part.GetInt() != 0 &&
				!Array.Exists(BloodRecipe, entry => entry.Id == liquid.identifier))
				return false;
		}
		return true;
	}

	private static void FillLiquids(GameItem item, params (string Id, double Ml)[] parts)
	{
		for (int i = 0; i < parts.Length; i++)
		{
			var (liquidId, num) = parts[i];
			if (num > 0.0)
			{
				int amount = (int)Math.Round(num * 1000.0);
				WaterHelper.AddLiquid(item, liquidId, amount);
#if WATER_TRACE
				WaterTrace.ModLiquid(item, liquidId, amount);
#endif
			}
		}
		// AddLiquid updates the feature and sprite but does not recalculate the price.
		WaterFeatureHelper.OnLiquidChanged(item, WaterFeatureHelper.GetWaterFeature(item).IsExposed());
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

}


