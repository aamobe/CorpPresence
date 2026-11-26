using CorpPresence;
using System.Text.Json;

internal static class IndustrialChecks
{
    private static void Check(bool result, string message)
    { if (!result) throw new Exception(message); }

    private static MachineryButton Button(GameItem machine, int index = 0)
    {
        var content = machine.contentWindow.childElement.GetPanel("control").Content;
        return (content is MachineryTabs tabs ? tabs.Panels[0].Items : (object[])content)
            .OfType<MachineryButton>().ElementAt(index);
    }

    private static void Select(GameItem machine, string tag, string recipe)
    {
        int steps = SinteringRecipes.All.Count;
        if (tag == "CORP_CRAFT_MILL_RECIPE")
        {
            var cycle = IndustrialWorkbench.CycleFor(recipe);
            if (!ReferenceEquals(cycle, IndustrialWorkbench.CycleFor(machine.GetTagReadonly(tag).GetString())))
            {
                var control = (object[])machine.contentWindow.childElement.GetPanel("control").Content;
                var groups = control.OfType<MachineryStack>().Single().Items.OfType<MachineryButton>().ToArray();
                groups[ReferenceEquals(cycle, IndustrialWorkbench.BasicCycle) ? 0 :
                    ReferenceEquals(cycle, IndustrialWorkbench.FormingCycle) ? 1 : 2].Click();
            }
            steps = cycle.Length;
        }
        for (int i = 0; i < steps && machine.GetTagReadonly(tag).GetString() != recipe; i++) Button(machine).Click();
        Check(machine.GetTagReadonly(tag).GetString() == recipe, "Recipe unavailable: " + recipe);
    }

    private static GameItem Move(GameItem item, GameInventory target)
    {
        item.parentInventory?.Expel(item);
        return target.Put(item);
    }

    private static GameItem Cell(GameItem machine, int charge) =>
        machine.Slot("power").Put(new GameItem("energy_credit") { Charge = charge });

    private static GameItem Box(GameInventory slot, params int[] parts)
    {
        var box = Goods.Make("powder_box");
        box.Parts = parts;
        return slot.Put(box);
    }

    private static GameItem One(GameItem machine, string key, string panel = "output") =>
        machine.Slot(panel).childItems.First(item => Goods.IdOf(item) == key);

    private static void MixedCrusherNight()
    {
        var machine = MineralCrusher.Definition.Shell();
        var input = machine.Slot("input");
        var contact = input.Put(Goods.Make(IndustrialGoods.SpentContact));
        contact.unitCount = 2;
        var billet = input.Put(Goods.Make(IndustrialGoods.Billet));
        billet.unitCount = 2;
        var cell = Cell(machine, 12);
        var box = Box(machine.Slot("receiver"), 0, 0, 460, 0, 0);
        machine.Night();
        Check(cell.Charge == 0 && contact.unitCount == 0 && billet.unitCount == 1
            && box.Parts.SequenceEqual(new[] { 0, 0, 480, 20, 0 })
            && machine.Slot("output").childItems.Count == IndustrialRecipes.AlloyYield,
            "Two spent contacts and one billet share one 12-charge three-item night, including a full powder box");
    }

