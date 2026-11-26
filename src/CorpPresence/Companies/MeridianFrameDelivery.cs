using System;

namespace CorpPresence;

internal static class MeridianFrameDelivery
{
    internal const string FrameId = MeridianCommissionReceipt.FrameId;

    internal static void Deliver(CompanyState state, Order order, int day, Func<bool> current)
    {
        if (!current() || !state.Orders.Contains(order) || order.Kind != MeridianCommissionReceipt.OrderKind
            || order.Day > day || order.Items.Count is < 1 or > MeridianCommissionReceipt.Maximum
            || order.Items.Exists(id => id != FrameId)) return;

        order.Day = day == int.MaxValue ? day : day + 1;
        int count = order.Items.Count;
        int arrived = WorkshopCounter.DeliverCrate(count, count, _ => Goods.Make("pump_frame"), current, "crate_meridian");
        if (!current() || arrived <= 0 || arrived > count) return;
        order.Items.RemoveRange(0, arrived);
        if (order.Items.Count == 0) state.Orders.Remove(order);
    }
}
