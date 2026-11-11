using System;
using System.Collections.Generic;
using UnityEngine;
namespace CorpPresence.Malice;
internal static partial class Envoy
{
	private static readonly List<(float At, string Text)> Timed = new List<(float, string)>();

	private static readonly List<string> Pending = new List<string>();

	internal static void ResetNotifications()
	{
		Timed.Clear();
		Pending.Clear();
	}

	private static void After(float seconds, string text)
	{
		Timed.Add((Time.unscaledTime + seconds, text));
		ModLog.Info("恶意巨企：" + text);
	}

	public static void Tick()
	{
		if (Timed.Count == 0)
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		for (int num = Timed.Count - 1; num >= 0; num--)
		{
			if (!(Timed[num].At > unscaledTime))
			{
				try
				{
					StoreUIManager.Instance?.Notify(Timed[num].Text);
				}
				catch
				{
				}
				Timed.RemoveAt(num);
			}
		}
	}

	private static void Later(string text)
	{
		Pending.Add(text);
		ModLog.Info("恶意巨企：" + text);
	}

	private static void FlushLater()
	{
		if (Pending.Count == 0)
		{
			return;
		}
		string message = string.Join(" ", Pending);
		Pending.Clear();
		try
		{
			StoreUIManager.Instance?.Notify(message);
		}
		catch
		{
		}
	}

}
