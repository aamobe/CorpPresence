namespace CorpPresence;

internal sealed class CartelShizuka : Character
{
    internal const string CharacterId = "cartel_shizuka";
    private const string PortraitKey = "cartel_shizuka";
    private static readonly int[] VisitThresholds = { 10, int.MinValue };

    internal static bool MineralizerUnlocked(CorpState state) =>
        state?.Characters != null && state.Characters.TryGetValue(LowerMachinist.CharacterId, out var seller)
        && seller?.Chapter >= 2;

    public override string Id => CharacterId;
    public override string Name => L.T("黑羽静", "Shizuka Kurobane");
    public override string Faction => "FACTION_CARTEL";
    public override int[] Thresholds => VisitThresholds;
    public override int CashNeeded => int.MinValue;
    public override string PortraitRef => "femaleUpper_1";
    public override int BuyGoodsNeeded => 0;
    public override int BuyGoodsHeld() => 0;
    public override bool HasDaily => false;

    public override string Portrait(CharacterState state, string kind) => PortraitKey;
    public override bool IsBuyChapter(int chapter) => false;
    public override Visit Daily(CharacterState state) => Chapter(1);
    public override bool Accepts(GameItem item) => false;
    public override double PriceMultiplier(GameItem item, string kind) => 1;

    public override Visit Chapter(int chapter) => chapter == 2 ? BlueprintVisit() : Introduction();

    private static Visit BlueprintVisit() => new()
    {
        TalkOnly = true,
        Lines = new[]
        {
            L.T("听阿岑说，你把矿化机搬回来了。这张光盐芯蓝图给你，配方写在图上。头一批备好四十点渣料、十点稀有金属粉，两盒纯度都要到八成一。", "Cen tells me your mineral growth station is in. Take this light salt core blueprint. The recipe is on it: forty points of residue and ten of rare metal powder, each at least 81% pure.")
        },
        Gift = new[] { "corp_presence_" + MineralGrowthBatch.LightSaltBlueprint },
        Exit = L.T("图纸收好。", "Keep that blueprint safe.")
    };

    private static Visit Introduction() => new()
    {
        TalkOnly = true,
        Lines = new[]
        {
            L.T("黑羽静。卡尔特的人说，你这间铺子值得我亲自来一趟。", "Shizuka Kurobane. The Cartel said your shop was worth a visit."),
            L.T("我带了一份眠苔。把接种包放进水培槽，长成了就能收菌块。", "I brought some sleep moss. Put the starter in a hydroponic tray and harvest the blocks when it matures.")
        },
        Gift = new[] { SleepMoss.SeedId },
        Exit = L.K("story.shizuka.mineralizerHint", "先把眠苔养起来。阿岑那套矿化机到手，我再带光盐芯的图纸来。", "Get the moss growing. Once you have Cen’s mineral growth station, I’ll bring the light salt core blueprint.")
    };
}
