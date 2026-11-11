using CorpPresence;
using System.Reflection;

internal static class OfficialTranslationChecks
{
    internal static void Run(Action<string, Action> check)
    {
        void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected [{expected}], got [{actual}]"); }
        void WithMod(Action<LoadedMod> action)
        {
            var previousLocale = LocHelper.Locale;
            var mod = new LoadedMod();
            ModLoader.Mods.Add(mod);
            LocHelper.Locale = "fr";
            try { action(mod); }
            finally { ModLoader.Mods.Remove(mod); LocHelper.Locale = previousLocale; }
        }
        string Get(string source = "Amount {0}", string locale = "fr") =>
            OfficialTranslations.TryGet("checks.cache", source, locale, out var value) ? value : null;

        check("official translation observes same-frame edits, table/mod replacement and readiness", () => WithMod(mod => {
            Equal<string>(null, Get());
            var table = mod.Localization["fr"] = new() { ["checks.cache"] = "Montant {0}" };
            Equal("Montant {0}", Get());
            table["checks.cache"] = "Total {0}";
            Equal("Total {0}", Get());
            mod.Localization["fr"] = new() { ["checks.cache"] = "Remplace {0}" };
            Equal("Remplace {0}", Get());
            ModLoader.Mods.Remove(mod);
            Equal<string>(null, Get());
            var replacement = new LoadedMod();
            replacement.Localization["fr"] = new() { ["checks.cache"] = "Nouveau {0}" };
            ModLoader.Mods.Add(replacement);
            try { Equal("Nouveau {0}", Get()); }
            finally { ModLoader.Mods.Remove(replacement); }
        }));
        check("official format cache includes source format and retries corrected invalid entries", () => WithMod(mod => {
            var table = mod.Localization["fr"] = new() { ["checks.cache"] = "Valeur {0}" };
            Equal("Valeur {0}", Get());
            Equal<string>(null, Get("Amount {1}"));
            Equal<string>(null, Get("Plain"));
            table["checks.cache"] = "Broken {0";
            Equal<string>(null, Get());
            table["checks.cache"] = "Repare {0}";
            Equal("Repare {0}", Get());
        }));
        check("official lookup keeps exact-locale guard despite warm English result", () => WithMod(mod => {
            mod.Localization["en"] = new() { ["checks.cache"] = "English {0}" };
            LocHelper.Locale = "en";
            Equal("English {0}", Get(locale: "en"));
            LocHelper.Locale = "fr-CA";
            Equal<string>(null, Get(locale: "fr-CA"));
            mod.Localization["fr-CA"] = new() { ["checks.cache"] = " " };
            Equal<string>(null, Get(locale: "fr-CA"));
            mod.Localization["fr-CA"]["checks.cache"] = "Quebec {0}";
            Equal("Quebec {0}", Get(locale: "fr-CA"));
            Equal<string>(null, Get(locale: "en"));
        }));
        check("official validation cache bounds retained entries and text without caching failures", () => WithMod(mod => {
            // Inspect retained entries only for the memory bound; all lookups use production code.
            var cache = (HashSet<(string Source, string Candidate)>)typeof(OfficialTranslations)
                .GetField("CompatibleFormats", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            if (cache == null) throw new Exception("No bounded validation cache");
            var table = mod.Localization["fr"] = new();
            for (int i = 0; i < 3000; i++) {
                table["checks.cache"] = "Unique " + i + " {0}";
                Equal(table["checks.cache"], Get());
            }
            if (cache.Count > 256) throw new Exception("Validation cache grew beyond its entry bound");
            int retained = cache.Count;
            table["checks.cache"] = new string('x', 10000) + " {0}";
            Equal(table["checks.cache"], Get());
            Equal(retained, cache.Count);
            table["checks.cache"] = "Wrong {1}";
            Equal<string>(null, Get());
            table.Clear();
            Equal<string>(null, Get());
            Equal(retained, cache.Count);
            table["checks.cache"] = "Recovered {0}";
            Equal("Recovered {0}", Get());
        }));
        check("repeated official formats avoid per-call placeholder set allocations", () => WithMod(mod => {
            mod.Localization["fr"] = new() { ["checks.cache"] = "Total {0:0.00} / {1}" };
            const string source = "Amount {0:0.00} / {1}";
            for (int i = 0; i < 100; i++) Get(source);
            long before = GC.GetAllocatedBytesForCurrentThread();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++) Get(source);
            watch.Stop();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Console.WriteLine($"MEASURE official TryGet .NET stub run: {allocated} bytes / 10000 calls, {watch.Elapsed.TotalMilliseconds:F3} ms (not Unity)");
            if (allocated > 160 * 10000) throw new Exception($"Repeated validation allocated {allocated} bytes; expected under 160 bytes/call including stub API");
        }));
    }
}
