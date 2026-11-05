using System.Runtime.CompilerServices;

namespace CorpPresence;

internal static class NightSettlement
{
    private sealed class Attempt { public int Day = int.MinValue; }
    private static readonly ConditionalWeakTable<CompanyState, Attempt> Attempts = new();

    // An uncertain debit must not be tried again by a repeated event that night.
    // A newly loaded state gets its own marker; nothing is serialized.
    public static bool TryBegin(CompanyState state, int day)
    {
        Attempt attempt = Attempts.GetValue(state, _ => new Attempt());
        if (attempt.Day == day) return false;
        attempt.Day = day;
        return true;
    }
}
