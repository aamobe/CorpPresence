using System;

namespace CorpPresence;

// Shared by hook stages; each stage may fail without blocking the following stage.
internal static class HookGuard
{
    internal static void Run(string what, Action action)
    {
        try { action(); }
        catch (Exception error)
        {
            try { MaintenanceDiagnostics.Record("hooks", what, "stage_failed", error); } catch { }
            try { ModLog.Warn(what + " 出错：" + error.Message); } catch { }
        }
    }
}
