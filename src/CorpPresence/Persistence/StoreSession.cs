namespace CorpPresence;

internal readonly struct StoreSession
{
    private readonly PlayerStore _store;
    private readonly CorpState _root;
    private readonly string _run;
    private readonly long _generation;

    private StoreSession(PlayerStore store, CorpState root)
    {
        _store = store; _root = root; _run = store?.runID; _generation = GameResources.Generation;
    }

    public static StoreSession Capture() => GameResources.Ready ? new StoreSession(SaveSlot.CurrentStore, CorpState.Get()) : default;

    public bool IsCurrent => GameResources.Ready && GameResources.Generation == _generation && _store != null && _root != null
        && ReferenceEquals(SaveSlot.CurrentStore, _store) && _store.runID == _run
        && ReferenceEquals(CorpState.Current, _root) && _root.RunId == _run && _root.IsBoundTo(_store);
}

