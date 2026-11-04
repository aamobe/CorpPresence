namespace CorpPresence.Malice;

internal sealed class EnvoyVisit
{
	public string Kind { get; set; }

	public int Day { get; set; }

	public int QueuedDay { get; set; } = -1;
}
