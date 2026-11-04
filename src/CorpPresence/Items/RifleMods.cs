using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CorpPresence;

internal static class RifleMods
{
	public const string GoodsId = "rosen_rifle";

	private static readonly Dictionary<string, (string View, float X, float Y)> Views = new Dictionary<string, (string, float, float)>
	{
		["silencer"] = ("silencer_view", 124f, -3f),
		["silencer2"] = ("silencer2_view", 124f, -4f),
		["silencer_makeshift"] = ("bottle_view", 124f, -2f),
		["barrel"] = ("barrel_view", 124f, -3f),
		["compensator"] = ("compensator_view", 124f, -4f),
		["rds"] = ("rds_view2", 52f, 4f),
		["rds_makeshift"] = ("rds_makeshift_view", 58f, 5f),
		["laser"] = ("laser_view", 90f, -14f),
		["laser2"] = ("laser2_view", 90f, -14f),
		["laser_toy"] = ("laser_toy_view", 76f, -15f),
		["grip"] = ("grip_view", 55f, -13f)
	};

	private static readonly HashSet<string> Muzzle = new HashSet<string> { "silencer", "silencer2", "silencer_makeshift", "barrel", "compensator" };

	private static readonly ConditionalWeakTable<GameInventory, object> Wrapped = new ConditionalWeakTable<GameInventory, object>();

	public static bool IsRifle(GameItem item)
	{
		if (item != null)
		{
			return Goods.IdOf(item) == "rosen_rifle";
		}
		return false;
	}

	public static void Attach(GameItem rifle)
	{
		if (rifle == null)
		{
			return;
		}
		rifle.getItemSpriteModifierForItemFunc = Overlays;
		try
		{
			foreach (GameInventory item in GraphUtils.FindChildrenType<GameInventory>(Compat.Cast<GraphNodeStorage>(rifle)))
			{
				if (!Wrapped.TryGetValue(item, out var _))
				{
					Wrapped.Add(item, null);
					Func<GameItem, GameInventory, bool> may = item.mayInventoryAddItemFunc;
					item.mayInventoryAddItemFunc = (GameItem mod, GameInventory target) => mod != null && !mod.IsTag("MOD_STOCK_TAG") && !mod.IsTag("MOD_MAGAZINE_TAG") && (may == null || may(mod, target));
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("步枪改装槽设置失败：" + ex.Message);
		}
	}

	private static List<ItemSpriteModifier> Overlays(GameItem gun)
	{
		List<ItemSpriteModifier> list = new List<ItemSpriteModifier>();
		try
		{
			foreach (GameInventory item in GraphUtils.FindChildrenType<GameInventory>(Compat.Cast<GraphNodeStorage>(gun)))
			{
				foreach (GameItem childItem in item.childItems)
				{
					if (childItem != null && childItem.identifier != null && Views.TryGetValue(childItem.identifier, out (string, float, float) value))
					{
						list.Add(new ItemSpriteModifier(RenderHandler.LoadFromAtlas("Items/items_mod", value.Item1), new Vector2(value.Item2, value.Item3)));
					}
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("步枪配件显示失败：" + ex.Message);
		}
		return list;
	}

	public static void AfterParentShape(GameItem __instance, GameItem newParentItem, ref (GridShape, GridShape) __result)
	{
		try
		{
			string text = __instance?.identifier;
			if (text != null && IsRifle(newParentItem))
			{
				if (Muzzle.Contains(text))
				{
					GridShape modifiedShape = __instance.modifiedShape;
					__result = (new GridShapeBuilder().SetDataFill(modifiedShape.width, modifiedShape.height, 1).SetTransform(8, 0, flipped: false, 0), null);
				}
				else if (text == "grip")
				{
					__result = (new GridShapeBuilder().SetDataFill(2, 1, 1).SetTransform(4, 2, flipped: false, 0), null);
				}
			}
		}
		catch
		{
		}
	}
}
