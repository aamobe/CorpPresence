#if LOCAL_TEST_FUNDS
using UnityEngine;

namespace CorpPresence.Runtime;

internal static class LocalTestFunds
{
    private const int Credit = 10_000;

    internal static void Tick()
    {
        if (!Input.GetKeyDown(KeyCode.F1)) return;

        var ui = StoreUIManager.Instance;
        if (ui == null) return;
        string result = Apply();
        ModLog.Info("F1 " + result);
        ui.Notify(result);
    }

    internal static string Apply()
    {
        if (!GameResources.Ready || Settings.CleanupForUninstall)
            return "测试未执行：游戏未就绪，或已开启卸载清理。";
        var session = StoreSession.Capture();
        var store = SaveSlot.CurrentStore;
        if (!session.IsCurrent || store == null || !SaveSlot.CanWrite("CorpPresence") ||
            store.GetCash() > int.MaxValue - Credit)
            return "测试未执行：存档未就绪、不可写或信用点已接近上限。";

        var root = CorpState.Current;
        foreach (Company company in Companies.All)
        {
            if (company.TierNames.Length == 0) continue;
            var state = root.CompanyOf(company.Id);
            state.Coop = 100;
            state.CoopFraction = 0;
        }
        ModHelper.AddCredit(Credit, playSound: false);
        return "测试：信用点 +10,000，合作度已设为 100。";
    }
}
#endif
