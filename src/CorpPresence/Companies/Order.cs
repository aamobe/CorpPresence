using System.Collections.Generic;

namespace CorpPresence;

internal sealed class Order
{
	internal const string SpentContacts = "@rosen_spent_contact";

	public int Day { get; set; }

	// Native visitor queues are rebuilt on load. Keep the due date while a frame courier is visiting.
	internal int CourierBookedDay = -1;

	public List<string> Items { get; set; } = new List<string>();

	public string Kind { get; set; } = "delivery";

	public int Cash { get; set; }
}
