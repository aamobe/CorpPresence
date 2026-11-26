using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static partial class SinteringStation
{
    private static MachineryStack TrialEditor(Func<GameItem> owner, GameSlotInventory[] inputs, Func<bool> writable,
        Func<CastingSetup> settings, Action updated, Action selectTrial, out Action reload, out Action refresh)
    {
        var sliders = new MachinerySlider[4];
        var labels = new MachineryText[4];
        var detail = new MachineryText(uppercase: false);
        var apply = new MachineryButton(L.T("确认配比并选用试配模", "Apply blend and select trial mold"));
        var trim = new MachineryButton();
        bool loading = false;

        int[] Draft()
        {
            var blend = new int[5];
            blend[0] = TrialAlloy.BatchPoints;
            for (int i = 0; i < sliders.Length; i++)
            {
                blend[i + 1] = sliders[i].GetInt();
                blend[0] -= blend[i + 1];
            }
            return blend;
        }

        void Refresh()
        {
            if (loading || owner() == null) return;
            var blend = Draft();
            var setup = settings();
            trim.SetText(!MachineService.Handbook ? L.K("workshop.sinter.trimLocked", "近配模式 · 需旧机检修图册", "Near-fit casting · repair handbook required") :
                setup.Trim ? L.K("workshop.sinter.trimOn", "近配模式 · 开", "Near-fit casting · On") :
                L.K("workshop.sinter.trimOff", "近配模式 · 关", "Near-fit casting · Off"));
            for (int i = 0; i < labels.Length; i++)
                labels[i].SetText(PowderBox.ComponentName((PowderComponent)(i + 1)) + " " + blend[i + 1]);
            if (!LowerMachinist.MineralizerUnlocked(CorpState.Current))
            {
                detail.SetText(L.T("阿岑二访设备成交后可试配", "Purchase Cen's equipment on the second visit to unlock trial blends"));
                return;
            }
            if (!TrialAlloy.Valid(blend))
            {
                detail.SetText(L.T($"金属余 {blend[0]} 点，至少需 55 点；渣至多 10 点",
                    $"Metal remaining: {blend[0]} points. At least 55 metal and at most 10 residue."));
                return;
            }
            var boxes = ReadBoxes(inputs);
            var plan = TrialAlloy.Plan(boxes, blend, out var missing, setup.Trim, setup.Retention);
            var stats = RodMaterial.ForOutput(blend, 2).CastProperties;
            string availability = missing == null ? L.T("请放入自己的粉盒", "Use owned powder boxes")
                : Array.Exists(missing, amount => amount != 0)
                    ? L.T("缺料 " + MineralNightReport.Vector(missing), "Shortfall " + MineralNightReport.Vector(missing))
                    : plan == null ? L.T("按现有粉盒比例凑不齐，请分装或回筛", "Box proportions cannot match this blend. Repack or rescreen the powder.")
                    : PlannedDraws(boxes, plan);
            if (plan?.Cast != null)
                availability += "\n" + L.F("workshop.sinter.trimForecast", "本炉飞散 {0} 点 · 原盒留回 {1} 点",
                    "Casting loses {0} points · {1} retained in source boxes", plan.Total - TrialAlloy.BatchPoints, plan.Retained);
            detail.SetText(MineralNightReport.Order + "\n" + MineralNightReport.Vector(blend) + " · " + availability + "\n" +
                L.T($"承力 {stats.Load} · 耐磨 {stats.Wear} · 导电 {stats.Conductivity} · 耐蚀 {stats.Corrosion}\n" +
                    $"脆裂 {stats.Brittleness} · 孔隙 {stats.Porosity} · 硬度 {stats.Hardness}",
                    $"Load {stats.Load} · Wear {stats.Wear} · Conductivity {stats.Conductivity} · Corrosion {stats.Corrosion}\n" +
                    $"Brittleness {stats.Brittleness} · Porosity {stats.Porosity} · Hardness {stats.Hardness}"));
        }

        var rows = new List<MachineryStack>();
        var defaults = TrialAlloy.DefaultBlend();
        for (int i = 0; i < sliders.Length; i++)
        {
            labels[i] = new MachineryText(uppercase: false);
            int limit = i == 3 ? TrialAlloy.MaxResidue : TrialAlloy.BatchPoints - TrialAlloy.MinMetal;
            sliders[i] = new MachinerySlider(defaults[i + 1]).SetRange(0, limit, 1);
            sliders[i].onChanged += _ => Refresh();
            rows.Add(MachineryStack.Horizontal(labels[i], sliders[i]));
        }

        apply.SetCallback(() => HookGuard.Run("确认烧结试配", () =>
        {
            var blend = Draft();
            if (!writable() || !LowerMachinist.MineralizerUnlocked(CorpState.Current) || !TrialAlloy.Valid(blend)) return;
            TrialAlloy.Save(owner(), blend);
            selectTrial();
        }));
        trim.SetCallback(() => WorkshopCycle.Run(owner(), () =>
        {
            if (!MachineService.Handbook || !writable()) return;
            var machine = owner();
            bool enabled = machine.GetTagReadonly(TrimTag).GetBool();
            machine.ModifyTag(TrimTag, tag => tag.Enable().SetBool(!enabled), skipValidate: true);
            updated();
        }));
        reload = () =>
        {
            loading = true;
            try
            {
                var blend = TrialAlloy.Read(owner()) ?? TrialAlloy.DefaultBlend();
                for (int i = 0; i < sliders.Length; i++) sliders[i].SetInt(blend[i + 1]);
            }
            finally { loading = false; }
        };
        refresh = Refresh;
        return MachineryStack.Vertical(rows[0], rows[1], rows[2], rows[3], detail, apply, trim);
    }

    private static string PlannedDraws(int[][] boxes, SinteringBatch plan)
    {
        var amounts = new int[boxes.Length];
        for (int slot = 0; slot < boxes.Length; slot++)
            for (int part = 0; part < boxes[slot].Length; part++)
                amounts[slot] += boxes[slot][part] - plan.Remaining[slot][part];
        string bySlot = string.Join("/", amounts);
        return L.T("各盒取料 " + bySlot, "Draw per box " + bySlot);
    }
}
