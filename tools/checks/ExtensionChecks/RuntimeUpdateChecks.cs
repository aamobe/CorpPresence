using CorpPresence;
using CorpPresence.Runtime;
internal static class RuntimeUpdateChecks
{
    internal static void Run(Action<bool,string> check)
    {
        int first=0,second=0;
        var a=RuntimeUpdates.Register("first",()=>first++);
        var b=RuntimeUpdates.Register("second",()=>second++);
        RuntimeUpdates.Run(0);
        check(first==1 && second==1,"registered module tasks execute once per host update");
        bool rejected=false;try{RuntimeUpdates.Register("first",()=>{});}catch(InvalidOperationException){rejected=true;}
        check(rejected,"duplicate task owners rejected");
        a.Dispose();a.Dispose();RuntimeUpdates.Run(1);
        check(first==1 && second==2,"task lease removes only its own callback");
        var fresh=RuntimeUpdates.Register("first",()=>first++);a.Dispose();RuntimeUpdates.Run(2);
        check(first==2 && second==3,"old task lease cannot remove replacement");
        fresh.Dispose();b.Dispose();
        IDisposable pending=null,added=null;
        int skipped=0,late=0;
        using(var mutator=RuntimeUpdates.Register("mutator",()=>{pending.Dispose();added??=RuntimeUpdates.Register("added",()=>late++);})) {
            pending=RuntimeUpdates.Register("pending",()=>skipped++);
            RuntimeUpdates.Run(2);
            check(skipped==0 && late==0,"revoked tasks skip current update and newly registered tasks wait");
            RuntimeUpdates.Run(3);
            check(skipped==0 && late==1,"new task joins next update exactly once");
        }
        added.Dispose();pending.Dispose();
        using var broken=RuntimeUpdates.Register("broken",()=>throw new Exception("private"));
        using var good=RuntimeUpdates.Register("good",()=>second++);
        MaintenanceDiagnostics.Reports=0;RuntimeUpdates.Run(3);RuntimeUpdates.Run(4);
        check(second==5 && MaintenanceDiagnostics.Reports==2,"failed task cannot block following task and failures stay diagnosed");
    }
}
