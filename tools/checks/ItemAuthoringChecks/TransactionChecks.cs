using CorpPresence.ItemAuthoring;

internal static class TransactionChecks
{
    internal static void Run(Action<string,Action> check)
    {
        // Use the authorized workspace: this host can write temp files but denies replacing them.
        string testRoot=Path.GetFullPath(Path.Combine(Environment.CurrentDirectory,"artifacts","item-authoring-fixtures"));
        string root=Path.Combine(testRoot,"corp-authoring-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        // Kept outside the production checkout; no game files are used.
        try
        {
            string a=Path.Combine(root,"a.txt"), b=Path.Combine(root,"b.txt");
            File.WriteAllText(a,"user change A"); File.WriteAllText(b,"user change B");
            check("preview captures bytes without changing files",()=> {
                var plan=new ChangePlan(root); plan.Add("a.txt","generated A");
                if(File.ReadAllText(a)!="user change A" || plan.Changes.Count!=1) throw new Exception("Preview wrote target");
            });
            check("concurrent changes stop apply without overwriting",()=> {
                var plan=new ChangePlan(root); plan.Add("a.txt","generated A");
                File.WriteAllText(a,"newer user change");
                try {plan.Apply();} catch(AuthoringException e) when(e.Code=="CONCURRENT_CHANGE")
                { if(File.ReadAllText(a)=="newer user change") return; }
                throw new Exception("Concurrent change lost");
            });
            check("source changing between planning read and Add is not overwritten",()=> {
                var plan=new ChangePlan(root); plan.Read("a.txt");
                File.WriteAllText(a,"changed during planning"); plan.Add("a.txt","generated A");
                try {plan.Apply();} catch(AuthoringException e) when(e.Code=="CONCURRENT_CHANGE")
                {if(File.ReadAllText(a)=="changed during planning") {File.WriteAllText(a,"newer user change");return;}}
                throw new Exception("Planning read race lost user work");
            });
            check("write failure restores original bytes including user edits",()=> {
                var plan=new ChangePlan(root); plan.Add("a.txt","generated A"); plan.Add("b.txt","generated B");
                try {plan.Apply(i=> {if(i==1) throw new IOException("Injected failure");});}
                catch(IOException)
                { if(File.ReadAllText(a)=="newer user change" && File.ReadAllText(b)=="user change B") return; }
                throw new Exception("Failed rollback");
            });
            check("apply then identical plan is no-op",()=> {
                var plan=new ChangePlan(root); plan.Add("a.txt","generated A"); plan.Apply();
                var again=new ChangePlan(root); again.Add("a.txt","generated A");
                if(again.Changes.Count!=0) throw new Exception("Not idempotent");
            });
            check("plan cannot escape repository",()=> {
                try {new ChangePlan(root).Add("../escape.txt","bad");}
                catch(AuthoringException e) when(e.Code=="UNSAFE_PATH") {return;}
                throw new Exception("Allowed escape");
            });
            check("rollback preserves edits arriving after tool write",()=> {
                var plan=new ChangePlan(root); plan.Add("a.txt","second A"); plan.Add("b.txt","second B");
                try {plan.Apply(i=> {if(i==1) {File.WriteAllText(a,"external edit"); throw new IOException("Stop");}});}
                catch(AuthoringException e) when(e.Code=="ROLLBACK_CONFLICT")
                {if(File.ReadAllText(a)=="external edit" && File.ReadAllText(b)=="user change B") return;}
                throw new Exception("Overwrote concurrent work during rollback");
            });
        }
        finally
        {
            if(!Path.GetFullPath(root).StartsWith(testRoot+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new Exception("Unsafe fixture cleanup path");
            Directory.Delete(root,true);
        }
    }
}
