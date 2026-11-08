using System;
using System.Collections.Generic;

namespace CorpPresence;

// CSV files are read by the game's loader, never by this mod.
internal static class OfficialTranslations
{
    private static LoadedMod _lastMod;
    private static string _lastLocale;
    private static Dictionary<string, string> _lastTable;
    private static int _lastCount = -1;
    private static int _version;

    private static Dictionary<string, string> Current(string locale)
    {
        var mod = ModLoader.Mods.Find(m => m.Manifest.ID == "CorpPresence");
        Dictionary<string, string> table = null;
        if (mod != null && locale != null) mod.Localization.TryGetValue(locale, out table);
        if (!ReferenceEquals(mod, _lastMod) || locale != _lastLocale || !ReferenceEquals(table, _lastTable) || (table?.Count ?? 0) != _lastCount)
        {
            _lastMod = mod; _lastLocale = locale; _lastTable = table;
            _lastCount = table?.Count ?? 0; _version++;
        }
        return table;
    }

    internal static int Version { get { Current(LocHelper.GetCurrentLocaleCode()); return _version; } }
    internal static bool HasLocale(string locale) => Current(locale)?.Count > 0;

    internal static bool TryGet(string key, string source, string locale, out string value)
    {
        value = null;
        if (key == null || source == null || !string.Equals(locale, LocHelper.GetCurrentLocaleCode(), StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            // Check exact-locale presence before the API's English fallback, so missing
            // regional CSV entries cannot replace existing Russian/French/Chinese text.
            var table = Current(locale);
            if (table == null || !table.TryGetValue(key, out var entry) || string.IsNullOrWhiteSpace(entry)) return false;
            string candidate = ModHelper.GetLocalized("CorpPresence", key);
            if (string.IsNullOrWhiteSpace(candidate) || !TranslationFormat.Compatible(source, candidate)) return false;
            value = candidate;
            return true;
        }
        catch (Exception) { return false; }
    }

    internal static bool TryFormat(string key, FormattableString source, string locale, out string value)
    {
        value = null;
        if (!TryGet(key, source.Format, locale, out var template)) return false;
        try { value = string.Format(template, source.GetArguments()); return true; }
        catch (FormatException) { return false; }
    }
}
