using System;
using System.Collections.Generic;
using System.Reflection;
using CorpPresence.Runtime;
using CorpPresence.UI;
using HarmonyLib;
using UnityEngine;

namespace CorpPresence;

internal static partial class Hooks
{
	private static bool BeforeGetSprite(string key, ref Sprite __result)
	{
		return BeforeResolveSprite(key, ref __result);
	}

	private static bool BeforeLoadFromAtlas(string name, ref Sprite __result)
	{
		return BeforeResolveSprite(name, ref __result);
	}

	private static bool BeforeResolveItemSprite(GameItemElement __instance, ref Sprite __result)
	{
		try
		{
			Sprite appearance = MachineAppearance.SpriteOverride(__instance);
			if (appearance != null)
			{
				__result = appearance;
				return false;
			}
			return BeforeResolveSprite(__instance?.spritePath, ref __result);
		}
		catch
		{
			return true;
		}
	}

	private static bool BeforeResolveSprite(string name, ref Sprite __result)
	{
		try
		{
			Sprite sprite = Cards.Find(name) ?? Goods.FindSprite(name);
			if (sprite == null)
			{
				return true;
			}
			__result = sprite;
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static bool BeforeActivateItem(GameItem __instance)
	{
		try
		{
			string text = Goods.IdOf(__instance);
			if (text != null && !Goods.VanillaUse(__instance))
			{
				if (Goods.CanUse(__instance))
				{
					Goods.Use(__instance, text);
				}
				return false;
			}
			if (!CardView.IsOurs(__instance))
			{
				return true;
			}
			CardView.Open(__instance);
			return false;
		}
		catch (Exception ex)
		{
			ModLog.Warn("使用物品失败：" + ex.Message);
			return true;
		}
	}

	private static bool BeforeChargeBattery(GameItem __0)
	{
		try
		{
			return Goods.IdOf(__0) != "anchor_cell";
		}
		catch
		{
			return true;
		}
	}

	private static void AfterBatteryChange(GameItem __0)
	{
		Goods.RefreshBattery(__0);
	}

	private static void AfterCanActivate(GameItem __instance, ref bool __result)
	{
		try
		{
			if (Goods.IdOf(__instance) != null && !Goods.VanillaUse(__instance))
			{
				__result = Goods.CanUse(__instance);
			}
		}
		catch
		{
		}
	}

	private static bool BeforePurifierUsed(GameItem purifier, GameItem container)
	{
		try
		{
			if (Goods.IdOf(purifier) != "leviathan" || !DavreCo.IsGutter(container))
			{
				return true;
			}
			GenericConfirmationUIManager.Instance?.OpenPanelSingleButton(L.K("integration.hooks.beforePurifierUsed.leviathanCanTHandleGutterflow", "利维坦对付不了沟渠水。\n只能净化铁锈水和幽灵水。", "The Leviathan can't handle Gutterflow.\nIt only purifies Rustwater and Ghostwater."), L.K("integration.hooks.beforePurifierUsed.oK", "确定", "OK"));
			return false;
		}
		catch (Exception ex)
		{
			ModLog.Warn("利维坦净水失败：" + ex.Message);
			return true;
		}
	}
}
