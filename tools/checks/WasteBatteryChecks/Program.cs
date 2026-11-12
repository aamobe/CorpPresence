using CorpPresence;
void Check(bool ok, string text) { if (!ok) throw new Exception(text); Console.WriteLine("PASS " + text); }
GameItem Battery(int charge) { var b = new GameItem { identifier = WasteBattery.ItemId }; b.ModifyTag("power_source_item_energy", t => t.SetInt(charge)); WasteBattery.Setup(b); return b; }
var battery = Battery(0);
var junk = new GameItem { identifier = "junk", unitCount = 3 };
Check(battery.canItemTargetThisFunc(junk, battery), "accept native junk");
battery.onItemTargetThisFunc(junk, battery);
Check(junk.unitCount == 2 && battery.GetTagReadonly("power_source_item_energy").GetInt() == 10, "one junk adds ten energy");
WasteBattery.Setup(battery); battery.onItemTargetThisFunc(junk, battery);
Check(junk.unitCount == 1 && battery.GetTagReadonly("power_source_item_energy").GetInt() == 20, "reattach does not duplicate consumption");
battery.onItemTargetThisFunc(junk, battery);
Check(junk.destroyed && battery.GetTagReadonly("power_source_item_energy").GetInt() == 30, "last unit consumed");
foreach (var charge in new[] { 1991, 2000 }) {
  var full = Battery(charge); var fuel = new GameItem { identifier = "junk" };
  full.onItemTargetThisFunc(fuel, full);
  Check(!fuel.destroyed && full.GetTagReadonly("power_source_item_energy").GetInt() == charge, "insufficient room consumes nothing " + charge);
}
var almost = Battery(1990); almost.onItemTargetThisFunc(new GameItem { identifier = "junk" }, almost);
Check(almost.GetTagReadonly("power_source_item_energy").GetInt() == 2000, "exact capacity");
Check(!battery.canItemTargetThisFunc(new GameItem { identifier = "scrap_metal" }, battery), "reject other waste");
Check(!battery.canItemTargetThisFunc(new GameItem { identifier = "junk", Owned = false }, battery), "reject unowned fuel");
SaveSlot.Writable = false;
Check(!battery.canItemTargetThisFunc(new GameItem { identifier = "junk" }, battery), "protected save cannot consume");
SaveSlot.Writable = true;
var failed = new GameItem { identifier = "junk", FailConsume = true };
try { battery.onItemTargetThisFunc(failed, battery); } catch (InvalidOperationException) { }
Check(battery.GetTagReadonly("power_source_item_energy").GetInt() == 30, "failed consumption gives no energy");
Check(WasteBattery.BlocksCharging(battery) && !WasteBattery.BlocksCharging(new GameItem { identifier = "energy_credit" }), "charging filter limited to waste battery");
var native = new GameItem { identifier = "energy_credit" };
native.ModifyTag("power_source_item", t=>t.Enable());
bool NativeTargetAggregation(bool? sourceDecision, bool? targetDecision) => (sourceDecision | targetDecision) == true && (sourceDecision & targetDecision) != false;
Check(NativeTargetAggregation(true, battery.canItemTargetThisFunc(native,battery)), "native battery swap accepted in target direction");
Check(!NativeTargetAggregation(false, battery.canItemTargetThisFunc(native,battery)), "native battery swap refusal preserved");

bool AnyDecision(Func<GameItem,GameItem,bool> callbacks, GameItem source, GameItem target) => callbacks.GetInvocationList().Cast<Func<GameItem,GameItem,bool>>().Any(f=>f(source,target));
GameItem NativeJunk() {
 var fuel = new GameItem { identifier = "junk", unitCount = 2 };
 fuel.mayThisTargetItemFunc += (s,t)=>t.identifier=="repairable_reactor";
 fuel.canThisTargetItemFunc += (s,t)=>t.identifier=="repairable_reactor";
 fuel.onThisTargetItemFunc += (s,t)=>{if(t.identifier=="repairable_reactor")s.Destroy();};
 return fuel;
}
var realFuel = NativeJunk(); var empty = Battery(0);
WasteBattery.AfterMakeJunk(realFuel); WasteBattery.AfterMakeJunk(realFuel);
Check(NativeTargetAggregation(AnyDecision(realFuel.canThisTargetItemFunc,realFuel,empty),empty.canItemTargetThisFunc(realFuel,empty)), "native junk source and battery target both permit feeding");
Check(AnyDecision(realFuel.mayThisTargetItemFunc,realFuel,empty), "native junk hover accepts battery");
Check(realFuel.canThisTargetItemFunc.GetInvocationList().Length==2, "junk setup is idempotent");
realFuel.onThisTargetItemFunc(realFuel,empty); empty.onItemTargetThisFunc(realFuel,empty);
Check(realFuel.unitCount==1 && !realFuel.destroyed && empty.GetTagReadonly("power_source_item_energy").GetInt()==10, "native repair action does not consume battery fuel twice");
var reactor=new GameItem{identifier="repairable_reactor"};
Check(AnyDecision(realFuel.canThisTargetItemFunc,realFuel,reactor), "native reactor repair preserved");
Check(!AnyDecision(realFuel.canThisTargetItemFunc,realFuel,new GameItem{identifier="other"}), "unrelated target still rejected");
Check(!AnyDecision(realFuel.canThisTargetItemFunc,realFuel,Battery(2000)), "source rejects full battery");