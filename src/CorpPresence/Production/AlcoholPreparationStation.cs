using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.UI;

namespace CorpPresence;

internal static class AlcoholPreparationStation
{
    internal const string Key = "alcohol_preparation_station";
    private const string FruitReserve = "CORP_ALCOHOL_FRUIT_KCAL";
    private const int FruitCost = 240;
    private const int BasePower = 8;
    private const int WaterUl = 100_000;
    private const int AlcoholUl = 30_000;

    internal static readonly Goods.Def Definition = new()
    {
        Name = "酒精制备站", NameEn = "Alcohol Preparation Station",
        Company = "cartel", Art = "alcohol-preparation-station", Fallback = "blender",
        W = 3, H = 3, Value = 300, Types = new[] { "MACHINE" }, Shell = Build
    };

    private static bool IsFruit(GameItem item) => item?.identifier == "nutrifruit"
        && GeneralHelper.IsItemOwned(item) && !item.IsTag("ANIMAL_PRION_TAG");

    private static bool IsWaterContainer(GameItem item) => item != null
        && GeneralHelper.IsItemOwned(item) && item.IsTag("LIQUID_CONTAINER_TAG")
        && item.IsTag("LIQUID_CONTAINER_BASE_PRICE_TAG") && !item.IsTag("WINE_CONTAINER_TAG")
        && WaterFeatureHelper.GetWaterFeature(item) != null;

    private static GameGridInventory Slot(Func<GameItem, bool> accepts) =>
        new(9, 6) { mayInventoryAddItemFunc = (item, _) => accepts(item) };

    private static long Calories(GameItem item) => IsFruit(item)
        ? (long)Math.Max(0, item.GetTagReadonly("CALORIE_VALUE_TAG").GetInt()) * Math.Max(0, item.unitCount)
        : 0;

    private static GameItem Build()
    {
        var battery = new GameSlotInventory();
        var modules = new GameGridInventory(4, 4);
        var fruit = Slot(IsFruit);
        var water = Slot(IsWaterContainer);
        var receiver = Slot(IsWaterContainer);
        var status = new MachineryText(uppercase: false);
        var window = new PixelWindow(true, null, WindowStyle.Machinery);
        window.Attach(new MachineryPanelRow(
            MachineHelper.Panel(LocHelper.GetLocalizedMechanic("mech_chem_finisher_label_module"), modules, "modules"),
            MachineHelper.Panel(L.T("营养果", "Nutrifruit"), fruit, "fruit"),
            MachineHelper.Panel(L.K("production.alcohol.waterIn", "进水容器", "Water container"), water, "water"),
            MachineHelper.Panel(L.K("production.alcohol.receiver", "接液容器", "Receiving container"), receiver, "receiver"),
            MachineHelper.Panel(L.T("过夜制备", "Overnight preparation"), status, "prepare"),
            MachineHelper.PowerPanel(battery))
            .SetColumns(3).SetDisplayOrder(5, 0, 4, 1, 2, 3));
        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineryHelper.InitMachinery(machine, BasePower);
        MachineBlender.SetupChemModuleGrid(modules, machine);

        long Reserve() => Math.Max(0, machine.GetTagReadonly(FruitReserve).GetLong());
        int Impurity() => (WaterUl + AlcoholUl) * MachineChemFinisher.GetLoss(machine) / 100;
        void Refresh()
        {
            int added = Impurity();
            var (_, output, transfer) = SelectContainers(water, receiver, added);
            string carried = transfer == null ? "—" : $"{(Volume(transfer) - WaterUl) / 1000.0:0.##} mL";
            long available = Reserve() + fruit.childItems.Sum(Calories);
            int power = MachineryHelper.GetMachinePowerUsage(machine);
            string readiness = !MachineHelper.CanPower(machine, battery) ? L.T("需装入有足够电量的电池", "Needs a charged battery")
                : available < FruitCost ? L.T("缺营养果", "Needs nutrifruit")
                : !water.childItems.Exists(IsWaterContainer) ? L.K("production.alcohol.insertWater", "放入装水的普通水容器", "Insert a regular water container with water")
                : transfer == null ? L.K("production.alcohol.waterShort", "单个进水容器须含 100 mL 水", "One input must contain 100 mL of water")
                : output != null ? L.T("今晚可制备", "Ready for tonight")
                : !receiver.childItems.Exists(IsWaterContainer) ? L.K("production.alcohol.insertReceiver", "放入普通水容器接液", "Insert a regular water container to collect the batch")
                : L.F("production.alcohol.freeSpace", "单个接液容器需空出 {0:0.###} mL", "One receiver needs {0:0.###} mL free", (Volume(transfer) + AlcoholUl + added) / 1000.0);
            status.SetText(L.T(
                $"{readiness}\n热量 {available:N0} / 240 kcal\n每批耗电 {power}，取水 100 mL，产酒精 30 mL\n带入杂质 {carried}，新增 {added / 1000.0:0.##} mL",
                $"{readiness}\nHeat {available:N0} / 240 kcal\nPer batch: {power} charge, 100 mL water, 30 mL alcohol\nCarried in {carried}, added {added / 1000.0:0.##} mL"));
        }

        bool busy = false;
        machine.onCycleEndSlotItemFunc = (_, _, _) => HookGuard.Run("酒精制备站过夜", () =>
        {
            if (busy || !GeneralHelper.IsItemOwned(machine) || !SaveSlot.CanWrite("CorpPresence")) return;
            busy = true;
            try
            {
                int impurity = Impurity();
                var (input, output, transfer) = SelectContainers(water, receiver, impurity);
                if (input != null && output != null)
                    Prepare(machine, battery, fruit.childItems, input, output, transfer, Reserve(), impurity);
#if LOCAL_TEST_KIT
                else if (transfer != null && receiver.childItems.Exists(IsWaterContainer))
                    ModLog.Info("酒精制备站接液空间不足，未扣料。");
#endif
            }
            finally
            {
                busy = false;
                Refresh();
            }
        });
        foreach (var inventory in new GameInventory[] { modules, fruit, water, receiver, battery })
        {
            inventory.onSlotAddItemEndFunc += (_, _, _) => Refresh();
            inventory.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
            inventory.onSlotChangeAmountItemFunc += (_, _, _) => Refresh();
        }
        window.openCallback += Refresh;
        return machine;
    }

