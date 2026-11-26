using CorpPresence;

int passed = 0;
foreach (string raw in new[]
{
    "{", "null", """{"RunId":"test","Companies":null}""",
    """{"RunId":"test","Workshop":{"FirstOverhaul":{"Serial":0,"SignedDay":7,"Trucks":[]}}}""",
    """{"RunId":"test","Workshop":{"Rotations":null}}""",
    """{"RunId":"test","Workshop":{"Rotations":[null]}}""",
    """{"RunId":"test","Workshop":{"Rotations":[{"Window":0}]}}""",
    """{"RunId":"test","Workshop":{"ShizukaKitRemaining":6}}""",
    """{"RunId":"test","Workshop":{"Commission":{"Quantity":2,"Ingots":3,"Alloy":0}}}""",
    """{"RunId":"test","Workshop":{"Commission":{"Quantity":2,"Ingots":0,"Alloy":0,"Paid":35}}}""",
    """{"RunId":"test","Companies":{"meridian":{"Orders":[{"Kind":"frame_commission","Day":10,"Items":["metal_ingot"]}]}}}""",
    """{"RunId":"test","Workshop":{"ShizukaKitRemaining":-1}}""",
    """{"RunId":"test","Workshop":{"GelOrder":{"Day":1,"Offer":"wire","Amount":64}}}""",
    """{"RunId":"test","Workshop":null}""",
    """{"RunId":"test","Workshop":{"Version":2}}""",
    """{"RunId":"test","Workshop":{"CenKit":"other"}}""",
    """{"RunId":"test","Workshop":{"CenKitRemaining":1}}""",
    """{"RunId":"test","Workshop":{"CenKit":"glass","CenKitRemaining":5}}""",
    """{"RunId":"test","Workshop":{"Sales":null}}""",
    """{"RunId":"test","Workshop":{"Sales":{"Rods":{"Day":7,"Offer":"rod_hard_stock","Amount":54,"Received":1,"Paid":19}}}}""",
    """{"RunId":"test","Workshop":{"FeederTrial":{"LoanId":5,"Batches":1,"LastNight":7,"Spec":{"Day":7,"Offer":"meridian_feeder_spec","Amount":68,"Received":1,"Paid":68,"CompletedDay":7}}}}""",
    """{"RunId":"test","Characters":{"rosen":null}}""",
    """{"RunId":"test","Companies":{"pastel":{"Orders":[{"Items":null}]}}}"""
})
{
    PlayerStore.Instance = new PlayerStore();
    PlayerStore.Instance.modData["CorpPresence"] = raw;
    CorpState.Current = null;
    CorpState.Get().CompanyOf("pastel").Coop = 42;
    CorpState.Save();
    if (PlayerStore.Instance.modData["CorpPresence"] != raw)
        throw new Exception("Unreadable record was overwritten");
    passed++;
}
PlayerStore.Instance = new PlayerStore();
PlayerStore.Instance.modData["CorpPresence"] = """{"RunId":"test","Seline":{"Chapter":3}}""";
CorpState.Current = null;
var state = CorpState.Get();
if (state.Of("seline").Chapter != 3) throw new Exception("Legacy progress lost");
if (state.Workshop.CenKit != null || state.Workshop.CenKitRemaining != 0)
    throw new Exception("Legacy save already consumed its starter entitlement");
if (state.Workshop.ShizukaKitRemaining != 5 || state.Workshop.GelOrder != null || state.Workshop.CartelDaily != null)
    throw new Exception("Legacy store lost Shizuka starter entitlement");
if (state.Workshop.Rotations.Count != 0) throw new Exception("Legacy store must have no rotation records");
if (state.Workshop.FirstOverhaul != null) throw new Exception("Legacy store already signed the overhaul");
if (state.Workshop.Commission != null) throw new Exception("Legacy store already submitted materials");
if (state.Workshop.Sales.Rods != null) throw new Exception("Legacy store already has a rod receipt");
if (state.Workshop.FeederTrial != null) throw new Exception("Legacy store already has a feeder loan");
state.Workshop.FeederTrial = new FeederTrialState { LoanId = 5, ToolsRemaining = 0, Batches = 2, LastNight = 7, Returned = true,
    Spec = new WorkshopSaleReceipt { Day = 8, Offer = WorkshopSaleOffer.MeridianFeederSpec.Id, Amount = 68, Received = 1, Paid = 19 } };
