using System.Linq;

namespace CorpPresence.Malice;

internal sealed class RebelCo : Company
{
	private static readonly string[] FrontItems = new string[2] { "@mutual_medkit", "@freedom_walkman" };

	public static readonly CatalogEntry[] Entries = SpacerCo.Entries.Where((CatalogEntry e) => e.Page == "军械" || FrontItems.Contains(e.Id)).ToArray();

	public static readonly string[] Factions = new string[2] { "FACTION_REVOLUTION", "FACTION_LOWER_LEVEL" };

	public override string Id => "rebel";

	public override string Name => L.T("反抗军", "The Resistance");

	public override string CharacterId => "odin";

	public override long PhoneNumber => 77224L;

	public override string[] TierNames => L.T(new string[3] { "外围", "同志", "骨干" }, new string[3] { "Sympathizer", "Comrade", "Cadre" });

	public override string CardFlavor => "";

	public override string DeskName => L.T("奥丁", "Odin");

	public override string DeskAsk => "";

	public override string[] FrontDesk => L.T(new string[2] { "是我。", "长话短说。" }, new string[2] { "It's me.", "Make it quick." });

	public override string FrontDeskDone => L.T("会有人送过去。", "Someone will bring it.");

	public override bool Active => Settings.MaliceMode;

	public override CatalogEntry[] Catalog => Entries;
}
