using CorpPresence.ItemAuthoring;

internal static class PlannerChecks
{
    internal static void Run(Action<string,Action> check, string valid)
    {
        string repo=Environment.CurrentDirectory;
        string root=Path.GetFullPath(Path.Combine(repo,"artifacts","item-authoring-fixtures","planner-"+Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        try
        {
            foreach(string source in Directory.EnumerateFiles(Path.Combine(repo,"src"),"*",SearchOption.AllDirectories)
                .Where(p=>p.EndsWith(".cs") || p.EndsWith(".json") || p.EndsWith("CorpPresence.art.pastel.pastel_gloves.png"))
                .Concat(Directory.EnumerateFiles(Path.Combine(repo,"Localization"),"*.csv"))
                .Append(Path.Combine(repo,"tools/checks/LocalizationChecks/Migration.json")))
            {
                string dest=Path.Combine(root,Path.GetRelativePath(repo,source));
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!); File.Copy(source,dest);
            }
            var spec=Spec.Read(valid.Replace("src/Assets/pastel_gloves.png","src/Assets/CorpPresence.art.pastel.pastel_gloves.png"));
            check("same-file key outside editable node is rejected",()=> {
                string path=Path.Combine(root,"src/CorpPresence/Items/Goods.Definitions.cs");
                string original=File.ReadAllText(path);
                File.WriteAllText(path,original.Replace("internal static class Catalog","private const string ReservedAuthoringKey = \"rogue_sample\";\n    internal static class Catalog"));
                try {Planner.Create(root,spec with {Key="rogue_sample",Id="corp_presence_rogue_sample"});}
                catch(AuthoringException e) when(e.Code=="REGISTRATION_CONFLICT") {File.WriteAllText(path,original);return;}
                throw new Exception("Same-file reservation ignored");
            });
            check("ordinary preview plans all nine registration files",()=> {
                var plan=Planner.Create(root,spec);
                if(plan.Changes.Count!=9) throw new Exception("Expected four C#, two JSON and three CSV files");
                if(File.ReadAllText(Path.Combine(root,"src/CorpPresence/Items/Goods.Definitions.cs")).Contains("authoring_sample"))
                    throw new Exception("Preview changed source");
            });
            check("ordinary apply is idempotent across complete registration",()=> {
                Planner.Create(root,spec).Apply();
                if(Planner.Create(root,spec).Changes.Count!=0) throw new Exception("Repeated registration");
            });
            check("conflicting registered item fails before writes",()=> {
                try {Planner.Create(root,spec with {Value=200});}
                catch(AuthoringException e) when(e.Code=="REGISTRATION_CONFLICT") {return;}
                throw new Exception("Conflict accepted");
            });
            check("missing image rejected",()=> {
                try {Planner.Create(root,spec with {ArtPath="src/Assets/missing.png"});}
                catch(AuthoringException e) when(e.Code=="MISSING_ART") {return;}
                throw new Exception("Missing image accepted");
            });
        }
        finally
        {
            string allowed=Path.GetFullPath(Path.Combine(repo,"artifacts","item-authoring-fixtures"))+Path.DirectorySeparatorChar;
            if(!root.StartsWith(allowed,StringComparison.OrdinalIgnoreCase)) throw new Exception("Unsafe cleanup");
            Directory.Delete(root,true);
        }
    }
}
