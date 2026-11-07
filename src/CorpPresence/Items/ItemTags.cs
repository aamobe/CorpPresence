using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class ItemTags
{
	private static readonly Dictionary<string, string[]> InheritedCache = new Dictionary<string, string[]>();

	public static void Apply(GameItem item, string[] desired, string[] inherited)
	{
		string[] array = inherited;
		foreach (string gameItemType in array)
		{
			item.RemoveGameItemType(gameItemType);
		}
		item.RemoveAllGameItemType();
		array = desired;
		foreach (string gameItemType2 in array)
		{
			item.SetGameItemType(gameItemType2);
		}
	}

	public static void Repair(GameItem item)
	{
		if (item == null)
		{
			return;
		}
		ItemIdentity.Entry entry = ItemIdentity.Find(item.identifier, item.spritePath);
		Perfume.RepairFlowerGenetics(item);
		if (entry != null) ProductionCompatibility.RemoveLegacyPause(item);
		Goods.Relabel(item, entry?.Goods);
		Cards.Relabel(item, entry);
		PastelCo.AfterGenuineAction(item);
		if (entry?.Goods == "rosen_rifle")
		{
			RifleMods.Attach(item);
		}
		string[] array = Goods.TypesOf(entry?.Goods);
		if (array != null)
		{
			string goods = entry.Goods;
			NormalizeMedicine(item, goods);
			// These goods have their own shells and do not use their legacy templates for tags.
			bool independentTypes = Goods.HasIndependentShell(goods);
			Apply(item, array, independentTypes ? Array.Empty<string>() : Inherited(entry.LegacyId));
			if ((goods == "crab_stick" && item.unitValue == 185) || (goods == "fish_can" && item.unitValue == 119))
				item.SetValue(Goods.ValueOf(goods));
			if (entry?.Goods == "anchor_cell" && !item.IsTag("CONTRABAND_ITEM_TAG"))
			{
				PowerHelper.InitDisposable(item);
				ContrabandHelper.InitContrabandItem(item);
			}
			if (item.IsTag("CONTRABAND_ITEM_TAG"))
			{
				item.SetGameItemType("CONTRABAND");
			}
			Forge.ApplyLuxury(item);
		}
	}

	private static readonly string[] LegacyMedicineChemTags =
	{
		"CHEMICAL_PRODUCT_TAG", "CHEM_ITEM_TAG", "BLENDABLE_TAG", "SOLUBLE_TAG",
		"CHEMICAL_INTENT_TAG", "ACTIVATE_INGREDIENT_TAG", "CHEMICAL_PRODUCT_PURITY_TAG"
	};

	private static bool IsIndependentMedicine(string goods) => goods == "painblock" || goods == "stamina";

	private static void NormalizeMedicine(GameItem item, string goods)
	{
		if (!IsIndependentMedicine(goods)) return;
		RemoveLegacyChemTags(item.state);
		RemoveLegacyChemTags(item.modifiedState);
	}

	private static void RemoveLegacyChemTags(TagSystem state)
	{
		if (state?.dict == null) return;
		foreach (var tag in LegacyMedicineChemTags) state.dict.Remove(tag);
		var ingredients = new List<string>();
		foreach (var tag in state.dict.Keys)
			if (tag.StartsWith("CURRENT_PART_", StringComparison.Ordinal)) ingredients.Add(tag);
		foreach (var tag in ingredients) state.dict.Remove(tag);
	}

	internal static void RemoveRestoredMedicinePurity()
	{
		var store = EmporiumEntry.Instance;
		if (store == null) return;
		foreach (var item in store.GetAllItems())
		{
			if (item == null) continue;
			string goods = ItemIdentity.GoodsKey(item.identifier, item.spritePath);
			if (!IsIndependentMedicine(goods)) continue;
			NormalizeMedicine(item, goods);
			// The native load attaches saved features after DecodeNodes has returned.
			for (int i = item.itemFeatures.Count - 1; i >= 0; i--)
			{
				var feature = item.itemFeatures[i];
				if (feature?.category == "CATEGORY_CHEMICAL_PURITY") item.RemoveItemFeature(feature);
			}
		}
	}

	private static string[] Inherited(string legacyId)
	{
		if (InheritedCache.TryGetValue(legacyId, out var value))
		{
			return value;
		}
		GameItem gameItem = ItemRegistry.Create(legacyId) ?? throw new InvalidOperationException("物品标签模板缺失：" + legacyId);
		try
		{
			string[] array = new string[gameItem.itemTypes.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = gameItem.itemTypes[i];
			}
			return InheritedCache[legacyId] = array;
		}
		finally
		{
			try
			{
				if (gameItem.parentInventory == null && !StorePlacement.Destroyed(gameItem))
				{
					gameItem.Destroy();
				}
			}
			catch
			{
			}
		}
	}

	public static void AfterDecode(GameItem __result)
	{
		if (__result == null)
		{
			return;
		}
		try
		{
			Repair(__result);
			List<GameItem> list = GraphUtils.FindAllChildrenType<GameItem>(Compat.Cast<GraphNodeStorage>(__result));
			if (list == null)
			{
				return;
			}
			foreach (GameItem item in list)
			{
				try
				{
					Repair(item);
				}
				catch (Exception ex)
				{
					ModLog.Warn("公司物品标签修复失败：" + ex.Message);
				}
			}
		}
		catch (Exception ex2)
		{
			ModLog.Warn("公司物品读档标签同步失败：" + ex2.Message);
		}
	}
}
