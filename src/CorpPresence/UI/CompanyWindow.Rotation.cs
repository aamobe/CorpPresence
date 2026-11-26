using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
    private static float _rotationHeight = 650f;
    private static IReadOnlyList<RotationContract> _returnContracts = Array.Empty<RotationContract>();
    private static int _returnSerial, _returnSource;
    private static string[] _returnNames=Array.Empty<string>();
    private static GameItem[] _returnStock = Array.Empty<GameItem>();

    private static string RotationName(RotationKind kind) => kind switch
    {
        RotationKind.Expansion => L.K("workshop.rotation.expansion","工坊扩建","Workshop expansion"),
        RotationKind.Supplies => L.K("workshop.rotation.supplies","滤材补给","Filter supplies"),
        RotationKind.Repair => L.K("workshop.rotation.repair","水站返修","Waterworks repairs"),
        _ => L.K("workshop.rotation.joint","联合大修","Joint overhaul")
    };

    private static void RotationLine(float w, ref float y, string text)
    {
        float h = Mathf.Max(30,_theme.Wrap.CalcHeight(new GUIContent(text),w));
        GUI.Label(new Rect(0,y,w,h),text,_theme.Wrap);
        y += h+8;
    }

    private static void DrawRotation(float w)
    {
        var q = _rotation;
        var c = q.Contract;
        float y=0;
        int epoch = _overhaul.Contract.CompletedDay.Value+1;
        int window = q.Window;
        long open = (long)epoch+window*3L;
        RotationLine(w,ref y,L.F("workshop.rotation.window","今日第 {0} 天 · 签约窗口 {1}–{2} 天：{3}。下个窗口：{4}。",
            "Day {0} · Signing window: days {1}–{2}, {3}. Next: {4}.",q.Day,open,open+2,
            RotationName((RotationKind)(window%4)),RotationName((RotationKind)((window+1)%4))));
        if (c != null)
            RotationLine(w,ref y,L.F("workshop.rotation.signed","本档合同 #{0} · 第 {1} 天签约 · {2}",
                "Contract #{0} · signed on day {1} · {2}",c.Serial,c.SignedDay,RotationName(c.Kind)));
        for (int i=0;i<q.Specs.Length;i++)
            y = DrawTruckCard(w, y, i, q.Specs[i], c?.Trucks[i], q.Day,
                c is { Abandoned: false, CompletedDay: null } && q.TruckIndex == i);
        if (q.Kind == RotationKind.Repair)
            RotationLine(w,ref y,L.K("workshop.rotation.repairGuide","二车可交新框架总成，或本合同净壳做的总成。到「领料与退件」取两只旧壳；工艺台清洗每只耗 15 mL 酒精。首车可先交。",
                "Truck two accepts new assemblies or assemblies made from this contract’s cleaned shells. Collect two used shells at the shell desk. Washing costs 15 mL of alcohol each. Truck one can go first."));
        if (c?.CompletedDay is int closed)
            RotationLine(w,ref y,L.F("workshop.rotation.paid","第 {0} 天结清，累计实收 {1} 元。此窗口只签一次。",
                "Settled on day {0}. Total paid: {1}. One contract per window.",closed,c.Trucks.Sum(t=>t.Paid)));
        else if (c == null || c.Abandoned)
        {
            RotationLine(w,ref y,L.K("workshop.rotation.rules","窗口最后一天仍可签约。在办合同可以跨窗交货；中止后仅能在原窗口续签，已付车款和原期限保留。",
                "Sign through the last day of the window. Active contracts can deliver later. Suspended jobs can resume only in their original window, with payments and deadlines retained."));
            if (q.CanSign && Ui.Button(new Rect(0,y,w,36),c == null ? L.K("workshop.overhaul.sign","按以上交期签约","Sign for these dates") :
                L.K("workshop.overhaul.resume","按原合同续办","Resume original contract"),_theme.NavOnStyle))
            {
                var result=q.Sign();
                if (result!=SaleStatus.Ready) Toast(SaleMessage(result));
                RefreshOverhaul();
            }
            y+=44;
        }
        else if (q.TruckIndex < q.Specs.Length)
        {
            var receipt=c.Trucks[q.TruckIndex];
            RotationLine(w,ref y,L.F("workshop.overhaul.quote","当前第 {0} 车 · 本车应付 {1} · 已收货 {2}/{3}",
                "Truck {0} · amount due {1} · collected {2}/{3}",q.TruckIndex+1,q.Amount,receipt.Received.Sum(),q.Specs[q.TruckIndex].Quantities.Sum()));
            y=DrawLoadPicker(w,y,q);
        }
        _rotationHeight=y+16;
    }

    private static void ResetReturnDesk()
    {
        _returnContracts=Array.Empty<RotationContract>();
        _returnStock=Array.Empty<GameItem>();
        _returnNames=Array.Empty<string>();
        _returnSerial=_returnSource=0;
        _rotationHeight=650;
    }

    private static void RefreshReturnDesk()
    {
        if (_rotation == null) { ResetReturnDesk(); return; }
        _returnContracts=RepairReturns.Pending();
        var selected=_returnContracts.FirstOrDefault(c=>c.Serial==_returnSerial) ?? _returnContracts.FirstOrDefault();
        _returnSerial=selected?.Serial ?? 0;
        _returnStock=selected==null ? Array.Empty<GameItem>() : RepairReturns.Selectable(selected);
        _returnNames=_returnStock.Select(item=>
        {
            string name=SaleSourceName(new WorkshopSaleStock(item,false));
            return IndustrialGoods.TryReadOrigin(item,out _,out _,out int slot) ? name+" #"+(slot+1) : name;
        }).ToArray();
        _returnSource=0;
    }

    private static void ReportReturn(RepairResult result)
    {
        string text=result switch
        {
            RepairResult.Done => L.K("workshop.rotation.recorded","已按实际交接登记。","Handover recorded."),
            RepairResult.NoRoom => L.K("workshop.rotation.noRoom","请在自己一侧的柜台腾出位置；未交齐的旧件留在原处。","Clear space on your side of the counter. Unreturned parts stay where they are."),
            RepairResult.Frozen => L.K("workshop.rotation.frozen","交接记录待核，已暂停本件。","Handover on hold pending reconciliation."),
            _ => L.K("workshop.rotation.changed","物品或合同状态已变，请重新选择。","Stock or contract changed. Select again.")
        };
        Toast(text);
        RefreshOverhaul();
    }

    private static void DrawReturns(float w)
    {
        float y=0;
        var records=_returnContracts;
        var c=records.FirstOrDefault(row=>row.Serial==_returnSerial);
        if (c==null)
        {
            RotationLine(w,ref y,L.K("workshop.rotation.noReturns","暂无待办退件。签下水站返修后，可在这里领壳；已中止合同的未归还旧件也可在这里交回。",
                "No returns are pending. Sign a waterworks repair contract to collect shells here. Outstanding shells from suspended contracts can also be returned here."));
            _rotationHeight=y+12;
            return;
        }
        if (Ui.Button(new Rect(0,y,w,34),L.F("workshop.rotation.selectReturn","返修合同 #{0} · 签约日 {1} · 共 {2} 份待办",
            "Repair #{0} · signed on day {1} · {2} pending contracts",c.Serial,c.SignedDay,records.Count) + (records.Count>1 ? "   ›" : ""),_theme.BtnStyle) && records.Count>1)
        {
            int pos=Array.IndexOf(records.ToArray(),c);
            _returnSerial=records[(pos+1)%records.Count].Serial;
            RefreshReturnDesk();
            return;
        }
        y+=43;
        RotationLine(w,ref y,L.F("workshop.rotation.balance","已领 {0}/2 · 交货核销 {1} · 已退 {2}。只接收本档、本合同对应的实物。",
            "Issued {0}/2 · shipped {1} · returned {2}. Only physical items from this save and this contract qualify.",
            c.Shells.Count(s=>s.Issued),c.Shells.Count(s=>s.Delivered),c.Shells.Count(s=>s.Returned)));
        for (int i=0;i<c.Shells.Length;i++)
            if (c.Shells[i].InFlight)
                RotationLine(w,ref y,L.F("workshop.rotation.frozenSlot","壳位 {0} 的上次交接待核，已暂停该壳位。","Handover for shell {0} is on hold pending reconciliation.",i+1));
        int left=RepairReturns.Unclaimed(c);
        if (left>0 && Ui.Button(new Rect(0,y,w,34),L.F("workshop.rotation.claim","领取一只旧滤壳 · 仍可领 {0} 只","Collect one used shell · {0} available",left),_theme.BtnStyle))
        { ReportReturn(RepairReturns.Claim(c)); return; }
        if (left>0) y+=43;
        RotationLine(w,ref y,L.K("workshop.rotation.refundRule","可以退原壳、净壳或带号总成。退总成时返还滤芯坯、衬层、眠苔胶各一件到自己一侧柜台；退壳不发货款。",
            "Return a used shell, a cleaned shell or its tagged assembly. Returning an assembly puts one blank, liner and gel on your counter. Shell returns pay no cash."));
        if (_returnStock.Length>0)
        {
            if (Ui.Button(new Rect(0,y,w,34),_returnNames[_returnSource] + "   ›",_theme.BtnStyle))
                _returnSource=(_returnSource+1)%_returnStock.Length;
            y+=41;
            if (Ui.Button(new Rect(0,y,w,34),L.K("workshop.rotation.returnSelected","归还所选旧件","Return selected item"),_theme.NavOnStyle))
            { ReportReturn(RepairReturns.Return(c,_returnStock[_returnSource])); return; }
            y+=41;
        }
        else if (c.Shells.Any(s=>s.Issued && !s.Returned && !s.Delivered && !s.InFlight))
            RotationLine(w,ref y,L.K("workshop.rotation.noStock","店内暂无可归还的该单旧件。移动物品后可刷新库存。","No matching returnable item in shop stock. Refresh after moving goods."));
        if (c.LiningInFlight)
            RotationLine(w,ref y,L.K("workshop.rotation.frozenLining","炉衬的上次签收待核，已暂停领取。","The last lining handover is on hold pending reconciliation."));
        else if (RepairReturns.LiningDue(c))
        {
            if (Ui.Button(new Rect(0,y,w,34),L.K("workshop.rotation.lining","签收返修回收炉衬","Collect recovered furnace lining"),_theme.BtnStyle))
            { ReportReturn(RepairReturns.ClaimLining(c)); return; }
            y+=42;
        }
        _rotationHeight=y+16;
    }
}
