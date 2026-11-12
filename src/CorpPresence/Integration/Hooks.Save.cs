using System;
using System.Collections.Generic;
using System.Reflection;
using CorpPresence.Runtime;
using CorpPresence.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorpPresence;

internal static partial class Hooks
{
	private static void BeforeSave(PlayerStore __instance)
	{
		if (!ReferenceEquals(SaveSlot.CurrentStore, __instance)) return;
		if (!SavePreparation.TryEnter()) return;
		try
		{
			Guard("写入存档", CorpState.Save);
			Ext.BeforeSave.Publish();
			Guard("卸载前清理", Cleanup.BeforeSave);
		}
		catch (Exception error)
		{
			try { ModLog.Warn("公司保存准备失败，继续原版保存：" + error.Message); } catch { }
		}
		finally { SavePreparation.Exit(); }
	}

	private static void BeforeStoreTransition()
	{
		StoreSession.Invalidate();
		CargoSortWindow.Close();
		CargoWarehouse.ResetControllerStock();
		_pendingVendingWater = 0;
		LoadedItemRepair.Reset();
		Guard("换档配送清理", Crates.Reset);
		Guard("换档估价清理", CatalogPricing.Reset);
	}

	internal static void InstallStoreScene() => SceneManager.activeSceneChanged += OnStoreSceneChanged;

	internal static void UninstallStoreScene() => SceneManager.activeSceneChanged -= OnStoreSceneChanged;

	private static void OnStoreSceneChanged(Scene previous, Scene next)
	{
		if (next.name == "EmporiumMenu")
		{
			ClearLoadState();
			Guard("返回主菜单清理公司会话", ResetState);
		}
	}

	private static void AfterStartNewGame(bool __runOriginal)
	{
		if (!__runOriginal) return;
		ClearLoadState();
		ResetState();
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
		try { Ext.StateReset.Publish(); }
		catch (Exception error)
		{
			try { ModLog.Warn("重置扩展状态失败：" + error.Message); } catch { }
		}
	}
}
