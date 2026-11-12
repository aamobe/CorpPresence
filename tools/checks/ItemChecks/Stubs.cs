namespace CorpPresence;

internal interface GraphNodeStorage { List<GraphNodeStorage> children { get; } }
internal sealed class GameItem : GraphNodeStorage
{
    internal string identifier, spritePath;
    internal object parentInventory;
    public List<GraphNodeStorage> children { get; } = new();
    internal int unitCount = 1;
    internal TagSystem state = new(), modifiedState = new();
    internal bool IsTag(string id) => state.Tags.Contains(id);
    internal void EnableTag(string id) { state.Tags.Add(id); modifiedState.Tags.Add(id); }
    internal bool IsDestroyed;
    internal int DestroyCalls;
    internal void Destroy() { DestroyCalls++; IsDestroyed=true; }
}
internal sealed class TagSystem
{
    internal readonly HashSet<string> Tags = new();
    internal TagSystem Clone() { var copy = new TagSystem(); copy.Tags.UnionWith(Tags); return copy; }
}
internal static class ItemDirectory
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal static GameItem CreateEmptyItem(string id) => new() { identifier = id, spritePath = "unknown" };
}
internal sealed class SaveItemNode
{
    internal string identifier, spritePath;
    internal long uuid { get; set; }
    internal GameItem tempLink { get; set; }
    internal TagSystem itemState = new();
    internal List<long> childItems = new();
    internal List<int> childItemInventoryNode = new();
}
internal static class StorePlacement { internal static bool Destroyed(GameItem item)=>item.IsDestroyed; }
internal static class GameResources { internal static bool Ready; internal static long Generation { get; set; } }
internal static class ModLog
{
    internal static readonly List<string> Warnings=new();
    internal static readonly List<string> Information=new();
    internal static void Warn(string text)=>Warnings.Add(text);
    internal static void Info(string text)=>Information.Add(text);
}
internal static partial class ModHook
{
    internal static event Action<ModItemDirectory> OnModItemDirectoryInit;
    internal static int Count=>OnModItemDirectoryInit?.GetInvocationList().Length??0;
    internal static void Fire(ModItemDirectory directory)=>OnModItemDirectoryInit?.Invoke(directory);
}
internal sealed class ModItemDirectory
{
    internal readonly Dictionary<string,Func<GameItem>> factoryDictionary=new(StringComparer.Ordinal);
    internal string RejectId, ThrowId, ThrowRemoveId;
    internal int Adds;
    internal bool SceneObjectDestroyed;
    public static bool operator ==(ModItemDirectory left, ModItemDirectory right)
    {
        if (left is null) return right is null || right.SceneObjectDestroyed;
        if (right is null) return left.SceneObjectDestroyed;
        return ReferenceEquals(left, right);
    }
    public static bool operator !=(ModItemDirectory left, ModItemDirectory right) => !(left == right);
    public override bool Equals(object other) => ReferenceEquals(this, other);
    public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
    internal bool Has(string id)=>factoryDictionary.ContainsKey(id);
    internal bool Add(string id,Func<GameItem> factory)
    {
        Adds++;
        if(id==ThrowId)throw new InvalidOperationException("injected add failure");
        if(id==RejectId || id==null || factory==null)return false;
        // The native Directory<T>.Add removes the existing global entry first.
        DirectoryMaster.Native.Remove(id);
        DirectoryMaster.Current?.factoryDictionary.Remove(id);
        return factoryDictionary.TryAdd(id,factory);
    }
    internal bool Remove(string id) { if(id==ThrowRemoveId) throw new InvalidOperationException("remove failed"); return factoryDictionary.Remove(id); }
}
internal static class DirectoryMaster
{
    internal static ModItemDirectory Current;
    internal static bool TryAddDirectory(ModItemDirectory directory)
    {
        if (Current is not null) return false;
        Current = directory;
        ModHook.Fire(directory);
        return true;
    }
    internal static readonly Dictionary<string,Func<GameItem>> Native=new();
    internal static bool Has<T>(string id)=>Native.ContainsKey(id) || Current?.Has(id)==true;
    internal static Func<GameItem> GetFactory<T>(string id)=>Native.TryGetValue(id,out var factory)?factory:Current?.factoryDictionary.GetValueOrDefault(id);
    internal static int DummyCreates, PostprocessCalls;
    internal static GameItem Item(string id,bool isOwned)
    {
        var item=GetFactory<GameItem>(id)?.Invoke();
        if(item==null){DummyCreates++;return ItemDirectory.CreateEmptyItem(id);}
        item.identifier=id;PostprocessCalls++;return item;
    }
}
internal static class ItemIdentity
{
    internal sealed record Entry(string Id,string LegacyId,string LegacySprite,string Goods,string Company);
    internal static readonly IReadOnlyList<Entry> All=new[] {
        new Entry("corp_presence_test","ammo_box","corp_med_test","test",null),
        new Entry("corp_presence_other","ammo_box","corp_med_other","other",null),
        new Entry("corp_presence_card_test","card_pharma","corp_card_test",null,"test") };
    internal static Entry Find(string id,string sprite)=>All.FirstOrDefault(e=>e.Id==id || (e.LegacyId==id && e.LegacySprite==sprite));
    internal static string ResolveId(string id,string sprite)=>Find(id,sprite)?.Id;
}
internal static partial class Goods
{
    internal static int Builds;
    internal static GameItem Product;
    internal static bool ReturnNull;
    internal static bool Throw;
    internal static GameItem Build(ItemIdentity.Entry entry)
    {
        Builds++;
        if (Throw) throw new InvalidOperationException("injected construction failure");
        return ReturnNull ? null : Product ?? new() { identifier = entry.Id };
    }
}
internal static class Cards
{
    internal static int Builds;
    internal static GameItem Build(ItemIdentity.Entry entry) { Builds++;return new(){identifier=entry.Id}; }
}
internal static class MaintenanceDiagnostics
{
    internal static readonly List<(string Source,string Stage,string Code,string ItemId)> Faults=new();
    internal static bool Fail;
    internal static void RecordRuntime(string source,string stage,string code,Exception error,string itemId=null) => Record(source,stage,code,error,itemId);
    internal static void Record(string source,string stage,string code,Exception error,string itemId=null)
    { if(Fail) throw new Exception("diagnostic failure"); Faults.Add((source,stage,code,itemId)); }
}
