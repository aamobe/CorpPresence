namespace CorpPresence;

internal sealed class CenContact : WorkshopContact
{
    public override string Id => "lower_mutual";
    public override string CharacterId => LowerMachinist.CharacterId;
    public override long PhoneNumber => 23610L;
    public override bool Active => LowerMachinist.StarterUnlocked(CorpState.Get());
    public override string Name => L.K("workshop.cen.contact", "下层互助", "Lower Levels Mutual Aid");
    public override string DeskName => L.K("workshop.cen.name", "阿岑", "Cen");
    public override string[] FrontDesk => new[] { L.K("workshop.cen.greeting", "阿岑接的。要领开工料、订一筐料，还是手上有货要出？", "Cen here. Calling for your starter supplies, a basket of feedstock, or a sale?") };
    public override string FrontDeskDone => L.K("workshop.cen.ordered", "单子记下了，明天送到你一侧的柜台。", "Order logged. Delivery to your side of the counter tomorrow.");
    public override CatalogEntry[] Catalog => CenSupplies.All;

    protected override string SupplyError(string id, CorpState root) =>
        (id is CenSupplies.Pump or CenSupplies.Classifier or CenSupplies.LubricantOrder or CenSupplies.FeederOrder or CenSupplies.ScrewdriverOrder)
        && !LowerMachinist.MineralizerUnlocked(root) ?
            L.K("workshop.cen.pumpLocked", "阿岑二访成交后供应", "Available after Cen’s second sale") : null;
}
