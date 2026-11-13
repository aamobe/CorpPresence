using System;

namespace CorpPresence;

internal static class PowderPricing
{
    internal const int EmptyBoxValue = 20;
    private static readonly int[] PricePerHundred = { 24, 18, 40, 32, 6 };

    internal static long BoxValue(ReadOnlySpan<int> parts)
    {
        long weightedPoints = 0;
        for (int i = 0; i < PricePerHundred.Length; i++)
            weightedPoints += (long)parts[i] * PricePerHundred[i];

        int percent = PowderClassification.FromParts(parts).Grade switch
        {
            PowderGrade.Mixed => 75,
            PowderGrade.Pure => 120,
            _ => 100
        };
        return EmptyBoxValue + (weightedPoints * percent + 5000) / 10000;
    }
}
