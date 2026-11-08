using System;
using System.Collections.Generic;
using System.Reflection;
using CorpPresence.Runtime;
using CorpPresence.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CorpPresence;

internal static class Hooks
{
	private static Action _nightly;

	private static string _reportedId;

	private static int _revBefore;

	[ThreadStatic]
	private static int _pendingVendingWater;

	public static void InstallCore(Harmony h)
	{
		Type typeFromHandle = typeof(Hooks);
		Type typeFromHandle2 = typeof(ItemRegistry);
		Patch<SaveManager>(h, "DecodeNodes", typeFromHandle2, "BeforeDecode", null, new Type[1] { typeof(List<SaveItemNode>) });
		Patch<SaveManager>(h, "DecodeNodes", typeof(ItemTags), null, "AfterDecode", new Type[1] { typeof(List<SaveItemNode>) });
		Patch<SaveManager>(h, "EncodeNode", typeFromHandle2, "BeforeEncode", null, new Type[1] { typeof(GameItem) });
		Patch<PlayerStore>(h, "SaveGame", typeFromHandle, "BeforeSave");
		Patch<PlayerStore>(h, "LoadGame", typeFromHandle, nameof(BeforeStoreTransition), nameof(AfterLoadGame));
		Patch<PlayerStore>(h, "StartNewGame", typeFromHandle, nameof(BeforeStoreTransition), "ResetState");
		Patch(h, AccessTools.Method(typeof(ModConfigWindow), nameof(ModConfigWindow.Open), new[] { typeof(LoadedMod) }),
			"ModConfigWindow.Open", typeof(ConfigLocalization), postfix: nameof(ConfigLocalization.AfterOpen));
#if TESTKIT
		SaveDiag.Install(h);   // 测试版：存档出错时把出错物品写进日志
#endif
	}

