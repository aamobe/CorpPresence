using System;
using CorpPresence.Runtime;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal sealed class Host : MonoBehaviour
{
    private readonly RuntimeTask[] _updates = {
        new RuntimeTask("读档后公司物品修复", LoadedItemRepair.Tick, Report),
        new RuntimeTask("面板更新", CompanyWindow.Tick, Report),
        new RuntimeTask("记事簿", Notebook.Tick, Report),
        new RuntimeTask("中文后备字体", CjkFallback.Tick, Report),
        new RuntimeTask("配送箱清理", Crates.Tick, Report),
        new RuntimeTask("机器状态贴图", MachineAppearance.Tick, Report),
        new RuntimeTask("模块任务", () => RuntimeUpdates.Run(Time.unscaledTime), Report),
        new RuntimeTask("立绘换档", Portraits.Tick, Report)
    };
    private readonly RuntimeTask _draw = new RuntimeTask("面板绘制", CompanyWindow.Draw, Report);
    private readonly RuntimeTask _drawCargoSort = new RuntimeTask("仓库分类面板", CargoSortWindow.Draw, Report);

    private void Update()
    {
        float now = Time.unscaledTime;
        for (int i = 0; i < _updates.Length; i++) _updates[i].Run(now);
#if MAINTENANCE
        MaintenanceConsole.Tick();
#endif
#if LOCAL_TEST_FUNDS
        LocalTestFunds.Tick();
#endif
#if TESTKIT
        TestKit.Tick();
#endif
    }

    private void OnGUI()
    {
        float now = Time.unscaledTime;
        // CompanyWindow uses explicit rectangles and consumes queued clicks on repaint.
        // CargoSortWindow has native scrolling controls and must still receive input events.
        if (Event.current != null && Event.current.type == EventType.Repaint) _draw.Run(now);
        _drawCargoSort.Run(now);
    }

    private static void Report(string name, Exception error, int suppressed)
    {
        ModLog.Warn(name + " 出错：" + error.Message + (suppressed > 0 ? "（期间另有 " + suppressed + " 次任务异常）" : ""));
    }
}
