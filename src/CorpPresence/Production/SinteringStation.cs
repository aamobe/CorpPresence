using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class SinteringStation
{
    private const string TrimTag = "CORP_SINTERING_TRIM";
    private const int DieRetention = 5;
    private readonly record struct CastingSetup(int Charge, bool Trim, int Retention);

    internal static bool DieStock(RodCondition stock) => stock is { Process: RodProcess.Densify, Jacket: null,
        Core: { Load: >= 55, Wear: >= 45, Porosity: <= 3 } };

    internal const string Key = "sintering_station";
    private const string RecipeTag = "CORP_SINTERING_RECIPE";
    private const string CarryTag = "CORP_SINTERING_CARRY";
    private const int BatchWork = 100;
    private const int BoxSlots = 3;
    private const int Power = 12;

    internal static readonly Goods.Def Definition = new()
    {
        Name = "综合烧结工作站", NameEn = "Sintering Workstation", Value = 320,
        Company = "mineral", Art = "sintering-station", Fallback = "furnace",
        W = 3, H = 3, Types = new[] { "MACHINE" }, Shell = Build
    };

    internal static string RecipeName(SinteringRecipes.Recipe recipe) =>
        recipe.NameKey == null ? Goods.NameOf(recipe.Id) : LocHelper.GetLocalizedItem(recipe.NameKey);

    internal static bool Unlocked(SinteringRecipes.Recipe recipe) =>
        recipe.Id switch
        {
            IndustrialRecipes.Billet or IndustrialRecipes.Blank
                or SinteringRecipes.StandardRod or SinteringRecipes.WearRod or SinteringRecipes.ConductiveRod or SinteringRecipes.TrialRod
                => LowerMachinist.MineralizerUnlocked(CorpState.Current),
            "hearth_tile" => LowerMachinist.StarterUnlocked(CorpState.Current),
            _ => true
        };

    private static GameItem Build()
    {
        var battery = new GameSlotInventory();
        var inputs = new GameSlotInventory[BoxSlots];
        for (int i = 0; i < inputs.Length; i++)
            inputs[i] = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) => PowderBox.IsOwnedBox(item) };
        var output = new GameGridInventory(9, 6) { mayInventoryAddItemFunc = (_, _) => false };
        var modules = MineralModuleBay.Create();
        var coil = new GameSlotInventory(2, 2) { mayInventoryAddItemFunc = (item, _) => MachineService.Handbook
            && MachineService.IsStockPart(item, RodMaterial.IndustrialCoil) };
        var die = new GameSlotInventory(2, 2) { mayInventoryAddItemFunc = (item, _) => MachineService.Handbook
            && MachineService.IsStockPart(item, RodMaterial.CastingDie) && DieStock(RodCondition.Read(item)) };
        var coilPanel = MachineHelper.Panel(Goods.NameOf(RodMaterial.IndustrialCoil), coil, "service_coil").SetHidden(true);
        var diePanel = MachineHelper.Panel(Goods.NameOf(RodMaterial.CastingDie), die, "service_die").SetHidden(true);
        var solid = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) =>
            LowerMachinist.MineralizerUnlocked(CorpState.Current) && GeneralHelper.IsItemOwned(item)
                && (Goods.IdOf(item) == ClassifierGoods.Coarse && item.unitCount > 0
                    || item.unitCount == 1 && (RodMaterial.IsRod(item.identifier) || RodMaterial.IsShortRod(item.identifier))) };
        GameItem machine = null;
        var select = new MachineryButton();
        var closeCover = new MachineryButton(L.T("合上检修盖", "Close cover"));
        var recipeName = new MachineryText(uppercase: false);
        var status = new MachineryText(uppercase: false);
        var tabs = new MachineryTabs();
        tabs.AddTab(L.K("production.sintering.recipe", "配方", "Recipe"), MachineryStack.Vertical(recipeName, select, status,
            MineralNightReport.Button(() => machine, MineralNightReport.Sinter), closeCover));
        Action refreshTrial = null;
        Action refreshTreatment = null;
        var trial = TrialEditor(() => machine, inputs, Writable, Setup, Refresh, () => SetRecipe(SinteringRecipes.TrialRod),
            out var reloadTrial, out refreshTrial);
        tabs.AddTab(L.T("试配", "Trial blend"), trial);
        var treatment = TreatmentEditor(() => machine, solid, battery, Charge, () =>
        {
            MachineAppearance.Rearm(machine);
            Refresh();
        }, out refreshTreatment);
        tabs.AddTab(L.T("组织处理", "Treatment"), treatment);
        tabs.onChanged += _ => { refreshTrial(); refreshTreatment(); };
        var window = MachineHelper.CreateMachineryWindow(true, new[] { 0, 4, 6, 7, 1, 5, 2, 3 },
            MachineHelper.PowerPanel(battery),
            MachineHelper.Panel(L.K("production.sintering.boxes", "粉盒", "Powder boxes"), MachineryStack.Horizontal(inputs), "input"),
            MachineHelper.Panel(L.K("production.sintering.recipe", "配方", "Recipe"), tabs, "control"),
            MachineHelper.Panel(L.K("production.sintering.output", "出料", "Output"), output, "output"),
            MachineHelper.Panel(LocHelper.GetLocalizedMechanic("mech_furnace_label_module"), modules, "module"),
            MachineHelper.Panel(L.K("workshop.industrial.solid", "固体料", "Solid feed"), solid, "solid"),
            coilPanel, diePanel);
        machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        MachineService.Register(machine, () => machine.identifier == "corp_presence_" + Key && MachineService.Handbook, visible =>
        {
            coilPanel.SetHidden(!visible);
            diePanel.SetHidden(!visible);
        });
        closeCover.SetCallback(() => MachineService.Close(machine));
        machine.EnableTag("CONTAINER_TAG");
        machine.EnableTag("STANDARD_MACHINE_TAG");
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineryHelper.InitMachinery(machine, Power);
        machine.ModifyTag("BASE_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(BatchWork));
        machine.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(BatchWork));

        int RecipeIndex() => SinteringRecipes.IndexOf(machine.GetTagReadonly(RecipeTag).GetString());
        bool Writable() => GeneralHelper.IsItemOwned(machine) && SaveSlot.CanWrite("CorpPresence");
        int Charge() => MachineService.CoilCharge(machine, coil.childItem);
        CastingSetup Setup() => new(Charge(), MachineService.Handbook && machine.GetTagReadonly(TrimTag).GetBool(),
            MachineService.Handbook && MachineService.IsStockPart(die.childItem, RodMaterial.CastingDie)
            && DieStock(RodCondition.Read(die.childItem)) ? DieRetention : 0);
        void SetRecipe(string id)
        {
            RodTreatmentJob.Clear(machine);
            machine.ModifyTag(RecipeTag, tag => tag.Enable().SetString(id));
            MachineAppearance.Rearm(machine);
            Refresh();
        }

        void Refresh()
        {
            var recipe = SinteringRecipes.All[RecipeIndex()];
            string quantity = recipe.OutputCount > 1 ? $" × {recipe.OutputCount}" : "";
            recipeName.SetText(RecipeName(recipe) + quantity);
            var setup = Setup();
            var plan = Plan(inputs, solid, machine, recipe, setup);
            string message = RodTreatmentJob.HasQueue(machine) ? L.T("已排组织处理 · 今夜处理固体料槽的棒", "Treatment queued · the rod in solid feed runs tonight")
                : plan == null
                ? L.K("production.sintering.missing", "粉量或纯度不足", "Insufficient powder or purity")
                : !MachineAppearance.CanFit(output, recipe.Product, recipe.OutputCount, separateUnit: true)
                    ? L.K("production.sintering.full", "请清空出料槽", "Clear the output slot")
                    : !WorkshopCycle.CanDrawPower(machine, battery, setup.Charge)
                        ? L.K("production.sintering.noPower", "电量不足", "Insufficient power")
                        : L.K("production.sintering.ready", "可过夜烧结", "Ready to sinter overnight");
            string requirements = string.Join("\n", Array.ConvertAll(recipe.Ingredients, ingredient =>
            {
                int available = 0;
                foreach (var slot in inputs)
                {
                    var box = slot.childItem;
                    if (!PowderBox.IsOwnedBox(box)) continue;
                    var parts = new int[PowderBox.ComponentCount];
                    if (PowderBox.Read(box, parts) > PowderColor.Capacity) continue;
                    var powder = PowderClassification.FromParts(parts);
                    if (powder.Branch == ingredient.Component && powder.Grade >= ingredient.MinimumGrade)
                        available += parts[(int)ingredient.Component];
                }
                return L.F("workshop.sinter.need", "{0} {1}/{2}（≥{3}%）· 差 {4} 点",
                    "{0} {1}/{2} (≥{3}%) · short {4} points", PowderBox.ComponentName(ingredient.Component),
                    available, ingredient.Points, ingredient.MinimumGrade == PowderGrade.Pure ? 99 : 81,
                    Math.Max(0, ingredient.Points - available));
            }));
            if (recipe.Solid != null)
                requirements = Goods.NameOf(recipe.Solid) + " × 1";
            if (recipe.Id == SinteringRecipes.TrialRod)
            {
                var blend = TrialAlloy.Read(machine);
                requirements = blend == null ? L.T("请重新确认试配配比", "Confirm a new trial blend") :
                    MineralNightReport.Order + "\n" + MineralNightReport.Vector(blend);
            }
            string purity = plan != null && recipe.GradeIngot
                ? "\n" + L.F("production.sintering.purity", "预计锭纯度 · {0}", "Expected ingot grade · {0}", GradeName(SinteringBatch.GetIngotGrade(plan.Metal, plan.Total)))
                : "";
            int speed = EffectiveSpeed(machine);
            status.SetText(requirements + "\n" + message + purity + "\n" +
                L.T($"产能 {speed}% · 每夜 {setup.Charge} 电", $"Throughput {speed}% · {setup.Charge} charge/night"));
            refreshTrial();
            refreshTreatment();
        }

        select.SetCallback(() => HookGuard.Run("切换烧结配方", () =>
        {
            if (!Writable()) return;
            int index = (RecipeIndex() + 1) % SinteringRecipes.All.Count;
            while (!Unlocked(SinteringRecipes.All[index])) index = (index + 1) % SinteringRecipes.All.Count;
            SetRecipe(SinteringRecipes.All[index].Id);
        }));
        select.SetText(L.T("切换配方", "Switch recipe"));
        void RefreshFeed()
        {
            var selected = SinteringRecipes.All[RecipeIndex()];
            var setup = Setup();
            if (!SaveManager.IsDecoding && Writable() && !RodTreatmentJob.HasQueue(machine) && selected.Id != SinteringRecipes.TrialRod
                && Plan(inputs, solid, machine, selected, setup) == null)
            {
                foreach (var recipe in SinteringRecipes.All)
                {
                    if (recipe.Id == SinteringRecipes.TrialRod || Plan(inputs, solid, machine, recipe, setup) == null) continue;
                    machine.ModifyTag(RecipeTag, tag => tag.Enable().SetString(recipe.Id));
                    break;
                }
            }
            Refresh();
        }
        foreach (var input in inputs) Watch(input, RefreshFeed);
        Watch(solid, RefreshFeed);
        Watch(battery, Refresh);
        Watch(coil, Refresh);
        Watch(die, Refresh);
        MachineHelper.SetupModuleBay(modules, machine, Refresh);
        output.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        window.openCallback += () => { MachineService.OpenWindow(machine); reloadTrial(); Refresh(); };
        window.closeCallback += () => MachineService.Close(machine);
        FoldedFactory.RegisterFurnace(machine, inputs, () =>
        {
            var selected = SinteringRecipes.All[RecipeIndex()];
            return !RodTreatmentJob.HasQueue(machine) && Unlocked(selected) ? selected : null;
        }, output, () =>
        {
            if (!Writable()) return;
            ModuleHelper.ComputeModuleEffect(modules, null, machine);
            ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
            var setup = Setup();
            if (RodTreatmentJob.HasQueue(machine))
            {
                var process = RodTreatmentJob.Read(machine);
                var result = RodTreatmentJob.Run(machine, solid.childItem, battery, setup.Charge);
                bool done = result == MineralStop.Ready;
                string processId = process > RodProcess.Cast ? RodCondition.Code(process) : "unknown";
                MineralNightReport.Save(machine, MineralNightReport.Sinter, result, done ? 1 : 0,
                    done ? setup.Charge : 0,
                    recipe: MineralNightReport.RodTreatmentPrefix + processId);
                Refresh();
                return;
            }
            var recipe = SinteringRecipes.All[RecipeIndex()];
            int work = EffectiveSpeed(machine) + Carry(machine);
            int made = 0;
            var consumed = new int[PowderBox.ComponentCount];
            var lost = new int[PowderBox.ComponentCount];
            while (made < work / BatchWork && Process(inputs, solid, output, battery, machine, recipe, setup,
                consumed, lost, drawPower: made == 0))
                made++;
            if (made > 0)
            {
                machine.ModifyTag(CarryTag, tag => tag.Enable().SetInt(work % BatchWork));
                MachineHelper.OnMachineActioned(machine, modules);
            }
            var reason = MineralStop.Ready;
            if (made == 0)
            {
                if (!Unlocked(recipe)) reason = MineralStop.Locked;
                else if (Plan(inputs, solid, machine, recipe, setup) == null) reason = MineralStop.Insufficient;
                else if (!MachineAppearance.CanFit(output, recipe.Product, recipe.OutputCount, separateUnit: true)) reason = MineralStop.Full;
                else if (!machine.GetTagReadonly(MachineryHelper.IS_MACHINERY_POWERED).GetBool()
                    || !WorkshopCycle.CanDrawPower(machine, battery, setup.Charge)) reason = MineralStop.NoPower;
                else if (work < BatchWork && ReadyBatch(inputs, solid, battery, machine, recipe, setup) != null)
                {
                    machine.ModifyTag(CarryTag, tag => tag.Enable().SetInt(work));
                    reason = MineralStop.Carry;
                }
                else reason = MineralStop.Full;
            }
            MineralNightReport.Save(machine, MineralNightReport.Sinter, reason, made, made > 0 ? setup.Charge : 0,
                consumed, tailings: lost, recipe: recipe.Id);
            Refresh();
        });
        MachineAppearance.WatchCycle(machine, output, () =>
        {
            var recipe = SinteringRecipes.All[RecipeIndex()];
            return EffectiveSpeed(machine) + Carry(machine) >= BatchWork
                && ReadyBatch(inputs, solid, battery, machine, recipe, Setup()) != null ? recipe.Product : null;
        }, () => SinteringRecipes.All[RecipeIndex()].OutputCount, separateUnit: true,
            inPlaceReady: () => RodTreatmentJob.HasQueue(machine) ? RodTreatmentJob.Ready(machine, solid.childItem, battery, Charge()) : null);
        Refresh();
        return machine;
    }

    private static int EffectiveSpeed(GameItem machine) =>
        Math.Max(0, machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt()
            + machine.GetTagReadonly("TOTAL_PERCENTAGE_QUALITY_BONUS_INT").GetInt());

    private static int Carry(GameItem machine) => Math.Clamp(machine.GetTagReadonly(CarryTag).GetInt(), 0, BatchWork - 1);

    private static void Watch(GameInventory inventory, Action refresh)
    {
        inventory.onSlotAddItemEndFunc += (_, _, _) => refresh();
        inventory.onSlotRemoveItemEndFunc += (_, _, _) => refresh();
    }

    private static SinteringBatch Plan(GameSlotInventory[] inputs, GameSlotInventory solid, GameItem machine,
        SinteringRecipes.Recipe recipe, CastingSetup setup, int[][] snapshot = null)
    {
        if (!Unlocked(recipe)) return null;
        if (recipe.Solid != null && (Goods.IdOf(solid.childItem) != recipe.Solid || solid.childItem.unitCount < 1
            || !GeneralHelper.IsItemOwned(solid.childItem))) return null;
        var boxes = snapshot ?? ReadBoxes(inputs);
        if (boxes == null) return null;
        if (recipe.Id == SinteringRecipes.TrialRod)
            return TrialAlloy.Plan(boxes, TrialAlloy.Read(machine), out _, setup.Trim, setup.Retention);
        var plan = SinteringBatch.Plan(boxes, recipe.Ingredients, WaterHelper.SplitProportionally, recipe.Exact);
        if (recipe.GradeIngot && plan != null && SinteringBatch.GetIngotGrade(plan.Metal, plan.Total) == SinteringIngotGrade.None)
            return null;
        return plan;
    }

    private static int[][] ReadBoxes(GameSlotInventory[] inputs)
    {
        var boxes = new int[inputs.Length][];
        for (int i = 0; i < inputs.Length; i++)
        {
            boxes[i] = new int[PowderBox.ComponentCount];
            var box = inputs[i].childItem;
            if (box == null) continue;
            if (!PowderBox.IsOwnedBox(box) || PowderBox.Read(box, boxes[i]) > PowderColor.Capacity) return null;
        }
        return boxes;
    }

    private static SinteringBatch ReadyBatch(GameSlotInventory[] inputs, GameSlotInventory solid, GameSlotInventory battery,
        GameItem machine, SinteringRecipes.Recipe recipe, CastingSetup setup, bool needsPower = true, int[][] snapshot = null)
    {
        if (!GeneralHelper.IsItemOwned(machine) || !SaveSlot.CanWrite("CorpPresence")
            || !machine.GetTagReadonly(MachineryHelper.IS_MACHINERY_POWERED).GetBool()
            || (needsPower && !WorkshopCycle.CanDrawPower(machine, battery, setup.Charge))
            || !DirectoryMaster.Has<GameItem>(recipe.Product)) return null;
        return Plan(inputs, solid, machine, recipe, setup, snapshot);
    }

    private static bool Process(GameSlotInventory[] inputs, GameSlotInventory solid, GameGridInventory output, GameSlotInventory battery,
        GameItem machine, SinteringRecipes.Recipe recipe, CastingSetup setup, int[] consumed, int[] lost, bool drawPower)
    {
        var sources = Array.ConvertAll(inputs, input => input.childItem);
        var before = ReadBoxes(inputs);
        if (before == null) return false;
        var plan = ReadyBatch(inputs, solid, battery, machine, recipe, setup, drawPower, before);
        if (plan == null) return false;

        RodMaterial material = null;
        if (RodMaterial.IsRod(recipe.Product))
        {
            material = RodMaterial.ForOutput(plan.Cast ?? plan.Spent, recipe.OutputCount);
            if (material == null) return false;
        }

        var products = new List<GameItem>(recipe.OutputCount);
        var session = StoreSession.Capture();
        var cell = battery.childItem;
        var solidFeed = solid.childItem;
        bool committed = false;
        try
        {
            for (int i = 0; i < recipe.OutputCount; i++)
            {
                var product = DirectoryMaster.Item(recipe.Product, isOwned: true);
                if (product == null) return false;
                products.Add(product);
                if (recipe.GradeIngot)
                    IngotPurityHelper.SetPurity(product, NativeGrade(SinteringBatch.GetIngotGrade(plan.Metal, plan.Total)));
                material?.Write(product);
            }
            if (!ProductionOutput.TryAddSeparateBatch(output, products)) return false;
            if (!session.IsCurrent || (drawPower && battery.childItem != cell)
                || (recipe.Solid != null && (solid.childItem != solidFeed || solidFeed.unitCount < 1
                    || !GeneralHelper.IsItemOwned(solidFeed)))) return false;
            var current = ReadBoxes(inputs);
            if (current == null) return false;
            for (int i = 0; i < inputs.Length; i++)
                if (plan.Used[i] && (inputs[i].childItem != sources[i] || !current[i].SequenceEqual(before[i]))) return false;
            if (drawPower && !WorkshopCycle.DrawPower(machine, battery, setup.Charge)) return false;
            if (recipe.Solid != null) solidFeed.unitCount--;
            for (int i = 0; i < inputs.Length; i++)
            {
                if (!plan.Used[i]) continue;
                PowderBox.CommitComposition(sources[i], plan.Remaining[i]);
            }
            for (int part = 0; part < consumed.Length; part++) consumed[part] += plan.Spent[part];
            if (plan.Cast != null)
                for (int part = 0; part < lost.Length; part++) lost[part] += plan.Spent[part] - plan.Cast[part];
            committed = true;
            if (recipe.Solid != null) HookGuard.Run("烧结固体料刷新", () =>
            {
                if (solidFeed.unitCount == 0) solidFeed.Destroy();
                else solidFeed.SetUnitCount(solidFeed.unitCount);
            });
            for (int i = 0; i < inputs.Length; i++)
                if (plan.Used[i]) HookGuard.Run("烧结粉盒刷新", () => PowderBox.RefreshComposition(sources[i], plan.Remaining[i]));
            return true;
        }
        finally
        {
            if (!committed) ProductionOutput.DiscardBatch(products);
        }
    }

    private static string NativeGrade(SinteringIngotGrade grade) => grade switch
    {
        SinteringIngotGrade.Perfect => IngotPurityHelper.INGOT_PURITY_PERFECT,
        SinteringIngotGrade.VeryHigh => IngotPurityHelper.INGOT_PURITY_VERY_HIGH,
        _ => IngotPurityHelper.INGOT_PURITY_HIGH
    };

    private static string GradeName(SinteringIngotGrade grade) => grade switch
    {
        SinteringIngotGrade.Perfect => L.K("production.sintering.perfect", "完美", "Perfect"),
        SinteringIngotGrade.VeryHigh => L.K("production.sintering.veryHigh", "极高", "Very high"),
        _ => L.K("production.sintering.high", "高纯", "High")
    };
}
