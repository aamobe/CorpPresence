using System;
namespace CorpPresence.Malice;
internal static partial class Malice
{
	private static bool _installed;
	private static CorpPresence.Runtime.OwnedCallbacks _callbacks;
	public static void Install()
	{
		if (_installed)
		{
			return;
		}
		var callbacks = new CorpPresence.Runtime.OwnedCallbacks(error => ModLog.Warn("释放扩展回调失败：" + error.Message));
		try
		{
			callbacks.Own(Ext.BeforeSave.Subscribe("malice", MaliceState.Save));
			callbacks.Own(Ext.StateReset.Subscribe("malice", MaliceState.Reset));
			callbacks.Own(Ext.DayStart.Subscribe("malice", OnDayStart));
			callbacks.Replace(() => Ext.ExtraModule, value => Ext.ExtraModule = value, (Company c) => (!Applies(c)) ? null : L.K("malice.malice.install.corporateTerms", "巨企合约", "Corporate Terms"));
			callbacks.Replace(() => Ext.DrawExtraModule, value => Ext.DrawExtraModule = value, MalicePanel.Draw);
			callbacks.Replace(() => Ext.OrderBlocked, value => Ext.OrderBlocked = value, OrderBlocked);
			callbacks.Replace(() => Ext.CatalogTier, value => Ext.CatalogTier = value, CatalogTier);
			callbacks.Replace(() => Ext.Limit, value => Ext.Limit = value, Limit);
			callbacks.Replace(() => Ext.ServicePrice, value => Ext.ServicePrice = value, ServicePrice);
			callbacks.Replace(() => Ext.Charge, value => Ext.Charge = value, Charge);
			callbacks.Replace(() => Ext.SpawnVisitor, value => Ext.SpawnVisitor = value, Rebels.SpawnVisitor);
			callbacks.Replace(() => Ext.SoldToClient, value => Ext.SoldToClient = value, (StoreClient cl, GameItem it, int amt) =>
			{
				Rebels.SoldToClient(cl, it, amt);
				if (On)
				{
					Envoy.SoldToClient(cl, it, amt);
					RebelStory.SoldToClient(cl, it, amt);
				}
			});
			callbacks.Replace(() => Ext.ScheduleExtra, value => Ext.ScheduleExtra = value, (StoreClientManager m) =>
			{
				Envoy.Schedule(m);
				RebelStory.Schedule(m);
			});
			callbacks.Replace(() => Ext.CustomerReported, value => Ext.CustomerReported = value, RebelStory.OnReported);
			callbacks.Replace(() => Ext.CallDialog, value => Ext.CallDialog = value, Envoy.MentorCall);
			callbacks.Replace(() => Ext.ClientBuys, value => Ext.ClientBuys = value, (StoreClient cl, GameItem it) => Envoy.ClientBuys(cl, it) || RebelStory.ClientBuys(cl, it));
			callbacks.Replace(() => Ext.LuckBonus, value => Ext.LuckBonus = value, Envoy.LuckBonus);
			callbacks.Replace(() => Ext.OrderFactor, value => Ext.OrderFactor = value, (Company c) => (!Applies(c)) ? 1.0 : Envoy.OrderFactor());
			callbacks.Replace(() => Ext.BuyFactor, value => Ext.BuyFactor = value, (GameItem it) => (!On) ? 1.0 : (Rebels.BuyFactor(it) * RebelStory.BuyFactor(it)));
			callbacks.Replace(() => Ext.LockVanillaDifficulty, value => Ext.LockVanillaDifficulty = value, () => On);
			callbacks.Replace(() => Ext.ForceOrderLimits, value => Ext.ForceOrderLimits = value, () => On);
			callbacks.Replace(() => Ext.ForceRestockWeeks, value => Ext.ForceRestockWeeks = value, () => On ? 1 : 0);
			_callbacks = callbacks;
			_installed = true;
		}
		catch
		{
			callbacks.Release();
			throw;
		}
	}

	public static void Uninstall()
	{
		if (!_installed) return;
		_installed = false;
		var callbacks = _callbacks;
		_callbacks = null;
		callbacks?.Release();
	}

}
