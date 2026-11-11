#if MAINTENANCE
using UnityEngine;
namespace CorpPresence;
internal static class MaintenanceConsole
{
    private static float _lastRequest = float.NegativeInfinity;
    internal static void Tick()
    {
        try
        {
            if (!Input.GetKeyDown(KeyCode.F10)
                || !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                || !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) return;
            float now = Time.unscaledTime;
            if (now - _lastRequest < 1f) return;
            _lastRequest = now;
            MaintenanceReport.TryWrite();
        }
        catch { /* Debug input and logging must not affect gameplay. */ }
    }
}
#endif