	private static void InstallGameplay(Harmony h)
	{
		Type typeFromHandle = typeof(Hooks);
		Type typeFromHandle2 = typeof(ItemRegistry);
		Patch<PlayerStore>(h, "BeginDay", typeFromHandle, null, "AfterBeginDay");
		MethodInfo endDrag = AccessTools.Method(typeof(ItemMouseDragHandler), "EndDrag", new[] { typeof(bool) })
			?? AccessTools.Method(typeof(ItemMouseDragHandler), "EndDrag", Type.EmptyTypes);
		Patch(h, endDrag, "ItemMouseDragHandler.EndDrag", typeof(DragFix), nameof(DragFix.BeforeEndDrag), nameof(DragFix.AfterEndDrag));
		Patch<StoreClientManager>(h, "GenerateClient", typeFromHandle, null, "AfterGenerateClient");
		Patch<PlayerStore>(h, "GetNextClient", typeFromHandle, "BeforeNextClient");
		Patch<GameItem>(h, "GetNegociatedValue", typeFromHandle, null, "AfterNegociatedValue");
		Patch<StoreClient>(h, nameof(StoreClient.ClientExposeFeature), typeof(Malice.Rebels), nameof(Malice.Rebels.BeforeClientExposeFeature), null, new[] { typeof(GameItem) });
		Patch<StoreClient>(h, "OnItemSold", typeFromHandle, null, "AfterClientSold", new Type[2]
		{
			typeof(GameItem),
			typeof(int)
		});
		Patch<StoreClient>(h, "OnItemBought", typeFromHandle, null, "AfterClientBought", new Type[2]
		{
			typeof(GameItem),
			typeof(int)
		});
		Patch<RunStats>(h, nameof(RunStats.OnItemSold), typeFromHandle, postfix: nameof(AfterCounterWaterSale),
			args: new[] { typeof(GameItem), typeof(int), typeof(int), typeof(StoreClient) });
		Patch<VendingMachineHelper>(h, nameof(VendingMachineHelper.TrackMislabelledSale), typeFromHandle, postfix: nameof(AfterVendingItemSale),
			args: new[] { typeof(GameItem), typeof(int) });
		Patch<RunStats>(h, nameof(RunStats.OnVendingSale), typeFromHandle, postfix: nameof(AfterVendingSale), args: new[] { typeof(int) });
		Patch<RunStats>(h, nameof(RunStats.OnFountainSale), typeFromHandle, postfix: nameof(AfterFountainWaterSale), args: new[] { typeof(int) });
		Patch<PlayerStore>(h, "EndDay", typeFromHandle, "BeforeEndDay");
		Patch<StorePhoneClient>(h, "InitPhoneClientDict", typeFromHandle, null, "AfterInitPhones");
		Patch<StorePhoneClient>(h, "GetCallDialog", typeFromHandle, null, "AfterGetCallDialog");
		Patch<PhoneUIManager>(h, "WillAnswerCall", typeFromHandle, null, "AfterWillAnswer", new Type[1] { typeof(long) });
		Patch<PhoneUIManager>(h, "HandleCall", typeFromHandle, "BeforeHandleCall", null, new Type[1] { typeof(long) });
		Patch<StorePhoneClient>(h, "GetLocalizedDisplayName", typeFromHandle, null, "AfterPhoneName");
		Patch<DialoguePhoneUIManager>(h, nameof(DialoguePhoneUIManager.DisplayClientText), typeof(PhoneDialogueLayout),
			postfix: nameof(PhoneDialogueLayout.AfterClientText), args: new[] { typeof(Dialogue) });
		Patch<DialoguePhoneUIManager>(h, nameof(DialoguePhoneUIManager.DisplayerPlayerText), typeof(PhoneDialogueLayout),
			postfix: nameof(PhoneDialogueLayout.AfterPlayerText), args: new[] { typeof(Dialogue) });
		MethodInfo scalePhoneBubbles = AccessTools.Method(typeof(DialoguePhoneUIManager), "ApplyBubbleScale", Type.EmptyTypes);
		if (scalePhoneBubbles != null)
			Patch(h, scalePhoneBubbles, "DialoguePhoneUIManager.ApplyBubbleScale", typeof(PhoneDialogueLayout),
				prefix: nameof(PhoneDialogueLayout.Reset), postfix: nameof(PhoneDialogueLayout.AfterBubbleScale));
		Patch<DialogUIManager>(h, nameof(DialogUIManager.ScaleByDialogSpeed), typeof(ReadingPace),
			postfix: nameof(ReadingPace.AfterDialoguePause), args: new[] { typeof(float) });
		Patch<StoreUIManager>(h, "GetLocaleSpeedMultiplier", typeof(ReadingPace), postfix: nameof(ReadingPace.AfterTypewriterSpeed));
		Patch<ReputationUIManager>(h, nameof(ReputationUIManager.Notify), typeof(ReadingPace),
			postfix: nameof(ReadingPace.AfterNotification), args: new[] { typeof(string), typeof(string) });
		Patch<ReputationUIManager>(h, nameof(ReputationUIManager.NotifyReputation), typeof(ReadingPace),
			postfix: nameof(ReadingPace.AfterNotification), args: new[] { typeof(string), typeof(int) });
		Patch<GameItemElement>(h, "ResolveSpriteByName", typeFromHandle, "BeforeResolveSprite", null, new Type[1] { typeof(string) });
		Patch<GameItemElement>(h, "ResolveItemSprite", typeFromHandle, "BeforeResolveItemSprite");
		Patch<SpriteDict>(h, "GetSprite", typeFromHandle, "BeforeGetSprite", null, new Type[1] { typeof(string) });
		Patch<ItemConditionActionList>(h, "GenuineCigaretteAction", typeof(PastelCo), null, "AfterGenuineAction", new Type[1] { typeof(GameItem) });
		Patch<HusbandryHelper>(h, "CreateButcherCuts", typeof(Forge), "BeforeButcherCuts", "AfterButcherCuts", new Type[1] { typeof(GameItem) });
		Patch<GameItem>(h, "BuildShapeFromSprite", typeof(Goods), "BeforeBuildShape", null, new Type[2]
		{
			typeof(string),
			typeof(string)
		});
		Patch<Image>(h, "IsRaycastLocationValid", typeof(Goods), null, "AfterRaycastValid", new Type[2]
		{
			typeof(Vector2),
			typeof(Camera)
		});
		Patch<AquacultureHelper>(h, "CreateFilletPieces", typeof(SilverBlood), "BeforeFillet", null, new Type[1] { typeof(GameItem) });
		// 私有嵌套类用固定的“类型:方法”字符串查找：加载器扫描时按游戏自身类型识别，不需要声明反射权限
		Patch(h, AccessTools.Method("AquacultureHelper+TankOccupancy:Fits", new Type[3]
		{
			typeof(int), typeof(int), typeof(List<Vector2Int>)
		}), "AquacultureHelper.TankOccupancy.Fits", typeof(SilverBlood), "BeforeTankFits", null);
		Patch<AquacultureHelper>(h, "WillSuffocateTonight", typeof(SilverBlood), null, "AfterWillSuffocate", new Type[1] { typeof(GameItem) });
		Patch<AquacultureHelper>(h, "ApplyWaterChecks", typeof(SilverBlood), "BeforeApplyWaterChecks", null, new Type[3]
		{
			typeof(List<GameItem>),
			typeof(GameItem),
			typeof(bool)
		});
		Patch<AquacultureHelper>(h, "TickTank", typeof(SilverBlood), "BeforeTickTank", "AfterTickTank", new Type[6]
		{
			typeof(GameItem),
			typeof(GameGridInventory),
			typeof(GameItem),
			typeof(List<GameSlotInventory>),
			typeof(GameSlotInventory),
			typeof(bool)
		});
		Patch(h, AccessTools.Method(typeof(ChemicalHelper), "HandleTemperatureReaction", new Type[1] { typeof(GameItem) }), "ChemicalHelper.HandleTemperatureReaction", typeof(SilverBlood), null, "AfterTemperatureReaction");
		Patch(h, AccessTools.Method(typeof(ChemicalHelper), "HandleTemperatureReaction", new Type[1] { typeof(GameItem) }), "ChemicalHelper.HandleTemperatureReaction.Perfume", typeof(Perfume), null, "AfterTemperatureReaction");
		Patch<MaterialDirectory>(h, "MetalIngot", typeof(MachineUpgrade), null, "AfterMakeIngot");
		Patch<MachineHelper>(h, nameof(MachineHelper.GetBodyStorage), typeof(Canning), null, nameof(Canning.AfterMachineBodyStorage),
			new[] { typeof(GameItem), typeof(int) });
		Patch<MaterialDirectory>(h, "Electronic", typeof(MachineUpgrade), null, "AfterMakeElectronic");
		Patch<HealthData>(h, "IsSentinel", typeof(SilverBlood), null, "AfterIsSentinel");
		Patch<StartingPerk>(h, "IsSentinel", typeof(SilverBlood), null, "AfterIsSentinel");
		Patch<StartingPerk>(h, "IsPerkActive", typeof(SilverBlood), null, "AfterIsPerkActive", new Type[1] { typeof(string) });
		Patch<StoreClient>(h, "IsClientBuyingThisItem", typeof(SilverBlood), null, "AfterIsBuying", new Type[1] { typeof(GameItem) });
		Patch<CigaretteUI>(h, "OnInit", typeof(PastelCo), null, "AfterCigaretteUI", new Type[1] { typeof(GameItem) });
		Patch<GameItem>(h, "GetParentShapeModifier", typeof(RifleMods), null, "AfterParentShape", new Type[4]
		{
			typeof(GameItem),
			typeof(SlotMarker),
			typeof(GridShape),
			typeof(TagSystem)
		});
		Patch(h, AccessTools.Method(typeof(RenderHandler), "LoadFromAtlasRaw", new Type[2]
		{
			typeof(string),
			typeof(string)
		}), "RenderHandler.LoadFromAtlasRaw", typeFromHandle, "BeforeLoadFromAtlas");
		Patch<ModItemDirectory>(h, "InitDirectory", typeFromHandle2, null, "AfterInitDirectory");
		Patch<GameItem>(h, "ActivateSlotItem", typeFromHandle, "BeforeActivateItem");
		Patch<PaperUIManager>(h, "CloseAllPaperUI", typeFromHandle, null, "AfterCloseAllPaper");
		Patch<GameItem>(h, "MayActivateSlotItem", typeFromHandle, null, "AfterCanActivate");
		Patch<PowerHelper>(h, "ChargeBattery", typeFromHandle, "BeforeChargeBattery", "AfterBatteryChange", new Type[1] { typeof(GameItem) });
		Patch<PowerHelper>(h, "UpdateEnergyCreditSprite", typeFromHandle, null, "AfterBatteryChange", new Type[1] { typeof(GameItem) });
		Patch<PowerHelper>(h, "SetPowerSourceAt", typeFromHandle, null, "AfterBatteryChange", new Type[2]
		{
			typeof(GameItem),
			typeof(int)
		});
		Patch<PowerHelper>(h, "DrawPowerSource", typeFromHandle, null, "AfterBatteryChange", new Type[2]
		{
			typeof(GameItem),
			typeof(int)
		});
		Patch<PowerHelper>(h, "ChargeBattery", typeFromHandle, "BeforeChargeBattery", "AfterBatteryChange", new Type[3]
		{
			typeof(GameItem),
			typeof(int),
			typeof(bool)
		});
		Patch<GameItem>(h, "CanActivateSlotItem", typeFromHandle, null, "AfterCanActivate");
		Patch<ScavHelper>(h, "GetMaxScavAttempts", typeFromHandle, null, "AfterMaxScav");
		Patch<ScavHelper>(h, "GetMinorWoundChance", typeFromHandle, null, "AfterMinorWoundChance");
		Patch<ScavHelper>(h, "GetMajorWoundChance", typeFromHandle, null, "AfterWoundChance");
		Patch<HealthData>(h, "GetTotalAddictionPenalty", typeFromHandle, null, "AfterAddictionPenalty");
		Patch<HealthData>(h, "GetLuck", typeFromHandle, null, "AfterGetLuck");
		Patch<PlayerStore>(h, "OnCustomerReported", typeFromHandle, "BeforeReported", "AfterReported");
		Patch<HealthData>(h, "HasNicotineDebuff", typeFromHandle, null, "AfterNicotineDebuff");
		Patch(h, AccessTools.Method(typeof(ShowcaseHelper), "HandleDisplayCase", Type.EmptyTypes), "ShowcaseHelper.HandleDisplayCase", typeFromHandle, null, "AfterDisplayCase");
		Patch<StoreClient>(h, "GetTradeRepMultiplier", typeFromHandle, null, "AfterTradeRepMultiplier", new Type[1] { typeof(bool) });
		Patch<PlayerStore>(h, "HandleAfterhourDebuff", typeFromHandle, "BeforeAfterhourDebuff");
		Patch<StorePhoneClient>(h, "ShownInPhoneBook", typeFromHandle, null, "AfterShownInPhoneBook");
		Patch<AmenitiesItemDirectory>(h, "OnPortableWaterPurifierUsed", typeFromHandle, "BeforePurifierUsed", null, new Type[2]
		{
			typeof(GameItem),
			typeof(GameItem)
		});
		Patch<PhoneUIManager>(h, "OpenUI", typeFromHandle, null, "AfterPhoneOpen");
		Type typeFromHandle3 = typeof(InputShield);
		Patch<InputActionManager>(h, "Update", typeFromHandle3, "BeforeGameInput");
		Patch<CustomEventHandler>(h, "Update", typeFromHandle3, "BeforeDoubleClick");
		Patch<StoreUIManager>(h, "CheckRaycast", typeFromHandle3, "BeforeStoreRaycast");
		Patch<StoreUIManager>(h, "Update", typeFromHandle3, null, "AfterStoreUpdate");
		Patch<StoreUIManager>(h, "ResolveEscape", typeFromHandle3, "BeforeEscape");
		Patch<GraphicRaycaster>(h, "Raycast", typeFromHandle3, "BeforeGraphicRaycast", null, new Type[2]
		{
			typeof(PointerEventData),
			typeof(List<RaycastResult>)
		});
	}

