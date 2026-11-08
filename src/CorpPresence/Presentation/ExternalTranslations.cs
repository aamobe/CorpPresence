using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace CorpPresence;

internal static class ExternalTranslations
{
    private sealed class Snapshot
    {
        internal readonly Dictionary<string, Dictionary<string, string>> Locales = new(StringComparer.Ordinal);
        internal readonly HashSet<string> Warnings = new(StringComparer.Ordinal);
        internal string Override;
    }

    private static readonly Regex LocalePattern = new("^[a-z]{2,8}(?:-[a-z0-9]{1,8})*$", RegexOptions.CultureInvariant);
    private static Snapshot _snapshot;
    private static int _version;
    internal static string DefaultDirectory => Path.Combine(
        Path.GetDirectoryName(typeof(ExternalTranslations).Assembly.Location) ?? AppContext.BaseDirectory, "Translations");
    private static Snapshot Current { get { if (_snapshot == null) Reload(); return _snapshot; } }
    internal static int Version { get { _ = Current; return _version; } }
    internal static void Reload() => Reload(DefaultDirectory);
    internal static void Reload(string directory)
    {
        Snapshot next;
        try { next = Load(directory); }
        catch (Exception error) { Warn("外部翻译目录读取失败，沿用内置文本：" + error.Message); next = new Snapshot(); }
        _snapshot = next;
        _version++;
    }
    internal static void Clear() { _snapshot = new Snapshot(); _version++; }
    internal static string ResolveLocale(string gameLocale) => Current.Override ?? gameLocale;
    internal static bool HasLocale(string locale) => Parents(locale).Any(code => Current.Locales.ContainsKey(code));

    internal static bool TryGet(string key, string english, string locale, out string value)
    {
        value = null;
        foreach (string candidate in Candidates(key, english, locale)) { value = candidate; return true; }
        return false;
    }
    internal static bool TryFormat(string key, FormattableString english, string locale, out string value)
    {
        value = null;
        foreach (string candidate in Candidates(key, english.Format, locale))
        {
            try { value = string.Format(candidate, english.GetArguments()); return true; }
            catch (FormatException) { WarnOnce(Current, locale, key, "外部翻译格式无效，已回退："); }
        }
        return false;
    }
    private static IEnumerable<string> Candidates(string key, string english, string locale)
    {
        if (key == null || english == null) yield break;
        var snapshot = Current;
        foreach (string code in Parents(locale))
        {
            if (!snapshot.Locales.TryGetValue(code, out var entries) || !entries.TryGetValue(key, out var candidate)) continue;
            if (TranslationFormat.Compatible(english, candidate)) yield return candidate;
            else WarnOnce(snapshot, code, key, "外部翻译占位符无效，已回退：");
        }
    }
    private static void WarnOnce(Snapshot snapshot, string locale, string key, string message)
    {
        lock (snapshot.Warnings)
            if (snapshot.Warnings.Add(locale + "/" + key)) Warn(message + locale + "/" + key);
    }

