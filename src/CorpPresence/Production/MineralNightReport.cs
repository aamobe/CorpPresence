using System;

namespace CorpPresence;

internal enum MineralStop
{
    Ready, NoFeed, NoBox, NoPower, Full, NoRecovery, Insufficient, Carry, Locked, SieveParts, SieveSettled,
    CrusherTeeth, CrusherLining, CrusherSettled
}

internal static class MineralNightReport
{
    internal const string Crusher = "CORP_CRUSHER_LAST";
    internal const string Sieve = "CORP_SIEVE_LAST";
    internal const string Sinter = "CORP_SINTERING_LAST";
    internal const string RodTreatmentPrefix = "rod_treatment/";

    internal static string Order => L.K("workshop.machine.order", "金属／硅质／稀有／导电／渣", "Metal / silica / rare / conductive / residue");
    internal static string Vector(int[] values) => values == null ? "0/0/0/0/0" : string.Join("/", values);

    internal static string Reason(MineralStop stop, bool returning = false) => stop switch
    {
        MineralStop.NoFeed when returning => L.K("workshop.sieve.emptyReturn", "回收仓没有可筛尾粉", "No tailings to sieve"),
        MineralStop.NoFeed => L.K("workshop.machine.feed", "放入可加工的原料", "Insert processable feed"),
        MineralStop.NoBox => L.K("workshop.machine.box", "放入自己的接粉盒", "Insert an owned powder box"),
        MineralStop.NoPower => L.K("production.sintering.noPower", "电量不足", "Insufficient power"),
        MineralStop.Full => L.K("workshop.machine.full", "腾出足以容纳整批的出料空间", "Make room for the whole batch"),
        MineralStop.NoRecovery when returning => L.K("workshop.sieve.zeroReturn", "预计回收 0 点。取消回筛后可先筛新料。", "Expected recovery is zero. Cancel to sieve new feed first."),
        MineralStop.NoRecovery => L.K("workshop.machine.combine", "五种成分均回收 0 点，留在进料盒。请合批后再筛。", "All five recoveries round to zero. Feed stays in the box. Combine it with more powder."),
        MineralStop.Insufficient => L.K("production.sintering.missing", "粉量或纯度不足", "Insufficient powder or purity"),
        MineralStop.Carry => L.K("workshop.machine.carry", "已累计产能，等待下一夜", "Work accumulated for the next night"),
        MineralStop.Locked => L.K("workshop.machine.locked", "先购得阿岑的首套设备", "Buy Cen’s starter equipment first"),
        MineralStop.SieveParts => L.K("workshop.sieve.returnParts", "回筛需细筛和回料导槽", "Fit a fine screen and return guide"),
        MineralStop.SieveSettled => L.K("workshop.sieve.returnSettled", "本夜回筛已结算", "Tailings already sieved this night"),
        MineralStop.CrusherTeeth => L.K("workshop.crusher.needRoller", "硬料档需装上齿面完好的组合齿辊", "Hard duty needs a roller with its toothed face intact"),
        MineralStop.CrusherLining => L.K("workshop.crusher.needLining", "放入旧炉衬供硬料档加工", "Load spent linings for hard duty"),
        MineralStop.CrusherSettled => L.K("workshop.crusher.hardSettled", "本夜硬料已结算", "Hard duty already settled this night"),
        _ => L.K("workshop.machine.ready", "可加工", "Ready")
    };

    internal static void Save(GameItem machine, string tag, MineralStop reason, int count = 0, int power = 0,
        int[] input = null, int[] output = null, int[] tailings = null, string recipe = "")
    {
        string record = string.Join("|", (int)reason, count, power, Vector(input), Vector(output), Vector(tailings), recipe);
        machine.ModifyTag(tag, state => state.Enable().SetString(record), skipValidate: true);
    }

