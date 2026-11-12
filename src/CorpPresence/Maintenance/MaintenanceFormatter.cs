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
        text.Append("\n[维护] 公司送货和来访暂停=").Append(snapshot.CompanyProgressPaused)
            .Append(' ').Append(snapshot.InventoryLoadReport ?? "本次库存恢复未观测");
        if (snapshot.Modules.Count == 0) text.Append("\n[维护] 模块状态未知");
        foreach (var m in snapshot.Modules) text.Append("\n[维护] ").Append(m.Owner)
            .Append(" 安装已提交=").Append(m.Installed).Append(" 清理遇错=").Append(m.ReleaseFailed);
        foreach (var c in snapshot.Counters) text.Append("\n[维护] ").Append(c.Name).Append('=').Append(c.Value);
        text.Append("\n[维护] 事件登记仅包含 BeforeSave/DayStart/StateReset；其他委托未迁移。缓存/登记计数不代表场景实物数量。");
        foreach (var r in snapshot.Registrations) text.Append("\n[维护] ").Append(r.EventName).Append(" owner=").Append(r.Owner);
        foreach (var f in snapshot.Faults)
        {
            text.Append("\n[维护] ").Append(f.Scope == MaintenanceFaultScope.Runtime ? "运行期 " : "当前存档 ")
                .Append(f.Source).Append('/').Append(f.Stage).Append('/').Append(f.Code);
            if (f.ItemId != null) text.Append(" item=").Append(f.ItemId);
            text.Append(' ').Append(f.ExceptionType).Append(" x").Append(f.Count).Append(' ').Append(f.Detail);
        }
        return text.ToString();
    }
}
