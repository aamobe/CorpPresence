using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
    private static void DrawShizuka()
    {
        var root = CorpState.Current;
        if (!_workshopSession.IsCurrent || root?.Workshop == null || !CartelShizuka.Introduced(root))
        {
            Close();
            return;
        }
        GUI.Label(new Rect(24, 12, 530, 32), _company.DeskName + " · " + _company.Name, _theme.Title);
        string[] tabs = { L.K("workshop.shizuka.samplesTab", "试料领取", "Collect samples"),
            L.K("workshop.shizuka.suppliesTab", "补订", "Resupply"), L.K("workshop.shizuka.salesTab", "交货与结款", "Deliver and settle") };
        for (int i = 0; i < tabs.Length; i++)
            if (Ui.Button(new Rect(24 + i * 232, 55, 224, 34), tabs[i], _module == i ? _theme.NavOnStyle : _theme.BtnStyle))
                _module = i;
        var r = new Rect(24, 106, _rect.width - 48, _rect.height - 130);
        if (_module == 2) { DrawShizukaGelOrder(r); return; }
        if (_module == 1)
        {
            DrawEquipmentOrder(r, _company.Catalog, L.K("workshop.shizuka.orderNote", "每天可合订一单，次日送到你一侧的柜台。", "Combine supplies into one daily order, delivered to your side of the counter tomorrow."),
                entry => _company.OrderUnitPrice(entry, State), L.K("ui.jackson.overview.placeOrder", "确认订购", "Place order"));
            return;
        }
        GUI.Label(new Rect(r.x, r.y, r.width, 60), L.K("workshop.shizuka.kit", "首批试料：基础反应槽 1 台、眠苔菌块 2 块、蓝壳树脂 2 份。反应槽归你，试料箱可分次领取。",
            "First samples: one permanent reactor, two sleep moss blocks and two portions of blue shell resin. You can collect the crate in parts."), _theme.Wrap);
        GUI.Label(new Rect(r.x, r.y + 65, r.width, 52), ShizukaSupplies.Receipt(root.Workshop), _theme.Wrap);
        if (root.Workshop.ShizukaKitRemaining > 0 && Ui.Button(new Rect(r.x, r.y + 121, 260, 36),
            L.K("workshop.shizuka.claim", "领取剩余试料", "Collect remaining samples"), _theme.BtnStyle))
            Toast(ShizukaSupplies.Claim(_workshopSession));
        GUI.Label(new Rect(r.x, r.y + 178, r.width, 88), L.K("workshop.shizuka.power", "给反应槽装上有电的普通电池。在蓝胆放 1 份树脂、共用进料格放 1 块菌块，关店过夜耗 8 电，次日取胶。若已认识阿岑，可向下层互助补订 24 电电池。",
            "Fit a charged battery to the reactor. Put one resin in the blue vessel and one moss block in the shared feed. Close the shop for the night with 8 charge available and collect the gel next day. Once you know Cen, Lower Levels Mutual Aid sells 24-charge batteries."), _theme.Wrap);
    }
}
