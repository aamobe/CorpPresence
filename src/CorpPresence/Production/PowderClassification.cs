using System;

namespace CorpPresence;

// The order matches the persisted component point array in PowderBox.
internal enum PowderComponent { Metal, Silica, RareMetal, Conductive, Residue }

internal enum PowderGrade { Empty, Mixed, Coarse, Pure }

internal readonly struct PowderClassification
{
    internal PowderGrade Grade { get; }
    internal PowderComponent? Branch { get; }
    internal int PurityBasisPoints { get; }

    private PowderClassification(PowderGrade grade, PowderComponent? branch, int purityBasisPoints)
    {
        Grade = grade;
        Branch = branch;
        PurityBasisPoints = purityBasisPoints;
    }

    internal static PowderClassification FromParts(ReadOnlySpan<int> parts)
    {
        long total = 0;
        int largest = 0;
        int dominant = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            total += parts[i];
            if (parts[i] <= largest) continue;
            largest = parts[i];
            dominant = i;
        }

        if (total == 0) return default;
        int purity = (int)(largest * 10000L / total);
        if (purity < 8100) return new(PowderGrade.Mixed, null, purity);
        return new(purity >= 9900 ? PowderGrade.Pure : PowderGrade.Coarse, (PowderComponent)dominant, purity);
    }
}
