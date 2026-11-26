namespace CorpPresence
{
    internal sealed class CorpState
    {
        internal static CorpState Current { get; } = new();
        internal static CorpState Get() => Current;
        internal Dictionary<string, CharacterState> Characters { get; } = new();
        internal CharacterState Of(string id) => Characters.TryGetValue(id, out var s) ? s : Characters[id] = new();
    }
    internal sealed class CharacterState
    {
        internal int Chapter, NextDailyDay, PendingDay = -1, LastStoryDay = -999, QueuedDay = -1;
        internal int LastVisitDay { get; set; } = -999;
        internal string PendingKind;
        internal List<int> SideDone = new();
    }
    internal sealed class GameItem
    {
        internal GameItem(string id, int[] parts = null) { Id = id; Parts = parts; }
        internal string GetDisplayName(bool preferCustomName) => Id;
        internal string Id { get; }
        internal int[] Parts { get; }
        internal List<string> Contents { get; } = new();
        internal List<string> itemTypes { get; } = new();
        internal bool IsTag(string key) => false;
        internal bool Destroyed { get; private set; }
        internal void Destroy() => Destroyed = true;
    }
    internal sealed class PlayerStore
    {
        internal int Cash { get; set; }
        internal int GetCash() => Cash;
        internal bool CanPay(int price) => Cash >= price;
        internal List<GameItem> Counter { get; } = new();
        internal StoreClient currentClientInstance => null;
        internal bool TryAddDirectSellingItemToTable(GameItem item, bool owned) { Counter.Add(item); return true; }
        internal List<GameItem> GetAllRepoEligibleItems() => Counter;
    }
    internal static class SaveSlot
    {
        internal static PlayerStore CurrentStore { get; } = new();
        internal static bool CanWrite(string id) => true;
    }
    internal sealed class EmporiumEntry
    {
        internal static EmporiumEntry Instance { get; } = new();
        internal List<GameItem> Items { get; } = new();
        internal List<GameItem> GetAllOwnedItems() => Items;
    }
    internal static class Goods
    {
        internal static string IdOf(GameItem i) => i.Id;
        internal static GameItem MakeSpecial(string id) => null;
    }
    internal static class Crates
    {
        internal static GameItem MakeFilled(string id, string[] goods)
        {
            var crate = new GameItem(id);
            crate.Contents.AddRange(goods);
            return crate;
        }
        internal static (List<string> childItems, bool attached)? InnerOf(GameItem crate) => (crate.Contents, true);
    }
    internal static class PowderBox
    {
        internal const string Key = "powder_box";
        internal const int ComponentCount = 5;
        internal static bool IsOwnedBox(GameItem item) => item.Id == Key;
        internal static long Read(GameItem item, Span<int> parts) { item.Parts.CopyTo(parts); return item.Parts.Sum(); }
    }
    internal static class PowderSieve
    {
        internal const string Key = "powder_sieve";
        internal static long QualifyingStock(GameItem machine, long[] bins, Action<int, int, long, long> report)
        {
            if (bins != null) for (int i = 0; i < machine.Parts.Length; i++) bins[i] += machine.Parts[i];
            return machine.Parts.Sum();
        }
    }
    internal static partial class Engine
    {
        internal static readonly Character[] All = { new LowerMachinist(), new CartelShizuka() };
        internal static int LowerRep = 9;
        internal static int Reputation(string faction) => faction == "FACTION_LOWER_LEVEL" ? LowerRep : 0;
        private static bool _scheduling;
        internal static bool BuyActive;
        private static StoreClient Create(Character c, string id) => new();
    }
    internal sealed class StoreClient { internal string identifier => ""; internal StoreClient GetClientBlueprint() => this; }
    internal sealed class StoreClientManager { internal List<StoreClient> clientStack = new(); }
    internal static class StoreStation { internal static int Day; internal static int GetDayCounter() => Day; }
    internal sealed class StoreSession
    {
        internal static StoreSession Capture() => new();
        internal bool IsCurrent => true;
        internal static bool CompanyProgressPaused => false;
    }
    internal static class ServiceRetry
    {
        internal static bool Ready(CharacterState s, float t) => true;
        internal static void Succeeded(CharacterState s) { }
        internal static void Failed(CharacterState s, float t) { }
    }
    internal static class CompanyVisitors
    {
        internal static bool Full;
        internal static void DiscardUnshipped(GameItem item) { if (item != null && !SaveSlot.CurrentStore.Counter.Contains(item)) item.Destroy(); }

        internal static void Schedule(StoreClientManager m) { }
    }
    internal static class WorkshopCounter
    {
        internal static bool HasRoom => !CompanyVisitors.Full;
        internal static bool CanFit(GameItem crate) => HasRoom;
        internal static bool TryPlaceOnCounter(GameItem crate) => CanFit(crate) && SaveSlot.CurrentStore.TryAddDirectSellingItemToTable(crate, true);
    }
    internal static class CompanyMoney
    {
        internal static bool ThrowAfterPayment;
        internal static void Pay(PlayerStore store, int price)
        {
            store.Cash -= price;
            if (ThrowAfterPayment) throw new Exception("test callback");
        }
    }
    internal static class StorePlacement { internal static bool Destroyed(GameItem item) => item.Destroyed; }
    internal static class Settings
    {
        internal static int DailyCap => 0;
        internal static int DailyGap(Character c) => 3;
        internal static int StoryGap(Character c) => 3;
    }
    internal static class PowderColor { internal const int Capacity = 500; }
    internal static class MineralCrusher { internal const string Key = "mineral_crusher"; }
    internal static class SinteringStation { internal const string Key = "sintering_station"; }
    internal static class CraftingStation { internal const string Key = "crafting_station", Blueprint = "electronic_blueprint"; }
    internal static class MineralGrowthStation { internal const string Key = "mineral_growth_station"; }
    internal static class MineralGrowthBatch { internal const string OreBlueprint = "mineral_ore_blueprint", LightSaltBlueprint = "light_salt_blueprint"; }
    internal static class SleepMoss { internal const string SeedId = "corp_presence_sleep_moss_seed"; }
    internal static class L { internal static string K(string key, string zh, string en) => zh; internal static string T(string zh, string en) => zh; }
    internal static class DialogueText { internal static string WithAction(string act, string line, bool show) => line; }
    internal static class ItemRegistry { internal static GameItem Create(string id) => new(id); }
    internal static class GameResources { internal static bool Ready => true; }
    internal static class ModLog { internal static void Info(string s) { } internal static void Warn(string s) => Console.WriteLine(s); }
    internal static class Main { internal static string DisplayName => "CorpPresence"; }
    internal static class ModHelper { internal static void SetClientSource(StoreClient c, string s) { } }
    internal sealed class StoreUIManager { internal static StoreUIManager Instance => null; internal void Notify(string s) { } }
    internal static class Ext { internal static void Fire(string key, Action action) => action(); }
}
namespace CorpPresence.Malice
{
    internal static class Malice { internal static bool IsInstalled => false; }
    internal static class Envoy { internal static void Schedule(StoreClientManager m) { } }
    internal static class RebelStory { internal static void Schedule(StoreClientManager m) { } }
}
namespace UnityEngine { internal static class Time { internal static float unscaledTime => 0; } }
