#if LOCAL_PLAYTEST || LOCAL_WORKSHOP_TEST
using System;
using UnityEngine;

namespace CorpPresence.Runtime;

internal static class PlaytestStoryKeys
{
#if LOCAL_PLAYTEST
    private static readonly bool Isolated = Application.productName.StartsWith("CPT", StringComparison.Ordinal)
        && Array.IndexOf(Environment.GetCommandLineArgs(), "-corp-playtest") >= 0
        && Environment.GetEnvironmentVariable("CORP_TEST_SESSION")?.Length == 32;
#endif

    private static readonly RuntimeTask Cen = Story(LowerMachinist.CharacterId);
    private static readonly RuntimeTask Shizuka = Story(CartelShizuka.CharacterId);
    private static readonly RuntimeTask Rosen = Story("rosen");

    internal static void Tick()
    {
        if (StoreUIManager.Instance == null) return;
        bool enabled = false;
#if LOCAL_PLAYTEST
        enabled = Isolated;
#endif
#if LOCAL_WORKSHOP_TEST
        bool workshop = SaveSlot.CurrentStore?.runID == WorkshopTestRun.Id;
        enabled |= workshop;
        if (workshop && Input.GetKeyDown(KeyCode.F5)) WorkshopTestPack.Grant.Run(Time.unscaledTime);
#endif
        if (!enabled) return;
        float now = Time.unscaledTime;
        if (Input.GetKeyDown(KeyCode.F1)) Cen.Run(now);
        if (Input.GetKeyDown(KeyCode.F2)) Shizuka.Run(now);
        if (Input.GetKeyDown(KeyCode.F3)) Rosen.Run(now);
    }

    private static RuntimeTask Story(string id) => new("测试剧情来访 " + id,
        () => StoreUIManager.Instance.Notify(Engine.BookPlaytestStory(id)),
        (name, error, _) => ModLog.Warn(name + "失败：" + error.Message));
}
#endif
