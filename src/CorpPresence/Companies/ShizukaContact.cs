namespace CorpPresence;

internal sealed class ShizukaContact : WorkshopContact
{
    public override string Id => "cartel_supplies";
    public override string CharacterId => CartelShizuka.CharacterId;
    public override long PhoneNumber => 23611L;
    public override bool Active => CartelShizuka.Introduced(CorpState.Get());
    public override string Name => L.K("workshop.shizuka.contact", "试料与补订", "Samples and resupply");
    public override string DeskName => L.K("workshop.shizuka.name", "黑羽静", "Shizuka Kurobane");
    public override string[] FrontDesk => new[] { L.K("workshop.shizuka.greeting", "黑羽静。要领试料，还是来交货？", "Shizuka here. Are you calling for samples or do you have a delivery?") };
    public override string FrontDeskDone => L.K("workshop.shizuka.ordered", "补订单收到了。明天送到你那侧柜台。", "I have your order. It will reach your side of the counter tomorrow.");
    public override CatalogEntry[] Catalog => ShizukaSupplies.All;
    protected override string SupplyError(string id, CorpState root) => ShizukaSupplies.OrderError(id, root);
}
