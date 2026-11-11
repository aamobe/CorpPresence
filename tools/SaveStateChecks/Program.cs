using CorpPresence;

int passed = 0;
foreach (string raw in new[]
{
    "{", "null", """{"RunId":"test","Companies":null}""",
    """{"RunId":"test","Characters":{"rosen":null}}""",
    """{"RunId":"test","Companies":{"pastel":{"Orders":[{"Items":null}]}}}"""
})
{
    PlayerStore.Instance = new PlayerStore();
    PlayerStore.Instance.modData["CorpPresence"] = raw;
    CorpState.Current = null;
    CorpState.Get().CompanyOf("pastel").Coop = 42;
    CorpState.Save();
    if (PlayerStore.Instance.modData["CorpPresence"] != raw)
        throw new Exception("Unreadable record was overwritten");
    passed++;
}
PlayerStore.Instance = new PlayerStore();
PlayerStore.Instance.modData["CorpPresence"] = """{"RunId":"test","Seline":{"Chapter":3}}""";
CorpState.Current = null;
var state = CorpState.Get();
if (state.Of("seline").Chapter != 3) throw new Exception("Legacy progress lost");
state.CompanyOf("pastel").Orders.Add(new Order());
CorpState.Save();
CorpState.Current = null;
state = CorpState.Get();
if (state.Of("seline").Chapter != 3 || state.CompanyOf("pastel").Orders.Count != 1)
    throw new Exception("Save roundtrip lost progress");
passed++;
Console.WriteLine($"{passed} passed: malformed/null records protected; legacy progress and orders roundtrip");


var blueprint = new StoreClient();
PlayerStore.Instance.currentClientInstance = new Client { Blueprint = blueprint };
var visit = new CharacterVisitSession("seline", state.Of("seline"), blueprint);
if (!visit.IsCurrent || !visit.TryFinish() || visit.TryFinish())
    throw new Exception("Visit completion was not exactly once");
var replacementGuard = new CharacterVisitSession("seline", state.Of("seline"), blueprint);
PlayerStore.Instance.currentClientInstance = new Client { Blueprint = new StoreClient() };
if (replacementGuard.IsCurrent || replacementGuard.TryFinish())
    throw new Exception("Old client callback remained valid");
PlayerStore.Instance.currentClientInstance.Blueprint = blueprint;
var reloadGuard = new CharacterVisitSession("seline", state.Of("seline"), blueprint);
PlayerStore.Instance.modData = new Dictionary<string, string>(PlayerStore.Instance.modData);
if (reloadGuard.IsCurrent) throw new Exception("Reload retained old visit");
CorpState.Get();
var generationGuard = new CharacterVisitSession("seline", CorpState.Current.Of("seline"), blueprint);
GameResources.Generation++;
if (generationGuard.IsCurrent) throw new Exception("Runtime generation retained old visit");
Console.WriteLine("4 passed: completion once; replaced customer, save dictionary and runtime reject stale visits");
