namespace CorpPresence;

// Values are internal only: never assigned to a vanilla condition or save tag.
internal enum WaterGrade { Unknown = -1, Pure, High, Base, Ghost, Rust, Gutter }
internal static class WaterGrades
{
    private static readonly string[] Keys = { "corp.water.pure", "corp.water.high", "corp.water.base", "corp.water.ghost", "corp.water.rust", "corp.water.gutter" };
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
    public static string Key(WaterGrade grade) => grade >= WaterGrade.Pure && grade <= WaterGrade.Gutter ? Keys[(int)grade] : "";
    public static WaterGrade FromKey(string key)
    {
        for (int i = 0; i < Keys.Length; i++) if (Keys[i] == key) return (WaterGrade)i;
        return WaterGrade.Unknown;
    }
    public static string FromField(string name) => name switch
    {
        "PURE_WATER_DISPLAY" => Keys[0], "HIGH_QUALITY_WATER_DISPLAY" => Keys[1],
        "BASE_WATER_DISPLAY" => Keys[2], "GHOSTWATER_DISPLAY" => Keys[3],
        "RUST_WATER_DISPLAY" => Keys[4], "GUTTERFLOW_WATER_DISPLAY" => Keys[5], _ => null
    };
}
