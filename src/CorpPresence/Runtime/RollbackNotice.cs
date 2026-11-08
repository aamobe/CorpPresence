using System;
using UnityEngine;

namespace CorpPresence.Runtime;

// Only a startup failure notice. No water patches or global compatibility behavior.
internal sealed class RollbackNotice : MonoBehaviour
{
    private static RollbackNotice _pending;
    private float _deadline;

    public static void Queue()
    {
        try
        {
            if (_pending != null) return;
            var host = new GameObject("CorpPresence_RollbackNotice");
            UnityEngine.Object.DontDestroyOnLoad(host);
            _pending = host.AddComponent<RollbackNotice>();
            _pending._deadline = Time.realtimeSinceStartup + 30f;
        }
        catch (Exception error) { ModLog.Warn("无法安排公司启动失败提示：" + error); }
    }

    private void Update()
    {
        try
        {
            var ui = GenericConfirmationUIManager.Instance;
            if (ui != null && ui.isActiveAndEnabled && ui.panel != null && !ui.panel.activeSelf)
            {
                ui.OpenPanelSingleButton(
                    L.K("runtime.rollbackNotice.update.corpPresenceFailedInitializeAttemptedRoll", "公司增强初始化失败，已尝试撤销本次加载。请暂时禁用公司增强，并附上本次游戏日志报告问题；请保留原存档。", "CorpPresence failed to initialize and attempted to roll back this startup. Temporarily disable CorpPresence and report this session's game log. Keep your original save."),
                    L.K("runtime.rollbackNotice.update.oK", "确定", "OK"), () => ui.ClosePanel());
                UnityEngine.Object.Destroy(gameObject);
            }
            else if (Time.realtimeSinceStartup >= _deadline)
            {
                ModLog.Warn("公司启动失败提示面板未就绪，请查看本次初始化异常日志。");
                UnityEngine.Object.Destroy(gameObject);
            }
        }
        catch (Exception error)
        {
            UnityEngine.Object.Destroy(gameObject);
            ModLog.Warn("无法显示公司启动失败提示：" + error);
        }
    }
}
