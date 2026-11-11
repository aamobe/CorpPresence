using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
bool release = args.Length == 4 && args[1] == "--release";
if (args.Length != 2 && !release) throw new Exception("Expected assembly path and development suffix, or --release version revision");
using var stream = File.OpenRead(args[0]);
using var pe = new PEReader(stream);
var md = pe.GetMetadataReader();
var main = md.TypeDefinitions.Select(h => md.GetTypeDefinition(h)).Single(t => md.GetString(t.Name) == "Main");
string Constant(string name) {
    var field = main.GetFields().Select(h => md.GetFieldDefinition(h)).Single(f => md.GetString(f.Name) == name);
    return Encoding.Unicode.GetString(md.GetBlobBytes(md.GetConstant(field.GetDefaultValue()).Value));
}
string Attribute(string name) {
    foreach (var handle in md.GetAssemblyDefinition().GetCustomAttributes()) {
        var attribute = md.GetCustomAttribute(handle);
        if (attribute.Constructor.Kind != HandleKind.MemberReference) continue;
        var member = md.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
        if (member.Parent.Kind != HandleKind.TypeReference) continue;
        if (md.GetString(md.GetTypeReference((TypeReferenceHandle)member.Parent).Name) != name) continue;
        var blob = md.GetBlobReader(attribute.Value);
        if (blob.ReadUInt16() != 1) throw new Exception("Invalid attribute blob");
        return blob.ReadSerializedString();
    }
    throw new Exception("Missing " + name);
}
var version = Constant("Version");
var expected = release ? args[2] : Constant("ReleaseVersion") + args[1];
if (version != expected) throw new Exception($"Expected identity {expected}; got {version}");
if (Attribute("AssemblyInformationalVersionAttribute") != version) throw new Exception("Informational version differs from runtime version");
if (Attribute("AssemblyFileVersionAttribute") != Constant("ReleaseVersion") + ".0") throw new Exception("File version differs from release baseline");
if (release) {
    if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+$")) throw new Exception("Not a release version");
    var forbidden = new[] { "MaintenanceConsole", "LocalTestFunds", "WaterTrace", "TestKit" };
    if (md.TypeDefinitions.Any(h => forbidden.Contains(md.GetString(md.GetTypeDefinition(h).Name)))) throw new Exception("Release contains development-only types");
    bool revisionMatches = false;
    foreach (var handle in md.GetAssemblyDefinition().GetCustomAttributes()) {
        var a = md.GetCustomAttribute(handle);
        if (a.Constructor.Kind != HandleKind.MemberReference) continue;
        var member = md.GetMemberReference((MemberReferenceHandle)a.Constructor);
        if (member.Parent.Kind != HandleKind.TypeReference || md.GetString(md.GetTypeReference((TypeReferenceHandle)member.Parent).Name) != "AssemblyMetadataAttribute") continue;
        var blob = md.GetBlobReader(a.Value);
        if (blob.ReadUInt16() != 1) throw new Exception("Invalid metadata attribute");
        if (blob.ReadSerializedString() == "SourceRevision") revisionMatches = blob.ReadSerializedString() == args[3];
    }
    if (!revisionMatches) throw new Exception("Release source revision mismatch");
}
Console.WriteLine("PASS runtime / informational / file version: " + version + (release ? "; release revision verified" : ""));
