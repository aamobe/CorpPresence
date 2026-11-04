using System.Runtime.CompilerServices;

namespace CorpPresence;

internal static class NightSettlement
{
	private sealed class Attempt
	{
		public int Day = int.MinValue;
	}

	private static readonly ConditionalWeakTable<CompanyState, Attempt> Attempts = new ConditionalWeakTable<CompanyState, Attempt>();

	public static bool TryBegin(CompanyState state, int day)
	{
		Attempt value = Attempts.GetValue(state, (CompanyState _) => new Attempt());
		if (value.Day == day)
		{
			return false;
		}
		value.Day = day;
		return true;
	}
}
