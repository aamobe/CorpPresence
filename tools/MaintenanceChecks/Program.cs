using CorpPresence;
void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
PlayerStore.Instance=null;CorpState.Current=null;
Check(MaintenanceDiagnostics.Capture().SaveStatus==SaveDiagnosticStatus.NoStore && !PlayerStore.IsInstanceExist() && PlayerStore.LazyCreates==0,"CaptureWithoutStoreDoesNotCreatePlayer");
PlayerStore.Instance=new();var data=PlayerStore.Instance.modData;data["CorpPresence"]="broken json";SaveSlot.ProtectUnreadable("CorpPresence");
var before=data["CorpPresence"];var snap=MaintenanceDiagnostics.Capture();
Check(snap.SaveStatus==SaveDiagnosticStatus.NotLoaded && CorpState.Current==null && data["CorpPresence"]==before && !SaveSlot.CanWrite("CorpPresence"),"CaptureDoesNotLoadOrRepairState");
PlayerStore.Instance=new();var state=CorpState.Get();state.Companies["test"]=new CompanyState();state.Companies["test"].Orders.Add(new Order{Items=new(){"a","b"}});
snap=MaintenanceDiagnostics.Capture();Check(snap.PendingOrders==1 && snap.PendingItemUnits==2,"counts are remaining items, not orders");
state.Companies["test"].Orders.Clear();Check(snap.PendingOrders==1 && snap.PendingItemUnits==2,"SnapshotDoesNotExposeLiveCollections");
PlayerStore.Instance.modData=new();Check(MaintenanceDiagnostics.Capture().SaveStatus==SaveDiagnosticStatus.Stale && MaintenanceDiagnostics.Capture().PendingOrders==null,"StaleBindingHasUnknownOrderCount");
CorpState.Get();Portraits.Probe=()=>PlayerStore.Instance=new();Check(MaintenanceDiagnostics.Capture().SaveStatus==SaveDiagnosticStatus.Unstable,"SessionChangeDiscardsCounts");Portraits.Probe=null;
CorpState.Get();Portraits.Probe=()=>GameResources.Generation++;Check(MaintenanceDiagnostics.Capture().PendingOrders==null,"resource generation change discards counts");Portraits.Probe=null;
MaintenanceDiagnostics.Reset();for(int i=0;i<65;i++)MaintenanceDiagnostics.Record("test","phase",i.ToString(),null);
snap=MaintenanceDiagnostics.Capture();Check(snap.Faults.Count==64 && snap.Faults[0].Code=="1","FaultBufferEvictsAt64");
MaintenanceDiagnostics.Record("test","phase","64",new InvalidOperationException("secret/path"));
snap=MaintenanceDiagnostics.Capture();Check(snap.Faults.All(f=>!f.Detail.Contains("secret")),"no exception message leakage");
MaintenanceDiagnostics.Record("test","phase","64",new InvalidOperationException());
Check(MaintenanceDiagnostics.Capture().Faults.Last().Count==2,"duplicate fault aggregation");
Check(MaintenanceFault.Increment(int.MaxValue)==int.MaxValue,"DuplicateFaultSaturates");
MaintenanceDiagnostics.Record(new string('a',9000),"p","c",null);Check(MaintenanceDiagnostics.Capture().Faults.Last().Source.Length<=128,"bounded fault identifiers");
PlayerStore.Instance=new();Check(MaintenanceDiagnostics.Capture().Faults.Count==0,"FaultsResetAcrossStores");
Portraits.Probe=()=>throw new Exception("private");Check(MaintenanceDiagnostics.Capture().SaveStatus==SaveDiagnosticStatus.Unstable,"DiagnosticFailureDoesNotEscape");Portraits.Probe=null;
Check(MaintenanceFormatter.Format(MaintenanceDiagnostics.Capture()).Contains("未知"),"unknown counts stay unknown");