state.Workshop.Sales.Rods = new WorkshopSaleReceipt { Day = 7, Offer = WorkshopSaleOffer.FatigueRod.Id, Amount = 54, Received = 1, Paid = 19 };
state.Workshop.Commission = new MeridianCommissionReceipt { Quantity = 2, Ingots = 1 };
state.Workshop.FirstOverhaul = FirstOverhaulState.Sign(7);
state.Workshop.ShizukaKitRemaining = 3;
state.Workshop.CenKit = "salvage";
state.Workshop.CenKitRemaining = 2;
state.CompanyOf("pastel").Orders.Add(new Order());
CorpState.Save();
CorpState.Current = null;
state = CorpState.Get();
if (state.Workshop.FirstOverhaul?.Trucks[1].DueDay != 17 || state.Workshop.ShizukaKitRemaining != 3 || state.Workshop.CenKit != "salvage" || state.Workshop.CenKitRemaining != 2)
    throw new Exception("Partial starter delivery was lost on reload");
if (state.Workshop.Commission?.Ingots != 1 || state.Workshop.Commission.Quantity != 2) throw new Exception("Partial commission lost on load");
if (state.Workshop.Sales.Rods is not { Paid: 19, Amount: 54, Received: 1, Complete: false })
    throw new Exception("Part-paid rod receipt did not retain its real delivery and amount");
if (state.Workshop.FeederTrial is not { LoanId: 5, Returned: true, Handbook: false, Spec: { Paid: 19 } })
    throw new Exception("Feeder return or partial Meridian spec was not restored");
if (state.Of("seline").Chapter != 3 || state.CompanyOf("pastel").Orders.Count != 1)
    throw new Exception("Save roundtrip lost progress");
passed++;
Console.WriteLine($"{passed} passed: malformed/null records protected; legacy progress and orders roundtrip");


// Replay a completed pre-rotation store through the real serializer and save protection boundary.
var first = state.Workshop.FirstOverhaul;
foreach (var truck in first.Trucks)
{
    truck.StartedDay = truck.CompletedDay = 17;
    truck.Amount = truck.Paid = truck.BasePrice + (truck.DueDay >= 17 ? truck.Bonus : 0);
}
first.Trucks[0].Received = new[] { 2, 4 };
first.Trucks[1].Received = new[] { 2 };
first.Trucks[0].SourceIds = new[] { 1, 1, 2, 2, 2, 2 };
first.Trucks[1].SourceIds = new[] { 3, 4 };
first.CompletedDay = 17;
CorpState.Save();
var oldData = Newtonsoft.Json.Linq.JObject.Parse(PlayerStore.Instance.modData["CorpPresence"]);
((Newtonsoft.Json.Linq.JObject)oldData["Workshop"]).Remove("Rotations");
PlayerStore.Instance.modData["CorpPresence"] = oldData.ToString();
CorpState.Current = null;
state = CorpState.Get();
if (state.Workshop.FirstOverhaul?.CompletedDay != 17 || state.Workshop.Rotations.Count != 0
    || RotationContract.WindowAt(state.Workshop.FirstOverhaul.CompletedDay, 18) != 0)
    throw new Exception("Pre-rotation settled first contract did not retain its calendar");
var retired = RotationContract.Sign(2, 24);
retired.Shells[1].Issued = true;
retired.Abandoned = true;
state.Workshop.Rotations.Add(retired);
state.Workshop.Rotations.Add(RotationContract.Sign(3, 27));
CorpState.Save();
CorpState.Current = null;
state = CorpState.Get();
if (state.Workshop.Rotations.Count != 2 || !state.Workshop.Rotations[0].Shells[1].Issued
    || !state.Workshop.Rotations[0].Abandoned || state.Workshop.Rotations[1].Trucks[1].DueDay != 37)
    throw new Exception("Historical shell and active contract did not roundtrip together");
Console.WriteLine("PASS completed legacy overhaul, archived shell slot and active rotation roundtrip");

