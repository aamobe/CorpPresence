using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CorpPresence.ItemAuthoring;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var options=new Dictionary<string,string>(StringComparer.Ordinal);
            for(int i=0;i<args.Length;i+=2)
            {
                if(i+1==args.Length || !new[]{"--spec","--repo","--mode","--game-managed","--dotnet","--expected-plan"}.Contains(args[i]) || !options.TryAdd(args[i],args[i+1]))
                    throw new AuthoringException("INVALID_ARGUMENT","Expected named argument pairs, no duplicates.");
            }
            if(!options.TryGetValue("--spec",out string? input)) throw new AuthoringException("INVALID_ARGUMENT","--spec required");
            var spec=Spec.Read(File.ReadAllText(input)); // Liquid rejection precedes target access in every mode.
            string mode=options.GetValueOrDefault("--mode","preview");
            if(!new[]{"preview","apply","check"}.Contains(mode)) throw new AuthoringException("INVALID_ARGUMENT","Unknown mode");
            if(spec.Kind!="ordinary")
            {
                Console.WriteLine(JsonSerializer.Serialize(new {status="REFERENCE_ONLY",spec.Kind,
                    reference="docs/maintenance/item-authoring/"+spec.Kind+".md",
                    unimplemented=spec.Requirements, checks="Review state ownership, persistence, failure behavior; no generated runtime implementation."},new JsonSerializerOptions{WriteIndented=true}));
                return mode=="preview"?0:3;
            }
            string repo=Path.GetFullPath(options.GetValueOrDefault("--repo",Environment.CurrentDirectory));
            var plan=Planner.Create(repo,spec);
            string hash=Fingerprint(plan);
            if(mode=="check")
            {
                if(plan.Changes.Count!=0) throw new AuthoringException("NOT_APPLIED","Apply and review the complete registration before Check.");
                if(!options.TryGetValue("--game-managed",out string? managed)) throw new AuthoringException("INVALID_ARGUMENT","--game-managed required for compiled checks");
                RunChecks(repo,managed,options.GetValueOrDefault("--dotnet","dotnet"));
                Console.WriteLine("CODE_INTEGRATION_CHECKS_PASSED — 代码接入检查通过；不代表游戏可用。发布前仍需人工审查身份基线，工具不会修改基线。");
                return 0;
            }
            Console.WriteLine($"PLAN {hash}; files={plan.Changes.Count}; no game validation");
            foreach(var change in plan.Changes)
            {
                Console.WriteLine($"FILE {change.RelativePath}: {change.Reason}");
                ShowDelta(change);
            }
            Console.WriteLine("TRANSLATION: omitted ru/fr explicitly fall back to English. Existing identity baseline remains read-only.");
            if(mode=="apply")
            {
                if(options.GetValueOrDefault("--expected-plan")!=hash) throw new AuthoringException("PLAN_CHANGED","Pass --expected-plan from the reviewed Preview; re-preview if any input or target changed.");
                string? backup=plan.Apply();
                Console.WriteLine("APPLIED_NOT_CHECKED; backup="+(backup??"none (already applied)")+"; run Check next.");
            }
            return 0;
        }
        catch(AuthoringException e) {Console.Error.WriteLine(e.Code+": "+e.Message);return 2;}
        catch(Exception e) {Console.Error.WriteLine("AUTHORING_FAILED: "+e.Message);return 2;}
    }
    private static string Fingerprint(ChangePlan plan)
    {
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach(var change in plan.Changes)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(change.RelativePath));
            hash.AppendData(SHA256.HashData(change.Before)); hash.AppendData(SHA256.HashData(change.After));
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    private static void ShowDelta(FileChange change)
    {
        var before=Encoding.UTF8.GetString(change.Before).Replace("\r\n","\n").Split('\n');
        var after=Encoding.UTF8.GetString(change.After).Replace("\r\n","\n").Split('\n');
        int start=0; while(start<before.Length && start<after.Length && before[start]==after[start]) start++;
        int tail=0; while(tail<before.Length-start && tail<after.Length-start && before[^(tail+1)]==after[^(tail+1)]) tail++;
        Console.WriteLine($"@@ line {start+1} @@");
        foreach(string line in before.Skip(start).Take(before.Length-start-tail)) Console.WriteLine("-"+line);
        foreach(string line in after.Skip(start).Take(after.Length-start-tail)) Console.WriteLine("+"+line);
    }
    private static void RunChecks(string repo,string managed,string dotnet)
    {
        Run(repo,"pwsh","-NoProfile","-File","tools/Build.ps1","-GameManaged",managed,"-Dotnet",dotnet);
        string dll=Path.Combine(repo,"artifacts/build/bin/CorpPresence/release/CorpPresence.dll");
        foreach(string name in new[]{"ItemDefinitionChecks","ItemChecks"})
        {
            string output=Path.Combine(repo,"artifacts/item-authoring-check/"+name);
            Run(repo,dotnet,"build","tools/checks/"+name+"/Checks.csproj","--artifacts-path",output);
            string runner=Path.Combine(output,"bin/Checks/debug/Checks.dll");
            if(name=="ItemDefinitionChecks") Run(repo,dotnet,runner,dll,managed);
            else Run(repo,dotnet,runner,dll);
        }
        Run(repo,"pwsh","-NoProfile","-File","tools/Check-Localization.ps1","-GameManaged",managed,"-Dotnet",dotnet,"-CompiledAssembly",dll);
    }
    private static void Run(string repo,string executable,params string[] arguments)
    {
        var info=new ProcessStartInfo(executable){WorkingDirectory=repo,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        info.Environment["UseSharedCompilation"]="false";
        foreach(var argument in arguments) info.ArgumentList.Add(argument);
        using var process=Process.Start(info)??throw new AuthoringException("CHECK_FAILED","Cannot start "+executable);
        var output=process.StandardOutput.ReadToEndAsync();
        var error=process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Console.Write(output.GetAwaiter().GetResult());
        Console.Error.Write(error.GetAwaiter().GetResult());
        if(process.ExitCode!=0) throw new AuthoringException("CHECK_FAILED",executable+" exited "+process.ExitCode);
    }
}
