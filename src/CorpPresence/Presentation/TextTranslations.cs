using System;
using System.Collections.Generic;
using System.IO;

namespace CorpPresence;

internal static class TextTranslations
{
	private static readonly Lazy<Dictionary<string, string[]>> Catalog = new(() => LoadCatalog("CorpPresence.Translations.json"));
	private static readonly Lazy<Dictionary<string, string[]>> KeyedCatalog = new(() => LoadCatalog("CorpPresence.Translations.Keys.json"));

	internal static bool HasLocale(string locale) => LanguageIndex(locale) >= 0 || OfficialTranslations.HasLocale(locale);

	private static int LanguageIndex(string locale)
	{
		if (string.IsNullOrEmpty(locale)) return -1;
		if (IsBuiltInLocale(locale, "ru")) return 0;
		if (IsBuiltInLocale(locale, "fr")) return 1;
		return -1;
	}
	internal static bool IsBuiltInLocale(string locale, string language) => locale != null &&
		(string.Equals(locale, language, StringComparison.OrdinalIgnoreCase) || locale.StartsWith(language + "-", StringComparison.OrdinalIgnoreCase) || locale.StartsWith(language + "_", StringComparison.OrdinalIgnoreCase));

	internal static string FormatKey(string key, string english, string locale, params object[] arguments)
	{
		var source = System.Runtime.CompilerServices.FormattableStringFactory.Create(english, arguments);
		if (OfficialTranslations.TryFormat(key, source, locale, out var translated)) return translated;
		string format = SelectBuiltInKey(key, english, locale);
		try { return string.Format(format, arguments); }
		catch (FormatException) { return source.ToString(); }
	}

	internal static string Select(string english, string locale)
	{
		if (english == null) return null;
		int language = LanguageIndex(locale);
		if (language < 0) return english;
		if (Catalog.Value.TryGetValue(english, out var localized) && localized?.Length > language)
			return string.IsNullOrEmpty(localized[language]) ? english : localized[language];
		return english;
	}

	// Stable keys are presentation identifiers only. Missing entries keep the legacy fallback.
	internal static string SelectKey(string key, string english, string locale)
	{
		return OfficialTranslations.TryGet(key, english, locale, out var translated) ? translated : SelectBuiltInKey(key, english, locale);
	}

	internal static string SelectBuiltInKey(string key, string english, string locale)
	{
		if (english == null) return null;
		int language = LanguageIndex(locale);
		if (language < 0) return english;
		if (key != null && KeyedCatalog.Value.TryGetValue(key, out var localized) &&
			localized?.Length > language && !string.IsNullOrEmpty(localized[language]))
			return localized[language];
		return Select(english, locale);
	}

	private static Dictionary<string, string[]> LoadCatalog(string resourceName)
	{
		try
		{
			using Stream stream = typeof(TextTranslations).Assembly.GetManifestResourceStream(resourceName);
			if (stream != null)
			{
				using var reader = new StreamReader(stream);
				var catalog = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string[]>>(reader.ReadToEnd());
				if (catalog != null) return catalog;
			}
		}
		catch (Exception error)
		{
			ModLog.Warn("翻译资源读取失败：" + error.Message);
		}
		return new Dictionary<string, string[]>(StringComparer.Ordinal);
	}
}