if (state.Workshop.PumpRepair != null) throw new Exception("An old store must not inherit a pump repair");
var handbook = state.Workshop.FeederTrial.Spec;
handbook.Paid = handbook.Amount;
handbook.CompletedDay = handbook.Day;
state.Workshop.PumpRepair = new WaterPumpRepairState { PumpId = 65, IssuedDay = 18, ReceivedDay = 19, Amount = 195, Paid = 50 };
CorpState.Save();
string repairSave = PlayerStore.Instance.modData["CorpPresence"];
CorpState.Current = null;
state = CorpState.Get();
if (state.Workshop.PumpRepair is not { PumpId: 65, Paid: 50, Amount: 195, ReceivedDay: 19, CompletedDay: null })
    throw new Exception("Repair receipt lost its actual payment after reload");
var badRepair = Newtonsoft.Json.Linq.JObject.Parse(repairSave);
badRepair["Workshop"]["PumpRepair"]["Paid"] = 196;
string badRepairSave = badRepair.ToString();
PlayerStore.Instance.modData["CorpPresence"] = badRepairSave;
CorpState.Current = null;
CorpState.Get();
CorpState.Save();
if (SaveSlot.CanWrite("CorpPresence") || PlayerStore.Instance.modData["CorpPresence"] != badRepairSave)
    throw new Exception("Overpaid repair data was incorrectly writable");
PlayerStore.Instance.modData["CorpPresence"] = repairSave;
CorpState.Current = null;
state = CorpState.Get();
Console.WriteLine("PASS pump repair roundtrip and original save protection for an impossible payment");

state.Workshop.PumpRepair = new WaterPumpRepairState
{
    FabricatedPumpId = 66, IssuedDay = 20, ReceivedDay = 20, CompletedDay = 20,
    Amount = WaterPumpRepairState.BaseBuildPay, Paid = WaterPumpRepairState.BaseBuildPay
};
state.Workshop.CoreIntake = new CoreIntakeState { TimingId = 67, PhaseId = 68 };
CorpState.Save();
string coreSave = PlayerStore.Instance.modData["CorpPresence"];
CorpState.Current = null;
state = CorpState.Get();
if (state.Workshop.PumpRepair is not { PumpId: 0, FabricatedPumpId: 66, CompletedDay: 20 }
    || state.Workshop.CoreIntake is not { TimingId: 67, PhaseId: 68 })
    throw new Exception("Direct pump delivery and issued core modules lost their identities");
var damagedCoreSave = Newtonsoft.Json.Linq.JObject.Parse(coreSave);
damagedCoreSave["Workshop"]["CoreIntake"]["PhaseId"] = 67;
string corruptCores = damagedCoreSave.ToString();
PlayerStore.Instance.modData["CorpPresence"] = corruptCores;
CorpState.Current = null;
CorpState.Get();
CorpState.Save();
if (SaveSlot.CanWrite("CorpPresence") || PlayerStore.Instance.modData["CorpPresence"] != corruptCores)
    throw new Exception("Identical core identities were not write-protected");
PlayerStore.Instance.modData["CorpPresence"] = coreSave;
CorpState.Current = null;
state = CorpState.Get();
Console.WriteLine("PASS direct pump delivery and independent core IDs roundtrip; rejected duplicated core provenance");

if (state.Workshop.PhaseSite != null) throw new Exception("Old stores must not inherit a reserved salvage site");
state.Workshop.PhaseSite = new PhaseSiteState { SurveyDay = 20, Route = PhaseRoute.Whole, DispatchDay = 21, StationId = 99, PowerSpent = 24 };
CorpState.Save();
string phaseSave = PlayerStore.Instance.modData["CorpPresence"];
CorpState.Current = null;
state = CorpState.Get();
if (state.Workshop.PhaseSite is not { Route: PhaseRoute.Whole, DueDay: 23, StationId: 99, PowerSpent: 24, CargoId: null })
    throw new Exception("The tracked shaft shipment lost the actual route or dispatch station");
var invalidPhase = Newtonsoft.Json.Linq.JObject.Parse(phaseSave);
invalidPhase["Workshop"]["PhaseSite"]["PowerSpent"] = 16;
string invalidPhaseSave = invalidPhase.ToString();
PlayerStore.Instance.modData["CorpPresence"] = invalidPhaseSave;
CorpState.Current = null;
CorpState.Get();
CorpState.Save();
if (SaveSlot.CanWrite("CorpPresence") || PlayerStore.Instance.modData["CorpPresence"] != invalidPhaseSave)
    throw new Exception("An underpowered intact dispatch was not write-protected");
