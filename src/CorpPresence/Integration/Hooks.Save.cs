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
	private static void BeforeSave()
	{
		// Skip only our recursive preparation; the void prefix never cancels vanilla.
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
		CargoSortWindow.Close();
		CargoWarehouse.ResetControllerStock();
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
		try { Ext.StateReset.Publish(); }
		catch (Exception error)
		{
			try { ModLog.Warn("重置扩展状态失败：" + error.Message); } catch { }
		}
	}
}
