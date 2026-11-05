namespace CorpPresence.Malice;

internal sealed class RebelState
{
	public int Done { get; set; }

	public int Active { get; set; }

	public bool Ch1Poison { get; set; }

	public int Ch1Value { get; set; }

	public string Ch2Route { get; set; } = "";

	public int Ch2BombDay { get; set; } = -1;

	public int Ch2FenceWeapons { get; set; }

	public int Ch3Start { get; set; } = -1;

	public int Ch3Medical { get; set; }

	public int OdinBuyDue { get; set; } = -1;

	public bool Invited { get; set; }

	public bool Joined { get; set; }

	public int Ch4Sales { get; set; }

	public int RaidDay { get; set; } = -1;

	public bool Appeased { get; set; }

	public bool Ch5 { get; set; }

	public bool RepCert { get; set; }

	public int OdinRepLost { get; set; }

	public int RescueValue { get; set; }

	public int BoostDay { get; set; } = -1;
}
