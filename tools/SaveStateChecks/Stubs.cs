namespace CorpPresence
{
    internal sealed class PlayerStore
    {
        internal static PlayerStore Instance;
        internal static bool IsInstanceExist() => Instance != null;
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
