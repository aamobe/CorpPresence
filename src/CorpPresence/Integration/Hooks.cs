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

internal static partial class Hooks
{
	public static void InstallCore(Harmony h)
	{
		Type typeFromHandle = typeof(Hooks);
		Type typeFromHandle2 = typeof(ItemRegistry);
		RatWheelGenerator.Install(h);
		Patch<SaveManager>(h, "DecodeNodes", typeFromHandle2, "BeforeDecode", null, new Type[1] { typeof(List<SaveItemNode>) });
		Patch<SaveManager>(h, "DecodeNodes", typeof(ItemTags), null, "AfterDecode", new Type[1] { typeof(List<SaveItemNode>) });
		Patch(h, AccessTools.Method(typeof(StorageExpansionHelper), nameof(StorageExpansionHelper.RestoreTabs), new[] { typeof(GameItem) }),
			"StorageExpansionHelper.RestoreTabs", typeof(ProductionWorkBay), prefix: nameof(ProductionWorkBay.BeforeRestoreNativeTabs));
		Patch<SaveManager>(h, "EncodeNode", typeFromHandle2, "BeforeEncode", null, new Type[1] { typeof(GameItem) });
		Patch<PlayerStore>(h, "SaveGame", typeFromHandle, nameof(BeforeSave));
		Patch<PlayerStore>(h, "LoadGame", typeFromHandle, nameof(BeforeStoreTransition), nameof(AfterLoadGame));
		Patch<PlayerStore>(h, "StartNewGame", typeFromHandle, nameof(BeforeStoreTransition), nameof(ResetState));
		Patch(h, AccessTools.Method(typeof(ModConfigWindow), nameof(ModConfigWindow.Open), new[] { typeof(LoadedMod) }),
			"ModConfigWindow.Open", typeof(ConfigLocalization), postfix: nameof(ConfigLocalization.AfterOpen));
#if TESTKIT
		SaveDiag.Install(h);   // 测试版：存档出错时把出错物品写进日志
#endif
	}

