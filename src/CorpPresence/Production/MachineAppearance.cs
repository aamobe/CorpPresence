using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CorpPresence;

internal static class MachineAppearance
{
    private static readonly ConditionalWeakTable<GameItem, GameInventory> Outputs = new();
    private static readonly ConditionalWeakTable<GameItem, Cycle> Cycles = new();
    private static readonly List<WeakReference<GameItem>> Machines = new();
    private static float _nextCheck;

    private sealed class Cycle
    {
        public GameInventory Output;
        public Func<string> ReadyProduct;
        public Func<int> BatchSize;
        public string CheckedProduct;
        public bool Dirty = true;
        public bool Settling;
        public bool Running;
    }

    internal static void WatchOutput(GameItem machine, GameInventory output)
    {
        Outputs.Add(machine, output);
        output.onSlotAddItemEndFunc += (_, _, _) => Refresh(machine);
        output.onSlotRemoveItemEndFunc += (_, _, _) => Refresh(machine);
        machine.onLoaded += _ => Refresh(machine);
    }

    internal static void WatchCycle(GameItem machine, GameInventory output, Func<string> readyProduct, Func<int> batchSize = null)
    {
        var cycle = new Cycle { Output = output, ReadyProduct = readyProduct, BatchSize = batchSize };
        Cycles.Add(machine, cycle);
        Machines.Add(new WeakReference<GameItem>(machine));

        var row = (MachineryPanelRow)machine.contentWindow.childElement;
        foreach (var inventory in row.children.OfType<GameInventory>())
        {
            inventory.onSlotAddItemEndFunc += (_, _, _) => Rearm(machine);
            inventory.onSlotRemoveItemEndFunc += (_, _, _) => Rearm(machine);
        }

        var settle = machine.onCycleEndSlotItemFunc;
        machine.onCycleEndSlotItemFunc = (item, inventory, slot) =>
        {
            cycle.Settling = true;
            try { settle(item, inventory, slot); }
            finally
            {
                cycle.Settling = false;
                cycle.Dirty = true;
                try { RefreshCycle(machine, cycle); }
                catch (Exception error) { ModLog.Warn("结算后机器贴图刷新失败：" + error.Message); }
            }
        };
    }

    internal static void Rearm(GameItem machine)
    {
        if (machine == null || !Cycles.TryGetValue(machine, out var cycle) || cycle.Settling) return;
        cycle.Dirty = true;
    }

    internal static bool CanPowerCycle(GameItem machine, GameSlotInventory battery, string carry, int speedBonus = 0)
    {
        int speed = Math.Max(0, machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt()) + speedBonus;
        if (speed + machine.GetTagReadonly(carry).GetInt() < 100) return false;
        GameItem source = battery.childItem;
        if (CurrentConverterHelper.IsCurrentConverter(source))
            return CurrentConverterHelper.IsOn(source) && !CurrentConverterHelper.IsBlackedOut() && CurrentConverterHelper.IsServiceActive();
        return PowerHelper.CanDrawPowerSource(source, MachineryHelper.GetMachinePowerUsage(machine));
    }

    internal static void Tick()
    {
        if (!GameResources.Ready || SaveSlot.CurrentStore == null || Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + 0.5f;
        for (int index = Machines.Count - 1; index >= 0; index--)
        {
            if (!Machines[index].TryGetTarget(out var machine) || StorePlacement.Destroyed(machine))
            {
                Machines.RemoveAt(index);
                continue;
            }
            if (machine.parentInventory == null || !Cycles.TryGetValue(machine, out var cycle) || cycle.Settling)
                continue;

            RefreshCycle(machine, cycle);
        }
    }

    private static void RefreshCycle(GameItem machine, Cycle cycle)
    {
        string product = cycle.ReadyProduct();
        if (!cycle.Dirty && product == cycle.CheckedProduct) return;
        SetRunning(machine, cycle, product != null && CanFit(cycle.Output, product, cycle.BatchSize?.Invoke() ?? 1));
        cycle.CheckedProduct = product;
        cycle.Dirty = false;
    }

    private static bool CanFit(GameInventory output, string product, int count)
    {
        if (!DirectoryMaster.Has<GameItem>(product)) return false;
        var sample = DirectoryMaster.Item(product, isOwned: true);
        if (sample == null) return false;
        try { return CanFitOutput(output, sample, count); }
        finally { sample.Destroy(); }
    }

    internal static bool CanFitOutput(GameInventory output, GameItem sample, int count)
    {
        var previous = output.mayInventoryAddItemFunc;
        try
        {
            output.mayInventoryAddItemFunc = (_, _) => true;
            if (!GraphUtils.CanAccept(output, sample)) return false;
            return count == 1 || output is GameGridInventory grid && CanFitBatch(grid, sample, count);
        }
        finally
        {
            output.mayInventoryAddItemFunc = previous;
        }
    }

    private static bool CanFitBatch(GameGridInventory grid, GameItem sample, int count)
    {
        // Reserve prospective placements without inserting real items into the machine.
        var reserved = new List<GridShape>(count);
        var ignored = new List<GameItem>();
        var builder = new GridShapeBuilder(sample.modifiedShape);
        for (int rotation = 0; rotation < 4; rotation++)
        for (int y = 0; y < grid.inventoryShape.height; y++)
        for (int x = 0; x < grid.inventoryShape.width; x++)
        {
            GridShape candidate = builder.SetTransform(x, y, false, -rotation).Build();
            if (reserved.Any(shape => shape.Intersects(candidate)) || !grid.CheckShapePlacement(sample, candidate, ignored))
                continue;
            reserved.Add(candidate);
            if (reserved.Count == count) return true;
        }
        return false;
    }

    private static void SetRunning(GameItem machine, Cycle cycle, bool running)
    {
        if (cycle.Running == running) return;
        cycle.Running = running;
        Refresh(machine);
    }

    internal static Sprite SpriteOverride(GameItem item)
    {
        if (item == null || item.spritePath?.StartsWith("corp_med_", System.StringComparison.Ordinal) != true)
            return null;
        bool running = Outputs.TryGetValue(item, out var output)
            ? output.childItems.Any(product => product != null && product.unitCount > 0)
            : Cycles.TryGetValue(item, out var cycle) && cycle.Running;
        string sprite = running ? item.spritePath + "_running" : item.spritePath;
        if (item.spritePath == "corp_med_forge_furnace" && item.shape?.width == 2)
            return Goods.FindLegacyFurnaceSprite(sprite);
        return running ? Goods.FindSprite(sprite) : null;
    }

    private static void Refresh(GameItem machine)
    {
        // Invalidate the rendered sprite while keeping the saved sprite key and shape.
        machine.SetSprite(machine.spriteAtlasPath, machine.spritePath);
        Goods.Redraw(machine);
    }
}
