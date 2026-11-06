using System.Collections.Generic;

namespace CorpPresence;

internal sealed class Visitor
{
	public int Day { get; set; }

	public string Kind { get; set; }

	public List<string> LabItems { get; set; }
}
