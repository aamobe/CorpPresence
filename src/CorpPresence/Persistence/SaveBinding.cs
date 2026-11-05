using System;
using System.Collections.Generic;
namespace CorpPresence;

// Non-persistent ownership of one decoded record. No global cache or native-player root.
internal sealed class SaveBinding
{
    private readonly WeakReference<PlayerStore> _store;
    private WeakReference<Dictionary<string, string>> _data;
    private readonly string _key;
    private string _raw;

    public SaveBinding(PlayerStore store, string key)
    {
        _store = new WeakReference<PlayerStore>(store);
        _key = key;
        Refresh(store);
    }

    private string Read(Dictionary<string, string> data) => data != null && data.TryGetValue(_key, out string value) ? value : null;

    public bool Matches(PlayerStore store)
    {
        if (store == null || !_store.TryGetTarget(out var owner) || !ReferenceEquals(owner, store)) return false;
        Dictionary<string, string> data = store.modData;
        if (_data == null)
        {
            // Other MODs may initialize an empty native dictionary before our first save.
            if (_raw != null || Read(data) != null) return false;
            if (data != null) _data = new WeakReference<Dictionary<string, string>>(data);
            return true;
        }
        return _data.TryGetTarget(out var original) && ReferenceEquals(original, data) && ReferenceEquals(_raw, Read(data));
    }

    public void Refresh(PlayerStore store)
    {
        var data = store.modData;
        if (data == null) _data = null;
        else if (_data == null) _data = new WeakReference<Dictionary<string, string>>(data);
        else _data.SetTarget(data);
        _raw = Read(data);
    }
}