    private static Snapshot Load(string directory)
    {
        var result = new Snapshot();
        if (string.IsNullOrEmpty(directory) || !System.IO.Directory.Exists(directory)) return result;
        foreach (string file in System.IO.Directory.GetFiles(directory).Where(p => string.Equals(Path.GetExtension(p), ".json", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p, StringComparer.Ordinal))
        {
            if (string.Equals(Path.GetFileName(file), "language.json", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                var root = Read(file);
                if (root["schemaVersion"]?.Type != JTokenType.Integer || (int)root["schemaVersion"] != 1) throw new InvalidDataException("schemaVersion must be 1");
                string locale = Normalize(root["locale"]?.Type == JTokenType.String ? (string)root["locale"] : null);
                if (locale == null) throw new InvalidDataException("Invalid locale code");
                if (root["translations"] is not JObject translations) throw new InvalidDataException("Missing translations object");
                if (!result.Locales.TryGetValue(locale, out var entries)) result.Locales[locale] = entries = new Dictionary<string, string>(StringComparer.Ordinal);
                int invalid = 0;
                foreach (var property in translations.Properties())
                {
                    if (property.Value.Type != JTokenType.String) { invalid++; continue; }
                    string text = (string)property.Value;
                    if (!string.IsNullOrEmpty(property.Name) && !string.IsNullOrWhiteSpace(text)) entries[property.Name] = text;
                }
                if (invalid > 0) Warn("外部翻译已忽略非字符串条目：" + Path.GetFileName(file));
            }
            catch (Exception error) { Warn("外部翻译文件已忽略 " + Path.GetFileName(file) + "：" + error.Message); }
        }
        string selection = Path.Combine(directory, "language.json");
        if (File.Exists(selection))
        {
            try
            {
                var root = Read(selection);
                string raw = root["locale"]?.Type == JTokenType.String ? (string)root["locale"] : null;
                if (!string.IsNullOrEmpty(raw) && !string.Equals(raw.Trim(), "auto", StringComparison.OrdinalIgnoreCase))
                {
                    string locale = Normalize(raw);
                    if (locale == null || !Parents(locale).Any(code => result.Locales.ContainsKey(code))) throw new InvalidDataException("Selected language has no loaded pack");
                    result.Override = locale;
                }
            }
            catch (Exception error) { Warn("翻译语言选择已忽略，使用游戏语言：" + error.Message); }
        }
        return result;
    }

    private static JObject Read(string file)
    {
        if (new FileInfo(file).Length > 4 * 1024 * 1024) throw new InvalidDataException("Translation file exceeds 4 MiB");
        return JObject.Parse(File.ReadAllText(file, new UTF8Encoding(false, true)));
    }
    private static string Normalize(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale)) return null;
        string normalized = locale.Trim().Replace('_', '-').ToLowerInvariant();
        return LocalePattern.IsMatch(normalized) ? normalized : null;
    }
    private static IEnumerable<string> Parents(string locale)
    {
        string code = Normalize(locale);
        while (code != null)
        {
            yield return code;
            int separator = code.LastIndexOf('-');
            code = separator < 0 ? null : code.Substring(0, separator);
        }
    }
    private static void Warn(string message) { try { ModLog.Warn(message); } catch { } }
}

// Compare placeholder identities before accepting player text; allow reordering/repetition.
internal static class TranslationFormat
{
    internal static bool Compatible(string source, string translated)
    {
        if (!TryIndices(translated, out var actual)) return false;
        return TryIndices(source, out var expected) ? expected.SetEquals(actual) : actual.Count == 0;
    }
    private static bool TryIndices(string text, out HashSet<int> indices)
    {
        indices = new HashSet<int>();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '}') { if (i + 1 < text.Length && text[i + 1] == '}') { i++; continue; } return false; }
            if (text[i] != '{') continue;
            if (i + 1 < text.Length && text[i + 1] == '{') { i++; continue; }
            i++;
            if (!Number(text, ref i, out int index)) return false;
            indices.Add(index);
            Spaces(text, ref i);
            if (i < text.Length && text[i] == ',')
            {
                i++; Spaces(text, ref i);
                if (i < text.Length && text[i] == '-') i++;
                if (!Number(text, ref i, out _)) return false;
                Spaces(text, ref i);
            }
            if (i < text.Length && text[i] == ':')
            {
                i++;
                while (i < text.Length && text[i] != '}') { if (text[i] == '{') return false; i++; }
            }
            if (i >= text.Length || text[i] != '}') return false;
        }
        return true;
    }
    private static bool Number(string text, ref int index, out int value)
    {
        value = 0; int start = index;
        while (index < text.Length && text[index] >= '0' && text[index] <= '9')
        {
            value = value * 10 + text[index++] - '0';
            if (value > 1024) return false;
        }
        return index != start;
    }
    private static void Spaces(string text, ref int index) { while (index < text.Length && text[index] == ' ') index++; }
}
