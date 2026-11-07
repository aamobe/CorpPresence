using System;

namespace CorpPresence;

internal static partial class Perfume
{
    private const string RedYieldTag = "CORP_PERFUME_RED_YIELD_INT";
    private const string BlueYieldTag = "CORP_PERFUME_BLUE_YIELD_INT";
    private const string OilSavingTag = "CORP_PERFUME_OIL_SAVING_INT";
    private const int CreditScale = 500;

    private static int Quality(GameItem machine) => Math.Clamp(MachineryHelper.GetCurrentQualityBonus(machine), 0, 200);
    private static int QualityCredit(GameItem machine) => Quality(machine);
    private static int Credit(GameItem machine, string tag) => Math.Clamp(machine.GetTagReadonly(tag).GetInt(), 0, CreditScale - 1);
    private static int OilCost(GameItem machine) => OilPerMaterialUl - Quality(machine) * 20;
    private static string YieldTag(PerfumeRig rig) => rig.Blue ? BlueYieldTag : RedYieldTag;
    private static int NextYieldCredit(PerfumeRig rig) => Credit(rig.Machine, YieldTag(rig)) + QualityCredit(rig.Machine);
    private static int ProductCount(PerfumeRig rig) => rig.Kind == 1 ? 1 + NextYieldCredit(rig) / CreditScale : 1;
    private static int NextSavingCredit(PerfumeRig rig) => Credit(rig.Machine, OilSavingTag) + QualityCredit(rig.Machine) * rig.OilMaterialsNeeded;
    private static int MaterialCost(PerfumeRig rig) => rig.OilMaterialsNeeded - NextSavingCredit(rig) / CreditScale;

    // Credits only advance after a whole batch is placed, powered and paid for.
    private static void CommitQuality(PerfumeRig rig)
    {
        if (rig.Kind == 1)
            rig.Machine.ModifyTag(YieldTag(rig), s => s.Enable().SetInt(NextYieldCredit(rig) % CreditScale));
        if (rig.Kind == 2)
            rig.Machine.ModifyTag(OilSavingTag, s => s.Enable().SetInt(NextSavingCredit(rig) % CreditScale));
    }

    internal static (string Zh, string En) Effects(int kind) => kind switch
    {
        0 => ("性能：增加每晚处理份数。\n品质：降低每份材料消耗的基底油。品质+100时由10mL降至8mL，最低6mL。\n效率：降低能耗，最低1电。",
            "Performance: increases portions processed per night.\nQuality: reduces base oil used per material. At +100 quality, 10 mL becomes 8 mL; minimum 6 mL.\nEfficiency: reduces power consumption, to a minimum of 1 charge."),
        1 => ("性能：增加每晚提取批次。\n品质：积累额外提取物产量。品质+100时每5批额外产出1份，最高增加40%；两种路线分别累计，读档保留。\n效率：降低能耗，最低1电。",
            "Performance: increases extraction batches per night.\nQuality: accumulates extra extract yield. At +100 quality, every 5 batches produce 1 extra portion, up to +40% yield. Each recipe saves its own progress across loads.\nEfficiency: reduces power consumption, to a minimum of 1 charge."),
        2 => ("性能：增加每晚合成瓶数。\n品质：节省基底油材料。品质+100时每5份基准用量节省1份，累计抵扣，长期最多节省40%；血液提取物照常消耗，读档保留进度。\n效率：降低能耗，最低1电。",
            "Performance: increases bottles made per night.\nQuality: saves base oil materials. At +100 quality, saves 1 of every 5 portions of recipe demand, up to 40%, using saved credit. Blood extract use is unchanged; credit persists across loads.\nEfficiency: reduces power consumption, to a minimum of 1 charge."),
        _ => ("性能：无效果，不增加每日采血次数。\n品质：无效果，不减轻采血伤害。\n效率：降低每次采血能耗，最低1电。",
            "Performance: no effect; daily draw limits do not increase.\nQuality: no effect; blood draw injuries are unchanged.\nEfficiency: reduces power per draw, to a minimum of 1 charge.")
    };

    internal static string EffectText(GameItem machine)
    {
        int kind = Goods.IdOf(machine) switch { ExtractorKey => 0, BloodKey => 1, MixerKey => 2, CollectorKey => 3, _ => -1 };
        if (kind < 0) return null;
        var effect = Effects(kind);
        return LocHelper.BulletLines(L.T(effect.Zh, effect.En));
    }
}
