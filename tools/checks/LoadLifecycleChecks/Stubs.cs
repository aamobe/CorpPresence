using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;

namespace CorpPresence
{
    internal sealed class GameInventory { }
    internal sealed class PlayerStore
    {
        internal string runID = "test";
        internal Dictionary<string, string> modData = new();
        internal bool HasSave = true, ThrowAfterLate, HasCompanyItem;
        internal int Loads, Saves, DecodeFailures; internal bool FailInventory; internal string cassettePlayerInvJSON;
        internal GameItem Decoded;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void LoadGame()
        {
            if (!HasSave) return;
            Loads++;
            var e = EmporiumEntry.Instance;
            DecodeSavedInventory("main inventory", "main", e.invElement);
            DecodeSavedInventory("back room", "back", e.backInvinvElement);
            foreach (var inventory in e.OtherInventories)
                DecodeSavedInventory("other", "other", inventory);
            ModHook.FireLate();
            ModHook.FireLate();
            if (ThrowAfterLate) throw new InvalidOperationException("Injected after the native completion signal.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void DecodeSavedInventory(string what, string json, GameInventory inventory)
        {
            try { DecodeSaveItem(json, inventory); }
            catch (InvalidOperationException) { DecodeFailures++; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void DecodeSaveItem(string json, GameInventory inventory)
        {
            if (FailInventory && (json == "main" || json == "back"))
                throw new InvalidOperationException(json == "main" ? "打印机结构不兼容" : "FutureTech stopped an unsafe store decode");
            if (json == "main") DecodeInventory();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void DecodeInventory()
        {
            Decoded = HasCompanyItem ? DecodeCompanyItem() : DirectoryMaster.Item("vanilla_test", isOwned: true);
        }

        internal static GameItem DecodeCompanyItem()
        {
            var node = new SaveItemNode { identifier = "corp_presence_test" };
            ItemRegistry.BeforeDecode(new() { node });
            return DirectoryMaster.Item(node.identifier, isOwned: true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void SaveGame() => Saves++;
    }

    internal static class SaveSlot { internal static PlayerStore CurrentStore; }
    internal sealed class CorpState
    {
        private readonly PlayerStore _store = SaveSlot.CurrentStore;
        internal static CorpState Current;
        internal string RunId => _store?.runID;
        internal bool IsBoundTo(PlayerStore store) => ReferenceEquals(_store, store);
        internal static CorpState Get() => Current ??= new CorpState();
        internal static void Save() { }
    }

    internal static class Probe
    {
        internal static readonly int[] RepairCalls = new int[4];
        internal static int Repairs => RepairCalls.Sum();
        internal static bool SkipNativeLoad;
        internal static bool SkipLoadWithLateSignal()
        {
            if (!SkipNativeLoad) return true;
            ModHook.FireLate();
            return false;
        }
    }
    internal static partial class ModHook
    {
        internal static event Action OnGameLoadedLate;
        internal static void FireLate() => OnGameLoadedLate?.Invoke();
    }
    internal sealed class EmporiumEntry
    {
        internal static EmporiumEntry Instance;
        internal readonly GameInventory invElement = new(), backInvinvElement = new(), hiddenElement = new(),
            showcaseElement = new(), soldElement = new(), hirelingInv = new(), vendingMachineElement = new(),
            vendingFountainElement = new(), cassettePlayerElement = new();
        internal readonly Container trashcan = new(), dossier = new();
        internal GameInventory[] OtherInventories => new[] { hiddenElement, showcaseElement, soldElement, hirelingInv,
            vendingMachineElement, vendingFountainElement, trashcan.contentWindow.child, dossier.contentWindow.child };
        internal sealed class Container { internal readonly Window contentWindow = new(); }
        internal sealed class Window { internal readonly GameInventory child = new(); }
        internal bool enabled = true;
        internal readonly UnityEngine.GameObject gameObject = new();
        internal int Starts;
        internal void RunStartIfEnabled()
        {
            if (!enabled || Starts != 0) return;
            Starts++;
            SaveSlot.CurrentStore.LoadGame();
        }
    }

    internal static class SavePreparation
    {
        internal static bool TryEnter() => true;
        internal static void Exit() { }
    }
    internal static class Ext
    {
        internal sealed class Event { internal void Publish() { } }
        internal static readonly Event BeforeSave = new(), StateReset = new();
    }
    internal static class Cleanup { internal static void BeforeSave() { } }
    internal static class CargoWarehouse { internal static void ResetControllerStock() { } }
    internal static class CatalogPricing { internal static void Reset() { } }
    internal static class Crates
    {
        internal static void Reset() { }
        internal static void RebindLoaded() => Probe.RepairCalls[0]++;
    }
    internal static class ItemTags { internal static void RemoveRestoredMedicinePurity() => Probe.RepairCalls[1]++; }
    internal static partial class Goods { internal static void RestoreLegacyFootprints() => Probe.RepairCalls[2]++; }
    internal static class Forge { internal static void AfterStoreLoaded() => Probe.RepairCalls[3]++; }
    internal static class Services { internal static void ResetNightly() { } }
    internal static class Engine { internal static bool BuyActive; }
    internal static class Main { internal const string DisplayName = "CorpPresence"; }
    internal sealed class StoreUIManager
    {
        internal static readonly StoreUIManager Instance = new();
        internal int Notices;
        internal void Notify(string message) => Notices++;
    }
    internal static class L { internal static string K(string key, string zh, string en) => en; }
    internal static partial class Hooks
    {
        private static int _pendingVendingWater;
        private static void Guard(string _, Action action) => action();
    }
}
namespace CorpPresence.UI
{
    internal static class CargoSortWindow { internal static void Close() { } }
    internal static class CompanyWindow { internal static void Close() { } }
}
namespace UnityEngine
{
    internal static class Time { internal static float realtimeSinceStartup; }
    internal sealed class GameObject { internal Scene scene; }
}
namespace UnityEngine.SceneManagement
{
    internal record struct Scene(string name);
    internal enum LoadSceneMode { Single }
    internal static class SceneManager
    {
        internal static event Action<Scene, Scene> activeSceneChanged;
        internal static event Action<Scene, LoadSceneMode> sceneLoaded;
        internal static void Load(Scene scene) => sceneLoaded?.Invoke(scene, LoadSceneMode.Single);
        internal static void ChangeActive(Scene previous, Scene next) => activeSceneChanged?.Invoke(previous, next);
    }
}