	private static void InstallGameplay(Harmony h)
	{
		Type typeFromHandle = typeof(Hooks);
		Patch<PlayerStore>(h, "BeginDay", typeFromHandle, null, nameof(AfterBeginDay));
		Patch<PlayerStore>(h, "GetNextClient", typeFromHandle, nameof(BeforeNextClient));
		Patch<HydroponicHelper>(h, nameof(HydroponicHelper.ContinueGrowing), typeof(CropHealth),
			postfix: nameof(CropHealth.AfterContinueGrowing), args: new[] { typeof(GameItem), typeof(int), typeof(GameItem) });
		Patch<GameItem>(h, "GetNegociatedValue", typeFromHandle, null, nameof(AfterNegociatedValue));
		Patch<StoreClient>(h, nameof(StoreClient.ClientExposeFeature), typeof(Malice.Rebels), nameof(Malice.Rebels.BeforeClientExposeFeature), null, new[] { typeof(GameItem) });
		Patch<StoreClient>(h, "OnItemSold", typeFromHandle, null, nameof(AfterClientSold), new Type[2]
		{
			typeof(GameItem),
			typeof(int)
		});
		Patch<StoreClient>(h, "OnItemBought", typeFromHandle, null, nameof(AfterClientBought), new Type[2]
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
		Patch<PlayerStore>(h, "EndDay", typeFromHandle, nameof(BeforeEndDay));
		Patch<StorePhoneClient>(h, "InitPhoneClientDict", typeFromHandle, null, nameof(AfterInitPhones));
		Patch<StorePhoneClient>(h, "GetCallDialog", typeFromHandle, null, nameof(AfterGetCallDialog));
		Patch<PhoneUIManager>(h, "WillAnswerCall", typeFromHandle, null, nameof(AfterWillAnswer), new Type[1] { typeof(long) });
		Patch<PhoneUIManager>(h, "HandleCall", typeFromHandle, nameof(BeforeHandleCall), null, new Type[1] { typeof(long) });
		Patch<StorePhoneClient>(h, "GetLocalizedDisplayName", typeFromHandle, null, nameof(AfterPhoneName));
		Patch<DialoguePhoneUIManager>(h, nameof(DialoguePhoneUIManager.DisplayClientText), typeof(PhoneDialogueLayout),
			postfix: nameof(PhoneDialogueLayout.AfterClientText), args: new[] { typeof(Dialogue) });
		Patch<DialoguePhoneUIManager>(h, nameof(DialoguePhoneUIManager.DisplayerPlayerText), typeof(PhoneDialogueLayout),
			postfix: nameof(PhoneDialogueLayout.AfterPlayerText), args: new[] { typeof(Dialogue) });
		MethodInfo scalePhoneBubbles = AccessTools.Method(typeof(DialoguePhoneUIManager), "ApplyBubbleScale", Type.EmptyTypes);
		if (scalePhoneBubbles != null)
			Patch(h, scalePhoneBubbles, "DialoguePhoneUIManager.ApplyBubbleScale", typeof(PhoneDialogueLayout),
				prefix: nameof(PhoneDialogueLayout.Reset), postfix: nameof(PhoneDialogueLayout.AfterBubbleScale));
		Patch<GameItemElement>(h, "ResolveSpriteByName", typeFromHandle, nameof(BeforeResolveSprite), null, new Type[1] { typeof(string) });
		Patch<GameItemElement>(h, "ResolveItemSprite", typeFromHandle, nameof(BeforeResolveItemSprite));
		Patch<SpriteDict>(h, "GetSprite", typeFromHandle, nameof(BeforeGetSprite), null, new Type[1] { typeof(string) });
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
		InstallSilverBloodPlacement(h);
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
		}), "RenderHandler.LoadFromAtlasRaw", typeFromHandle, nameof(BeforeLoadFromAtlas));
		Patch<GameItem>(h, "ActivateSlotItem", typeFromHandle, nameof(BeforeActivateItem));
		Patch<PaperUIManager>(h, "CloseAllPaperUI", typeFromHandle, null, nameof(AfterCloseAllPaper));
		Patch<GameItem>(h, "MayActivateSlotItem", typeFromHandle, null, nameof(AfterCanActivate));
		Patch<PowerHelper>(h, "ChargeBattery", typeFromHandle, nameof(BeforeChargeBattery), nameof(AfterBatteryChange), new Type[1] { typeof(GameItem) });
		Patch<PowerHelper>(h, "UpdateEnergyCreditSprite", typeFromHandle, null, nameof(AfterBatteryChange), new Type[1] { typeof(GameItem) });
		Patch<PowerHelper>(h, "SetPowerSourceAt", typeFromHandle, null, nameof(AfterBatteryChange), new Type[2]
		{
			typeof(GameItem),
			typeof(int)
		});
		Patch<PowerHelper>(h, "DrawPowerSource", typeFromHandle, null, nameof(AfterBatteryChange), new Type[2]
		{
			typeof(GameItem),
			typeof(int)
		});
		Patch<PowerHelper>(h, "ChargeBattery", typeFromHandle, nameof(BeforeChargeBattery), nameof(AfterBatteryChange), new Type[3]
		{
			typeof(GameItem),
			typeof(int),
			typeof(bool)
		});
		Patch<GameItem>(h, "CanActivateSlotItem", typeFromHandle, null, nameof(AfterCanActivate));
		Patch<ScavHelper>(h, "GetMaxScavAttempts", typeFromHandle, null, nameof(AfterMaxScav));
		Patch<ScavHelper>(h, "GetMinorWoundChance", typeFromHandle, null, nameof(AfterMinorWoundChance));
		Patch<ScavHelper>(h, "GetMajorWoundChance", typeFromHandle, null, nameof(AfterWoundChance));
		Patch<HealthData>(h, "GetTotalAddictionPenalty", typeFromHandle, null, nameof(AfterAddictionPenalty));
		Patch<HealthData>(h, "GetLuck", typeFromHandle, null, nameof(AfterGetLuck));
		Patch<PlayerStore>(h, "OnCustomerReported", typeFromHandle, nameof(BeforeReported), nameof(AfterReported));
		Patch<HealthData>(h, "HasNicotineDebuff", typeFromHandle, null, nameof(AfterNicotineDebuff));
		Patch(h, AccessTools.Method(typeof(ShowcaseHelper), "HandleDisplayCase", Type.EmptyTypes), "ShowcaseHelper.HandleDisplayCase", typeFromHandle, null, nameof(AfterDisplayCase));
		Patch<StoreClient>(h, "GetTradeRepMultiplier", typeFromHandle, null, nameof(AfterTradeRepMultiplier), new Type[1] { typeof(bool) });
		Patch<PlayerStore>(h, "HandleAfterhourDebuff", typeFromHandle, nameof(BeforeAfterhourDebuff));
		Patch<StorePhoneClient>(h, "ShownInPhoneBook", typeFromHandle, null, nameof(AfterShownInPhoneBook));
		Patch<AmenitiesItemDirectory>(h, "OnPortableWaterPurifierUsed", typeFromHandle, nameof(BeforePurifierUsed), null, new Type[2]
		{
			typeof(GameItem),
			typeof(GameItem)
		});
		Patch<PhoneUIManager>(h, "OpenUI", typeFromHandle, null, nameof(AfterPhoneOpen));
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
		// Preserve the original typed lookup shape for the official permission scanner.
		Type[] parameters = args ?? Type.EmptyTypes;
		MethodInfo methodInfo = AccessTools.Method(typeof(T), method, parameters);
		Patch(h, methodInfo, typeof(T).Name + "." + method, owner, prefix, postfix);
	}

	private static void Patch(Harmony h, MethodInfo original, string label, Type owner, string prefix = null, string postfix = null)
	{
		PatchInstallation.Run(label, () =>
		{
			if (original == null) return false;
			h.Patch(original, (prefix == null) ? null : new HarmonyMethod(owner, prefix), (postfix == null) ? null : new HarmonyMethod(owner, postfix));
			return true;
		});
	}

	private static void InstallSilverBloodPlacement(Harmony h)
	{
		MethodInfo grow = AccessTools.Method(typeof(AquacultureHelper), "TryGrowIntoRoom");
		MethodInfo wander = AccessTools.Method(typeof(AquacultureHelper), "TryWander");
		MethodInfo fits = AccessTools.Method("AquacultureHelper+TankOccupancy:Fits",
			new[] { typeof(int), typeof(int), typeof(List<Vector2Int>) });
		if (grow == null || wander == null || fits == null)
		{
			PatchInstallation.Run("AquacultureHelper.Placement", () => false);
			return;
		}
		Type owner = typeof(SilverBlood);
		var prefix = new HarmonyMethod(owner, nameof(SilverBlood.BeginPlacement));
		var finalizer = new HarmonyMethod(owner, nameof(SilverBlood.EndPlacement));
		PatchInstallation.Run("AquacultureHelper.TryGrowIntoRoom", () => { h.Patch(grow, prefix: prefix, finalizer: finalizer); return true; });
		PatchInstallation.Run("AquacultureHelper.TryWander", () => { h.Patch(wander, prefix: prefix, finalizer: finalizer); return true; });
		PatchInstallation.Run("AquacultureHelper.TankOccupancy.Fits", () => { h.Patch(fits, prefix: new HarmonyMethod(owner, nameof(SilverBlood.BeforeTankFits))); return true; });
	}
}
