using System.Reflection;
using System.Runtime.Loader;
using System.Xml.Linq;

if (args.Length != 2) throw new ArgumentException("Usage: Checks <GameManaged> <mod folder>");
var managed = Path.GetFullPath(args[0]);
var folder = Path.GetFullPath(args[1]);
// Load the installed game's scanner without starting Unity or executing mod code.
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var path = Path.Combine(managed, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
var game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(managed, "Assembly-CSharp.dll"));
var manifest = XDocument.Load(Path.Combine(folder, "manifest.xml")).Root!;
var declared = manifest.Element("Capabilities")!.Elements("Capability").Select(e => e.Value.Trim()).ToArray();
var dlls = manifest.Element("Assemblies")!.Elements("Assembly").Select(e => Path.Combine(folder, e.Value.Trim())).ToArray();
var allowed = Directory.GetFiles(managed, "*.dll").Select(Path.GetFileNameWithoutExtension).Concat(dlls.Select(Path.GetFileNameWithoutExtension)).ToArray();
var result = game.GetType("ModScanner")!.GetMethod("Scan")!.Invoke(null, new object[] { dlls, allowed, folder, false })!;
var resultType = result.GetType();
var detected = ((IEnumerable<string>)resultType.GetField("Capabilities")!.GetValue(result)!).ToArray();
var capability = game.GetType("ModCapability")!;
var unknown = declared.Where(c => !(bool)capability.GetMethod("IsKnown")!.Invoke(null, new object[] { c })!).ToArray();
var missing = ((IEnumerable<string>)capability.GetMethod("Undeclared")!.Invoke(null, new object[] { detected, declared })!).ToArray();
Console.WriteLine($"Methods scanned: {resultType.GetField("MethodsScanned")!.GetValue(result)}");
Console.WriteLine($"Detected: {string.Join(", ", detected)}; declared: {string.Join(", ", declared)}");
Console.WriteLine($"Unknown declarations: {unknown.Length}; undeclared capabilities: {missing.Length}");
Console.WriteLine($"Refused: {resultType.GetProperty("Refused")!.GetValue(result)}; error: {resultType.GetField("Error")!.GetValue(result) ?? "none"}");
Console.WriteLine(resultType.GetMethod("FormatFindings")!.Invoke(result, new object[] { 12 }));
return (bool)resultType.GetProperty("Refused")!.GetValue(result)! || missing.Length > 0 || unknown.Length > 0 ? 1 : 0;
