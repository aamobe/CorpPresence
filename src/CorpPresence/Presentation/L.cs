using System;
using UnityEngine;

namespace CorpPresence;

internal static class L
{
	private static int _frame = -1;

	private static bool _en;

	private static bool _ready;

	private static string _locale = "";

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
				_locale = "";
				return _en = false;
			}
			_ready = true;
			try
			{
				_locale = LocHelper.GetCurrentLocaleCode() ?? "";
				_en = _locale.Length != 0 && !_locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				_locale = "";
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
		return TextTranslations.Select(en, _locale);
	}

	internal static string Locale
	{
		get { _ = En; return _locale; }
	}

	internal static string Translate(string en) => TextTranslations.Select(en, Locale);

	internal static string Translate(FormattableString en)
	{
		string format = Translate(en.Format);
		if (string.Equals(format, en.Format, StringComparison.Ordinal))
			return en.ToString();
		try { return string.Format(format, en.GetArguments()); }
		catch (FormatException) { return en.ToString(); }
	}

	public static string[] T(string[] zh, string[] en)
	{
		if (!En)
		{
			return zh;
		}
		return TextTranslations.HasLocale(_locale) && en != null
			? Array.ConvertAll(en, value => TextTranslations.Select(value, _locale))
			: en;
	}
}
