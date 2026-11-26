using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
    private static Vector2 _pumpRepairScroll;
    private static int? _pumpRepairQuote;
    private static bool _pumpRepairBuilt;
    private static List<GameItem> _pumpCandidates = new();
    private static int _pumpCandidateIndex;

    private static void ResetPumpRepairSelection()
    {
        _pumpRepairBuilt = false;
        _pumpRepairQuote = null;
        _pumpCandidates.Clear();
        _pumpCandidateIndex = 0;
    }

    private static void RefreshBuiltPumps()
    {
        _pumpCandidates = EmporiumEntry.Instance?.GetAllOwnedItems(includeAfterhour: false)
            ?.Where(WaterStationRepair.EligibleBuiltPump).Distinct().ToList() ?? new();
        _pumpCandidateIndex = 0;
        _pumpRepairQuote = null;
    }

    private static void DrawPumpRepair(Rect area)
    {
        float w = area.width - 22f;
        var workshop = CorpState.Current?.Workshop;
        float height = workshop?.PhaseSite is { Route: PhaseRoute.Whole, CargoId: > 0 } ? 590
            : workshop?.PumpRepair?.CompletedDay.HasValue == true ? 505 : 342;
        _pumpRepairScroll = Ui.BeginPanelScrollView(area, _pumpRepairScroll, new Rect(0, 0, w, height));
        var previousOrigin = Ui.Origin;
        var previousClip = Ui.Clip;
        Ui.Origin = area.position - _pumpRepairScroll;
        Ui.Clip = new Rect(area.x, area.y, w, area.height);
        try { DrawPumpRepairContents(w); }
        finally
        {
            Ui.Origin = previousOrigin;
            Ui.Clip = previousClip;
            GUI.EndScrollView();
        }
    }

    private static void DrawPumpRepairContents(float w)
    {
        var root = CorpState.Current;
        var receipt = root?.Workshop?.PumpRepair;
        GUI.Label(new Rect(0, 0, w, 30), _pumpRepairBuilt || receipt?.ReceivedDay.HasValue == true
            ? L.K("workshop.pumpRepair.intakeTitle", "下层水站 · 整机验收", "Lower water station · Pump acceptance")
            : L.K("workshop.pumpRepair.title", "下层水站 · 旧泵返修", "Lower water station · Pump repair"), _theme.Body);
        if (receipt == null && !WaterStationRepair.Unlocked(root))
        {
            GUI.Label(new Rect(0, 33, w, 70), L.K("workshop.pumpRepair.locked", "结清水站大修并取得检修图册后，罗森在此交接旧泵。", "Finish the water station overhaul and obtain the service handbook. Rosen will hand over the old pump here."), _theme.Wrap);
            return;
        }
        if (receipt?.ReceivedDay is int)
        {
            GUI.Label(new Rect(0, 35, w, 38), L.F("workshop.pumpRepair.receipt", "整机已收 · 实付 {0}/{1}₵", "Pump received · Paid {0}/{1}₵", receipt.Paid, receipt.Amount), _theme.Wrap);
            if (receipt.CompletedDay.HasValue)
            {
                DrawCoreIntake(w, root.Workshop.CoreIntake);
                DrawPhaseSite(w, root.Workshop.PhaseSite);
                if (root.Workshop.PhaseSite is { Route: PhaseRoute.Whole, CargoId: > 0 })
                    DrawJointWaterWork(w, root.Workshop.JointWaterWork);
            }
            else if (Ui.Button(new Rect(0, 76, w, 36), L.K("workshop.pumpRepair.balance", "续结余款", "Settle the balance"), _theme.BtnStyle))
                ShowPumpRepairResult(WaterStationRepair.Submit(), built: receipt.FabricatedPumpId.HasValue);
            return;
        }

        float half = (w - 8f) / 2;
        if (Ui.Button(new Rect(0, 32, half, 32), L.K("workshop.pumpRepair.loanTab", "旧泵返修", "Repair loan"), _pumpRepairBuilt ? _theme.NavStyle : _theme.NavOnStyle))
        {
            _pumpRepairBuilt = false;
            _pumpRepairQuote = null;
        }
        if (Ui.Button(new Rect(half + 8, 32, half, 32), L.K("workshop.pumpRepair.builtTab", "自制整机交付", "Deliver built pump"), _pumpRepairBuilt ? _theme.NavOnStyle : _theme.NavStyle))
        {
            _pumpRepairBuilt = true;
            RefreshBuiltPumps();
        }
        if (_pumpRepairBuilt) { DrawBuiltPump(w); return; }

        GUI.Label(new Rect(0, 74, w, 65), L.K("workshop.pumpRepair.job", "保留能用的原件，给拉伤的轴套找替换件。原泵须通过普通试水和连续起停。", "Keep any usable original parts and replace the scored sleeve. The original pump must pass the flow and restart tests."), _theme.Wrap);
        GUI.Label(new Rect(0, 140, w, 51), L.F("workshop.pumpRepair.prices", "工钱 {0}₵ · 低损试水 +{1}₵ · 加压合格 +{2}₵", "Base {0}₵ · Low-loss test +{1}₵ · Pressure test +{2}₵", WaterPumpRepairState.BasePay, WaterPumpRepairState.LowLossBonus, WaterPumpRepairState.PressureBonus), _theme.Wrap);
        if (receipt == null)
        {
            if (Ui.Button(new Rect(0, 196, w, 38), L.K("workshop.pumpRepair.claim", "领旧泵到柜台", "Collect the old pump"), _theme.BtnStyle))
            {
                var status = WaterStationRepair.Claim();
                if (status != SaleStatus.Ready) ShowPumpRepairResult(status, claiming: true);
                _pumpRepairQuote = null;
            }
            return;
        }
        GUI.Label(new Rect(0, 193, w, 53), L.K("workshop.pumpRepair.tooling", "验收前把电池和两个试液容器取回，再合上检修盖。", "Before inspection, remove the battery and both test vessels, then close the cover."), _theme.Wrap);
        if (_pumpRepairQuote is int quote)
        {
            if (Ui.Button(new Rect(0, 251, w, 38), L.F("workshop.pumpRepair.confirm", "交回原泵 · 结款 {0}₵", "Return the original pump · Receive {0}₵", quote), _theme.BtnStyle))
            {
                ShowPumpRepairResult(WaterStationRepair.Submit(quote));
                _pumpRepairQuote = null;
            }
        }
        else if (Ui.Button(new Rect(0, 251, w, 38), L.K("workshop.pumpRepair.inspect", "验收原泵并报价", "Inspect the original pump"), _theme.BtnStyle))
        {
            var status = WaterStationRepair.Check(out int amount);
            if (status == SaleStatus.Ready) _pumpRepairQuote = amount;
            else ShowPumpRepairResult(status);
        }
    }

    private static void DrawBuiltPump(float w)
    {
        GUI.Label(new Rect(0, 74, w, 67), L.K("workshop.pumpRepair.builtRule", "自制泵须通过普通、连续起停和加压。若原借机还在店内，须先走返修验收。", "A built pump needs flow, restart and pressure proof. If the original loan is still in the shop, complete its repair."), _theme.Wrap);
        GUI.Label(new Rect(0, 142, w, 37), L.F("workshop.pumpRepair.builtPrice", "整机 {0}₵ · 低损试水 +{1}₵", "Installation {0}₵ · Low-loss test +{1}₵", WaterPumpRepairState.BaseBuildPay, WaterPumpRepairState.LowLossBonus), _theme.Wrap);
        if (Ui.Button(new Rect(0, 183, w, 34), L.K("workshop.sales.refresh", "刷新库存", "Refresh stock"), _theme.BtnStyle))
            RefreshBuiltPumps();
        if (_pumpCandidates.Count == 0)
        {
            GUI.Label(new Rect(0, 222, w, 50), L.K("workshop.pumpRepair.builtMissing", "把自用试水泵放进店内库存后刷新。", "Place your test pump in the shop and refresh stock."), _theme.Wrap);
            return;
        }
        var item = _pumpCandidates[_pumpCandidateIndex];
        string label = item.uniqueId > 0 ? item.name + " #" + item.uniqueId : item.name;
        string pick = L.F("workshop.pumpRepair.builtPick", "{0}/{1} · {2} ›", "{0}/{1} · {2} ›", _pumpCandidateIndex + 1, _pumpCandidates.Count, label);
        if (Ui.Button(new Rect(0, 222, w, 35), pick, _theme.BtnStyle))
        {
            _pumpCandidateIndex = (_pumpCandidateIndex + 1) % _pumpCandidates.Count;
            _pumpRepairQuote = null;
            return;
        }
        string action = _pumpRepairQuote is int quote
            ? L.F("workshop.pumpRepair.builtConfirm", "交付所选整机 · {0}₵", "Deliver selected pump · {0}₵", quote)
            : L.K("workshop.pumpRepair.builtInspect", "验收所选整机并报价", "Inspect selected pump");
        if (!Ui.Button(new Rect(0, 264, w, 38), action, _theme.BtnStyle)) return;
        if (_pumpRepairQuote is int expected)
        {
            ShowPumpRepairResult(WaterStationRepair.Submit(expected, item), built: true);
            _pumpRepairQuote = null;
            return;
        }
        var status = WaterStationRepair.Check(out int amount, item);
        if (status == SaleStatus.Ready) _pumpRepairQuote = amount;
        else ShowPumpRepairResult(status, built: true);
    }

    private static void DrawCoreIntake(float w, CoreIntakeState receipt)
    {
        GUI.Label(new Rect(0, 77, w, 68), L.K("workshop.core.desk", "核心鉴定已开放。到综合工艺台装配校准台，领回原件做预校与复测。", "Core certification is open. Assemble a rig at the workbench and collect original modules for their two tests."), _theme.Wrap);
        DrawCoreClaim(w, 153, receipt, IndustrialCoreKind.Timing);
        DrawCoreClaim(w, 198, receipt, IndustrialCoreKind.Phase);
    }

    private static void DrawCoreClaim(float w, float y, CoreIntakeState receipt, IndustrialCoreKind kind)
    {
        string label = Goods.NameOf(MeridianCoreIntake.Feed(kind));
        if (receipt?.Id(kind) != null)
        {
            GUI.Label(new Rect(0, y, w, 37), L.F("workshop.core.collected", "{0} · 已领", "{0} · Collected", label), _theme.Body);
            return;
        }
        if (!Ui.Button(new Rect(0, y, w, 37), L.F("workshop.core.collect", "领回{0}", "Collect {0}", label), _theme.BtnStyle)) return;
        var status = MeridianCoreIntake.Claim(kind);
        if (status is not (SaleStatus.Ready or SaleStatus.TrialSettled))
            Toast(status == SaleStatus.Short ? L.K("workshop.core.space", "柜台空间不足，请腾出原件位置。", "Make room on the counter for the module.") : SaleMessage(status));
    }

    private static void DrawPhaseSite(float w, PhaseSiteState state)
    {
        const float top = 250;
        GUI.Label(new Rect(0, top, w, 31), L.K("workshop.phase.site", "运输井 · 停用泵组", "Transport shaft · Retired pump"), _theme.Body);
        if (state == null)
        {
            GUI.Label(new Rect(0, top + 36, w, 53), L.K("workshop.phase.surveyDetail", "请班组勘察。下一营业日取得载荷与布标记录。", "Request a crew survey. The lift rating and beacon will be ready next business day."), _theme.Wrap);
            if (Ui.Button(new Rect(0, top + 94, w, 36), L.K("workshop.phase.survey", "请求勘察布标", "Request survey and beacon"), _theme.BtnStyle))
                Toast(PhaseRecoveryStation.Message(PhaseSite.Survey()));
            return;
        }
        if (state.CargoId.HasValue)
        {
            string delivered = L.F("workshop.phase.received", "已回柜 · 实物 #{0}", "Received on counter · Item #{0}", state.CargoId);
            if (state.Route != PhaseRoute.Whole) delivered += "\n" + PhaseRecoveryStation.RouteCommitment();
            GUI.Label(new Rect(0, top + 36, w, 58), delivered, _theme.Wrap);
            return;
        }
        if (state.DueDay is int due)
        {
            GUI.Label(new Rect(0, top + 36, w, 42), L.F("workshop.phase.arrival", "回柜日 {0}", "Counter arrival on day {0}", due), _theme.Wrap);
            if (Ui.Button(new Rect(0, top + 83, w, 36), L.K("workshop.phase.claim", "签收回货", "Collect the shipment"), _theme.BtnStyle))
                Toast(PhaseRecoveryStation.Message(PhaseSite.Claim()));
            return;
        }
        if (Day <= state.SurveyDay)
        {
            GUI.Label(new Rect(0, top + 36, w, 52), PhaseRecoveryStation.Message(PhaseResult.Surveying), _theme.Wrap);
            return;
        }
        GUI.Label(new Rect(0, top + 36, w, 90), L.K("workshop.phase.siteOptions", "布标完成。班组拆运次日送来原绕组与旧炉衬。相位站可用 16 电分装，或用 24 电走两日整机回运。", "Beacon placed. Crew dismantling delivers the winding and lining next day. A phase station can split for 16 charge or recover the intact pump for 24 over two days.")
            + "\n" + PhaseRecoveryStation.RouteCommitment(), _theme.Wrap);
        if (Ui.Button(new Rect(0, top + 132, w, 37), L.F("workshop.phase.bookCrew", "委托班组拆运 · 待付 {0}₵", "Book crew dismantling · {0}₵ due", PhaseSiteState.CrewFee - state.CrewPaid), _theme.BtnStyle))
            Toast(PhaseRecoveryStation.Message(PhaseSite.BookCrew()));
    }

    private static void DrawJointWaterWork(float w, JointWaterWorkState work)
    {
        const float top = 350;
        GUI.Label(new Rect(0, top, w, 31), L.K("workshop.joint.title", "水站联合复工", "Joint water station recommissioning"), _theme.Body);
        if (work == null)
        {
            GUI.Label(new Rect(0, top + 38, w, 95), L.K("workshop.joint.instructions", "用折叠硅板制作复工备件，修复原回收泵并加装承压壳。在原相位站装入这两件实物再签发。",
                "Make a kit from a folded plate. Repair the recovered pump and fit its pressure shell, then dock both in the original phase station."), _theme.Wrap);
            return;
        }
        GUI.Label(new Rect(0, top + 36, w, 68), JointWaterWork.Progress(work), _theme.Wrap);
        GUI.Label(new Rect(0, top + 107, w, 43), L.F("workshop.joint.receipt", "实付 {0}/{1}₵ · 已挣得 {2}₵",
            "Paid {0}/{1}₵ · Earned {2}₵", work.Paid, JointWaterWorkState.Total, work.Earned), _theme.Wrap);
        if (work.Due > 0 && Ui.Button(new Rect(0, top + 152, w, 39),
            L.F("workshop.joint.pay", "结本段工钱 {0}₵", "Collect milestone payment {0}₵", work.Due), _theme.BtnStyle))
            Toast(SaleMessage(JointWaterWork.PayNext()));
    }

    private static void ShowPumpRepairResult(SaleStatus status, bool claiming = false, bool built = false)
    {
        Toast(status switch
        {
            SaleStatus.Short when claiming => L.K("workshop.pumpRepair.missing", "柜台放不下旧泵，腾出 3×3 空位后重试。", "The counter needs an empty 3×3 area for the pump."),
            SaleStatus.Short when built => L.K("workshop.pumpRepair.builtTests", "所选整机还缺普通、起停或加压凭据。", "The selected pump needs flow, restart and pressure proof."),
            SaleStatus.Short => L.K("workshop.pumpRepair.tests", "原泵还缺有效的普通试水或连续起停凭据。", "The original pump needs valid flow and restart results."),
            SaleStatus.Outstanding when built => L.K("workshop.pumpRepair.builtOutstanding", "先验收店内原借机，或取回所选整机的试验器材并合盖。", "Handle the original loan first, or clear and close the selected pump’s test slots."),
            SaleStatus.Outstanding => L.K("workshop.pumpRepair.tooling", "验收前把电池和两个试液容器取回，再合上检修盖。", "Before inspection, remove the battery and both test vessels, then close the cover."),
            SaleStatus.Changed when built => L.K("workshop.pumpRepair.builtChanged", "所选整机或报价已变，请刷新库存并重新验收。", "The selected pump or quote changed. Refresh stock and inspect again."),
            SaleStatus.Changed => L.K("workshop.pumpRepair.changed", "原泵、验收条件或报价有变化，请把原泵放在店内库存并重新验收。", "The pump, inspection or quote changed. Put the original pump in shop stock and inspect again."),
            SaleStatus.Paid or SaleStatus.TrialSettled when built => L.K("workshop.pumpRepair.builtSettled", "水站已接收整机并结款。", "The water station has received the installation and paid in full."),
            SaleStatus.Paid or SaleStatus.TrialSettled => L.K("workshop.pumpRepair.settled", "水站已收回旧泵，返修款已结清。", "The water station has its pump back and the repair is paid in full."),
            _ => SaleMessage(status)
        });
    }
}
