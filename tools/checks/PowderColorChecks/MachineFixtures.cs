using System;
using System.Collections.Generic;
using System.Linq;

// Minimal substitutes for the native grid's two placement paths. A merge increments the
// target and consumes the incoming unit without parenting it (SlotMarker.StackItemUnchecked).
internal sealed class GameItem
{
    internal string identifier = "wire";
    internal int unitCount = 1;
    internal void Destroy() { parentInventory?.childItems.Remove(this); parentInventory = null; unitCount = 0; }
    internal (int minX, int minY) modifiedShape;
    internal GameInventory parentInventory;
    internal Func<GameItem, GameItem, bool> mayStackItemFunc = (a, b) => a.identifier == b.identifier;
    private readonly Dictionary<string, TagState> tags = new();
    internal bool IsTag(string name) => tags.ContainsKey(name);
    internal TagState GetTagReadonly(string name) => tags.TryGetValue(name, out var tag) ? tag : new();
    internal void ModifyTag(string name, Action<TagState> set, bool skipValidate = false)
    {
        if (!tags.TryGetValue(name, out var tag)) tags[name] = tag = new();
        set(tag);
    }
    internal void DisableTag(string name, bool skipValidate = false) => tags.Remove(name);
}

internal sealed class TagState
{
    private int value;
    private string text;
    internal int GetInt() => value;
    internal string GetString() => text;
    internal TagState Enable() => this;
    internal void SetInt(int next) => value = next;
    internal void SetString(string next) => text = next;
}

internal class GameInventory
{
    internal List<GameItem> childItems = new();
    internal Func<GameItem, GameInventory, bool> mayInventoryAddItemFunc = (_, _) => true;
    internal int Slots = 2;
}
internal sealed class GameGridInventory : GameInventory { }
internal static class SaveManager { internal static bool IsDecoding; }
internal sealed class SlotMarker { }
internal static class GeneralHelper { internal static bool IsItemOwned(GameItem item) => true; }
internal static class ModLog { internal static void Warn(string text) { } }

internal static class GraphUtils
{
    private static GameItem Stack(GameInventory grid, GameItem item) => grid.childItems.FirstOrDefault(other =>
        item.mayStackItemFunc?.Invoke(item, other) == true && other.mayStackItemFunc?.Invoke(item, other) == true);
    internal static bool CanAccept(GameInventory grid, GameItem item) =>
        grid.mayInventoryAddItemFunc(item, grid) && (Stack(grid, item) != null || grid.childItems.Count < grid.Slots);

    internal static void TryAcceptAll(GameInventory grid, GameItem item)
    {
        if (!CanAccept(grid, item)) return;
        var target = Stack(grid, item);
        if (target == null)
        {
            grid.childItems.Add(item);
            item.parentInventory = grid;
        }
        else
        {
            target.unitCount += item.unitCount;
            item.unitCount = 0;
        }
    }
}
