namespace CorpPresence;

internal sealed class ModItemDirectory
{
    internal readonly Dictionary<string, Func<GameItem>> factoryDictionary = new();
    internal bool SceneObjectDestroyed;

    internal bool Has(string id) => factoryDictionary.ContainsKey(id);
    internal bool Add(string id, Func<GameItem> factory) => factoryDictionary.TryAdd(id, factory);
    internal bool Remove(string id) => factoryDictionary.Remove(id);
    public static bool operator ==(ModItemDirectory left, ModItemDirectory right) =>
        left is null ? right is null || right.SceneObjectDestroyed :
        right is null ? left.SceneObjectDestroyed : ReferenceEquals(left, right);
    public static bool operator !=(ModItemDirectory left, ModItemDirectory right) => !(left == right);
    public override bool Equals(object other) => ReferenceEquals(this, other);
    public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
}

internal static class ModHook
{
    internal static event Action<ModItemDirectory> OnModItemDirectoryInit;
    internal static void Initialize(ModItemDirectory directory) => OnModItemDirectoryInit?.Invoke(directory);
}

internal static class DirectoryMaster
{
    internal static ModItemDirectory Current;
    internal static bool AddDirectory(ModItemDirectory directory)
    {
        if (Current is not null) return false;
        Current = directory;
        ModHook.Initialize(directory);
        return true;
    }
    internal static bool Has<T>(string id) => Current is null || GetFactory<T>(id) != null;
    internal static Func<GameItem> GetFactory<T>(string id) => Current?.factoryDictionary.GetValueOrDefault(id);
    internal static GameItem Item(string id, bool isOwned) =>
        Current is null ? Goods.Factory() : GetFactory<GameItem>(id)?.Invoke();
}

internal static class ItemIdentity
{
    internal sealed record Entry(string Id, string Goods);
    internal static readonly IReadOnlyList<Entry> All = new Entry[]
    {
        new("corp_presence_crate_pastel", "crate_pastel"),
        new("corp_presence_paid_goods", "paid_goods"),
        new("corp_presence_card_pastel", null),
        new("corp_presence_fine_mince", "fine_mince"),
        new("corp_presence_coarse_mince", "coarse_mince"),
        new("corp_presence_fish_mince", "fish_mince"),
        new("corp_presence_fruit_paste", "fruit_paste")
    };
    internal static Entry Find(string id, string sprite) => All.SingleOrDefault(entry => entry.Id == id);
    internal static string ResolveId(string id, string sprite) => id;
}

internal sealed class SaveItemNode { internal string identifier, spritePath; }
internal static class Cards
{
    internal static GameItem Make(Company company) => ItemRegistry.Create("corp_presence_card_" + company.Id);
    internal static GameItem Build(ItemIdentity.Entry entry) => new() { identifier = entry.Id };
}
internal static class Phone
{
    internal static int Activations;
    internal static void Activate(Company _) => Activations++;
}
internal static class MaintenanceDiagnostics
{
    internal static readonly List<(string Source, string Stage, string Code, string ItemId)> Faults = new();
    internal static void Record(string source, string stage, string code, Exception error, string itemId = null) =>
        Faults.Add((source, stage, code, itemId));
    internal static void RecordRuntime(string source, string stage, string code, Exception error, string itemId = null) =>
        Record(source, stage, code, error, itemId);
}

internal sealed class TagState
{
    private int _int;
    private long _long;
    private bool _bool;
    public string Value;
    internal TagState Enable() => this;
    internal TagState SetInt(int value) { _int = value; return this; }
    internal TagState SetLong(long value) { _long = value; return this; }
    internal TagState SetBool(bool value) { _bool = value; return this; }
    internal TagState SetString(string value) { Value = value; return this; }
    internal int GetInt() => _int;
    internal long GetLong() => _long;
    internal bool GetBool() => _bool;
    internal string GetString() => Value;
}

internal static partial class Canning
{
    private const string FishMinceId = "corp_presence_fish_mince", PasteId = "corp_presence_fruit_paste";
    private const string Prion = "ANIMAL_PRION_TAG", BatteryEnergyTag = "energy";
    private const int PasteKcal = 240, PastePower = 1, MincePower = 2;
    internal static void Process(GameItem machine, GameInventory input, GameInventory output) =>
        Grind(machine, new GameSlotInventory { childItem = new GameItem() }, input, output);
    internal static long FruitReserve(GameItem machine) => StoredFruitKcal(machine);
    private static void ProductionNotice(string _) { }
    private static void CheckOutput(GameInventory inventory, Func<GameItem> make, string ready, int count = 1) =>
        throw new InvalidOperationException("This harness exercises processing, not the preview button.");
}

internal sealed class GameSlotInventory { internal GameItem childItem; }
internal static class MachineUpgrade { internal static int PowerCut(GameItem _) => 0; }
internal static class GutterflowPelletHelper { internal static bool IsTainted(GameItem _) => false; }
internal static class CurrentConverterHelper { internal static bool IsCurrentConverter(GameItem _) => false; }
internal static class HypertechBatteryHelper { internal static bool IsHypertechBattery(GameItem _) => false; }
internal static class PowerHelper
{
    internal static bool CanDrawPowerSource(GameItem item, int _) => item != null;
    internal static bool DrawPowerSource(GameItem item, int _) => item != null;
    internal static void SetPowerSourceAt(GameItem item, int _) { }
}
