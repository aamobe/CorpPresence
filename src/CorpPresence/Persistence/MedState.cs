namespace CorpPresence;

internal sealed class MedState
{
	public int StaminaShots { get; set; }

	public int PainBlockDay { get; set; } = -1;

	public int PatchUntil { get; set; } = -1;

	public int ElixirUntil { get; set; } = -1;

	public bool MaskReady { get; set; }

	public int CandleUntil { get; set; } = -1;

	public int RejuvShots { get; set; }

	public bool SentinelCured { get; set; }
}
