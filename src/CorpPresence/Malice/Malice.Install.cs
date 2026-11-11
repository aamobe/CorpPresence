using System;
namespace CorpPresence.Malice;
internal static partial class Malice
{
	private static bool _installed;
	internal static bool IsInstalled => _installed;
	internal static bool LastReleaseFailed => _lastReleaseFailed;
	private static bool _lastReleaseFailed;
	private static CorpPresence.Runtime.OwnedCallbacks _callbacks;
	public static void Install()
	{
		if (_lastReleaseFailed) throw new InvalidOperationException("恶意模式清理失败，不能重新安装；请重启游戏");
		if (_installed)
		{
			return;
		}
		_lastReleaseFailed = false;
		// The captured stage belongs to this lease collection, not a global state machine.
		string releaseStage = "rollback";
		var callbacks = new CorpPresence.Runtime.OwnedCallbacks(error =>
		{
			_lastReleaseFailed = true;
			ReportLifecycleFailure(releaseStage, error);
		});
		try
		{
			callbacks.Own(Ext.BeforeSave.Subscribe("malice", MaliceState.Save));
			callbacks.Own(Ext.StateReset.Subscribe("malice", () => { MaliceState.Reset(); Envoy.ResetNotifications(); }));
			callbacks.Own(Ext.DayStart.Subscribe("malice", OnDayStart));
			callbacks.Own(CorpPresence.Runtime.RuntimeUpdates.Register("malice.envoy", Envoy.Tick));
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
			_callbacks = callbacks;
			releaseStage = "uninstall";
			_installed = true;
		}
		catch (Exception error)
		{
			ReportLifecycleFailure("install", error);
			callbacks.Release();
			Envoy.ResetNotifications();
			if (callbacks.ReleaseFailed) _callbacks = callbacks;
			throw;
		}
	}

	public static void Uninstall()
	{
		if (!_installed)
		{
			if (_lastReleaseFailed) throw new InvalidOperationException("恶意模式清理失败，不能重放可能部分完成的释放");
			return;
		}
		_installed = false;
		Envoy.ResetNotifications();
		_lastReleaseFailed = false;
		var callbacks = _callbacks;
		callbacks?.Release();
		if (callbacks?.ReleaseFailed == true)
			throw new InvalidOperationException("恶意模式清理失败，已继续其他释放；请重启游戏");
		_callbacks = null;
	}

	private static void ReportLifecycleFailure(string stage, Exception error)
	{
		// Startup rollback clears diagnostics; a safe warning must stand on its own.
		try { ModLog.Warn("模块 malice/" + stage + " 失败：" + error.GetType().FullName); } catch { }
		try { MaintenanceDiagnostics.Record("malice", stage, "lifecycle-failed", error); } catch { }
	}

}