// Query must not adopt a dictionary or alter the binding used by later gameplay.
PlayerStore.Instance=new(){modData=null};CorpState.Current=null;state=CorpState.Get();
PlayerStore.Instance.modData=new();
Check(MaintenanceDiagnostics.Capture().SaveStatus==SaveDiagnosticStatus.Stale,"empty dictionary remains unadopted by capture");
PlayerStore.Instance.modData=null;
Check(MaintenanceDiagnostics.Capture().SaveStatus==SaveDiagnosticStatus.Bound,"readonly binding still matches original null dictionary");
PlayerStore.Instance=new();CorpState.Current=null;state=CorpState.Get();
Portraits.Probe=()=>PlayerStore.Instance.modData=new();
Check(MaintenanceDiagnostics.Capture().SaveStatus==SaveDiagnosticStatus.Unstable,"dictionary replacement during capture discards counts");Portraits.Probe=null;
state=CorpState.Get();Portraits.Probe=()=>CorpState.Current=new();
Check(MaintenanceDiagnostics.Capture().SaveStatus==SaveDiagnosticStatus.Unstable,"state replacement during capture discards counts");Portraits.Probe=null;
CorpState.Get();MaintenanceDiagnostics.Reset();
try{throw new InvalidOperationException("private path and account");}catch(Exception error){MaintenanceDiagnostics.Record("test","stack","thrown",error);}
snap=MaintenanceDiagnostics.Capture();Check(snap.Faults.Single().Detail.Length>0 && !snap.Faults.Single().Detail.Contains("private") && !snap.Faults.Single().Detail.Contains(".cs:"),"managed call chain excludes message and source location");
MaintenanceDiagnostics.Record("test","stack","thrown",new InvalidOperationException());
Check(snap.Faults.Single().Count==1,"previous fault snapshot remains immutable");
var list=(System.Collections.Generic.IList<MaintenanceFault>)snap.Faults;
bool readOnly=false;try{list.Clear();}catch(NotSupportedException){readOnly=true;}Check(readOnly,"snapshot collections reject mutation");
var oldData=PlayerStore.Instance.modData;oldData["CorpPresence"]="bad";SaveSlot.ProtectUnreadable("CorpPresence");
CorpState.Get();snap=MaintenanceDiagnostics.Capture();Check(snap.CorpWriteProtected==true && snap.PendingOrders==null && oldData["CorpPresence"]=="bad","protected fallback state never reports empty orders as trustworthy");
MaintenanceDiagnostics.Record("test","phase","old",null);PlayerStore.Instance=null;CorpState.Current=null;
Check(MaintenanceDiagnostics.Capture().Faults.Count==0,"leaving store drops business faults");
MaintenanceDiagnostics.Record("startup","phase","error",null);PlayerStore.Instance=new();
Check(MaintenanceDiagnostics.Capture().Faults.Count==0,"entering store drops menu faults");
MaintenanceDiagnostics.Record("test","phase","reset",null);MaintenanceDiagnostics.Reset();Check(MaintenanceDiagnostics.Capture().Faults.Count==0,"disable reset clears diagnostics");

UnityEngine.Time.unscaledTime=0;UnityEngine.Input.Down=false;MaintenanceConsole.Tick();Check(ModLog.Reports==0,"console does not report automatically");
UnityEngine.Input.Held.Add(UnityEngine.KeyCode.LeftControl);UnityEngine.Input.Held.Add(UnityEngine.KeyCode.RightShift);UnityEngine.Input.Down=true;
MaintenanceConsole.Tick();Check(ModLog.Reports==1,"modifier chord reports once");
UnityEngine.Input.Down=false;UnityEngine.Time.unscaledTime=2;MaintenanceConsole.Tick();Check(ModLog.Reports==1,"held key does not repeat");
UnityEngine.Input.Down=true;UnityEngine.Time.unscaledTime=0.5f;MaintenanceConsole.Tick();Check(ModLog.Reports==1,"console throttles within one second");
UnityEngine.Time.unscaledTime=2;ModLog.Fail=true;MaintenanceConsole.Tick();Check(ModLog.Reports==2,"output failure does not escape");
ModLog.Fail=false;UnityEngine.Time.unscaledTime=4;UnityEngine.Input.Held.Clear();MaintenanceConsole.Tick();Check(ModLog.Reports==2,"F10 alone does not request report");

int events=0;var lease=Ext.DayStart.Subscribe("test-owner",()=>events++);
snap=MaintenanceDiagnostics.Capture();Check(snap.Registrations.Any(r=>r.EventName=="DayStart" && r.Owner=="test-owner") && events==0,"snapshot reads event ownership without publishing");
lease.Dispose();Check(snap.Registrations.Count==1 && MaintenanceDiagnostics.Capture().Registrations.Count==0,"registration snapshots are independent");
// Run the real bootstrap Update while resources are unavailable.
var bootstrap=new CorpPresence.Runtime.ResourceBootstrap();int completed=0;bootstrap.Configure(()=>completed++, _=>{});
GameResources.Ready=false;UnityEngine.Time.unscaledTime=10;UnityEngine.Input.Down=true;UnityEngine.Input.Held.Add(UnityEngine.KeyCode.LeftControl);UnityEngine.Input.Held.Add(UnityEngine.KeyCode.LeftShift);
var reportsBefore=ModLog.Reports;
typeof(CorpPresence.Runtime.ResourceBootstrap).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(bootstrap,null);
Check(ModLog.Reports==reportsBefore+1 && completed==0 && !MaintenanceDiagnostics.Capture().ResourcesReady,"bootstrap reports while resources are unavailable without completing initialization");
bootstrap.Detach();GameResources.Ready=true;

