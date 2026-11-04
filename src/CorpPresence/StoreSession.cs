namespace CorpPresence;

internal readonly struct StoreSession
{
	private readonly PlayerStore _store;

	private readonly CorpState _root;

	private readonly string _run;

	private readonly long _generation;

	public bool IsCurrent
	{
		get
		{
			return Matches(_store, _root, _run, _generation);
		}
	}

	internal static bool Matches(PlayerStore store, CorpState root, string run, long generation)
	{
		return GameResources.Ready && GameResources.Generation == generation
			&& store != null && root != null && SaveSlot.CurrentStore == store
			&& store.runID == run && CorpState.Current == root && root.RunId == run
			&& root.IsBoundTo(store);
	}

	private StoreSession(PlayerStore store, CorpState root)
	{
		_store = store;
		_root = root;
		_run = store?.runID;
		_generation = GameResources.Generation;
	}

	public static StoreSession Capture()
	{
		if (!GameResources.Ready)
		{
			return default;
		}
		return new StoreSession(SaveSlot.CurrentStore, CorpState.Get());
	}
}
