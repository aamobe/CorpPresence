using System;

namespace CorpPresence;

internal abstract class WorkshopContact : Company
{
    public override string[] TierNames => Array.Empty<string>();
    public override string CardFlavor => "";
    public override string DeskAsk => "";

    protected sealed override string ContentOrderError(CatalogEntry entry) =>
        StoreSession.CompanyProgressPaused || !SaveSlot.CanWrite("CorpPresence")
            ? UnavailableOrderEntry() : SupplyError(entry.Id, CorpState.Get());

    protected abstract string SupplyError(string id, CorpState root);
}
