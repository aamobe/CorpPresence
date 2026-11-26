using System.Linq;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
    private static void WorkshopBar(Rect r, long done, long total, Color color, long minimum = 0)
    {
        Ui.Fill(r, _theme.Paper);
        Ui.Fill(new Rect(r.x, r.y, r.width, 1), _theme.Line);
        if (total <= 0 || done <= 0) return;
        float width = r.width * (float)System.Math.Clamp((double)done / total, 0, 1);
        Ui.Fill(new Rect(r.x, r.y + 1, width, r.height - 1), color);
        if (minimum > 0 && minimum < done)
        {
            float guaranteed = width * minimum / done;
            Ui.Fill(new Rect(r.x, r.y + 1, width, r.height - 1), _theme.Accent2);
            Ui.Fill(new Rect(r.x, r.y + 1, guaranteed, r.height - 1), color);
        }
    }

    private static string PowderLabel(int i) => i == 5
        ? L.K("workshop.cen.tailingsBin", "回收粉", "Tailings") : PowderBox.ComponentName((PowderComponent)i);

    private static Color PowderInk(int i) =>
        i >= PowderColor.Palette.Length ? _theme.Accent2 : Tex.Hex(PowderColor.Palette[i]);

    private static void WorkshopMetric(Rect r, string label, string value)
    {
        Ui.Fill(r, _theme.Paper);
        GUI.Label(new Rect(r.x + 6, r.y + 2, r.width - 12, 25), label, _theme.Tight);
        GUI.Label(new Rect(r.x + 4, r.y + 24, r.width - 8, 22), value, _theme.Price);
    }

    private static void DrawWorkshopStockRow(float w, ref float y, string label, long required, long available)
    {
        float name = w * .41f;
        float values = Mathf.Min(118, w * .24f);
        GUI.Label(new Rect(0, y, name - 8, 26), label, _theme.Body);
        WorkshopBar(new Rect(name, y + 8, w - name - values, 10), available, required,
            available >= required ? _theme.Accent : _theme.Hot);
        GUI.Label(new Rect(w - values, y, values, 26), $"{available} / {required}", _theme.Price);
        y += 25f;
    }

    private static float DrawTruckCard(float w, float y, int index, TruckSpec spec, OverhaulTruck receipt, int day, bool current)
    {
        int received = receipt?.Received.Sum() ?? 0;
        int total = spec.Quantities.Sum();
        long due = receipt?.DueDay ?? (long)day + spec.Days;
        float height = 64 + spec.ItemIds.Length * 25;
        Ui.Fill(new Rect(0, y, w, height), _theme.Paper);
        Ui.Fill(new Rect(0, y, 3, height), current ? _theme.Accent : _theme.Line);
        GUI.Label(new Rect(12, y + 4, w * .32f, 28),
            (receipt?.CompletedDay != null ? "✓ " : current ? "› " : "") +
            L.F("workshop.chart.truck", "第 {0} 车", "Truck {0}", index + 1), _theme.Body);
        GUI.Label(new Rect(w * .36f, y + 4, w * .62f, 28),
            L.F("workshop.chart.due", "第 {0} 天关店前", "Due by closing, day {0}", due), _theme.Center);
        y += 33;
        for (int i = 0; i < spec.ItemIds.Length; i++)
        {
            var key = spec.ItemIds[i].Substring("corp_presence_".Length);
            int count = receipt?.Received[i] ?? 0;
            GUI.Label(new Rect(12, y, w * .47f - 12, 24), Goods.NameOf(key), _theme.Body);
            WorkshopBar(new Rect(w * .48f, y + 8, w * .31f, 9), count, spec.Quantities[i], _theme.Accent);
            GUI.Label(new Rect(w * .80f, y, w * .18f, 24), $"{count} / {spec.Quantities[i]}", _theme.Price);
            y += 25;
        }
        GUI.Label(new Rect(12, y + 2, w * .48f - 11, 25),
            L.F("workshop.chart.price", "底价 {0}₵  ·  准时 +{1}₵", "Base {0}₵  ·  on time +{1}₵", spec.BasePrice, spec.Bonus), _theme.Small);
        GUI.Label(new Rect(w * .50f, y + 2, w * .48f, 25),
            L.F("workshop.chart.paid", "实收 {0}₵  ·  交货 {1}/{2}", "Paid {0}₵  ·  cargo {1}/{2}", receipt?.Paid ?? 0, received, total), _theme.Center);
        return y + 40;
    }
}
