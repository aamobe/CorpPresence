using System;

namespace CorpPresence;

internal static class ConfigLocalization
{
	private const string VisitSectionMarker = " / Visits: ";

	internal static void AfterOpen(LoadedMod mod, CustomUIWindow __result)
	{
		if (__result == null || mod?.Manifest.ID != "CorpPresence") return;
		HookGuard.Run("配置诊断入口", () => ConfigDiagnostics.Attach(__result));
		if (!TextTranslations.HasLocale(L.Locale)) return;
		ModConfig config = ModConfig.Find(mod.Manifest.ID);
		if (config == null) return;
		__result.SetTitle(ConfigDisplay.Text(mod.Manifest.Name));

		foreach (string section in config.Sections)
		{
			int marker = section.IndexOf(VisitSectionMarker, StringComparison.Ordinal);
			string label = marker < 0
				? ConfigDisplay.Text(section)
				: TextTranslations.FormatKey("presentation.configLocalization.afterOpen.visits", "Visits: {0}", L.Locale, ConfigDisplay.Text(section.Substring(marker + VisitSectionMarker.Length)));
			__result.Get("section_" + section)?.SetText(label);
		}

		foreach (ConfigEntry entry in config.Entries)
		{
			if (entry.Choices == null || entry.Choices.Length == 0) continue;
			CustomUIElement element = __result.Get(entry.Key);
			int index = element?.GetIndex() ?? -1;
			if (index < 0) continue;
			element.SetOptions(Array.ConvertAll(entry.Choices, choice => ConfigDisplay.Text(choice)));
			element.SetIndex(index);
		}
	}
}
