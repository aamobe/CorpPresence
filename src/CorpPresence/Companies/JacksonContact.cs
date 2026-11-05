namespace CorpPresence;

internal sealed class JacksonContact : Company
{
    private static readonly string[] Greeting = { "我这里有些养鼠设备。你需要什么？" };
    private static readonly string[] GreetingEn = { "I have some rat-keeping equipment. What do you need?" };
    private static readonly CatalogEntry[] Equipment =
    {
        new() { Id = "@" + RatWheelGenerator.GoodsKey, MinTier = 0 },
        new() { Id = "animal_cage", MinTier = 0 },
        new() { Id = "livestock_feed_dispenser", MinTier = 0 }
    };

    public override string Id => "jackson_private";
    public override string Name => L.T("杰克逊博士 · 私人电话", "Dr. Jackson · Private Line");
    public override string CharacterId => "dr_jackson";
    public override long PhoneNumber => 52746L;
    public override string[] TierNames => System.Array.Empty<string>();
    public override string[] FrontDesk => L.T(Greeting, GreetingEn);
    public override string FrontDeskDone => L.T("记下了，明天送到。", "Got it. I'll send it tomorrow.");
    public override string CardFlavor => "";
    public override string DeskName => L.T("杰克逊博士", "Dr. Jackson");
    public override string DeskAsk => "";
    public override CatalogEntry[] Catalog => Equipment;
}
