using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

if (args.Length != 3) throw new ArgumentException("Usage: Checks <GameManaged> <repository> <CSV folder>");
var managed = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (_, name) => {
    string path = Path.Combine(managed, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
var game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(managed, "Assembly-CSharp.dll"));
var parser = game.GetType("ModLoader")!.GetMethod("ParseCsvValue", BindingFlags.NonPublic | BindingFlags.Static)!;
string Parse(string raw) => (string)parser.Invoke(null, new object[] { raw })!;
var english = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(args[1], "tools/LocalizationChecks/Migration.json")))!;
var builtIn = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(Path.Combine(args[1], "src/CorpPresence/Presentation/Translations.Keys.json")))!;
foreach (var (locale, index) in new[] { ("en", -1), ("ru", 0), ("fr", 1) }) {
    var actual = new Dictionary<string, string>();
    foreach (string line in File.ReadAllLines(Path.Combine(args[2], locale + ".csv"))) {
        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
        int separator = line.IndexOf(',');
        if (separator < 1) throw new Exception("Invalid CSV row");
        string key = line[..separator].Trim();
        if (!actual.TryAdd(key, Parse(line[(separator + 1)..]))) throw new Exception("Duplicate key: " + key);
    }
    if (actual.Count != english.Count) throw new Exception("Wrong CSV key count: " + locale);
    foreach (var entry in english) {
        string expected = entry.Value;
        if (index >= 0 && builtIn.TryGetValue(entry.Key, out var row) && row?.Length > index && !string.IsNullOrEmpty(row[index])) expected = row[index];
        if (!actual.TryGetValue(entry.Key, out var value) || value != expected) throw new Exception("CSV round-trip mismatch: " + locale + "/" + entry.Key);
    }
    Console.WriteLine($"PASS {locale}: {actual.Count} entries exactly match existing text through the installed game's CSV value parser");
}
if (Parse("\" leading, \"\"quoted\"\"\\ntrailing \"") != " leading, \"quoted\"\ntrailing ") throw new Exception("Official CSV escaping mismatch");
Console.WriteLine("PASS official parser preserves commas, quotes, line breaks and surrounding spaces");
