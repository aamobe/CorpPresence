namespace CorpPresence;

internal static class ConfigDiagnostics
{
    internal static void Attach(CustomUIWindow window)
    {
        var manager = CustomUIManager.Instance;
        if (window == null || manager == null) return;
        var builder = new CustomUIBuilder(manager, window);
        builder.AddLabel(L.K("diagnostics.report.help", "排障：只采集运行状态，不修改存档。", "Diagnostics: reads runtime state without changing saves."));
        builder.AddLabel("", "corp_diagnostics_status");
        var status = builder.Last;
        builder.AddButton(L.K("diagnostics.report.button", "生成诊断报告", "Generate diagnostic report"), () =>
        {
            bool written = MaintenanceReport.TryWrite();
            status.SetText(written
                ? L.K("diagnostics.report.success", "报告已写入 Player.log。", "Report written to Player.log.")
                : L.K("diagnostics.report.failure", "报告写入失败，请保留当前日志。", "Report could not be written. Keep the current log."));
        }, "corp_diagnostics_report");
        CustomUIBuilder.RebuildLayout(window.ContentRoot);
    }
}
