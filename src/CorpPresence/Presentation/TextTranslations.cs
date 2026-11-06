using System;
using System.Collections.Generic;
using System.IO;

namespace CorpPresence;

internal static class TextTranslations
{
	private static readonly Lazy<Dictionary<string, string[]>> Catalog = new(LoadCatalog);

	internal static bool HasLocale(string locale) => LanguageIndex(locale) >= 0;

	private static int LanguageIndex(string locale)
	{
		if (locale.StartsWith("ru", StringComparison.OrdinalIgnoreCase)) return 0;
		if (locale.StartsWith("fr", StringComparison.OrdinalIgnoreCase)) return 1;
		return -1;
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

	private static Dictionary<string, string[]> LoadCatalog()
	{
		try
		{
			using Stream stream = typeof(TextTranslations).Assembly.GetManifestResourceStream("CorpPresence.Translations.json");
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