PlayerStore.Instance.modData["CorpPresence"] = phaseSave;
CorpState.Current = null;
state = CorpState.Get();
Console.WriteLine("PASS phase site route, return day and station roundtrip; invalid dispatch protected");

if (state.Workshop.JointWaterWork != null) throw new Exception("Legacy store unexpectedly has a joint work order");
state.Workshop.PhaseSite.CargoId = 100;
state.Workshop.JointWaterWork = new JointWaterWorkState
{
    SignedDay = 23, KitId = 101, ArrivalDay = 24, LoadDay = 25, ServiceNights = 1, LastServiceDay = 26, Paid = 100
};
CorpState.Save();
string jointSave = PlayerStore.Instance.modData["CorpPresence"];
CorpState.Current = null;
state = CorpState.Get();
if (state.Workshop.JointWaterWork is not { KitId: 101, Earned: 225, Paid: 100, Due: 125, LastServiceDay: 26 })
    throw new Exception("Saved joint service lost its measured shift or partial payment");
var badJoint = Newtonsoft.Json.Linq.JObject.Parse(jointSave);
badJoint["Workshop"]["JointWaterWork"]["ServiceNights"] = 2;
string badJointSave = badJoint.ToString();
PlayerStore.Instance.modData["CorpPresence"] = badJointSave;
CorpState.Current = null;
CorpState.Get();
CorpState.Save();
if (SaveSlot.CanWrite("CorpPresence") || PlayerStore.Instance.modData["CorpPresence"] != badJointSave)
    throw new Exception("Two service shifts on the same day were not write-protected");
PlayerStore.Instance.modData["CorpPresence"] = jointSave;
CorpState.Current = null;
state = CorpState.Get();
Console.WriteLine("PASS joint service milestone and balance roundtrip; protected impossible shift history");

var blueprint = new StoreClient();
PlayerStore.Instance.currentClientInstance = new Client { Blueprint = blueprint };
var blockedVisit = new CharacterVisitSession("seline", state.Of("seline"), blueprint);
StoreSession.RecordInventoryLoad("partial", true);
if (blockedVisit.IsCurrent || blockedVisit.TryFinish())
    throw new Exception("Incomplete inventory allowed a visit to finish");
StoreSession.Invalidate();
if (blockedVisit.TryFinish()) throw new Exception("Old visit completed after reload");
Console.WriteLine("PASS incomplete inventory preserves visit completion until a new session");
var visit = new CharacterVisitSession("seline", state.Of("seline"), blueprint);
if (!visit.IsCurrent || !visit.TryFinish() || visit.TryFinish())
    throw new Exception("Visit completion was not exactly once");
var replacementGuard = new CharacterVisitSession("seline", state.Of("seline"), blueprint);
PlayerStore.Instance.currentClientInstance = new Client { Blueprint = new StoreClient() };
if (replacementGuard.IsCurrent || replacementGuard.TryFinish())
    throw new Exception("Old client callback remained valid");
PlayerStore.Instance.currentClientInstance.Blueprint = blueprint;
var reloadGuard = new CharacterVisitSession("seline", state.Of("seline"), blueprint);
PlayerStore.Instance.modData = new Dictionary<string, string>(PlayerStore.Instance.modData);
if (reloadGuard.IsCurrent) throw new Exception("Reload retained old visit");
CorpState.Get();
var generationGuard = new CharacterVisitSession("seline", CorpState.Current.Of("seline"), blueprint);
GameResources.Generation++;
if (generationGuard.IsCurrent) throw new Exception("Runtime generation retained old visit");
Console.WriteLine("4 passed: completion once; replaced customer, save dictionary and runtime reject stale visits");

