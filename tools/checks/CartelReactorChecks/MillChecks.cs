using CorpPresence;

internal static class MillChecks
{
    private static void Check(bool result, string what)
    { if (!result) throw new Exception(what); }

    private static MachineryButton Button(GameItem machine, int index) =>
        ((object[])machine.contentWindow.childElement.GetPanel("control").Content).OfType<MachineryButton>().ElementAt(index);

    internal static void Run(GameItem precipitate)
    {
        const string crystal = "corp_presence_light_salt_grit";
        var mill = PowderClassifier.Definition.Shell();
        mill.identifier = "corp_presence_powder_classifier";
        var sleeve = mill.Slot("tool").Put(Goods.Make(ClassifierGoods.Sleeve));
        var power = mill.Slot("power").Put(new GameItem("energy_credit"){Charge=60});
        var feed = mill.Slot("feed");
        Check(feed.Capacity==54 && mill.Slot("output").Capacity==54,"Classifier input/output grids");
        foreach(string key in new[]{ClassifierGoods.Crystal,ClassifierGoods.Fine,ClassifierGoods.Coarse})
        {
            var bag=Goods.Make(key);
            ClassifierGoods.Definitions[key].Setup(bag);
            Check(bag.maxSlotAmountItemFunc(bag,null,null)==10 && bag.mayStackItemFunc(bag,Goods.Make(key))
                && !bag.mayStackItemFunc(bag,sleeve),"Separate bag identities and ten-unit stacks");
        }
        precipitate.parentInventory.childItems.Remove(precipitate);
        feed.Put(precipitate);
        StoreSession.CompanyProgressPaused = true;
        Button(mill,0).Click();
        mill.Night();
        Check(mill.GetTagReadonly("CORP_CLASSIFIER_MODE").Number==0 && power.Charge==60 && precipitate.unitCount==1,
            "Paused store preserves mode and stock");
        StoreSession.CompanyProgressPaused = false;
        Button(mill, 0).Click(); // Fine.
        Button(mill, 0).Click(); // Blueprint not delivered yet: back to flow.
        Check(mill.GetTagReadonly("CORP_CLASSIFIER_MODE").Number == 0, "Undiscovered crystal mode");
        CartelShizuka.Delivered = true;
        Button(mill, 0).Click();
        Button(mill, 0).Click();
        power.Charge=7;
        mill.Night();
        Check(!PowderClassifier.HasActiveBatch(mill) && precipitate.unitCount==1 && power.Charge==7, "No charge preserves feed");
        power.Charge=16;
        mill.Slot("output").Capacity=1;
        mill.Night();
        Check(!PowderClassifier.HasActiveBatch(mill) && precipitate.unitCount==1 && power.Charge==16, "Full output before start");
        mill.Slot("output").Capacity=2;
        mill.Night();
        Check(precipitate.unitCount==0 && power.Charge==8 && PowderClassifier.HasActiveBatch(mill), "Paid one night");
        Check(!mill.Slot("tool").mayInventoryRemoveItemFunc(sleeve,mill.Slot("tool")), "Captive sleeve during batch");
        Button(mill, 0).Click();
        Check(mill.GetTagReadonly("CORP_CLASSIFIER_MODE").Number==2, "Mode freeze");
        // A newly constructed shell receives only the game's persisted tags and inventory.
        var saved=mill;
        mill=PowderClassifier.Definition.Shell();
        mill.identifier=saved.identifier;
        foreach(var tag in saved.Tags)
            mill.ModifyTag(tag.Key,t=>t.SetInt(tag.Value.Number).SetString(tag.Value.Text));
        foreach(var key in new[]{"power","tool"})
        {
            var item=saved.Slot(key).childItems.Single();
            saved.Slot(key).childItems.Remove(item);
            mill.Slot(key).Put(item);
        }
        mill.onLoaded(mill);
        Check(!mill.Slot("tool").mayInventoryRemoveItemFunc(sleeve,mill.Slot("tool")), "Captive after reload");
        var output=mill.Slot("output");
        output.Capacity=1;
        mill.Night();
        Check(power.Charge==0 && output.childItems.Count==0 && PowderClassifier.HasActiveBatch(mill), "Mature while full");
        mill.Night();
        Check(power.Charge==0 && output.childItems.Count==0, "Full retries free");
        output.Capacity=2;
        mill.Night();
        Check(!PowderClassifier.HasActiveBatch(mill) && output.childItems.Count==2 && output.childItems.All(i=>i.identifier==crystal),
            "Whole batch delivered without another night of power");

        var bench=CraftingStation.Definition.Shell();
        var choose=Button(bench,0);
        var assemble=Button(bench,1);
        var workInputs=(GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content;
        choose.Click(); // Screen recipe.
        workInputs[0].Put(Goods.Make(ClassifierGoods.Coarse));
        workInputs[1].Put(Goods.Make(CraftingStation.SiliconPlate));
        bench.Slot("output").Capacity=0;
        assemble.Click();
        Check(workInputs[0].childItem!=null && workInputs[1].childItem!=null, "Workbench receipts preflight");
        bench.Slot("output").Capacity=1;
        workInputs[1].childItem.AfterDestroy=()=>throw new InvalidOperationException("plate removal callback");
        assemble.Click();
        var screen=bench.Slot("output").childItems.Single();
        Check(screen.identifier=="corp_presence_fine_screen" && workInputs[0].childItem==null && workInputs[1].childItem==null,
            "Fine screen uses beginner materials");
        choose.Click(); // Refurbish.
        mill.Slot("tool").childItems.Remove(sleeve);
        workInputs[0].Put(sleeve);
        var powder=output.childItems[0];
        output.childItems.Remove(powder);
        workInputs[1].Put(powder);
        assemble.Click();
        Check(ClassifierGoods.Charges(sleeve)==3 && powder.unitCount==0 && workInputs[0].childItem==sleeve,
            "Refurbished original sleeve");
        var spare=output.childItems.Single();
        output.childItems.Remove(spare);
        workInputs[1].Put(spare);
        assemble.Click();
        Check(ClassifierGoods.Charges(sleeve)==3 && spare.unitCount==1, "Full sleeve refuses unnecessary powder");

        workInputs[0].childItems.Remove(sleeve);
        mill.Slot("tool").Put(sleeve);
        var four=mill.Slot("feed").Put(Goods.Make(CartelReactor.Crystal));
        four.unitCount=4;
        power.Charge=40;
        for(int completed=1;completed<=3;completed++)
        {
            mill.Night();
            Check(four.unitCount==4-completed && ClassifierGoods.Charges(sleeve)==3-completed
                && output.childItems.Count==2 && power.Charge==40-8*completed,"Fast batch "+completed);
            foreach(var product in output.childItems.ToArray())product.Destroy();
        }
        mill.Night();
        Check(output.childItems.Count==0 && power.Charge==8 && four.unitCount==0,"Fourth batch normal speed");
        mill.Night();
        Check(output.childItems.Count==2 && power.Charge==0 && !PowderClassifier.HasActiveBatch(mill),"Fourth batch delivered");
        foreach(var product in output.childItems.ToArray())product.Destroy();

        mill.Slot("tool").childItems.Remove(sleeve);
        bench.Slot("output").childItems.Remove(screen);
        mill.Slot("tool").Put(screen);
        var box=mill.Slot("silica").Put(Goods.Make("powder_box"));
        box.Parts=new[]{0,150,0,0,20};
        output.Capacity=3;
        Button(mill,0).Click(); // Throughput.
        power.Charge=24;
        PowderBox.FailPowderRedraw=true;
        try { mill.Night(); } catch(InvalidOperationException e) when(e.Message=="injected powder redraw") {}
        finally { PowderBox.FailPowderRedraw=false; }
        Check(box.Parts.SequenceEqual(new[]{0,90,0,0,20}) && power.Charge==16
            && output.childItems.Count(i=>i.identifier=="corp_presence_coarse_silica_grit")==2
            && output.childItems.Count(i=>i.identifier=="corp_presence_fine_silica_grit")==1,"Exact throughput batch");
        foreach(var product in output.childItems.ToArray())product.Destroy();
        Button(mill,0).Click();
        mill.Night(); mill.Night();
        Check(box.Parts.SequenceEqual(new[]{0,30,0,0,20}) && power.Charge==0
            && output.childItems.Count(i=>i.identifier=="corp_presence_coarse_silica_grit")==1
            && output.childItems.Count(i=>i.identifier=="corp_presence_fine_silica_grit")==2,"Fine batch after fitting crafted screen");
        foreach(var product in output.childItems.ToArray())product.Destroy();
        mill.Slot("tool").childItems.Remove(screen);
        mill.Slot("tool").Put(sleeve);
        Button(mill,0).Click();
        var fragile=mill.Slot("feed").Put(Goods.Make(CartelReactor.Crystal));
        fragile.BeforeDestroy=()=>throw new InvalidOperationException("before debit");
        power.Charge=16;
        mill.Night();
        Check(power.Charge==8 && fragile.unitCount==1 && output.childItems.Count==0
            && mill.GetTagReadonly("CORP_CLASSIFIER_BATCH").Text=="charge:crystal.v1","Input callback before debit retains paid night");
        fragile.BeforeDestroy=null;
        fragile.AfterDestroy=()=>throw new InvalidOperationException("after debit");
        mill.Night();
        Check(power.Charge==8 && fragile.unitCount==0 && output.childItems.Count==0 &&
            mill.GetTagReadonly("CORP_CLASSIFIER_NIGHTS").Number==1,"Prepaid input retry preserves second night");
        mill.Night();
        Check(power.Charge==0 && output.childItems.Count==2 && !PowderClassifier.HasActiveBatch(mill),"Exactly one complete paid batch");
        Console.WriteLine("PASS actual classifier and workbench: blueprint gate, restart, whole-batch delivery, three fast cycles, silica modes.");
    }
}

namespace CorpPresence
{
    internal class GenericConfirmationUIManager
    {
        internal static GenericConfirmationUIManager Instance=>null;
        internal void OpenPanelSingleButton(string a,string b,Action click){}
        internal void OpenPanel(string message, string ok, string cancel, Action accept, Action reject){}
        internal void ClosePanel(){}
    }
}
