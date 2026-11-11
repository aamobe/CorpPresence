using System.Collections.Generic;

namespace CorpPresence.UI;

// Entries retain owned samples, not translated strings or failed-name fallbacks.
internal sealed class PanelCatalogState
{
    private readonly Dictionary<string, CatalogEntry> _names = new();
    private CatalogEntry _leviathan;

    internal CatalogEntry Leviathan => _leviathan ??= new CatalogEntry
    {
        Id = "@leviathan",
        Page = "利维坦"
    };

    internal string Name(string id)
    {
        if (string.IsNullOrEmpty(id)) return id;
        if (!_names.TryGetValue(id, out var entry))
            _names.Add(id, entry = new CatalogEntry { Id = id });
        return entry.Name();
    }

    internal void Reset()
    {
        var names = new List<CatalogEntry>(_names.Values);
        var leviathan = _leviathan;
        _names.Clear();
        _leviathan = null;
        foreach (var entry in names) CatalogPricing.Release(entry);
        CatalogPricing.Release(leviathan);
    }
}
