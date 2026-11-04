using System;
using UnityEngine;

namespace CorpPresence.Runtime;

internal sealed class RollbackNotice : MonoBehaviour
{
    private static RollbackNotice _pending;
    private bool _fullRollback;
    private float _deadline;

    public static void Queue(bool fullRollback)
    {
        try
        {
            if (_pending == null)
            {
                var host = new GameObject("CorpPresence_RollbackNotice");
                DontDestroyOnLoad(host);
                _pending = host.AddComponent<RollbackNotice>();
                _pending._deadline = Time.realtimeSinceStartup + 30f;
            }
            _pending._fullRollback |= fullRollback;
        }
        catch (Exception error)
        {
            ModLog.Warn("无法安排回滚提示：" + error);
        }
    }

    private void Update()
    {
        try
        {
            var confirmation = GenericConfirmationUIManager.Instance;
            if (confirmation != null && confirmation.isActiveAndEnabled &&
                confirmation.panel != null && !confirmation.panel.activeSelf)
            {
                string message = _fullRollback
                    ? L.T("模组「公司增强 CorpPresence」触发回滚。请暂时禁用该模组并报告错误。回滚后请勿覆盖原存档。",
                        "CorpPresence rolled back. Temporarily disable the mod and report the error. Do not overwrite your original save.")
                    : L.T("模组「公司增强 CorpPresence」的水质兼容补丁触发回滚。公司物品仍可读取。请附上游戏日志报告错误。",
                        "CorpPresence rolled back its water compatibility patches. Company items can still load. Please report the error with your game log.");
                confirmation.OpenPanelSingleButton(message, L.T("确定", "OK"), () => { });
                Destroy(gameObject);
            }
            else if (Time.realtimeSinceStartup >= _deadline)
            {
                ModLog.Warn("原版提示面板未就绪，请查看回滚日志。");
                Destroy(gameObject);
            }
        }
        catch (Exception error)
        {
            Destroy(gameObject);
            ModLog.Warn("无法显示回滚提示：" + error);
        }
    }
}
