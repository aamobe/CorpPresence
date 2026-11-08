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
void WritePack(string directory, string filename, string locale, Dictionary<string, string> translations)
{
    File.WriteAllText(Path.Combine(directory, filename), JsonConvert.SerializeObject(new { schemaVersion = 1, locale, translations }));
}
void WithPacks(Action<string> setup, Action<string> check)
{
    string directory = Path.Combine(Path.GetTempPath(), "CorpPresence-LocalizationChecks-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try { setup(directory); ExternalTranslations.Reload(directory); check(directory); }
    finally {
        ExternalTranslations.Clear();
        if (!Path.GetFullPath(directory).StartsWith(Path.Combine(Path.GetTempPath(), "CorpPresence-LocalizationChecks-"), StringComparison.OrdinalIgnoreCase)) throw new Exception("Unexpected fixture directory");
        Directory.Delete(directory, true);
    }
}
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
Check("external JSON adds a language and supports parent locale and menu labels", () => {
    var type = typeof(L).Assembly.GetType("CorpPresence.ExternalTranslations");
    if (type == null) throw new Exception("Missing external translation pack loader");
    string directory = Path.Combine(Path.GetTempPath(), "CorpPresence-LocalizationChecks-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try {
        File.WriteAllText(Path.Combine(directory, "es.json"), "{\"schemaVersion\":1,\"locale\":\"es\",\"translations\":{\"ui.menu.brandCatalog\":\"Catálogo\",\"ui.checkout.cash\":\"Saldo {0}₵\"}}");
        File.WriteAllText(Path.Combine(directory, "language.json"), "{\"locale\":\"es-MX\"}");
        var reload = type.GetMethod("Reload", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
        reload.Invoke(null, new object[] { directory });
        Locale("en"); Equal("es-mx", L.Locale);
        Equal("Catálogo", L.K("ui.menu.brandCatalog", "品牌目录", "Brand Catalog"));
        Equal("Saldo 42₵", L.K("ui.checkout.cash", (FormattableString)$"现金 {42}₵", (FormattableString)$"Cash {42}₵"));
        Equal("Catálogo", MenuLabels.Get(9, L.En)[3]);
    } finally {
        type.GetMethod("Clear", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, null);
        if (!Path.GetFullPath(directory).StartsWith(Path.Combine(Path.GetTempPath(), "CorpPresence-LocalizationChecks-"), StringComparison.OrdinalIgnoreCase)) throw new Exception("Unexpected fixture directory");
        Directory.Delete(directory, true);
    }
});
Check("regional packs inherit parent entries then built-in and source fallbacks", () => WithPacks(dir => {
    WritePack(dir, "fr.json", "fr", new() { ["ui.menu.brandCatalog"] = "Parent", ["ui.checkout.cash"] = "Solde {0}₵" });
    WritePack(dir, "fr-ca.json", "FR_ca", new() { ["ui.menu.brandCatalog"] = "Regional", ["ui.checkout.cash"] = "" });
}, _ => {
    Locale("fr-CA"); Equal("Regional", L.K("ui.menu.brandCatalog", "品牌目录", "Brand Catalog"));
    Equal("Solde 42₵", L.K("ui.checkout.cash", (FormattableString)$"现金 {42}₵", (FormattableString)$"Cash {42}₵"));
    Equal(TextTranslations.Select("Top tier reached", "fr"), L.K("ui.company.topTier", "已是最高档", "Top tier reached"));
    Equal("Fallback", L.K("missing.key", "中文", "Fallback"));
}));
Check("English and Chinese packs override explicit keys while retaining native missing text", () => WithPacks(dir => {
    WritePack(dir, "en.json", "en", new() { ["ui.menu.brandCatalog"] = "Custom catalog" });
    WritePack(dir, "zh.json", "zh-Hant", new() { ["ui.menu.brandCatalog"] = "品牌目錄", ["ui.checkout.cash"] = "餘額 {0}₵" });
}, _ => {
    Locale("en-US"); Equal("Custom catalog", MenuLabels.Get(9, L.En)[3]);
    Locale("zh-Hant-TW"); Equal(false, L.En);
    Equal("品牌目錄", L.K("ui.menu.brandCatalog", "品牌目录", "Brand Catalog"));
    Equal("餘額 42₵", L.K("ui.checkout.cash", (FormattableString)$"现金 {42}₵", (FormattableString)$"Cash {42}₵"));
    Equal("中文原文", L.K("missing.key", "中文原文", "Fallback"));
    Equal("品牌目錄", MenuLabels.Get(9, L.En)[3]); Equal("香水工艺", MenuLabels.Get(9, L.En)[4]);
    Equal("品牌目錄", L.K(new[] { "ui.menu.brandCatalog" }, new[] { "品牌目录" }, new[] { "Brand Catalog" })[0]);
}));
Check("invalid files and values cannot suppress valid packs or built-in text", () => WithPacks(dir => {
    File.WriteAllText(Path.Combine(dir, "broken.json"), "{ broken");
    File.WriteAllText(Path.Combine(dir, "version.json"), "{\"schemaVersion\":2,\"locale\":\"es\",\"translations\":{}}");
    File.WriteAllText(Path.Combine(dir, "locale.json"), "{\"schemaVersion\":1,\"locale\":\"../../es\",\"translations\":{}}");
    File.WriteAllText(Path.Combine(dir, "fr.json"), "{\"schemaVersion\":1,\"locale\":\"fr\",\"translations\":{\"ui.menu.brandCatalog\":123,\"ui.checkout.cash\":\"Cash without argument\"}}");
    File.WriteAllText(Path.Combine(dir, "language.json"), "{\"locale\":\"missing-language\"}");
}, _ => {
    Locale("fr"); Equal("fr", L.Locale); Equal(false, TextTranslations.HasLocale("es"));
    Equal(TextTranslations.Select("Brand Catalog", "fr"), L.K("ui.menu.brandCatalog", "品牌目录", "Brand Catalog"));
    Equal(string.Format(TextTranslations.Select("Cash {0}₵", "fr"), 7), L.K("ui.checkout.cash", (FormattableString)$"现金 {7}₵", (FormattableString)$"Cash {7}₵"));
}));
Check("external placeholder order, escaping and runtime format fallback are safe", () => WithPacks(dir => {
    WritePack(dir, "fr.json", "fr", new() { ["ui.checkout.cash"] = "Parent {0}₵", ["ui.checkout.voucherDetails"] = "{1} / {0} / {0}" });
    WritePack(dir, "fr-ca.json", "fr-CA", new() { ["ui.checkout.cash"] = "{0:Q}" });
}, dir => {
    Locale("fr-CA"); Equal("Parent 7₵", L.K("ui.checkout.cash", (FormattableString)$"现金 {7}₵", (FormattableString)$"Cash {7}₵"));
    Equal("2 / 1 / 1", L.K("ui.checkout.voucherDetails", (FormattableString)$"券 {1} 现金 {2}", (FormattableString)$"Vouchers −{1}₵ · Cash due {2}₵"));
    WritePack(dir, "fr-ca.json", "fr-CA", new() { ["ui.checkout.cash"] = "Saldo {{x}} {0,4:0.0}₵" });
    ExternalTranslations.Reload(dir);
    Equal(string.Format("Saldo {{x}} {0,4:0.0}₵", 12.5), L.K("ui.checkout.cash", (FormattableString)$"现金 {12.5}₵", (FormattableString)$"Cash {12.5}₵"));
    foreach (var bad in new[] { "{1}", "{0", "{0,9999999999999}", "no placeholder" }) {
        WritePack(dir, "fr-ca.json", "fr-CA", new() { ["ui.checkout.cash"] = bad }); ExternalTranslations.Reload(dir);
        Equal("Parent 7₵", L.K("ui.checkout.cash", (FormattableString)$"现金 {7}₵", (FormattableString)$"Cash {7}₵"));
    }
}));
Check("deterministic file precedence and reload invalidate menu and locale caches", () => WithPacks(dir => {
    WritePack(dir, "01.json", "fr", new() { ["ui.menu.brandCatalog"] = "First" });
    WritePack(dir, "99.json", "fr", new() { ["ui.menu.brandCatalog"] = "Last" });
}, dir => {
    Locale("fr"); var original = MenuLabels.Get(9, L.En); Equal("Last", original[3]);
    WritePack(dir, "99.json", "fr", new() { ["ui.menu.brandCatalog"] = "Reloaded" }); ExternalTranslations.Reload(dir);
    Equal("Reloaded", MenuLabels.Get(9, L.En)[3]); Equal(false, ReferenceEquals(original, MenuLabels.Get(9, L.En)));
    WritePack(dir, "es.json", "es", new() { ["ui.menu.brandCatalog"] = "Catálogo" });
    File.WriteAllText(Path.Combine(dir, "language.json"), "{\"locale\":\"es\"}"); ExternalTranslations.Reload(dir);
    Equal("es", L.Locale); Equal("Catálogo", MenuLabels.Get(9, L.En)[3]);
}));
Check("previously rendered dynamic text can receive external formatted translations", () => WithPacks(dir => {
    WritePack(dir, "es.json", "es", new() { ["production.perfume.plants.registerAroma.producesMLOilflowerMash"] = "Produce {0:0.#} mL de pulpa." });
}, _ => {
    Locale("es"); Equal($"Produce {16.5:0.#} mL de pulpa.", L.KRendered("production.perfume.plants.registerAroma.producesMLOilflowerMash", $"产生 {16.5:0.#} mL 含油花浆。", (FormattableString)$"Produces {16.5:0.#} mL of oilflower mash."));
    ExternalTranslations.Clear(); Locale("fr");
    Equal(L.T($"产生 {16.5:0.#} mL 含油花浆。", $"Produces {16.5:0.#} mL of oilflower mash."),
        L.KRendered("production.perfume.plants.registerAroma.producesMLOilflowerMash", $"产生 {16.5:0.#} mL 含油花浆。", (FormattableString)$"Produces {16.5:0.#} mL of oilflower mash."));
}));
Check("brand information refreshes when the same locale pack is reloaded", () => WithPacks(dir => {
    WritePack(dir, "fr.json", "fr", new() { ["ui.menu.brandCatalog"] = "First catalog" });
}, dir => {
    Locale("fr"); Equal("First catalog", Brand.Info("shampoo").Name);
    WritePack(dir, "fr.json", "fr", new() { ["ui.menu.brandCatalog"] = "Updated catalog" }); ExternalTranslations.Reload(dir);
    Equal("Updated catalog", Brand.Info("shampoo").Name);
}));
Check("caller-formatted UI templates fall back for invalid syntax and numeric formats", () => WithPacks(dir => {
    WritePack(dir, "fr.json", "fr", new() { ["presentation.storyGuide.build.chapter"] = "Parent {0} / {1}" });
    WritePack(dir, "fr-ca.json", "fr-ca", new() {
        ["presentation.storyGuide.build.chapter"] = "Chapter {0:Q} ({1})",
        ["presentation.configLocalization.afterOpen.visits"] = "{0,+3}",
        ["production.tobacco.addManual.instructions"] = "{0,+3}"
    });
}, _ => {
    Locale("fr-ca");
    Equal("Parent 1 / main story", L.F("presentation.storyGuide.build.chapter", "第 {0} 章（{1}）", "Chapter {0} ({1})", 1, "main story"));
    Equal("Parent 1 / main story", TextTranslations.FormatKey("presentation.storyGuide.build.chapter", "Chapter {0} ({1})", "fr-ca", 1, "main story"));
    Equal(string.Format(TextTranslations.Select("Visits: {0}", "fr"), "Isa"), TextTranslations.FormatKey("presentation.configLocalization.afterOpen.visits", "Visits: {0}", "fr-ca", "Isa"));
    Equal(string.Format(TextTranslations.Select("{0} Instructions", "fr"), "Machine"), TextTranslations.FormatKey("production.tobacco.addManual.instructions", "{0} Instructions", "fr-ca", "Machine"));
    Locale("zh"); Equal("第 1 章（主线）", L.F("presentation.storyGuide.build.chapter", "第 {0} 章（{1}）", "Chapter {0} ({1})", 1, "主线"));
}));
Check("new language codes sharing a prefix with Russian keep their own fallback", () => WithPacks(dir => {
    WritePack(dir, "rup.json", "rup", new() { ["ui.storyGuide.next"] = "Next in rup" });
}, _ => {
    Locale("rup"); Equal("Brand Catalog", L.K("ui.menu.brandCatalog", "品牌目录", "Brand Catalog"));
    Equal("Brand Catalog", MenuLabels.Get(9, L.En)[3]);
    ExternalTranslations.Clear(); Equal(false, TextTranslations.HasLocale("rup"));
    Equal("Brand Catalog", MenuLabels.Get(9, L.En)[3]);
}));
Check("new language codes sharing a prefix with Chinese reach translated branches", () => WithPacks(dir => {
    WritePack(dir, "zha.json", "zha", new() { ["ui.menu.brandCatalog"] = "Zha catalog" });
}, _ => {
    Locale("zha"); Equal(true, L.En);
    Equal("Zha catalog", L.En ? L.K("ui.menu.brandCatalog", "Brand Catalog") : "品牌目录");
    Equal("English fallback", L.K("missing.key", "中文回退", "English fallback"));
    ExternalTranslations.Clear(); Equal(true, L.En);
    Equal("Brand Catalog", MenuLabels.Get(9, L.En)[3]);
}));
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
if (args.Length >= 2)
    Check("exported complete template loads through the actual external loader", () => WithPacks(dir => {
        File.Copy(args[1], Path.Combine(dir, "template.json"));
    }, _ => {
        var pack = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(args[1]));
        string locale = (string)pack["locale"];
        Equal(1621, ((Newtonsoft.Json.Linq.JObject)pack["translations"]).Count);
        Locale(locale); Equal(true, TextTranslations.HasLocale(locale));
        Equal((string)pack["translations"]["ui.menu.brandCatalog"], L.K("ui.menu.brandCatalog", "品牌目录", "Brand Catalog"));
        Equal(string.Format((string)pack["translations"]["ui.checkout.cash"], 42),
            L.K("ui.checkout.cash", (FormattableString)$"现金 {42}₵", (FormattableString)$"Cash {42}₵"));
    }));
Console.WriteLine($"{passed} passed, {failed} failed");
return failed == 0 ? 0 : 1;

record MigrationCase(string File, string Key, string Method, string English, string Chinese, bool Formatted, bool Nested);
record ComposedPart(string Text, bool Translate);
