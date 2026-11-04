namespace CorpPresence.Malice;

internal sealed class CorpTerms
{
	public bool Subscribed { get; set; }

	public int SubUntil { get; set; } = -1;

	public bool AutoRenew { get; set; }

	public int Unlocked { get; set; } = 1;

	public int Vouchers { get; set; }

	public int RemindedDay { get; set; } = -1;
}
