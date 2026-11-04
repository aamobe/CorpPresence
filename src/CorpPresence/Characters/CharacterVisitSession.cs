namespace CorpPresence;

// One generated visitor owns this guard; no static callback registry.
internal sealed class CharacterVisitSession
{
    private readonly StoreSession _session = StoreSession.Capture();
    private readonly CorpState _root = CorpState.Current;
    private readonly string _id;
    private readonly CharacterState _state;
    private readonly StoreClient _client;
    private bool _finished;

    public CharacterVisitSession(string id, CharacterState state, StoreClient client)
    { _id = id; _state = state; _client = client; }

    public bool IsCurrent => _session.IsCurrent && SaveSlot.CanWrite("CorpPresence")
        && _root != null && _root.Characters.TryGetValue(_id, out var current) && ReferenceEquals(current, _state)
        && ReferenceEquals(SaveSlot.CurrentStore?.currentClientInstance?.GetClientBlueprint(), _client);

    public bool TryFinish()
    {
        if (_finished || !IsCurrent) return false;
        _finished = true;
        return true;
    }
}