	public static void InstallResources(Harmony h)
	{
		if (!GameResources.Ready) throw new InvalidOperationException("液体补丁资源尚未就绪");
		InstallGameplay(h);
#if WATER_TRACE
		var probe = typeof(WaterTrace);
		Patch<DirectoryMaster>(h, "Item", probe, postfix: nameof(WaterTrace.Created), args: new[] { typeof(string), typeof(bool) });
		Patch<WaterHelper>(h, "AddWater", probe, nameof(WaterTrace.BeforeFill), nameof(WaterTrace.AfterFill),
			new[] { typeof(GameItem), typeof(int), typeof(int), typeof(bool), typeof(int), typeof(int), typeof(bool) });
		Patch<WaterHelper>(h, "UpdateContainerGoodType", probe, nameof(WaterTrace.BeforeType), nameof(WaterTrace.AfterType), new[] { typeof(GameItem) });
		Patch<WaterHelper>(h, "RemoveMl", probe, postfix: nameof(WaterTrace.Drained), args: new[] { typeof(GameItem), typeof(int) });
		Patch<WaterFeatureHelper>(h, "OnLiquidChanged", probe, postfix: nameof(WaterTrace.LiquidChanged), args: new[] { typeof(GameItem), typeof(bool) });
		Patch<SaveManager>(h, "DecodeNodes", probe, postfix: nameof(WaterTrace.Decoded), args: new[] { typeof(List<SaveItemNode>) });
		Patch<GameItem>(h, "AddParent", probe, postfix: nameof(WaterTrace.Moved), args: new[] { typeof(GraphNodeStorage) });
		Patch<GameItem>(h, "RemoveParent", probe, postfix: nameof(WaterTrace.Moved), args: new[] { typeof(GraphNodeStorage) });
		Patch<WaterFeatureHelper>(h, "GetCurrentWaterDisplay", probe, postfix: nameof(WaterTrace.WaterDisplay), args: new[] { typeof(GameItem) });
		Patch<StoreClient>(h, "IsClientBuyingThisItem", probe, nameof(WaterTrace.BeforeCheck), nameof(WaterTrace.Checked), new[] { typeof(GameItem) });
		Patch<StoreClient>(h, "IsClientBuyingThisItem", probe, nameof(WaterTrace.BeforeCheck), nameof(WaterTrace.CheckedInner), new[] { typeof(GameItem), typeof(bool) });
		Patch<StoreClient>(h, "OnItemBought", probe, postfix: nameof(WaterTrace.Sold), args: new[] { typeof(GameItem), typeof(int) });
		WaterTrace.Ready();
#endif
	}

