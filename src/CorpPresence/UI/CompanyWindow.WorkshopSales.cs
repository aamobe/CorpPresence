using System;
using System.Linq;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
    private static WorkshopSaleQuote _saleQuote;
    private static void ResetWorkshopSales()
    {
        _saleQuote = null;
        ResetRodInspection();
    }

    private static string SaleName(WorkshopSaleOffer offer) => offer.IsPowder
        ? PowderBox.ComponentName((PowderComponent)offer.Component)
        : offer.Id switch
        {
            "pump_contact" => Goods.NameOf(IndustrialGoods.Contact),
            "wire" => L.K("workshop.sales.wire", "电线", "Wire"),
            "silicon_plate" => Goods.NameOf("silicon_plate"),
            "first_gel" or "cartel_gel" => Goods.NameOf(CartelReactor.Gel),
            "cartel_precipitate" => Goods.NameOf(CartelReactor.Crystal),
            "cartel_fine_grit" => Goods.NameOf(ClassifierGoods.Fine),
            _ => Goods.NameOf("hearth_tile")
        };

    private static string SaleMessage(SaleStatus status) => status switch
    {
        SaleStatus.Unavailable => L.K("workshop.sales.unavailable", "当前店铺或来访进度无法交货。", "Delivery is unavailable in this shop or at this story stage."),
        SaleStatus.DailyLimit => L.K("workshop.sales.dailyLimit", "这类收购今天已经结清。", "Today's order in this category has been paid."),
        SaleStatus.Outstanding => L.K("workshop.sales.outstanding", "请先结清这类收购的上一张凭单。", "Finish the outstanding order in this category first."),
        SaleStatus.Frozen => L.K("workshop.sales.frozen", "凭单的实收结果无法核实，已暂停结算。", "This receipt is on hold because the last transfer could not be verified."),
        SaleStatus.Short => L.K("workshop.sales.short", "所选货物不足，请补齐后刷新。", "The selected stock is short. Restock and refresh."),
        SaleStatus.Changed => L.K("workshop.sales.changed", "库存或凭单有变，请刷新报价。", "Stock or receipt changed. Refresh the quote."),
        SaleStatus.WalletFull => L.K("workshop.sales.walletFull", "钱包已接近上限，请腾出额度再结款。", "Make room in your wallet to receive payment."),
        SaleStatus.Paid => L.K("workshop.sales.paid", "货已收齐，货款已到账。", "Delivery complete. Payment received."),
        SaleStatus.Partial => L.K("workshop.sales.partial", "凭单已保留，下次只结剩余货物或货款。", "Receipt retained. Next time only the balance is settled."),
        SaleStatus.TrialSettled => L.K("workshop.shizuka.trialSettled", "首单已结清，可以补订酒精制备站。", "Trial paid. The alcohol preparation station is now available to order."),
        _ => ""
    };

    private static void DrawWorkshopSales(Rect r)
    {
        const float row = 36f;
        float y = r.y;
        GUI.Label(new Rect(r.x, y, r.width, row), L.K("workshop.sales.products", "成品小单 · 每营业日任选一份", "Finished goods · one order per business day"), _theme.Body);
        if (LowerMachinist.MineralizerUnlocked(CorpState.Current) && Ui.Button(new Rect(r.xMax - 310, y, 310, 32),
            L.K("workshop.overhaul.repairSale", "机修日单 · 2 触片换 76 元", "Repair desk daily · 2 contacts for 76"),_theme.BtnStyle))
            _saleQuote = new WorkshopSaleQuote(WorkshopSaleOffer.RepairContacts);
        y += row;
        for (int i = 0; i < 3; i++) SaleOfferButton(r.x + i * r.width / 3f, y, r.width / 3f - 4f, WorkshopSaleOffer.All[i]);
        y += row + 8f;
        GUI.Label(new Rect(r.x, y, r.width, row), L.K("workshop.sales.powders", "散料收购 · 二访后每天另选一份，单盒纯度 ≥81%，≥99% 加价 25%", "Loose powder · one more order daily after the second visit. One box at ≥81%, +25% at ≥99%."), _theme.Body);
        y += row;
        bool enabled = GUI.enabled;
        GUI.enabled = enabled && LowerMachinist.MineralizerUnlocked(CorpState.Current);
        for (int i = 3; i < WorkshopSaleOffer.All.Length; i++)
            SaleOfferButton(r.x + (i - 3) * r.width / 5f, y, r.width / 5f - 4f, WorkshopSaleOffer.All[i]);
        GUI.enabled = enabled;
        y += row + 8f;
        var sales = CorpState.Current?.Workshop?.Sales;
        DrawSaleReceipt(r, ref y, sales?.Product);
        DrawSaleReceipt(r, ref y, sales?.Powder);
        DrawSaleReceipt(r, ref y, sales?.Contacts);
        DrawSaleQuote(r, y);
    }

    private static void DrawShizukaGelOrder(Rect r)
    {
        const float row = 36f;
        float y = r.y;
        var root = CorpState.Current;
        bool settled = WorkshopSaleOffer.GelOrderSettled(root);
        GUI.Label(new Rect(r.x, y, r.width, 48f), settled ? SaleMessage(SaleStatus.TrialSettled) :
            L.K("workshop.shizuka.trial", "蓝胆试单 · 用 1 菌块和 1 树脂制胶，交清后开放酒精制备站", "Blue vessel trial · Make gel with one moss block and one resin. Full payment unlocks the alcohol station."), _theme.Wrap);
        y += 48f;
        if (!settled)
        {
            SaleOfferButton(r.x, y, r.width, WorkshopSaleOffer.FirstGel);
            y += row;
        }
        DrawSaleReceipt(r, ref y, root?.Workshop?.GelOrder);
        GUI.Label(new Rect(r.x, y, r.width, row), L.K("workshop.shizuka.stockSales", "现货收购 · 每营业日任选一份", "Stock orders · choose one per business day"), _theme.Body);
        y += row;
        bool enabled = GUI.enabled;
        float width = r.width / WorkshopSaleOffer.CartelDaily.Length;
        for (int i = 0; i < WorkshopSaleOffer.CartelDaily.Length; i++)
        {
            var offer = WorkshopSaleOffer.CartelDaily[i];
            GUI.enabled = enabled && WorkshopSaleQuote.Unlocked(offer, root);
            SaleOfferButton(r.x + i * width, y, width - 4f, offer);
        }
        GUI.enabled = enabled;
        y += row;
        if (!CartelShizuka.BlueprintDelivered(root) || !LowerMachinist.MineralizerUnlocked(root))
        {
            GUI.Label(new Rect(r.x, y, r.width, 48f), L.K("workshop.shizuka.stockGates", "析晶单在收到光盐蓝图后开放，细砂单在阿岑开放分级机目录后开放。", "Precipitate orders open with the light salt blueprint. Fine grit orders open with Cen’s classifier catalog."), _theme.Wrap);
            y += 48f;
        }
        DrawSaleReceipt(r, ref y, root?.Workshop?.CartelDaily);
        DrawSaleQuote(r, y);
    }

    private static void DrawSaleReceipt(Rect r, ref float y, WorkshopSaleReceipt receipt)
    {
        if (receipt == null || receipt.Complete && receipt.QuotaDay < Day) return;
        var offer = WorkshopSaleOffer.Find(receipt.Offer);
        if (offer == null) return;
        string line = L.F("workshop.sales.receipt", "第 {0} 天 · {1} · 收货 {2}/{3} · 实付 {4}/{5}₵",
            "Day {0} · {1} · collected {2}/{3} · paid {4}/{5}₵", receipt.QuotaDay, SaleName(offer), receipt.Received, offer.Quantity, receipt.Paid, receipt.Amount);
        GUI.Label(new Rect(r.x, y, r.width, 24f), line, _theme.Small);
        y += 24f;
    }

    private static void DrawSaleQuote(Rect r, float y)
    {
        const float row = 36f;
        var quote = _saleQuote;
        if (quote == null) return;
        GUI.Label(new Rect(r.x, y, r.width - 145f, row), L.F("workshop.sales.quote", "{0} · 本单 {1}₵", "{0} · quote {1}₵", SaleName(quote.Offer), quote.Amount), _theme.Body);
        if (Ui.Button(new Rect(r.xMax - 142f, y, 140f, row - 3f), L.K("workshop.sales.refresh", "刷新库存", "Refresh stock"), _theme.BtnStyle))
        {
            _saleQuote = new WorkshopSaleQuote(quote.Offer);
            return;
        }
        y += row;
        if (quote.Availability() == SaleStatus.Ready)
        {
            for (int i = 0; i < quote.Sources.Length; i++)
            {
                var source = quote.Selected(i);
                string label = source == null ? SaleMessage(SaleStatus.Short) :
                    L.F("workshop.sales.source", "货源 {0}/{1} · {2} · 库存 {3}", "Source {0}/{1} · {2} · stock {3}",
                        quote.Sources[i] + 1, quote.Stock.Count, SaleSourceName(source),
                        quote.Offer.IsPowder ? string.Join("/", source.Parts) : source.Removable.ToString());
                if (Ui.Button(new Rect(r.x, y, r.width, row - 3f), label + "  ›", _theme.BtnStyle) && quote.Stock.Count > 0)
                    quote.Sources[i] = (quote.Sources[i] + 1) % quote.Stock.Count;
                y += row;
            }
            if (quote.RemainingParts() is int[] after)
            {
                int left = after.Sum();
                decimal percent = left == 0 ? 0 : after[quote.Offer.Component] * 10000 / left / 100m;
                GUI.Label(new Rect(r.x, y, r.width, row), L.F("workshop.sales.after", "交货后（金／硅／稀／导／渣） {0} · 本成分 {1}%",
                    "After delivery (metal/silica/rare/conductive/residue) {0} · selected component {1}%", string.Join("/", after), percent), _theme.Small);
                y += row;
            }
        }
        var status = quote.Check();
        if (status != SaleStatus.Ready) GUI.Label(new Rect(r.x, y, r.width, row), SaleMessage(status), _theme.Small);
        else if (Ui.Button(new Rect(r.x, y, r.width, row), L.K("workshop.sales.confirm", "确认交货并结款", "Confirm delivery and payment"), _theme.BtnStyle))
        {
            Toast(SaleMessage(quote.Submit()));
            _saleQuote = new WorkshopSaleQuote(quote.Offer);
        }
    }

    private static void SaleOfferButton(float x, float y, float w, WorkshopSaleOffer offer)
    {
        string label = L.F("workshop.sales.offer", "{0} ×{1} · {2}₵", "{0} ×{1} · {2}₵", SaleName(offer), offer.Quantity, offer.Price);
        if (Ui.Button(new Rect(x, y, w, 32f), label, _theme.BtnStyle)) _saleQuote = new WorkshopSaleQuote(offer);
    }

    private static string SaleSourceName(WorkshopSaleStock stock)
    {
        string name = stock.Item.GetDisplayName(preferCustomName: true);
        var container = stock.Inventory.GetParentItem();
        if (container != null) name = container.GetDisplayName(preferCustomName: true) + " / " + name;
        if (stock.Inventory is GameGridInventory && stock.Item.modifiedShape is GridShape shape)
            name += $" ({shape.minX + 1}, {shape.minY + 1})";
        return name;
    }
}
