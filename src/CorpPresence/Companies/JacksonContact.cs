namespace CorpPresence;

internal sealed class JacksonContact : Company
{
    private static readonly string[] Greeting =
    {
        "杰克逊。机器改装是我的老本行。我自己做的几台机器和配件，最近正好有些能出手。",
        "工作台上刚腾出点地方。我做的机器和改装配件，想看看吗？"
    };
    private static readonly string[] GreetingEn =
    {
        "Jackson here. Custom modifications are my trade. I've got a few machines and parts I built myself that I can let go.",
        "I've just cleared some space on the workbench. Want to see the machines and custom parts I've been working on?"
    };
    private static readonly CatalogEntry[] Equipment =
    {
        new() { Id = "@" + RatWheelGenerator.GoodsKey, MinTier = 0 },
        new() { Id = ProductionWorkBay.OrderId, MinTier = 0, FixedListValue = ProductionWorkBay.Price },
        new() { Id = "animal_cage", MinTier = 0 },
        new() { Id = "livestock_feed_dispenser", MinTier = 0 },
        new() { Id = CargoWarehouse.OrderId, MinTier = 0, FixedListValue = CargoWarehouse.WarehousePrice },
        new() { Id = CargoWarehouse.ModuleOrderId, MinTier = 0, FixedListValue = CargoWarehouse.ModulePrice },
        new() { Id = CargoWarehouse.ControllerOrderId, MinTier = 0, FixedListValue = CargoWarehouse.ControllerPrice }
    };

    public override string Id => "jackson_private";
    public override string Name => L.K("companies.jacksonContact.name.drJacksonPrivateLine", "杰克逊博士 · 私人电话", "Dr. Jackson · Private Line");
    public override string CharacterId => "dr_jackson";
    public override long PhoneNumber => 52746L;
    public override string[] TierNames => System.Array.Empty<string>();
    private static readonly string[] GreetingKeys = { "companies.jacksonContact.greeting.customWorkshop", "companies.jacksonContact.greeting.workbench" };
    public override string[] FrontDesk => L.K(GreetingKeys, Greeting, GreetingEn);
    public override string FrontDeskDone => L.K("companies.jacksonContact.frontDeskDone.gotILlPackUp", "记下了。我收拾好，明天托人给你送过去。", "Got it. I'll pack it up and have someone bring it over tomorrow.");
    public override string CardFlavor => "";
    public override string DeskName => L.K("companies.jacksonContact.deskName.drJackson", "杰克逊博士", "Dr. Jackson");
    public override string DeskAsk => "";
    public override CatalogEntry[] Catalog => Equipment;
}
