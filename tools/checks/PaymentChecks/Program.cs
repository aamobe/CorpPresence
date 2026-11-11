using CorpPresence;
using PaymentPolicy = CorpPresence.Malice.Malice;
void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
void Reset(){PlayerStore.Instance=new();Companies.State=new();PaymentPolicy.IsInstalled=true;PaymentPolicy.Handler=null;Settings.MaliceMode=false;EmporiumAudioPlayer.Fail=false;MaintenanceDiagnostics.Codes.Clear();MaintenanceDiagnostics.Fail=false;}
var company=new Company();
foreach(var mode in new[]{"null","success","refused","throw","paid-throw"}){
 Reset();PaymentPolicy.Handler=(c,n,k)=>{if(mode=="paid-throw")PlayerStore.Instance.PayMoney(n,false);if(mode.EndsWith("throw"))throw new InvalidOperationException();return mode=="null"?null:mode=="success"?"":"refused";};
 var result=Services.Pay(company,100,"order");
 int expected=mode=="null"||mode=="paid-throw"?1:0;
 Check(PlayerStore.Instance.Calls==expected && PlayerStore.Instance.Cash==1000-100*expected,"exact debit count "+mode);
 Check(mode=="null"||mode=="success"?result==null:result!=null,"return contract "+mode);
}
Reset();PlayerStore.Instance=null;Check(Services.Pay(company,0,"free")!=null,"zero price still checks store");
foreach(var key in new[]{"CorpPresence","CorpPresence.Malice"}){Reset();Settings.MaliceMode=true;PlayerStore.Instance.modData[key]="broken";SaveSlot.ProtectUnreadable(key);Check(Services.Pay(company,0,"free")!=null && PlayerStore.Instance.Calls==0,"protected free service "+key);}
Reset();Companies.State.Visitors=null;Check(Services.Pay(company,100,"order")!=null && PlayerStore.Instance.Calls==0,"incomplete company state refuses before payment");
Reset();PlayerStore.Instance.Cash=1;Check(Services.Pay(company,100,"order")!=null && PlayerStore.Instance.Calls==0,"insufficient cash does not debit");
Reset();EmporiumAudioPlayer.Fail=true;Check(Services.Pay(company,100,"order")==null && PlayerStore.Instance.Calls==1,"audio failure preserves successful payment");
Reset();PlayerStore.Instance.ThrowAfterDebit=true;bool uncertain=false;try{Services.Pay(company,100,"order");}catch(InvalidOperationException){uncertain=true;}Check(uncertain && PlayerStore.Instance.Calls==1 && PlayerStore.Instance.Cash==900,"native debit exception remains uncertain and is not retried");

Reset();PaymentPolicy.Handler=(c,n,k)=>throw new Exception("private");Services.Pay(company,100,"order");Check(MaintenanceDiagnostics.Codes.Contains("payment.extension:uncertain"),"extension uncertainty recorded");
Reset();PlayerStore.Instance.Cash=0;Services.Pay(company,100,"order");Check(MaintenanceDiagnostics.Codes.Contains("payment.cash:insufficient"),"cash rejection recorded");
Reset();PlayerStore.Instance=null;Services.Pay(company,0,"free");Check(MaintenanceDiagnostics.Codes.Contains("payment.check:no_store"),"store rejection recorded");
Reset();MaintenanceDiagnostics.Fail=true;PaymentPolicy.Handler=(c,n,k)=>{PlayerStore.Instance.PayMoney(n,false);throw new Exception();};Check(Services.Pay(company,100,"order")!=null && PlayerStore.Instance.Calls==1,"diagnostic failure cannot cause additional debit");

Reset();PaymentPolicy.IsInstalled=false;PaymentPolicy.Handler=(c,n,k)=>throw new Exception("must not run");Check(Services.Pay(company,100,"order")==null && PlayerStore.Instance.Calls==1,"uninstalled policy uses cash without calling module");
Reset();PaymentPolicy.Handler=(c,n,k)=>throw new Exception("must not run");Check(Services.Pay(null,100,"order")==null && PlayerStore.Instance.Calls==1,"company-less payment bypasses module");
