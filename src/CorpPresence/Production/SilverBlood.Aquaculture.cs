using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static partial class SilverBlood
{

	public static void RegisterSpecies()
	{
		if (!GameResources.Ready) throw new InvalidOperationException("银血物种资源尚未就绪");
		if (FishSpecies.All.TryGetValue("FISH_CORP_SILVERBLOOD", out var existing))
		{
			if (!ReferenceEquals(existing, Species)) throw new InvalidOperationException("银血物种标识已由其他内容占用");
			return;
		}
		_ownsSpecies = true;
		FishSpecies.All["FISH_CORP_SILVERBLOOD"] = Species;
	}

	public static bool IsCreature(GameItem item)
	{
		try
		{
			return item != null && item.GetTagReadonly("FISH_SPECIES_TAG").GetString() == "FISH_CORP_SILVERBLOOD";
		}
		catch
		{
			return false;
		}
	}

	public static bool BeforeTankFits(int originX, int originY, List<Vector2Int> cells, int ___width, int ___height, ref bool __result)
	{
		// Native FindFit can try a negative saved origin. Reject invalid cells, then let
		// the original search choose another position; never cancel nightly processing.
		foreach (Vector2Int cell in cells)
		{
			long x = (long)originX + cell.x;
			long y = (long)originY + cell.y;
			if (x < 0 || y < 0 || x >= ___width || y >= ___height)
			{
				__result = false;
				return false;
			}
		}
		return true;
	}

	private static GameItem BuildCreature()
	{
		RegisterSpecies();
		GameItem gameItem = Empty().SetName(Species.GetDisplayName()).SetSpriteAndShape(Species.spriteAtlas, Species.stageSprites[2]);
		gameItem.unitValue = 0L;
		AquacultureHelper.InitFishItem(gameItem, Species);
		AudioHelper.InitMeaty(gameItem);
		return gameItem;
	}

	private static void SetupCreature(GameItem item)
	{
		AquacultureHelper.UpdateNameAndSprite(item);
		item.onCycleEndSlotItemFunc = (GameItem cur, GameInventory inv, SlotMarker _) =>
		{
			Nightly(cur, inv);
		};
	}

	private static void Nightly(GameItem item, GameInventory inv)
	{
		try
		{
			if (AquacultureHelper.IsDead(item))
			{
				return;
			}
			// Age, feeding and growth are owned exclusively by native TickTank.
			if (AquacultureHelper.IsInTank(inv))
			{
				if (item.IsTag("CORP_SB_DRY_TAG") || AquacultureHelper.GetStage(item) < FishStage.Adult || AquacultureHelper.GetHealth(item) < Species.maxHealth * 0.9f)
				{
					return;
				}
				int cap = BloodCap(item);
				int blood = Blood(item);
				if (blood < cap)
				{
					item.ModifyTag("CORP_SB_BLOOD_ML", (TagState s) =>
					{
						s.Enable().SetInt(Math.Min(cap, blood + 40));
					});
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("银血三叶蜥每晚处理失败：" + ex.Message);
		}
	}

	public static void BeforeApplyWaterChecks(ref List<GameItem> live, bool isPowered)
	{
		try
		{
			// Unpowered stock must wait for AfterTickTank's health exemption.
			live = SilverBloodFlow.WaterChecks(live, IsCreature, ApplyWaterExemption, !isPowered);
		}
		catch (Exception ex)
		{
			ModLog.Warn("三叶蜥养殖缸水量检查失败：" + ex.Message);
		}
	}

	private static void ApplyWaterExemption(GameItem item)
	{
		if (AquacultureHelper.GetHunger(item) >= Species.maxHunger * 0.5f)
			AquacultureHelper.ModifyHealth(item, Species.maxHealth * 0.1f);
	}

	public static void BeforeTickTank(GameGridInventory tankGrid, GameItem waterTank, bool isPowered, out Dictionary<GameItem, float> __state)
	{
		__state = null;
		if (tankGrid == null)
		{
			return;
		}
		try
		{
			int waterMl = AquacultureHelper.GetWaterMl(waterTank);
			foreach (GameItem childItem in tankGrid.childItems)
			{
				if (IsCreature(childItem) && !AquacultureHelper.IsDead(childItem))
				{
					if (waterMl < Species.stageWaterNeedMl[(int)AquacultureHelper.GetStage(childItem)])
					{
						childItem.EnableTag("CORP_SB_DRY_TAG");
					}
					else
					{
						childItem.DisableTag("CORP_SB_DRY_TAG");
					}
					if (!isPowered)
					{
						(__state ?? (__state = new Dictionary<GameItem, float>()))[childItem] = AquacultureHelper.GetHealth(childItem);
					}
				}
			}
		}
		catch
		{
			__state = null;
		}
	}

	public static void AfterTickTank(Dictionary<GameItem, float> __state)
	{
		if (__state == null)
		{
			return;
		}
		foreach (KeyValuePair<GameItem, float> item in __state)
		{
			try
			{
				if (!AquacultureHelper.IsDead(item.Key))
				{
					float health = AquacultureHelper.GetHealth(item.Key);
					if (health < item.Value)
					{
						AquacultureHelper.ModifyHealth(item.Key, item.Value - health);
					}
					if (AquacultureHelper.GetHealth(item.Key) <= 0f)
						AquacultureHelper.OnFishDead(item.Key);
				}
			}
			catch
			{
			}
		}
	}

	public static void AfterWillSuffocate(GameItem stock, ref bool __result)
	{
		try
		{
			if (__result && IsCreature(stock) && AquacultureHelper.GetHostRAS(stock) == null)
			{
				__result = false;
			}
		}
		catch
		{
		}
	}

	public static bool BeforeFillet(GameItem fish, ref List<GameItem> __result)
	{
		try
		{
			if (!IsCreature(fish))
			{
				return true;
			}
			FishStage stage = AquacultureHelper.GetStage(fish);
			string id;
			if (stage >= FishStage.Elder)
			{
				id = "carapace_full";
			}
			else
			{
				id = ((stage >= FishStage.Adult) ? "carapace_medium" : "carapace_small");
			}
			__result = new List<GameItem>();
			GameItem gameItem = Goods.Make(id);
			if (gameItem != null)
			{
				GeneralHelper.SetItemOwned(gameItem, GeneralHelper.IsItemOwned(fish));
				__result.Add(gameItem);
			}
			return false;
		}
		catch (Exception ex)
		{
			ModLog.Warn("三叶蜥屠宰失败：" + ex.Message);
			return true;
		}
	}

}



