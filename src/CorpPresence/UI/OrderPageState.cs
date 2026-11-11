using System.Collections.Generic;

namespace CorpPresence.UI;

// One open company window owns one order session, shared across its order tabs.
internal sealed class OrderPageState
{
    internal readonly Dictionary<CatalogEntry, int> Cart = new();
    internal string Category = "日用";
    internal int PartsPage;
    internal int Page;
    internal string PageKey;

    internal void Reset()
    {
        Cart.Clear();
        Category = "日用";
        PartsPage = 0;
        Page = 0;
        PageKey = null;
    }
}
