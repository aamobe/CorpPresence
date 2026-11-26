using System;

namespace CorpPresence;

internal static partial class SinteringStation
{
    private static MachineryStack TreatmentEditor(Func<GameItem> owner, GameSlotInventory solid, GameSlotInventory battery,
        Func<int> power, Action updated, out Action refresh)
    {
        var detail = new MachineryText(uppercase: false);
        var cancel = new MachineryButton(L.T("取消排程", "Cancel treatment"));

        MachineryButton Button(RodProcess process)
        {
            var button = new MachineryButton(RodProcessName(process));
            return button.SetCallback(() => WorkshopCycle.Run(owner(), () =>
            {
                RodTreatmentJob.TryQueue(owner(), solid.childItem, process);
                updated();
            }));
        }
        cancel.SetCallback(() => WorkshopCycle.Run(owner(), () =>
        {
            RodTreatmentJob.Clear(owner());
            updated();
        }));

        refresh = () =>
        {
            if (owner() == null) return;
            if (!LowerMachinist.MineralizerUnlocked(CorpState.Current))
            {
                detail.SetText(L.T("阿岑二访设备成交后开放组织处理", "Purchase Cen's equipment on the second visit to unlock treatment"));
                return;
            }
            var current = RodCondition.Read(solid.childItem);
            var queued = RodTreatmentJob.Read(owner());
            string hint = current == null ? L.T("把一根有档案的棒放进固体料槽", "Put one rod with a material record in the solid feed slot")
                : current.Jacket != null ? L.K("workshop.clad.treat", "芯和外套须在包合前分别处理。", "Treat the core and jacket separately before cladding.")
                : current.Process != RodProcess.Cast ? L.T("这根棒已完成一道处理", "This rod has already received one treatment")
                : L.T("每根只选一道。定向需要导电 ≥60。", "Choose one treatment per rod. Alignment requires conductivity ≥60.");
            string readings = current == null ? "" : "\n" + ExplainRod(current);
            if (queued != RodProcess.Cast)
            {
                var next = RodCondition.Preview(solid.childItem, queued);
                hint = L.T("待加工 · ", "Queued · ") + RodProcessName(queued);
                hint += next == null ? L.T(" · 当前棒料不合格", " · current rod ineligible")
                    : RodTreatmentJob.Ready(owner(), solid.childItem, battery, power()) ? L.T(" · 可过夜", " · ready overnight")
                    : L.T(" · 检查供电", " · check power");
                if (next != null) readings += "\n" + L.T("预计 · ", "Expected · ") + ExplainRod(next);
            }
            detail.SetText(hint + readings);
        };
        return MachineryStack.Vertical(Button(RodProcess.Temper), Button(RodProcess.Densify), Button(RodProcess.Align),
            Button(RodProcess.Harden), cancel, detail);
    }

    internal static string RodProcessName(RodProcess process) => process switch
    {
        RodProcess.Cast => L.T("铸态", "As cast"),
        RodProcess.Temper => L.T("回火", "Temper"),
        RodProcess.Densify => L.T("真空致密", "Vacuum densify"),
        RodProcess.Align => L.T("定向", "Align"),
        RodProcess.Harden => L.T("表面硬化", "Case harden"),
        _ => L.T("未知工艺", "Unknown process")
    };

    internal static string ExplainRod(RodCondition rod)
    {
        var core = rod.Core;
        var surface = rod.Surface;
        string readout = L.T($"承力 {core.Load} · 耐磨 {surface.Wear} · 导电 {core.Conductivity} · 耐蚀 {surface.Corrosion}\n脆裂 {surface.Brittleness} · 孔隙 {surface.Porosity} · 硬度 {surface.Hardness}",
            $"Load {core.Load} · Wear {surface.Wear} · Conductivity {core.Conductivity} · Corrosion {surface.Corrosion}\nBrittleness {surface.Brittleness} · Porosity {surface.Porosity} · Hardness {surface.Hardness}");
        if (rod.Core != rod.Surface)
            readout += "\n" + L.T($"芯部 · 耐磨 {core.Wear} · 脆裂 {core.Brittleness} · 硬度 {core.Hardness}",
                $"Core · Wear {core.Wear} · Brittleness {core.Brittleness} · Hardness {core.Hardness}");
        if (rod.AxialConductivity is int axial)
            readout += "\n" + L.T($"沿轴导电 {axial}", $"Axial conductivity {axial}");
        return RodProcessName(rod.Process) + "\n" + readout;
    }
}
