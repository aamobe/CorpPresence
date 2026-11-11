using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
if (args.Length != 2) throw new Exception("Expected assembly path and exact expected development suffix");
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
var expected = Constant("ReleaseVersion") + args[1];
if (version != expected) throw new Exception($"Expected development identity {expected}; got {version}");
if (Attribute("AssemblyInformationalVersionAttribute") != version) throw new Exception("Informational version differs from runtime version");
if (Attribute("AssemblyFileVersionAttribute") != "1.1.0.0") throw new Exception("Historical file version changed");
Console.WriteLine("PASS runtime constant / informational version: " + version + "; historical file version preserved");
