using CorpPresence;

void Check(bool ok, string reason) { if (!ok) throw new Exception(reason); }
var plan = new OverhaulPlan(new(), 2, 4, 2, false, 0);
Check(plan.Powder.SequenceEqual(new[]{90,120,28,40,40}), "mixed-line raw inputs");
Check(plan.Ingots == 4 && plan.Moss == 3 && plan.Resin == 2 && plan.Power == 194 && plan.Water == 440, "base consumption");
Check(plan.ExtraIngotMetal == 480 && plan.ExtraIngotNights == 4, "separate ingot estimate");
var finished = new OverhaulPlan(new(){[Part.Frame]=2,[Part.Contact]=4,[Part.Unit]=2}, 2,4,2,false,0);
Check(finished.Power == 0 && finished.Water == 0 && finished.Ingots == 0 && finished.Powder.Sum()==0, "finished stock");
var split = new OverhaulPlan(new(){[Part.Contact]=4,[Part.Grit]=2,[Part.Gel]=2,[Part.Blank]=2}, 2,4,2,true,1);
Check(split.FrameOrders==3 && split.FrameFee==105 && split.Water==0 && split.Moss==0 && split.Powder[4]==0, "incoming frames and finished components");
var stock = new Dictionary<Part,int>{[Part.Ingot]=4,[Part.Moss]=3,[Part.Resin]=2};
var dates = new OverhaulPlan(stock,2,4,2,false,0).Estimate(false);
Check(dates.First==2 && dates.Second==5, "planned loads D+2 and D+5");
Console.WriteLine("PASS overhaul material balance, alternative frame routes and dispatch-day estimates");

var unknownMill = new OverhaulQueue();
unknownMill.Observe(OverhaulBay.Mill,-1);
var stocked = new Dictionary<Part,int>{{Part.Frame,2},{Part.Contact,4},{Part.Unit,2}};
Check(new OverhaulPlan(stocked,2,4,2,false,0,unknownMill).Estimate(false) == (0,0),
    "an unrelated unconfirmed production cannot block either stocked truck");
var oneReady = new Dictionary<Part,int>{{Part.Frame,2},{Part.Contact,4}};
Check(new OverhaulPlan(oneReady,2,4,2,false,0,unknownMill).Estimate(false) == (0,-1),
    "show a ready first truck even if the second has no release date");

var working = new OverhaulQueue();
working.Observe(OverhaulBay.Mill,2,16,0,(Part.Grit,2));
var finish = new Dictionary<Part,int>{{Part.Frame,2},{Part.Gel,2},{Part.Blank,2}};
var useCrystals = new OverhaulPlan(finish,0,0,2,false,0,working);
Check(useCrystals.CrystalRuns == 0 && useCrystals.WhiteRuns == 0 && useCrystals.Moss == 0
    && useCrystals.Estimate(false) == (0,2), "confirmed crystal batch replaces upstream demand and becomes available only on recorded day");

var occupied = new OverhaulQueue();
occupied.Observe(OverhaulBay.Mill,2,16,0,(Part.Grit,2));
var wait = new OverhaulPlan(new(),0,2,0,false,0,occupied);
Check(wait.Estimate(false).First == 3, "paid crystal run occupies mill until day two before the sand run");
occupied.Observe(OverhaulBay.Mill,0);
Check(wait.Estimate(false).First == 1, "an idle second classifier can take the reserved new work");
var due = new OverhaulPlan(new(),1,0,0,false,1);
Check(due.Estimate(false,new Dictionary<int,int>{{0,1}}) == (0,0), "paid frames due today can arrive on day zero");
Check(due.Estimate(false,new Dictionary<int,int>{{2,1}}) == (2,2), "in transit stock waits for its recorded date");

var prepaid = new OverhaulEscrow(2,2,2,25);
var resume = new OverhaulPlan(new(),2,0,0,true,0,null,prepaid);
Check(resume.Ingots == 0 && resume.AlloyRuns == 0 && resume.FrameFee == 45 && resume.FrameOrders == 2
    && resume.Estimate(false) == (2,2), "resume the exact outstanding commission and charge only the remaining fee");
Check(resume.Estimate(false,canOrderToday:false) == (3,3), "today's reserved ordering allowance delays completion");
var partial = new OverhaulPlan(new(){{Part.Alloy,1}},1,0,0,true,0,null,new OverhaulEscrow(2,2,1,0));
Check(partial.FrameOrders == 2 && partial.Ingots == 0 && partial.Regrinds == 0 && partial.Estimate(false) == (2,2),
    "a fixed two-frame order cannot be resized when only one frame is needed");
var extra = new OverhaulPlan(new(){{Part.Moss,3},{Part.Resin,2}},2,4,2,false,0);
Check(!extra.InputsReady(new long[]{570,120,28,40,40},0,true), "81-percent metal cannot cover a pure smelting budget");
Check(!extra.InputsReady(new long[]{480,120,28,40,40},480,true), "pure metal cannot count twice for alloy and smelting");
Check(extra.InputsReady(new long[]{570,120,28,40,40},480,true), "separate pure and total metal requirements can both be met");
Check(extra.Estimate(false,supplyMissingIngots:true)==(2,5), "show the schedule conditional on supplying missing ingots");
Console.WriteLine("PASS occupied, mature and unknown queues, independent trucks, due-today delivery, partial escrow, pure-metal budgets");

var expansion = new OverhaulPlan(new(){[Part.Frame]=4},4,0,0,false,0, cargo:new RotationCargo(Pads:2));
Check(expansion.Pads==2 && expansion.SandRuns==1 && expansion.AlloyRuns==1 && expansion.Regrinds==1 &&
    expansion.Estimate(false)==(2,2), "expansion reserves two pads and delays loading until both are made");
var supply = new OverhaulPlan(new(){[Part.Grit]=2,[Part.Moss]=2,[Part.Resin]=2},0,0,0,false,0,
    cargo:new RotationCargo(Fine:3,Liners:2,Gel:2));
Check(supply.BlueRuns==2 && supply.Liners==2 && supply.SandRuns==3 && supply.Estimate(false)==(3,3),
    "supply truck waits for actual fine grit, liners and gel even without frames or assemblies");
var repair = new OverhaulPlan(new(){[Part.Contact]=2,[Part.Gel]=2,[Part.Blank]=2,[Part.Liner]=2},0,2,2,false,0,
    cargo:new RotationCargo(ReusedShells:2));
Check(repair.Frames==0 && repair.Estimate(false)==(0,0),"two usable same-contract shells replace two frames for repair only");
var joint = new OverhaulPlan(new(){[Part.Contact]=4,[Part.Frame]=2,[Part.Gel]=2,[Part.Blank]=2,[Part.Liner]=2},2,4,2,false,0);
Check(joint.Frames==2,"joint overhaul still needs two new frames for assemblies");
Console.WriteLine("PASS four rotation cargo bills, repair frame substitution and single-truck dispatch timing");