	private static void Patch<T>(Harmony h, string method, Type owner, string prefix = null, string postfix = null, Type[] args = null)
	{
		Type[] parameters = args ?? Type.EmptyTypes;
		MethodInfo methodInfo = AccessTools.Method(typeof(T), method, parameters);
		if (methodInfo == null)
		{
			ModLog.Warn("找不到 " + typeof(T).Name + "." + method + "，相关功能停用");
		}
		else
		{
			h.Patch(methodInfo, (prefix == null) ? null : new HarmonyMethod(owner, prefix), (postfix == null) ? null : new HarmonyMethod(owner, postfix));
		}
	}

	private static void Patch(Harmony h, MethodInfo original, string label, Type owner, string prefix = null, string postfix = null)
	{
		if (original == null)
		{
			ModLog.Warn("找不到 " + label + "，相关功能停用");
		}
		else
		{
			h.Patch(original, (prefix == null) ? null : new HarmonyMethod(owner, prefix), (postfix == null) ? null : new HarmonyMethod(owner, postfix));
		}
	}

	public static void InstallEvents()
	{
		if (_nightly == null)
		{
			_nightly = () =>
			{
				Guard("每晚服务", Services.OnNightlyServices);
			};
			ModHook.OnHandlingNightlyServicesEarly += _nightly;
		}
	}

