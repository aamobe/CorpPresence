using Mono.Cecil;
var game = args[0];
using var assembly = AssemblyDefinition.ReadAssembly(game);
var types = new HashSet<string>(args.Skip(1));
foreach (var t in assembly.MainModule.Types.Where(t => types.Contains(t.Name)))
{
    Console.WriteLine("TYPE " + t.FullName);
    foreach (var f in t.Fields) Console.WriteLine($"FIELD {f.FieldType} {f.Name} {(f.HasConstant ? f.Constant : "")}");
    foreach (var m in t.Methods)
    {
        Console.WriteLine("METHOD " + m.FullName);
        if (!m.HasBody) continue;
        foreach (var i in m.Body.Instructions) Console.WriteLine("  " + i);
    }
}
