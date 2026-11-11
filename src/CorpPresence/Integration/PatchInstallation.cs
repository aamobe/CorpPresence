using System;
using System.Collections.Generic;
namespace CorpPresence;

// Installation boundary: false means missing target; exceptions preserve rollback semantics.
internal static class PatchInstallation
{
    private static int _installed;
    internal static void Reset() => _installed = 0;
    internal static void AppendDiagnostics(ICollection<MaintenanceCounter> counters)
        => counters.Add(new MaintenanceCounter("hooks.installed", _installed));
    internal static void Run(string label, Func<bool> install)
    {
        bool installed;
        try { installed = install(); }
        catch (Exception error)
        {
            MaintenanceDiagnostics.RecordRuntime("hooks", label, "patch_failed", error);
            throw;
        }
        if (installed) { _installed++; return; }
        MaintenanceDiagnostics.RecordRuntime("hooks", label, "target_missing", null);
        try { ModLog.Warn("找不到 " + label + "，相关功能停用"); } catch { }
    }
}
