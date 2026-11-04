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
		if (entry != null)
		{
			ProductionCompatibility.RemoveLegacyPause(item);
		}
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
			Apply(item, array, Inherited(entry.LegacyId));
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

	private static string[] Inherited(string legacyId)
	{
		if (InheritedCache.TryGetValue(legacyId, out var value))
		{
			return value;
		}
		GameItem template = ItemRegistry.Create(legacyId) ?? throw new InvalidOperationException("物品标签模板缺失：" + legacyId);
		try
		{
			return InheritedCache[legacyId] = template.itemTypes.ToArray();
		}
		finally
		{
			try
			{
				if (template.parentInventory == null && !StorePlacement.Destroyed(template))
				{
					template.Destroy();
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
