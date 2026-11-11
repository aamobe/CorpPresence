using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using CorpPresence.UI;

internal static class CompiledContent
{
    private const BindingFlags Fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static object Field(object value, string name) => value.GetType().GetField(name, Fields)?.GetValue(value);
    private static string Text(object value, string name) => (string)Field(value, name);
    private static IEnumerable<(string Key, object Value)> Pairs(object dictionary)
    {
        foreach (var pair in (IEnumerable)dictionary)
            yield return ((string)pair.GetType().GetProperty("Key").GetValue(pair), pair.GetType().GetProperty("Value").GetValue(pair));
    }

    internal static void Check(string dll, string managed)
    {
        Assembly Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            var path = Path.Combine(Path.GetFullPath(managed), name.Name + ".dll");
            return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        }
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(dll));
            // Nested Catalog has its own initializer. Do not read Goods static fields or invoke factories.
            var catalog = assembly.GetType("CorpPresence.Goods+Catalog", true);
            var definitions = Pairs(catalog.GetField("Defs", Fields).GetValue(null)).ToArray();
            var sources = Pairs(catalog.GetField("Sources", Fields).GetValue(null)).ToDictionary(p => p.Key, p => (string)p.Value);
            var extras = Pairs(catalog.GetField("ExtraArt", Fields).GetValue(null)).ToArray();
            var rows = new List<ContentRow>();
            foreach (var (key, def) in definitions)
            {
                var textKeys = new List<string>();
                var binding = Field(def, "TextKeys");
                textKeys.Add(binding == null ? null : Text(binding, "Name"));
                foreach (var (content, slot) in new[] { ("Flavor", "Flavor"), ("Desc", "Description") })
                    if (Field(def, content) != null) textKeys.Add(binding == null ? null : Text(binding, slot));
                foreach (var (content, slot) in new[] { ("Manual", "Manual"), ("ModuleEffects", "Effects") })
                    if (Field(Field(def, content), "Item1") != null) textKeys.Add(binding == null ? null : Text(binding, slot));
                var art = Text(def, "Art");
                var company = Text(def, "Company");
                string[] images;
                try { images = ContentContract.RequiredResources(Field(def, "ArtSource").ToString(), company, art, Text(def, "Fallback"), Field(def, "Power") != null); }
                catch (ArgumentException ex) { throw new Exception($"{sources[key]}/{key}: {ex.Message}", ex); }
                if (Field(def, "ArtSource").ToString() == "TintedNativeSprite" && !key.StartsWith("fate_coin_", StringComparison.Ordinal))
                    throw new Exception($"{sources[key]}/{key}: tinted native art currently requires the fate_coin_ runtime route");
                rows.Add(new(key, sources[key], textKeys.ToArray(), images, Text(def, "Name")));
            }
            var identityType = assembly.GetType("CorpPresence.ItemIdentity", true);
            var identities = new List<FactoryRow>();
            foreach (var entry in (IEnumerable)identityType.GetProperty("All").GetValue(null))
            {
                string Get(string name) => (string)entry.GetType().GetProperty(name).GetValue(entry);
                identities.Add(new(Get("Id"), Get("Goods"), Get("Company"), Get("LegacySprite")));
            }
            using var stream = assembly.GetManifestResourceStream("CorpPresence.Translations.Keys.json") ?? throw new Exception("Missing keyed translations resource");
            using var json = JsonDocument.Parse(stream);
            var texts = json.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
            var resources = assembly.GetManifestResourceNames().ToHashSet(StringComparer.Ordinal);
            var errors = ContentContract.Validate(rows, identities, texts, resources);
            var companyType = assembly.GetType("CorpPresence.Companies", true);
            var artLookup = catalog.GetMethod("TryGetArt", Fields) ?? throw new Exception("Missing production art lookup");
            void CheckLookup(string key, string expectedCompany, string expectedArt)
            {
                var parameters = new object[] { key, null, null, null };
                if (!(bool)artLookup.Invoke(null, parameters) || (string)parameters[2] != expectedCompany || (string)parameters[3] != expectedArt)
                    errors.Add("production art mapping disagrees with descriptor: " + key);
            }
            bool ResolveSprite(FactoryRow identity)
            {
                if (identity.Company != null)
                    return identity.Sprite == "corp_card_" + identity.Company && resources.Contains(ArtResource.Name(identity.Company, "card"));
                return identity.Sprite != null && identity.Sprite.StartsWith("corp_med_", StringComparison.Ordinal)
                    && (bool)artLookup.Invoke(null, new object[] { identity.Sprite.Substring("corp_med_".Length), null, null, null });
            }
            errors.AddRange(ContentContract.ValidateSpritePaths(identities, ResolveSprite));
            if (ResolveSprite(new("invalid", "invalid", null, "corp_med_\0missing"))) errors.Add("production art lookup accepts unknown sprite");
            foreach (var key in extras.Select(e => e.Key))
                if (!ResolveSprite(new("state", key, null, "corp_med_" + key))) errors.Add("production art lookup rejects explicit state: " + key);
            foreach (var (key, def) in definitions) CheckLookup(key, Text(def, "Company"), Text(def, "Art"));
            foreach (var (key, extra) in extras) CheckLookup(key, Text(extra, "Item1"), Text(extra, "Item2"));
            foreach (var (key, def) in definitions.Where(d => Field(d.Value, "Power") != null))
                foreach (var suffix in new[] { "_half", "_empty" })
                    CheckLookup(key + suffix, Text(def, "Company"), Text(def, "Art") + suffix);
            foreach (var identity in identities.Where(i => i.Company != null))
                if (companyType.GetMethod("Find").Invoke(null, new object[] { identity.Company }) == null)
                    errors.Add($"missing company factory: {identity.Id}/{identity.Company}");
            foreach (var (key, extra) in extras)
            {
                var name = ArtResource.Name(Text(extra, "Item1"), Text(extra, "Item2"));
                if (!resources.Contains(name)) errors.Add($"missing extra-state resource: {key}: {name}");
            }
            if (errors.Count != 0) throw new Exception(string.Join(Environment.NewLine, errors));
            Console.WriteLine($"COMPILED CONTENT PASSED: {rows.Count} definitions, {identities.Count} identities, {extras.Length} explicit state images; no factories invoked.");
        }
        finally { AssemblyLoadContext.Default.Resolving -= Resolve; }
    }
}
