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
OfficialTranslationChecks.Run(Check);
void Locale(string locale) { LocHelper.Locale = locale; UnityEngine.Time.frameCount++; }
Check("dialogue omits selected decorative actions while preserving standalone and essential beats", () => {
    foreach (var locale in new[] { "zh", "en", "ru", "fr" }) {
        Locale(locale);
        Equal("原有台词 {0}", DialogueText.WithAction("action", "原有台词 {0}", false));
        string prefix = locale == "zh" ? "<i>（action）</i>" : "<i>(action)</i> ";
        Equal(prefix + "原有台词", DialogueText.WithAction("action", "原有台词"));
        Equal(prefix, DialogueText.WithAction("action", "", false));
    }
});
Check("official CSV lookup overrides built-in text and menus without mod file IO", () => {
    var mod = new LoadedMod();
    ModLoader.Mods.Add(mod);
    try {
        Locale("fr"); Equal("Catalogue de marque", L.K("ui.menu.brandCatalog", "品牌目录", "Brand Catalog"));
        mod.Localization["fr"] = new() { ["ui.menu.brandCatalog"] = "CSV catalogue" };
        Locale("fr"); Equal("CSV catalogue", L.K("ui.menu.brandCatalog", "品牌目录", "Brand Catalog"));
        Equal("CSV catalogue", MenuLabels.Get(9, L.En)[3]);
        Equal("CSV catalogue", Brand.Info("shampoo").Name);
        if (ModHelper.Calls == 0) throw new Exception("Official API was never called");
    } finally { ModLoader.Mods.Clear(); Locale("en"); }
});
Check("built-in release contains no external translation file loader", () => {
    Equal<Type>(null, typeof(L).Assembly.GetType("CorpPresence.ExternalTranslations"));
});
Check("partial official CSV preserves regional, Chinese and missing-entry fallback", () => {
    var mod = new LoadedMod(); ModLoader.Mods.Add(mod);
    try {
        mod.Localization["en"] = new() { ["ui.menu.brandCatalog"] = "English CSV" };
        mod.Localization["fr-CA"] = new() { ["ui.menu.brandCatalog"] = " " };
        Locale("fr-CA"); Equal("Catalogue de marque", L.K("ui.menu.brandCatalog", "品牌目录", "Brand Catalog"));
        Equal("Untranslated", L.K("missing.key", "中文", "Untranslated"));
        Locale("zh-CN"); Equal("中文原文", L.K("ui.menu.brandCatalog", "中文原文", "Brand Catalog"));
        mod.Localization["zh-CN"] = new() { ["ui.menu.brandCatalog"] = "中文CSV", ["ui.checkout.cash"] = "余额 {0}₵" };
        Locale("zh-CN"); Equal("中文CSV", L.K("ui.menu.brandCatalog", "品牌目录", "Brand Catalog"));
        Equal("中文CSV", L.K(new[] { "ui.menu.brandCatalog" }, new[] { "品牌目录" }, new[] { "Brand Catalog" })[0]);
        Equal("余额 42₵", L.K("ui.checkout.cash", (FormattableString)$"现金 {42}₵", (FormattableString)$"Cash {42}₵"));
        Locale("en"); Equal("English CSV", MenuLabels.Get(9, L.En)[3]);
        mod.Localization["de"] = new() { ["ui.menu.brandCatalog"] = "Katalog" };
        Locale("de"); Equal("Katalog", MenuLabels.Get(9, L.En)[3]);
    } finally { ModLoader.Mods.Clear(); Locale("en"); }
});
Check("official CSV invalid placeholders and runtime formats use existing fallback", () => {
    var mod = new LoadedMod(); ModLoader.Mods.Add(mod);
    try {
        var table = mod.Localization["fr"] = new();
        Locale("fr");
        foreach (string bad in new[] { "Missing argument", "Wrong {1}", "Broken {0", "Bad {0,+3}", "Bad {0:Q}" }) {
            table["ui.checkout.cash"] = bad;
            Equal(string.Format(TextTranslations.Select("Cash {0}₵", "fr"), 42), L.F("ui.checkout.cash", "现金 {0}₵", "Cash {0}₵", 42));
            Equal(string.Format(TextTranslations.Select("Cash {0}₵", "fr"), 42), L.K("ui.checkout.cash", (FormattableString)$"现金 {42}₵", (FormattableString)$"Cash {42}₵"));
        }
        table["checks.order"] = "{1}: {0:0.00} / {0:0.00}";
        Equal($"value: {12.5:0.00} / {12.5:0.00}", L.K("checks.order", (FormattableString)$"中文 {12.5} {"value"}", (FormattableString)$"Amount {12.5} {"value"}"));
        table["production.perfume.plants.registerAroma.producesMLOilflowerMash"] = "CSV {0:0.#} mL";
        Equal($"CSV {16.5:0.#} mL", L.KRendered("production.perfume.plants.registerAroma.producesMLOilflowerMash", "中文", (FormattableString)$"Produces {16.5:0.#} mL of oilflower mash."));
    } finally { ModLoader.Mods.Clear(); Locale("en"); }
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
static bool HasNewAidCopy(string key) => key is
    "malice.envoy.onDayStart.aidSubscriptionGrace"
    or "malice.envoy.onDayStart.pastelOrCarmichealSubscriptionLapsed"
    or "malice.envoy.create.administrationCanOfferGrantNeedn"
    or "malice.envoy.create.threeWeeksNotDayMissed"
    or "presentation.storyGuide.envoyPages.chapterAidMaliceModeEnvoy";
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
                    if (HasNewAidCopy(item.Key)) continue;
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
Check("shipped CSV preserves all migration fixtures in four languages and two cultures", () => {
    var mod = new LoadedMod();
    var audit = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Migration.json")));
    using var catalogStream = typeof(L).Assembly.GetManifestResourceStream("CorpPresence.Translations.Keys.json");
    using var catalogReader = new StreamReader(catalogStream);
    var catalog = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(catalogReader.ReadToEnd());
    foreach (var locale in new[] { "en", "ru", "fr" }) {
        var entries = mod.Localization[locale] = new();
        foreach (string line in File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Localization", locale + ".csv"))) {
            int separator = line.IndexOf(',');
            string raw = line[(separator + 1)..].Trim();
            string value = raw[1..^1].Replace("\"\"", "\"").Replace("\\n", "\n");
            entries.Add(line[..separator], value);
        }
        Equal(audit.Count, entries.Count);
        foreach (var entry in audit.Where(entry => entry.Key.StartsWith("production.workBay.", StringComparison.Ordinal)))
            Equal(locale == "en" ? entry.Value : catalog[entry.Key][locale == "ru" ? 0 : 1], entries[entry.Key]);
    }
    ModLoader.Mods.Add(mod);
    try {
        foreach (var (file, count) in new[] { ("Batch2Cases.json", 258), ("Batch3Cases.json", 171), ("Batch4Cases.json", 793), ("DefinitionCases.json", 231), ("DynamicCases.json", 38), ("ConfigCases.json", 77), ("CourierCases.json", 22) }) VerifyMigrationCases(file, count);
    } finally { ModLoader.Mods.Clear(); Locale("en"); }
});
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
Check("stable translations match legacy copy or current feature translations", () => {
    using var stream = typeof(L).Assembly.GetManifestResourceStream("CorpPresence.Translations.Keys.json");
    if (stream == null) throw new Exception("Missing keyed embedded resource");
    using var reader = new StreamReader(stream);
    var catalog = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(reader.ReadToEnd());
    // English fallback metadata used for migration audit lives outside the runtime catalog.
    var migration = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Migration.json")));
    var composed = JsonConvert.DeserializeObject<Dictionary<string, List<ComposedPart>>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Composed.json")));
    Equal(migration.Count, catalog.Count);
    foreach (var entry in migration) foreach (var locale in new[] { "ru", "fr" }) {
        string expected = HasNewAidCopy(entry.Key)
            || entry.Key.StartsWith("diagnostics.report.", StringComparison.Ordinal)
            || entry.Key == "integration.hooks.load.inventoryDecodeFailed"
            || entry.Key.StartsWith("mango.", StringComparison.Ordinal)
            || entry.Key.StartsWith("wasteBattery.", StringComparison.Ordinal)
            || entry.Key.StartsWith("ufoBackpack.", StringComparison.Ordinal) || entry.Key.StartsWith("fufuDoll.", StringComparison.Ordinal)
            || entry.Key == "production.silverBlood.tooltip.bloodReserve"
            || entry.Key.StartsWith("production.workBay.", StringComparison.Ordinal)
            || entry.Key.StartsWith("production.cargoWarehouse.", StringComparison.Ordinal)
            ? catalog[entry.Key][locale == "ru" ? 0 : 1]
            : composed.TryGetValue(entry.Key, out var parts)
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
