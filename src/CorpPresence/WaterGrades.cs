namespace CorpPresence;

// Values are internal only: never assigned to a vanilla condition or save tag.
internal enum WaterGrade { Unknown = -1, Pure, High, Base, Ghost, Rust, Gutter }
internal static class WaterGrades
{
    public static WaterGrade Of(int purity)
    {
        if (purity < 3500 || purity > 10000) return WaterGrade.Unknown;
        if (purity >= 9990) return WaterGrade.Pure;
        if (purity >= 9900) return WaterGrade.High;
        if (purity >= 9800) return WaterGrade.Base;
        if (purity >= 9600) return WaterGrade.Ghost;
        if (purity >= 9000) return WaterGrade.Rust;
        return WaterGrade.Gutter;
    }
}
