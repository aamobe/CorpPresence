using CorpPresence;
using CorpPresence.Runtime;
void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
var logging=new ExtensionEvent("logging");int continued=0;
logging.Subscribe("broken",()=>throw new Exception("private-message"));logging.Subscribe("next",()=>continued++);
MaintenanceDiagnostics.Fail=true;ModLog.Fail=true;logging.Publish();logging.Publish();MaintenanceDiagnostics.Fail=false;ModLog.Fail=false;
Check(continued==2 && ModLog.Warnings.Count==1,"event warning survives diagnostic failure, deduplicates and isolates logger failure");
Check(ModLog.Warnings[0].Contains("logging") && ModLog.Warnings[0].Contains("broken") && !ModLog.Warnings[0].Contains("private-message"),"event warning identifies fixed owner and event without exception text");
var e=new ExtensionEvent("test");var calls=new List<string>();
var a=e.Subscribe("A",()=>calls.Add("A"));var b=e.Subscribe("B",()=>calls.Add("B"));
try{e.Subscribe("A",()=>{});throw new Exception("duplicate accepted");}catch(InvalidOperationException){}
e.Publish();Check(string.Join("",calls)=="AB" && e.Owners.Count==2,"duplicate rejected without losing ordered subscribers");
var owners=e.Owners;a.Dispose();a.Dispose();Check(e.Owners.Count==1 && owners.Count==2,"idempotent lease and independent owner snapshot");
e.Clear();a=e.Subscribe("A",()=>throw new Exception());b=e.Subscribe("B",()=>calls.Add("B"));calls.Clear();MaintenanceDiagnostics.Fail=true;e.Publish();Check(string.Join("",calls)=="B","listener and diagnostic failure isolated");MaintenanceDiagnostics.Fail=false;
e.Clear();IDisposable c=null;a=e.Subscribe("A",()=>{if(c==null)c=e.Subscribe("C",()=>calls.Add("C"));calls.Add("A");});b=e.Subscribe("B",()=>calls.Add("B"));calls.Clear();e.Publish();Check(string.Join("",calls)=="AB","new subscriber starts next publish");calls.Clear();e.Publish();Check(string.Join("",calls)=="ABC","new subscriber preserves registration order");
e.Clear();a=e.Subscribe("A",()=>{b.Dispose();calls.Add("A");});b=e.Subscribe("B",()=>calls.Add("B"));calls.Clear();e.Publish();Check(string.Join("",calls)=="A","revoked subscriber skipped in current snapshot");
e.Clear();int n=0;a=e.Subscribe("A",()=>{n++;e.Publish();});e.Publish();Check(n==1,"same event reentry rejected");
e.Clear();var old=a;var fresh=e.Subscribe("A",()=>{});old.Dispose();Check(e.Owners.Count==1,"old lease cannot remove registration after clear");
e.Clear();var owned=new OwnedCallbacks(_=>throw new Exception());owned.Own(e.Subscribe("owned",()=>{}));var external=e.Subscribe("other",()=>{});owned.Release();owned.Release();Check(e.Owners.SequenceEqual(new[]{"other"}),"owned cleanup preserves unrelated subscription");
foreach(var name in new[]{null,""," "}){bool rejected=false;try{e.Subscribe(name,()=>{});}catch(ArgumentException){rejected=true;}Check(rejected,"empty owner rejected");}
bool nullRejected=false;try{e.Subscribe("null",null);}catch(ArgumentNullException){nullRejected=true;}Check(nullRejected,"null callback rejected");
e.Clear();a=e.Subscribe("A",()=>e.Clear());b=e.Subscribe("B",()=>throw new Exception("must not run"));e.Publish();Check(e.Owners.Count==0,"clear during publish revokes pending callbacks");

