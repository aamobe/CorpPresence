using System.Diagnostics;
using System.Security.Cryptography;
using CorpPresence.ItemAuthoring;

internal static class CliChecks
{
    internal static void Run(Action<string,Action> check)
    {
        string root=Path.GetFullPath(Path.Combine(Environment.CurrentDirectory,"artifacts","item-authoring-fixtures","cli-"+Guid.NewGuid().ToString("N")));
        string target=Path.Combine(root,"target"); Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target,"user.txt"),"untouched user data");
        try
        {
            string[] inputs = [
                "{\"kind\":\"liquid\"}",
                "{\"kind\":\"liquid-container\"}",
                "{\"kind\":\"ordinary\",\"types\":[\"WATER\"]}",
                "{\"kind\":\"plant\",\"requirements\":{\"behavior\":\"新增花浆液体\"}}",
                "{\"kind\":\"machine\",\"extra\":{\"init\":\"InitLiquidContainerItem\"}}",
                "{\"kind\":\"ordinary\",\"tags\":[\"CURRENT_PART_WATER\"]}",
                "{\"kind\":\"ordinary\",\"ingredients\":[]}",
                "{\"kind\":\"ordinary\",\"unknown\":{\"items\":[{\"behavior\":\"独立调酒\"}]}}"
            ];
            foreach(string mode in new[]{"preview","apply","check"})
                for(int i=0;i<inputs.Length;i++)
                {
                    int index=i;
                    check($"CLI {mode} refuses liquid case {index} without touching target",()=> {
                        var before=Snapshot(target);
                        string spec=Path.Combine(root,"spec.json");File.WriteAllText(spec,inputs[index]);
                        var result=RunTool("--spec",spec,"--repo",target,"--mode",mode);
                        if(result.Code==0 || !result.Output.Contains(Spec.LiquidCode) || Snapshot(target)!=before)
                            throw new Exception("Liquid refusal contract failed: "+result.Output);
                    });
                }
            foreach(string kind in Spec.Kinds.Where(k=>k!="ordinary"))
                check("reference example "+kind+" previews only and cannot apply/check",()=> {
                    string spec=Path.GetFullPath("tools/ItemAuthoring/examples/"+kind+".json");
                    if(!File.Exists("docs/maintenance/item-authoring/"+kind+".md")) throw new Exception("Missing category reference");
                    var before=Snapshot(target);
                    var preview=RunTool("--spec",spec,"--repo",target,"--mode","preview");
                    if(preview.Code!=0 || !preview.Output.Contains("REFERENCE_ONLY")) throw new Exception(preview.Output);
                    foreach(string mode in new[]{"apply","check"})
                        if(RunTool("--spec",spec,"--repo",target,"--mode",mode).Code==0) throw new Exception("Complex implementation accepted");
                    if(Snapshot(target)!=before) throw new Exception("Reference request wrote target");
                });
        }
        finally
        {
            string allowed=Path.GetFullPath(Path.Combine(Environment.CurrentDirectory,"artifacts","item-authoring-fixtures"))+Path.DirectorySeparatorChar;
            if(!root.StartsWith(allowed,StringComparison.OrdinalIgnoreCase)) throw new Exception("Unsafe cleanup");
            Directory.Delete(root,true);
        }
    }
    internal static (int Code,string Output) RunTool(params string[] args)
    {
        var info=new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")??"dotnet")
        {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
        info.ArgumentList.Add(typeof(Spec).Assembly.Location);
        foreach(string arg in args) info.ArgumentList.Add(arg);
        using var process=Process.Start(info)!;
        var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        process.WaitForExit();return(process.ExitCode,stdout.GetAwaiter().GetResult()+stderr.GetAwaiter().GetResult());
    }
    private static string Snapshot(string root) => string.Join("\n",Directory.EnumerateFileSystemEntries(root,"*",SearchOption.AllDirectories).Order()
        .Select(p=>Path.GetRelativePath(root,p)+":"+(File.Exists(p)?Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))):"dir")));
}
