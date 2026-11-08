using System.Reflection;
using System.Runtime.CompilerServices;
using CorpPresence;
using CorpPresence.UI;
using Newtonsoft.Json;

int passed = 0, failed = 0;
void Check(string name, Action action)
{
    try { action(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + (e.InnerException ?? e).Message); }
}
void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected [{expected}], got [{actual}]");
}
void Locale(string locale) { LocHelper.Locale = locale; UnityEngine.Time.frameCount++; }
Check("built-in release contains no external translation file loader", () => {
    Equal<Type>(null, typeof(L).Assembly.GetType("CorpPresence.ExternalTranslations"));
});
string Key(string key, string zh, string en)
{
    return L.K(key, zh, en);
}
string FormatKey(string key, FormattableString zh, FormattableString en)
{
    return L.K(key, zh, en);
}
object NewKeyOverload(Type[] types, params object[] values)
{
    var method = typeof(L).GetMethod("K", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
    if (method == null) throw new Exception("Missing stable-key overload: " + string.Join(", ", types.Select(t => t.Name)));
    return method.Invoke(null, values);
}
// Controlled malformed resource entries exercise the public fallback behavior.
void WithTranslation(string[] values, Action<string> action)
{
    var field = typeof(TextTranslations).GetField("KeyedCatalog", BindingFlags.NonPublic | BindingFlags.Static);
    var catalog = ((Lazy<Dictionary<string, string[]>>)field.GetValue(null)).Value;
    const string key = "checks.temporary";
    catalog.Add(key, values);
    try { action(key); } finally { catalog.Remove(key); }
}
Check("stable key survives English copy changes", () => {
    Locale("fr-FR"); Equal("Catalogue de marque", Key("ui.menu.brandCatalog", "品牌目录", "Changed English copy"));
    Locale("RU-ru"); Equal(TextTranslations.Select("Brand Catalog", "ru"), Key("ui.menu.brandCatalog", "品牌目录", "Changed English copy"));
});
Check("Chinese, English and unsupported locales keep supplied copy", () => {
    foreach (var locale in new[] { "zh-CN", "zh-TW", "", "en-US", "de-DE" }) {
        Locale(locale); Equal(locale.StartsWith("zh") || locale == "" ? "原中文" : "English fallback",
            Key("ui.menu.brandCatalog", "原中文", "English fallback"));
    }
});
Check("missing keys retain legacy translations then English fallback", () => {
    Locale("fr"); Equal(TextTranslations.Select("Brand Catalog", "fr"), Key("missing.key", "中文", "Brand Catalog"));
    Equal("Untranslated fallback", Key("missing.key", "中文", "Untranslated fallback"));
    Equal(null, Key(null, "中文", null));
});
Check("formatted strings keep format specifiers, arguments and locale switching", () => {
    Locale("fr"); Equal(string.Format(TextTranslations.Select("List {0}", "fr"), "12.50₵"),
        FormatKey("ui.catalog.item.listPrice", $"标价 {"12.50₵"}", $"List {"12.50₵"}"));
    Locale("zh"); Equal($"金额 {12.5:0.00}", FormatKey("missing.key", $"金额 {12.5:0.00}", $"Amount {12.5:0.00}"));
    Locale("en"); Equal($"Amount {12.5:0.00}", FormatKey("missing.key", $"金额 {12.5:0.00}", $"Amount {12.5:0.00}"));
});
Check("null, empty and short translations retain the legacy fallback", () => {
    foreach (var values in new[] { null, Array.Empty<string>(), new[] { "", "" }, new[] { "Russian only" } })
        WithTranslation(values, key => { Locale("fr"); Equal("Catalogue de marque", Key(key, "中文", "Brand Catalog")); });
    Equal("Fallback", TextTranslations.SelectKey("ui.menu.brandCatalog", "Fallback", null));
});
Check("bad translated formats fall back and reordered arguments keep their formats", () => {
    WithTranslation(new[] { "{4}", "Broken {4}" }, key => {
        Locale("fr"); Equal("Amount 7", FormatKey(key, $"金额 {7}", $"Amount {7}"));
    });
    WithTranslation(new[] { "", "{1}: {0:0.00} / {0:0.00}" }, key => {
        Locale("fr"); Equal($"value: {12.5:0.00} / {12.5:0.00}",
            FormatKey(key, $"中文 {12.5} {"value"}", $"Amount {12.5} {"value"}"));
    });
});
Check("translate-only keys preserve existing language branches", () => {
    foreach (var locale in new[] { "zh", "en", "ru", "fr", "de" }) {
        Locale(locale);
        Equal(L.Translate("Brand Catalog"), (string)NewKeyOverload(new[] { typeof(string), typeof(string) }, "ui.menu.brandCatalog", "Brand Catalog"));
        Equal(null, (string)NewKeyOverload(new[] { typeof(string), typeof(string) }, "missing", null));
    }
});
Check("translate-only format keys use stable templates and retain format fallback", () => {
    Locale("fr");
    Equal("Catalogue 12.50₵", (string)NewKeyOverload(new[] { typeof(string), typeof(FormattableString) },
        "ui.catalog.item.listPrice", (FormattableString)$"Changed {"12.50₵"}"));
    WithTranslation(new[] { "", "Broken {8}" }, key =>
        Equal("Amount 7", (string)NewKeyOverload(new[] { typeof(string), typeof(FormattableString) }, key, (FormattableString)$"Amount {7}")));
});
Check("mixed string and template keys preserve eagerly formatted Chinese", () => {
    foreach (var locale in new[] { "zh", "en", "ru", "fr" }) {
        Locale(locale);
        Equal(L.T("标价 12.50₵", L.Translate((FormattableString)$"List {"12.50₵"}")),
            (string)NewKeyOverload(new[] { typeof(string), typeof(string), typeof(FormattableString) },
                "ui.catalog.item.listPrice", "标价 12.50₵", (FormattableString)$"List {"12.50₵"}"));
    }
});
void VerifyMigrationCases(string filename, int expectedCount)
{
    var cases = JsonConvert.DeserializeObject<List<MigrationCase>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, filename)));
    var priorCulture = System.Globalization.CultureInfo.CurrentCulture;
    try {
        foreach (var culture in new[] { "en-US", "fr-FR" }) {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
            foreach (var locale in new[] { "zh", "en", "ru", "fr" }) {
                Locale(locale);
                foreach (var item in cases) {
                    var arguments = Enumerable.Range(0, 16).Select(i => (object)(i + 1)).ToArray();
                    var english = FormattableStringFactory.Create(item.English, arguments);
                    string expected, actual;
                    if (item.Method == "L.Translate") {
                        expected = item.Formatted ? L.Translate(english) : L.Translate(item.English);
                        actual = item.Formatted ? L.K(item.Key, english) : L.K(item.Key, item.English);
                    } else if (item.Nested) {
                        // A dynamic Chinese concatenation is preserved verbatim in source;
                        // use a supplied string here to check the mixed-overload boundary.
                        string chinese = item.Chinese == null ? "动态中文示例" : string.Format(item.Chinese, arguments);
                        expected = L.T(chinese, L.Translate(english));
                        actual = L.K(item.Key, chinese, english);
                    } else if (item.Formatted) {
                        string chinese = string.Format(item.Chinese, arguments);
                        string renderedEnglish = english.ToString();
                        expected = L.T(chinese, renderedEnglish);
                        actual = L.K(item.Key, chinese, renderedEnglish);
                    } else {
                        expected = L.T(item.Chinese, item.English);
                        actual = L.K(item.Key, item.Chinese, item.English);
                    }
                    if (expected != actual) throw new Exception($"{item.Key} ({locale}, {culture}): [{expected}] != [{actual}]");
                }
            }
        }
        Equal(expectedCount, cases.Count);
    } finally { System.Globalization.CultureInfo.CurrentCulture = priorCulture; }
}
Check("batch 2 calls preserve all four languages and culture-specific formatting", () => VerifyMigrationCases("Batch2Cases.json", 258));
Check("batch 3 company texts preserve all four languages and culture-specific formatting", () => VerifyMigrationCases("Batch3Cases.json", 171));
Check("remaining fixed texts preserve all four languages and culture-specific formatting", () => VerifyMigrationCases("Batch4Cases.json", 793));
Check("all bound item display fields preserve their source translations", () => VerifyMigrationCases("DefinitionCases.json", 231));
Check("dynamic arrays, catalog labels and period settings preserve their translations", () => VerifyMigrationCases("DynamicCases.json", 38));
Check("configuration display identities preserve their source translations", () => VerifyMigrationCases("ConfigCases.json", 77));
Check("courier producers preserve their original translations", () => VerifyMigrationCases("CourierCases.json", 22));
Check("keyed arrays preserve references, indices, missing keys and locale fallback", () => {
    string[] zh = { "品牌目录", "后一项" }, en = { "Brand Catalog", "Untranslated" };
    string[] keys = { "ui.menu.brandCatalog" };
    Locale("zh"); Equal(true, ReferenceEquals(zh, L.K(keys, zh, en)));
    Locale("en"); Equal(true, ReferenceEquals(en, L.K(keys, zh, en)));
    Locale("fr"); var localized = L.K(keys, zh, en);
    Equal("Catalogue de marque", localized[0]); Equal("Untranslated", localized[1]);
    Equal(null, L.K(keys, zh, null));
});
Check("configuration adapter keeps identities and unknown values intact", () => {
    var cases = JsonConvert.DeserializeObject<List<MigrationCase>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ConfigCases.json")));
    foreach (var locale in new[] { "ru", "fr" }) {
        Locale(locale);
        foreach (var item in cases) Equal(TextTranslations.Select(item.English, locale), ConfigDisplay.Text(item.English));
        Equal("foreign unregistered value", ConfigDisplay.Text("foreign unregistered value"));
        Equal(null, ConfigDisplay.Text(null));
    }
});
Check("every registered item definition receives independent display key metadata", () => {
    var defs = new Dictionary<string, Goods.Def> { ["stamina"] = new(), ["foreign_definition"] = new() };
    GoodsText.Bind(defs);
    Equal("items.definitions.staminaBooster.name", defs["stamina"].TextKeys.Name);
    Equal(null, defs["foreign_definition"].TextKeys);
    Equal(null, GoodsText.ForId(null));
});
Check("unsupported locale prefixes keep English fallback", () => {
    foreach (var locale in new[] { "rup", "zha" }) {
        Locale(locale); Equal(true, L.En); Equal(false, TextTranslations.HasLocale(locale));
        Equal("Brand Catalog", L.K("ui.menu.brandCatalog", "品牌目录", "Brand Catalog"));
        Equal("Brand Catalog", MenuLabels.Get(9, L.En)[3]);
    }
});
Check("caller-formatted built-in templates preserve fallback", () => {
    Locale("fr-ca");
    Equal(string.Format(TextTranslations.Select("Chapter {0} ({1})", "fr"), 1, "main story"),
        L.F("presentation.storyGuide.build.chapter", "第 {0} 章（{1}）", "Chapter {0} ({1})", 1, "main story"));
    Locale("zh"); Equal("第 1 章（主线）", L.F("presentation.storyGuide.build.chapter", "第 {0} 章（{1}）", "Chapter {0} ({1})", 1, "主线"));
});
Check("company order error uses a stable key even after English copy changes", () => {
    Locale("ru");
    Equal(TextTranslations.Select("Save not ready", "ru"),
        L.K("companies.services.orders.placeOrder.saveNotReady", "存档未就绪", "Changed fallback"));
});
Check("every migrated translation matches the legacy table", () => {
    using var stream = typeof(L).Assembly.GetManifestResourceStream("CorpPresence.Translations.Keys.json");
    if (stream == null) throw new Exception("Missing keyed embedded resource");
    using var reader = new StreamReader(stream);
    var catalog = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(reader.ReadToEnd());
    // English fallback metadata used for migration audit lives outside the runtime catalog.
    var migration = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Migration.json")));
    var composed = JsonConvert.DeserializeObject<Dictionary<string, List<ComposedPart>>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Composed.json")));
    Equal(migration.Count, catalog.Count);
    foreach (var entry in migration) foreach (var locale in new[] { "ru", "fr" }) {
        string expected = composed.TryGetValue(entry.Key, out var parts)
            ? string.Concat(parts.Select(p => p.Translate ? TextTranslations.Select(p.Text, locale) : p.Text))
            : TextTranslations.Select(entry.Value, locale);
        Locale(locale); Equal(expected, Key(entry.Key, "中文", entry.Value));
        var args = Enumerable.Range(0, 12).Select(i => (object)(i + 1)).ToArray();
        string formatted;
        try { formatted = string.Format(expected, args); }
        catch (FormatException) { formatted = FormattableStringFactory.Create(entry.Value, args).ToString(); }
        Equal(formatted,
            FormatKey(entry.Key, FormattableStringFactory.Create("中文", args), FormattableStringFactory.Create(entry.Value, args)));
    }
});
Check("menus keep all indices, cached arrays and extension behavior", () => {
    var baseline = JsonConvert.DeserializeObject<Dictionary<string, string[][]>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MenuBaseline.json")));
    foreach (var locale in new[] { "zh", "en", "ru", "fr" }) {
        Locale(locale);
        int[] lengths = { 5, 4, 5, 4, 3, 3, 2, 1, 1, 5, 3, 6 };
        for (int i = 0; i < lengths.Length; i++) {
            var labels = MenuLabels.Get(i, L.En); Equal(lengths[i], labels.Length);
            for (int line = 0; line < labels.Length; line++) Equal(baseline[locale][i][line], labels[line]);
            Equal(true, ReferenceEquals(labels, MenuLabels.Get(i, L.En)));
            Equal(true, ReferenceEquals(labels, MenuLabels.Append(labels, null)));
            var extra = MenuLabels.Append(labels, "extra"); Equal(labels.Length + 1, extra.Length);
            Equal("extra", extra[^1]); Equal(true, ReferenceEquals(extra, MenuLabels.Append(labels, "extra")));
        }
    }
});
if (args.Length >= 1)
    Check("compiled mod embeds the exact keyed resource", () => {
        var assembly = Assembly.LoadFile(Path.GetFullPath(args[0]));
        using var stream = assembly.GetManifestResourceStream("CorpPresence.Translations.Keys.json");
        if (stream == null) throw new Exception("Compiled mod is missing keyed translations");
        using var reader = new StreamReader(stream);
        using var expectedStream = typeof(L).Assembly.GetManifestResourceStream("CorpPresence.Translations.Keys.json");
        using var expectedReader = new StreamReader(expectedStream);
        Equal(expectedReader.ReadToEnd(), reader.ReadToEnd());
    });
Console.WriteLine($"{passed} passed, {failed} failed");
return failed == 0 ? 0 : 1;

record MigrationCase(string File, string Key, string Method, string English, string Chinese, bool Formatted, bool Nested);
record ComposedPart(string Text, bool Translate);
