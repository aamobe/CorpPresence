using System;
using System.Collections.Generic;
using HarmonyLib;

namespace CorpPresence;

internal static class RatWheelGenerator
{
    internal const string GoodsKey = "rat_wheel_generator";
    private const int EnergyPerRat = 2;
    private const int MaxNightlyEnergyPerBattery = 16;

    internal static Goods.Def Definition() => new Goods.Def
    {
        Name = "跑轮发电机", NameEn = "Rat Wheel Generator",
        Value = 350, Company = "husbandry",
        Art = "rat_wheel_generator_retrofit", Fallback = "animal_cage", W = 4, H = 3,
        Shape = "####|####|####",
        Types = new[] { "MACHINE" }, Shell = Build,
        Desc = "在笼里养活鼠，在充电槽放普通可充电电池。每只亚成年及以上活鼠每晚为两个槽各充 2 kJ，每槽上限 16 kJ，不超过电池容量。笼格可放清扫机，并保留原版喂食器槽。",
        DescEn = "Keep live rats in the cage and put standard rechargeable batteries in the charging slots. Each live adolescent or older rat provides 2 kJ per slot per night, up to 16 kJ per slot and within battery capacity. The cage accepts a cleaning robot and retains its feeder slot.",
        Flavor = "笼子里装了跑轮，笼子外接了两个充电槽。电池插好以后，剩下的就看老鼠了。",
        FlavorEn = "Wheels inside the cage, two charging slots outside. Once the batteries are in, the rest is up to the rats."
    };

    private static bool IsRat(GameItem item) =>
        item.IsTag(HusbandryHelper.ANIMAL_ITEM_TAG) &&
        item.GetTagReadonly(HusbandryHelper.ANIMAL_SPECIES_TAG).GetString() == HusbandryHelper.ANIMAL_RAT;

    private static bool IsBattery(GameItem item) =>
        item != null && item.IsTag("power_source_item") && !PowerHelper.IsDisposablePowerSource(item) &&
        !CurrentConverterHelper.IsCurrentConverter(item) && !HypertechBatteryHelper.IsHypertechBattery(item);

    private static GameItem Build()
    {
        var machine = AnimalCage.CreateAnimalCage();
        var cage = AnimalCage.GetCageInventory(machine);
        var accepts = cage.mayInventoryAddItemFunc;
        cage.mayInventoryAddItemFunc = (item, inventory) =>
            !IsBattery(item) && (!item.IsTag(HusbandryHelper.ANIMAL_ITEM_TAG) || IsRat(item)) && accepts(item, inventory);

        var first = new GameSlotInventory();
        var second = new GameSlotInventory();
        first.mayInventoryAddItemFunc = second.mayInventoryAddItemFunc =
            (item, _) => IsBattery(item) && GeneralHelper.IsItemOwned(machine, item);
        // Append the slots after the original cage, feeder and butcher storage.
        var panels = (MachineryPanelRow)machine.contentWindow.childElement;
        panels.Add(MachineHelper.Panel(L.K("production.ratWheelGenerator.build.chargeSlotPrimary", "充电槽 1", "Charge slot 1"), first, "rat_charge_1"));
        panels.Add(MachineHelper.Panel(L.K("production.ratWheelGenerator.build.chargeSlotSecondary", "充电槽 2", "Charge slot 2"), second, "rat_charge_2"));
        panels.Add(new MachineryPanel(L.K("production.ratWheelGenerator.build.maxKJSlotNight", $"每槽每晚最多 {MaxNightlyEnergyPerBattery} kJ", (System.FormattableString)$"Max {MaxNightlyEnergyPerBattery} kJ/slot/night")));
        // The center column follows the cage width, so keep the two slots in the side columns.
        panels.SetDisplayOrder(1, 0, 2, 3, 5, 4);
        panels.SetColumns(3);
        machine.contentWindow.Validate();
        // Native panel plates are visual-only. Give this machine's plates their own
        // hit area so a resized window background cannot expose the inventory below.
        var plateField = AccessTools.Field(typeof(MachineryPanel), "plate");
        foreach (var panel in panels.Panels)
        {
            if (plateField?.GetValue(panel) is TreeNodeRender plate && plate.image != null)
            {
                plate.image.alphaHitTestMinimumThreshold = 0f;
                plate.image.raycastTarget = true;
            }
        }

        void RefreshAppearance()
        {
            bool occupied = false;
            foreach (var animal in cage.childItems)
                if (IsRat(animal) && !animal.IsTag(HusbandryHelper.ANIMAL_IS_DEAD_TAG))
                {
                    occupied = true;
                    break;
                }
            string sprite = "corp_med_" + GoodsKey + (occupied ? "_occupied" : "");
            if (machine.spritePath == sprite) return;
            machine.SetSprite("", sprite);
            Goods.Redraw(machine);
        }
        cage.onSlotAddItemEndFunc += (_, _, _) => RefreshAppearance();
        cage.onSlotRemoveItemEndFunc += (_, _, _) => RefreshAppearance();
        machine.onLoaded += _ =>
        {
            RestoreFootprint(machine, removing: false);
            RefreshAppearance();
        };
        // A legacy cage against another item or an inventory edge can expand safely
        // when picked up, without deleting/recreating the cage or its contents.
        machine.onSlotRemoveItemFunc += (_, _, _) => RestoreFootprint(machine, removing: true);

        var settleCage = machine.onCycleEndSlotItemFunc;
        machine.onCycleEndSlotItemFunc = (item, inventory, marker) =>
        {
            settleCage?.Invoke(item, inventory, marker);
            Charge(cage, first.childItem, second.childItem);
            RefreshAppearance();
        };
        return machine;
    }

