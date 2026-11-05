using System;
using CorpPresence.Malice;
using CorpPresence.Runtime;
using CorpPresence.UI;
using UnityEngine;
using HarmonyLib;

namespace CorpPresence;

internal sealed class Host : MonoBehaviour
{
    private Harmony _pendingCompatibility;
    public void Configure(Harmony harmony) => _pendingCompatibility = harmony;
    private readonly RuntimeTask[] _updates = {
        new RuntimeTask("读档后公司物品修复", LoadedItemRepair.Tick, Report),
        new RuntimeTask("面板更新", CompanyWindow.Tick, Report),
        new RuntimeTask("记事簿", Notebook.Tick, Report),
        new RuntimeTask("中文后备字体", CjkFallback.Tick, Report),
        new RuntimeTask("配送箱清理", Crates.Tick, Report),
        new RuntimeTask("巨企通知", Envoy.Tick, Report),
        new RuntimeTask("立绘换档", Portraits.Tick, Report)
    };
    private readonly RuntimeTask _draw = new RuntimeTask("面板绘制", CompanyWindow.Draw, Report);

    private void Update()
    {
        // ModLoader loads DLLs and invokes OnEnable synchronously. The first
        // frame runs after all companions have loaded, unlike resource startup.
        if (_pendingCompatibility != null)
        {
            var harmony = _pendingCompatibility;
            _pendingCompatibility = null;
            NeonCompatibility.Install(harmony);
        }
        float now = Time.unscaledTime;
        for (int i = 0; i < _updates.Length; i++) _updates[i].Run(now);
#if TESTKIT
        TestKit.Tick();
#endif
    }

    private void OnGUI() => _draw.Run(Time.unscaledTime);

    private static void Report(string name, Exception error, int suppressed)
    {
        ModLog.Warn(name + " 出错：" + error.Message + (suppressed > 0 ? "（期间另有 " + suppressed + " 次任务异常）" : ""));
    }
}
