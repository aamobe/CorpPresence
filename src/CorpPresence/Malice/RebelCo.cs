using System.Linq;

namespace CorpPresence.Malice;

internal sealed class RebelCo : Company
{
	private static readonly string[] FrontItems = new string[2] { "@mutual_medkit", "@freedom_walkman" };

	public static readonly CatalogEntry[] Entries = SpacerCo.Entries.Where((CatalogEntry e) => e.Page == "军械" || FrontItems.Contains(e.Id)).ToArray();

	public static readonly string[] Factions = new string[2] { "FACTION_REVOLUTION", "FACTION_LOWER_LEVEL" };

	protected override double OrderPriceRate(CatalogEntry entry, CompanyState state) => entry.Id == SpacerCo.ModBox ? 1.0 : base.OrderPriceRate(entry, state);

	public override string Id => "rebel";

	public override string Name => L.K("malice.rebel.name.resistance", "反抗军", "The Resistance");

	public override string CharacterId => "odin";

	public override long PhoneNumber => 77224L;

	public override string[] TierNames => new string[3] { L.K("malice.rebel.tierNames.sympathizer", "外围", "Sympathizer"), L.K("malice.rebel.tierNames.comrade", "同志", "Comrade"), L.K("malice.rebel.tierNames.cadre", "骨干", "Cadre") };

	public override string CardFlavor => "";

	public override string DeskName => L.K("malice.rebel.deskName.odin", "奥丁", "Odin");

	public override string DeskAsk => "";

	public override string[] FrontDesk => new string[2] { L.K("malice.rebel.frontDesk.sMe", "是我。", "It's me."), L.K("malice.rebel.frontDesk.makeQuick", "长话短说。", "Make it quick.") };

	public override string FrontDeskDone => L.K("malice.rebel.frontDeskDone.someoneWillBring", "会有人送过去。", "Someone will bring it.");

	public override bool Active => Settings.MaliceMode;

	public override CatalogEntry[] Catalog => Entries;
}
