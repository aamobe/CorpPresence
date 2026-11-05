using System;
using UnityEngine;

namespace CorpPresence;

internal static class L
{
	private static int _frame = -1;

	private static bool _en;

	private static bool _ready;

	public static bool En
	{
		get
		{
			if (_frame == Time.frameCount)
			{
				return _en;
			}
			_frame = Time.frameCount;
			if (!_ready && FontLoader.Instance == null)
			{
				return _en = false;
			}
			_ready = true;
			try
			{
				string currentLocaleCode = LocHelper.GetCurrentLocaleCode();
				_en = !string.IsNullOrEmpty(currentLocaleCode) && !currentLocaleCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				_en = false;
			}
			return _en;
		}
	}

	public static string T(string zh, string en)
	{
		if (!En)
		{
			return zh;
		}
		return en;
	}

	public static string[] T(string[] zh, string[] en)
	{
		if (!En)
		{
			return zh;
		}
		return en;
	}
}
