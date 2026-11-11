using System.Reflection;
using CorpPresence;
using Module = CorpPresence.Malice.Malice;
using Envoy = CorpPresence.Malice.Envoy;
internal static class NotificationChecks
{
    internal static void Run(Action<bool,string> check)
    {
        void Call(string name,params object[] args)=>typeof(Envoy).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
        Module.Install();
        UnityEngine.Time.unscaledTime=0;
        Call("After",1f,"old timed");Call("Later","old pending");
        Ext.StateReset.Publish();
        UnityEngine.Time.unscaledTime=2;Envoy.Tick();Call("FlushLater");
        check(StoreUIManager.Instance.Messages.Count==0,"save transition discards old timed and deferred notifications");
        Call("After",1f,"new timed");Call("Later","new pending");
        UnityEngine.Time.unscaledTime=4;CorpPresence.Runtime.RuntimeUpdates.Run(4);Call("FlushLater");
        check(StoreUIManager.Instance.Messages.SequenceEqual(new[]{"new timed","new pending"}),"new save notifications still deliver once");
        StoreUIManager.Instance.Messages.Clear();Call("After",1f,"disabled");Call("Later","disabled pending");
        Module.Uninstall();Module.Install();UnityEngine.Time.unscaledTime=8;Envoy.Tick();Call("FlushLater");
        check(StoreUIManager.Instance.Messages.Count==0,"module reinstall cannot deliver notifications from previous lifetime");
        Module.Uninstall();
        Call("After",0f,"uninstalled");CorpPresence.Runtime.RuntimeUpdates.Run(9);
        check(StoreUIManager.Instance.Messages.Count==0,"uninstalled module has no host update callback");
        Call("ResetNotifications");
        var runtime=new CorpPresence.Runtime.RuntimeLifetime(_=>{});
        runtime.Start(()=>runtime.Own(Module.Install,Module.Uninstall));
        Call("After",0f,"rollback timed");Call("Later","rollback pending");
        try { runtime.Continue(()=>throw new InvalidOperationException("later startup failure")); } catch(InvalidOperationException) { }
        Envoy.Tick();Call("FlushLater");CorpPresence.Runtime.RuntimeUpdates.Run(10);
        check(StoreUIManager.Instance.Messages.Count==0 && !Module.IsInstalled,"runtime rollback clears notifications and revokes module update");
    }
}
