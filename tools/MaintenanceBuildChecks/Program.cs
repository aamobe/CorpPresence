using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
if(args.Length!=2)throw new Exception("Expected regular and maintenance assemblies");
for(int i=0;i<2;i++){
 using var stream=File.OpenRead(args[i]);using var pe=new PEReader(stream);var md=pe.GetMetadataReader();
 var names=md.TypeDefinitions.Select(h=>md.GetString(md.GetTypeDefinition(h).Name)).ToHashSet();
 bool expected=i==1;
 if(names.Contains("MaintenanceConsole")!=expected)throw new Exception("MaintenanceConsole boundary failed");
 if(names.Contains("LocalTestFunds") || names.Contains("WaterTrace"))throw new Exception("Unexpected debug type");
 var main=md.TypeDefinitions.Select(h=>md.GetTypeDefinition(h)).Single(t=>md.GetString(t.Name)=="Main");
 var version=main.GetFields().Select(h=>md.GetFieldDefinition(h)).Single(f=>md.GetString(f.Name)=="Version");
 string text=Encoding.Unicode.GetString(md.GetBlobBytes(md.GetConstant(version.GetDefaultValue()).Value));
 if(text.EndsWith("-maintenance")!=expected)throw new Exception("Version identity mismatch");
 Console.WriteLine("PASS "+(expected?"maintenance":"regular")+" types and version "+text);
}