// Read actual Capture/Formatter results; module inputs alone are substituted here.
var moduleProperty=typeof(MaintenanceSnapshot).GetProperty("Modules");
Check(moduleProperty!=null,"snapshot exposes module values");
System.Collections.IList Modules(MaintenanceSnapshot value)=>(System.Collections.IList)moduleProperty.GetValue(value);
bool Flag(object value,string name)=>(bool)value.GetType().GetProperty(name).GetValue(value);
PlayerStore.Instance=null;CorpState.Current=null;
CorpPresence.Malice.Malice.IsInstalled=true;CorpPresence.Malice.Malice.LastReleaseFailed=false;
snap=MaintenanceDiagnostics.Capture();
Check(Modules(snap).Count==2 && Flag(Modules(snap)[0],"Installed") && !Flag(Modules(snap)[0],"ReleaseFailed") && !PlayerStore.IsInstanceExist() && CorpState.Current==null,"module capture is readonly without a player or save");
CorpPresence.Malice.Malice.IsInstalled=false;CorpPresence.Malice.Malice.LastReleaseFailed=true;
Check(Flag(Modules(snap)[0],"Installed") && !Flag(Modules(snap)[0],"ReleaseFailed"),"old module snapshot does not follow live state");
readOnly=false;try{Modules(snap).Clear();}catch(NotSupportedException){readOnly=true;}Check(readOnly,"module list rejects mutation");
snap=MaintenanceDiagnostics.Capture();
Check(MaintenanceFormatter.Format(snap).Contains("malice 安装已提交=False 清理遇错=True"),"formatter distinguishes uninstalled from failed cleanup");
CorpPresence.Malice.Malice.IsInstalled=true;CorpPresence.Malice.Malice.LastReleaseFailed=false;
PlayerStore.Instance=new();MaintenanceDiagnostics.Reset();
Check(Flag(Modules(MaintenanceDiagnostics.Capture())[0],"Installed"),"session reset does not revoke module installation");
Portraits.Probe=()=>GameResources.Generation++;
snap=MaintenanceDiagnostics.Capture();Portraits.Probe=null;
Check(snap.SaveStatus==SaveDiagnosticStatus.Unstable && Modules(snap).Count==0 && MaintenanceFormatter.Format(snap).Contains("模块状态未知"),"unstable module state is unknown not uninstalled");
ItemRegistry.IsInstalled=true;ItemRegistry.LastReleaseFailed=false;
snap=MaintenanceDiagnostics.Capture();
Check(MaintenanceFormatter.Format(snap).Contains("items 安装已提交=True 清理遇错=False"),"items lifecycle is part of the shared module report");
ItemRegistry.IsInstalled=false;ItemRegistry.LastReleaseFailed=true;
Check(MaintenanceFormatter.Format(snap).Contains("items 安装已提交=True 清理遇错=False") && MaintenanceFormatter.Format(MaintenanceDiagnostics.Capture()).Contains("items 安装已提交=False 清理遇错=True"),"item module snapshot is immutable and keeps release failures visible");

// The same RuntimeTask used by Host must report every failure, even while logs are throttled.
MaintenanceDiagnostics.Reset();
int executions=0, taskLogs=0, suppressedFailures=-1;
var task=new CorpPresence.Runtime.RuntimeTask("test.task",()=>{executions++;throw new InvalidOperationException("private/path");},
    (_,_,suppressed)=>{taskLogs++;suppressedFailures=suppressed;throw new Exception("log unavailable");});
task.Run(0);task.Run(1);task.Run(2);
snap=MaintenanceDiagnostics.Capture();
Check(executions==3 && taskLogs==1 && snap.Faults.Single(f=>f.Source=="runtime" && f.Stage=="test.task").Count==3,"runtime task diagnostics count failures suppressed from logs");
Check(snap.Faults.All(f=>!f.Detail.Contains("private/path")),"runtime task diagnostics exclude exception messages");
task.Run(60);
Check(taskLogs==2 && suppressedFailures==2 && MaintenanceDiagnostics.Capture().Faults.Single(f=>f.Source=="runtime").Count==4,"log throttling and failed logger do not alter diagnostics or task retries");
var successfulTask=new CorpPresence.Runtime.RuntimeTask("test.success",()=>executions++,(_,_,_)=>taskLogs++);
successfulTask.Run(61);
Check(executions==5 && taskLogs==2 && MaintenanceDiagnostics.Capture().Faults.Count==1,"healthy tasks create no diagnostic faults");
PlayerStore.Instance=new();
Check(MaintenanceDiagnostics.Capture().Faults.Count==0,"runtime task failures do not leak into another save");
task.Run(62);
Check(MaintenanceDiagnostics.Capture().Faults.Single(f=>f.Source=="runtime").Count==1 && taskLogs==2,"new save receives failures even while old log throttle remains active");
MaintenanceDiagnostics.Reset();int stages=0;
var guardedTask=new CorpPresence.Runtime.RuntimeTask("outer.repair",()=>{
    HookGuard.Run("inner.repair",()=>throw new InvalidOperationException("private-save-message"));
    HookGuard.Run("next.repair",()=>stages++);
},(_,_,_)=>throw new Exception("outer should not log a handled exception"));
guardedTask.Run(0);
snap=MaintenanceDiagnostics.Capture();
Check(stages==1 && snap.Faults.Count==1 && snap.Faults[0].Source=="hooks" && snap.Faults[0].Stage=="inner.repair","handled inner hook failure reaches diagnostics exactly once and later stages continue");
Check(!snap.Faults[0].Detail.Contains("private-save-message"),"inner hook diagnostic excludes exception message");
ModLog.Fail=true;guardedTask.Run(1);ModLog.Fail=false;
Check(stages==2 && MaintenanceDiagnostics.Capture().Faults.Single().Count==2,"hook logging failure cannot hide diagnostics or stop later stages");
