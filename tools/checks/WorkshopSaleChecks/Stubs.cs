namespace CorpPresence;

internal sealed class StoreSession
{
    internal static int Revision;
    private int _revision = Revision;
    internal static StoreSession Capture() => new();
    internal bool IsCurrent => _revision == Revision;
    internal static bool CompanyProgressPaused;
}
internal sealed class CorpState
{
    internal static CorpState Current = new();
    internal string RunId = "test";
    internal WorkshopState Workshop = new();
}
internal static class CartelShizuka
{
    internal static bool Visited = true, HasBlueprint = true;
    internal static bool Introduced(CorpState root) => Visited;
    internal static bool BlueprintDelivered(CorpState root) => HasBlueprint;
}
internal static class SaveSlot
{
    internal static PlayerStore CurrentStore = new();
    internal static bool Writable = true;
    internal static bool CanWrite(string key) => Writable;
}
internal sealed class PlayerStore
{
    internal int Cash;
    internal Action<int> Pay;
    internal int GetCash() => Cash;
    internal void ModCash(int value, bool playSound) { if (playSound) throw new Exception(); Pay(value); }
}
internal static class LowerMachinist
{
    internal static bool Starter = true, Advanced = true;
    internal static bool StarterUnlocked(CorpState root) => Starter;
    internal static bool MineralizerUnlocked(CorpState root) => Advanced;
}
internal static class StoreStation { internal static int Day = 1; internal static int GetDayCounter() => Day; }
internal sealed class EmporiumEntry
{
    internal static EmporiumEntry Instance = new();
    internal List<GameItem> Owned = new();
    internal List<GameItem> GetAllOwnedItems(bool includeAfterhour) => Owned;
}
internal sealed class GameItem
{
    internal sealed class ItemTag
    {
        private string _text;
        private int _integer;
        internal ItemTag Enable() => this;
        internal void SetString(string text) => _text = text;
        internal string GetString() => _text;
        internal void SetInt(int value) => _integer = value;
        internal int GetInt() => _integer;
    }

    private readonly Dictionary<string, ItemTag> _tags = new();
    internal ItemTag GetTagReadonly(string key) => _tags.GetValueOrDefault(key) ?? new();
    internal void DisableTag(string key, bool skipValidate = false) => _tags.Remove(key);
    internal void ModifyTag(string key, Action<ItemTag> update, bool skipValidate = false)
    {
        if (!_tags.TryGetValue(key, out var tag)) _tags[key] = tag = new();
        update(tag);
    }

    internal string identifier;
    private static int _nextId;
    private readonly int _id = ++_nextId;
    internal int GetUniqueID() => _id;
    internal bool Fresh;
    internal string OriginRun;
    internal int OriginSerial, OriginSlot;
    internal int unitCount;
    internal int[] Parts;
    private GameInventory _parent;
    internal GameItem() => parentInventory = new();
    internal GameInventory parentInventory
    {
        get => _parent;
        set { _parent?.childItems.Remove(this); _parent = value; _parent?.childItems.Add(this); }
    }
    internal bool Owned = true, Bound, Dead;
    internal int Limit = int.MaxValue;
    internal int MaxNumRemove() => Math.Min(unitCount, Limit);
    internal bool MayRemove() => parentInventory != null && !parentInventory.Locked && parentInventory.Owned;
    internal void Destroy() => Dead = true;
    internal void SetUnitCount(int value) => unitCount = value;
}
internal sealed class GameInventory
{
    internal List<GameItem> childItems = new();
    internal bool Locked, Detached;
    internal bool Owned = true;
    internal Action<GameItem> Remove = item => item.parentInventory = null;
    internal void Expel(GameItem item) => Remove(item);
}
internal static class StorePlacement { internal static bool Destroyed(GameItem item) => item.Dead; }
internal static class ShopInventory { internal static bool Contains(GameItem item) => item.parentInventory is { Detached: false }; }
internal static class GeneralHelper { internal static bool IsItemOwned(GameItem item) => item.Owned; }
internal static class PowderBox
{
    internal const int ComponentCount = 5;
    internal static bool IsOwnedBox(GameItem item) => item.Parts != null && item.Owned && !item.Bound && item.unitCount == 1;
    internal static void Read(GameItem item, int[] parts) => Array.Copy(item.Parts, parts, 5);
    internal static void WriteComposition(GameItem item, int[] parts) => item.Parts = parts;
}
internal static class ModLog { internal static void Warn(string message) { } }
