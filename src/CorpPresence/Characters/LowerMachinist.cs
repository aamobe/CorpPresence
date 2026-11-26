using System;
using System.Collections.Generic;

namespace CorpPresence;

internal sealed class LowerMachinist : Character
{
    private static readonly int[] Gates = { 10, int.MinValue };
    private static readonly string[] Starter = { MineralCrusher.Key, PowderSieve.Key, SinteringStation.Key,
        PowderBox.Key, PowderBox.Key, PowderBox.Key };
    private static readonly string[] Workshop = { CraftingStation.Key, MineralGrowthStation.Key,
        CraftingStation.Blueprint, MineralGrowthBatch.OreBlueprint };

    internal const string CharacterId = "lower_machinist";
    public override string Id => CharacterId;
    public override string Name => L.T("阿岑", "Cen");
    public override string Faction => "FACTION_LOWER_LEVEL";
    public override string PortraitRef => "maleLower_0";
    public override string Portrait(CharacterState state, string kind) => CharacterId;
    public override int[] Thresholds => Gates;
    public override int CashNeeded => int.MinValue;
    public override bool HasDaily => false;
    public override int BuyGoodsNeeded => 0;
    public override int BuyGoodsHeld() => 0;
    public override bool IsBuyChapter(int chapter) => false;
    public override bool Accepts(GameItem item) => false;
    public override double PriceMultiplier(GameItem item, string kind) => 1;
    public override Visit Daily(CharacterState state) => Chapter(1);

    private static bool Reached(CorpState state, int chapter) =>
        state?.Characters != null && state.Characters.TryGetValue(CharacterId, out var seller)
        && seller?.Chapter >= chapter;

    internal static bool StarterUnlocked(CorpState state) => Reached(state, 1);
    internal static bool MineralizerUnlocked(CorpState state) => Reached(state, 2);

    internal static bool HasPowder() => QualifiedStock() >= 300;

    // The notebook and visit scheduler read the same physical inventory. The optional array
    // totals bins 0–4 and the recovery bin across all sieves, excluding their display boxes.
    internal sealed record StockLine(string Container, int Ordinal, int Bin, int Purity, long Amount, long Qualified);

    internal static long QualifiedStock(long[] bins = null, List<StockLine> detail = null)
    {
        var items = EmporiumEntry.Instance?.GetAllOwnedItems();
        if (bins != null)
        {
            if (bins.Length != 6) throw new ArgumentException("Expected six sieve bins.", nameof(bins));
            Array.Clear(bins, 0, bins.Length);
        }
        detail?.Clear();
        if (items == null) return 0;
        int boxNumber = 0, sieveNumber = 0;
        long qualified = 0;
        var seen = new HashSet<GameItem>();
        Span<int> parts = stackalloc int[PowderBox.ComponentCount];
        foreach (var item in items)
        {
            if (item == null || !seen.Add(item)) continue;
            if (Goods.IdOf(item) == PowderSieve.Key)
            {
                int number = ++sieveNumber;
                qualified += PowderSieve.QualifyingStock(item, bins, detail == null ? null :
                    (bin, purity, amount, credit) => detail.Add(new StockLine(item.GetDisplayName(true), number, bin, purity, amount, credit)));
                continue;
            }
            if (!PowderBox.IsOwnedBox(item)) continue;
            long amount = PowderBox.Read(item, parts);
            int purity = PowderClassification.FromParts(parts).PurityBasisPoints;
            long credit = purity >= 8100 ? amount : 0;
            qualified += credit;
            boxNumber++;
            if (detail != null && amount > 0)
                detail.Add(new StockLine(item.GetDisplayName(true), boxNumber, -1, purity, amount, credit));
        }
        return qualified;
    }

    public override Visit Chapter(int chapter)
    {
        bool first = chapter == 1;
        int price = first ? 300 : 450;
        bool buying = false;
        return new Visit
        {
            TalkOnly = true,
            Lines = new[] { first
                ? L.T("我叫阿岑，住下层。街坊把闲着的破碎机、筛分机和烧结站修了一遍，凑了三个接粉盒。三百块一套，收回来的钱留给下一家换零件。", "I'm Cen from downstairs. The neighbors fixed up a crusher, a sieve and a sintering station, and found three powder boxes. Three hundred for the lot. It buys parts for the next shop.")
                : L.T("听说你筛出了三百点好粉。这回带了综合工艺台和矿化机，配普通电子元件、普通矿石两张图。粉你留着用。", "Heard you sorted three hundred points of good powder. I've brought a crafting station, a mineral growth station and plans for electronics and common ore. Keep your powder.") },
            Ask = L.T($"整套 {price} 元，要搬进来吗？", $"The set is {price} credits. Shall I bring it in?"),
            Choices = new[] {
                new Choice(L.T("整套买下", "Buy the set"), first
                    ? L.K("workshop.cen.firstHandoff", "翻开电话簿找「下层互助」，先挑一筐免费试料，里面有电池。", "Look up Lower Levels Mutual Aid in the phone book and pick a free starter kit with a battery.") + "\n" +
                      L.K("story.cen.nextVisit", "接粉盒和筛分仓里的粉都算。每盒或每仓纯度够 81%，凑满 300 点，我就带矿化机来。粉给你留着。", "I count powder in boxes and sieve bins. Get 300 points in total from boxes or bins at least 81% pure, and I’ll bring the mineral growth station. You keep the powder.")
                    : L.T("给箱子腾个地方。", "Make room for the crate."), () => buying = true),
                new Choice(L.T("下次再说", "Another time"), L.T("行，我改天再来。", "Sure. I'll come back."), null) },
            Exit = L.T("回见。", "See you around."),
            TryComplete = () => buying && Deliver(first ? Starter : Workshop, price)
        };
    }

    private static bool Deliver(string[] contents, int price)
    {
        var store = SaveSlot.CurrentStore;
        var session = StoreSession.Capture();
        if (!session.IsCurrent || store == null || !SaveSlot.CanWrite("CorpPresence")) return false;
        if (!store.CanPay(price)) return Notice(L.T("现金不足，阿岑改天再来。", "Not enough cash. Cen will return."));
        var crate = Crates.MakeFilled("crate_spacer", contents);
        if (crate == null) return false;
        bool paid = false;
        try
        {
            if (Crates.InnerOf(crate)?.childItems.Count != contents.Length)
                return Notice(L.T("设备没装齐，这次先不收钱。", "The set is incomplete. No charge."));
            if (!session.IsCurrent || !WorkshopCounter.CanFit(crate) || !WorkshopCounter.TryPlaceOnCounter(crate))
                return Notice(L.T("柜台放不下设备箱，阿岑改天再来。", "No room for the crate. Cen will return."));
            if (!session.IsCurrent || !SaveSlot.CanWrite("CorpPresence") || !store.CanPay(price)) return false;
            int before = store.GetCash();
            try { CompanyMoney.Pay(store, price); }
            catch (Exception error) { ModLog.Warn("阿岑交货扣款回调异常：" + error.GetType().Name); }
            paid = store.GetCash() == before - price;
            return paid;
        }
        finally { if (!paid && !StorePlacement.Destroyed(crate)) crate.Destroy(); }
    }

    private static bool Notice(string text)
    {
        StoreUIManager.Instance?.Notify(text);
        return false;
    }
}
