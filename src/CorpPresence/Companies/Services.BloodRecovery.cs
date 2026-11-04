using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class Services
{
    public const int BloodRecoveryPrice = 62;
    public const int BloodRecoveryGap = 3;
    public const int BloodRecoveryLimit = 6;

    public static int BloodRecoveryNextDay(CompanyState state) =>
        state.BloodRecoveryLastDay < 0 ? Day : state.BloodRecoveryLastDay + BloodRecoveryGap;

    private static List<GameItem> BloodRecoverySamples()
    {
        var store = EmporiumEntry.Instance;
        if (store == null) return new List<GameItem>();
        return store.GetAllOwnedItems(includeAfterhour: false)
            .Where(CanRecoverBloodSample).Distinct().Take(BloodRecoveryLimit).ToList();
    }

    private static bool CanRecoverBloodSample(GameItem item)
    {
        if (!SilverBlood.IsRecoverySample(item)) return false;
        for (var container = item.parentInventory?.GetParentItem(); container != null;
             container = container.parentInventory?.GetParentItem())
        {
            if (container.IsGameItemType("MACHINE") || container.IsTag("STANDARD_MACHINE_TAG") ||
                container.IsTag("machinery") || container.IsTag("machinery_drawn_power"))
                return false;
        }
        return true;
    }

    public static int BloodRecoveryCount() => BloodRecoverySamples().Count;

    public static string RequestBloodRecovery(Company company, int expectedCount)
    {
        var state = Companies.StateOf(company);
        var store = PlayerStore.Instance;
        if (company is not CarmichaelCo || state?.Licensed != true || store == null)
            return L.T("需要卡迈克尔的养殖许可。", "A Carmicheal breeding license is required.");

        int day = Day;
        int nextDay = BloodRecoveryNextDay(state);
        if (day < nextDay)
            return L.T($"第 {nextDay} 天可再次回收。", $"Recovery opens again on day {nextDay}.");

        var samples = BloodRecoverySamples();
        if (samples.Count == 0)
            return L.T("店内没有符合条件的完整银血样品。", "There are no eligible full Silver Blood samples in the shop.");
        if (samples.Count != expectedCount)
            return L.T("样品数量有变化，请核对后重试。", "The sample count changed. Check the updated quote and try again.");
        if (store.GetCash() > int.MaxValue - samples.Count * BloodRecoveryPrice)
            return L.T("现金已接近上限，暂时无法结算。", "Cash is near its limit. Recovery cannot be settled.");

        int recovered = 0;
        foreach (var sample in samples)
        {
            if (TryCollectBloodSample(sample)) recovered++;
        }
        if (recovered == 0)
            return L.T("未收走样品，未开始冷却。", "No samples were collected. No cooldown started.");

        int payment = recovered * BloodRecoveryPrice;
        store.ModCash(payment, playSound: false);
        state.BloodRecoveryLastDay = day;
        ModLog.Info($"卡迈克尔银血回收：{recovered} 支，到账 {payment}₵，下次第 {day + BloodRecoveryGap} 天");
        return L.T($"已回收 {recovered} 支银血，到账 {payment}₵。", $"Collected {recovered} Silver Blood samples. Received {payment}₵.");
    }

    private static bool TryCollectBloodSample(GameItem sample)
    {
        try
        {
            if (!CanRecoverBloodSample(sample)) return false;
            sample.parentInventory.Expel(sample);
        }
        catch (Exception ex) { ModLog.Warn("移出银血样品失败：" + ex.Message); }

        // A late inventory callback can throw after the sample has already been detached.
        if (sample.parentInventory != null) return false;
        try { sample.Destroy(); }
        catch (Exception ex) { ModLog.Warn("清理已收走的银血样品失败：" + ex.Message); }
        return true;
    }
}
