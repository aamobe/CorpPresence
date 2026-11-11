using System.Text.Json;
using System.Text.RegularExpressions;

namespace CorpPresence.ItemAuthoring;

public sealed class AuthoringException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed record Spec(string Kind, string Key, string Id, string Base,
    IReadOnlyDictionary<string,string> Names, IReadOnlyDictionary<string,string> Description,
    string ArtPath, int Width, int Height, string Cells, long Value,
    string[] Types, string Company, string Page, int MinTier,
    IReadOnlyDictionary<string,string> Requirements)
{
    public string NameEn => Names["en"];
    public const string LiquidCode = "LIQUID_REQUIRES_SEPARATE_DEVELOPMENT";
    public const string LiquidReference = "docs/maintenance/item-authoring/liquid-systems.md";
    public static readonly string[] Kinds = ["ordinary", "consumable", "equipment", "container",
        "machine", "plant", "plant-produce", "aquaculture", "seafood-raw", "food",
        "creature", "credential", "multistate"];
    // A plain static base only. Broadening this list requires reviewing native capabilities.
    private static readonly string[] Bases = ["toothpaste"];
    private static readonly string[] AllowedTypes = ["HOUSEHOLD_GOOD", "TOOL", "LUXURY_ITEM", "CONTRABAND", "MEDICAL", "FOOD"];
    private static readonly Regex Liquid = new(
        @"liquid|water|wine|cocktail|alcohol|ingredient|current_part|fill(container|ing)?|pour|mix(ture|ing)?|distill|evaporat|液体|成分|灌装|倾倒|调酒|酒精|水质|水价|水名|溶液|配液",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static Spec Read(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            // Always scan the entire request before schema validation or any filesystem access.
            ScanLiquid(root, "");
            Object(root, "schemaVersion", "kind", "key", "identity", "names", "description", "art", "shape", "value", "types", "acquisition", "requirements");
            Require(root.GetProperty("schemaVersion").GetInt32() == 1, "Unsupported schemaVersion");
            string kind = Text(root, "kind"), key = Text(root, "key");
            Require(Kinds.Contains(kind), "Unknown kind");
            Require(key.Length is >= 3 and <= 64 && Regex.IsMatch(key, "^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$"), "Invalid goods key; empty underscore segments are not allowed");
            var identity = root.GetProperty("identity"); Object(identity, "id", "base");
            string id = Text(identity, "id"), baseId = Text(identity, "base");
            Require(id == "corp_presence_" + key, "Identity must match goods key");
            Require(Bases.Contains(baseId), "Unreviewed base; use independent development");
            var names = Localized(root.GetProperty("names"));
            var description = Localized(root.GetProperty("description"));
            var art = root.GetProperty("art"); Object(art, "path");
            string path = Text(art, "path");
            Require(!Path.IsPathRooted(path) && !path.Contains(':') && !path.Contains('\\') &&
                path.Split('/').All(p => p.Length > 0 && p is not "." and not "..") &&
                path.StartsWith("src/Assets/", StringComparison.Ordinal) && path.EndsWith(".png", StringComparison.OrdinalIgnoreCase), "Art must be a repository-relative PNG in src/Assets");
            var shape = root.GetProperty("shape"); Object(shape, "width", "height", "cells");
            int w = shape.GetProperty("width").GetInt32(), h = shape.GetProperty("height").GetInt32();
            string cells = Text(shape, "cells");
            var rows = cells.Split('|');
            Require(w is >= 1 and <= 16 && h is >= 1 and <= 16 && rows.Length == h &&
                rows.All(r => r.Length == w && r.All(c => c is '#' or '.')) && cells.Contains('#'), "Invalid occupied shape");
            long value = root.GetProperty("value").GetInt64();
            Require(value is > 0 and <= 1000000000, "Value outside supported range");
            var types = root.GetProperty("types").EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
            Require(types.Length > 0 && types.Distinct().Count() == types.Length && types.All(AllowedTypes.Contains), "Unsupported/duplicate types");
            var acquisition = root.GetProperty("acquisition"); Object(acquisition, "company", "page", "minTier");
            string company = Text(acquisition, "company"), page = Text(acquisition, "page");
            int tier = acquisition.GetProperty("minTier").GetInt32();
            Require(company == "pastel" && page == "日用" && tier is >= 1 and <= 3, "First version supports pastel 日用, tier 1–3 (actual company levels)");
            var requirements = new Dictionary<string,string>();
            if(root.TryGetProperty("requirements", out var req))
            {
                Object(req, "behavior", "states", "ownership", "persistence", "failure", "verification");
                foreach(var p in req.EnumerateObject()) requirements.Add(p.Name, Text(req,p.Name));
            }
            Require(kind != "ordinary" || requirements.Count == 0, "Ordinary goods cannot have hidden behavior requirements");
            Require(kind == "ordinary" || requirements.Count == 6, "Complex categories require all six requirement sections");
            return new(kind,key,id,baseId,names,description,path,w,h,cells,value,types,company,page,tier,requirements);
        }
        catch(AuthoringException) { throw; }
        catch(Exception e) when(e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw new AuthoringException("INVALID_SPEC", "Invalid JSON input: " + e.Message); }
    }

    private static void ScanLiquid(JsonElement node, string field)
    {
        if(node.ValueKind == JsonValueKind.Object)
        {
            foreach(var property in node.EnumerateObject())
            {
                if(Liquid.IsMatch(property.Name)) RejectLiquid();
                ScanLiquid(property.Value, property.Name);
            }
        }
        else if(node.ValueKind == JsonValueKind.Array)
            foreach(var child in node.EnumerateArray()) ScanLiquid(child, field);
        else if(node.ValueKind == JsonValueKind.String && Liquid.IsMatch(node.GetString()!)) RejectLiquid();
    }
    private static void RejectLiquid() => throw new AuthoringException(LiquidCode,
        "新液体必须走独立开发流程；工具不输出设计、配方或骨架。参考：" + LiquidReference);
    private static void Object(JsonElement node, params string[] allowed)
    {
        Require(node.ValueKind == JsonValueKind.Object, "Expected object");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach(var p in node.EnumerateObject())
            Require(seen.Add(p.Name) && allowed.Contains(p.Name), "Unknown or duplicate field: " + p.Name);
    }
    private static string Text(JsonElement node, string key)
    {
        string? value = node.GetProperty(key).GetString();
        Require(!string.IsNullOrWhiteSpace(value) && value.Length <= 4000 && !value.Contains('\0'), "Invalid text: " + key);
        return value!;
    }
    private static IReadOnlyDictionary<string,string> Localized(JsonElement node)
    {
        Object(node, "zh", "en", "ru", "fr");
        var result = new Dictionary<string,string> { ["zh"] = Text(node, "zh"), ["en"] = Text(node,"en") };
        foreach(var language in new[]{"ru","fr"})
            if(node.TryGetProperty(language, out _)) result[language] = Text(node,language);
        foreach(string language in result.Keys.ToArray())
        {
            Require(!result[language].Contains("\\n",StringComparison.Ordinal), "Literal backslash-n cannot round-trip through official CSV");
            result[language]=result[language].Replace("\r\n","\n").Replace("\r","\n");
        }
        return result;
    }
    private static void Require(bool condition, string message)
    { if(!condition) throw new AuthoringException("INVALID_SPEC", message); }
}
