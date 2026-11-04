namespace CorpPresence.Malice;

internal sealed class EnvoyCo : Company
{
	public override string Id => "envoy";

	public override string Name => L.T("联合站区管理处", "Joint District Administration");

	public override string CharacterId => "envoy";

	public override long PhoneNumber => 77225L;

	public override string[] TierNames => L.T(new string[3] { "登记", "合作", "伙伴" }, new string[3] { "Registered", "Associate", "Partner" });

	public override string CardFlavor => "";

	public override string DeskName => L.T("撒该", "Zacchaeus");

	public override string DeskAsk => "";

	public override string[] FrontDesk => L.T(new string[2] { "撒该·索恩。请讲。", "您好，这里是联合站区管理处。" }, new string[2] { "Zacchaeus Thorne. Go ahead.", "Joint District Administration. How can I help?" });

	public override string FrontDeskDone => L.T("已经记下了。", "Noted.");

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