    internal static MachineryButton Button(Func<GameItem> machine, string tag)
    {
        var button = new MachineryButton(L.K("production.canning.buildCanner.lastCycle", "最近结算", "Last cycle"));
        return button.SetCallback(() =>
        {
            string[] fields = machine()?.GetTagReadonly(tag).GetString()?.Split('|');
            string message = L.K("workshop.machine.noRecord", "还没有上一夜的记录", "No previous night recorded");
            if (fields?.Length == 7 && int.TryParse(fields[0], out int stop) && Enum.IsDefined(typeof(MineralStop), stop))
            {
                bool returning = tag == Sieve && fields[6] == PowderSieve.TailingsRecord;
                message = Reason((MineralStop)stop, returning) + "\n" + L.F("workshop.machine.usedPower", "实际耗电 {0}", "Charge used {0}", fields[2]);
                if (tag == Sinter && fields[6].StartsWith(RodTreatmentPrefix, StringComparison.Ordinal))
                {
                    string process = SinteringStation.RodProcessName(RodCondition.Decode(fields[6].Substring(RodTreatmentPrefix.Length)));
                    message += "\n" + (fields[1] == "0" ? L.T("组织处理未完成 · ", "Treatment pending · ") :
                        L.T("已原位处理 1 根棒 · ", "Treated one rod in place · ")) + process;
                }
                else if (fields[1] != "0")
                {
                    string detail = tag == Crusher
                        ? L.F("workshop.machine.crushed", "粉盒收到 {0} 点\n投料件数（垃圾／废金属／玻璃／电子残料／炉衬） {1}", "To box {0} points\nItems (junk / scrap / glass / electronics / lining) {1}", fields[4], fields[3])
                        : tag == Sieve
                            ? L.F("workshop.machine.sieved", "投入 {0}\n分类仓 +{1}\n回收仓 +{2}", "Feed {0}\nSorted bins +{1}\nTailings +{2}", fields[3], fields[4], fields[5])
                            : L.F("workshop.machine.sintered", "烧结用粉 {0}\n产出 {1} × {2}", "Powder used {0}\nProduced {1} × {2}", fields[3], SinteringStation.RecipeName(SinteringRecipes.All[SinteringRecipes.IndexOf(fields[6])]), fields[1]);
                    if (tag == Crusher && fields[6] == MineralCrusher.HardLiningReceipt)
                        detail = L.F("workshop.crusher.hardReceipt", "硬料档 · 旧炉衬 {0} 件\n粉盒 +{1}",
                            "Hard duty · {0} spent linings\nPowder box +{1}", fields[1], fields[4]);
                    else if (tag == Crusher && fields[3].Split('/').Length == 7)
                        detail = L.F("workshop.industrial.crushed", "投料件数（垃圾／废金属／玻璃／电子残料／炉衬／报废触点／合金坯） {0}\n粉盒 +{1} · 成品格合金粉 +{2}",
                            "Items (junk / scrap / glass / electronics / lining / spent contacts / billets) {0}\nPowder box +{1} · alloy bags +{2}", fields[3], fields[4], fields[6]);
                    else if (tag == Crusher && fields[3].Split('/').Length == 8)
                        detail = L.F("workshop.industrial.crushedWithOffcuts", "投料件数（垃圾／废金属／玻璃／电子残料／炉衬／报废触点／合金坯／边料） {0}\n粉盒 +{1} · 成品格合金粉 +{2}",
                            "Items (junk / scrap / glass / electronics / lining / spent contacts / billets / offcuts) {0}\nPowder box +{1} · alloy bags +{2}", fields[3], fields[4], fields[6]);
                    else if (returning)
                        detail = L.F("workshop.sieve.returnReceipt", "回筛投入 {0}\n分类仓 +{1}\n回收仓 −{2}",
                            "Tailings processed {0}\nSorted bins +{1}\nTailings −{2}", fields[3], fields[4], fields[5]);
                    else if (tag == Sinter && fields[5] != "0/0/0/0/0")
                        detail += "\n" + L.F("workshop.sinter.trimReceipt", "飞散 {0}", "Casting loss {0}", fields[5]);
                    message += "\n" + Order + "\n" + detail;
                }
            }
            var ui = GenericConfirmationUIManager.Instance;
            ui?.OpenPanelSingleButton(message, L.K("production.canning.buildCanner.oK", "确定", "OK"), () => ui.ClosePanel());
        });
    }
}