// Execute the actual production installer, not a model of it.
bool ModuleFlag(string name)
{
    var property=typeof(CorpPresence.Malice.Malice).GetProperty(name,System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
    Check(property!=null,"module exposes readonly "+name);
    Check(property.SetMethod==null,"module flag has no setter "+name);
    return (bool)property.GetValue(null);
}
Check(!ModuleFlag("IsInstalled") && !ModuleFlag("LastReleaseFailed"),"initial module state is uninstalled and clean");
var blocker=Ext.StateReset.Subscribe("malice",()=>{});
bool failed=false;try{CorpPresence.Malice.Malice.Install();}catch(InvalidOperationException){failed=true;}
Check(failed && Ext.BeforeSave.Owners.Count==0 && Ext.StateReset.Owners.Count==1,"second registration failure rolls back first without removing other lease");
Check(!ModuleFlag("IsInstalled") && !ModuleFlag("LastReleaseFailed"),"failed install remains uninstalled after clean rollback");
blocker.Dispose();CorpPresence.Malice.Malice.Install();CorpPresence.Malice.Malice.Install();
Check(Ext.BeforeSave.Owners.Count==1 && Ext.DayStart.Owners.Count==1 && Ext.StateReset.Owners.Count==1,"installer retry succeeds and repeated install is idempotent");
Check(ModuleFlag("IsInstalled"),"successful installation is committed");
Ext.BeforeSave.Publish();Ext.DayStart.Publish();Ext.StateReset.Publish();
Check(CorpPresence.Malice.MaliceState.Saves==1 && CorpPresence.Malice.MaliceState.Resets==1 && CorpPresence.Malice.Malice.Days==1,"real installer callbacks dispatched once");
var unrelated=Ext.DayStart.Subscribe("unrelated",()=>{});CorpPresence.Malice.Malice.Uninstall();CorpPresence.Malice.Malice.Uninstall();
Check(Ext.BeforeSave.Owners.Count==0 && Ext.StateReset.Owners.Count==0 && Ext.DayStart.Owners.SequenceEqual(new[]{"unrelated"}),"actual uninstall preserves unrelated event lease");
Check(!ModuleFlag("IsInstalled") && !ModuleFlag("LastReleaseFailed"),"repeated normal uninstall remains clean");
CorpPresence.Malice.Malice.Install();Check(Ext.DayStart.Owners.SequenceEqual(new[]{"unrelated","malice"}),"reinstall preserves existing order");CorpPresence.Malice.Malice.Uninstall();unrelated.Dispose();

// Every failed invocation is diagnosed even when human warnings are suppressed.
MaintenanceDiagnostics.Reports=0;ModLog.Warnings.Clear();
for(int i=0;i<3;i++) {
    Check(Ext.Ask<int>("review.ask",()=>throw new InvalidOperationException("private-path"),7)==7,"failed query keeps fallback");
    Ext.Fire("review.fire",()=>throw new InvalidOperationException("private-path"));
}
Check(MaintenanceDiagnostics.Reports==6,"Ask and Fire diagnose failures hidden by warning throttle");
Check(ModLog.Warnings.Count==2 && ModLog.Warnings.All(x=>!x.Contains("private-path")),"extension warnings omit exception messages");
MaintenanceDiagnostics.Fail=true;ModLog.Fail=true;
Check(Ext.Ask<int>("review.logger",()=>throw new Exception("secret"),9)==9,"query fallback survives broken diagnostics and logger");
Ext.Fire("review.logger.fire",()=>throw new Exception("secret"));
MaintenanceDiagnostics.Fail=false;ModLog.Fail=false;

var resetWarnings=typeof(Ext).GetMethod("ResetWarnings",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
Check(resetWarnings!=null,"runtime exposes warning reset without revoking leases");
using(var bad=Ext.DayStart.Subscribe("warning-reset",()=>throw new Exception("private"))) {
    Ext.DayStart.Publish();ModLog.Warnings.Clear();
    resetWarnings.Invoke(null,null);
    Ext.DayStart.Publish();Ext.DayStart.Publish();
    Ext.Fire("review.fire",()=>throw new Exception());Ext.Fire("review.fire",()=>throw new Exception());
    Check(ModLog.Warnings.Count==2 && Ext.DayStart.Owners.Contains("warning-reset"),"new runtime warns again while retaining listeners and throttling repeats");
}

NotificationChecks.Run(Check);
RuntimeUpdateChecks.Run(Check);
LifecycleChecks.Run(Check);
