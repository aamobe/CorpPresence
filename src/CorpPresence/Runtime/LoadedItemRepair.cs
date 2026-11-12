using System;
using System.Collections.Generic;

namespace CorpPresence.Runtime;

// Native saved features are restored after DecodeNodes. Defer only this completed
// load's company-item repair when localization/resources are still starting.
internal static class LoadedItemRepair
{
    private static WeakReference<PlayerStore> _store;
    private static WeakReference<Dictionary<string, string>> _data;
    private static string _run;
    private static long _generation;
    private static long _revision;
    private static Action _repair;

    public static void Request(Action repair)
    {
        Reset();
        PlayerStore store = SaveSlot.CurrentStore;
        if (store == null || repair == null) return;
        _store = new WeakReference<PlayerStore>(store);
        _data = store.modData == null ? null : new WeakReference<Dictionary<string, string>>(store.modData);
        _run = store.runID;
        _generation = GameResources.Generation;
        _revision = StoreSession.Revision;
        _repair = repair;
    }

    public static void Reset()
    {
        _store = null;
        _data = null;
        _run = null;
        _repair = null;
    }

    public static void Tick()
    {
        if (_repair == null) return;
        PlayerStore current = SaveSlot.CurrentStore;
        if (_generation != GameResources.Generation || _revision != StoreSession.Revision
            || _store == null || !_store.TryGetTarget(out var owner)
            || !ReferenceEquals(current, owner) || current.runID != _run
            || (_data == null ? current.modData != null : !_data.TryGetTarget(out var data) || !ReferenceEquals(current.modData, data)))
        {
            Reset();
            return;
        }
        if (!GameResources.Ready) return;
        Action repair = _repair;
        Reset(); // No duplicate repair on reentry or exception.
        repair();
    }
}
