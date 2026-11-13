using System;

namespace CorpPresence;

internal static class PowderSieveRules
{
    internal const int BinCapacity = 1500;
    internal const int BaseThroughput = 300;
    internal static int RecoveryRate(int quality) => 50 + Math.Clamp(quality, 0, 35);
    internal static int Recovered(int amount, int rate) => amount * rate / 100;
}
