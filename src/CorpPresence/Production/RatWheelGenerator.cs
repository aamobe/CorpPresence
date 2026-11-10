using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace CorpPresence;

internal static class RatWheelGenerator
{
    private const string WidthTag = "CORP_RAT_WHEEL_WIDTH";
    private const string HeightTag = "CORP_RAT_WHEEL_HEIGHT";
    internal const string GoodsKey = "rat_wheel_generator";
    private const int EnergyPerRat = 2;
    private const int MaxNightlyEnergyPerBattery = 16;

    // 以下到 Definition 之前：独立笼体与容量标签（从 1.0.0 订阅版还原，2026-11-10）。
    // 不借用原版 CreateAnimalCage 和 CAGE_SIZE_TAG，改用自己的宽高标签；读档时把旧的尺寸标签换算过来并移除。
    internal static void Install(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(GameItem), "ValidateShapeState"), new HarmonyMethod(typeof(RatWheelGenerator), "BeforeValidateCapacity")
        {
            priority = 800
        });
        harmony.Patch(AccessTools.Method(typeof(AnimalCage), "CanUpgradeSize"), new HarmonyMethod(typeof(RatWheelGenerator), "BeforeCanUpgrade"));
        harmony.Patch(AccessTools.Method(typeof(AnimalCage), "UpgradeSize"), new HarmonyMethod(typeof(RatWheelGenerator), "BeforeUpgrade"));
        harmony.Patch(AccessTools.Method(typeof(AnimalCage), "CreateTooltip"), new HarmonyMethod(typeof(RatWheelGenerator), "BeforeCageTooltip"));
    }

    private static bool Ours(GameItem item)
    {
        if (item?.state != null)
        {
            if (!item.state.IsTag("CORP_RAT_WHEEL_HEIGHT"))
            {
                return Goods.IdOf(item) == "rat_wheel_generator";
            }
            return true;
        }
        return false;
    }

    private static GameItem FeederIn(GameSlotInventory slot)
    {
        if (slot?.childItem == null || !slot.childItem.IsTag("FEED_DISPENSER_TAG"))
        {
            return null;
        }
        return slot.childItem;
    }

    private static GameItem CreateIndependentCage()
    {
        (PixelWindow, GameInventory, GameSlotInventory, MachineryButton, GameSlotInventory, MachineryButton) tuple = AnimalCage.CreateCageWindow();
        PixelWindow item2 = tuple.Item1;
        GameInventory storage = tuple.Item2;
        GameSlotInventory feeder = tuple.Item3;
        MachineryButton blend = tuple.Item4;
        GameSlotInventory item3 = tuple.Item5;
        MachineryButton item4 = tuple.Item6;
        GameItem machine = ItemDirectory.CreateEmptyItem().SetContentWindow(item2).SetSpriteAndShape("Items/items_husbandry", "animal_cage");
        machine.EnableTag("CAGE_TAG");
        machine.EnableTag("CONTAINER_TAG");
        machine.EnableTag("IMPORTANT_TAG");
        machine.state.GetTag("CORP_RAT_WHEEL_WIDTH").Enable().SetInt(10);
        machine.state.GetTag("CORP_RAT_WHEEL_HEIGHT").Enable().SetInt(3);
        machine.state.GetTag("CAGE_HEALTH_TAG").Enable().SetInt(100);
        machine.state.GetTag("CAGE_MAX_HEALTH_TAG").Enable().SetInt(100);
        AudioHelper.InitMetalMachine(machine);
        storage.mayInventoryAddItemFunc = (GameItem item, GameInventory _) => GeneralHelper.IsItemOwned(machine, item) && !item.IsTag("CONTAINER_TAG") && (!item.IsGameItemType("MACHINE") || item.IsTag("ALLOWED_IN_CAGE_TAG"));
        feeder.mayInventoryAddItemFunc = (GameItem item, GameInventory _) => GeneralHelper.IsItemOwned(machine, item) && item.IsTag("FEED_DISPENSER_TAG");
        feeder.SetBackgroundFadeSprite("Items/items_husbandry", "animal_feeder");
        blend.SetCallback(delegate
        {
            GameItem gameItem2 = FeederIn(feeder);
            if (gameItem2 != null)
            {
                MachineFeedDispenser.Blend(gameItem2, MachineFeedDispenser.GetStorageGrid(gameItem2));
            }
        });
        GameSlotInventory gameSlotInventory = feeder;
        gameSlotInventory.onSlotAddItemEndFunc = (Action<GameItem, GameInventory, SlotMarker>)Delegate.Combine(gameSlotInventory.onSlotAddItemEndFunc, (Action<GameItem, GameInventory, SlotMarker>)delegate
        {
            RefreshFeeder();
        });
        GameSlotInventory gameSlotInventory2 = feeder;
        gameSlotInventory2.onSlotRemoveItemEndFunc = (Action<GameItem, GameInventory, SlotMarker>)Delegate.Combine(gameSlotInventory2.onSlotRemoveItemEndFunc, (Action<GameItem, GameInventory, SlotMarker>)delegate
        {
            RefreshFeeder();
        });
        GameItem gameItem = machine;
        gameItem.onLoaded = (Action<GameItem>)Delegate.Combine(gameItem.onLoaded, (Action<GameItem>)delegate(GameItem item)
        {
            RestoreCapacity(item, (GameGridInventory)storage);
            RefreshFeeder();
        });
        RefreshFeeder();
        MachineAutoButcher.SetupHostSlot(machine, item3, item4);
        machine.onCycleEndSlotItemFunc = delegate(GameItem item, GameInventory _, SlotMarker _)
        {
            int num = 0;
            bool flag = false;
            foreach (GameItem childItem in storage.childItems)
            {
                if (childItem.IsTag("ANIMAL_ITEM_TAG") && !childItem.IsTag("ANIMAL_IS_DEAD_TAG"))
                {
                    num += childItem.GetItemSize();
                    flag |= childItem.GetTagReadonly("ANIMAL_OVERCROWDING_TAG").GetInt() > 0;
                }
            }
            int wear = num * ((!flag) ? 1 : 2) / 4;
            if (wear > 0)
            {
                item.ModifyTag("CAGE_HEALTH_TAG", delegate(TagState s)
                {
                    s.SetInt(Math.Max(0, item.GetTagReadonly("CAGE_HEALTH_TAG").GetInt() - wear));
                });
            }
        };
        return machine;
        void RefreshFeeder()
        {
            blend.SetHidden(FeederIn(feeder) == null);
        }
    }

    public static void BeforeValidateCapacity(GameItem __instance)
    {
        if (Ours(__instance))
        {
            int @int = __instance.state.GetTag("CAGE_SIZE_TAG").GetInt();
            (int, int) tuple = RatWheelCapacity.Restore(__instance.state.GetTag("CORP_RAT_WHEEL_WIDTH").GetInt(), __instance.state.GetTag("CORP_RAT_WHEEL_HEIGHT").GetInt(), @int, 0, 0, 0, 0);
            __instance.state.GetTag("CORP_RAT_WHEEL_WIDTH").Enable().SetInt(tuple.Item1);
            __instance.state.GetTag("CORP_RAT_WHEEL_HEIGHT").Enable().SetInt(tuple.Item2);
            __instance.state.dict.Remove("CAGE_SIZE_TAG");
            __instance.modifiedState?.dict.Remove("CAGE_SIZE_TAG");
        }
    }

    private static void RestoreCapacity(GameItem item, GameGridInventory grid)
    {
        BeforeValidateCapacity(item);
        item.EnableTag("IMPORTANT_TAG", skipValidate: true);
        int num = 0;
        int num2 = 0;
        foreach (GameItem childItem in grid.childItems)
        {
            GridShape modifiedShape = childItem.modifiedShape;
            if (modifiedShape != null)
            {
                num = Math.Max(num, modifiedShape.maxX + 1);
                num2 = Math.Max(num2, modifiedShape.maxY + 1);
            }
        }
        (int, int) tuple = RatWheelCapacity.Restore(item.state.GetTag("CORP_RAT_WHEEL_WIDTH").GetInt(), item.state.GetTag("CORP_RAT_WHEEL_HEIGHT").GetInt(), 0, grid.inventoryShape.width, grid.inventoryShape.height, num, num2);
        item.state.GetTag("CORP_RAT_WHEEL_WIDTH").Enable().SetInt(tuple.Item1);
        item.state.GetTag("CORP_RAT_WHEEL_HEIGHT").Enable().SetInt(tuple.Item2);
        item.ValidateShapeState();
        grid.SetShape(tuple.Item1, tuple.Item2);
        grid.Validate();
        item.contentWindow.Validate();
        item.Validate();
    }

    public static bool BeforeCanUpgrade(GameItem cage, ref bool __result)
    {
        if (!Ours(cage))
        {
            return true;
        }
        __result = RatWheelCapacity.CanUpgrade(cage.state.GetTag("CORP_RAT_WHEEL_HEIGHT").GetInt());
        return false;
    }

    public static bool BeforeUpgrade(GameItem cage, GameItem ingot)
    {
        if (!Ours(cage))
        {
            return true;
        }
        BeforeValidateCapacity(cage);
        int @int = cage.state.GetTag("CORP_RAT_WHEEL_HEIGHT").GetInt();
        if (!RatWheelCapacity.CanUpgrade(@int))
        {
            return false;
        }
        cage.state.GetTag("CORP_RAT_WHEEL_HEIGHT").SetInt(@int + 1);
        int maxHealth = cage.GetTagReadonly("CAGE_MAX_HEALTH_TAG").GetInt() + 20;
        cage.ModifyTag("CAGE_MAX_HEALTH_TAG", delegate(TagState s)
        {
            s.SetInt(maxHealth);
        }, skipValidate: true);
        cage.ModifyTag("CAGE_HEALTH_TAG", delegate(TagState s)
        {
            s.SetInt(Math.Min(maxHealth, cage.GetTagReadonly("CAGE_HEALTH_TAG").GetInt() + 20));
        }, skipValidate: true);
        RestoreCapacity(cage, AnimalCage.GetCageInventory(cage));
        cage.unitValue += 40L;
        StartingPerkContent.ConsumeUpgradeMaterial(ingot);
        EmporiumAudioPlayer.Instance.ratchet.Play();
        return false;
    }

    public static bool BeforeCageTooltip(RichTextBuilder builder, GameItem item)
    {
        if (!Ours(item))
        {
            return true;
        }
        int @int = item.GetTagReadonly("CAGE_HEALTH_TAG").GetInt();
        int int2 = item.GetTagReadonly("CAGE_MAX_HEALTH_TAG").GetInt();
        builder.AddLine(" ");
        builder.Add(LocHelper.GetLocalizedMechanic("husbandry_cage_durability"), RenderHandler.ColorPalette.LightCyan);
        builder.AddProgressBar((int2 > 0) ? ((float)@int / (float)int2) : 0f, 10, RenderHandler.ColorPalette.LightCyan, RenderHandler.ColorPalette.VeryDarkCyan, null, " ");
        builder.AddLine($"({@int}/{int2})", ignoreEmpty: true, RenderHandler.ColorPalette.LightCyan);
        MachineFeedDispenser.CreateSlottedTooltip(builder, FeederIn(AnimalCage.GetDispenserSlot(item)));
        int int3 = item.state.GetTag("CORP_RAT_WHEEL_HEIGHT").GetInt();
        builder.AddLine(LocHelper.GetLocalizedMechanic(RatWheelCapacity.CanUpgrade(int3) ? "husbandry_cage_expand_hint" : "husbandry_cage_max_size", Math.Max(0, 8 - int3)), ignoreEmpty: true, RenderHandler.ColorPalette.LightBlue);
        if (AnimalCage.CanRepair(item))
        {
            builder.AddLine(LocHelper.GetLocalizedMechanic("husbandry_cage_repair_hint"), ignoreEmpty: true, RenderHandler.ColorPalette.LightBlue);
        }
        else
        {
            builder.AddLine(LocHelper.GetLocalizedMechanic("husbandry_cage_fully_repaired"), ignoreEmpty: true, RenderHandler.ColorPalette.Cyan);
        }
        return false;
    }

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
        var machine = CreateIndependentCage();
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
