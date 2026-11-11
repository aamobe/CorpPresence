using System;
namespace CorpPresence;

// Shared by the release configuration button and the maintenance shortcut.
// Explicit requests only: capture values, format, write the existing Unity log.
internal static class MaintenanceReport
{
    internal static bool TryWrite()
    {
        try
        {
            ModLog.Info(MaintenanceFormatter.Format(MaintenanceDiagnostics.Capture()));
            return true;
        }
        catch (Exception error)
        {
            MaintenanceDiagnostics.RecordRuntime("diagnostics", "report", "write_failed", error);
            return false;
        }
    }
}
