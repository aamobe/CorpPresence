using System;
using UnityEngine;

namespace CorpPresence.Runtime;

internal sealed class ResourceBootstrap : MonoBehaviour
{
    private Action _complete;
    private Action<Exception> _abort;
    private Action<ModItemDirectory> _directory;

    public void Configure(Action complete, Action<Exception> abort)
    {
        _complete = complete;
        _abort = abort;
        _directory = _ => Tick();
        ModHook.OnModItemDirectoryInit += _directory;
    }

    public void Detach()
    {
        if (_directory != null) ModHook.OnModItemDirectoryInit -= _directory;
        _directory = null; _complete = null; _abort = null;
    }

    private void Update()
    {
#if MAINTENANCE
        MaintenanceConsole.Tick();
#endif
        Tick();
    }

    public void Tick()
    {
        if (_complete == null) return;
        // Completion can fail after RuntimeLifetime.Stop has detached this host.
        // Keep the reporter alive locally until acquisition has fully finished.
        Action<Exception> abort = _abort;
        try
        {
            if (!GameResources.Poll()) return;
            Action complete = _complete;
            _complete = null; // Publish once, including reentrant directory events.
            complete();
        }
        catch (Exception error)
        {
            Detach();
            abort?.Invoke(error);
            throw; // Visible failure after rollback; never retry a poisoned type.
        }
    }
}
