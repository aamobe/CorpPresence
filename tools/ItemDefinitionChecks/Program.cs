using CorpPresence;

var count = 0;
void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; Console.WriteLine("PASS " + message); }
void Reject(Action action, string message)
{
    try { action(); } catch (ArgumentException) { Check(true, message); return; }
    throw new Exception("Expected rejection: " + message);
}
var set = new DefinitionSet<object>();
Reject(() => { var duplicated = new DefinitionSet<object>("initializer") { ["same"] = new(), ["same"] = new() }; }, "duplicate initializer rejected before aggregation");
var first = new object();
set.Add("ordinary", "one", first);
Reject(() => set.Add("machine", "one", new object()), "duplicate definition");
Check(ReferenceEquals(set.Values["one"], first), "duplicate preserves original value");
Reject(() => set.AddRange("batch", new Dictionary<string, object> { ["two"] = new(), ["one"] = new() }), "batch conflict");
Check(!set.Values.ContainsKey("two"), "batch conflict does not partly install");
Reject(() => set.Add("", "two", new()), "empty source");
Reject(() => set.Add("source", " ", new()), "blank key");
Reject(() => set.Add("source", "two", null), "null definition");
set.Add("machine", "two", new());
Check(set.Values.Keys.SequenceEqual(new[] { "one", "two" }), "insertion order retained");
Check(set.Sources["two"] == "machine", "definition source retained");
Reject(() => set.AddRange("batch", new[] { KeyValuePair.Create("three", new object()), KeyValuePair.Create("three", new object()) }), "duplicate inside batch");
Check(!set.Values.ContainsKey("three"), "duplicate batch is atomic");
Console.WriteLine($"DEFINITION CHECKS PASSED: {count}");
var row = new ContentRow("one", "ordinary", new[] { "name" }, new[] { "one.png" });
var identity = new FactoryRow("corp_presence_one", "one", null);
var texts = new HashSet<string> { "name" };
var resources = new HashSet<string> { "one.png" };
List<string> Validate(ContentRow[] rows, FactoryRow[] ids, HashSet<string> t = null, HashSet<string> r = null) =>
    ContentContract.Validate(rows, ids, t ?? texts, r ?? resources);
Check(Validate(new[] { row }, new[] { identity }).Count == 0, "valid content association");
Check(Validate(new[] { row }, new[] { identity with { Goods = "missing" } }).Any(e => e.Contains("missing definition")), "missing factory definition rejected");
Check(Validate(new[] { row }, Array.Empty<FactoryRow>()).Any(e => e.Contains("missing identity")), "unregistered definition rejected");
Check(Validate(new[] { row }, new[] { identity }, new()).Any(e => e.Contains("missing text")), "missing text rejected");
Check(Validate(new[] { row with { TextKeys = Array.Empty<string>() } }, new[] { identity }).Any(e => e.Contains("missing text")), "missing name binding rejected");
foreach (var invalidName in new[] { null, "", " " })
    Check(Validate(new[] { row with { Name = invalidName } }, new[] { identity }).Any(e => e.Contains("missing name")), "missing display name rejected");
Check(Validate(new[] { row }, new[] { identity }, r: new()).Any(e => e.Contains("missing resource")), "missing image rejected");
Check(Validate(new[] { row }, new[] { identity, identity with { Id = "corp_presence_one_lit" }, new FactoryRow("corp_presence_card", null, "company") }).Count == 0, "shared definition and company factory permitted");
Check(Validate(new[] { row, row }, new[] { identity }).Any(e => e.Contains("duplicate")), "duplicate content rejected");
Check(Validate(new[] { row }, new[] { identity with { Id = "corp_presence_different" } }).Any(e => e.Contains("unreachable")), "definition must be reachable by Goods.Make naming");
Check(ContentContract.ValidateSpritePaths(new[] { identity with { Sprite = "corp_med_typo" } }, i => i.Sprite == "corp_med_one").Count == 1, "unresolved identity sprite rejected");
Reject(() => ContentContract.RequiredResources("Embedded", null, "image", "vanilla", false), "embedded requires resource coordinates despite fallback");
Reject(() => ContentContract.RequiredResources("NativeSprite", null, null, null, false), "native art requires named fallback");
Reject(() => ContentContract.RequiredResources("unknown", "x", "y", "native", false), "unknown art mode rejected");
Check(ContentContract.RequiredResources("NativeSprite", null, null, "book2", false).Length == 0, "explicit native sprite has no embedded image");
Check(ContentContract.RequiredResources("TintedNativeSprite", "malice", "coin", "the_coin", false).Length == 0, "tinted native sprite is explicit");
Check(ContentContract.RequiredResources("Embedded", "a", "b", null, true).SequenceEqual(new[] { "CorpPresence.art.a.b.png", "CorpPresence.art.a.b_half.png", "CorpPresence.art.a.b_empty.png" }), "battery variants use real naming convention");
if (args.Length == 2) CompiledContent.Check(args[0], args[1]);
else if (args.Length != 0) throw new ArgumentException("Expected compiled DLL and Managed directory");
Console.WriteLine($"ALL DEFINITION CHECKS PASSED: {count}");