    private static void RestoreFootprint(GameItem machine, bool removing)
    {
        var old = machine.shape;
        if (old.width == 4 && old.height == 3) return;
        var replacement = new GridShapeBuilder(old.minX, old.minY, 4, 3, old.flipped, old.orientation)
            .SetDataFill(4, 3, 1).Build();
        if (!removing && machine.parentInventory is GameGridInventory inventory)
        {
            var placed = machine.modifiedShape;
            var candidate = new GridShapeBuilder(placed.minX, placed.minY, 4, 3, placed.flipped, placed.orientation)
                .SetDataFill(4, 3, 1).Build();
            if (!inventory.CheckShapePlacement(machine, candidate, new List<GameItem>()))
            {
                ModLog.Warn("旧跑轮发电机需要 4×3 空间；请拿起并重新放置，以更新占格。");
                return;
            }
        }
        machine.SetShape(replacement);
    }

    private static void Charge(GameInventory cage, GameItem first, GameItem second)
    {
        int eligibleRats = 0;
        foreach (var item in cage.childItems)
        {
            if (IsRat(item) && !item.IsTag(HusbandryHelper.ANIMAL_IS_DEAD_TAG) &&
                item.GetTagReadonly(HusbandryHelper.ANIMAL_AGE_TAG).GetInt() >= AnimalSpecies.Rat.growthStages[2])
            {
                eligibleRats++;
            }
        }
        int energy = Math.Min(MaxNightlyEnergyPerBattery, eligibleRats * EnergyPerRat);
        if (energy == 0) return;
        int firstCharge = Math.Min(energy, FreeCapacity(first));
        int secondCharge = Math.Min(energy, FreeCapacity(second));
        if (firstCharge > 0) PowerHelper.ChargeBattery(first, firstCharge, ignoreBlackout: true);
        if (secondCharge > 0) PowerHelper.ChargeBattery(second, secondCharge, ignoreBlackout: true);
    }

    private static int FreeCapacity(GameItem battery) => IsBattery(battery)
        ? Math.Max(0, battery.GetTagReadonly("power_source_item_max_energy").GetInt() -
            battery.GetTagReadonly("power_source_item_energy").GetInt())
        : 0;
}
