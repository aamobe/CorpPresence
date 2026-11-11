using System.Text.Json.Nodes;
using CorpPresence.ItemAuthoring;

var valid = """
{
  "schemaVersion": 1,
  "kind": "ordinary",
  "key": "authoring_sample",
  "identity": {"id":"corp_presence_authoring_sample", "base":"toothpaste"},
  "names": {"zh":"测试\"商品", "en":"Sample\nitem"},
  "description": {"zh":"仅用于工具演练", "en":"Authoring exercise only"},
  "art": {"path":"src/Assets/pastel_gloves.png"},
  "shape": {"width":2,"height":2,"cells":"##|##"},
  "value": 100,
  "types": ["HOUSEHOLD_GOOD"],
  "acquisition": {"company":"pastel", "page":"日用", "minTier":1}
}
""";
int passed=0;
void Check(string name, Action action) { action(); Console.WriteLine("PASS " + name); passed++; }
void Reject(string name, string code, Action<JsonObject> edit) => Check(name, () => {
    var node=JsonNode.Parse(valid)!.AsObject(); edit(node);
    try { Spec.Read(node.ToJsonString()); }
    catch (AuthoringException e) when(e.Code==code) { return; }
    throw new Exception("Expected " + code);
});
Check("ordinary input preserves quoted and multiline text", () => {
    var spec=Spec.Read(valid);
    if(spec.Key!="authoring_sample" || spec.NameEn!="Sample\nitem") throw new Exception("Wrong parsing");
});
Reject("unknown field rejected", "INVALID_SPEC", n=>n["code"]="Console.WriteLine(1)");
Reject("unknown category never falls back", "INVALID_SPEC", n=>n["kind"]="typo");
Reject("unknown schema version rejected", "INVALID_SPEC", n=>n["schemaVersion"]=2);
Reject("identity cannot alias released goods", "INVALID_SPEC", n=>n["identity"]!["id"]="corp_presence_gloves");
Reject("path traversal rejected", "INVALID_SPEC", n=>n["art"]!["path"]="../outside.png");
Reject("absolute path rejected", "INVALID_SPEC", n=>n["art"]!["path"]="C:/outside.png");
Reject("nonrectangular occupancy rejected", "INVALID_SPEC", n=>n["shape"]!["cells"]="##|#");
Reject("empty occupancy rejected", "INVALID_SPEC", n=>n["shape"]!["cells"]="..|..");
Reject("arbitrary base forbidden", "INVALID_SPEC", n=>n["identity"]!["base"]="custom_unknown");
foreach(var kind in new[]{"liquid", "liquid-container"})
    Reject(kind, "LIQUID_REQUIRES_SEPARATE_DEVELOPMENT", n=>n["kind"]=kind);
Reject("disguised liquid type", "LIQUID_REQUIRES_SEPARATE_DEVELOPMENT", n=>n["types"]=new JsonArray("WATER"));
Reject("unknown nested liquid capability rejected before unknown-field error", "LIQUID_REQUIRES_SEPARATE_DEVELOPMENT", n=>n["extras"]=new JsonObject { ["initialize"]="InitLiquidContainerItem" });
Reject("mixed ingredient request rejected wholesale", "LIQUID_REQUIRES_SEPARATE_DEVELOPMENT", n=>n["requirements"]=new JsonObject { ["ingredients"]=new JsonArray("water","alcohol") });
Reject("Chinese liquid design declaration", "LIQUID_REQUIRES_SEPARATE_DEVELOPMENT", n=>n["requirements"]=new JsonObject { ["behavior"]="新增调酒配方" });
Check("duplicate JSON members rejected", () => {
    try { Spec.Read(valid.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 1, \"schemaVersion\": 1")); }
    catch(AuthoringException e) when(e.Code=="INVALID_SPEC") { return; }
    throw new Exception("Accepted duplicate member");
});
TransactionChecks.Run(Check);
PlannerChecks.Run(Check,valid);
Console.WriteLine($"{passed} checks passed.");
