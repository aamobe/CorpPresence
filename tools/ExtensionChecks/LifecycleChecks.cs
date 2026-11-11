using System.Reflection;
using CorpPresence;
using CorpPresence.Runtime;
using Module = CorpPresence.Malice.Malice;

internal static class LifecycleChecks
{
    private sealed class ReleaseAction(Action action) : IDisposable { public void Dispose() => action(); }

    internal static void Run(Action<bool,string> check)
    {
        // Inject into the actual private lease collection; no production test hooks.
        var field=typeof(Module).GetField("_callbacks",BindingFlags.NonPublic|BindingFlags.Static);
        bool ReleaseFailed() => (bool)typeof(Module).GetProperty("LastReleaseFailed",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
        var blocker=Ext.StateReset.Subscribe("malice",()=>{});
        ModLog.Warnings.Clear();MaintenanceDiagnostics.Fail=true;ModLog.Fail=true;
        Exception original=null;
        try { Module.Install(); } catch(InvalidOperationException error) { original=error; }
        MaintenanceDiagnostics.Fail=false;ModLog.Fail=false;
        check(original!=null && original.Message.Contains("malice"),"installation exception survives both failing reporters");
        check(ModLog.Warnings.Any(s=>s.Contains("malice/install") && s.Contains("InvalidOperationException")),"installation warning identifies owner stage and exception type");
        check(Ext.BeforeSave.Owners.Count==0 && Ext.StateReset.Owners.Count==1,"reporter errors do not block rollback");
        blocker.Dispose();

        // Main's two-phase ownership pattern, with both production classes linked.
        var runtime=new RuntimeLifetime(_=>throw new Exception("reporter"));
        int baseReleases=0;
        void Start() => runtime.Start(()=>runtime.Own(()=>{},()=>baseReleases++));
        void Continue() => runtime.Continue(()=>runtime.Own(Module.Install,Module.Uninstall));
        var external=Ext.DayStart.Subscribe("external",()=>{});
        Start();check(!Module.IsInstalled,"resource-waiting phase does not install module");
        Continue();check(Module.IsInstalled,"resource continuation commits module");
        Module.On=false;check(Module.IsInstalled,"mode switch is not module uninstall");Module.On=true;
        runtime.Stop();runtime.Stop();
        check(!Module.IsInstalled && baseReleases==1 && Ext.DayStart.Owners.SequenceEqual(new[]{"external"}),"runtime stop is idempotent and preserves external lease");

        Start();blocker=Ext.StateReset.Subscribe("malice",()=>{});
        try { Continue();throw new Exception("expected registration conflict"); } catch(InvalidOperationException) { }
        check(!Module.IsInstalled && baseReleases==2 && Ext.BeforeSave.Owners.Count==0 && Ext.StateReset.Owners.Count==1,"resource install failure rolls back both lifetime phases");
        blocker.Dispose();runtime.Stop();
        Start();Continue();
        var resourceError=new InvalidOperationException("resource after module");
        Exception seen=null;
        try { runtime.Continue(()=>throw resourceError); } catch(Exception error) { seen=error; }
        check(ReferenceEquals(seen,resourceError) && !Module.IsInstalled && baseReleases==3 && Ext.DayStart.Owners.SequenceEqual(new[]{"external"}),"later resource failure preserves original exception and releases installed module");
        Start();Continue();
        check(Module.IsInstalled && Ext.BeforeSave.Owners.Count==1 && Ext.StateReset.Owners.Count==1 && Ext.DayStart.Owners.SequenceEqual(new[]{"external","malice"}),"clean rollback allows one successful reenable");
        runtime.Stop();external.Dispose();
        // A failed release is tested last: this module must remain quarantined for this process.
        var failedRuntime=new RuntimeLifetime(_=>throw new Exception("reporter"));
        int remainingCleanup=0;
        failedRuntime.Start(()=>{failedRuntime.Own(()=>{},()=>remainingCleanup++);failedRuntime.Own(Module.Install,Module.Uninstall);});
        var callbacks=(OwnedCallbacks)field.GetValue(null);
        var order=new List<int>();
        callbacks.Own(new ReleaseAction(()=>order.Add(1)));
        callbacks.Own(new ReleaseAction(()=>{order.Add(2);throw new InvalidOperationException("private-release-message");}));
        callbacks.Own(new ReleaseAction(()=>order.Add(3)));
        ModLog.Warnings.Clear();MaintenanceDiagnostics.Fail=true;ModLog.Fail=true;
        failedRuntime.Stop();failedRuntime.Stop();
        MaintenanceDiagnostics.Fail=false;ModLog.Fail=false;
        check(ReleaseFailed() && !Module.IsInstalled && remainingCleanup==1,"failed actual release remains visible while other runtime cleanup continues");
        check(order.SequenceEqual(new[]{3,2,1}) && Ext.DayStart.Owners.Count==0 && Ext.BeforeSave.Owners.Count==0 && Ext.StateReset.Owners.Count==0,"release failure and reporter failures preserve reverse cleanup");
        check(ModLog.Warnings.Any(s=>s.Contains("malice/uninstall") && s.Contains("InvalidOperationException") && !s.Contains("private-release-message")),"release warning identifies stage without private message");
        bool blockedModule=false,blockedRuntime=false;
        try{Module.Install();}catch(InvalidOperationException){blockedModule=true;}
        try{failedRuntime.Start(()=>{});}catch(InvalidOperationException){blockedRuntime=true;}
        check(blockedModule && blockedRuntime && ReleaseFailed(),"failed cleanup blocks module and runtime reinstallation without clearing evidence");
        try{Module.Uninstall();}catch(InvalidOperationException){}
        check(ReleaseFailed() && order.SequenceEqual(new[]{3,2,1}),"unknown partial releases are not replayed by repeated uninstall");
    }
}