foreach(int giftStatus in new[]{0,1,2}){
    PlayerStore.Instance=new PlayerStore();CorpState.Current=null;
    var giftState=CorpState.Get();
    if(giftState.MangoGiftStatus!=0)throw new Exception("Old saves must default to unclaimed");
    giftState.MangoGiftStatus=giftStatus;CorpState.Save();CorpState.Current=null;
    if(CorpState.Get().MangoGiftStatus!=giftStatus)throw new Exception("Mango gift status lost on load");
}
foreach(int bad in new[]{-1,3,int.MaxValue}){
    PlayerStore.Instance=new PlayerStore();CorpState.Current=null;
    string raw="{\"RunId\":\"test\",\"MangoGiftStatus\":"+bad+"}";
    PlayerStore.Instance.modData["CorpPresence"]=raw;
    CorpState.Get();CorpState.Save();
    if(SaveSlot.CanWrite("CorpPresence")||PlayerStore.Instance.modData["CorpPresence"]!=raw)throw new Exception("Invalid gift state was overwritten");
}
Console.WriteLine("6 passed: mango defaults, three-state roundtrip and invalid-state write protection");

foreach(int giftStatus in new[]{0,1,2}){
    PlayerStore.Instance=new PlayerStore();CorpState.Current=null;
    var giftState=CorpState.Get();
    if(giftState.WasteBatteryGiftStatus!=0)throw new Exception("Old saves must default waste battery to unclaimed");
    giftState.WasteBatteryGiftStatus=giftStatus;CorpState.Save();CorpState.Current=null;
    if(CorpState.Get().WasteBatteryGiftStatus!=giftStatus)throw new Exception("Waste battery gift status lost on load");
}
foreach(int bad in new[]{-1,3,int.MaxValue}){
    PlayerStore.Instance=new PlayerStore();CorpState.Current=null;
    string raw="{\"RunId\":\"test\",\"WasteBatteryGiftStatus\":"+bad+"}";
    PlayerStore.Instance.modData["CorpPresence"]=raw;
    CorpState.Get();CorpState.Save();
    if(SaveSlot.CanWrite("CorpPresence")||PlayerStore.Instance.modData["CorpPresence"]!=raw)throw new Exception("Invalid waste battery state was overwritten");
}
Console.WriteLine("6 passed: waste battery defaults, three-state roundtrip and invalid-state write protection");

foreach(int giftStatus in new[]{0,1,2}){
    PlayerStore.Instance=new PlayerStore();CorpState.Current=null;
    var giftState=CorpState.Get();
    if(giftState.UfoBackpackGiftStatus!=0)throw new Exception("Old saves must default ufo backpack to unclaimed");
    giftState.UfoBackpackGiftStatus=giftStatus;CorpState.Save();CorpState.Current=null;
    if(CorpState.Get().UfoBackpackGiftStatus!=giftStatus)throw new Exception("UFO backpack gift status lost on load");
}
foreach(int bad in new[]{-1,3,int.MaxValue}){
    PlayerStore.Instance=new PlayerStore();CorpState.Current=null;
    string raw="{\"RunId\":\"test\",\"UfoBackpackGiftStatus\":"+bad+"}";
    PlayerStore.Instance.modData["CorpPresence"]=raw;
    CorpState.Get();CorpState.Save();
    if(SaveSlot.CanWrite("CorpPresence")||PlayerStore.Instance.modData["CorpPresence"]!=raw)throw new Exception("Invalid ufo backpack state was overwritten");
}
Console.WriteLine("6 passed: ufo backpack defaults, three-state roundtrip and invalid-state write protection");
foreach(int giftStatus in new[]{0,1,2}){
    PlayerStore.Instance=new PlayerStore();CorpState.Current=null;
    var giftState=CorpState.Get();
    if(giftState.FufuDollGiftStatus!=0)throw new Exception("Old saves must default fufu doll to unclaimed");
    giftState.FufuDollGiftStatus=giftStatus;CorpState.Save();CorpState.Current=null;
    if(CorpState.Get().FufuDollGiftStatus!=giftStatus)throw new Exception("Fufu doll gift status lost on load");
}
foreach(int bad in new[]{-1,3,int.MaxValue}){
    PlayerStore.Instance=new PlayerStore();CorpState.Current=null;
    string raw="{\"RunId\":\"test\",\"FufuDollGiftStatus\":"+bad+"}";
    PlayerStore.Instance.modData["CorpPresence"]=raw;
    CorpState.Get();CorpState.Save();
    if(SaveSlot.CanWrite("CorpPresence")||PlayerStore.Instance.modData["CorpPresence"]!=raw)throw new Exception("Invalid fufu doll state was overwritten");
}
Console.WriteLine("6 passed: fufu doll defaults, three-state roundtrip and invalid-state write protection");
