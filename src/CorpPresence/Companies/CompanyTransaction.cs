namespace CorpPresence;

// Native inventory, wallet and UI callbacks can call back synchronously into another service.
internal static class CompanyTransaction
{
    [System.ThreadStatic] private static bool _busy;
    [System.ThreadStatic] private static PlayerStore _saveStore;
    [System.ThreadStatic] private static StoreSession _saveSession;
    [System.ThreadStatic] private static System.Action _deferredSave;

    internal static bool TryEnter(PlayerStore saveStore = null)
    {
        if (_busy) return false;
        _busy = true;
        try
        {
            if (saveStore != null)
            {
                _saveSession = StoreSession.Capture();
                _saveStore = saveStore;
            }
            return true;
        }
        catch { Exit(); throw; }
    }

    internal static bool DeferSave(PlayerStore store, System.Action save)
    {
        if (store == null || !ReferenceEquals(store, _saveStore) || !_saveSession.IsCurrent) return false;
        _deferredSave ??= save;
        return true;
    }

    internal static void Exit()
    {
        var save = _deferredSave;
        var session = _saveSession;
        _deferredSave = null;
        _saveStore = null;
        _saveSession = default;
        _busy = false;
        if (save != null && session.IsCurrent) save();
    }
}
