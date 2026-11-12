namespace CorpPresence;

internal readonly struct StoreSession
{
    internal static long Revision { get; private set; }
    private static System.WeakReference<PlayerStore> _inventoryLoadStore;
    private static bool _companyProgressPaused;
    private static string _inventoryLoadReport;
    private static bool MatchesInventoryLoadStore => _inventoryLoadStore != null
        && _inventoryLoadStore.TryGetTarget(out var store) && ReferenceEquals(SaveSlot.CurrentStore, store);

    internal static bool CompanyProgressPaused => _companyProgressPaused && MatchesInventoryLoadStore;
    internal static string InventoryLoadReport => MatchesInventoryLoadStore ? _inventoryLoadReport : null;
    internal static void RecordInventoryLoad(string report, bool paused)
    {
        var store = SaveSlot.CurrentStore;
        _inventoryLoadStore = store == null ? null : new System.WeakReference<PlayerStore>(store);
        _inventoryLoadReport = report;
        _companyProgressPaused = paused;
    }

    internal static void Invalidate()
    {
        unchecked { Revision++; }
        _inventoryLoadStore = null;
        _companyProgressPaused = false;
        _inventoryLoadReport = null;
    }

    private readonly PlayerStore _store;
    private readonly CorpState _root;
    private readonly string _run;
    private readonly long _generation;
    private readonly long _revision;

    private StoreSession(PlayerStore store, CorpState root)
    {
        _store = store; _root = root; _run = store?.runID; _generation = GameResources.Generation;
        _revision = Revision;
    }

    public static StoreSession Capture() => GameResources.Ready ? new StoreSession(SaveSlot.CurrentStore, CorpState.Get()) : default;
    internal static StoreSession Capture(PlayerStore store, CorpState root) => new StoreSession(store, root);

    public bool IsCurrent => Revision == _revision && GameResources.Ready && GameResources.Generation == _generation
        && _store != null && _root != null && ReferenceEquals(SaveSlot.CurrentStore, _store) && _store.runID == _run
        && ReferenceEquals(CorpState.Current, _root) && _root.RunId == _run && _root.IsBoundTo(_store);
}

