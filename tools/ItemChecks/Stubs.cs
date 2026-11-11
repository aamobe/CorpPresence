namespace CorpPresence;

internal sealed class GameItem
{
    internal string identifier, spritePath;
    internal object parentInventory;
    internal bool IsDestroyed;
    internal int DestroyCalls;
    internal void Destroy() { DestroyCalls++; IsDestroyed=true; }
}
internal sealed class SaveItemNode { internal string identifier, spritePath; }
internal static class StorePlacement { internal static bool Destroyed(GameItem item)=>item.IsDestroyed; }
internal static class GameResources { internal static bool Ready; }
internal static class ModLog
{
    internal static readonly List<string> Warnings=new();
    internal static void Warn(string text)=>Warnings.Add(text);
    internal static void Info(string text) { }
}
internal static class ModHook
{
    internal static event Action<ModItemDirectory> OnModItemDirectoryInit;
    internal static int Count=>OnModItemDirectoryInit?.GetInvocationList().Length??0;
    internal static void Fire(ModItemDirectory directory)=>OnModItemDirectoryInit?.Invoke(directory);
}
internal sealed class ModItemDirectory
{
    internal readonly Dictionary<string,Func<GameItem>> factoryDictionary=new(StringComparer.Ordinal);
    internal string RejectId, ThrowId;
    internal int Adds;
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
    internal bool Remove(string id)=>factoryDictionary.Remove(id);
}
internal static class DirectoryMaster
{
    internal static ModItemDirectory Current;
    internal static readonly Dictionary<string,Func<GameItem>> Native=new();
    internal static bool Has<T>(string id)=>Native.ContainsKey(id) || Current?.Has(id)==true;
    internal static GameItem Item(string id,bool isOwned)=>Native.TryGetValue(id,out var factory)?factory():Current.factoryDictionary[id]();
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
internal static class Goods
{
    internal static int Builds;
    internal static GameItem Build(ItemIdentity.Entry entry) { Builds++;return new(){identifier=entry.Id}; }
}
internal static class Cards
{
    internal static int Builds;
    internal static GameItem Build(ItemIdentity.Entry entry) { Builds++;return new(){identifier=entry.Id}; }
}
