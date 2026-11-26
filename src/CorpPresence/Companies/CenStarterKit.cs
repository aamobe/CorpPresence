using System;

namespace CorpPresence;

internal static class CenStarterKit
{
    internal static string Claim(string choice, StoreSession session)
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return Unavailable();
        try { return ClaimCore(choice, session); }
        finally { CompanyTransaction.Exit(); }
    }

    private static string ClaimCore(string choice, StoreSession session)
    {
        var root = CorpState.Current;
        var state = root?.Workshop;
        var kit = CenSupplies.Kit(choice);
        bool Current() => session.IsCurrent && !StoreSession.CompanyProgressPaused && SaveSlot.CanWrite("CorpPresence")
            && ReferenceEquals(CorpState.Current, root) && LowerMachinist.StarterUnlocked(root);
        if (kit == null || state == null || !Current() ||
            (state.CenKit != null && state.CenKit != choice))
            return Unavailable();

        int count = kit.Contents.Length + 1;
        if (state.CenKit == null)
        {
            state.CenKit = choice;
            state.CenKitRemaining = count;
        }
        state.CenKitRemaining -= WorkshopCounter.DeliverCrate(count, state.CenKitRemaining,
            i => i == kit.Contents.Length ? CenSupplies.MakeBattery() : CatalogEntry.MakeDefault(kit.Contents[i]), Current);
        return state.CenKitRemaining == 0 ? Claimed() : Pending(state);
    }

    private static string Unavailable() => L.K("workshop.cen.claimUnavailable", "当前无法领取，请重新打开互助页。", "Claim unavailable. Reopen the mutual aid page.");
    internal static string Claimed() => L.K("workshop.cen.claimed", "开工料和电池已交到你一侧的柜台。", "Starter materials and battery delivered to your side of the counter.");
    internal static string Pending(WorkshopState state) => L.F("workshop.cen.pending", "这筐还剩 {0} 件待领。请在你一侧的柜台留出一箱的位置。", "{0} items still due. Leave space for a crate on your side of the counter.", state.CenKitRemaining);
}
