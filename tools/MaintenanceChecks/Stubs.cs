namespace CorpPresence
{
    internal sealed class PlayerStore
    {
        private static PlayerStore _instance;
        internal static int LazyCreates;
        internal static PlayerStore Instance { get { if (_instance == null) { LazyCreates++; _instance = new(); } return _instance; } set => _instance = value; }
        internal static bool IsInstanceExist() => _instance != null;
        internal Client currentClientInstance;
        internal string runID = "test";
        internal Dictionary<string, string> modData = new();
    }
    internal sealed class StoreClient { }
    internal sealed class Client { internal StoreClient Blueprint; internal StoreClient GetClientBlueprint() => Blueprint; }
    internal static class GameResources { internal static bool Ready = true; internal static long Generation = 1; }
    internal static class ModLog { internal static void Warn(string text) { } }
}
namespace CorpPresence.Malice
{
    // Only required to compile the unused malice validation overload.
    internal sealed class MaliceState
    {
        public Dictionary<string, object> Corps = new();
        public EnvoyStub Envoy = new();
        public object Rebel = new();
    }
    internal sealed class EnvoyStub
    {
        public List<object> Coins = new();
        public List<object> Visits = new();
    }
}

namespace CorpPresence {
internal static class Main {internal const string Version="test";}
internal static class Portraits {internal static Action Probe; internal static void AppendDiagnostics(ICollection<MaintenanceCounter> c){Probe?.Invoke();c.Add(new MaintenanceCounter("portrait.textures",2));}}
internal static class CatalogPricing {internal static void AppendDiagnostics(ICollection<MaintenanceCounter> c){}}
internal static class ItemRegistry {internal static void AppendDiagnostics(ICollection<MaintenanceCounter> c){}}
}
