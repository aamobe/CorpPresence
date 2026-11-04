using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static partial class SilverBlood
{

	private static int Blood(GameItem item)
	{
		return item.GetTagReadonly("CORP_SB_BLOOD_ML").GetInt();
	}

	private static int BloodCap(GameItem item)
	{
		FishStage stage = AquacultureHelper.GetStage(item);
		if (stage >= FishStage.Adult)
		{
			return Mathf.RoundToInt((float)Species.stageWeightGrams[(int)stage] * 0.08f);
		}
		return 0;
	}

	private static void InitDrawer(GameItem tool)
	{
		tool.maySelectSlotItemFunc = (GameItem _, GameInventory _, SlotMarker _) => true;
		tool.canSelectSlotItemFunc = (GameItem _, GameInventory _, SlotMarker _) => true;
		tool.mayThisTargetItemFunc = (Func<GameItem, GameItem, bool>)Delegate.Combine(tool.mayThisTargetItemFunc, (Func<GameItem, GameItem, bool>)((GameItem s, GameItem t) => GeneralHelper.IsItemOwned(s, t) && IsCreature(t)));
		tool.canThisTargetItemFunc = (Func<GameItem, GameItem, bool>)Delegate.Combine(tool.canThisTargetItemFunc, (Func<GameItem, GameItem, bool>)((GameItem s, GameItem t) => GeneralHelper.IsItemOwned(s, t) && CanDraw(t)));
		tool.onThisTargetItemFunc = (Action<GameItem, GameItem>)Delegate.Combine(tool.onThisTargetItemFunc, (Action<GameItem, GameItem>)((GameItem _, GameItem t) =>
		{
			if (IsCreature(t))
			{
				Draw(t);
			}
		}));
	}

	private static float DrawCost(GameItem fish)
	{
		int num = BloodCap(fish);
		float num2 = ((num > 0) ? Mathf.Clamp01((float)Blood(fish) / (float)num) : 1f);
		return Species.maxHealth * (0.15f + 0.2f * (1f - num2));
	}

	private static bool CanDraw(GameItem fish)
	{
		if (IsCreature(fish) && !AquacultureHelper.IsDead(fish) && Blood(fish) >= 50)
		{
			return AquacultureHelper.GetHealth(fish) > DrawCost(fish);
		}
		return false;
	}

	private static void Draw(GameItem fish)
	{
		try
		{
			if (!CanDraw(fish))
			{
				return;
			}
			int num = Blood(fish) / 50;
			float num2 = DrawCost(fish);
			GameInventory parentInventory = fish.parentInventory;
			int tubes = 0;
			for (int i = 0; i < num; i++)
			{
				GameItem gameItem = Goods.Make("silver_blood");
				if (gameItem != null)
				{
					bool placed = false;
					try
					{
						GeneralHelper.SetItemOwned(gameItem);
						placed = Placed(parentInventory, gameItem);
					}
					finally
					{
						if (!placed && gameItem.parentInventory == null) gameItem.Destroy();
					}
					if (!placed)
					{
						break;
					}
					tubes++;
				}
			}
			if (tubes == 0)
			{
				Tell(L.T("店里放不下了，先腾点地方再采血。", "No room in the shop. Make some space before drawing blood."));
				return;
			}
			fish.ModifyTag("CORP_SB_BLOOD_ML", (TagState s) =>
			{
				s.Enable().SetInt(Blood(fish) - tubes * 50);
			});
			AquacultureHelper.ModifyHealth(fish, 0f - num2);
			CarmichaelCo.OnBloodDrawn(tubes);
			try
			{
				AudioManager.Instance.Play3("click_vintage");
			}
			catch
			{
			}
			ModLog.Info($"采血：{tubes} 管银血，扣健康 {num2:0}");
		}
		catch (Exception ex)
		{
			ModLog.Warn("采血失败：" + ex.Message);
		}
	}

	private static bool Placed(GameInventory where, GameItem item)
	{
		GraphUtils.EmporiumTryAdd(where, item);
		try
		{
			return item.parentInventory != null;
		}
		catch
		{
			return false;
		}
	}

}

