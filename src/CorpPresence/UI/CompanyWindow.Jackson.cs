using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
    private static void DrawJackson()
    {
        GUI.Label(new Rect(24f, 16f, _rect.width - 70f, 30f), _company.Name, _theme.Body);
        GUI.Label(new Rect(24f, 49f, _rect.width - 48f, 24f), _desk, _theme.Small);
        DrawEquipmentOrder(new Rect(24f, 83f, _rect.width - 48f, _rect.height - 107f),
            _company.Catalog, L.T("工作间自制机器与改装配件，次日送达。", "Workshop-built machines & custom parts. Delivery tomorrow."),
            static entry => entry.Quote(1.0), L.T("确认订购", "Place order"));
    }
}
