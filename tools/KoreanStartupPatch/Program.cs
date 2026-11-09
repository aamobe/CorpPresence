using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using DseCecil;
using DseCecil.Cil;
using CecilCode = DseCecil.Cil.Code;

if (args.Length != 3) throw new ArgumentException("Usage: Checks <16:22 base DLL> <output DLL> <GameManaged>");
string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data));
if (Hash(File.ReadAllBytes(args[0])) != "855CF8D493E3B8DC74E584449208DCE457ADE86C4159003873D9AD609F35A744") throw new Exception("Unexpected subscription baseline; refusing to patch");
if (string.Equals(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)) throw new Exception("Keep the original baseline intact");
using var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(args[2]);
resolver.AddSearchDirectory(Path.Combine(args[2], "Facades"));
using var assembly = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters { AssemblyResolver = resolver });
const string targetName = "System.String CorpPresence.GameResources::ResolveMenuStartupLocale()";
var methods = assembly.MainModule.GetTypes().SelectMany(t => t.Methods).ToDictionary(m => m.FullName);
var target = methods[targetName];
string MethodFingerprint(MethodDefinition method)
{
    if (!method.HasBody) return "no-body";
    var instructions = method.Body.Instructions;
    string Operand(object value) => value switch {
        null => "", Instruction branch => "branch:" + instructions.IndexOf(branch),
        Instruction[] branches => "switch:" + string.Join(",", branches.Select(i => instructions.IndexOf(i))),
        VariableDefinition local => "local:" + local.Index,
        ParameterDefinition parameter => "arg:" + parameter.Index,
        MemberReference member => member.FullName,
        string text => Convert.ToBase64String(Encoding.UTF8.GetBytes(text)),
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!
    };
    return string.Join("\n", method.Body.Variables.Select(v => v.VariableType.FullName)) + "\n" +
        string.Join("\n", instructions.Select(i => i.OpCode.Code + ":" + Operand(i.Operand)));
}
var before = methods.ToDictionary(p => p.Key, p => MethodFingerprint(p.Value));
var resources = assembly.MainModule.Resources.OfType<EmbeddedResource>().ToDictionary(r => r.Name, r => Hash(r.GetResourceData()));
Func<string, string> ExecuteMapping(MethodDefinition method)
{
    var dm = new DynamicMethod("LocaleMappingProbe", typeof(string), new[] { typeof(string) });
    var il = dm.GetILGenerator();
    var locals = method.Body.Variables.Select(v => il.DeclareLocal(v.VariableType.FullName switch {
        "System.String" => typeof(string), "System.Int32" => typeof(int), "System.Char" => typeof(char), _ => throw new Exception("Unexpected local")
    })).ToArray();
    var labels = method.Body.Instructions.ToDictionary(i => i, _ => il.DefineLabel());
    var opcodes = typeof(System.Reflection.Emit.OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public)
        .Select(f => (System.Reflection.Emit.OpCode)f.GetValue(null)!).ToDictionary(o => o.Name!);
    foreach (var instruction in method.Body.Instructions) {
        il.MarkLabel(labels[instruction]);
        if (instruction.Operand is MethodReference call && call.DeclaringType.FullName == "LocHelper" && call.Name == "ResolveStartupLocale") { il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); continue; }
        var opcode = opcodes[instruction.OpCode.Name];
        switch (instruction.Operand) {
            case null: il.Emit(opcode); break;
            case Instruction branch: il.Emit(opcode, labels[branch]); break;
            case Instruction[] branches: il.Emit(opcode, branches.Select(i => labels[i]).ToArray()); break;
            case VariableDefinition local: il.Emit(opcode, locals[local.Index]); break;
            case string text: il.Emit(opcode, text); break;
            case sbyte number: il.Emit(opcode, number); break;
            case int number: il.Emit(opcode, number); break;
            case MethodReference member when member.DeclaringType.FullName == "System.String":
                Type[] parameters = member.Parameters.Select(p => p.ParameterType.FullName == "System.String" ? typeof(string) : typeof(int)).ToArray();
                il.Emit(opcode, typeof(string).GetMethod(member.Name, parameters)!); break;
            default: throw new Exception("Unsupported mapping instruction: " + instruction);
        }
    }
    return (Func<string, string>)dm.CreateDelegate(typeof(Func<string, string>));
}
var original = ExecuteMapping(target);
if (original("ko") != "en") throw new Exception("Baseline does not reproduce reported Korean startup bug");
var body = target.Body;
var first = body.Instructions[0];
if (first.Operand is not MethodReference startup || startup.Name != "ResolveStartupLocale") throw new Exception("Unexpected mapping body");
var store = body.Instructions[1];
if (store.OpCode.Code != CecilCode.Stloc_1) throw new Exception("Unexpected locale local");
var resume = body.Instructions[2];
var equality = (MethodReference)body.Instructions.First(i => i.Operand is MethodReference m && m.DeclaringType.FullName == "System.String" && m.Name == "op_Equality").Operand;
var processor = body.GetILProcessor();
foreach (var instruction in new[] {
    Instruction.Create(DseCecil.Cil.OpCodes.Ldloc_1), Instruction.Create(DseCecil.Cil.OpCodes.Ldstr, "ko"),
    Instruction.Create(DseCecil.Cil.OpCodes.Call, equality), Instruction.Create(DseCecil.Cil.OpCodes.Brfalse, resume),
    Instruction.Create(DseCecil.Cil.OpCodes.Ldstr, "ko"), Instruction.Create(DseCecil.Cil.OpCodes.Ret)
}) processor.InsertBefore(resume, instruction);
var patched = ExecuteMapping(target);
foreach (string code in new[] { "en", "zh", "ru", "de", "ja", "fr", "es", "pl", "ko", "unknown", "", "fr-CA" }) {
    string expected = code == "ko" ? "ko" : original(code);
    if (patched(code) != expected) throw new Exception("Mapping regression: " + code);
}
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
assembly.Write(args[1]);
using var output = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { AssemblyResolver = resolver });
var after = output.MainModule.GetTypes().SelectMany(t => t.Methods).ToDictionary(m => m.FullName);
if (after.Count != before.Count) throw new Exception("Method set changed");
var changed = before.Keys.Where(key => !after.ContainsKey(key) || MethodFingerprint(after[key]) != before[key]).ToArray();
if (changed.Length != 1 || changed[0] != targetName) throw new Exception("Unexpected method changes: " + string.Join(", ", changed));
var afterResources = output.MainModule.Resources.OfType<EmbeddedResource>().ToDictionary(r => r.Name, r => Hash(r.GetResourceData()));
if (resources.Count != afterResources.Count || resources.Any(p => !afterResources.TryGetValue(p.Key, out var hash) || hash != p.Value)) throw new Exception("Resource bytes changed");
if (ExecuteMapping(after[targetName])("ko") != "ko") throw new Exception("Written DLL lost Korean fix");
Console.WriteLine($"PASS only Korean startup mapping changed; {after.Count - 1} other methods and {resources.Count} embedded resources unchanged");
Console.WriteLine("PASS 12 locale cases retain baseline behavior; ko now maps to ko");
Console.WriteLine("Output SHA256: " + Hash(File.ReadAllBytes(args[1])));