    internal static void Run()
    {
        TrialMold();
        RodTreatment();
        RodVisualProfiles();
        RodFormingJourney();
        FeederJourney();
        var handbook = FeederTrialJourney();
        CompositeJourney(handbook);
        WetContactJourney(handbook);
        SieveReturnJourney(handbook);
        SinterRetrofitJourney(handbook);
        ClassifierRetrofitJourney(handbook);
        MineralizerRetrofitJourney(handbook);
        var rodStation = SinteringStation.Definition.Shell();
        var rodCell = Cell(rodStation, 12);
        var rodInput = (GameSlotInventory[])rodStation.contentWindow.childElement.GetPanel("input").Content;
        var rodMetal = Box(rodInput[0], 60, 0, 0, 0, 5);
        var rodSilica = Box(rodInput[1], 0, 12, 0, 0, 2);
        Select(rodStation, "CORP_SINTERING_RECIPE", SinteringRecipes.StandardRod);
        var rodOutput = rodStation.Slot("output");
        rodOutput.Capacity = 1;
        rodStation.Night();
        Check(rodOutput.childItems.Count == 0 && rodCell.Charge == 12
            && rodMetal.Parts[0] == 60 && rodSilica.Parts[1] == 12, "An incomplete pair consumes nothing");
        rodOutput.Capacity = 2;
        rodStation.Night();
        Check(rodOutput.childItems.Count == 2 && rodOutput.childItems.All(r => Goods.IdOf(r) == SinteringRecipes.StandardRod)
            && rodCell.Charge == 0 && rodMetal.Parts.SequenceEqual(new[] {0,0,0,0,5})
            && rodSilica.Parts.SequenceEqual(new[] {0,0,0,0,2}), "Two rods settle the exact powder and one charge");
        foreach (var rod in rodOutput.childItems)
        {
            string savedMaterial = rod.GetTagReadonly(RodMaterial.Tag).GetString();
            Check(savedMaterial == "1|60|12|0|0|0", "Each rod stores its share of the consumed powder");
            var restored = DirectoryMaster.Item(rod.identifier, isOwned: true);
            Check(RodMaterial.Read(restored) == RodMaterial.Read(rod), "Untagged old rods use the original fixed mold");
            restored.ModifyTag(RodMaterial.Tag, tag => tag.Enable().SetString(savedMaterial));
            var material = RodMaterial.Read(restored);
            Check(material != null && material.CastProperties == new RodProperties(66,33,0,16,6,0,53),
                "Read saved mass as normalized material properties");
            restored.ModifyTag(RodMaterial.Tag, tag => tag.Enable().SetString("2|60|12|0|0|0"));
            Check(RodMaterial.Read(restored) == null, "An unknown material version never masquerades as an old fixed mold");
        }
        var wearRod = DirectoryMaster.Item("corp_presence_" + SinteringRecipes.WearRod, isOwned: true);
        Check(RodMaterial.Read(wearRod) == new RodMaterial(45,0,5,0,0), "An old wear rod retains half-point masses");
        Check(RodMaterial.ForOutput(new[] { 45,0,5,0,0 }, 2) == RodMaterial.Read(wearRod),
            "Splitting 45 metal and 5 rare between two rods conserves mass");

        CorpState.Current.Characters["rosen"].SideDone.Add(5);
        int signedDay = StoreStation.Day;
        Check(new FirstOverhaulQuote().Sign() == SaleStatus.Ready &&
            CorpState.Current.Workshop.FirstOverhaul.SignedDay == signedDay, "Sign before preparing materials");
        MixedCrusherNight();
        var sinter = SinteringStation.Definition.Shell();
        var crusher = MineralCrusher.Definition.Shell();
        var sinterCell = Cell(sinter, 48);
        var crusherCell = Cell(crusher, 24);
        var sinterSlots = (GameSlotInventory[])sinter.contentWindow.childElement.GetPanel("input").Content;
        var metal = Box(sinterSlots[0], 90, 0, 0, 0, 9);
        var rare = Box(sinterSlots[1], 0, 0, 10, 0, 1);
        Select(sinter, "CORP_SINTERING_RECIPE", IndustrialGoods.Billet);
        for (int i = 0; i < 2; i++)
        {
            sinter.Night();
            Move(One(sinter, IndustrialGoods.Billet), crusher.Slot("input"));
            crusher.Slot("output").Capacity = 2 * i + 1;
            crusher.Night();
            Check(crusherCell.Charge == 24 - 12 * i && crusher.Slot("input").childItems.Count == 1,
                "A billet needs room for both bags before consumption or charge");
            crusher.Slot("output").Capacity++;
            crusher.Night();
        }
        Check(metal.Parts.SequenceEqual(new[]{0,0,0,0,9}) && rare.Parts.SequenceEqual(new[]{0,0,0,0,1})
            && sinterCell.Charge == 24 && crusherCell.Charge == 0, "Exact alloy composition and machine-night power");
        var alloy = crusher.Slot("output").childItems.ToArray();
        Check(alloy.Length == 4 && alloy.All(item=>Goods.IdOf(item)==IndustrialGoods.Alloy), "Four actual reground alloy bags");
        var frames = ForgeChecks.Frames(alloy);
        Check(frames.Length == 4 && frames.All(item=>Goods.IdOf(item)==IndustrialGoods.Frame), "Four frames received");

        var mill = PowderClassifier.Definition.Shell(); mill.identifier = "corp_presence_" + PowderClassifier.Key;
        var millPower = Cell(mill, 32);
        Box(mill.Slot("silica"), 0,120,0,0,12);
        mill.Night(); mill.Night();
        var bench = CraftingStation.Definition.Shell();
        var inputs = (GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content;
        void Choose(string key) => Select(bench, "CORP_CRAFT_MILL_RECIPE", key);
        void Assemble() => Button(bench, 1).Click();
        Choose(IndustrialGoods.Blend);
        var conductive = Box(inputs[1], 0,0,0,40,4);
        var blendRare = Box(inputs[2], 0,0,8,0,0);
        for (int i = 0; i < 2; i++)
        {
            Move(One(mill, ClassifierGoods.Fine), inputs[0]);
            Assemble();
        }
        Check(conductive.Parts.SequenceEqual(new[]{0,0,0,0,4}) && blendRare.Parts.SequenceEqual(new[]{0,0,0,0,0}),
            "Conductive blend consumed only its two named components");
        inputs[1].Expel(conductive); inputs[2].Expel(blendRare);
        Choose(IndustrialGoods.Contact);
        for (int i = 0; i < 2; i++)
        {
            var blend = Move(One(bench, IndustrialGoods.Blend), inputs[0]);
            bench.Slot("output").Capacity = bench.Slot("output").childItems.Count + 1;
            Assemble();
            Check(blend.unitCount == 1, "One free space cannot consume blend for two contacts");
            bench.Slot("output").Capacity = 24;
            if (i == 0)
            {
                int before = bench.Slot("output").childItems.Count;
                GraphUtils.AfterAccept = (_, _) => { if (blend.parentInventory == inputs[0]) inputs[0].Expel(blend); };
                try { Assemble(); }
                finally { GraphUtils.AfterAccept = null; }
                Check(blend.unitCount == 1 && bench.Slot("output").childItems.Count == before,
                    "If staged output moves the selected feed, roll back both contacts");
                inputs[0].Put(blend);
            }
            Assemble();
        }

        var store = EmporiumEntry.Instance;
        var rootInventory = store.frontInvinvElement;
        store.ListedOutput = rootInventory;
        foreach (var frame in frames.Take(2)) Move(frame, rootInventory);
        foreach (var item in bench.Slot("output").childItems.ToArray()) Move(item, rootInventory);
        int wallet = SaveSlot.CurrentStore.GetCash();
        StoreStation.Day = signedDay + 6;
        Check(new FirstOverhaulQuote().Submit() == SaleStatus.Paid && SaveSlot.CurrentStore.GetCash() == wallet + 360,
            "First truck pays for the real forged/commissioned frames and four assembled contacts");
        var saved = JsonSerializer.Serialize(CorpState.Current.Workshop);
        CorpState.Current.Workshop = JsonSerializer.Deserialize<WorkshopState>(saved);
        StoreStation.Day++;

        Select(sinter, "CORP_SINTERING_RECIPE", IndustrialGoods.Blank);
        for (int i = 0; i < 2; i++)
        {
            Move(One(mill, ClassifierGoods.Coarse), sinter.Slot("solid"));
            sinter.Night();
        }
        Check(sinterCell.Charge == 0 && mill.Slot("output").childItems.Count == 2, "Two blanks, two surplus coarse bags");

        var grow = MineralGrowthStation.Definition.Shell();
        grow.identifier = "corp_presence_" + MineralGrowthStation.Key;
        var water = new GameItem("test_water_container");
        water.EnableTag("LIQUID_CONTAINER_TAG"); water.ModifyTag("water", t => t.SetInt(40000));
        grow.Slot("water").Put(water); Cell(grow, 12);
        Box(grow.Slot("input"), 0,0,0,0,40);
        Box(grow.Slot("secondary"), 0,0,10,0,0);
        grow.Slot("blueprint").Put(Goods.Make(MineralGrowthBatch.LightSaltBlueprint));
        grow.Night(); grow.Night();
        var reactor = CartelReactor.Defs[CartelReactor.Key].Shell();
        reactor.identifier = "corp_presence_" + CartelReactor.Key;
        Cell(reactor, 24);
        reactor.Slot("moss").Put(new GameItem(SleepMoss.BlockId){unitCount=3});
        reactor.Slot("resin").Put(new GameItem("corp_presence_"+CartelReactor.Resin){unitCount=2});
        reactor.Night(); reactor.Night();
        Move(One(grow, CartelReactor.Salt), reactor.Slot("salt"));
        reactor.Night();
        Move(One(reactor, CartelReactor.Crystal, "white_output"), mill.Slot("feed"));
        mill.Slot("tool").Put(Goods.Make(ClassifierGoods.Sleeve));
        Button(mill).Click(); Button(mill).Click();
        mill.Night(); mill.Night();
        Check(millPower.Charge == 0 && grow.Slot("output").childItems.Count == 1, "Two normal grinding nights and one spare core");
        Choose(IndustrialGoods.Liner);
        for (int i = 0; i < 2; i++)
        {
            Move(One(mill, ClassifierGoods.Crystal), inputs[0]);
            Assemble();
        }
        rootInventory.Put(reactor);
        var gels = reactor.Slot("blue_output");
        gels.ParentItem = reactor;
        store.ListedOutput = gels;
        var competingGelOrder = new WorkshopSaleQuote(WorkshopSaleOffer.CartelDaily[0]);
        Check(competingGelOrder.Check() == SaleStatus.Ready, "Two produced gels qualify for today's Cartel order");
        Choose(IndustrialGoods.Unit);
        foreach (var frame in frames.Skip(2))
        {
            Move(frame, inputs[0]);
            Move(One(sinter, IndustrialGoods.Blank), inputs[1]);
            Move(One(bench, IndustrialGoods.Liner), inputs[2]);
            Move(One(reactor, CartelReactor.Gel, "blue_output"), bench.Slot("extra"));
            Assemble();
            var unit = One(bench,IndustrialGoods.Unit);
            Check(IndustrialGoods.IsFreshUnit(unit), "Only a new frame stamps a fresh assembly");
            Move(unit,rootInventory);
        }
        Check(competingGelOrder.Submit() != SaleStatus.Paid && SaveSlot.CurrentStore.GetCash() == wallet + 360,
            "Gels already assembled cannot also be sold to the Cartel that day");
        store.ListedOutput = rootInventory;
        StoreStation.Day = signedDay + 10;
        Check(!IndustrialGoods.IsFreshUnit(Goods.Make(IndustrialGoods.Unit)), "An unstamped filter must not qualify");
        Check(new FirstOverhaulQuote().Submit() == SaleStatus.Paid && SaveSlot.CurrentStore.GetCash() == wallet + 840,
            "Second truck consumes two real fresh assemblies after reload and pays once");
        PumpRepairJourney(handbook);
        BuiltPumpJourney(handbook);
        Console.WriteLine("PASS signed before production: sintering -> crusher -> four frames; silica, crystal and gel -> contacts and new assemblies; D+6 and D+10 payments across reload (+360/+480).");
    }

    private static void TrialMold()
    {
        var station = SinteringStation.Definition.Shell();
        var cell = Cell(station, 12);
        var input = (GameSlotInventory[])station.contentWindow.childElement.GetPanel("input").Content;
        var first = Box(input[0], 120, 36, 16, 18, 4);
        var second = Box(input[1], 10, 0, 0, 0, 0);
        var output = station.Slot("output");
        var tabs = (MachineryTabs)station.contentWindow.childElement.GetPanel("control").Content;
        var controls = tabs.Panels[1].Items;
        var apply = controls.OfType<MachineryButton>().First();
        var conductive = ((MachineryStack)controls[2]).Items.OfType<MachinerySlider>().Single();
        const string recipeTag = "CORP_SINTERING_RECIPE";
        LowerMachinist.SecondVisit = false;
        apply.Click();
        Check(station.GetTagReadonly(recipeTag).GetString() != SinteringRecipes.TrialRod, "Trial mold requires the second purchase");
        LowerMachinist.SecondVisit = true;
        conductive.SetInt(9);
        apply.Click();
        Check(station.GetTagReadonly(recipeTag).GetString() == SinteringRecipes.TrialRod
            && station.GetTagReadonly(TrialAlloy.BlendTag).GetString() == "63|18|8|9|2",
            "The workshop editor commits the selected trial blend");
        station.ModifyTag(TrialAlloy.BlendTag, tag => tag.Enable().SetString("62|18|8|10|2"));
        station.contentWindow.openCallback.Invoke();
        Check(conductive.GetInt() == 10, "Opening the machine restores its saved blend in the editor");
        conductive.SetInt(9);
        apply.Click();
        first.Parts[1] = 17;
        station.Night();
        Check(output.childItems.Count == 0 && cell.Charge == 12 && first.Parts[1] == 17, "A missing mix component costs nothing");
        first.Parts[1] = 36;
        first.Parts[4] = 100;
        station.Night();
        Check(output.childItems.Count == 0 && cell.Charge == 12 && first.Parts[4] == 100,
            "Having enough constituents cannot bypass proportional draws from a mixed powder box");
        first.Parts[4] = 4;
        output.Capacity = 1;
        station.Night();
        Check(output.childItems.Count == 0 && cell.Charge == 12 && first.Parts[0] == 120
            && second.Parts[0] == 10, "A full trial output reserves the inputs");
        output.Capacity = 2;
        station.Night();
        var expected = new RodMaterial(63, 18, 8, 9, 2);
        Check(output.childItems.Count == 2 && output.childItems.All(rod => RodMaterial.Read(rod) == expected)
            && cell.Charge == 0 && first.Parts.SequenceEqual(new[] {59,18,8,9,2})
            && second.Parts.SequenceEqual(new[] {8,0,0,0,0}), "The saved blend uses the largest matching draw from the first box");
        Check(RodMaterial.Read(DirectoryMaster.Item("corp_presence_" + SinteringRecipes.TrialRod, isOwned: true)) == null,
            "A trial rod requires its actual material archive");
        var corrupt = SinteringStation.Definition.Shell();
        var spareCell = Cell(corrupt, 12);
        var corruptInput = (GameSlotInventory[])corrupt.contentWindow.childElement.GetPanel("input").Content;
        Box(corruptInput[0], 64, 18, 8, 18, 2);
        corrupt.ModifyTag(TrialAlloy.BlendTag, tag => tag.Enable().SetString("54|18|8|18|2"));
        Select(corrupt, recipeTag, SinteringRecipes.TrialRod);
        corrupt.Night();
        Check(spareCell.Charge == 12 && corrupt.Slot("output").childItems.Count == 0, "Out-of-range stored blends cannot produce");
        Console.WriteLine("PASS trial mold unlock, editor, mixed stock, output retry and material records.");
    }

    private static void RodTreatment()
    {
        var station = SinteringStation.Definition.Shell();
        var cell = Cell(station, 24);
        var tabs = (MachineryTabs)station.contentWindow.childElement.GetPanel("control").Content;
        var actions = tabs.Panels[2].Items.OfType<MachineryButton>().ToArray();
        var inputs = (GameSlotInventory[])station.contentWindow.childElement.GetPanel("input").Content;
        var powder = Box(inputs[0], 120, 0, 0, 0, 0);
        var rod = DirectoryMaster.Item("corp_presence_" + SinteringRecipes.TrialRod, isOwned: true);
        var material = new RodMaterial(60, 23, 11, 5, 1);
        material.Write(rod);
        station.Slot("solid").Put(rod);
        actions[2].Click();
        Check(!RodTreatmentJob.HasQueue(station), "An alloy below conductivity 60 cannot queue alignment");
        actions[3].Click();
        Check(RodTreatmentJob.HasQueue(station), "The treatment UI queues hardening on the loaded rod");
        cell.Charge = 11;
        station.Night();
        Check(RodTreatmentJob.HasQueue(station) && RodCondition.Read(rod)?.Process == RodProcess.Cast
            && cell.Charge == 11 && powder.Parts[0] == 120, "Insufficient energy holds the job without a fallback smelt");
        cell.Charge = 24;
        station.Night();
        var condition = RodCondition.Read(rod);
        Check(!RodTreatmentJob.HasQueue(station) && ReferenceEquals(station.Slot("solid").childItems.Single(), rod)
            && RodMaterial.Read(rod) == material && powder.Parts[0] == 120 && cell.Charge == 12
            && condition?.Process == RodProcess.Harden && condition.Core.Hardness == 77
            && condition.Surface.Hardness == 89 && condition.Surface.Wear == 74, "One in-place job keeps mass and hardens only the surface");
        Check(station.GetTagReadonly(MineralNightReport.Sinter).GetString().EndsWith("rod_treatment/harden"),
            "The last-night report records treatment as in-place work");
        actions[3].Click();
        Check(!RodTreatmentJob.HasQueue(station), "Repeated treatment cannot stack");
        var afterLoad = DirectoryMaster.Item(rod.identifier, isOwned: true);
        material.Write(afterLoad);
        afterLoad.ModifyTag(RodCondition.ProcessTag, tag => tag.Enable().SetString(rod.GetTagReadonly(RodCondition.ProcessTag).GetString()));
        Check(RodCondition.Read(afterLoad) == condition, "The process tag restores the same core and surface readings");
        Check(condition.Traits.HasFlag(RodTrait.HardTooth) &&
            condition.Labels(condition.Surface).HasFlag(RodLabel.HighHardness) &&
            !condition.Labels(condition.Core).HasFlag(RodLabel.HighHardness), "The counter can distinguish hard surface from soft core");
        var temper = RodCondition.Evaluate(new(64, 18, 8, 8, 2), RodProcess.Temper);
        var aligned = RodCondition.Evaluate(new(64, 7, 7, 20, 2), RodProcess.Align);
        var dense = RodCondition.Evaluate(new(64, 7, 7, 20, 2), RodProcess.Densify);
        Check(temper.Core.Load == 53 && temper.Core.Brittleness == 3 && temper.Traits.HasFlag(RodTrait.Fatigue),
            "Tempered wear alloy passes the fatigue condition");
        Check(aligned.AxialConductivity == 75 && aligned.Core.Conductivity == 67 && aligned.Traits.HasFlag(RodTrait.AxialConductor)
            && aligned.Labels(aligned.Core).HasFlag(RodLabel.HighAxialConductivity)
            && !aligned.Labels(aligned.Core).HasFlag(RodLabel.HighConductivity), "Directional and bulk scores remain separate");
        Check(dense.Core.Porosity == 2 && dense.Traits.HasFlag(RodTrait.LowLoss), "Vacuum treatment enables low loss");
        var exactHardness = RodCondition.Evaluate(new(60, 27, 7, 4, 2), RodProcess.Cast);
        Check(exactHardness.Labels(exactHardness.Core).HasFlag(RodLabel.HighHardness)
            && !exactHardness.Traits.HasFlag(RodTrait.HardTooth), "A standalone label can qualify without a treatment trait");
        Console.WriteLine("PASS in-place rod treatment, queue, power guard, no stacking and material replay.");
    }

    private static void RodVisualProfiles()
    {
        var first = new RodMaterial(64, 18, 8, 8, 2);
        var second = new RodMaterial(64, 7, 7, 20, 2);
        var firstCast = RodVisual.From(RodCondition.Evaluate(first, RodProcess.Cast));
        var secondCast = RodVisual.From(RodCondition.Evaluate(second, RodProcess.Cast));
        Check(firstCast.Tint != secondCast.Tint && firstCast.Art == secondCast.Art,
            "Two recipes in one mold have distinct color but share the cast pattern");
        var treatments = new[] { RodProcess.Cast, RodProcess.Temper, RodProcess.Densify, RodProcess.Align, RodProcess.Harden };
        var profiles = treatments.Select(process => RodVisual.From(RodCondition.Evaluate(second, process))).ToArray();
        Check(profiles.All(profile => profile.Tint == secondCast.Tint)
            && profiles.Select(profile => profile.Art).Distinct().Count() == treatments.Length,
            "Each treatment has a pattern of its own while preserving the recipe tone");
        var segment = RodCondition.Evaluate(new RodMaterial(32, 9, 4, 4, 1), RodProcess.Temper, first);
        var shortVisual = RodVisual.From(segment, shortStock: true);
        Check(shortVisual.Tint == firstCast.Tint && shortVisual.Art == "alloy-rod-short-temper",
            "A treated segment shares the furnace color but uses its own size and process pattern");
        Console.WriteLine("PASS rod appearance keys preserve recipe tone across processing and distinguish textures");
    }

    private static void RodFormingJourney()
    {
        var bench = CraftingStation.Definition.Shell();
        var input = ((GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content)[0];
        var output = bench.Slot("output");
        const string mode = "CORP_CRAFT_MILL_RECIPE";
        GameItem Rod(RodMaterial material, RodProcess process)
        {
            var rod = DirectoryMaster.Item("corp_presence_" + SinteringRecipes.TrialRod, isOwned: true);
            material.Write(rod);
            if (process != RodProcess.Cast) RodCondition.Evaluate(material, process).WriteProcess(rod);
            return rod;
        }
        void SameMass(RodMaterial source)
        {
            var parts = output.childItems.Select(RodMaterial.Read).ToArray();
            Check(parts.All(p => p != null) && new RodMaterial(parts.Sum(p => p.Metal), parts.Sum(p => p.Silica), parts.Sum(p => p.Rare),
                parts.Sum(p => p.Conductive), parts.Sum(p => p.Residue)) == source, "All five powder masses are conserved");
        }
        void Empty()
        {
            foreach (var product in output.childItems.ToArray()) product.Destroy();
        }

        var wear = new RodMaterial(64,18,8,8,2);
        Select(bench, mode, RodMaterial.ShortRod);
        var bar = input.Put(Rod(wear, RodProcess.Temper));
        output.Capacity = 1;
        Button(bench, 1).Click();
        Check(output.childItems.Count == 0 && ReferenceEquals(input.childItem, bar) && bar.unitCount == 1,
            "When either short rod cannot be placed the original stays in the workbench");
        output.Capacity = 20;
        var priorAccept = GraphUtils.AfterAccept;
        GraphUtils.AfterAccept = (inventory, item) =>
        {
            priorAccept?.Invoke(inventory, item);
            if (ReferenceEquals(inventory, output)) output.Capacity = 1;
        };
        try { Button(bench, 1).Click(); }
        finally { GraphUtils.AfterAccept = priorAccept; output.Capacity = 20; }
        Check(output.childItems.Count == 0 && ReferenceEquals(input.childItem, bar) && bar.unitCount == 1,
            "When capacity changes after the first placement the staged short rod is rolled back");
        Button(bench, 1).Click();
        Check(output.childItems.Count == 2 && output.childItems.All(i => Goods.IdOf(i) == RodMaterial.ShortRod
            && RodCondition.Read(i).Traits.HasFlag(RodTrait.Fatigue)) && input.childItem == null, "Cutting yields two separate fatigue-rated segments");
        SameMass(wear);
        var shortRod = output.childItems[0];
        var shortMass = RodMaterial.Read(shortRod);
        output.Expel(shortRod);
        Empty();
        Select(bench, mode, RodMaterial.PorousBlank);
        input.Put(shortRod);
        Button(bench, 1).Click();
        Check(output.childItems.Any(i => Goods.IdOf(i) == RodMaterial.PorousBlank), "A porous segment can become a sleeve blank");
        SameMass(shortMass);
        Empty();

        Select(bench, mode, RodMaterial.Strip);
        input.Put(Rod(wear, RodProcess.Temper));
        Button(bench, 1).Click();
        Check(output.childItems.Any(i => Goods.IdOf(i) == RodMaterial.Strip), "The long rod can be rolled");
        SameMass(wear);
        Empty();

        var conductor = new RodMaterial(64,7,7,20,2);
        Select(bench, mode, RodMaterial.WireBlank);
        input.Put(Rod(conductor, RodProcess.Align));
        Button(bench, 1).Click();
        var blank = output.childItems.Single(i => Goods.IdOf(i) == RodMaterial.WireBlank);
        Check(RodCondition.Read(blank).AxialConductivity == 75, "Coarse wire keeps the original directional assay across rounding");
        SameMass(conductor);
        var loaded = DirectoryMaster.Item(blank.identifier, isOwned: true);
        RodMaterial.Read(blank).Write(loaded);
        foreach (string tag in new[]{ RodCondition.ProcessTag, RodMaterial.AssayTag })
            loaded.ModifyTag(tag, t => t.Enable().SetString(blank.GetTagReadonly(tag).GetString()));
        Check(RodCondition.Read(loaded) == RodCondition.Read(blank), "Reloaded wire blank retains its geometry, chemistry and axial score");
        Check(!AlloyForms.Accept(RodMaterial.Strip, blank), "Wire and short pieces do not qualify as full-length rolling stock");
        output.Expel(blank);
        Empty();
        Select(bench, mode, RodMaterial.IndustrialCoil);
        input.Put(blank);
        Button(bench, 1).Click();
        Check(output.childItems.Count == 0 && ReferenceEquals(input.childItem, blank),
            "Aligned coarse wire is too brittle to wind a fine coil");
        Select(bench, mode, RodMaterial.AxialLead);
        Button(bench, 1).Click();
        Check(output.childItems.Any(item => Goods.IdOf(item) == RodMaterial.AxialLead
            && RodCondition.Read(item).AxialConductivity == 75), "Aligned stock becomes a directional lead");
        SameMass(RodMaterial.Read(loaded));
        Empty();

        GameItem Finish(string recipe, GameItem feed)
        {
            Select(bench, mode, recipe);
            input.Put(feed);
            Button(bench, 1).Click();
            var result = output.childItems.Single(i => Goods.IdOf(i) == recipe);
            output.Expel(result);
            Empty();
            return result;
        }
        var shaft = Finish(RodMaterial.FeedShaft, FromBarSegment(wear, RodProcess.Temper));
        Check(RodCondition.Read(shaft).Traits.HasFlag(RodTrait.Fatigue),
            "Machining the shaft preserves its fatigue qualification");
        var porous = Finish(RodMaterial.PorousBlank, FromBarSegment(wear, RodProcess.Cast));
        var sleeve = Finish(RodMaterial.FeedSleeve, porous);
        Select(bench, mode, SleeveOiling.Recipe);
        input.Put(sleeve);
        var oilSlot = ((GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content)[1];
        Button(bench, 1).Click();
        Check(SleeveOiling.Oil(sleeve) == 0, "Dry sleeve cannot be oiled for free");
        var oil = oilSlot.Put(DirectoryMaster.Item("corp_presence_" + SleeveOiling.Lubricant, isOwned: true));
        oil.unitCount = 2;
        Button(bench, 1).Click();
        Button(bench, 1).Click();
        Check(ReferenceEquals(input.childItem, sleeve) && SleeveOiling.Oil(sleeve) == 1 && oil.unitCount == 1,
            "One physical portion is consumed, with no repeat debit or replacement sleeve");
        input.Expel(sleeve);
        oilSlot.Expel(oil);
        var denseBlank = Finish(RodMaterial.WireBlank, Rod(conductor, RodProcess.Densify));
        var coil = Finish(RodMaterial.IndustrialCoil, denseBlank);
        Check(RodCondition.Read(coil).Traits.HasFlag(RodTrait.LowLoss), "Dense stock becomes a low-loss winding");
        var strip = Finish(RodMaterial.Strip, Rod(wear, RodProcess.Temper));
        var guide = Finish(RodMaterial.ReturnGuide, strip);
        Check(RodCondition.Read(guide).Surface.Wear == 52, "Bending keeps the guide's wear rating");
        var basketBlank = Finish(RodMaterial.WireBlank, Rod(conductor, RodProcess.Cast));
        var basket = Finish(RodMaterial.FilterBasket, basketBlank);
        Check(RodCondition.Read(basket).Core.Conductivity == 67, "Wire stock becomes a real pump filter basket");
        Console.WriteLine("PASS formed stock journey: two segments, porous blank, strip, wire, offcuts, full-output retry and reload.");

        Select(bench, mode, RodMaterial.Strip);
        input.Put(Rod(new RodMaterial(30,20,15,15,20), RodProcess.Cast));
        Button(bench, 1).Click();
        var offcut = output.childItems.Single(item => Goods.IdOf(item) == RodMaterial.Offcut);
        Check(RodMaterial.Read(offcut) == new RodMaterial(6,4,3,3,4), "Keep the real five cutting remainders");
        var recovered = DirectoryMaster.Item(offcut.identifier, isOwned: true);
        recovered.ModifyTag(RodMaterial.Tag, tag => tag.Enable().SetString(offcut.GetTagReadonly(RodMaterial.Tag).GetString()));
        Empty();

        var crusher = MineralCrusher.Definition.Shell();
        var feedSlot = crusher.Slot("input");
        var untagged = DirectoryMaster.Item(offcut.identifier, isOwned: true);
        var fragmented = DirectoryMaster.Item(offcut.identifier, isOwned: true);
        new RodMaterial(2,2,2,2,2).Write(fragmented);
        Check(!feedSlot.mayInventoryAddItemFunc(untagged, feedSlot) && !feedSlot.mayInventoryAddItemFunc(fragmented, feedSlot),
            "Unknown material and offcuts that yield no whole points cannot enter the crusher");
        feedSlot.Put(recovered);
        var cell = Cell(crusher, 12);
        var box = Box(crusher.Slot("receiver"), 495, 0, 0, 0, 0);
        crusher.Night();
        Check(cell.Charge == 12 && recovered.unitCount == 1 && box.Parts[0] == 495,
            "A powder box with one point too little free retains the recovered offcut and charge");
        box.Parts[0] = 494;
        crusher.Night();
        Check(cell.Charge == 0 && feedSlot.childItems.Count == 0 && box.Parts.SequenceEqual(new[] { 496, 1, 1, 1, 1 }),
            "The existing crusher recovers 80 percent, rounded down independently per component");
        Check(crusher.GetTagReadonly(MineralNightReport.Crusher).GetString().Contains("|0/0/0/0/0/0/0/1|2/1/1/1/1|"),
            "The night receipt records the offcut separately from old feed categories");
        cell.Charge = 12;
        crusher.Night();
        Check(cell.Charge == 12 && box.Parts.SequenceEqual(new[] { 496, 1, 1, 1, 1 }), "Consumed offcuts cannot grind again");
        Console.WriteLine("PASS cuttings loop: saved five-component offcut, exact capacity, one charge and no replay");

        GameItem FromBarSegment(RodMaterial material, RodProcess process)
        {
            var piece = DirectoryMaster.Item("corp_presence_" + RodMaterial.ShortRod, isOwned: true);
            new RodMaterial(32,9,4,4,1).Write(piece);
            material.Write(piece, RodMaterial.AssayTag);
            if (process != RodProcess.Cast) RodCondition.Evaluate(material, process).WriteProcess(piece);
            return piece;
        }
    }

    private static void CompositeJourney(FeederTrialState handbook)
    {
        const string rodKey = "clad_alloy_rod";
        const string thickKey = "clad_alloy_rod_thick";
        const string recipe = "CORP_CRAFT_MILL_RECIPE";
        var root = CorpState.Current;
        Check(!IndustrialWorkbench.Learned(rodKey), "Cladding is gated by the old-machine handbook");
        root.Workshop.FeederTrial = handbook;
        var bench = CraftingStation.Definition.Shell();
        var slots = (GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content;
        var output = bench.Slot("output");
        var core = new RodMaterial(64,18,8,8,2);
        var shell = new RodMaterial(50,15,20,14,1);
        GameItem Stock(RodMaterial mass, RodProcess process)
        {
            var item = Goods.Make(SinteringRecipes.TrialRod);
            mass.Write(item);
            if (process != RodProcess.Cast) RodCondition.Evaluate(mass, process).WriteProcess(item);
            return item;
        }

        GameItem Jacketed(string mode)
        {
            Select(bench, recipe, RodMaterial.Strip);
            slots[0].Put(Stock(shell, RodProcess.Cast));
            Button(bench, 1).Click();
            var strip = One(bench, RodMaterial.Strip);
            output.Expel(strip);
            One(bench, RodMaterial.Offcut).Destroy();
            Select(bench, recipe, mode);
            var inlet = slots[0].Put(Stock(core, RodProcess.Temper));
            slots[1].Put(strip);
            var feed = RodMaterial.Read(strip);
            output.Capacity = 1;
            Button(bench, 1).Click();
            Check(inlet.unitCount == 1 && strip.unitCount == 1 && output.childItems.Count == 0, "A full tray preserves both cladding inputs");
            output.Capacity = 24;
            if (mode == thickKey)
            {
                var original = GraphUtils.AfterAccept;
                GraphUtils.AfterAccept = (inventory, product) =>
                {
                    original?.Invoke(inventory, product);
                    if (inventory == output && Goods.IdOf(product) == rodKey) core.Write(strip, RodMaterial.AssayTag);
                };
                try { Button(bench, 1).Click(); }
                finally { GraphUtils.AfterAccept = original; shell.Write(strip, RodMaterial.AssayTag); }
                Check(inlet.unitCount == 1 && strip.unitCount == 1 && output.childItems.Count == 0,
                    "A material change during native placement retracts the entire staged cladding batch");
            }
            Button(bench, 1).Click();
            var rod = One(bench, rodKey);
            var cuttings = One(bench, RodMaterial.Offcut);
            var made = RodCondition.Read(rod);
            Check(inlet.unitCount == 0 && strip.unitCount == 0 && made.Core.Load == 53 && made.Surface.Wear == 70
                && made.Traits.HasFlag(RodTrait.Fatigue), "The core bears load and keeps fatigue resistance while the jacket supplies wear resistance");
            Check(Add(made.Material, RodMaterial.Read(cuttings)) == Add(core, feed) && made.Jacket?.Material.PowderPoints == (mode == rodKey ? 10 : 20),
                "Each cladding recipe conserves all five physical components with a distinct thickness");
            var appearance = RodVisual.From(made);
            Check(appearance.JacketTint >= 0 && appearance.Tint != appearance.JacketTint,
                "The display keeps the selected core and jacket colors");
            output.Expel(rod);
            cuttings.Destroy();
            return rod;
        }

        var thin = Jacketed(rodKey);
        var thick = Jacketed(thickKey);
        var readback = Goods.Make(rodKey);
        foreach (var field in new[] { RodMaterial.Tag, RodMaterial.AssayTag, RodCondition.ProcessTag,
            "CORP_ROD_JACKET_MASS", "CORP_ROD_JACKET_ASSAY", "CORP_ROD_JACKET_PROCESS" })
            readback.ModifyTag(field, tag => tag.Enable().SetString(thick.GetTagReadonly(field).GetString()));
        Check(RodCondition.Read(readback) == RodCondition.Read(thick), "Composite alloy tags reconstruct both layers");
        readback.DisableTag("CORP_ROD_JACKET_ASSAY", skipValidate: true);
        Check(RodCondition.Read(readback) == null, "Incomplete composite tags do not fall back to monolithic stock");
        Check(!AlloyForms.Accept(RodMaterial.Strip, thick) && RodCondition.Preview(thick, RodProcess.Harden) == null,
            "Clad stock cannot be rerolled or heat treated as one material");

        GameItem Shaft(GameItem rod)
        {
            var before = RodCondition.Read(rod);
            Select(bench, recipe, RodMaterial.ShortRod);
            slots[0].Put(rod);
            Button(bench, 1).Click();
            var ends = output.childItems.ToArray();
            Check(ends.Length == 2 && Add(RodCondition.Read(ends[0]).Jacket.Material, RodCondition.Read(ends[1]).Jacket.Material) == before.Jacket.Material,
                "The two ends conserve and retain their own jacket");
            output.Expel(ends[0]);
            ends[1].Destroy();
            Select(bench, recipe, RodMaterial.FeedShaft);
            slots[0].Put(ends[0]);
            Button(bench, 1).Click();
            var shaft = One(bench, RodMaterial.FeedShaft);
            output.Expel(shaft);
            One(bench, RodMaterial.Offcut).Destroy();
            return shaft;
        }
        var thinShaft = Shaft(thin);
        var thickShaft = Shaft(thick);
        Check(RodCondition.Read(thinShaft).Jacket.Material.PowderPoints < 6
            && RodCondition.Read(thickShaft).Jacket.Material.PowderPoints >= 6, "Machining retains the meaningful sleeve thickness difference");

        var pump = DirectoryMaster.Item("corp_presence_" + IndustrialPump.Key, isOwned: true);
        var installed = EmporiumEntry.Instance.frontInvinvElement.Put(pump);
        Check(IndustrialPump.PopulateRepairPump(installed, new List<GameItem>()), "Set up a real pump part loadout");
        var sleeve = pump.Slot("sleeve").childItems.Single();
        sleeve.DisableTag(RodCondition.ScoredSleeveTag, skipValidate: true);
        var shaftSlot = pump.Slot("shaft");
        shaftSlot.Expel(shaftSlot.childItems.Single());
        var power = Cell(pump, 17);
        var vessel = new GameItem("container");
        vessel.EnableTag("LIQUID_CONTAINER_TAG");
        vessel.EnableTag("LIQUID_CONTAINER_BASE_PRICE_TAG");
        vessel.ModifyTag("water", tag => tag.Enable().SetInt(180000));
        vessel.ModifyTag("capacity", tag => tag.Enable().SetInt(200000));
        var receiver = new GameItem("container");
        receiver.EnableTag("LIQUID_CONTAINER_TAG");
        receiver.EnableTag("LIQUID_CONTAINER_BASE_PRICE_TAG");
        receiver.ModifyTag("capacity", tag => tag.Enable().SetInt(200000));
        pump.Slot("water").Put(vessel);
        pump.Slot("receiver").Put(receiver);
        var driver = DirectoryMaster.Item("screwdriver", isOwned: true);
        Check(MachineService.TryOpen(driver, pump), "Open the existing pump with the original screwdriver");
        shaftSlot.Put(thinShaft);
        MachineService.Close(pump);
        sleeve.ModifyTag("CORP_SLEEVE_OIL", tag => tag.Enable().SetInt(1));
        Button(pump, 1).Click();
        Check(power.Charge == 17 && IndustrialPump.Passed(pump, IndustrialPump.Test.Restart) == null,
            "A thin jacket still uses the full 18-charge startup test");
        Check(MachineService.TryOpen(driver, pump), "Open the cover again to swap the part");
        shaftSlot.Expel(thinShaft);
        shaftSlot.Put(thickShaft);
        MachineService.Close(pump);
        Button(pump, 1).Click();
        Check(power.Charge == 1 && IndustrialPump.Passed(pump, IndustrialPump.Test.Restart) is { Charge: 16 },
            "The real thick wear-resistant shaft completes the startup test at 16 charge");
        core.Write(thickShaft, "CORP_ROD_JACKET_ASSAY");
        Check(IndustrialPump.Passed(pump, IndustrialPump.Test.Restart) == null, "Changing the jacket invalidates the saved rating");
        pump.Destroy();
        root.Workshop.FeederTrial = null;
        Console.WriteLine("PASS clad rods: physical core and jacket, thin versus thick, split, reload and screwdriver pump test");

        static RodMaterial Add(RodMaterial a, RodMaterial b) =>
            new(a.Metal + b.Metal, a.Silica + b.Silica, a.Rare + b.Rare, a.Conductive + b.Conductive, a.Residue + b.Residue);
    }

    private static void WetContactJourney(FeederTrialState handbook)
    {
        const string contactKey = "wet_pump_contact";
        const string recipe = "CORP_CRAFT_MILL_RECIPE";
        var root = CorpState.Current;
        Check(!IndustrialWorkbench.Learned(contactKey), "The wet contact starts locked");
        root.Workshop.FeederTrial = handbook;
        var bench = CraftingStation.Definition.Shell();
        var slots = (GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content;
        var output = bench.Slot("output");
        var core = new RodMaterial(64,7,7,20,2);
        var shell = new RodMaterial(68,8,15,8,1);
        var coreRod = Goods.Make(SinteringRecipes.TrialRod);
        RodCondition.Evaluate(core, RodProcess.Densify).WriteFormed(coreRod);
        var shellRod = Goods.Make(SinteringRecipes.TrialRod);
        shell.Write(shellRod);
        Select(bench, recipe, RodMaterial.Strip);
        slots[0].Put(shellRod);
        Button(bench, 1).Click();
        var strip = One(bench, RodMaterial.Strip);
        output.Expel(strip);
        One(bench, RodMaterial.Offcut).Destroy();
        Select(bench, recipe, RodMaterial.CladRod);
        slots[0].Put(coreRod);
        slots[1].Put(strip);
        Button(bench, 1).Click();
        var bar = One(bench, RodMaterial.CladRod);
        output.Expel(bar);
        One(bench, RodMaterial.Offcut).Destroy();
        Select(bench, recipe, RodMaterial.ShortRod);
        slots[0].Put(bar);
        Button(bench, 1).Click();
        var segment = One(bench, RodMaterial.ShortRod);
        output.Expel(segment);
        One(bench, RodMaterial.ShortRod).Destroy();
        var sourceStock = RodCondition.Read(segment);
        Select(bench, recipe, contactKey);
        slots[0].Put(segment);
        output.Capacity = 1;
        Button(bench, 1).Click();
        Check(segment.unitCount == 1 && output.childItems.Count == 0, "Cannot trim a contact without room for its offcut");
        output.Capacity = 24;
        Button(bench, 1).Click();
        var contact = One(bench, contactKey);
        var stock = RodCondition.Read(contact);
        Check(stock.Core.Conductivity == 67 && stock.Surface.Corrosion == 51 && stock.Surface.Porosity == 3
            && stock.Traits.HasFlag(RodTrait.LowLoss)
            && stock.Material.Add(RodMaterial.Read(One(bench, RodMaterial.Offcut))) == sourceStock.Material,
            "A real conductive core plus corrosion-resistant jacket conserves the five components");

        var pump = DirectoryMaster.Item("corp_presence_" + IndustrialPump.Key, isOwned: true);
        EmporiumEntry.Instance.frontInvinvElement.Put(pump);
        Check(IndustrialPump.PopulateRepairPump(pump, new List<GameItem>()), "Populate stock repair pump");
        pump.Slot("sleeve").childItems.Single().DisableTag(RodCondition.ScoredSleeveTag, skipValidate: true);
        var basketSlot = pump.Slot("basket");
        basketSlot.Expel(basketSlot.childItems.Single());
        var basket = Goods.Make(RodMaterial.FilterBasket);
        RodCondition.Evaluate(shell, RodProcess.Cast).WriteFormed(basket);
        basketSlot.Put(basket);
        var cell = Cell(pump, 130);
        var from = Vessel(300000, 100000, 400000);
        var to = Vessel(0, 0, 39000);
        pump.Slot("water").Put(from);
        pump.Slot("receiver").Put(to);
        Button(pump).Click();
        Check(cell.Charge == 130, "Ordinary flow rejects 25 percent physical grit, even with a good basket");
        var screwdriver = DirectoryMaster.Item("screwdriver", isOwned: true);
        Check(MachineService.TryOpen(screwdriver, pump), "Open the pump contact bay with the original tool");
        Move(contact, pump.Slot("wet_contact"));
        MachineService.Close(pump);
        Check(!IndustrialPump.TryCommissioningShift(pump, pressure: false) && cell.Charge == 130
            && RodCondition.Read(contact) == stock, "Blocked receiver does not spend power or jacket");
        to.ModifyTag("capacity", tag => tag.SetInt(400000));
        Button(pump, 4).Click();
        var used = RodCondition.Read(contact);
        Check(cell.Charge == 117 && WaterHelper.GetLiquidParts(to).SequenceEqual(new[] { 30000,0,10000 })
            && used.Jacket.Material.PowderPoints == stock.Jacket.Material.PowderPoints - 0.5m
            && used.CoreMaterial == stock.CoreMaterial, "Low-loss core drives 40 mL for 13 charge and half a jacket point");
        Check(IndustrialPump.Passed(pump, IndustrialPump.Test.WetContact) is { Charge: 13, Volume: 40000 }
            && IndustrialPump.Passed(pump, IndustrialPump.Test.Flow) == null, "The wet test has its own post-wear evidence");
        var replacement = Goods.Make(contactKey);
        used.WriteFormed(replacement);
        Check(RodCondition.Read(replacement) == used, "Save tags restore the remaining jacket");
        var contactSlot = pump.Slot("wet_contact");
        contactSlot.Expel(contact);
        contactSlot.Put(replacement);
        Check(IndustrialPump.Passed(pump, IndustrialPump.Test.WetContact) == null, "A same-spec replacement cannot inherit the test");
        contactSlot.Expel(replacement);
        contactSlot.Put(contact);
        int shifts = (int)(2 * used.Jacket.Material.PowderPoints);
        for (int i = 0; i < shifts; i++)
            Check(IndustrialPump.TryCommissioningShift(pump, pressure: false), "The field shift uses the loaded wet contact until it wears through");
        Check(RodCondition.Read(contact) is { Jacket: null } bare && bare.Material == stock.CoreMaterial
            && bare.Core.Conductivity == 67 && !IndustrialPump.TryCommissioningShift(pump, pressure: false),
            "Exhaustion exposes the unchanged conductor and disables heavy-water service");
        Check(cell.Charge == 117 - shifts * 13, "The exhausted contact cannot draw an additional charge");
        var clean = Vessel(40_000, 0, 40_000);
        pump.Slot("water").Expel(from);
        pump.Slot("water").Put(clean);
        Button(pump).Click();
        contactSlot.Expel(contact);
        Check(IndustrialPump.Passed(pump, IndustrialPump.Test.Flow) is { Charge: 12 },
            "The original flow test does not depend on the optional contact");
        foreach (var item in output.childItems.ToArray()) item.Destroy();
        Select(bench, recipe, RodMaterial.Strip);
        var recoveryPlate = Goods.Make(SinteringRecipes.TrialRod);
        shell.Write(recoveryPlate);
        slots[0].Put(recoveryPlate);
        Button(bench, 1).Click();
        var freshStrip = One(bench, RodMaterial.Strip);
        output.Expel(freshStrip);
        One(bench, RodMaterial.Offcut).Destroy();
        var freshMass = RodMaterial.Read(freshStrip);
        Select(bench, recipe, "recoat_wet_pump_contact");
        slots[0].Put(contact);
        slots[1].Put(freshStrip);
        Button(bench, 1).Click();
        var resurfaced = One(bench, contactKey);
        var restored = RodCondition.Read(resurfaced);
        Check(contact.unitCount == 0 && freshStrip.unitCount == 0 && restored.CoreMaterial == stock.CoreMaterial
            && restored.Core == stock.Core && restored.Process == stock.Process
            && restored.Jacket.Material.PowderPoints == 4
            && restored.Material.Add(RodMaterial.Read(One(bench, RodMaterial.Offcut))) == stock.CoreMaterial.Add(freshMass),
            "Rejacketing preserves the actual recovered core and accounts for the entire strip");
        pump.Slot("water").Expel(clean);
        pump.Slot("water").Put(Vessel(30_000, 10_000, 40_000));
        Move(resurfaced, contactSlot);
        Check(IndustrialPump.Passed(pump, IndustrialPump.Test.WetContact) == null
            && IndustrialPump.TryCommissioningShift(pump, pressure: false)
            && RodCondition.Read(resurfaced).Jacket.Material.PowderPoints == 3.5m,
            "A rebuilt contact works again and earns its own wet-shift evidence");
        pump.Destroy();
        root.Workshop.FeederTrial = null;
        Console.WriteLine("PASS wet contact: actual dual-layer stock, native fluid conservation, jacket wear, reload and lost qualification");

        static GameItem Vessel(int water, int dirt, int capacity)
        {
            var item = new GameItem("container");
            item.EnableTag("LIQUID_CONTAINER_TAG");
            item.EnableTag("LIQUID_CONTAINER_BASE_PRICE_TAG");
            item.ModifyTag("capacity", t => t.SetInt(capacity));
            item.ModifyTag("water", t => t.SetInt(water));
            item.ModifyTag("physical_contaminant", t => t.SetInt(dirt));
            return item;
        }
    }

    private static void FeederJourney()
    {
        var machine = MineralCrusher.FeederDefinition.Shell();
        machine.identifier = "corp_presence_" + MineralCrusher.FeederKey;
        var ordinary = MineralCrusher.Definition.Shell();
        ordinary.identifier = "corp_presence_" + MineralCrusher.Key;
        var tool = DirectoryMaster.Item("screwdriver", isOwned: true);
        EmporiumEntry.Instance.frontInvinvElement.Put(machine);
        EmporiumEntry.Instance.frontInvinvElement.Put(ordinary);
        int legacyCalls = 0;
        tool.mayThisTargetItemFunc = (_, target) => target.IsTag("BROKEN_MACHINE_TAG");
        tool.canThisTargetItemFunc = tool.mayThisTargetItemFunc;
        tool.onThisTargetItemFunc = (_, _) => legacyCalls++;
        ScrewdriverService.AfterCreate(tool);
        Check(tool.mayThisTargetItemFunc(tool, machine) && tool.canThisTargetItemFunc(tool, machine)
            && !tool.canThisTargetItemFunc(tool, ordinary),
            "Only the registered company feeder accepts the screwdriver");
        var shaftPanel = machine.contentWindow.childElement.GetPanel("shaft");
        Check(shaftPanel.Hidden, "An ordinary machine open keeps the cover hidden");
        tool.onThisTargetItemFunc(tool, machine);
        Check(!shaftPanel.Hidden && legacyCalls == 0, "Screwdriver opens the feeder service cover");
        ordinary.EnableTag("BROKEN_MACHINE_TAG");
        Check(tool.canThisTargetItemFunc(tool, ordinary), "Native broken targets still accept the tool");
        tool.onThisTargetItemFunc(tool, ordinary);
        Check(legacyCalls == 1, "Native broken-machine behavior still receives the action");
        var battery = Cell(machine, 16);
        var box = Box((GameSlotInventory)machine.Slot("receiver"), 0, 0, 0, 0, 0);
        var input = machine.Slot("input");
        var feed = input.Put(DirectoryMaster.Item("empty_beer_bottle", isOwned: true));
        feed.unitCount = 4;
        machine.Night();
        Check(feed.unitCount == 2 && battery.Charge == 6 && box.Parts.SequenceEqual(new[] {0,170,0,0,30}),
            "Original feeder clears one two-piece batch for 10 charge");
        box.Parts = new int[5];
        feed.unitCount = 4;
        var assay = new RodMaterial(64,18,8,8,2);
        var shaft = DirectoryMaster.Item("corp_presence_feed_shaft", isOwned: true);
        var sleeve = DirectoryMaster.Item("corp_presence_feed_sleeve", isOwned: true);
        foreach (var part in new[] { shaft, sleeve })
        {
            new RodMaterial(28,8,4,4,1).Write(part);
            assay.Write(part, RodMaterial.AssayTag);
        }
        RodCondition.Evaluate(assay, RodProcess.Temper).WriteProcess(shaft);
        sleeve.ModifyTag("CORP_SLEEVE_OIL", tag => tag.Enable().SetInt(1), skipValidate: true);
        machine.Slot("shaft").Put(shaft);
        machine.Slot("sleeve").Put(sleeve);
        MachineService.Close(machine);
        battery.Charge = 12;
        machine.Night();
        Check(feed.unitCount == 2 && battery.Charge == 2 && SleeveOiling.Oil(sleeve) == 1,
            "Below 16 charge the upgraded feeder still runs one batch and saves its oil");
        box.Parts = new[] {200,0,0,0,0};
        feed.unitCount = 4;
        battery.Charge = 16;
        machine.Night();
        Check(feed.unitCount == 2 && battery.Charge == 6 && SleeveOiling.Oil(sleeve) == 1
            && box.Parts.Sum() == 400, "A box that cannot fit the complete second batch preserves the oil");
        box.Parts = new int[5];
        feed.unitCount = 4;
        battery.Charge = 16;
        machine.Night();
        Check(feed.unitCount == 0 && battery.Charge == 0 && SleeveOiling.Oil(sleeve) == 0
            && box.Parts.SequenceEqual(new[] {0,340,0,0,60}), "Four inputs really run in two batches for 16 charge and one portion");
        Check(MachineService.TryOpen(tool, machine), "The cover can be opened to remove parts");
        machine.contentWindow.closeCallback?.Invoke();
        Check(shaftPanel.Hidden, "Closing the window hides the cover without changing its parts");
        machine.Slot("shaft").Expel(shaft);
        Check(RodCondition.Read(shaft)?.Core.Load == 53, "Unloading yields the unchanged physical shaft");
        Console.WriteLine("PASS screwdriver route, physical feeder slots, baseline and two converted batches.");
    }

    private static void PumpJourney()
    {
        var bench = CraftingStation.Definition.Shell();
        const string mode = "CORP_CRAFT_MILL_RECIPE";
        var parts = (GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content;
        var bin = bench.Slot("output");
        var frame = DirectoryMaster.Item("corp_presence_" + IndustrialGoods.Frame, true);
        var contact = DirectoryMaster.Item("corp_presence_" + IndustrialGoods.Contact, true);
        Select(bench, mode, IndustrialPump.Key);
        parts[0].Put(frame);
        parts[1].Put(contact);
        bin.Capacity = 0;
        Button(bench, 1).Click();
        Check(frame.unitCount == 1 && contact.unitCount == 1, "Full output preserves the pump assembly kit");
        bin.Capacity = 20;
        Button(bench, 1).Click();
        var pump = One(bench, IndustrialPump.Key);
        Check(frame.unitCount == 0 && contact.unitCount == 0, "Bench assembles a real pump body");
        var shop = EmporiumEntry.Instance.frontInvinvElement;
        Move(pump, shop);
        Check(MachineService.TryOpen(DirectoryMaster.Item("screwdriver", true), pump), "Screwdriver opens pump");

        var wear = new RodMaterial(64, 18, 8, 8, 2);
        var conductor = new RodMaterial(54, 10, 14, 22, 0);
        Select(bench, mode, RodMaterial.PressureShell);
        parts[0].Put(Part(RodMaterial.Strip, new RodMaterial(66, 12, 18, 3, 1), RodProcess.Densify));
        Button(bench, 1).Click();
        var shell = One(bench, RodMaterial.PressureShell);
        Move(shell, pump.Slot("shell"));
        pump.Slot("shaft").Put(Part(RodMaterial.FeedShaft, wear, RodProcess.Temper));
        var sleeve = pump.Slot("sleeve").Put(Part(RodMaterial.FeedSleeve, wear, RodProcess.Cast));
        var coil = pump.Slot("coil").Put(Part(RodMaterial.IndustrialCoil, conductor, RodProcess.Densify));
        pump.Slot("basket").Put(Part(RodMaterial.FilterBasket, conductor, RodProcess.Cast));
        var cell = Cell(pump, 50);
        pump.Slot("power").ParentItem = pump;
        var water = pump.Slot("water").Put(Vessel(200000, 180000, 20000));
        var receiver = pump.Slot("receiver").Put(Vessel(39999, 0, 0));
        Button(pump).Click();
        Check(cell.Charge == 50, "An open pump never energizes");
        MachineService.Close(pump);
        Button(pump).Click();
        Check(cell.Charge == 50 && WaterHelper.GetLiquidParts(water).Sum() == 200000, "Full receiver preserves charge and water");
        receiver.ModifyTag("capacity", t => t.SetInt(160000));
        cell.Charge = 8;
        Button(pump).Click();
        Check(cell.Charge == 8 && WaterHelper.GetLiquidParts(water).Sum() == 200000, "Insufficient power preserves both vessels");
        cell.Charge = 50;
        PowerHelper.AfterDraw = _ => Move(receiver, shop);
        try { Button(pump).Click(); }
        finally { PowerHelper.AfterDraw = null; }
        Check(cell.Charge == 50 && WaterHelper.GetLiquidParts(water).Sum() == 200000
            && WaterHelper.GetLiquidParts(receiver).Sum() == 0 && IndustrialPump.Passed(pump, IndustrialPump.Test.Flow) == null,
            "A receiver moved by the battery callback cannot take liquid or earn a report");
        Move(receiver, pump.Slot("receiver"));
        Button(pump).Click();
        Check(cell.Charge == 41 && WaterHelper.GetLiquidParts(receiver).SequenceEqual(new[] { 36000, 0, 4000 }),
            "Low-loss coil debits nine charge and transfers actual mixed water");
        Check(IndustrialPump.Passed(pump, IndustrialPump.Test.Flow) is { Charge: 9, Volume: 40000 }, "Pump has a live flow report");
        Button(pump, 1).Click();
        Check(cell.Charge == 41, "Dry sleeve does not pass repeated starts");
        sleeve.ModifyTag("CORP_SLEEVE_OIL", t => t.Enable().SetInt(1));
        Button(pump, 1).Click();
        Check(cell.Charge == 26 && SleeveOiling.Oil(sleeve) == 0
            && IndustrialPump.Passed(pump, IndustrialPump.Test.Restart) is { Volume: 80000 },
            "Repeated starts debit oil, charge and a double wet run");
        Button(pump, 2).Click();
        Check(cell.Charge == 13 && WaterHelper.GetLiquidParts(receiver).SequenceEqual(new[] { 144000, 0, 16000 })
            && IndustrialPump.Passed(pump, IndustrialPump.Test.Pressure) != null, "Pressure test retains all transferred constituents");

        var restored = DirectoryMaster.Item(pump.identifier, true);
        foreach (string tag in new[] { "CORP_PUMP_TRIAL_FLOW", "CORP_PUMP_TRIAL_RESTART", "CORP_PUMP_TRIAL_PRESSURE" })
        {
            string saved = pump.GetTagReadonly(tag).GetString();
            restored.ModifyTag(tag, state => state.Enable().SetString(saved));
        }
        foreach (var name in new[] { "shaft", "sleeve", "coil", "basket", "shell" })
            Move(pump.Slot(name).childItems.Single(), restored.Slot(name));
        Check(IndustrialPump.Passed(restored, IndustrialPump.Test.Flow) != null
            && IndustrialPump.Passed(restored, IndustrialPump.Test.Pressure) != null, "Restored slots and tags retain valid results");
        restored.Slot("shell").Expel(shell);
        Check(IndustrialPump.Passed(restored, IndustrialPump.Test.Flow) != null
            && IndustrialPump.Passed(restored, IndustrialPump.Test.Pressure) == null, "Removing a shell invalidates pressure only");
        restored.Slot("coil").Expel(coil);
        restored.Slot("coil").Put(Part(RodMaterial.IndustrialCoil, conductor, RodProcess.Densify));
        Check(IndustrialPump.Passed(restored, IndustrialPump.Test.Flow) == null, "A same-grade replacement coil cannot inherit the result");

        shop.Put(restored);
        restored.Slot("coil").Expel(restored.Slot("coil").childItems.Single());
        var ordinaryCoil = new RodMaterial(64, 7, 7, 20, 2);
        restored.Slot("coil").Put(Part(RodMaterial.IndustrialCoil, ordinaryCoil, RodProcess.Cast));
        restored.Slot("basket").Expel(restored.Slot("basket").childItems.Single());
        restored.Slot("basket").Put(Part(RodMaterial.FilterBasket, ordinaryCoil, RodProcess.Cast));
        var ordinaryCell = Cell(restored, 12);
        var ordinaryReceiver = restored.Slot("receiver").Put(Vessel(40000, 0, 0));
        Move(water, restored.Slot("water"));
        Button(restored).Click();
        Check(ordinaryCell.Charge == 12 && WaterHelper.GetLiquidParts(ordinaryReceiver).Sum() == 0,
            "A standard basket refuses turbid feed without spending charge");
        water.ModifyTag("water", t => t.SetInt(38000));
        water.ModifyTag("physical_contaminant", t => t.SetInt(2000));
        WaterFeatureHelper.FailRedraw = true;
        try { Button(restored).Click(); }
        finally { WaterFeatureHelper.FailRedraw = false; }
        Check(ordinaryCell.Charge == 0 && WaterHelper.GetLiquidParts(water).Sum() == 0
            && WaterHelper.GetLiquidParts(ordinaryReceiver).SequenceEqual(new[] { 38000, 0, 2000 })
            && IndustrialPump.Passed(restored, IndustrialPump.Test.Flow) is { Charge: 12 },
            "Ordinary parts run on clearer feed and settle once even if the display callbacks fail");
        restored.Destroy();
        pump.Destroy();
        Console.WriteLine("PASS pump bench assembly, installed parts, real liquid and power, restart oil and independent saved tests");

        static GameItem Part(string key, RodMaterial assay, RodProcess process)
        {
            var item = DirectoryMaster.Item("corp_presence_" + key, true);
            assay.Write(item);
            assay.Write(item, RodMaterial.AssayTag);
            if (process != RodProcess.Cast) RodCondition.Evaluate(assay, process).WriteProcess(item);
            return item;
        }

        static GameItem Vessel(int capacity, int water, int dirt)
        {
            var item = new GameItem("container");
            item.EnableTag("LIQUID_CONTAINER_TAG");
            item.EnableTag("LIQUID_CONTAINER_BASE_PRICE_TAG");
            item.ModifyTag("capacity", t => t.SetInt(capacity));
            item.ModifyTag("water", t => t.SetInt(water));
            item.ModifyTag("physical_contaminant", t => t.SetInt(dirt));
            return item;
        }
    }

    private static void PumpRepairJourney(FeederTrialState handbook)
    {
        var root = CorpState.Current;
        Check(WaterStationRepair.Check(out _) == SaleStatus.Unavailable, "Repair requires an issued machine");
        Check(WaterStationRepair.Claim() == SaleStatus.Unavailable, "Repair requires the service handbook too");
        root.Workshop.FeederTrial = handbook;
        var shop = EmporiumEntry.Instance;
        var listing = shop.ListedOutput;
        var counter = shop.backInvinvElement;
        int capacity = counter.Capacity;
        shop.ListedOutput = counter;
        counter.Capacity = counter.childItems.Count;
        Check(WaterStationRepair.Claim() == SaleStatus.Short && root.Workshop.PumpRepair == null,
            "A full counter never records an undelivered pump");
        counter.Capacity = capacity + 4;
        int count = counter.childItems.Count;
        Check(WaterStationRepair.Claim() == SaleStatus.Ready && root.Workshop.PumpRepair.Valid(),
            "The counter receives one serialized old pump");
        var pump = WaterStationRepair.Find();
        Reload();
        Check(WaterStationRepair.Claim() == SaleStatus.Ready && counter.childItems.Count == count + 1 && WaterStationRepair.Find() == pump,
            "Reload and another claim keep the original pump only");
        var worn = pump.Slot("sleeve").childItems.Single();
        Check(!WorkshopSaleQuote.RodQualifies(WorkshopSaleOffer.Find("part_feed_sleeve"), RodCondition.Read(worn)),
            "The scored sleeve is rejected by the new-part specification");
        var originalCoil = pump.Slot("coil").childItems.Single();
        var cell = Cell(pump, 30);
        AttachVessel("water", 120000);
        AttachVessel("receiver", 0);
        Button(pump).Click();
        Check(cell.Charge == 30 && WaterStationRepair.Check(out _) == SaleStatus.Short,
            "A worn sleeve stalls the claimed pump before power or certification");

        var tool = DirectoryMaster.Item("screwdriver", true);
        Check(MachineService.TryOpen(tool, pump), "The original pump can be serviced with the screwdriver");
        pump.Slot("sleeve").Expel(worn);
        var replacement = DirectoryMaster.Item("corp_presence_" + RodMaterial.FeedSleeve, true);
        var alloy = new RodMaterial(64, 18, 8, 8, 2);
        alloy.Write(replacement);
        alloy.Write(replacement, RodMaterial.AssayTag);
        replacement.ModifyTag("CORP_SLEEVE_OIL", tag => tag.Enable().SetInt(1));
        pump.Slot("sleeve").Put(replacement);
        MachineService.Close(pump);
        Button(pump).Click();
        Button(pump, 1).Click();
        Check(cell.Charge == 0 && SleeveOiling.Oil(replacement) == 0
            && pump.Slot("coil").childItems.Single() == originalCoil, "Conserve the original coil and prove both tests on the repaired pump");
        Check(WaterStationRepair.Submit() == SaleStatus.Outstanding && pump.parentInventory == counter,
            "Counter will not take the user's power or trial vessels");
        TakeTrialGear();
        Check(WaterStationRepair.Check(out int quote) == SaleStatus.Ready && quote == WaterPumpRepairState.BasePay,
            "The original pump at standard efficiency is ready to send back");
        Check(WaterStationRepair.Submit(quote + WaterPumpRepairState.LowLossBonus) == SaleStatus.Changed && pump.parentInventory == counter,
            "A stale optional bonus quote never takes the pump");

        Check(MachineService.TryOpen(tool, pump), "The repaired pump can be upgraded before its return");
        pump.Slot("coil").Expel(originalCoil);
        var conductor = new RodMaterial(64, 7, 7, 20, 2);
        var lowLoss = DirectoryMaster.Item("corp_presence_" + RodMaterial.IndustrialCoil, true);
        conductor.Write(lowLoss);
        conductor.Write(lowLoss, RodMaterial.AssayTag);
        RodCondition.Evaluate(conductor, RodProcess.Densify).WriteProcess(lowLoss);
        pump.Slot("coil").Put(lowLoss);
        var pressureShell = DirectoryMaster.Item("corp_presence_" + RodMaterial.PressureShell, true);
        var shellStock = new RodMaterial(66, 12, 18, 3, 1);
        shellStock.Write(pressureShell);
        shellStock.Write(pressureShell, RodMaterial.AssayTag);
        pump.Slot("shell").Put(pressureShell);
        MachineService.Close(pump);
        replacement.ModifyTag("CORP_SLEEVE_OIL", tag => tag.Enable().SetInt(1));
        Cell(pump, 37);
        AttachVessel("water", 160000);
        AttachVessel("receiver", 0);
        Button(pump).Click();
        Button(pump, 1).Click();
        Button(pump, 2).Click();
        TakeTrialGear();
        Check(WaterStationRepair.Check(out int upgraded) == SaleStatus.Ready
            && upgraded == quote + WaterPumpRepairState.LowLossBonus + WaterPumpRepairState.PressureBonus,
            "The upgraded pump earns both bonuses through actual low-loss and pressure tests");
        pump.Slot("shell").Expel(pressureShell);
        Check(WaterStationRepair.Submit(upgraded) == SaleStatus.Changed && pump.parentInventory == counter,
            "Removing the tested shell invalidates the accepted price without taking the pump");
        pump.Slot("shell").Put(pressureShell);

        var wallet = SaveSlot.CurrentStore;
        int before = wallet.GetCash();
        wallet.PayoutLimit = 50;
        try
        {
            Check(WaterStationRepair.Submit() == SaleStatus.Partial && wallet.GetCash() == before + 50
                && WaterStationRepair.Find() == null, "Handoff records the removed assembled pump and the actual partial transfer");
        }
        finally { wallet.PayoutLimit = null; }
        Reload();
        Check(WaterStationRepair.Submit() == SaleStatus.Paid && wallet.GetCash() == before + upgraded,
            "The sale resumes by paying only the balance after reload");
        Reload();
        Check(WaterStationRepair.Claim() == SaleStatus.TrialSettled && WaterStationRepair.Submit() == SaleStatus.TrialSettled
            && counter.childItems.Count == count && wallet.GetCash() == before + upgraded,
            "The completed repair pays only once and blocks repeat issue");
        root.Workshop.PumpRepair = null;
        root.Workshop.FeederTrial = null;
        counter.Capacity = capacity;
        shop.ListedOutput = listing;
        Console.WriteLine("PASS water station issues a worn pump once; replacement, real tests, partial payment and reload");

        void Reload()
        {
            root.Workshop.PumpRepair = JsonSerializer.Deserialize<WaterPumpRepairState>(JsonSerializer.Serialize(root.Workshop.PumpRepair));
            Check(root.Workshop.PumpRepair.Valid(), "A serialized pump receipt is valid after reload");
        }

        void AttachVessel(string name, int amount)
        {
            var vessel = new GameItem("container");
            vessel.EnableTag("LIQUID_CONTAINER_TAG");
            vessel.EnableTag("LIQUID_CONTAINER_BASE_PRICE_TAG");
            vessel.ModifyTag("capacity", t => t.SetInt(160000));
            vessel.ModifyTag("water", t => t.SetInt(amount));
            pump.Slot(name).Put(vessel);
        }

        void TakeTrialGear()
        {
            foreach (string name in new[] { "power", "water", "receiver" })
                pump.Slot(name).Expel(pump.Slot(name).childItems.Single());
        }
    }

    private static void BuiltPumpJourney(FeederTrialState handbook)
    {
        var root = CorpState.Current;
        root.Workshop.FeederTrial = handbook;
        var shop = EmporiumEntry.Instance;
        var counter = shop.backInvinvElement;
        var previousListing = shop.ListedOutput;
        shop.ListedOutput = counter;
        var bench = CraftingStation.Definition.Shell();
        var supply = (GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content;
        Select(bench, "CORP_CRAFT_MILL_RECIPE", IndustrialPump.Key);
        supply[0].Put(Goods.Make(IndustrialGoods.Frame));
        supply[1].Put(Goods.Make(IndustrialGoods.Contact));
        Button(bench, 1).Click();
        var built = One(bench, IndustrialPump.Key);
        Move(built, counter);
        var wornAlloy = new RodMaterial(64, 18, 8, 8, 2);
        var wire = new RodMaterial(64, 7, 7, 20, 2);
        Part("shaft", RodMaterial.FeedShaft, wornAlloy, RodProcess.Temper);
        var sleeve = Part("sleeve", RodMaterial.FeedSleeve, wornAlloy, RodProcess.Cast);
        Part("coil", RodMaterial.IndustrialCoil, wire, RodProcess.Densify);
        Part("basket", RodMaterial.FilterBasket, wire, RodProcess.Cast);
        Part("shell", RodMaterial.PressureShell, new RodMaterial(66, 12, 18, 3, 1), RodProcess.Cast);
        sleeve.ModifyTag("CORP_SLEEVE_OIL", tag => tag.Enable().SetInt(1));
        var cell = Cell(built, 37);
        var inlet = Vessel("water", 160000);
        var outlet = Vessel("receiver", 0);
        Button(built).Click();
        Button(built, 1).Click();
        Check(WaterStationRepair.Check(out _, built) == SaleStatus.Short, "A new installation requires pressure proof");
        Button(built, 2).Click();
        Check(cell.Charge == 0, "Three certified tests debit 37 charge with a low-loss coil");
        built.Slot("power").Expel(cell);
        built.Slot("water").Expel(inlet);
        built.Slot("receiver").Expel(outlet);
        Check(WaterStationRepair.Check(out int bid, built) == SaleStatus.Ready
            && bid == WaterPumpRepairState.BaseBuildPay + WaterPumpRepairState.LowLossBonus
            && root.Workshop.PumpRepair == null, "A real manufactured pump can be quoted before accepting a loan");
        Check(WaterStationRepair.Claim() == SaleStatus.Ready, "Loan is still available until a built pump is delivered");
        var borrowed = WaterStationRepair.Find();
        int loanId = borrowed.uniqueId;
        Check(WaterStationRepair.Submit(bid, built) == SaleStatus.Outstanding, "Cannot sacrifice a new pump while holding the station's loan");
        borrowed.Destroy();
        var wallet = SaveSlot.CurrentStore;
        int before = wallet.GetCash();
        Check(WaterStationRepair.Submit(bid, built) == SaleStatus.Paid && wallet.GetCash() == before + bid
            && root.Workshop.PumpRepair is { CompletedDay: not null } paid && paid.PumpId == loanId
            && paid.FabricatedPumpId == built.uniqueId && WaterStationRepair.EngineeringCleared(root),
            "An actually missing loan can be replaced by a tested new machine and unlocks core certification");
        Check(WaterStationRepair.Submit(bid, built) == SaleStatus.TrialSettled && wallet.GetCash() == before + bid,
            "Loan and new installation share one entitlement");
        HardCrusherJourney(counter);
        CoreJourney(bench, counter);
        root.Workshop.CoreIntake = null;
        root.Workshop.PumpRepair = null;
        root.Workshop.FeederTrial = null;
        shop.ListedOutput = previousListing;
        Console.WriteLine("PASS manufactured pump pressure acceptance, loan replacement, single payment and engineering gate");

        GameItem Part(string slot, string key, RodMaterial assay, RodProcess process)
        {
            var item = DirectoryMaster.Item("corp_presence_" + key, true);
            assay.Write(item);
            assay.Write(item, RodMaterial.AssayTag);
            if (process != RodProcess.Cast) RodCondition.Evaluate(assay, process).WriteProcess(item);
            return built.Slot(slot).Put(item);
        }

        GameItem Vessel(string slot, int volume)
        {
            var item = new GameItem("container");
            item.EnableTag("LIQUID_CONTAINER_TAG");
            item.EnableTag("LIQUID_CONTAINER_BASE_PRICE_TAG");
            item.ModifyTag("capacity", t => t.SetInt(160000));
            item.ModifyTag("water", t => t.SetInt(volume));
            return built.Slot(slot).Put(item);
        }
    }

    private static void CoreJourney(GameItem workbench, GameInventory counter)
    {
        var root = CorpState.Current;
        int counterCapacity = counter.Capacity;
        counter.Capacity = counter.childItems.Count;
        Check(MeridianCoreIntake.Claim(IndustrialCoreKind.Timing) == SaleStatus.Short && root.Workshop.CoreIntake == null,
            "No core is receipted at a full counter");
        counter.Capacity = counterCapacity + 5;
        Check(MeridianCoreIntake.Claim(IndustrialCoreKind.Timing) == SaleStatus.Ready
            && MeridianCoreIntake.Claim(IndustrialCoreKind.Phase) == SaleStatus.Ready, "Each branch delivers an actual old core");
        var timing = counter.childItems.Single(item => Goods.IdOf(item) == MeridianCoreIntake.TimingFeed);
        var phase = counter.childItems.Single(item => Goods.IdOf(item) == MeridianCoreIntake.PhaseFeed);
        root.Workshop.CoreIntake = JsonSerializer.Deserialize<CoreIntakeState>(JsonSerializer.Serialize(root.Workshop.CoreIntake));
        Check(MeridianCoreIntake.Claim(IndustrialCoreKind.Timing) == SaleStatus.TrialSettled
            && MeridianCoreIntake.IsIssued(root, timing, IndustrialCoreKind.Timing), "Reload leaves the original incoming cores unique");

        var feed = (GameSlotInventory[])workbench.contentWindow.childElement.GetPanel("input").Content;
        Select(workbench, "CORP_CRAFT_MILL_RECIPE", CoreCalibration.Key);
        feed[0].Put(Goods.Make(IndustrialGoods.Frame));
        feed[1].Put(Goods.Make(CraftingStation.Substrate));
        feed[2].Put(Goods.Make(CraftingStation.SiliconPlate));
        Button(workbench, 1).Click();
        var rig = One(workbench, CoreCalibration.Key);
        Move(rig, counter);
        var battery = Cell(rig, 20);
        Move(timing, rig.Slot("input"));
        var wire = new RodMaterial(64, 7, 7, 20, 2);
        var winding = MakePart(RodMaterial.IndustrialCoil, wire, RodProcess.Densify);
        rig.Slot("part").Put(winding);
        Button(rig, 1).Click();
        Check(battery.Charge == 20, "Cannot skip pulse calibration");
        Button(rig).Click();
        Check(battery.Charge == 12, "Timing pretest uses real power");
        var originalPulse = timing.GetTagReadonly("CORP_CORE_PRETEST").GetString();
        var restoredController = DirectoryMaster.Item(timing.identifier, true);
        restoredController.uniqueId = timing.uniqueId;
        restoredController.ModifyTag(MeridianCoreIntake.OriginTag, tag => tag.Enable().SetString(root.RunId));
        restoredController.ModifyTag("CORP_CORE_PRETEST", tag => tag.Enable().SetString(originalPulse));
        timing.Destroy();
        timing = rig.Slot("input").Put(restoredController);
        var bin = rig.Slot("output");
        int binCapacity = bin.Capacity;
        bin.Capacity = 0;
        Button(rig, 1).Click();
        Check(timing.unitCount == 1 && battery.Charge == 12, "Blocked output leaves the tested controller and power intact");
        bin.Capacity = binCapacity;
        rig.Slot("part").Expel(winding);
        var other = MakePart(RodMaterial.IndustrialCoil, wire, RodProcess.Densify);
        rig.Slot("part").Put(other);
        Button(rig, 1).Click();
        Check(battery.Charge == 12, "Another winding cannot adopt the signed pulse test");
        rig.Slot("part").Expel(other);
        rig.Slot("part").Put(winding);
        Button(rig, 1).Click();
        var timingCore = One(rig, MeridianCoreIntake.TimingCore);
        Check(CoreCalibration.IsVerified(timingCore, IndustrialCoreKind.Timing) && timing.unitCount == 0
            && winding.unitCount == 0 && battery.Charge == 0, "Original winding and controller become one certified timing core");

        battery.Charge = 28;
        Move(phase, rig.Slot("input"));
        var lead = MakePart(RodMaterial.AxialLead, wire, RodProcess.Align);
        rig.Slot("part").Put(lead);
        Button(rig).Click();
        Check(battery.Charge == 28, "Locator cannot be calibrated without the pressure-rated reference shell");
        var fixture = MakePart(RodMaterial.PressureShell, new RodMaterial(66, 12, 18, 3, 1), RodProcess.Densify);
        rig.Slot("fixture").Put(fixture);
        Button(rig).Click();
        Button(rig, 1).Click();
        var phaseCore = One(rig, MeridianCoreIntake.PhaseCore);
        Check(CoreCalibration.IsVerified(phaseCore, IndustrialCoreKind.Phase) && phase.unitCount == 0 && lead.unitCount == 0
            && fixture.unitCount == 1 && battery.Charge == 0, "Phase proof consumes real work material, retaining the reference fixture");
        var kit = FoldedJourney(workbench, counter, timingCore, fixture);
        PhaseJourney(workbench, counter, phaseCore, kit);
        rig.Destroy();
        counter.Capacity = counterCapacity;
        Console.WriteLine("PASS both terminal core intake and calibrated manufacture; power, identity, fixture, full-output retry and provenance");

        static GameItem MakePart(string key, RodMaterial assay, RodProcess process)
        {
            var item = DirectoryMaster.Item("corp_presence_" + key, true);
            assay.Write(item);
            assay.Write(item, RodMaterial.AssayTag);
            RodCondition.Evaluate(assay, process).WriteProcess(item);
            return item;
        }
    }

    private static void PhaseJourney(GameItem bench, GameInventory counter, GameItem core, GameItem kit)
    {
        var inputs = (GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content;
        var steel = new RodMaterial(64, 18, 8, 8, 2);
        var wire = new RodMaterial(64, 7, 7, 20, 2);
        GameItem winch = Craft(PhaseRecoveryStation.Winch, Part(RodMaterial.FeedShaft, steel, RodProcess.Temper), Goods.Make(IndustrialGoods.Frame));
        GameItem drive = Craft(PhaseRecoveryStation.Drive, Part(RodMaterial.AxialLead, wire, RodProcess.Align),
            Part(RodMaterial.IndustrialCoil, wire, RodProcess.Densify));
        var shell = Part(RodMaterial.PressureShell, new RodMaterial(66, 12, 18, 3, 1), RodProcess.Densify);
        var station = Craft(PhaseRecoveryStation.Key, winch, drive, shell, core);
        Move(station, counter);
        var power = Cell(station, 24);
        var state = CorpState.Current.Workshop;
        int day = StoreStation.Day;
        Check(PhaseSite.Survey() == PhaseResult.Ready, "The finished water project opens the shaft survey");
        Button(station).Click();
        Button(station, 1).Click();
        Button(station, 2).Click();
        Check(power.Charge == 24 && state.PhaseSite.DispatchDay == null, "No launch before the beacon is placed");
        StoreStation.Day++;
        Button(station, 2).Click();
        Check(power.Charge == 24 && state.PhaseSite.DispatchDay == null, "Below the charted lift rating, nothing is charged");
        Button(station, 1).Click();
        int capacity = counter.Capacity;
        counter.Capacity = counter.childItems.Count;
        Button(station, 2).Click();
        Check(power.Charge == 24 && state.PhaseSite.DispatchDay == null, "No launch without a return footprint");
        counter.Capacity = capacity;
        PowerHelper.AfterDraw = _ => Move(power, counter);
        try { Button(station, 2).Click(); }
        finally { PowerHelper.AfterDraw = null; }
        Check(power.Charge == 24 && state.PhaseSite.DispatchDay == null,
            "A battery moved out of the dispatch bay cannot launch the station");
        Move(power, station.Slot("power"));
        Button(station, 2).Click();
        Check(power.Charge == 0 && state.PhaseSite is { Route: PhaseRoute.Whole, PowerSpent: 24 }, "Launch pays real battery power once");
        Button(station, 2).Click();
        Check(PhaseSite.Claim() == PhaseResult.EnRoute && PhaseSite.BookCrew() == PhaseResult.Used, "One site cannot be dismantled while its intact machine is in flight");
        StoreStation.Day += 2;
        state.PhaseSite = JsonSerializer.Deserialize<PhaseSiteState>(JsonSerializer.Serialize(state.PhaseSite));
        Move(station, new GameGridInventory(9, 6));
        Check(PhaseSite.Claim() == PhaseResult.StationMissing, "The exact dispatch station must be back on the shop floor");
        Move(station, counter);
        counter.Capacity = counter.childItems.Count;
        Check(PhaseSite.Claim() == PhaseResult.Space && state.PhaseSite.CargoId == null, "A full saved counter leaves the arrival unclaimed");
        counter.Capacity = capacity;
        Check(PhaseSite.Claim() == PhaseResult.Ready && state.PhaseSite.CargoId > 0, "The intact pump is accepted as a real counter item");
        var pump = counter.childItems.Single(item => item.uniqueId == state.PhaseSite.CargoId);
        state.PhaseSite = JsonSerializer.Deserialize<PhaseSiteState>(JsonSerializer.Serialize(state.PhaseSite));
        Check(PhaseSite.Claim() == PhaseResult.Used && counter.childItems.Count(item => item == pump) == 1, "Reload does not duplicate that shipment");
        var worn = pump.Slot("sleeve").childItems.Single();
        var coil = pump.Slot("coil").childItems.Single();
        Check(RodCondition.IsScoredSleeve(worn) && MachineService.TryOpen(DirectoryMaster.Item("screwdriver", true), pump),
            "Recovering whole preserves the original coil and actual damage");
        pump.Slot("sleeve").Expel(worn);
        var restored = Part(RodMaterial.FeedSleeve, steel, RodProcess.Cast);
        restored.ModifyTag("CORP_SLEEVE_OIL", tag => tag.Enable().SetInt(1));
        pump.Slot("sleeve").Put(restored);
        MachineService.Close(pump);
        var cell = Cell(pump, 30);
        Water("water", 120000);
        Water("receiver", 0);
        Button(pump).Click();
        Button(pump, 1).Click();
        Check(cell.Charge == 0 && IndustrialPump.Passed(pump, IndustrialPump.Test.Restart) != null
            && pump.Slot("coil").childItems.Single() == coil, "The salvaged machine reenters production with a replaced sleeve and its original winding");
        var pressureShell = Part(RodMaterial.PressureShell, new RodMaterial(66, 12, 18, 3, 1), RodProcess.Densify);
        pump.Slot("shell").Put(pressureShell);
        JointJourney(station, pump, kit, cell);
        state.JointWaterWork = null;
        pump.Destroy();

        // Replay the two alternative choices on independent copies of the unassigned site.
        state.PhaseSite = null;
        Check(PhaseSite.Survey() == PhaseResult.Ready, "The split scenario has its own survey");
        StoreStation.Day++;
        Button(station).Click();
        for (int i = 0; i < 3; i++) Button(station, 1).Click();
        power.Charge = 16;
        Button(station, 2).Click();
        Check(power.Charge == 0 && state.PhaseSite.Route == PhaseRoute.Parts && PhaseSite.Claim() == PhaseResult.EnRoute,
            "Splitting draws 16 charge and promises parts only");
        StoreStation.Day++;
        Check(PhaseSite.Claim() == PhaseResult.Ready, "Own split shipment arrives the next day");
        var split = CounterCargo();
        Check(Crates.Contents[split].childItems.Count == 2
            && Crates.Contents[split].childItems.All(item => Goods.IdOf(item) != IndustrialPump.Key),
            "The split route returns exactly two pieces without an intact pump");
        split.Destroy();

        state.PhaseSite = null;
        Move(station, new GameGridInventory(6, 6));
        Check(PhaseSite.Survey() == PhaseResult.Ready, "The crew scenario has its own survey");
        StoreStation.Day++;
        var wallet = SaveSlot.CurrentStore;
        int cash = wallet.GetCash();
        wallet.DebitLimit = 15;
        try { Check(PhaseSite.BookCrew() == PhaseResult.Partial, "Interrupted crew debit stays in its receipt"); }
        finally { wallet.DebitLimit = null; }
        state.PhaseSite = JsonSerializer.Deserialize<PhaseSiteState>(JsonSerializer.Serialize(state.PhaseSite));
        Check(wallet.GetCash() == cash - 15 && state.PhaseSite is { CrewPaid: 15, DispatchDay: null }
            && PhaseSite.Claim() == PhaseResult.NoDispatch, "A partially paid crew stays unlaunched across reload");
        Check(PhaseSite.BookCrew() == PhaseResult.Ready && PhaseSite.BookCrew() == PhaseResult.Used
            && wallet.GetCash() == cash - PhaseSiteState.CrewFee, "Crew booking charges only the outstanding cash");
        StoreStation.Day++;
        Check(PhaseSite.Claim() == PhaseResult.Ready, "The crew ships actual material without a phase station");
        var shipment = CounterCargo();
        var storage = Crates.Contents[shipment];
        var lining = storage.childItems.Single(item => Goods.IdOf(item) == "spent_lining");
        var crusher = MineralCrusher.Definition.Shell();
        Cell(crusher, 12);
        var box = Box(crusher.Slot("receiver"), 0,0,0,0,0);
        Move(lining, crusher.Slot("input"));
        crusher.Night();
        Check(lining.unitCount == 0 && box.Parts.SequenceEqual(new[] { 5, 10, 0, 0, 85 })
            && storage.childItems.Single().identifier == "corp_presence_" + RodMaterial.IndustrialCoil,
            "The returned liner goes back into the original crusher and the dismounted winding remains physical");
        shipment.Destroy();
        station.Destroy();
        state.PhaseSite = null;
        StoreStation.Day = day;
        Console.WriteLine("PASS intact phase recovery, split and crew alternatives; actual power, cash, reload and early reuse");

        GameItem CounterCargo() => counter.childItems.Single(item => item.uniqueId == state.PhaseSite.CargoId);

        GameItem Craft(string key, params GameItem[] goods)
        {
            Select(bench, "CORP_CRAFT_MILL_RECIPE", key);
            for (int i = 0; i < goods.Length; i++) Move(goods[i], i < 3 ? inputs[i] : bench.Slot("extra"));
            Button(bench, 1).Click();
            return One(bench, key);
        }

        static GameItem Part(string key, RodMaterial material, RodProcess process)
        {
            var part = Goods.Make(key);
            material.Write(part);
            material.Write(part, RodMaterial.AssayTag);
            if (process != RodProcess.Cast) RodCondition.Evaluate(material, process).WriteProcess(part);
            return part;
        }

        void Water(string slot, int volume)
        {
            var item = new GameItem("container");
            item.EnableTag("LIQUID_CONTAINER_TAG");
            item.EnableTag("LIQUID_CONTAINER_BASE_PRICE_TAG");
            item.ModifyTag("capacity", tag => tag.SetInt(120000));
            item.ModifyTag("water", tag => tag.SetInt(volume));
            pump.Slot(slot).Put(item);
        }
    }

    private static void JointJourney(GameItem station, GameItem pump, GameItem kit, GameItem cell)
    {
        var workshop = CorpState.Current.Workshop;
        var wallet = SaveSlot.CurrentStore;
        int cash = wallet.GetCash();
        Check(JointWaterWork.Sign(station) == SaleStatus.Short, "The job requires real docked equipment");
        var berth = station.Slot("service_pump");
        berth.ParentItem = station;
        Move(pump, berth);
        Move(kit, station.Slot("service_kit"));
        var source = pump.Slot("water").childItems.Single();
        var receiver = pump.Slot("receiver").childItems.Single();
        source.ModifyTag("water", t => t.SetInt(120000));
        receiver.ModifyTag("water", t => t.SetInt(0));
        cell.Charge = 40;
        Check(JointWaterWork.Sign(station) == SaleStatus.Ready && workshop.JointWaterWork.Valid()
            && JointWaterWork.PayNext() == SaleStatus.Short, "Signing does not prepay the three milestones");
        var kitBay = station.Slot("service_kit");
        Check(berth.mayInventoryRemoveItemFunc?.Invoke(pump, berth) == false
            && kitBay.mayInventoryRemoveItemFunc?.Invoke(kit, kitBay) == false,
            "The signed pump and kit cannot be removed before on-site use");
        station.Night();
        Check(workshop.JointWaterWork.ArrivalDay == null, "A newly signed shipment is not already on site");

        StoreStation.Day++;
        station.Night();
        Check(workshop.JointWaterWork.ArrivalDay == StoreStation.Day && cell.Charge == 40, "Actual arrival does not run a pump");
        wallet.PayoutLimit = 25;
        try { Check(JointWaterWork.PayNext() == SaleStatus.Partial, "Partial arrival payment is recorded"); }
        finally { wallet.PayoutLimit = null; }
        workshop.JointWaterWork = JsonSerializer.Deserialize<JointWaterWorkState>(JsonSerializer.Serialize(workshop.JointWaterWork));
        Check(JointWaterWork.PayNext() == SaleStatus.Paid && wallet.GetCash() == cash + 90, "Reload pays only the unpaid arrival balance");
        station.Night();
        Check(workshop.JointWaterWork.LoadDay == null && kit.unitCount == 1, "Repeated arrival callbacks cannot start pressure testing");

        StoreStation.Day++;
        station.Night();
        Check(workshop.JointWaterWork.LoadDay == StoreStation.Day && cell.Charge == 24 && kit.unitCount == 0
            && IndustrialPump.Passed(pump, IndustrialPump.Test.Pressure) is { Volume: 40000 },
            "The new pressure shift uses the repaired pump, its battery, actual liquid and the folded service kit");
        Check(JointWaterWork.PayNext() == SaleStatus.Paid && wallet.GetCash() == cash + 225, "Pressure milestone settles once");
        receiver.ModifyTag("capacity", t => t.SetInt(40000));
        StoreStation.Day++;
        station.Night();
        Check(workshop.JointWaterWork.ServiceNights == 0 && cell.Charge == 24, "A blocked return line spends nothing");
        receiver.ModifyTag("capacity", t => t.SetInt(120000));
        StoreStation.Day++;
        station.Night();
        station.Night();
        Check(workshop.JointWaterWork.ServiceNights == 1 && cell.Charge == 12, "One active day cannot produce two service shifts");
        StoreStation.Day++;
        station.Night();
        Check(workshop.JointWaterWork.ServiceNights == 2 && cell.Charge == 0
            && WaterHelper.GetLiquidParts(receiver).Sum() == 120000 && pump.parentInventory == berth,
            "Two measured shifts finish the job while retaining the player's repaired pump");
        Check(berth.mayInventoryRemoveItemFunc?.Invoke(pump, berth) != false,
            "A completed service releases the recovered pump");
        Check(JointWaterWork.PayNext() == SaleStatus.Paid && JointWaterWork.PayNext() == SaleStatus.TrialSettled
            && wallet.GetCash() == cash + 460, "The endgame service pays exactly its three milestones");
        Console.WriteLine("PASS joint water service: folded kit, actual recovered pump, pressure, two wet shifts and three restart-safe payments");
    }

    private static void MineralizerRetrofitJourney(FeederTrialState handbook)
    {
        const string guideKey = "mineral_growth_guide", fixture = "CORP_MINERAL_GROWTH_RIG";
        const string paid = "CORP_MINERAL_GROWTH_DUE", progress = "CORP_MINERAL_GROWTH_PROGRESS";
        var root = CorpState.Current;
        var previousHandbook = root.Workshop.FeederTrial;
        root.Workshop.FeederTrial = handbook;
        int day = StoreStation.Day;
        var counter = EmporiumEntry.Instance.frontInvinvElement;
        var station = Station();
        var screwdriver = counter.Put(new GameItem("screwdriver"));
        Check(MachineService.TryOpen(screwdriver, station) && !station.contentWindow.childElement.GetPanel("service_guide").Hidden,
            "The old mineralizer opens the standard screwdriver retrofit cover");

        var bench = CraftingStation.Definition.Shell();
        var inputs = (GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content;
        const string recipe = "CORP_CRAFT_MILL_RECIPE";
        var alloy = new RodMaterial(68,8,15,8,1);
        Select(bench, recipe, RodMaterial.Strip);
        var rod = inputs[0].Put(Goods.Make(SinteringRecipes.TrialRod));
        alloy.Write(rod);
        RodCondition.Evaluate(alloy, RodProcess.Densify).WriteProcess(rod);
        Button(bench, 1).Click();
        Select(bench, recipe, guideKey);
        Move(One(bench, RodMaterial.Strip), inputs[0]);
        Button(bench, 1).Click();
        var guide = One(bench, guideKey);
        Check(RodCondition.Read(guide) is { Process: RodProcess.Densify, Surface.Corrosion: >= 45, Surface.Porosity: <= 1 },
            "Densified real strip creates a dense corrosion-resistant guide");
        var coil = Goods.Make(RodMaterial.IndustrialCoil);
        RodCondition.Evaluate(new RodMaterial(64,7,7,20,2), RodProcess.Densify).WriteFormed(coil);
        station.Slot("service_coil").Put(coil);
        Move(guide, station.Slot("service_guide"));

        var crusher = MineralCrusher.Definition.Shell();
        var box = Box(crusher.Slot("receiver"), 0,0,0,0,0);
        crusher.Slot("input").Put(Goods.Make("spent_lining"));
        Cell(crusher, 12);
        crusher.Night();
        Check(box.Parts.SequenceEqual(new[] { 5,10,0,0,85 }), "An ordinary crushed spent lining supplies the same real residue as before");
        Move(box, station.Slot("input"));
        var other = Box(station.Slot("secondary"), 30,0,0,0,0);
        var blueprint = station.Slot("blueprint").Put(Goods.Make(MineralGrowthBatch.OreBlueprint));
        var water = new GameItem("water_jug");
        water.EnableTag("LIQUID_CONTAINER_TAG");
        water.ModifyTag("water", t => t.SetInt(28000));
        water.ModifyTag("physical_contaminant", t => t.SetInt(7000));
        station.Slot("water").Put(water);
        var cell = Cell(station, 6);
        station.Slot("output").Capacity = 0;
        station.Night();
        Check(cell.Charge == 6 && box.Parts[4] == 85 && water.GetTagReadonly("water").GetInt() == 28000, "Full output blocks initial ore spending");
        station.Slot("output").Capacity = 1;
        PowerHelper.AfterDraw = _ => station.Slot("water").Expel(water);
        try { station.Night(); }
        finally { PowerHelper.AfterDraw = null; }
        var pending = MineralGrowthStation.ReadQueue(station);
        Check(pending.Active && pending.Nights == 2 && pending.Charge == 3 && pending.Water == 28
            && cell.Charge == 3 && box.Parts[4] == 85 && station.GetTagReadonly(paid).GetInt() == 1,
            "A power callback that moves water keeps one saved paid night and reserves the unpaid physical stock");

        var restored = Station();
        foreach (var tag in station.Tags) restored.Tags[tag.Key] = tag.Value;
        foreach (string name in new[] { "power", "input", "secondary", "blueprint", "service_coil", "service_guide" })
            Move(station.Slot(name).childItems.Single(), restored.Slot(name));
        restored.Slot("water").Put(water);
        restored.onLoaded(restored);
        Check(!restored.Slot("service_coil").mayInventoryRemoveItemFunc(coil, restored.Slot("service_coil"))
            && !restored.Slot("service_guide").mayInventoryRemoveItemFunc(guide, restored.Slot("service_guide")),
            "Only fitted parts used by the paid batch are captive until delivery");
        restored.Night();
        restored.Night();
        Check(cell.Charge == 3 && box.Parts.SequenceEqual(new[] { 5,10,0,0,25 }) && other.Parts[0] == 15
            && water.GetTagReadonly("water").GetInt() == 14000 && restored.GetTagReadonly(progress).GetInt() == 100
            && restored.GetTagReadonly(paid).GetInt() == 0, "Reload settles just the owed powder and water and cannot advance a second time");
        var rate = restored.GetTagReadonly(fixture).GetString();
        StoreStation.Day++;
        restored.ModifyTag(fixture, t => t.SetString("unknown"));
        restored.Night();
        Check(cell.Charge == 3 && MineralGrowthStation.ReadQueue(restored) is { Active: true, Recipe: null },
            "An unsupported saved profile is retained without spending any stock");
        restored.ModifyTag(fixture, t => t.SetString(rate));
        restored.ModifyTag(ModuleHelper.TOTAL_PERCENTAGE_POWER_BONUS_INT, t => t.SetInt(75));
        var output = restored.Slot("output");
        output.Capacity = 0;
        PowerHelper.AfterDraw = _ => restored.Slot("water").Expel(water);
        try { restored.Night(); }
        finally { PowerHelper.AfterDraw = null; }
        Check(cell.Charge == 0 && restored.GetTagReadonly(paid).GetInt() == 2
            && MineralGrowthStation.ReadQueue(restored) is { Nights: 1, Charge: 0, Water: 14 },
            "The second interrupted night owes water alone and appears as already paid in the forecast");
        restored.Slot("water").Put(water);
        restored.Night();
        Check(cell.Charge == 0 && water.GetTagReadonly("water").GetInt() == 0
            && water.GetTagReadonly("physical_contaminant").GetInt() == 7000 && MineralGrowthStation.ReadQueue(restored).Nights == 0,
            "A full tray preserves the fully paid batch and frozen water cost across a module change");
        StoreStation.Day++;
        output.Capacity = 2;
        cell.Charge = 3;
        water.ModifyTag("water", t => t.SetInt(14000));
        PowderBox.WriteComposition(box, new[] { 5,10,0,0,85 });
        PowderBox.WriteComposition(other, new[] { 30,0,0,0,0 });
        restored.Night();
        restored.Night();
        Check(!MineralGrowthStation.HasActiveBatch(restored) && output.childItems.Single().identifier == "common_ore"
            && cell.Charge == 3 && water.GetTagReadonly("water").GetInt() == 14000 && box.Parts[4] == 85 && other.Parts[0] == 30
            && restored.Slot("service_guide").mayInventoryRemoveItemFunc(guide, restored.Slot("service_guide")),
            "Delayed delivery releases the parts for free and cannot start another batch on that closing cycle");

        output.childItems[0].Destroy();
        StoreStation.Day++;
        restored.ModifyTag("CORP_MINERAL_GROWTH_RECIPE", t => t.SetString(MineralGrowthBatch.OreBlueprint));
        restored.ModifyTag(progress, t => t.SetInt(100));
        restored.ModifyTag(ModuleHelper.TOTAL_PERCENTAGE_POWER_BONUS_INT, t => t.SetInt(0));
        cell.Charge = 6;
        water.ModifyTag("water", t => t.SetInt(20000));
        Check(MineralGrowthStation.ReadQueue(restored) is { Nights: 1, Charge: 6, Water: 20 },
            "A legacy in-process ore batch stays on its original rate even with both new parts fitted");
        restored.Night();
        Check(cell.Charge == 0 && water.GetTagReadonly("water").GetInt() == 0 && box.Parts[4] == 85 && other.Parts[0] == 30
            && output.childItems.Single().identifier == "common_ore", "The legacy in-process batch completes without re-debiting feed");

        Move(blueprint, counter);
        restored.Slot("blueprint").Put(Goods.Make(MineralGrowthBatch.LightSaltBlueprint));
        PowderBox.WriteComposition(box, new[] { 0,0,0,0,40 });
        PowderBox.WriteComposition(other, new[] { 0,0,10,0,0 });
        water.ModifyTag("water", t => t.SetInt(40000));
        cell.Charge = 12;
        output.childItems[0].Destroy();
        StoreStation.Day++;
        restored.Night();
        restored.Night();
        Check(cell.Charge == 0 && water.GetTagReadonly("water").GetInt() == 0 && other.Parts[2] == 0
            && output.childItems.Count == 2 && output.childItems.All(i => i.identifier == "corp_presence_light_salt_core"),
            "The same fitted machine still follows the original two light-salt nights at the old rates");
        station.Destroy();
        restored.Destroy();
        screwdriver.Destroy();
        blueprint.Destroy();
        root.Workshop.FeederTrial = previousHandbook;
        StoreStation.Day = day;
        Console.WriteLine("PASS mineralizer retrofit: real lining, frozen payable ore shifts, liquid preservation and original light salt");

        GameItem Station()
        {
            var machine = MineralGrowthStation.Definition.Shell();
            machine.identifier = "corp_presence_" + MineralGrowthStation.Key;
            return counter.Put(machine);
        }
    }

    private static void ClassifierRetrofitJourney(FeederTrialState handbook)
    {
        const string part = "classifier_support", benchRecipe = "CORP_CRAFT_MILL_RECIPE";
        var root = CorpState.Current;
        var previous = root.Workshop.FeederTrial;
        root.Workshop.FeederTrial = handbook;
        int day = StoreStation.Day;
        var counter = EmporiumEntry.Instance.frontInvinvElement;
        var mill = Machine();
        var tool = counter.Put(new GameItem("screwdriver"));
        Check(MachineService.TryOpen(tool, mill) && !mill.contentWindow.childElement.GetPanel("service_support").Hidden,
            "Old classifier machines open a native screwdriver cover");
        var cell = Cell(mill, 10);
        var screen = mill.Slot("tool").Put(Goods.Make(ClassifierGoods.Screen));
        var box = Box(mill.Slot("silica"), 0,140,0,0,60);
        var output = mill.Slot("output");
        Button(mill).Click();
        mill.Night();
        Check(!PowderClassifier.HasActiveBatch(mill) && cell.Charge == 10 && box.Parts[1] == 140,
            "A regular fine screen refuses mixed feed even when it contains enough silica");

        var bench = CraftingStation.Definition.Shell();
        var slots = (GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content;
        var tough = new RodMaterial(68,8,15,8,1);
        Select(bench, benchRecipe, RodMaterial.Strip);
        var bar = slots[0].Put(Goods.Make(SinteringRecipes.TrialRod));
        tough.Write(bar);
        RodCondition.Evaluate(tough, RodProcess.Temper).WriteProcess(bar);
        Button(bench, 1).Click();
        Select(bench, benchRecipe, part);
        var strip = Move(One(bench, RodMaterial.Strip), slots[0]);
        int oldOutputs = bench.Slot("output").childItems.Count;
        Button(bench, 1).Click();
        var newPart = One(bench, part);
        var material = RodCondition.Read(newPart);
        var cut = bench.Slot("output").childItems.Last(item => Goods.IdOf(item) == RodMaterial.Offcut);
        Check(material != null && material.Traits.HasFlag(RodTrait.DimensionStable) && strip.unitCount == 0
            && oldOutputs == 1 && material.Material.Add(RodCondition.Read(cut).Material).PowderPoints == 40,
            "A tempered C strip becomes a trued support and retains the whole five-component balance with offcuts");
        var coil = Goods.Make(RodMaterial.IndustrialCoil);
        RodCondition.Evaluate(new RodMaterial(64,7,7,20,2), RodProcess.Densify).WriteFormed(coil);
        Move(newPart, mill.Slot("service_support"));
        mill.Slot("service_coil").Put(coil);
        PowderBox.WriteComposition(box, new[] { 0,139,0,0,61 });
        mill.Night();
        Check(!PowderClassifier.HasActiveBatch(mill) && cell.Charge == 10 && box.Parts[1] == 139,
            "Precision feed still enforces the 70 percent floor");
        PowderBox.WriteComposition(box, new[] { 0,140,0,0,60 });
        output.Capacity = 2;
        mill.Night();
        Check(cell.Charge == 10 && box.Parts[1] == 140, "Starting requires three real output cells");
        output.Capacity = 3;
        PowerHelper.AfterDraw = _ => mill.Slot("silica").Expel(box);
        try { mill.Night(); }
        finally { PowerHelper.AfterDraw = null; }
        Check(cell.Charge == 5 && box.Parts[1] == 140 && mill.GetTagReadonly("CORP_CLASSIFIER_BATCH").GetString() == "charge:fine.v1",
            "If a power callback displaces the feed, the paid night remains owed without charging or consuming more");
        mill.Slot("silica").Put(box);
        mill.Night();
        mill.Night();
        var forecast = PowderClassifier.ReadQueue(mill);
        Check(cell.Charge == 5 && box.Parts.SequenceEqual(new[] { 0,80,0,0,60 }) && forecast.Nights == 1
            && forecast.Recipe.NightPower == 5
            && !mill.Slot("service_support").mayInventoryRemoveItemFunc(newPart, mill.Slot("service_support"))
            && !mill.Slot("service_coil").mayInventoryRemoveItemFunc(coil, mill.Slot("service_coil")),
            "The same night cannot charge twice and the paid batch keeps both fitted parts captive");

        var restored = Machine();
        foreach (var entry in mill.Tags) restored.Tags[entry.Key] = entry.Value;
        var savedPart = DirectoryMaster.Item(newPart.identifier, isOwned: true);
        material.WriteFormed(savedPart);
        restored.Slot("service_support").Put(savedPart);
        Move(coil, restored.Slot("service_coil"));
        Move(screen, restored.Slot("tool"));
        Move(box, restored.Slot("silica"));
        Move(cell, restored.Slot("power"));
        restored.onLoaded(restored);
        restored.Night();
        Check(cell.Charge == 5 && RodCondition.Read(savedPart) == material && PowderClassifier.ReadQueue(restored).Nights == 1,
            "Reload restores the trued item, paid-night stamp and frozen five-charge cost");
        StoreStation.Day++;
        output = restored.Slot("output");
        output.Capacity = 2;
        restored.Night();
        Check(cell.Charge == 0 && PowderClassifier.HasActiveBatch(restored) && output.childItems.Count == 0,
            "A started run can mature while blocked, with delivery still owed");
        output.Capacity = 3;
        restored.Night();
        cell.Charge = 5;
        PowderBox.WriteComposition(box, new[] { 0,140,0,0,60 });
        restored.Night();
        Check(cell.Charge == 5 && box.Parts[1] == 140 && !PowderClassifier.HasActiveBatch(restored)
            && output.childItems.Count(item => Goods.IdOf(item) == ClassifierGoods.Coarse) == 1
            && output.childItems.Count(item => Goods.IdOf(item) == ClassifierGoods.Fine) == 2
            && restored.Slot("service_support").mayInventoryRemoveItemFunc(savedPart, restored.Slot("service_support")),
            "Full-output retry delivers three bags at no cost, cannot restart that day and releases the hardware");

        foreach (var item in output.childItems.ToArray()) item.Destroy();
        var blueprint = CartelShizuka.Delivered;
        CartelShizuka.Delivered = true;
        Button(restored).Click();
        Move(screen, counter);
        var sleeve = restored.Slot("tool").Put(Goods.Make(ClassifierGoods.Sleeve));
        ClassifierGoods.SetCharges(sleeve, 1);
        var precipitate = restored.Slot("feed").Put(Goods.Make(CartelReactor.Crystal));
        StoreStation.Day++;
        cell.Charge = 5;
        restored.Night();
        Check(cell.Charge == 5 && ClassifierGoods.Charges(sleeve) == 1 && precipitate.unitCount == 1,
            "Mineral retrofits do not discount the crystal path");
        cell.Charge = 8;
        restored.Night();
        Check(cell.Charge == 0 && precipitate.unitCount == 0 && ClassifierGoods.Charges(sleeve) == 0
            && output.childItems.Count(item => Goods.IdOf(item) == ClassifierGoods.Crystal) == 2,
            "The Cartel crystal path still spends the original single sleeve charge and eight power");
        foreach (var item in output.childItems.ToArray()) item.Destroy();
        restored.Slot("pad").Put(Goods.Make(IndustrialGoods.Pad));
        Button(restored).Click();
        StoreStation.Day++;
        output.Capacity = 6;
        PowderBox.WriteComposition(box, new[] { 0,140,0,0,60 });
        cell.Charge = 13;
        restored.Night();
        Check(cell.Charge == 13 && box.Parts[1] == 140, "The precision support does not loosen the heavy pad's purity requirement");
        PowderBox.WriteComposition(box, new[] { 0,160,0,0,20 });
        restored.Night();
        Check(cell.Charge == 0 && box.Parts.SequenceEqual(new[] { 0,40,0,0,20 })
            && output.childItems.Count(item => Goods.IdOf(item) == ClassifierGoods.Coarse) == 4
            && output.childItems.Count(item => Goods.IdOf(item) == ClassifierGoods.Fine) == 2,
            "The original heavy pad makes both physical batches for thirteen charge with the coil");

        var crusher = MineralCrusher.Definition.Shell();
        crusher.identifier = "corp_presence_" + MineralCrusher.Key;
        counter.Put(crusher);
        var crusherCell = Cell(crusher, 24);
        var mixed = Box(crusher.Slot("receiver"), 0,0,0,0,0);
        crusher.Slot("input").Put(new GameItem("empty_beer_bottle") { unitCount = 4 });
        crusher.Slot("input").Put(Goods.Make("spent_lining"));
        StoreStation.Day++;
        crusher.Night();
        StoreStation.Day++;
        crusher.Night();
        Check(crusherCell.Charge == 0 && mixed.Parts.SequenceEqual(new[] { 5,350,0,0,145 }),
            "Early glass and a spent lining actually produce the seventy-percent silica stock");
        foreach (var item in output.childItems.ToArray()) item.Destroy();
        Move(sleeve, counter);
        Move(screen, restored.Slot("tool"));
        Move(box, counter);
        Move(mixed, restored.Slot("silica"));
        Button(restored).Click();
        Button(restored).Click();
        cell.Charge = 10;
        restored.Night();
        StoreStation.Day++;
        restored.Night();
        Check(cell.Charge == 0 && mixed.Parts.SequenceEqual(new[] { 5,290,0,0,145 }) && output.childItems.Count == 3,
            "The renewed old classifier processes early scrap and leaves every other original component in the same box");
        CartelShizuka.Delivered = blueprint;
        StoreStation.Day = day;
        root.Workshop.FeederTrial = previous;
        mill.Destroy();
        restored.Destroy();
        crusher.Destroy();
        screen.Destroy();
        sleeve.Destroy();
        box.Destroy();
        tool.Destroy();
        Console.WriteLine("PASS classifier retrofit: calibrated support, real mixed feed, frozen cost, replay guards and original crystal work");

        GameItem Machine()
        {
            var machine = PowderClassifier.Definition.Shell();
            machine.identifier = "corp_presence_" + PowderClassifier.Key;
            return counter.Put(machine);
        }
    }

    private static void SinterRetrofitJourney(FeederTrialState handbook)
    {
        const string wheel = "dense_trial_die", selectRecipe = "CORP_SINTERING_RECIPE", benchRecipe = "CORP_CRAFT_MILL_RECIPE";
        const string trim = "CORP_SINTERING_TRIM";
        var blend = TrialAlloy.DefaultBlend();
        var separated = TrialAlloy.Plan(new[] { new[] { 64,0,0,0,6 }, new[] { 0,18,8,8,2 }, new int[5] },
            blend, out _, allowTrim: true, trimRetention: 5);
        Check(separated is { Total: 101, Retained: 5 } && separated.Cast.SequenceEqual(blend)
            && separated.Remaining[0][4] == 3 && separated.Remaining[1][4] == 2,
            "The matcher spends the minimum six extra points across boxes and retains five of the same component in their sources");
        var exact = TrialAlloy.Plan(new[] { blend.ToArray(), new int[5], new int[5] }, blend, out _, allowTrim: true, trimRetention: 5);
        Check(exact is { Total: 100, Retained: 0, Cast: null }, "An exact proportional batch takes priority in near-fit mode");
        var root = CorpState.Current;
        var previous = root.Workshop.FeederTrial;
        root.Workshop.FeederTrial = handbook;
        var counter = EmporiumEntry.Instance.frontInvinvElement;
        var kiln = SinteringStation.Definition.Shell();
        kiln.identifier = "corp_presence_" + SinteringStation.Key;
        counter.Put(kiln);
        var tool = counter.Put(new GameItem("screwdriver"));
        Check(MachineService.TryOpen(tool, kiln) && !kiln.contentWindow.childElement.GetPanel("service_coil").Hidden,
            "The old sinter uses the registered screwdriver cover");
        var slots = (GameSlotInventory[])kiln.contentWindow.childElement.GetPanel("input").Content;
        var box = Box(slots[0], 64,18,8,8,10);
        var cell = Cell(kiln, 12);
        var tray = kiln.Slot("output");
        Select(kiln, selectRecipe, SinteringRecipes.TrialRod);
        kiln.Night();
        Check(tray.childItems.Count == 0 && box.Parts.Sum() == 108 && cell.Charge == 12,
            "The base trial rejects a near match while the new mode is off");
        var workbench = CraftingStation.Definition.Shell();
        var feed = (GameSlotInventory[])workbench.contentWindow.childElement.GetPanel("input").Content;
        var denseBar = new RodMaterial(68,8,15,8,1);
        var ordinary = Goods.Make(RodMaterial.IndustrialCoil);
        var good = Goods.Make(RodMaterial.IndustrialCoil);
        var wire = new RodMaterial(64,7,7,20,2);
        RodCondition.Evaluate(wire, RodProcess.Cast).WriteFormed(ordinary);
        RodCondition.Evaluate(wire, RodProcess.Densify).WriteFormed(good);
        var coilSlot = kiln.Slot("service_coil");
        coilSlot.Put(ordinary);
        Select(workbench, benchRecipe, RodMaterial.Strip);
        var bar = feed[0].Put(Goods.Make(SinteringRecipes.TrialRod));
        denseBar.Write(bar);
        RodCondition.Evaluate(denseBar, RodProcess.Densify).WriteProcess(bar);
        Button(workbench, 1).Click();
        Select(workbench, benchRecipe, wheel);
        Move(One(workbench, RodMaterial.Strip), feed[0]);
        Button(workbench, 1).Click();
        var die = One(workbench, wheel);
        Check(RodCondition.Read(die) is { Process: RodProcess.Densify, Core.Porosity: <= 3, Core.Wear: >= 45 },
            "The die is made from the actual conditioned strip");
        var menu = (MachineryTabs)kiln.contentWindow.childElement.GetPanel("control").Content;
        menu.Panels[1].Items.OfType<MachineryButton>().Last().Click();
        Check(kiln.GetTagReadonly(trim).GetBool(), "The player opts into trim explicitly");
        cell.Charge = 9;
        kiln.Night();
        Check(cell.Charge == 9 && box.Parts.Sum() == 108 && tray.childItems.Count == 0,
            "A normal coil cannot run the original cost with only nine charge");
        Move(ordinary, counter);
        coilSlot.Put(good);
        kiln.Night();
        Check(cell.Charge == 0 && tray.childItems.Count == 2 && box.Parts.Sum() == 0
            && kiln.GetTagReadonly(MineralNightReport.Sinter).GetString().Contains("|64/18/8/8/10|0/0/0/0/0|0/0/0/0/8|"),
            "A real near-fit run weighs out 108 total for two exact rods and records eight lost residue");
        foreach (var rod in tray.childItems.ToArray()) rod.Destroy();

        var loaded = SinteringStation.Definition.Shell();
        loaded.identifier = kiln.identifier;
        foreach (var tag in kiln.Tags) loaded.Tags[tag.Key] = tag.Value;
        counter.Put(loaded);
        var restoredSlots = (GameSlotInventory[])loaded.contentWindow.childElement.GetPanel("input").Content;
        Move(box, restoredSlots[0]);
        Move(good, loaded.Slot("service_coil"));
        Move(die, loaded.Slot("service_die"));
        Move(cell, loaded.Slot("power"));
        PowderBox.WriteComposition(box, new[] { 64,18,8,8,10 });
        cell.Charge = 9;
        tray = loaded.Slot("output");
        tray.Capacity = 1;
        loaded.Night();
        Check(tray.childItems.Count == 0 && cell.Charge == 9 && box.Parts.Sum() == 108,
            "A full output fails before either physical input or power is spent");
        tray.Capacity = 4;
        GraphUtils.AfterAccept = (inv, _) =>
        {
            if (ReferenceEquals(inv, tray)) box.Parts = new[] { 64,18,8,8,11 };
        };
        try { loaded.Night(); }
        finally { GraphUtils.AfterAccept = null; }
        Check(tray.childItems.Count == 0 && cell.Charge == 9 && box.Parts[4] == 11,
            "The plan rejects changed physical feed discovered at output placement");
        PowderBox.WriteComposition(box, new[] { 64,18,8,8,10 });
        loaded.Night();
        Check(tray.childItems.Count == 2 && cell.Charge == 0 && box.Parts.SequenceEqual(new[] { 0,0,0,0,5 })
            && tray.childItems.All(item => RodMaterial.Read(item) == new RodMaterial(64,18,8,8,2))
            && loaded.GetTagReadonly(MineralNightReport.Sinter).GetString().Contains("|64/18/8/8/5|0/0/0/0/0|0/0/0/0/3|"),
            "Restored die saves five real points without changing either physical rod or the nine-charge debit");
        var sample = Move(tray.childItems[0], loaded.Slot("solid"));
        Check(RodTreatmentJob.TryQueue(loaded, sample, RodProcess.Temper), "An exact cast rod can still queue treatment");
        cell.Charge = 9;
        loaded.Night();
        Check(cell.Charge == 0 && RodCondition.Read(sample).Process == RodProcess.Temper
            && box.Parts.Sum() == 5 && loaded.GetTagReadonly(MineralNightReport.Sinter).GetString().Contains("|1|9|"),
            "In-place treatment forecasts and debits the same retrofit cost and writes it to the receipt");

        var factory = FoldedFactory.Definition.Shell();
        factory.identifier = "corp_presence_" + FoldedFactory.Key;
        counter.Put(factory);
        var kilnDock = factory.Slot("furnace");
        kilnDock.ParentItem = factory;
        Move(loaded, kilnDock);
        var crusher = MineralCrusher.Definition.Shell();
        crusher.identifier = "corp_presence_" + MineralCrusher.Key;
        var crusherDock = factory.Slot("crusher");
        crusherDock.ParentItem = factory;
        crusherDock.Put(crusher);
        var supply = Box(crusher.Slot("receiver"), 0,0,0,0,0);
        var crusherCell = Cell(crusher, 12);
        crusher.Slot("input").Put(new GameItem("empty_beer_bottle"));
        Select(loaded, selectRecipe, CraftingStation.SiliconPlate);
        PowderBox.WriteComposition(box, new int[5]);
        cell.Charge = 18;
        Button(factory).Click();
        int previousDay = StoreStation.Day;
        StoreStation.Day++;
        loaded.Night();
        factory.Night();
        Check(cell.Charge == 9 && crusherCell.Charge == 0 && supply.Parts.Sum() == 65
            && tray.childItems.Count(item => FoldedFactory.IsFoldedPlate(item)) == 1
            && loaded.GetTagReadonly(MineralNightReport.Sinter).GetString().Contains("|1|9|"),
            "Folded dispatch uses the actual modded kiln, ordinary glass and one discounted debit");
        StoreStation.Day = previousDay;
        root.Workshop.FeederTrial = previous;
        factory.Destroy();
        kiln.Destroy();
        loaded.Destroy();
        tool.Destroy();
        ordinary.Destroy();
        Console.WriteLine("PASS sinter retrofit: real mold, original route, minimum trim, stock revalidation, restored yield and actual power");
    }

    private static void HardCrusherJourney(GameInventory counter)
    {
        const string roller = "crusher_tooth_roller", reface = "reface_crusher_tooth_roller";
        const string recipeTag = "CORP_CRAFT_MILL_RECIPE", hardTag = "CORP_CRUSHER_HARD_DUTY";
        int day = StoreStation.Day;
        var bench = CraftingStation.Definition.Shell();
        var slots = (GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content;
        var impact = new RodMaterial(65, 12, 15, 7, 1);
        var hard = new RodMaterial(60, 23, 11, 5, 1);
        var d = Cut(impact, RodProcess.Temper);
        var e = Cut(hard, RodProcess.Harden);
        var cast = Cut(new RodMaterial(60, 27, 7, 4, 2), RodProcess.Cast);
        var core = RodCondition.Read(d);
        var face = RodCondition.Read(e);
        Select(bench, recipeTag, roller);
        Check(RodCondition.Read(cast).Surface.Hardness == 80 && !IndustrialWorkbench.Accept(roller, cast) && IndustrialWorkbench.Accept(roller, d)
            && IndustrialWorkbench.Accept(roller, e), "Surface hardness must come from the actual hardening process and the impact hub must be tempered");
        Move(d, slots[0]);
        Move(e, slots[1]);
        Button(bench, 1).Click();
        var mounted = One(bench, roller);
        var stock = RodCondition.Read(mounted);
        Check(stock.CoreMaterial == core.Material && stock.Core.Load == 60 && stock.Surface.Wear == 74
            && stock.Jacket.Material.PowderPoints == 12.5m
            && stock.Material.Add(RodCondition.Read(One(bench, RodMaterial.Offcut)).Material) == core.Material.Add(face.Material),
            "The workbench builds a real dual-layer roller and returns the exact five-component balance");

        var crusher = Crusher();
        var tool = counter.Put(new GameItem("screwdriver"));
        Check(MachineService.TryOpen(tool, crusher) && !crusher.contentWindow.childElement.GetPanel("hard_roller").Hidden,
            "Screwdriver opens the old crusher's retrofit slot");
        Move(mounted, crusher.Slot("hard_roller"));
        var box = Box(crusher.Slot("receiver"), 0,0,0,0,0);
        var power = Cell(crusher, 12);
        var lining = crusher.Slot("input").Put(Goods.Make("spent_lining"));
        lining.unitCount = 2;
        crusher.Night();
        Check(box.Parts.SequenceEqual(new[] { 10,20,0,0,170 }) && power.Charge == 0 && RodCondition.Read(mounted) == stock,
            "Two ordinary linings preserve the old residue yield and never abrade a fitted roller");
        Button(crusher, 3).Click();
        Check(crusher.GetTagReadonly(hardTag).GetBool(), "A qualifying roller enables an explicit hard-duty schedule");

        var restored = Crusher();
        foreach (var entry in crusher.Tags) restored.Tags[entry.Key] = entry.Value;
        var restoredRoller = DirectoryMaster.Item(mounted.identifier, isOwned: true);
        stock.WriteFormed(restoredRoller);
        restored.Slot("hard_roller").Put(restoredRoller);
        Move(box, restored.Slot("receiver"));
        Move(power, restored.Slot("power"));
        SetBox(450);
        var other = restored.Slot("input").Put(new GameItem("glass_shard"));
        lining = restored.Slot("input").Put(Goods.Make("spent_lining"));
        lining.unitCount = 24;
        StoreStation.Day++;
        power.Charge = 14;
        restored.Night();
        Check(lining.unitCount == 24 && box.Parts[0] == 450 && power.Charge == 14 && RodCondition.Read(restoredRoller) == stock,
            "A hard batch restored into a full box has no material, power or surface wear side effects");
        SetBox(0);
        power.Charge = 13;
        restored.Night();
        Check(lining.unitCount == 24 && power.Charge == 13 && box.Parts.Sum() == 0, "Hard duty waits for its extra charge");
        power.Charge = 28;
        restored.Night();
        restored.Night();
        Check(lining.unitCount == 21 && other.unitCount == 1 && power.Charge == 14
            && box.Parts.SequenceEqual(new[] { 30,45,0,0,225 }) && RodCondition.Read(restoredRoller).Jacket.Material.PowderPoints == 11
            && restored.GetTagReadonly(MineralNightReport.Crusher).GetString().EndsWith("|hard_lining"),
            "Hard duty improves the real original feed, skips other feed and only wears the measured crop once");
        for (int shift = 0; shift < 7; shift++)
        {
            StoreStation.Day++;
            SetBox(0);
            power.Charge = 14;
            restored.Night();
        }
        var last = restored.Slot("input").Put(Goods.Make("spent_lining"));
        last.unitCount = 2;
        StoreStation.Day++;
        SetBox(0);
        power.Charge = 28;
        restored.Night();
        var bare = RodCondition.Read(restoredRoller);
        Check(last.unitCount == 1 && power.Charge == 14 && bare.Jacket == null && bare.Material == core.Material && bare.Core == core.Core,
            "The last half point permits exactly one more lining and leaves an intact tempered hub");
        StoreStation.Day++;
        restored.Night();
        Check(last.unitCount == 1 && power.Charge == 14, "A bare hub keeps the queued raw material intact");
        var replacement = Cut(hard, RodProcess.Harden);
        Select(bench, recipeTag, reface);
        Move(restoredRoller, slots[0]);
        Move(replacement, slots[1]);
        Button(bench, 1).Click();
        var returned = One(bench, roller);
        Check(returned != restoredRoller && RodCondition.Read(returned).CoreMaterial == core.Material
            && RodCondition.Read(returned).Jacket.Material.PowderPoints == 12.5m,
            "The native workbench replaces a worn face while retaining the original hub stock");
        Move(returned, restored.Slot("hard_roller"));
        SetBox(0);
        var factory = counter.Put(FoldedFactory.Definition.Shell());
        factory.identifier = "corp_presence_" + FoldedFactory.Key;
        var crusherDock = factory.Slot("crusher");
        crusherDock.ParentItem = factory;
        Move(restored, crusherDock);
        var sieveDock = factory.Slot("sieve");
        sieveDock.ParentItem = factory;
        var sieve = PowderSieve.Definition.Shell();
        sieve.identifier = "corp_presence_" + PowderSieve.Key;
        sieveDock.Put(sieve);
        var sorted = Box(sieve.Slot("input"), 0,0,0,0,0);
        var sorterCell = Cell(sieve, 12);
        Button(factory).Click();
        Button(factory).Click();
        StoreStation.Day++;
        restored.Night();
        factory.Night();
        Check(last.unitCount == 0 && power.Charge == 0 && sorterCell.Charge == 0 && box.Parts.Sum() == 0 && sorted.Parts.Sum() == 0
            && PowderSieve.ReadBin(sieve, 0)[0] == 5 && PowderSieve.ReadBin(sieve, 1)[1] == 7
            && PowderSieve.ReadBin(sieve, 4)[4] == 37 && PowderSieve.ReadBin(sieve, 5).Sum() == 51,
            "The rebuilt part works in the same old crusher and the folded cell routes its physical yield into the sieve once");
        StoreStation.Day = day;
        factory.Destroy();
        crusher.Destroy();
        restored.Destroy();
        tool.Destroy();
        Console.WriteLine("PASS hard crusher: real impact hub, hardened skin, restored plans, wear through and reface");

        void SetBox(int metal) => PowderBox.WriteComposition(box, new[] { metal,0,0,0,0 });
        GameItem Crusher()
        {
            var machine = MineralCrusher.Definition.Shell();
            machine.identifier = "corp_presence_" + MineralCrusher.Key;
            return counter.Put(machine);
        }
        GameItem Cut(RodMaterial material, RodProcess process)
        {
            Select(bench, recipeTag, RodMaterial.ShortRod);
            var bar = slots[0].Put(Goods.Make(SinteringRecipes.StandardRod));
            material.Write(bar);
            if (process != RodProcess.Cast) RodCondition.Evaluate(material, process).WriteProcess(bar);
            Button(bench, 1).Click();
            var tray = bench.Slot("output");
            var halves = tray.childItems.Where(item => Goods.IdOf(item) == RodMaterial.ShortRod).ToArray();
            Check(halves.Length == 2, "One real bar produces two short lengths");
            tray.Expel(halves[0]);
            halves[1].Destroy();
            return halves[0];
        }
    }

    private static void SieveReturnJourney(FeederTrialState handbook)
    {
        var root = CorpState.Current;
        var oldHandbook = root.Workshop.FeederTrial;
        root.Workshop.FeederTrial = handbook;
        int day = StoreStation.Day;
        var counter = EmporiumEntry.Instance.frontInvinvElement;
        var sieve = counter.Put(PowderSieve.Definition.Shell());
        sieve.identifier = "corp_presence_" + PowderSieve.Key;
        var cell = Cell(sieve, 24);
        var feed = Box(sieve.Slot("input"), 225, 30, 15, 15, 15);
        sieve.Night();
        Check(PowderSieve.ReadBin(sieve, 5).SequenceEqual(new[] { 113, 15, 8, 8, 8 }) && cell.Charge == 12,
            "The original scrap batch leaves 152 actual points of mixed tailings");
        var tool = counter.Put(new GameItem("screwdriver"));
        Check(MachineService.TryOpen(tool, sieve), "The old sieve has a screwdriver service cover");
        var screenSlot = sieve.Slot("return_screen");
        var guideSlot = sieve.Slot("return_guide");
        var bench = counter.Put(CraftingStation.Definition.Shell());
        const string recipeTag = "CORP_CRAFT_MILL_RECIPE";
        var benchInputs = (GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content;
        Select(bench, recipeTag, RodMaterial.Strip);
        var bar = benchInputs[0].Put(Goods.Make(SinteringRecipes.StandardRod));
        var alloy = new RodMaterial(64, 18, 8, 8, 2);
        alloy.Write(bar);
        RodCondition.Evaluate(alloy, RodProcess.Temper).WriteProcess(bar);
        Button(bench, 1).Click();
        Select(bench, recipeTag, RodMaterial.ReturnGuide);
        Move(One(bench, RodMaterial.Strip), benchInputs[0]);
        Button(bench, 1).Click();
        var guide = One(bench, RodMaterial.ReturnGuide);
        var screen = screenSlot.Put(Goods.Make(ClassifierGoods.Screen));
        Move(guide, guideSlot);
        var fresh = new[] { 0, 0, 60, 60, 0 };
        PowderBox.WriteComposition(feed, fresh);
        Button(sieve, 2).Click();
        Check(PowderSieve.TailingsQueued(sieve), "An installed screen and real guide allow one queued return batch");

        var loaded = counter.Put(PowderSieve.Definition.Shell());
        loaded.identifier = sieve.identifier;
        foreach (var entry in sieve.Tags) loaded.Tags[entry.Key] = entry.Value;
        Move(feed, loaded.Slot("input"));
        Move(cell, loaded.Slot("power"));
        Move(guide, loaded.Slot("return_guide"));
        loaded.onLoaded(loaded);
        StoreStation.Day++;
        loaded.Night();
        Check(PowderSieve.TailingsQueued(loaded) && cell.Charge == 12 && feed.Parts.SequenceEqual(fresh),
            "Restored queue waits for the missing screen without diverting to new feed");
        Move(screen, loaded.Slot("return_screen"));
        loaded.ModifyTag("CORP_SIEVE_BIN_0", tag => tag.SetInt(1490));
        loaded.Night();
        Check(cell.Charge == 12 && PowderSieve.TailingsQueued(loaded)
            && PowderSieve.ReadBin(loaded, 5).Sum() == 152, "A full destination preserves one queued batch and charge");
        loaded.ModifyTag("CORP_SIEVE_BIN_0", tag => tag.SetInt(112));
        cell.Charge = 11;
        loaded.Night();
        Check(cell.Charge == 11 && PowderSieve.TailingsQueued(loaded), "An undercharged return keeps its place in the next night");
        cell.Charge = 24;
        loaded.Night();
        loaded.Night();
        Check(!PowderSieve.TailingsQueued(loaded) && cell.Charge == 12 && feed.Parts.SequenceEqual(fresh)
            && PowderSieve.ReadBin(loaded, 0)[0] == 168 && PowderSieve.ReadBin(loaded, 3)[3] == 11
            && PowderSieve.ReadBin(loaded, 5).SequenceEqual(new[] { 57, 8, 4, 4, 4 })
            && loaded.GetTagReadonly(MineralNightReport.Sieve).GetString().EndsWith("|tailings"),
            "Seventy-five real points move to pure bins, with an auditable return receipt and no duplicate new-feed run");

        var factory = counter.Put(FoldedFactory.Definition.Shell());
        factory.identifier = "corp_presence_" + FoldedFactory.Key;
        var sieveDock = factory.Slot("sieve");
        sieveDock.ParentItem = factory;
        Move(loaded, sieveDock);
        var crusherDock = factory.Slot("crusher");
        crusherDock.ParentItem = factory;
        var crusher = MineralCrusher.Definition.Shell();
        crusher.identifier = "corp_presence_" + MineralCrusher.Key;
        crusherDock.Put(crusher);
        var supply = Box(crusher.Slot("receiver"), 0, 0, 0, 40, 0);
        Button(factory).Click();
        Button(factory).Click();
        Button(loaded, 2).Click();
        StoreStation.Day++;
        crusher.Night();
        loaded.Night();
        Check(supply.Parts[3] == 40 && feed.Parts.SequenceEqual(fresh) && cell.Charge == 0
            && PowderSieve.ReadBin(loaded, 5).SequenceEqual(new[] { 29, 4, 2, 2, 2 }),
            "The folded route defers delivery on the single return batch and settles the ore once");
        StoreStation.Day++;
        cell.Charge = 12;
        factory.Night();
        Check(supply.Parts.Sum() == 0 && feed.Parts.Sum() == 0 && cell.Charge == 0,
            "The next factory night feeds and sieves the waiting new powder");

        for (int i = 0; i < 5; i++) loaded.ModifyTag("CORP_SIEVE_RECOVERY_" + i, tag => tag.SetInt(1));
        Button(loaded, 2).Click();
        StoreStation.Day++;
        cell.Charge = 12;
        factory.Night();
        Check(PowderSieve.TailingsQueued(loaded) && cell.Charge == 12 && PowderSieve.ReadBin(loaded, 5).Sum() == 5,
            "Zero-yield tailings stay in the return bin without using power");
        Button(loaded, 2).Click();
        Check(!PowderSieve.TailingsQueued(loaded), "The pending return can be cancelled manually");
        for (int i = 0; i < 5; i++) loaded.ModifyTag("CORP_SIEVE_RECOVERY_" + i, tag => tag.SetInt(0));
        loaded.ModifyTag("CORP_SIEVE_RECOVERY_4", tag => tag.SetInt(1500));
        int previous = PowderSieve.ReadBin(loaded, 4)[4];
        Button(loaded, 2).Click();
        StoreStation.Day++;
        factory.Night();
        Check(cell.Charge == 0 && PowderSieve.ReadBin(loaded, 5)[4] == 1350 && PowderSieve.ReadBin(loaded, 4)[4] == previous + 150,
            "A full 1500-point return bin uses the normal 300-point ceiling and only loses what is recovered");
        root.Workshop.FeederTrial = oldHandbook;
        StoreStation.Day = day;
        factory.Destroy();
        sieve.Destroy();
        bench.Destroy();
        tool.Destroy();
        Console.WriteLine("PASS sieve return: physical parts, saved queue, shortage, conserved powder and folded scheduling");
    }

    private static GameItem FoldedJourney(GameItem workbench, GameInventory counter, GameItem timingCore, GameItem shell)
    {
        var inputs = (GameSlotInventory[])workbench.contentWindow.childElement.GetPanel("input").Content;
        Select(workbench, "CORP_CRAFT_MILL_RECIPE", FoldedFactory.Key);
        Move(shell, inputs[0]);
        var coil = Goods.Make(RodMaterial.IndustrialCoil);
        var conductor = new RodMaterial(64, 7, 7, 20, 2);
        conductor.Write(coil);
        conductor.Write(coil, RodMaterial.AssayTag);
        RodCondition.Evaluate(conductor, RodProcess.Densify).WriteProcess(coil);
        inputs[1].Put(coil);
        var shaft = Goods.Make(RodMaterial.FeedShaft);
        var alloy = new RodMaterial(64, 18, 8, 8, 2);
        alloy.Write(shaft);
        alloy.Write(shaft, RodMaterial.AssayTag);
        RodCondition.Evaluate(alloy, RodProcess.Temper).WriteProcess(shaft);
        inputs[2].Put(shaft);
        Move(timingCore, workbench.Slot("extra"));
        Button(workbench, 1).Click();
        var factory = One(workbench, FoldedFactory.Key);
        Move(factory, counter);
        Check(timingCore.unitCount == 0 && shell.unitCount == 0, "The real certified core and shell are used to assemble the factory");

        var crusher = FoldedMachine("crusher", MineralCrusher.Key);
        var sieve = FoldedMachine("sieve", PowderSieve.Key);
        var furnace = FoldedMachine("furnace", SinteringStation.Key);
        var crusherPower = Cell(crusher, 24);
        var screenPower = Cell(sieve, 24);
        var furnacePower = Cell(furnace, 24);
        var crushed = Box(crusher.Slot("receiver"), 0,0,0,0,0);
        var sieveInlet = Box(sieve.Slot("input"), 0,0,0,0,0);
        var sinter = (GameSlotInventory[])furnace.contentWindow.childElement.GetPanel("input").Content;
        Box(sinter[0], 0,0,0,0,0);
        Select(furnace, "CORP_SINTERING_RECIPE", CraftingStation.SiliconPlate);
        crusher.Slot("input").Put(new GameItem("empty_beer_bottle"));
        Button(factory).Click();
        Button(factory, 3).Click();
        sieve.Night();
        Check(crusherPower.Charge == 12 && screenPower.Charge == 24 && furnacePower.Charge == 12
            && One(factory, CraftingStation.SiliconPlate, "pickup") != null && crushed.Parts.Sum() == 65,
            "A nested native callback triggers a complete glass bypass and real output delivery on the same night");
        var nextBottle = crusher.Slot("input").Put(new GameItem("glass_shard"));
        factory.Night();
        crusher.Night();
        furnace.Night();
        Check(nextBottle.unitCount == 1 && crusherPower.Charge == 12 && furnacePower.Charge == 12
            && factory.Slot("pickup").childItems.Count == 1, "Remaining native callbacks cannot repeat a linked night");

        int day = StoreStation.Day;
        StoreStation.Day++;
        nextBottle.unitCount = 2;
        Button(factory).Click();
        Button(factory, 1).Click();
        furnace.Night();
        Check(crusherPower.Charge == 0 && screenPower.Charge == 12 && furnacePower.Charge == 0
            && crushed.Parts.Sum() == 30 && PowderSieve.ReadBin(sieve, 1)[1] == 70
            && factory.Slot("pickup").childItems.Count == 2, "Sorting priority keeps the source reserve and feeds the kiln with that night's real sorted stock");
        Move(crusher, counter);
        var standaloneBottle = crusher.Slot("input").Put(new GameItem("empty_beer_bottle"));
        crusherPower.Charge = 12;
        crusher.Night();
        Check(standaloneBottle.unitCount == 1 && crusherPower.Charge == 12,
            "Undocking cannot run the same paid machine a second time that night");

        StoreStation.Day++;
        crusher.Night();
        Check(standaloneBottle.unitCount == 0 && crusherPower.Charge == 0 && crushed.Parts.Sum() == 130,
            "An undocked crusher still uses its original standalone route the next night");
        var pickup = factory.Slot("pickup");
        pickup.Capacity = pickup.childItems.Count;
        furnacePower.Charge = 12;
        factory.Night();
        Check(furnacePower.Charge == 0 && pickup.childItems.Count == 2 && furnace.Slot("output").childItems.Count == 1,
            "A blocked pickup keeps the finished item in its original kiln rather than losing it");

        Select(furnace, "CORP_SINTERING_RECIPE", "wire");
        PowderBox.WriteComposition(sieveInlet, new[] { 0, 0, 0, 100, 0 });
        Button(factory, 2).Click();
        StoreStation.Day++;
        furnacePower.Charge = 12;
        factory.Night();
        Check(furnacePower.Charge == 12 && PowderSieve.ReadBin(sieve, 3)[3] == 50,
            "A reserved conductive bin is not spent on the active wire recipe");
        Button(factory, 2).Click();
        StoreStation.Day++;
        factory.Night();
        Check(furnacePower.Charge == 0 && PowderSieve.ReadBin(sieve, 3)[3] == 38
            && One(furnace, "wire") != null, "Releasing the reserve feeds the original kiln without minting powder");

        var classifier = PowderClassifier.Definition.Shell();
        classifier.identifier = "corp_presence_" + PowderClassifier.Key;
        counter.Put(classifier);
        var classifierPower = Cell(classifier, 24);
        Move(crushed, classifier.Slot("silica"));
        classifier.Night();
        var coarse = One(classifier, ClassifierGoods.Coarse);
        var plate = One(factory, CraftingStation.SiliconPlate, "pickup");
        Select(workbench, "CORP_CRAFT_MILL_RECIPE", ClassifierGoods.Screen);
        Move(plate, inputs[0]);
        Move(coarse, inputs[1]);
        Button(workbench, 1).Click();
        var screen = One(workbench, ClassifierGoods.Screen);
        Move(screen, classifier.Slot("tool"));
        var blend = new int[5];
        PowderBox.Read(crushed, blend);
        Check(PowderSieve.FeedFactory(sieve, PowderComponent.Silica, crushed, blend, 0, 40) == 40,
            "Retained sorted silica really replenishes the old classifier's box");
        Button(classifier).Click();
        classifier.Night();
        classifier.Night();
        Check(plate.unitCount == 0 && coarse.unitCount == 0 && classifierPower.Charge == 0
            && screen.parentInventory == classifier.Slot("tool")
            && classifier.Slot("output").childItems.Count(item => Goods.IdOf(item) == ClassifierGoods.Fine) == 3,
            "A real folded plate becomes a screen and upgrades the original early classifier");
        Select(workbench, "CORP_CRAFT_MILL_RECIPE", JointWaterWork.Kit);
        Check(!IndustrialWorkbench.Accept(JointWaterWork.Kit, Goods.Make(CraftingStation.SiliconPlate)),
            "Ordinary unstamped stock cannot stand in for a folded kit plate");
        var servicePlate = One(factory, CraftingStation.SiliconPlate, "pickup");
        var loadedPlate = DirectoryMaster.Item(servicePlate.identifier, isOwned: true);
        var plainPlate = DirectoryMaster.Item(servicePlate.identifier, isOwned: true);
        foreach (var candidate in new[] { loadedPlate, plainPlate })
        {
            candidate.mayStackItemFunc = (_, _) => true;
            CraftingStation.SiliconPlateDefinition.Setup(candidate);
        }
        const string foldedRun = "CORP_FOLD_MADE_RUN";
        loadedPlate.ModifyTag(foldedRun, tag => tag.Enable().SetString(servicePlate.GetTagReadonly(foldedRun).GetString()));
        loadedPlate.onLoaded?.Invoke(loadedPlate);
        plainPlate.onLoaded?.Invoke(plainPlate);
        Check(loadedPlate.mayStackItemFunc == null && plainPlate.mayStackItemFunc != null && FoldedFactory.IsFoldedPlate(loadedPlate),
            "The silicon plate owns restoration of its nonstackable production provenance");
        Move(servicePlate, inputs[0]);
        inputs[1].Put(Goods.Make(IndustrialGoods.Contact));
        inputs[2].Put(Goods.Make(SleeveOiling.Lubricant));
        Button(workbench, 1).Click();
        var kit = One(workbench, JointWaterWork.Kit);
        Check(servicePlate.unitCount == 0, "The kit spends that night's physical folded plate");
        StoreStation.Day++;
        crusherPower.Charge = 24;
        Box(crusher.Slot("receiver"), 0, 0, 0, 0, 0);
        var earlyBottle = crusher.Slot("input").Put(new GameItem("empty_beer_bottle"));
        crusher.Night();
        Check(earlyBottle.unitCount == 0 && crusherPower.Charge == 12, "The undocked crusher did one normal batch");
        var sameNightBottle = crusher.Slot("input").Put(new GameItem("empty_beer_bottle"));
        Move(crusher, factory.Slot("crusher"));
        factory.Night();
        Check(sameNightBottle.unitCount == 1 && crusherPower.Charge == 12,
            "Docking an already used crusher cannot run a second paid batch that night");
        classifier.Destroy();
        crusher.Destroy();
        StoreStation.Day = day;
        factory.Destroy();
        Console.WriteLine("PASS folded factory native scheduling, glass bypass, same-night sieve feed, stock reserve and physical pickup");
        return kit;

        GameItem FoldedMachine(string slot, string key)
        {
            var bin = factory.Slot(slot);
            bin.ParentItem = factory; // Mirrors native container graph ancestry.
            var item = key switch
            {
                MineralCrusher.Key => MineralCrusher.Definition.Shell(),
                PowderSieve.Key => PowderSieve.Definition.Shell(),
                _ => SinteringStation.Definition.Shell()
            };
            item.identifier = "corp_presence_" + key;
            return bin.Put(item);
        }
    }

    private static FeederTrialState FeederTrialJourney()
    {
        var root = CorpState.Current;
        var shop = EmporiumEntry.Instance;
        var counter = shop.backInvinvElement;
        var priorStock = shop.ListedOutput;
        shop.ListedOutput = counter;
        int capacity = counter.Capacity;
        counter.Capacity = counter.childItems.Count;
        Check(CenFeederTrial.Claim() == FeederTrialResult.Waiting && root.Workshop.FeederTrial?.LoanId == null,
            "A full saved counter leaves the loan available to retry");
        counter.Capacity = capacity + 6;
        Check(CenFeederTrial.Claim() == FeederTrialResult.Done && root.Workshop.FeederTrial is { LoanId: > 0, ToolsRemaining: 0 },
            "The saved counter receives one tagged loan and its tools");
        var machine = CenFeederTrial.FindLoan();
        Check(machine != null, "Find the exact delivered loan");
        int onCounter = counter.childItems.Count;
        root.Workshop.FeederTrial = JsonSerializer.Deserialize<FeederTrialState>(JsonSerializer.Serialize(root.Workshop.FeederTrial));
        Check(CenFeederTrial.Claim() == FeederTrialResult.Done && counter.childItems.Count == onCounter
            && ReferenceEquals(CenFeederTrial.FindLoan(), machine), "Reload never issues a second loan or tool pack");
        CorpState.Current.Characters["rosen"].SideDone.Add(5);
        var offer = WorkshopSaleOffer.MeridianFeederSpec;
        Check(new WorkshopSaleQuote(offer).Availability() == SaleStatus.Unavailable, "Meridian requires a completed bench trial");
        var battery = Cell(machine, 20);
        var box = Box((GameSlotInventory)machine.Slot("receiver"), 0,0,0,0,0);
        var feed = machine.Slot("input").Put(DirectoryMaster.Item("empty_beer_bottle", isOwned: true));
        feed.unitCount = 4;
        machine.Night();
        Check(root.Workshop.FeederTrial.Batches == 1 && feed.unitCount == 2, "A real base shift records one tested batch");
        CenFeederTrial.Record(machine, 2);
        Check(root.Workshop.FeederTrial.Batches == 1, "A duplicate record in the same night does not advance the trial");
        int today = StoreStation.Day;
        StoreStation.Day++;
        machine.Night();
        Check(root.Workshop.FeederTrial.Batches == 2 && root.Workshop.FeederTrial.Valid(), "A second ordinary shift completes the trial");
        Check(CenFeederTrial.Return(machine) == FeederTrialResult.Changed && !root.Workshop.FeederTrial.Returned,
            "Returning equipment with installed inventory is blocked");
        machine.Slot("receiver").Expel(box);
        machine.Slot("power").Expel(battery);
        Check(CenFeederTrial.Return(machine) == FeederTrialResult.Done && root.Workshop.FeederTrial.Returned
            && CenFeederTrial.FindLoan() == null, "The empty loan returns once");
        var shaft = DirectoryMaster.Item("corp_presence_feed_shaft", isOwned: true);
        new RodMaterial(28,8,4,4,1).Write(shaft);
        new RodMaterial(64,18,8,8,2).Write(shaft, RodMaterial.AssayTag);
        var sieve = PowderSieve.Definition.Shell();
        sieve.identifier = "corp_presence_" + PowderSieve.Key;
        var screwdriver = DirectoryMaster.Item("screwdriver", isOwned: true);
        EmporiumEntry.Instance.frontInvinvElement.Put(sieve);
        Check(!MachineService.CanOpen(screwdriver, sieve), "An old sieve stays closed until the spec is paid");
        Check(!IndustrialWorkbench.Learned(IndustrialPump.Key), "The pump recipe stays locked before the spec is paid");
        counter.Put(shaft);
        Check(!root.Workshop.FeederTrial.Handbook && new WorkshopSaleQuote(offer, shaft).Submit() == SaleStatus.Paid
            && root.Workshop.FeederTrial.Handbook, "A standard shaft and real payment unlock the old-machine book");
        root.Workshop.FeederTrial = JsonSerializer.Deserialize<FeederTrialState>(JsonSerializer.Serialize(root.Workshop.FeederTrial));
        Check(root.Workshop.FeederTrial.Handbook && new WorkshopSaleQuote(offer).Check() == SaleStatus.TrialSettled,
            "The first specification and book survive reload without repeat collection");
        Check(MachineService.TryOpen(screwdriver, sieve), "The handbook opens a previously unsupported machine");
        var sievePower = Cell(sieve, 9);
        var sieveInput = Box((GameSlotInventory)sieve.Slot("input"), 100,0,0,0,0);
        sieve.Night();
        Check(sievePower.Charge == 9 && sieveInput.Parts[0] == 100, "The old sieve still requires 12 charge with empty slots");
        var coil = DirectoryMaster.Item("corp_presence_industrial_coil", isOwned: true);
        new RodMaterial(42,6,5,16,1).Write(coil);
        var coilAssay = new RodMaterial(64,7,7,20,2);
        coilAssay.Write(coil, RodMaterial.AssayTag);
        RodCondition.Evaluate(coilAssay, RodProcess.Densify).WriteProcess(coil);
        sieve.Slot("service_coil").Put(coil);
        MachineService.Close(sieve);
        sieve.Night();
        Check(sievePower.Charge == 0 && sieveInput.Parts[0] == 0 && PowderSieve.ReadBin(sieve, 0)[0] == 50,
            "The real installed coil runs the same sieve at 9 charge");
        sieve.Slot("service_coil").Expel(coil);
        Check(RodCondition.Read(coil).Traits.HasFlag(RodTrait.LowLoss), "Uninstalling yields the same physical coil");
        sieve.Destroy();
        PumpJourney();
        StoreStation.Day = today;
        counter.Capacity = capacity;
        shop.ListedOutput = priorStock;
        var handbook = root.Workshop.FeederTrial;
        root.Workshop.FeederTrial = null;
        Console.WriteLine("PASS two-night feeder loan and blank return, first Meridian spec and handbook replay.");
        return handbook;
    }
}
