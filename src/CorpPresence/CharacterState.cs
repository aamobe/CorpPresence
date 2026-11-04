using System.Collections.Generic;
using Newtonsoft.Json;

namespace CorpPresence;

internal sealed class CharacterState
{
	[JsonIgnore]
	public int QueuedDay = -1;

	public int Chapter { get; set; }

	public int LastVisitDay { get; set; } = -999;

	public int LastStoryDay { get; set; } = -999;

	public int PendingDay { get; set; } = -1;

	public string PendingKind { get; set; }

	public int NextDailyDay { get; set; }

	public int DailyCount { get; set; }

	public List<int> SideDone { get; set; } = new List<int>();

	public string Order { get; set; }
}
