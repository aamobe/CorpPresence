using System;

namespace CorpPresence;

internal static class ShizukaSupplies
{
    private const string Reactor = "@" + CartelReactor.Key;
    private const string Resin = "@" + CartelReactor.Resin;
    private const string Block = "@sleep_moss_block";
    private const string Alcohol = "@" + AlcoholPreparationStation.Key;
    private const string Sleeve = "@" + ClassifierGoods.Sleeve;
    private const string Blueprint = "@" + MineralGrowthBatch.LightSaltBlueprint;
    private static readonly string[] Starter = { Reactor, Block, Block, Resin, Resin };

    internal static readonly CatalogEntry[] All =
    {
        Supply(Resin, 12), Supply(Block, 16), Supply("@sleep_moss_seed", 35), Supply(Reactor, 300),
        Supply(Alcohol, 300), Supply(Sleeve, 40), Supply(Blueprint, 35)
    };

    private static CatalogEntry Supply(string id, int price) => new()
    {
        Id = id, MinTier = 0, FixedBaseValue = price, FixedListValue = price,
        IgnoreOrderRate = true, NoQuantityQuota = true
    };

    internal static string OrderError(string id, CorpState root) => id switch
    {
        Alcohol when !WorkshopSaleOffer.GelOrderSettled(root) =>
            L.K("workshop.shizuka.gelLocked", "交清首单眠苔胶后供应", "Settle the first gel order"),
        Sleeve or Blueprint when !CartelShizuka.BlueprintDelivered(root) =>
            L.K("workshop.shizuka.blueprintLocked", "收到黑羽静的光盐芯图后供应", "Receive Shizuka’s light salt blueprint"),
        _ => null
    };

    internal static string Claim(StoreSession session)
    {
        var root = CorpState.Current;
        var state = root?.Workshop;
        bool Current() => session.IsCurrent && !StoreSession.CompanyProgressPaused && SaveSlot.CanWrite("CorpPresence")
            && ReferenceEquals(state, CorpState.Current?.Workshop) && CartelShizuka.Introduced(root);
        if (state == null || !Current() || !CompanyTransaction.TryEnter(SaveSlot.CurrentStore))
            return L.K("workshop.shizuka.claimUnavailable", "请重新打开黑羽静的试料页。", "Reopen Shizuka’s samples page.");
        try
        {
            state.ShizukaKitRemaining -= WorkshopCounter.DeliverCrate(Starter.Length, state.ShizukaKitRemaining,
                i => CatalogEntry.MakeDefault(Starter[i]), Current);
            return Receipt(state);
        }
        finally { CompanyTransaction.Exit(); }
    }

    internal static string Receipt(WorkshopState state) => state.ShizukaKitRemaining == 0
        ? L.K("workshop.shizuka.claimed", "试料已全部交到你一侧的柜台。", "All samples have reached your side of the counter.")
        : L.F("workshop.shizuka.remaining", "试料还剩 {0} 件。请在你一侧的柜台留出一箱的位置。", "{0} sample items remain. Leave room for one crate on your side of the counter.", state.ShizukaKitRemaining);
}
