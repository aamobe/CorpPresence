using System;

namespace CorpPresence;

internal static class SinteringStation
{
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

    private static GameItem Build()
    {
        var battery = new GameSlotInventory();
        var inputs = new GameSlotInventory[BoxSlots];
        for (int i = 0; i < inputs.Length; i++)
            inputs[i] = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) => PowderBox.IsOwnedBox(item) };
        var output = new GameGridInventory(6, 4) { mayInventoryAddItemFunc = (_, _) => false };
        var modules = MineralModuleBay.Create();
        var select = new MachineryButton();
        var recipeName = new MachineryText(uppercase: false);
        var status = new MachineryText(uppercase: false);
        var window = MachineHelper.CreateMachineryWindow(true, new[] { 0, 4, 1, 2, 3 },
            MachineHelper.PowerPanel(battery),
            MachineHelper.Panel(L.K("production.sintering.boxes", "粉盒", "Powder boxes"), MachineryStack.Horizontal(inputs), "input"),
            MachineHelper.Panel(L.K("production.sintering.recipe", "配方", "Recipe"), MachineryStack.Vertical(recipeName, select, status), "control"),
            MachineHelper.Panel(L.K("production.sintering.output", "出料", "Output"), output, "output"),
            MachineHelper.Panel(LocHelper.GetLocalizedMechanic("mech_furnace_label_module"), modules, "module"));
        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");
        machine.EnableTag("STANDARD_MACHINE_TAG");
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineryHelper.InitMachinery(machine, Power);
        machine.ModifyTag("BASE_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(BatchWork));
        machine.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(BatchWork));

        int RecipeIndex() => SinteringRecipes.IndexOf(machine.GetTagReadonly(RecipeTag).GetString());
        bool Writable() => GeneralHelper.IsItemOwned(machine) && SaveSlot.CanWrite("CorpPresence");

        void Refresh()
        {
            var recipe = SinteringRecipes.All[RecipeIndex()];
            recipeName.SetText(recipe.NameKey == null ? Goods.NameOf(recipe.Id) : LocHelper.GetLocalizedItem(recipe.NameKey));
            var plan = Plan(inputs, recipe);
            string message = plan == null
                ? L.K("production.sintering.missing", "粉量或纯度不足", "Insufficient powder or purity")
                : !MachineAppearance.CanFit(output, recipe.Product)
                    ? L.K("production.sintering.full", "请清空出料槽", "Clear the output slot")
                    : !MachineHelper.CanPower(machine, battery)
                        ? L.K("production.sintering.noPower", "电量不足", "Insufficient power")
                        : L.K("production.sintering.ready", "可过夜烧结", "Ready to sinter overnight");
            string requirements = string.Join("\n", Array.ConvertAll(recipe.Ingredients, ingredient =>
                $"{PowderBox.ComponentName(ingredient.Component)} {ingredient.Points} · {(ingredient.MinimumGrade == PowderGrade.Pure ? 99 : 81)}%"));
            string purity = plan != null && recipe.GradeIngot
                ? "\n" + L.F("production.sintering.purity", "预计锭纯度 · {0}", "Expected ingot grade · {0}", GradeName(SinteringBatch.GetIngotGrade(plan.Metal, plan.Total)))
                : "";
            int speed = EffectiveSpeed(machine);
            int power = MachineryHelper.GetMachinePowerUsage(machine);
            status.SetText(requirements + "\n" + message + purity + "\n" +
                L.T($"产能 {speed}% · 每夜 {power} 电", $"Throughput {speed}% · {power} charge/night"));
        }

        select.SetCallback(() => HookGuard.Run("切换烧结配方", () =>
        {
            if (!Writable()) return;
            var next = SinteringRecipes.All[(RecipeIndex() + 1) % SinteringRecipes.All.Count];
            machine.ModifyTag(RecipeTag, tag => tag.Enable().SetString(next.Id));
            Refresh();
        }));
        select.SetText(L.T("切换配方", "Switch recipe"));
        void RefreshFeed()
        {
            if (Writable() && Plan(inputs, SinteringRecipes.All[RecipeIndex()]) == null)
            {
                foreach (var recipe in SinteringRecipes.All)
                {
                    if (Plan(inputs, recipe) == null) continue;
                    machine.ModifyTag(RecipeTag, tag => tag.Enable().SetString(recipe.Id));
                    break;
                }
            }
            Refresh();
        }
        foreach (var input in inputs) Watch(input, RefreshFeed);
        Watch(battery, Refresh);
        MachineHelper.SetupModuleBay(modules, machine, Refresh);
        output.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        window.openCallback += Refresh;
        machine.onCycleEndSlotItemFunc = (_, _, _) => HookGuard.Run("过夜烧结", () =>
        {
            if (!Writable()) return;
            ModuleHelper.ComputeModuleEffect(modules, null, machine);
            ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
            var recipe = SinteringRecipes.All[RecipeIndex()];
            int work = EffectiveSpeed(machine) + Carry(machine);
            int made = 0;
            while (made < work / BatchWork && Process(inputs, output, battery, machine, recipe, drawPower: made == 0))
                made++;
            if (made > 0)
            {
                machine.ModifyTag(CarryTag, tag => tag.Enable().SetInt(work % BatchWork));
                MachineHelper.OnMachineActioned(machine, modules);
            }
            else if (work < BatchWork && ReadyBatch(inputs, battery, machine, recipe) != null
                && MachineAppearance.CanFit(output, recipe.Product))
                machine.ModifyTag(CarryTag, tag => tag.Enable().SetInt(work));
            Refresh();
        });
        MachineAppearance.WatchCycle(machine, output, () =>
        {
            var recipe = SinteringRecipes.All[RecipeIndex()];
            return EffectiveSpeed(machine) + Carry(machine) >= BatchWork
                && ReadyBatch(inputs, battery, machine, recipe) != null ? recipe.Product : null;
        });
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

    private static SinteringBatch Plan(GameSlotInventory[] inputs, SinteringRecipes.Recipe recipe)
    {
        var boxes = new int[inputs.Length][];
        for (int i = 0; i < inputs.Length; i++)
        {
            boxes[i] = new int[PowderBox.ComponentCount];
            var box = inputs[i].childItem;
            if (box == null) continue;
            if (!PowderBox.IsOwnedBox(box) || PowderBox.Read(box, boxes[i]) > PowderColor.Capacity) return null;
        }
        var plan = SinteringBatch.Plan(boxes, recipe.Ingredients, WaterHelper.SplitProportionally);
        if (recipe.GradeIngot && plan != null && SinteringBatch.GetIngotGrade(plan.Metal, plan.Total) == SinteringIngotGrade.None)
            return null;
        return plan;
    }

    private static SinteringBatch ReadyBatch(GameSlotInventory[] inputs, GameSlotInventory battery,
        GameItem machine, SinteringRecipes.Recipe recipe, bool needsPower = true)
    {
        if (!GeneralHelper.IsItemOwned(machine) || !SaveSlot.CanWrite("CorpPresence")
            || !machine.GetTagReadonly(MachineryHelper.IS_MACHINERY_POWERED).GetBool()
            || (needsPower && !MachineHelper.CanPower(machine, battery))
            || !DirectoryMaster.Has<GameItem>(recipe.Product)) return null;
        return Plan(inputs, recipe);
    }

    private static bool Process(GameSlotInventory[] inputs, GameGridInventory output, GameSlotInventory battery,
        GameItem machine, SinteringRecipes.Recipe recipe, bool drawPower)
    {
        var plan = ReadyBatch(inputs, battery, machine, recipe, drawPower);
        if (plan == null) return false;

        var product = DirectoryMaster.Item(recipe.Product, isOwned: true);
        if (product == null) return false;
        bool committed = false;
        try
        {
            if (recipe.GradeIngot)
                IngotPurityHelper.SetPurity(product, NativeGrade(SinteringBatch.GetIngotGrade(plan.Metal, plan.Total)));
            if (!ProductionOutput.TryAdd(output, product) || (drawPower && !MachineHelper.TryDrawCyclePower(machine, battery))) return false;
            for (int i = 0; i < inputs.Length; i++)
            {
                if (plan.Used[i]) PowderBox.WriteComposition(inputs[i].childItem, plan.Remaining[i]);
            }
            committed = true;
            return true;
        }
        finally
        {
            if (!committed) product.Destroy();
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
