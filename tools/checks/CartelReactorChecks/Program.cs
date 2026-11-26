using System;
using CorpPresence;

static void Check(bool passed) { if (!passed) throw new Exception("Batch state assertion failed"); }
Check(CartelBatch.Advance(0) == 0);
Check(CartelBatch.Advance(1) == 2);
Check(CartelBatch.Advance(2) == 2);
Check(CartelBatch.AfterOutput(2, false) == 2);
Check(CartelBatch.AfterOutput(2, true) == 0);
Console.WriteLine("5 cartel batch checks passed.");

var reactor = CartelReactor.Defs[CartelReactor.Key].Shell();
reactor.identifier = "corp_presence_cartel_reactor";
GameItem Load(GameItem machine,string slot,string id,int count=1) =>
    machine.Slot(slot).Put(new GameItem(id){unitCount=count});
var cell = Load(reactor,"power","energy_credit");
var blocks = Load(reactor,"moss",SleepMoss.BlockId,2);
var resin = Load(reactor,"resin","corp_presence_blue_shell_resin");
cell.Charge = 7;
reactor.Night();
Check(blocks.unitCount == 2 && resin.unitCount == 1 && cell.Charge == 7);
cell.Charge = 8;
reactor.Slot("blue_output").Capacity = 0;
reactor.Night();
Check(blocks.unitCount == 2 && resin.unitCount == 1 && cell.Charge == 8);
reactor.Slot("blue_output").Capacity = 1;
MachineHelper.ThrowAfterDraw = true;
blocks.FailCountRedraw = true;
reactor.Night();
MachineHelper.ThrowAfterDraw = false;
Check(reactor.Slot("blue_output").childItems.Single().identifier == "corp_presence_sleep_moss_gel"
    && blocks.unitCount == 1 && resin.unitCount == 0 && cell.Charge == 0);
ShizukaSaleCheck.Run(reactor);

var growth = MineralGrowthStation.Definition.Shell();
growth.identifier = "corp_presence_" + MineralGrowthStation.Key;
var water = LoadWater(growth, 40000);
GameItem LoadWater(GameItem station,int volume)
{
    var vessel = new GameItem("test_water_container");
    vessel.EnableTag("LIQUID_CONTAINER_TAG");
    vessel.ModifyTag("water",t=>t.SetInt(volume));
    return station.Slot("water").Put(vessel);
}
var primary = Load(growth,"input","corp_presence_powder_box");
primary.Parts = new[]{0,0,0,0,40};
var secondary = Load(growth,"secondary","corp_presence_powder_box");
secondary.Parts = new[]{0,0,10,0,0};
var blueprint = Load(growth,"blueprint","corp_presence_light_salt_blueprint");
var growthCell = Load(growth,"power","energy_credit");
growthCell.Charge = 5;
growth.Night();
Check(water.GetTagReadonly("water").GetInt()==40000 && primary.Parts[4]==40 && secondary.Parts[2]==10 && growthCell.Charge==5);
growthCell.Charge=12;
WaterFeatureHelper.FailRedraw = true;
try { growth.Night(); } catch (InvalidOperationException ex) when (ex.Message == "water redraw") { }
WaterFeatureHelper.FailRedraw = false;
Check(MineralGrowthStation.HasActiveBatch(growth) && growth.GetTagReadonly("CORP_MINERAL_GROWTH_PROGRESS").GetInt() > 0);
Check(water.GetTagReadonly("water").GetInt()==20000 && primary.Parts[4]==0 && secondary.Parts[2]==0 && growthCell.Charge==6);
blueprint.Destroy();
growth.Slot("output").Capacity=1;
growth.Night();
Check(water.GetTagReadonly("water").GetInt()==0 && growthCell.Charge==0 && growth.Slot("output").childItems.Count==0);
growth.Night();
Check(growth.Slot("output").childItems.Count==0);
growth.Slot("output").Capacity=2;
GraphUtils.AfterAccept = (inventory, item) =>
{
    if (inventory.childItems.Count == 2) throw new InvalidOperationException("core accepted before redraw");
};
growth.Night();
GraphUtils.AfterAccept = null;
var cores=growth.Slot("output").childItems;
Check(!MineralGrowthStation.HasActiveBatch(growth) && cores.Count==2 && cores.All(i=>i.identifier=="corp_presence_light_salt_core"));
var firstCore=cores[0]; cores.RemoveAt(0); firstCore.parentInventory=null;
reactor.Slot("salt").Put(firstCore);
cell.Charge=8;
reactor.Night();
Check(blocks.unitCount==0 && firstCore.unitCount==0 && cores.Count==1 && cell.Charge==0);
var precipitate=reactor.Slot("white_output").childItems.Single();
Check(precipitate.identifier=="corp_presence_light_salt_precipitate");
Console.WriteLine("PASS blue gel and white precipitate through actual reactor/growth night callbacks, 8/6 charge boundaries and full-output recovery.");
var old = CartelReactor.Defs[CartelReactor.Key].Shell();
old.identifier = reactor.identifier;
old.ModifyTag("CORP_CARTEL_BLUE_BATCH", t=>t.SetInt(1));
old.ModifyTag("CORP_CARTEL_WHITE_BATCH", t=>t.SetInt(2));
var oldCell = Load(old, "power", "energy_credit");
oldCell.Charge=8;
old.Slot("blue_output").Capacity=0;
old.Night();
Check(CartelReactor.HasLegacyBatch(old) && oldCell.Charge==8 && old.Slot("white_output").childItems.Count==1);
old.Slot("blue_output").Capacity=1;
old.Night();
old.Night();
Check(!CartelReactor.HasLegacyBatch(old) && oldCell.Charge==0
    && old.Slot("blue_output").childItems.Count==1 && old.Slot("white_output").childItems.Count==1);
Console.WriteLine("PASS loaded legacy blue and white batches retain blocked stock and settle once.");
MillChecks.Run(precipitate);
var preparedAlcohol = AlcoholChecks.Run();
HeavyScreenChecks.Run();
IndustrialChecks.Run();
RotationJourneyChecks.Run(preparedAlcohol);
OverhaulSnapshotChecks.Run();
SieveGoalChecks.Run();
