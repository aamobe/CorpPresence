using System.Collections.Generic;

namespace CorpPresence.Malice;

internal sealed class EnvoyState
{
	public bool Met { get; set; }

	public bool Gone { get; set; }

	public int RichDay { get; set; } = -1;

	public string Aid { get; set; } = "";

	public int AidStart { get; set; } = -1;

	public int DeclineUntil { get; set; } = -1;

	public int RepayLeft { get; set; }

	public int RepayDue { get; set; } = -1;

	public int LateWeeks { get; set; }

	public bool Medal { get; set; }

	public string Ch2 { get; set; } = "";
	public bool BombIssued { get; set; }

	public int BombDeadline { get; set; } = -1;

	public int BombPauseDay { get; set; } = -1;

	public int BombSoldDay { get; set; } = -1;

	public int RetaliateFrom { get; set; } = -1;

	public int RetaliateUntil { get; set; } = -1;

	public bool OdinUnderstood { get; set; }

	public bool SubFree { get; set; }

	public int ChannelUntil { get; set; } = -1;

	public int ChannelDue { get; set; } = -1;

	public int ChannelFee { get; set; }

	public string Ch3 { get; set; } = "";

	public bool RebelCut { get; set; }

	public bool BribedEnvoy { get; set; }

	public bool ReconciledRebels { get; set; }

	public int SubsidyDue { get; set; } = -1;

	public string Ch4 { get; set; } = "";

	public List<string> Coins { get; set; } = new List<string>();

	public bool Agent { get; set; }

	public List<EnvoyVisit> Visits { get; set; } = new List<EnvoyVisit>();

	public int CutDay { get; set; } = -1;

	public int BoostDay { get; set; } = -1;
}
