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
var blocker=Ext.StateReset.Subscribe("malice",()=>{});
bool failed=false;try{CorpPresence.Malice.Malice.Install();}catch(InvalidOperationException){failed=true;}
Check(failed && Ext.BeforeSave.Owners.Count==0 && Ext.StateReset.Owners.Count==1,"second registration failure rolls back first without removing other lease");
blocker.Dispose();CorpPresence.Malice.Malice.Install();CorpPresence.Malice.Malice.Install();
Check(Ext.BeforeSave.Owners.Count==1 && Ext.DayStart.Owners.Count==1 && Ext.StateReset.Owners.Count==1,"installer retry succeeds and repeated install is idempotent");
Ext.BeforeSave.Publish();Ext.DayStart.Publish();Ext.StateReset.Publish();
Check(CorpPresence.Malice.MaliceState.Saves==1 && CorpPresence.Malice.MaliceState.Resets==1 && CorpPresence.Malice.Malice.Days==1,"real installer callbacks dispatched once");
var unrelated=Ext.DayStart.Subscribe("unrelated",()=>{});CorpPresence.Malice.Malice.Uninstall();CorpPresence.Malice.Malice.Uninstall();
Check(Ext.BeforeSave.Owners.Count==0 && Ext.StateReset.Owners.Count==0 && Ext.DayStart.Owners.SequenceEqual(new[]{"unrelated"}),"actual uninstall preserves unrelated event lease");
CorpPresence.Malice.Malice.Install();Check(Ext.DayStart.Owners.SequenceEqual(new[]{"unrelated","malice"}),"reinstall preserves existing order");CorpPresence.Malice.Malice.Uninstall();unrelated.Dispose();
