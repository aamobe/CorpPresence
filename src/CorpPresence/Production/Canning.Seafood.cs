using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class Canning
{
    private const string FishMinceId = "corp_presence_fish_mince";
    internal const string SeafoodSealerOrder = "@seafood_sealer";
    private const string FishModeTag = "CORP_SEAL_WHOLE_FISH_BOOL";
    private const string GalaxyModeTag = "CORP_SEAL_GALAXY_BLEND_BOOL";
    private const string SealerCarryTag = "CORP_SEAL_CARRY_INT";
    private const string BatteryEnergyTag = "power_source_item_energy";
    private const int WaterMl = 100;
    private const int SealPower = 6;
    private const int CannerBaseValue = 350;

    private static Goods.Def SeafoodFood(string zh, string en, int price, string art, int kcal, bool canned, bool luxury = false)
    {
        return new Goods.Def
        {
            Name = zh, NameEn = en, Value = price, Company = "davre", Manufacturer = "davre",
            Art = art, Fallback = "processed_meat", W = 2, H = 1,
            Types = luxury ? new[] { "FOOD", "PROCESSED_FOOD", "LUXURY_ITEM" } : new[] { "FOOD" },
            Shell = Empty,
            Setup = item =>
            {
                if (canned) AudioHelper.InitCannedFood(item);
                else AudioHelper.InitFoodAudio(item);
                FoodItemHelper.InitFoodItem(item, kcal, isCanned: canned, isMeat: true);
            }
        };
    }

    private static Goods.Def SeafoodSealerDef() => new Goods.Def
    {
        Name = "封装机", NameEn = "Sealer",
        Value = CannerBaseValue, Company = "davre", Manufacturer = "davre",
        Art = "seafood_sealer", Fallback = "furnace", W = 3, H = 3,
        Types = new[] { "MACHINE" }, Shell = BuildSeafoodSealer,
        Desc = "过夜或充能时封装，每次启动默认耗 6 电。蟹肉棒档每批用 4 份鱼肉糜，槽中须放 1 个可重复使用的 3D 打印耗材。鱼罐头档每批用 1 条已宰杀、未切片的亚成年普通罗非鱼。这两档每批需 100 毫升基础水或更高档的水。繁星果糊档每批用 1 份营养果糊制成 1 份繁星果糊，无需水或耗材。性能模组增加批次，效率模组降低耗电。",
        DescEn = "Runs overnight or when turbo boosted, using 6 charge per run by default. Crab Stick mode uses 4 fish mince per batch and requires a reusable printer plastic item. Canned Fish mode uses one dead, whole adolescent common tilapia per batch. Both modes use 100 mL of Basewater or better per batch. Galaxy Blend mode turns 1 nutrifruit paste into 1 Galaxy Blend without water or printer plastic. Performance modules add batches and efficiency modules reduce power use."
    };

    private static bool IsWholeTilapia(GameItem item) =>
        item != null && AquacultureHelper.IsFish(item) && !AquacultureHelper.IsGroup(item) &&
        AquacultureHelper.GetSpecies(item) == FishSpecies.Tilapia &&
        AquacultureHelper.IsDead(item) && AquacultureHelper.GetStage(item) == FishStage.Adolescent;

    private static bool IsWaterContainer(GameItem item) => item != null && item.IsTag("LIQUID_CONTAINER_TAG");

    private static GameItem BuildSeafoodSealer()
    {
        var battery = new GameSlotInventory();
        var water = new GameSlotInventory(1, 3, false, false);
        var modules = new GameGridInventory("10000010001000001110000010001000001", 7);
        var ingredients = new GameGridInventory(6, 4);
        var output = new GameGridInventory(4, 2);
        var plastic = new GameSlotInventory();
        GameItem machine = null;
        var crab = new MachineryRadioButton(L.T("蟹肉棒档", "Crab Stick"), true, on =>
        {
            if (!on || machine == null) return;
            machine.DisableTag(FishModeTag);
            machine.DisableTag(GalaxyModeTag);
        });
        var fish = new MachineryRadioButton(L.T("鱼罐头档", "Canned Fish"), false, on =>
        {
            if (!on || machine == null) return;
            machine.EnableTag(FishModeTag);
            machine.DisableTag(GalaxyModeTag);
        });
        var galaxy = new MachineryRadioButton(L.T("繁星果糊档", "Galaxy Blend"), false, on =>
        {
            if (!on || machine == null) return;
            machine.DisableTag(FishModeTag);
            machine.EnableTag(GalaxyModeTag);
        }).SetDescription(L.T("1 份营养果糊 → 1 份繁星果糊（无需水或耗材）",
            "1 nutrifruit paste → 1 Galaxy Blend (no water or printer plastic)"));
        new MachineryRadioGroup().Add(crab).Add(fish).Add(galaxy);
        var check = new MachineryButton(L.T("检查生产条件", "Check Production"));
        var modeControls = MachineryStack.Vertical(crab, fish, galaxy, check).SetAlign(MachineryStack.Align.Start);
        // Display the module bay after power while keeping the saved inventory order.
        var window = MachineHelper.CreateMachineryWindow(true, new[] { 0, 6 }, MachineHelper.PowerPanel(battery),
            MachineHelper.Panel(L.T("水槽", "Water"), water, "water"),
            MachineHelper.Panel(L.T("原料", "Ingredients"), ingredients, "input"),
            MachineHelper.Panel(L.T("档位", "Mode"), modeControls, "control"),
            MachineHelper.Panel(L.T("成品", "Output"), output, "output"),
            MachineHelper.Panel(L.T("3D 打印耗材（可复用）", "Reusable 3D Material"), plastic, "plastic"),
            MachineHelper.Panel(L.T("模组", "Modules"), modules, "module"));
        machine = Empty().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");
        machine.EnableTag("STANDARD_MACHINE_TAG");
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineryHelper.InitMachinery(machine, SealPower);
        machine.ModifyTag("BASE_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(SpeedScale));
        machine.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(SpeedScale));
        MachineHelper.SetupModuleBay(modules, machine);
        MachineUpgrade.Attach(machine, 2, ingredients, output);
        water.mayInventoryAddItemFunc = (item, _) => GeneralHelper.IsItemOwned(item) && IsWaterContainer(item);
        ingredients.mayInventoryAddItemFunc = (item, _) => GeneralHelper.IsItemOwned(item) &&
            (item.identifier == FishMinceId || item.identifier == PasteId || IsWholeTilapia(item));
        plastic.mayInventoryAddItemFunc = (item, _) => GeneralHelper.IsItemOwned(item) && item.identifier == "printer_plastic";
        output.mayInventoryAddItemFunc = (_, _) => false;
        void SyncMode() => (machine.IsTag(GalaxyModeTag) ? galaxy : machine.IsTag(FishModeTag) ? fish : crab).SetOn(on: true);
        machine.onLoaded += _ => SyncMode();
        window.openCallback += SyncMode;
        check.SetCallback(() =>
        {
            StoreUIManager.Instance?.Notify(RunSealer(machine, battery.childItem, modules, water.childItem, ingredients, plastic, output, inspect: true));
        });
        machine.onCycleEndSlotItemFunc = (_, _, _) =>
        {
            try { RunSealer(machine, battery.childItem, modules, water.childItem, ingredients, plastic, output, inspect: false); }
            catch (Exception error) { ModLog.Warn("水产封装结算失败：" + error); }
        };
        return machine;
    }

    private static bool TakeIngredients(GameInventory inventory, Func<GameItem, bool> accepts, int count,
        List<(GameItem Item, int Count)> batch)
    {
        foreach (var item in inventory.childItems)
        {
            if (item == null || item.unitCount <= 0 || !GeneralHelper.IsItemOwned(item) || !accepts(item)) continue;
            int taken = Math.Min(count, item.unitCount);
            batch.Add((item, taken));
            count -= taken;
            if (count == 0) return true;
        }
        return false;
    }

    private static string RunSealer(GameItem machine, GameItem battery, GameInventory modules, GameItem water,
        GameInventory ingredients, GameSlotInventory plastic, GameInventory output, bool inspect)
    {
        ModuleHelper.ComputeModuleEffect(modules.childItems.ToList(), machine);
        ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
        int power = MachineryHelper.GetMachinePowerUsage(machine);
        int speed = Math.Max(0, machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt());
        int progress = speed + machine.GetTagReadonly(SealerCarryTag).GetInt();
        int batches = progress / SpeedScale;
        if (batches <= 0) return L.T("加工速度不足。", "Processing speed is too low.");
        if (battery == null) return L.T("请放入供电设备。", "Install a power source.");

        int done = 0;
        try
        {
            for (; done < batches; done++)
            {
                string status = SealBatch(machine, battery, done == 0 ? power : 0, water, ingredients, plastic, output,
                    inspect, out bool sealedBatch);
                if (inspect || !sealedBatch) return status;
            }
            return L.T("已完成封装。", "Sealing complete.");
        }
        finally
        {
            if (done > 0)
            {
                machine.ModifyTag(SealerCarryTag, tag => tag.Enable().SetInt(progress % SpeedScale));
                MachineHelper.OnMachineActioned(machine, modules);
            }
        }
    }

    private static string SealBatch(GameItem machine, GameItem battery, int power, GameItem water,
        GameInventory ingredients, GameSlotInventory plastic, GameInventory output, bool inspect, out bool sealedBatch)
    {
        sealedBatch = false;
        bool galaxy = machine.IsTag(GalaxyModeTag);
        bool fish = machine.IsTag(FishModeTag);
        bool usesWater = !galaxy;
        var batch = new List<(GameItem Item, int Count)>();
        if (galaxy)
        {
            if (!TakeIngredients(ingredients, item => item.identifier == PasteId, 1, batch))
                return L.T("需要 1 份营养果糊。", "Load 1 nutrifruit paste.");
        }
        else if (fish)
        {
            if (!TakeIngredients(ingredients, IsWholeTilapia, 1, batch))
                return L.T("需要一条已宰杀、未切片的亚成年普通罗非鱼。", "Load one dead, whole adolescent common tilapia.");
        }
        else if (!TakeIngredients(ingredients, item => item.identifier == FishMinceId, 4, batch) ||
                 plastic.childItem?.identifier != "printer_plastic")
        {
            return L.T("需要 4 份鱼肉糜和 1 个 3D 打印耗材。", "Load 4 fish mince and 1 printer plastic.");
        }

        if (usesWater)
        {
            if (!IsWaterContainer(water) || WaterHelper.GetTotalVolume(water) < WaterMl * 1000)
                return L.T("需要一个装有至少 100 毫升水的容器。", "Load a container with at least 100 mL of water.");
            if (WaterFeatureHelper.GetPurityArrayIndex(WaterHelper.GetWaterPurity(water)) > 2)
                return L.T("水质须为基础水或更高档。", "Use Basewater or better.");
        }
        if (power > 0 && !PowerHelper.CanDrawPowerSource(battery, power))
            return L.T($"需要 {power} 电。", $"{power} charge is required.");

        var product = galaxy ? ItemRegistry.Create("galaxy_blend") : Goods.Make(fish ? "fish_can" : "crab_stick");
        if (product == null) return L.T("成品尚未就绪。", "The product is unavailable.");
        bool committed = false;
        try
        {
            GeneralHelper.SetItemOwned(product);
            if (batch.Any(input => input.Item.IsTag("ANIMAL_PRION_TAG")))
                product.EnableTag("ANIMAL_PRION_TAG");
            if (inspect)
            {
                var previous = output.mayInventoryAddItemFunc;
                output.mayInventoryAddItemFunc = (_, _) => true;
                try
                {
                    return GraphUtils.CanAccept(output, product)
                        ? L.T("首批条件满足，可过夜生产或使用充能器。", "First batch ready for overnight production or turbo boost.")
                        : L.T("成品槽空间不足。", "The output tray is full.");
                }
                finally { output.mayInventoryAddItemFunc = previous; }
            }
            if (!ProductionOutput.TryAdd(output, product))
                return L.T("成品槽空间不足。", "The output tray is full.");

            var liquids = usesWater ? Liquid.Liquids.Select(liquid => liquid.currentPart).ToArray() : Array.Empty<string>();
            var amounts = liquids.Select(tag => water.GetTagReadonly(tag).GetInt()).ToArray();
            int charge = power > 0 ? battery.GetTagReadonly(BatteryEnergyTag).GetInt() : 0;
            bool measureCharge = power > 0 && battery.IsTag("power_source_item") &&
                !CurrentConverterHelper.IsCurrentConverter(battery) &&
                !HypertechBatteryHelper.IsHypertechBattery(battery);
            bool paid = false;
            try
            {
                bool waterPaid = true;
                if (usesWater)
                {
                    int volumeBefore = WaterHelper.GetTotalVolume(water);
                    WaterHelper.RemoveMl(water, WaterMl);
                    waterPaid = volumeBefore - WaterHelper.GetTotalVolume(water) == WaterMl * 1000;
                }
                if (waterPaid)
                {
                    paid = power == 0 || PowerHelper.DrawPowerSource(battery, power);
                    if (measureCharge)
                    {
                        int remaining = battery.GetTagReadonly(BatteryEnergyTag).GetInt();
                        ModLog.Info($"封装机扣电：{charge} → {remaining}，应扣 {power} 电，供电结果={paid}");
                        paid = paid && charge - remaining == power;
                    }
                }
            }
            finally
            {
                if (!paid)
                {
                    for (int i = 0; i < liquids.Length; i++)
                    {
                        int amount = amounts[i];
                        if (water.GetTagReadonly(liquids[i]).GetInt() != amount)
                            water.ModifyTag(liquids[i], tag => tag.Enable().SetInt(amount));
                    }
                    if (measureCharge && battery.GetTagReadonly(BatteryEnergyTag).GetInt() != charge)
                    {
                        battery.ModifyTag(BatteryEnergyTag, tag => tag.Enable().SetInt(charge));
                        PowerHelper.UpdateEnergyCreditSprite(battery);
                    }
                    if (usesWater) WaterFeatureHelper.OnLiquidContainerPourOutOf(water);
                }
            }
            if (!paid) return usesWater
                ? L.T("扣水或供电失败，本批未封装。", "Water or power draw failed. No batch was sealed.")
                : L.T("供电失败，本批未封装。", "Power draw failed. No batch was sealed.");

            // Irreversible ingredient disposal begins after the water and power commit.
            committed = true;
            sealedBatch = true;
            foreach (var (item, count) in batch)
            {
                if (item.unitCount == count) item.Destroy();
                else item.SetUnitCount(item.unitCount - count);
            }
            return L.T("已封装一批。", "Sealed one batch.");
        }
        finally
        {
            if (!committed) product.Destroy();
        }
    }
}
