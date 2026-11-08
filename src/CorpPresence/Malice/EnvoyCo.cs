namespace CorpPresence.Malice;

internal sealed class EnvoyCo : Company
{
	public override string Id => "envoy";

	public override string Name => L.K("malice.envoy.name.jointDistrictAdministration", "联合站区管理处", "Joint District Administration");

	public override string CharacterId => "envoy";

	public override long PhoneNumber => 77225L;

	public override string[] TierNames => new string[3] { L.K("malice.envoy.tierNames.registered", "登记", "Registered"), L.K("malice.envoy.tierNames.associate", "合作", "Associate"), L.K("malice.envoy.tierNames.partner", "伙伴", "Partner") };

	public override string CardFlavor => "";

	public override string DeskName => L.K("malice.envoy.deskName.zacchaeus", "撒该", "Zacchaeus");

	public override string DeskAsk => "";

	public override string[] FrontDesk => new string[2] { L.K("malice.envoy.frontDesk.zacchaeusThorneGoAhead", "撒该·索恩。请讲。", "Zacchaeus Thorne. Go ahead."), L.K("malice.envoy.frontDesk.jointDistrictAdministrationHowCan", "您好，这里是联合站区管理处。", "Joint District Administration. How can I help?") };

	public override string FrontDeskDone => L.K("malice.envoy.frontDeskDone.noted", "已经记下了。", "Noted.");

	public override bool Active
	{
		get
		{
			if (Settings.MaliceMode)
			{
				EnvoyState envoyState = MaliceState.Get()?.Envoy;
				if (envoyState != null && envoyState.Met)
				{
					return !envoyState.Gone;
				}
				return false;
			}
			return false;
		}
	}
}
