using System.Text;
namespace CorpPresence;
internal static class MaintenanceFormatter
{
    internal static string Format(MaintenanceSnapshot snapshot)
    {
        var text = new StringBuilder("[维护] ");
        text.Append(snapshot.Version).Append(" 资源=").Append(snapshot.ResourcesReady).Append(" 代次=").Append(snapshot.Generation);
        text.Append(" 存档=").Append(snapshot.SaveStatus).Append(" 公司写保护=").Append(snapshot.CorpWriteProtected?.ToString() ?? "未知");
        text.Append(" 恶意记录写保护=").Append(snapshot.MaliceWriteProtected?.ToString() ?? "未知");
        text.Append(" 待配送订单=").Append(snapshot.PendingOrders?.ToString() ?? "未知");
        text.Append(" 剩余物品条目=").Append(snapshot.PendingItemUnits?.ToString() ?? "未知");
        foreach (var c in snapshot.Counters) text.Append("\n[维护] ").Append(c.Name).Append('=').Append(c.Value);
        text.Append("\n[维护] 事件登记：尚未接入；缓存/登记计数不代表场景实物数量。");
        foreach (var f in snapshot.Faults) text.Append("\n[维护] ").Append(f.Source).Append('/').Append(f.Stage).Append('/').Append(f.Code)
            .Append(' ').Append(f.ExceptionType).Append(" x").Append(f.Count).Append(' ').Append(f.Detail);
        return text.ToString();
    }
}
