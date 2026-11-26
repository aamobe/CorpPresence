using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
    private static StoreSession _workshopSession;
    private static long _cenPowder;
    private static readonly List<LowerMachinist.StockLine> CenStock = new();
    private static Vector2 _cenStockScroll;
    private static bool _showCenStock;
    private static readonly long[] CenBins = new long[6];
    private static bool ShowCenFeederButton => LowerMachinist.MineralizerUnlocked(CorpState.Current);
    private static Rect CenFeederButton
    {
        get
        {
            var guide = StoryGuideButton;
            return new Rect(guide.xMin - 146, guide.y, 136, guide.height);
        }
    }

    private static void OpenCen()
    {
        _showCenStock = false;
        _cenStockScroll = Vector2.zero;
        RefreshCenStock();
    }

    private static void RefreshCenStock()
    {
        _cenPowder = LowerMachinist.QualifiedStock(CenBins, CenStock);
    }

    private static void DrawCen()
    {
        if (!_workshopSession.IsCurrent || !LowerMachinist.StarterUnlocked(CorpState.Current))
        {
            Close();
            return;
        }
        bool showTrial = ShowCenFeederButton;
        var trial = CenFeederButton;
        float titleRight = showTrial ? trial.xMin : StoryGuideButton.xMin;
        GUI.Label(new Rect(24, 12, titleRight - 36, 32), _company.Name + " · " + _company.DeskName, _theme.Title);
        if (showTrial && Ui.Button(trial, L.T("给料机试机", "Feeder trial"),
                _module == 6 ? _theme.NavOnStyle : _theme.BtnStyle)) _module = 6;
        string[] tabs = { L.K("workshop.cen.starterTab", "开工领料", "Starter supplies"),
            L.K("workshop.cen.orderTab", "订货", "Order supplies"), L.K("workshop.cen.salesTab", "现货收购", "Sell stock"),
            L.K("workshop.cen.feedTab", "来料签", "Feedstock guide"), L.K("workshop.overhaul.tab", "联合工单", "Joint contracts"),
            L.T("棒料验货", "Rod inspection") };
        for (int i = 0; i < tabs.Length; i++)
        {
            if (Ui.Button(new Rect(24 + i * ((_rect.width - 48) / tabs.Length), 55, (_rect.width - 48) / tabs.Length - 5, 34), tabs[i], _module == i ? _theme.NavOnStyle : _theme.BtnStyle))
            {
                _module = i;
                if (i == 0) RefreshCenStock();
                if (i == 4) RefreshOverhaul();
                if (i == 5) { _inspectionScroll = Vector2.zero; RefreshRodInspection(); }
            }
        }
        var r = new Rect(24, 106, _rect.width - 48, _rect.height - 130);
        if (_module == 6) { DrawFeederTrial(r); return; }
        if (_module == 5) { DrawRodInspection(r); return; }
        if (_module == 4) { DrawOverhaul(r); return; }
        if (_module == 3) { DrawCenFeed(r); return; }
        if (_module == 2) { DrawWorkshopSales(r); return; }
        if (_module == 1)
        {
            DrawEquipmentOrder(r, _company.Catalog, L.K("workshop.cen.orderNote", "每天一单，可合并品项。次日送到柜台；付费料筐只含所列来料。", "One order a day. Combine items for delivery tomorrow. Paid baskets contain the listed feedstock only."),
                entry => _company.OrderUnitPrice(entry, State), L.K("ui.jackson.overview.placeOrder", "确认订购", "Place order"));
            return;
        }
        var state = CorpState.Current.Workshop;
        string note = state.CenKit == null ? L.K("workshop.chart.starter", "任选一筐 · 免费 · 附 24 电电池", "Pick one free basket · includes a 24-charge cell") :
            state.CenKitRemaining == 0 ? CenStarterKit.Claimed() : CenStarterKit.Pending(state);
        GUI.Label(new Rect(r.x, r.y, r.width, 37), note, _theme.Wrap);
        string[] choices = { "glass", "metal", "salvage" };
        float basketWidth = state.CenKit == null ? (r.width - 12) / 3 : r.width;
        for (int i = 0; i < choices.Length; i++)
        {
            var entry = CenSupplies.Kit(choices[i]);
            if (state.CenKit != null && entry.Id != state.CenKit) continue;
            var row = new Rect(r.x + (state.CenKit == null ? i * (basketWidth + 6) : 0), r.y + 34, basketWidth, 36);
            if (state.CenKitRemaining == 0 && state.CenKit != null) GUI.Label(row, entry.Name(), _theme.Body);
            else if (Ui.Button(row, entry.Name(), _theme.BtnStyle))
                Toast(CenStarterKit.Claim(entry.Id, _workshopSession));
        }
        float y = r.y + 92;
        GUI.Label(new Rect(r.x, y, r.width - 145, 30), L.F("workshop.cen.stock", "二访备货 · 合格粉 {0} / 300 点", "Second visit · {0} / 300 qualifying powder points", _cenPowder), _theme.Title);
        if (Ui.Button(new Rect(r.xMax - 138, y, 138, 29), L.K("workshop.cen.refresh", "重新清点", "Count again"), _theme.BtnStyle)) RefreshCenStock();
        WorkshopBar(new Rect(r.x, y + 36, r.width, 13), _cenPowder, 300, _theme.Accent);
        GUI.Label(new Rect(r.x, y + 61, r.width - 170, 36), L.K("workshop.chart.purity", "单盒／仓 ≥81% 才计入 · 矿粉保留", "≥81% per box or bin · powder stays with you"), _theme.Wrap);
        if (Ui.Button(new Rect(r.xMax - 160, y + 59, 160, 30), L.K("workshop.cen.stockDetails", "逐盒逐仓明细", "Box and bin details"), _theme.BtnStyle))
        {
            _showCenStock = !_showCenStock;
            _cenStockScroll = Vector2.zero;
        }
        if (_showCenStock)
        {
            var area = new Rect(r.x, y + 103, r.width, Mathf.Max(1, r.yMax - y - 103));
            var list = new Rect(0, 0, area.width - 20, CenStock.Count * 29);
            _cenStockScroll = Ui.BeginPanelScrollView(area, _cenStockScroll, list);
            for (int i = 0; i < CenStock.Count; i++)
            {
                var line = CenStock[i];
                string name = line.Container + " #" + line.Ordinal;
                if (line.Bin >= 0) name += " · " + PowderLabel(line.Bin);
                float rowY = i * 29;
                GUI.Label(new Rect(0, rowY, list.width * .48f, 28), name, _theme.Body);
                WorkshopBar(new Rect(list.width * .49f, rowY + 9, list.width * .16f, 9), line.Purity, 10000,
                    line.Qualified > 0 ? _theme.Accent : _theme.Hot);
                GUI.Label(new Rect(list.width * .65f, rowY, list.width * .15f, 28), $"{line.Purity / 100m:0.##}%", _theme.Center);
                GUI.Label(new Rect(list.width * .80f, rowY, list.width * .20f, 28), $"{line.Qualified} / {line.Amount}", _theme.Price);
            }
            GUI.EndScrollView();
            return;
        }
        GUI.Label(new Rect(r.x, y + 101, r.width, 28), L.F("workshop.cen.looseBoxes", "独立接粉盒合计 {0} 点", "Standalone powder boxes · {0} points", _cenPowder - CenBins.Sum()), _theme.Body);
        for (int i = 0; i < CenBins.Length; i++)
        {
            float width = r.width / 3;
            float left = r.x + i % 3 * width;
            float top = y + 133 + i / 3 * 58;
            WorkshopMetric(new Rect(left, top, width - 6, 52), PowderLabel(i), CenBins[i].ToString());
            WorkshopBar(new Rect(left + 6, top + 45, width - 18, 5), CenBins[i], _cenPowder, PowderInk(i));
        }
    }

    private static void DrawFeederTrial(Rect r)
    {
        var trial = CorpState.Current?.Workshop?.FeederTrial;
        GUI.Label(new Rect(r.x, r.y, r.width, 83),
            L.T("借一台旧给料机，处理两批旧料。每批至少两件。普通机分两夜，耐疲劳轴和含油套可排双批。阿岑随借机提供螺丝刀和一份润滑料。",
                "Borrow a feeder and process two batches of at least two pieces each. Run two ordinary nights or fit a fatigue-rated shaft and oiled sleeve for both batches in one night. Cen supplies a screwdriver and lubricant."), _theme.Wrap);
        float y = r.y + 88;
        var result = trial?.Batches ?? 0;
        GUI.Label(new Rect(r.x, y, r.width, 32), L.T($"实机记录 {result}/2 批", $"Completed runs {result}/2 batches"), _theme.Body);
        y += 36;
        if (trial is not { Returned: true } && (trial?.LoanId == null || trial.ToolsRemaining > 0))
        {
            string button = trial?.LoanId == null ? L.T("领用借机及工具", "Collect feeder and tools") : L.T("领取剩余工具", "Collect remaining tools");
            if (Ui.Button(new Rect(r.x, y, r.width, 34), button, _theme.BtnStyle))
                Toast(FeederTrialMessage(CenFeederTrial.Claim()));
            y += 40;
        }
        if (trial is not { Passed: true }) return;
        if (!trial.Returned)
        {
            GUI.Label(new Rect(r.x, y, r.width, 50),
                L.T("取下自己的轴、轴套、模组和电池，清空来料、粉盒与出料，再归还借机。",
                    "Remove your shaft, sleeve, modules and battery, then clear feed, powder box and output before returning the loan."), _theme.Wrap);
            y += 53;
            if (Ui.Button(new Rect(r.x, y, r.width, 34), L.T("归还空机身", "Return empty feeder"), _theme.BtnStyle))
                Toast(FeederTrialMessage(CenFeederTrial.Return(CenFeederTrial.FindLoan())));
            y += 40;
        }
        GUI.Label(new Rect(r.x, y, r.width, 64), trial.Handbook
            ? L.T("已取得子午线旧机检修图册。", "Meridian's service handbook has been issued.")
            : L.T("完成试机。去子午线“锻造 → 检修规格单”交一根符合规格的给料轴。", "Trial complete. Submit one qualifying feed shaft at Meridian’s Forging → Service specification desk."), _theme.Wrap);
    }

    private static string FeederTrialMessage(FeederTrialResult result) => result switch
    {
        FeederTrialResult.Done => L.T("借机实物已登记", "Loan inventory recorded"),
        FeederTrialResult.Waiting => L.T("还有物品待领，请在自己一侧的柜台留出空间", "Items still due. Make space on your side of the counter."),
        FeederTrialResult.Changed => L.T("确认借机在店内，机身和所有槽位已清空", "Bring the borrowed feeder into the shop and empty all its slots."),
        FeederTrialResult.Frozen => L.T("交割记录待核", "Loan receipt on hold"),
        _ => L.T("当前无法办理", "Service unavailable")
    };

    private static void DrawCenFeed(Rect r)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 56), L.K("workshop.cen.feedBasis", "每件来料破碎后的基础点数。保渣时保留表中渣量；普通垃圾显示各成分可能范围。", "Base powder points per piece. Preserve residue to keep the amounts shown. Junk shows each component’s possible range."), _theme.Wrap);
        float labelWidth = Mathf.Min(224, r.width * .33f), column = (r.width - labelWidth) / 5;
        for (int i = 0; i < 5; i++)
            GUI.Label(new Rect(r.x + labelWidth + column * i, r.y + 67, column, 34), PowderLabel(i), _theme.Center);
        void Row(int n, string name, CrusherFeed feed)
        {
            float y = r.y + 110 + n * 52;
            GUI.Label(new Rect(r.x, y, labelWidth - 8, 44), name, _theme.Wrap);
            var (min, max) = CrusherBatch.Range(feed, 0);
            for (int i = 0; i < min.Length; i++)
            {
                float x = r.x + labelWidth + column * i;
                GUI.Label(new Rect(x, y, column, 28), min[i] == max[i] ? min[i].ToString() : $"{min[i]}–{max[i]}", _theme.Center);
                WorkshopBar(new Rect(x + 5, y + 30, column - 10, 7), max[i], 100, PowderInk(i), min[i]);
            }
        }
        Row(0, L.K("workshop.cen.glassFeed", "空啤酒瓶、玻璃碎片", "Empty beer bottle or glass shard"), CrusherFeed.Glass);
        Row(1, L.K("workshop.cen.scrapFeed", "废金属", "Scrap metal"), CrusherFeed.Scrap);
        Row(2, L.K("workshop.cen.electronicsFeed", "电子残料", "Electronic salvage"), CrusherFeed.SalvageElectronics);
        Row(3, L.K("workshop.cen.liningFeed", "旧炉衬碎块", "Spent furnace lining"), CrusherFeed.SpentLining);
        Row(4, L.K("workshop.cen.junkFeed", "普通垃圾", "Junk"), CrusherFeed.Junk);
        GUI.Label(new Rect(r.x, r.y + 372, r.width, 60),
            L.K("workshop.cen.powerRoute", "基础机器每夜用 12 电。开工电池够两夜：单磨炉衬，次夜可直接烧垫砖。整筐混磨后再筛、再烧，共需 36 电，可提前订一枚 16 元电池。",
                "Base machines use 12 charge per night. The starter cell lasts two nights: crush the lining alone, then fire a tile. Crushing a mixed basket, sieving and firing takes 36 charge. Order another cell for 16 beforehand."), _theme.Wrap);
        if (LowerMachinist.MineralizerUnlocked(CorpState.Current))
            GUI.Label(new Rect(r.x, r.y + 435, r.width, 67),
                L.K("workshop.cen.nextBuyers", "卡尔特声望到 10 并见过黑羽静后，留意她的光盐芯蓝图。备两盒合格粉：40 渣、10 稀有金属。走子午线锻造的，留 120 点合格金属粉烧成高纯度以上的原版锭，罗森的精锻炉可以接。",
                    "After reaching 10 Cartel reputation and meeting Shizuka, watch for her light salt blueprint. Keep separate qualifying boxes with 40 residue and 10 rare metal. For Meridian forging, sinter 120 metal points from qualifying powder into a high-purity or better ingot for Rosen’s furnace."), _theme.Wrap);
    }
}
