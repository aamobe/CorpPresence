using System;

namespace CorpPresence.Runtime;

// Callbacks are allocated once per host, never once per frame.
internal sealed class RuntimeTask
{
    private readonly string _name;
    private readonly Action _execute;
    private readonly Action<string, Exception, int> _report;
    private float _reportAt = float.NegativeInfinity;
    private int _suppressed;

    public RuntimeTask(string name, Action execute, Action<string, Exception, int> report)
    {
        _name = name; _execute = execute; _report = report;
    }

    public void Run(float now)
    {
        try { _execute(); }
        catch (Exception error)
        {
            // Structured counts must include failures hidden by the human-readable log throttle.
            try { MaintenanceDiagnostics.Record("runtime", _name, "task_failed", error); } catch { }
            if (now < _reportAt)
            {
                if (_suppressed < int.MaxValue) _suppressed++;
                return;
            }
            int suppressed = _suppressed;
            _suppressed = 0;
            _reportAt = now + 60f;
            try { _report?.Invoke(_name, error, suppressed); } catch { }
        }
    }
}
