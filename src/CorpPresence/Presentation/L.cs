using System;
using UnityEngine;

namespace CorpPresence;

internal static class L
{
	private static int _frame = -1;

	private static bool _en;

	private static bool _ready;

	private static string _locale = "";
	private static int _translationVersion = -1;

	public static bool En
	{
		get
		{
			int version = ExternalTranslations.Version;
			if (_frame == Time.frameCount && _translationVersion == version)
			{
				return _en;
			}
			_frame = Time.frameCount;
			_translationVersion = version;
			if (!_ready && FontLoader.Instance == null)
			{
				_locale = "";
				return _en = false;
			}
			_ready = true;
			try
			{
				_locale = ExternalTranslations.ResolveLocale(LocHelper.GetCurrentLocaleCode() ?? "");
				_en = _locale.Length != 0 && !TextTranslations.IsBuiltInLocale(_locale, "zh");
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

	public static string K(string key, string zh, string en)
	{
		if (En) return TextTranslations.SelectKey(key, en, _locale);
		return ExternalTranslations.TryGet(key, en, _locale, out var external) ? external : zh;
	}

	// These overloads preserve existing L.En branches and eagerly formatted Chinese text.
	internal static string K(string key, string en) => TextTranslations.SelectKey(key, en, Locale);

	internal static string K(string key, FormattableString en)
	{
		string locale = Locale;
		return ExternalTranslations.TryFormat(key, en, locale, out var external) ? external : FormatBuiltIn(key, en, locale);
	}

	private static string FormatBuiltIn(string key, FormattableString en, string locale)
	{
		string format = TextTranslations.SelectBuiltInKey(key, en.Format, locale);
		if (string.Equals(format, en.Format, StringComparison.Ordinal)) return en.ToString();
		try { return string.Format(format, en.GetArguments()); }
		catch (FormatException) { return en.ToString(); }
	}

	public static string K(string key, string zh, FormattableString en)
	{
		bool english = En;
		if (ExternalTranslations.TryFormat(key, en, _locale, out var external)) return external;
		return english ? FormatBuiltIn(key, en, _locale) : zh;
	}

	// Cast interpolated arguments to FormattableString so translation precedes formatting.
	public static string K(string key, FormattableString zh, FormattableString en)
	{
		bool english = En;
		if (ExternalTranslations.TryFormat(key, en, _locale, out var external)) return external;
		return english ? FormatBuiltIn(key, en, _locale) : zh.ToString();
	}

	// Keep caller-supplied templates inside the safe formatting path.
	internal static string F(string key, string zh, string en, params object[] arguments)
	{
		if (En) return TextTranslations.FormatKey(key, en, _locale, arguments);
		var source = System.Runtime.CompilerServices.FormattableStringFactory.Create(en, arguments);
		return ExternalTranslations.TryFormat(key, source, _locale, out var external) ? external : string.Format(zh, arguments);
	}

	// Preserve previously rendered English lookup while allowing an external template.
	internal static string KRendered(string key, string zh, FormattableString en)
	{
		bool english = En;
		if (ExternalTranslations.TryFormat(key, en, _locale, out var external)) return external;
		return english ? TextTranslations.Select(en.ToString(), _locale) : zh;
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

	internal static string[] K(string[] keys, string[] zh, string[] en)
	{
		bool english = En;
		if (!english && (!ExternalTranslations.HasLocale(_locale) || zh == null || en == null)) return zh;
		if (!TextTranslations.HasLocale(_locale) || en == null) return en;
		var result = new string[en.Length];
		for (int i = 0; i < en.Length; i++)
		{
			string key = keys != null && i < keys.Length ? keys[i] : null;
			result[i] = english ? TextTranslations.SelectKey(key, en[i], _locale)
				: ExternalTranslations.TryGet(key, en[i], _locale, out var external) ? external : zh[i];
		}
		return result;
	}
}
