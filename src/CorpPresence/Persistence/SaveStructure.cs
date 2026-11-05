using System;
namespace CorpPresence;

// Validate required structure once after decoding. Missing older fields keep ctor defaults.
// Explicit null records are preserved on disk rather than replaced by empty state.
internal static class SaveStructure
{
    public static void Validate(CorpState state)
    {
        Require(state.Characters != null && state.Companies != null && state.Meds != null, "公司根状态集合缺失");
        if (state.Seline != null) Require(state.Seline.SideDone != null, "旧瑟琳支线记录缺失");
        foreach (var entry in state.Characters) Require(entry.Value != null && entry.Value.SideDone != null, "人物记录缺失：", entry.Key);
        foreach (var entry in state.Companies)
        {
            CompanyState company = entry.Value;
            Require(company != null, "公司记录缺失：", entry.Key);
            Require(company.Brand != null && company.Orders != null && company.LimitedBought != null && company.LabList != null && company.Visitors != null, "公司必要集合缺失：", entry.Key);
            foreach (Order order in company.Orders)
            {
                Require(order != null && order.Items != null, "配送明细缺失：", entry.Key);
                foreach (string item in order.Items) Require(item != null, "配送物品标识为空：", entry.Key);
            }
            foreach (Visitor visitor in company.Visitors) Require(visitor != null, "预约记录为空：", entry.Key);
        }
    }

    public static void Validate(Malice.MaliceState state)
    {
        Require(state.Corps != null && state.Envoy != null && state.Rebel != null, "巨企根状态集合缺失");
        foreach (var entry in state.Corps) Require(entry.Value != null, "巨企合约记录缺失：", entry.Key);
        Require(state.Envoy.Coins != null && state.Envoy.Visits != null, "特使必要集合缺失");
        foreach (var visit in state.Envoy.Visits) Require(visit != null, "特使预约记录为空");
    }

    private static void Require(bool valid, string message, string key = null)
    {
        if (!valid) throw new InvalidOperationException(key == null ? message : message + key);
    }
}

