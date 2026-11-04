using System;
using UnityEngine;

namespace CorpPresence.Runtime;

internal sealed class ResourceBootstrap : MonoBehaviour
{
    private Action _complete;
    private Action _abort;
    private Action<ModItemDirectory> _directory;

    public void Configure(Action complete, Action abort)
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

    private void Update() => Tick();

    public void Tick()
    {
        if (_complete == null) return;
        try
        {
            if (!GameResources.Poll()) return;
            Action complete = _complete;
            _complete = null; // Publish once, including reentrant directory events.
            complete();
        }
        catch
        {
            Action abort = _abort;
            Detach();
            abort?.Invoke();
            throw; // Visible failure after rollback; never retry a poisoned type.
        }
    }
}