    private static (GameItem Input, GameItem Output, int[] Transfer) SelectContainers(
        GameGridInventory water, GameGridInventory receiver, int impurity)
    {
        int waterIndex = Index("water");
        if (waterIndex < 0 || Index("alcohol") < 0 || Index("physical_contaminant") < 0) return default;

        var outputs = receiver.childItems.FindAll(IsWaterContainer);
        int[] smallestTransfer = null;
        long leastSpace = long.MaxValue;
        foreach (var input in water.childItems)
        {
            if (!IsWaterContainer(input)) continue;
            var transfer = PlanWater(WaterHelper.GetLiquidParts(input), waterIndex);
            if (transfer == null) continue;
            long requiredSpace = Volume(transfer) + AlcoholUl + impurity;
            if (requiredSpace < leastSpace)
            {
                leastSpace = requiredSpace;
                smallestTransfer = transfer;
            }
            foreach (var output in outputs)
                if (!ReferenceEquals(input, output) && WaterHelper.GetFreeCapacity(output) >= requiredSpace)
                    return (input, output, transfer);
        }
        return (null, null, smallestTransfer);
    }

    private static void Prepare(GameItem machine, GameSlotInventory battery, List<GameItem> fruits, GameItem input, GameItem output,
        int[] transfer, long reserve, int impurity)
    {
        var consumed = new List<(GameItem Fruit, int Count)>();
        long available = reserve;
        foreach (var fruit in fruits)
        {
            if (available >= FruitCost) break;
            long calories = Calories(fruit);
            if (calories <= 0) continue;
            long perFruit = calories / fruit.unitCount;
            int count = (int)Math.Min(fruit.unitCount, (FruitCost - available + perFruit - 1) / perFruit);
            consumed.Add((fruit, count));
            available += count * perFruit;
        }
        if (available < FruitCost) return;

        int alcoholIndex = Index("alcohol"), dirtIndex = Index("physical_contaminant");
        var source = WaterHelper.GetLiquidParts(input);
        var destination = WaterHelper.GetLiquidParts(output);
        var nextSource = new int[source.Length];
        var nextDestination = new int[destination.Length];
        for (int i = 0; i < source.Length; i++)
        {
            nextSource[i] = source[i] - transfer[i];
            nextDestination[i] = checked(destination[i] + transfer[i]);
        }
        nextDestination[alcoholIndex] = checked(nextDestination[alcoholIndex] + AlcoholUl);
        nextDestination[dirtIndex] = checked(nextDestination[dirtIndex] + impurity);

        if (!MachineHelper.TryDrawCyclePower(machine, battery)) return;

        long remaining = available - FruitCost;
        try
        {
            WriteParts(input, source, nextSource);
            WriteParts(output, destination, nextDestination);
            machine.ModifyTag(FruitReserve, tag => tag.Enable().SetLong(remaining), skipValidate: true);
            foreach (var (fruit, eaten) in consumed)
            {
                if (eaten == fruit.unitCount) fruit.Destroy();
                else fruit.SetUnitCount(fruit.unitCount - eaten);
            }
        }
        catch
        {
            WriteParts(input, nextSource, source);
            WriteParts(output, nextDestination, destination);
            machine.ModifyTag(FruitReserve, tag => tag.Enable().SetLong(reserve), skipValidate: true);
            throw;
        }

#if LOCAL_TEST_KIT
        ModLog.Info($"酒精制备站制备成功：扣热量 {FruitCost} kcal、取水 {WaterUl} µL、产酒精 {AlcoholUl} µL、带入其他成分 {Volume(transfer) - WaterUl} µL、新增杂质 {impurity} µL，余热 {remaining} kcal。");
#endif
        HookGuard.Run("刷新制备站进水", () => RefreshLiquid(input));
        HookGuard.Run("刷新制备站接液", () => RefreshLiquid(output));
    }

    private static int[] PlanWater(int[] parts, int waterIndex)
    {
        if (waterIndex < 0 || waterIndex >= parts.Length || parts[waterIndex] < WaterUl) return null;
        int water = parts[waterIndex];
        var transfer = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            transfer[i] = (int)(((long)parts[i] * WaterUl + water / 2) / water);
        return transfer;
    }

    private static int Index(string id) => Liquid.Liquids.FindIndex(liquid => liquid.identifier == id);

    private static long Volume(int[] parts)
    {
        long total = 0;
        foreach (int part in parts) total += part;
        return total;
    }

    private static void WriteParts(GameItem item, int[] before, int[] after)
    {
        for (int i = 0; i < after.Length; i++)
        {
            if (after[i] == before[i]) continue;
            int amount = after[i];
            item.ModifyTag(Liquid.Liquids[i].currentPart, tag => tag.Enable().SetInt(amount), skipValidate: true);
        }
    }

    private static void RefreshLiquid(GameItem item) =>
        WaterFeatureHelper.OnLiquidChanged(item, WaterFeatureHelper.GetWaterFeature(item).IsExposed());
}
