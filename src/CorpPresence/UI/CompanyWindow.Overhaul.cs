using System;
using System.Linq;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
    private static FirstOverhaulQuote _overhaul;
    private static OverhaulWorkshopStock _overhaulStock;
    private enum ContractPage { Manifest, Preparation, Returns }
    private static ContractPage _contractPage;
    private static RotationQuote _rotation;
    private static bool _commissionRoute;
    private static Vector2 _overhaulScroll;
    private static float _manifestHeight = 540;
    private static float _nextStockRead;

    private static void ResetOverhaul()
    {
        _overhaul = null;
        _overhaulStock = null;
        _overhaulScroll = Vector2.zero;
        _manifestHeight = 540;
        _contractPage = ContractPage.Manifest;
        _commissionRoute = false;
        _rotation = null;
        ResetReturnDesk();
    }

    private static void RefreshOverhaul()
    {
        _overhaul = new FirstOverhaulQuote();
        _rotation = _overhaul.Contract?.CompletedDay < _overhaul.Day ? new RotationQuote() : null;
        RefreshReturnDesk();
        _overhaulStock = null;
    }

    private static void DrawOverhaul(Rect r)
    {
        if (_overhaul == null || _overhaul.Day != Day) RefreshOverhaul();
        string title = _rotation == null ? L.K("workshop.overhaul.title","下层水站大修","Lower Levels waterworks overhaul") : RotationName(_rotation.Kind);
        int tabCount = _rotation == null ? 3 : 4;
        float controlsWidth = tabCount * 112f + 6f;
        bool wrapTabs = r.width < controlsWidth + 190f;
        float tabY = r.y + (wrapTabs ? 38f : 0f);
        float tabWidth = wrapTabs ? r.width / tabCount : controlsWidth / tabCount;
        float tabX = wrapTabs ? r.x : r.xMax - controlsWidth;
        GUI.Label(new Rect(r.x,r.y,wrapTabs ? r.width : r.width-controlsWidth-4f,36),title,_theme.Title);
        void Tab(ContractPage page, string label)
        {
            if (Ui.Button(new Rect(tabX,tabY,tabWidth-4f,32),label,_contractPage == page ? _theme.NavOnStyle : _theme.BtnStyle))
            {
                _contractPage = page;
                _overhaulScroll = Vector2.zero;
                if (page == ContractPage.Returns) RefreshReturnDesk();
            }
            tabX += tabWidth;
        }
        Tab(ContractPage.Manifest,L.K("workshop.overhaul.manifest","交货单","Manifest"));
        Tab(ContractPage.Preparation,L.K("workshop.overhaul.plan","备料排班","Preparation"));
        if (_rotation != null) Tab(ContractPage.Returns,L.K("workshop.rotation.returns","领料与退件","Shell desk"));
        if (Ui.Button(new Rect(tabX,tabY,tabWidth,32),L.K("workshop.sales.refresh","刷新库存","Refresh stock"),_theme.BtnStyle)) RefreshOverhaul();
        var area = new Rect(r.x,tabY+44f,r.width,r.yMax-tabY-44f);
        float w = area.width-22;
        float bottom = _contractPage == ContractPage.Preparation ? _overhaulPlanHeight : _rotation != null ? _rotationHeight : _manifestHeight;
        _overhaulScroll=Ui.BeginPanelScrollView(area,_overhaulScroll,new Rect(0,0,w,bottom));
        // Ui.Button receives pointer positions relative to the company window.
        var previousOrigin = Ui.Origin;
        var previousClip = Ui.Clip;
        Ui.Origin = area.position - _overhaulScroll;
        Ui.Clip = new Rect(area.x, area.y, w, area.height);
        try
        {
            if (_contractPage == ContractPage.Preparation) DrawOverhaulPlan(w);
            else if (_rotation != null && _contractPage == ContractPage.Returns) DrawReturns(w);
            else if (_rotation != null) DrawRotation(w);
            else DrawOverhaulManifest(w);
        }
        finally
        {
            Ui.Origin = previousOrigin;
            Ui.Clip = previousClip;
            GUI.EndScrollView();
        }
    }

    private static void DrawOverhaulManifest(float w)
    {
        var q = _overhaul;
        var c = q.Contract;
        var state = CorpState.Current;
        var gates = new[] {
            (LowerMachinist.MineralizerUnlocked(state),L.K("workshop.overhaul.cen","阿岑二访成交","Cen’s second sale")),
            (WorkshopSaleOffer.GelOrderSettled(state),L.K("workshop.overhaul.gel","蓝胆首单结清","Gel trial paid")),
            (CartelShizuka.BlueprintDelivered(state),L.K("workshop.overhaul.salt","黑羽静已送光盐图","Salt blueprint received")),
            (MeridianIndustrial.Eligible(state),L.K("workshop.overhaul.forge","子午线框架工艺","Meridian frame process")) };
        int columns = w < 540 ? 2 : 4;
        for (int i = 0; i < gates.Length; i++)
        {
            var (done, label) = gates[i];
            var box = new Rect(i % columns * w / columns, i / columns * 40, w / columns - 5, 36);
            Ui.Fill(box, _theme.Paper);
            Ui.Fill(new Rect(box.x, box.y, 3, box.height), done ? _theme.Accent : _theme.Line);
            GUI.Label(new Rect(box.x + 9, box.y + 5, box.width - 13, 28), (done ? "✓ " : "□ ") + label, _theme.Wrap);
        }
        float y = gates.Length / columns * 40 + 7;
        for (int i = 0; i < q.Specs.Length; i++)
            y = DrawTruckCard(w, y, i, q.Specs[i], c?.Trucks[i], q.Day,
                c is { Abandoned: false, CompletedDay: null } && q.TruckIndex == i);
        if (c?.CompletedDay is int paidDay)
        {
            GUI.Label(new Rect(0,y,w,40),L.F("workshop.overhaul.finished","第 {0} 天两车已结清，累计实收 {1} 元。","Both trucks settled on day {0}. Total paid: {1}.",paidDay,c.Trucks.Sum(t=>t.Paid)),_theme.Title);
            GUI.Label(new Rect(0,y+44,w,44),L.F("workshop.rotation.opens","第 {0} 天起开放三日轮换签约。","Three-day signing windows begin on day {0}.",(long)paidDay+1),_theme.Wrap);
            _manifestHeight = y + 94;
            return;
        }
        if (c == null || c.Abandoned)
        {
            RotationLine(w, ref y, L.K("workshop.overhaul.terms","签约日决定两车交期。逾期照付底价。中止后可按原交期和已交进度续办。","Signing sets both deadlines. Late loads retain the base price. Resuming keeps the original deadlines and delivered stock."));
            if (FirstOverhaulQuote.Eligible(state) && Ui.Button(new Rect(0,y,w,36),c==null ?
                L.K("workshop.overhaul.sign","按以上交期签约","Sign for these dates") : L.K("workshop.overhaul.resume","按原合同续办","Resume original contract"),_theme.NavOnStyle))
            {
                var result=q.Sign(); if (result!=SaleStatus.Ready) Toast(SaleMessage(result)); RefreshOverhaul();
            }
            else if (!FirstOverhaulQuote.Eligible(state))
                RotationLine(w, ref y, L.K("workshop.overhaul.rosen","完成上方缺项。子午线工艺要求阿岑二访成交，并已与罗森谈过锻造或取得锻具。","Complete the unchecked milestones. Meridian requires Cen’s second sale and Rosen’s forge offer or existing forge equipment."));
            _manifestHeight = y + 46;
            return;
        }
        GUI.Label(new Rect(0,y,w,36),L.F("workshop.overhaul.quote","当前第 {0} 车 · 本车应付 {1} · 已收货 {2}/{3}",
            "Truck {0} · amount due {1} · collected {2}/{3}",q.TruckIndex+1,q.Amount,c.Trucks[q.TruckIndex].Received.Sum(),OverhaulTruck.Quantities[q.TruckIndex].Sum()),_theme.Title);
        y+=41;
        _manifestHeight = DrawLoadPicker(w,y,q) + 14;
    }

    private static float DrawLoadPicker(float w, float y, OverhaulQuote quote)
    {
        for (int i=0;i<quote.Slots.Length;i++)
        {
            int selected=quote.Sources[i];
            string name=Goods.NameOf(quote.Slots[i].Substring("corp_presence_".Length));
            string source=selected>=0 ? SaleSourceName(quote.Stock[selected]) : L.K("workshop.overhaul.missing","缺货","Missing stock");
            if (selected>=0 && IndustrialGoods.TryReadOrigin(quote.Stock[selected].Item,out _,out int serial,out int slot))
                source+=" · "+L.F("workshop.rotation.origin","返修合同 #{0} · 壳位 {1}","Repair contract #{0} · shell {1}",serial,slot+1);
            if (Ui.Button(new Rect(0,y,w,33),name + "   ·   " + source + "   ›",_theme.BtnStyle))
            {
                for (int j=1;j<=quote.Stock.Count;j++)
                {
                    int index=(selected+j)%quote.Stock.Count;
                    if (quote.Stock[index].Item.identifier!=quote.Slots[i]) continue;
                    quote.Sources[i]=index; break;
                }
            }
            y+=38;
        }
        var status=quote.Check();
        if (status==SaleStatus.Ready)
        {
            if (Ui.Button(new Rect(0,y,w,36),L.K("workshop.sales.confirm","确认交货并结款","Confirm delivery and payment"),_theme.NavOnStyle))
            { Toast(SaleMessage(quote.Submit())); RefreshOverhaul(); return y+36; }
        }
        else GUI.Label(new Rect(0,y,w,35),SaleMessage(status),_theme.Wrap);
        if (quote.CanAbandon && Ui.Button(new Rect(0,y+45,245,32),L.K("workshop.overhaul.abandon","中止并保留原合同","Suspend and retain contract"),_theme.BtnStyle))
        { var result=quote.Abandon(); if(result!=SaleStatus.Ready) Toast(SaleMessage(result)); RefreshOverhaul(); }
        return y+82;
    }
}
