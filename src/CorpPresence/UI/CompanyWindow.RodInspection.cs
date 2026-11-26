using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
    private static List<GameItem> _inspectionRods = new();
    private static GameItem _inspectionRod;
    private static Vector2 _inspectionScroll;
    private static WorkshopSaleQuote _rodSaleQuote;

    private static void ResetRodInspection()
    {
        _inspectionRods.Clear();
        _inspectionRod = null;
        _inspectionScroll = Vector2.zero;
        _rodSaleQuote = null;
    }

    private static bool InspectableRod(GameItem item) =>
        (RodMaterial.IsRod(item?.identifier) || RodMaterial.IsFormedStock(item?.identifier))
        && item.unitCount == 1 && GeneralHelper.IsItemOwned(item)
        && !StorePlacement.Destroyed(item) && ShopInventory.Contains(item);

    private static void RefreshRodInspection()
    {
        _inspectionRods = EmporiumEntry.Instance?.GetAllOwnedItems(includeAfterhour: false)
            .Where(InspectableRod).Distinct().ToList() ?? new();
        if (!_inspectionRods.Contains(_inspectionRod)) _inspectionRod = _inspectionRods.FirstOrDefault();
        _rodSaleQuote = null;
    }

    private static void DrawRodInspection(Rect r)
    {
        if (!LowerMachinist.MineralizerUnlocked(CorpState.Current))
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 45), L.T("阿岑二访成交后开放验货", "Inspection opens after Cen's second sale"), _theme.Wrap);
            return;
        }
        if (Ui.Button(new Rect(r.x, r.y, 150, 32), L.K("workshop.sales.refresh", "刷新库存", "Refresh stock"), _theme.BtnStyle))
            RefreshRodInspection();
        if (_inspectionRods.Count > 1)
        {
            int index = _inspectionRods.IndexOf(_inspectionRod);
            string next = L.T($"送验实物 {index + 1}/{_inspectionRods.Count}  ›", $"Inspect item {index + 1}/{_inspectionRods.Count}  ›");
            if (Ui.Button(new Rect(r.x + 158, r.y, r.width - 158, 32), next, _theme.BtnStyle))
            {
                _inspectionRod = _inspectionRods[(index + 1) % _inspectionRods.Count];
                _rodSaleQuote = null;
            }
        }
        float top = r.y + 40;
        var receipt = CorpState.Current?.Workshop?.Sales?.Rods;
        if (receipt != null && (!receipt.Complete || receipt.QuotaDay >= Day))
        {
            string due = L.T($"棒料和零件凭单 · 已收 {receipt.Received}/1 · 已付 {receipt.Paid}/{receipt.Amount}₵",
                $"Alloy stock receipt · received {receipt.Received}/1 · paid {receipt.Paid}/{receipt.Amount}₵");
            GUI.Label(new Rect(r.x, top, r.width, 30), due, _theme.Wrap);
            top += 32;
            if (!receipt.Complete && receipt.Received == 1 && WorkshopSaleOffer.Find(receipt.Offer) is { } pending
                && Ui.Button(new Rect(r.x, top, r.width, 32), L.T("继续结算凭单余款", "Pay the outstanding balance"), _theme.BtnStyle))
                SubmitRodSale(new WorkshopSaleQuote(pending));
            if (!receipt.Complete && receipt.Received == 1) top += 36;
        }
        var area = new Rect(r.x, top, r.width, Mathf.Max(1, r.yMax - top));
        if (!InspectableRod(_inspectionRod))
        {
            GUI.Label(area, L.T("将自有棒料或毛坯放到店内，再刷新库存", "Place owned alloy stock in the shop and refresh"), _theme.Wrap);
            return;
        }
        var condition = RodCondition.Read(_inspectionRod);
        if (condition == null)
        {
            GUI.Label(area, L.T("这根棒料的材料档案或工艺履历无法读取", "This rod's material record or process cannot be read"), _theme.Wrap);
            return;
        }
        bool layered = condition.Jacket != null;
        bool splitReadings = layered || condition.Core != condition.Surface;
        const float summaryStart = 76;
        float layerHeight = layered ? 88 : 0;
        float quoteTop = (splitReadings ? 335 : 290) + layerHeight;
        var offers = RodMaterial.IsRod(_inspectionRod.identifier) ? WorkshopSaleOffer.RodOrders
            : WorkshopSaleOffer.PartOrders.Where(order => order.ItemId == _inspectionRod.identifier).ToArray();
        Rect scroll = new(0, 0, area.width - 20, quoteTop + (offers.Length > 0 ? offers.Length * 38 + 76 : 0));
        _inspectionScroll = Ui.BeginPanelScrollView(area, _inspectionScroll, scroll);
        GUI.Label(new Rect(0, 0, scroll.width, 28), SaleSourceName(new WorkshopSaleStock(_inspectionRod, false)), _theme.Body);
        GUI.Label(new Rect(0, 29, scroll.width, 44), MineralNightReport.Order + "\n" + Mix(condition.Material), _theme.Wrap);
        if (layered)
        {
            var jacket = condition.Jacket;
            string layers = L.F("workshop.clad.inspectCore", "芯 {0:0.#} 点 · {1}\n{2}", "Core {0:0.#} points · {1}\n{2}",
                condition.CoreMaterial.PowderPoints, SinteringStation.RodProcessName(condition.Process), Mix(condition.CoreMaterial))
                + "\n" + L.F("workshop.clad.inspectJacket", "外套 {0:0.#} 点 · {1}\n{2}", "Jacket {0:0.#} points · {1}\n{2}",
                    jacket.Material.PowderPoints, SinteringStation.RodProcessName(jacket.Process), Mix(jacket.Material));
            GUI.Label(new Rect(0, summaryStart, scroll.width, layerHeight), layers, _theme.Wrap);
        }
        string readings = SinteringStation.ExplainRod(condition);
        if (_inspectionRod.identifier == "corp_presence_" + RodMaterial.FeedSleeve)
            readings += "\n" + (condition.Scored
                ? L.K("workshop.pumpRepair.scoredStock", "已拉伤 · 须换件", "Scored · replacement required")
                : SleeveOiling.Oil(_inspectionRod) == 0 ? L.T("干套", "Dry sleeve") : L.T("已浸油 · 1 份", "Oiled · 1 portion"));
        GUI.Label(new Rect(0, summaryStart + layerHeight, scroll.width, 132), readings, _theme.Wrap);
        GUI.Label(new Rect(0, 210 + layerHeight, scroll.width, 30), L.T("组织 · ", "Structure · ") + InspectionTraits(condition.Traits), _theme.Wrap);
        if (splitReadings)
        {
            GUI.Label(new Rect(0, 242 + layerHeight, scroll.width, 42), L.T("表层标签 · ", "Surface labels · ") + InspectionLabels(condition.Labels(condition.Surface)), _theme.Wrap);
            GUI.Label(new Rect(0, 285 + layerHeight, scroll.width, 42), L.T("芯部标签 · ", "Core labels · ") + InspectionLabels(condition.Labels(condition.Core)), _theme.Wrap);
        }
        else GUI.Label(new Rect(0, 242, scroll.width, 42), L.T("材料标签 · ", "Material labels · ") + InspectionLabels(condition.Labels(condition.Core)), _theme.Wrap);
        if (offers.Length > 0) DrawRodOffers(scroll.width, quoteTop, condition, offers);
        GUI.EndScrollView();

        static string Mix(RodMaterial material) => string.Join(" / ", material.Parts().Select(n => (n / 2m).ToString("0.#")));
    }

    private static void DrawRodOffers(float width, float top, RodCondition condition, WorkshopSaleOffer[] offers)
    {
        foreach (var offer in offers)
        {
            string label = RodOfferName(offer);
            bool eligible = WorkshopSaleQuote.RodQualifies(offer, condition);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && eligible;
            if (Ui.Button(new Rect(0, top, width, 34), label, _theme.BtnStyle))
                _rodSaleQuote = new WorkshopSaleQuote(offer, _inspectionRod);
            GUI.enabled = wasEnabled;
            top += 38;
        }
        var quote = _rodSaleQuote;
        if (quote == null) return;
        var status = quote.Check();
        string name = RodOfferName(quote.Offer);
        GUI.Label(new Rect(0, top, width, 33),
            L.T($"{name} · 报价 {quote.Amount}₵", $"{name} · Quote {quote.Amount}₵"), _theme.Wrap);
        top += 35;
        if (status != SaleStatus.Ready)
            GUI.Label(new Rect(0, top, width, 34), SaleMessage(status), _theme.Wrap);
        else if (Ui.Button(new Rect(0, top, width, 34), L.K("workshop.sales.confirm", "确认交货并结款", "Confirm delivery and payment"), _theme.BtnStyle))
            SubmitRodSale(quote);
    }

    private static void SubmitRodSale(WorkshopSaleQuote quote)
    {
        Toast(SaleMessage(quote.Submit()));
        RefreshRodInspection();
    }

    private static string RodOfferName(WorkshopSaleOffer offer) =>
        offer.ItemId != null ? Goods.NameOf(offer.ItemId.Substring("corp_presence_".Length)) + " · " + (offer.Id switch
        {
            "part_feed_shaft" => L.T("耐疲劳、承力 ≥50", "Fatigue resistant, load ≥50"),
            "part_industrial_coil" => L.T("低损、脆裂 ≤2", "Low-loss, brittleness ≤2"),
            _ => L.T("规格收购", "Specification order")
        })
        : offer == WorkshopSaleOffer.FatigueRod ? L.T("轴坯收购 · 耐疲劳", "Shaft stock · Fatigue resistant")
        : offer == WorkshopSaleOffer.HardRod ? L.T("硬料收购 · 表层硬度 ≥80", "Hard stock · Surface hardness ≥80")
        : offer == WorkshopSaleOffer.ConductorRod ? L.T("导体收购 · 低损或定向", "Conductors · Low-loss or axial")
        : L.T("整棒收购 · 至少 18 粉点", "Whole rods · At least 18 powder points");

    private static string InspectionTraits(RodTrait traits) => traits == 0 ? L.T("暂无", "None") : string.Join(" · ",
        Enum.GetValues(typeof(RodTrait)).Cast<RodTrait>().Where(trait => traits.HasFlag(trait)).Select(trait => trait switch
        {
            RodTrait.LoadBearing => L.T("承载骨架", "Load bearing"),
            RodTrait.Fatigue => L.T("耐疲劳", "Fatigue resistant"),
            RodTrait.Impact => L.T("抗冲击", "Impact resistant"),
            RodTrait.LowLoss => L.T("低损导电", "Low-loss conductor"),
            RodTrait.AxialConductor => L.T("定向导电", "Axial conductor"),
            RodTrait.DimensionStable => L.K("workshop.rod.dimensionStable", "尺寸稳定", "Dimensionally stable"),
            _ => L.T("硬质齿面", "Hardened tooth face")
        }));

    private static string InspectionLabels(RodLabel labels) => labels == 0 ? L.T("暂无", "None") : string.Join(" · ",
        Enum.GetValues(typeof(RodLabel)).Cast<RodLabel>().Where(label => labels.HasFlag(label)).Select(label => label switch
        {
            RodLabel.HighHardness => L.T("高硬度", "High hardness"),
            RodLabel.HighLoad => L.T("高承力", "High load"),
            RodLabel.HighWear => L.T("高耐磨", "High wear resistance"),
            RodLabel.HighConductivity => L.T("高导电", "High conductivity"),
            RodLabel.HighAxialConductivity => L.T("沿向高导电", "High axial conductivity"),
            RodLabel.HighCorrosion => L.T("高耐蚀", "High corrosion resistance"),
            RodLabel.LowPorosity => L.T("低孔隙", "Low porosity"),
            RodLabel.OilPores => L.T("储油孔段", "Oil-retaining pores"),
            _ => L.T("易脆", "Brittle")
        }));
}
