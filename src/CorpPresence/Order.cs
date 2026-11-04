using System.Collections.Generic;

namespace CorpPresence;

internal sealed class Order
{
	public int Day { get; set; }

	public List<string> Items { get; set; } = new List<string>();

	public string Kind { get; set; } = "delivery";

	public int Cash { get; set; }
}
