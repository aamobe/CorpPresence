using System;
using System.Collections.Generic;

namespace CorpPresence.Runtime;

// One enabled session. Register releases before acquisition to cover partial failures.
internal sealed class RuntimeLifetime
{
    private enum Phase { Stopped, Starting, Running, Stopping }
    private readonly List<Action> _releases = new List<Action>();
    private readonly Action<Exception> _report;
    private Phase _phase;

    public RuntimeLifetime(Action<Exception> report) { _report = report; }

    public bool Start(Action initialize)
    {
        if (_phase != Phase.Stopped) return false;
        _phase = Phase.Starting;
        try
        {
            initialize();
            if (_phase != Phase.Starting) throw new InvalidOperationException("Runtime stopped during initialization.");
            _phase = Phase.Running;
            return true;
        }
        catch { Stop(); throw; }
    }

    public void Own(Action acquire, Action release)
    {
        if (_phase != Phase.Starting) throw new InvalidOperationException("Resources must be acquired during startup.");
        _releases.Add(release ?? throw new ArgumentNullException(nameof(release)));
        acquire();
    }

    public void Continue(Action initialize)
    {
        if (_phase != Phase.Running) throw new InvalidOperationException("Runtime is not awaiting a startup phase.");
        _phase = Phase.Starting;
        try
        {
            initialize();
            if (_phase != Phase.Starting) throw new InvalidOperationException("Runtime stopped during initialization.");
            _phase = Phase.Running;
        }
        catch { Stop(); throw; }
    }

    public void Stop()
    {
        if (_phase == Phase.Stopped || _phase == Phase.Stopping) return;
        _phase = Phase.Stopping;
        for (int i = _releases.Count - 1; i >= 0; i--)
        {
            try { _releases[i](); }
            catch (Exception error) { try { _report?.Invoke(error); } catch { } }
        }
        _releases.Clear();
        _phase = Phase.Stopped;
    }
}