	public static void UninstallEvents()
	{
		if (_nightly != null)
		{
			ModHook.OnHandlingNightlyServicesEarly -= _nightly;
			_nightly = null;
		}
	}

	private static void Guard(string what, Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			try { ModLog.Warn(what + " 出错：" + ex.Message); } catch { }
		}
	}

	private static void AfterBeginDay()
	{
		Guard("开店检查", () =>
		{
			Engine.OnDayStart();
			SpacerCo.OnDayStart();
			Phone.RegisterCurrent();
			Goods.OnDayStart();
			Brand.ScanOwned();
			SilverBlood.OnDayStart();
			CarmichaelCo.OnDayStart();
			Ext.Fire("DayStart", Ext.DayStart);
		});
	}

	private static void AfterCounterWaterSale(GameItem item, int cost, StoreClient client)
	{
		Guard("柜台售水记录", () => Services.OnItemWaterSold(item, cost, client));
	}

	private static void AfterVendingItemSale(GameItem item, int soldValue)
	{
		// Vanilla calls this for every selected item before destroying it and recording the payout.
		_pendingVendingWater = 0;
		Guard("识别售货机售水", () =>
		{
			if (soldValue > 0 && Services.IsWaterSaleItem(item))
				_pendingVendingWater = soldValue;
		});
	}

	private static void AfterVendingSale(int value)
	{
		int pending = _pendingVendingWater;
		_pendingVendingWater = 0;
		if (pending > 0 && pending == value)
			Guard("售货机售水记录", () => Services.OnWaterSold(value));
	}

	private static void AfterFountainWaterSale(int value)
	{
		Guard("售水机售水记录", () => Services.OnWaterSold(value));
	}

	private static void BeforeEndDay()
	{
		Guard("打烊记录", Services.OnEndDay);
		Guard("银血线", CarmichaelCo.OnEndDay);
		Guard("预约来访", Engine.OnDayEnd);
	}

	private static void AfterClientBought(StoreClient __instance, GameItem item, int cost)
	{
		Guard("卖出记录", () =>
		{
			Services.OnSoldToClient(__instance, item, cost);
		});
	}

	private static void AfterClientSold(StoreClient __instance, GameItem item, int soldAmount)
	{
		Guard("买入记录", () =>
		{
			Services.OnBoughtFromClient(__instance, item, soldAmount);
		});
	}

	private static void AfterInitPhones(Dictionary<long, StorePhoneClient> __result)
	{
		Guard("电话登记", () =>
		{
			Phone.Register(__result);
		});
	}

	private static void AfterShownInPhoneBook(StorePhoneClient __instance, ref bool __result)
	{
		try
		{
			Notebook.AfterShownInPhoneBook(__instance, ref __result);
		}
		catch
		{
		}
	}

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

	private static void BeforeReported(PlayerStore __instance)
	{
		try
		{
			_reportedId = __instance.currentClientInstance?.GetClientBlueprint()?.identifier;
			_revBefore = Engine.Reputation("FACTION_REVOLUTION");
		}
		catch
		{
			_reportedId = null;
		}
	}

	private static void AfterReported()
	{
		Action<string, int> f = Ext.CustomerReported;
		if (f != null && _reportedId != null)
		{
			int delta = Engine.Reputation("FACTION_REVOLUTION") - _revBefore;
			string id = _reportedId;
			_reportedId = null;
			Ext.Fire("CustomerReported", () =>
			{
				f(id, delta);
			});
		}
	}

	private static void AfterGetLuck(ref int __result)
	{
		Func<int> luckBonus = Ext.LuckBonus;
		if (luckBonus != null)
		{
			__result += Ext.Ask("LuckBonus", luckBonus, 0);
		}
	}

	private static void AfterMaxScav(ref int __result)
	{
		try
		{
			__result += Goods.ExtraScavAttempts;
		}
		catch
		{
		}
	}

	private static float MinorFactor()
	{
		return (Goods.PainBlockedToday ? 0.5f : 1f) * (Goods.GlovesCarried() ? 0.7f : 1f);
	}

	private static float MajorFactor()
	{
		if (!Goods.PainBlockedToday)
		{
			return 1f;
		}
		return 0.5f;
	}

	private static void AfterMinorWoundChance(ref float __result)
	{
		try
		{
			__result *= MinorFactor();
		}
		catch
		{
		}
	}

	private static void AfterWoundChance(ref float __result)
	{
		try
		{
			__result *= MajorFactor();
		}
		catch
		{
		}
	}

	private static void AfterAddictionPenalty(HealthData __instance, ref int __result)
	{
		try
		{
			if (Goods.PatchActive && HealthData.IsNicotineAddicted() && __instance.daysSinceNicotineConsumption >= 3)
			{
				__result = Math.Max(0, __result - 8);
			}
		}
		catch
		{
		}
	}

	private static void AfterNicotineDebuff(ref bool __result)
	{
		try
		{
			if (Goods.PatchActive)
			{
				__result = false;
			}
		}
		catch
		{
		}
	}

	private static void AfterDisplayCase(ref List<StoreClient> __result)
	{
		try
		{
			if (!Goods.SignInShowcase())
			{
				return;
			}
			StoreClient storeClient = CompanyVisitors.PastelSeller();
			if (storeClient != null)
			{
				if (__result == null)
				{
					__result = new List<StoreClient>();
				}
				__result.Add(storeClient);
				ModLog.Info("特约店灯牌：今天会有一位帕斯泰尔货商上门");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("灯牌卖家失败：" + ex.Message);
		}
	}

	private static void AfterTradeRepMultiplier(ref double __result)
	{
		try
		{
			if (Goods.ElixirActive)
			{
				__result *= 1.5;
			}
		}
		catch
		{
		}
	}

	private static void BeforeAfterhourDebuff(PlayerStore __instance, bool __runOriginal)
	{
		if (!__runOriginal) return;
		try
		{
			Goods.TryUseMask(__instance);
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

	private static void AfterCloseAllPaper()
	{
		Guard("关名片", CardView.CloseAll);
	}

	private static void AfterPhoneOpen()
	{
		Guard("记事簿", () =>
		{
			Phone.RegisterCurrent();
			Notebook.Refresh();
		});
	}

	private static void AfterGetCallDialog(StorePhoneClient __instance, ref Dialogue __result)
	{
		try
		{
			Company company = Companies.ByLocId(__instance?.locID);
			if (company != null)
			{
				__result = Phone.CallDialog(company);
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("公司电话出错：" + ex.Message);
		}
	}

	private static void AfterPhoneName(StorePhoneClient __instance, ref string __result)
	{
		try
		{
			if (Companies.ByLocId(__instance?.locID) != null)
			{
				__result = __instance.displayName;
			}
		}
		catch
		{
		}
	}

	private static void BeforeHandleCall(long currentNumber)
	{
		Guard("公司电话拒接提示", () =>
		{
			Phone.WarnIfShunned(currentNumber);
		});
	}

	private static void AfterWillAnswer(long number, ref bool __result)
	{
		try
		{
			Company company = Companies.ByNumber(number);
			if (company != null)
			{
				__result = Companies.StateOf(company)?.CardGiven ?? false;
			}
		}
		catch
		{
		}
	}

	private static void BeforeSave()
	{
		// Skip only our recursive preparation; the void prefix never cancels vanilla.
		if (!SavePreparation.TryEnter()) return;
		try
		{
			Guard("写入存档", CorpState.Save);
			Ext.Fire("BeforeSave", Ext.BeforeSave);
			Guard("卸载前清理", Cleanup.BeforeSave);
		}
		catch (Exception error)
		{
			try { ModLog.Warn("公司保存准备失败，继续原版保存：" + error.Message); } catch { }
		}
		finally { SavePreparation.Exit(); }
	}

	private static void AfterLoadGame(bool __runOriginal)
	{
		ResetState();
		if (__runOriginal)
		{
			LoadedItemRepair.Request(() =>
			{
				Guard("读档后配送箱清理绑定", Crates.RebindLoaded);
				Guard("清理旧药品的纯度特征", ItemTags.RemoveRestoredMedicinePurity);
				Guard("修复旧物品占格", Goods.RestoreLegacyFootprints);
				Guard("读档后定制刀分类", Forge.AfterStoreLoaded);
			});
			Guard("读档后公司物品修复", LoadedItemRepair.Tick);
		}
	}

	private static void BeforeStoreTransition()
	{
		_pendingVendingWater = 0;
		LoadedItemRepair.Reset();
		Guard("换档配送清理", Crates.Reset);
		Guard("换档估价清理", CatalogPricing.Reset);
	}

	private static void ResetState()
	{
		BeforeStoreTransition();
		CorpState.Current = null;
		Services.ResetNightly();
		Engine.BuyActive = false;
		try { CompanyWindow.Close(); }
		catch (Exception error)
		{
			try { ModLog.Warn("换档关闭面板失败，继续重置公司状态：" + error.Message); } catch { }
		}
		try { Ext.Fire("StateReset", Ext.StateReset); }
		catch (Exception error)
		{
			try { ModLog.Warn("重置扩展状态失败：" + error.Message); } catch { }
		}
	}

	private static void AfterGenerateClient(StoreClientManager __instance)
	{
		Guard("安排来访", () =>
		{
			Engine.Schedule(__instance);
			Goods.ApplyCandle(__instance);
		});
	}

	private static void BeforeNextClient(PlayerStore __instance)
	{
		Guard("安排来访", () =>
		{
			Engine.BuyActive = false;
			Engine.Schedule(__instance?.storeClientManager);
		});
	}

	private static bool IsCarmichael(GameItem item)
	{
		return Companies.Find("carmicheal")?.Owns(item) ?? ManufacturerIs(item, "carmicheal");
	}

	private static bool ManufacturerIs(GameItem item, string id)
	{
		try
		{
			TagState tagState = item?.GetTagReadonly("manufacturer");
			return tagState != null && tagState.GetString() == id;
		}
		catch
		{
			return false;
		}
	}

	private static bool VanillaBuyerAtCounter()
	{
		StoreClient storeClient = PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
		if (storeClient != null && storeClient.clientIntent == StoreClient.ClientIntent.BUY)
		{
			if (storeClient.identifier != null)
			{
				return !storeClient.identifier.StartsWith("corp_");
			}
			return true;
		}
		return false;
	}

	private static void AfterNegociatedValue(GameItem __instance, ref long __result, bool __runOriginal)
	{
		if (!__runOriginal) return;
		try
		{
			long? num = Engine.SellFixedPrice(__instance) ?? Engine.BuyFixedPrice(__instance);
			if (num.HasValue && num.Value >= 0)
			{
				__result = num.Value;
				return;
			}
			double num2 = Engine.BuyMultiplier(__instance);
			if (num2 == 1.0 && !Engine.BuyActive && Isa.Licensed && IsCarmichael(__instance) && VanillaBuyerAtCounter())
			{
				num2 = 1.1;
			}
			Func<GameItem, double> bf = Ext.BuyFactor;
			if (bf != null)
			{
				num2 *= Ext.Ask("BuyFactor", () => bf(__instance), 1.0);
			}
			if (num2 != 1.0)
			{
				double quote = (double)__result * num2;
				if (double.IsNaN(quote) || double.IsInfinity(quote) || quote < 0 || quote >= 9223372036854775808d) return;
				__result = (long)Math.Round(quote);
			}
		}
		catch
		{
		}
	}
}



