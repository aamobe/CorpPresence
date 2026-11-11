using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class Ext
{
	public static readonly ExtensionEvent BeforeSave = new("BeforeSave");

	public static readonly ExtensionEvent DayStart = new("DayStart");

	public static readonly ExtensionEvent StateReset = new("StateReset");

	private static readonly HashSet<string> Reported = new HashSet<string>();

	internal static void ResetWarnings()
	{
		Reported.Clear();
		BeforeSave.ResetWarning();
		DayStart.ResetWarning();
		StateReset.ResetWarning();
	}

	public static void Fire(string name, Action a)
	{
		if (a == null)
		{
			return;
		}
		try
		{
			a();
		}
		catch (Exception ex)
		{
			ReportFailure(name, ex);
		}
	}

	public static T Ask<T>(string name, Func<T> call, T fallback)
	{
		try
		{
			return call();
		}
		catch (Exception ex)
		{
			ReportFailure(name, ex);
			return fallback;
		}
	}

	private static void ReportFailure(string name, Exception error)
	{
		try { MaintenanceDiagnostics.Record("ext", name, "callback_failed", error); } catch { }
		if (!Reported.Add(name)) return;
		try { ModLog.Warn("内部回调 " + name + " 出错：" + error.GetType().FullName); } catch { }
	}
}
