using System;
using System.Collections.Generic;
namespace CorpPresence.Runtime;

internal static class RuntimeUpdates
{
    private static readonly List<Lease> Entries = new();
    private static Lease[] _snapshot = Array.Empty<Lease>();

    internal static IDisposable Register(string owner, Action callback)
    {
        if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("Task owner required", nameof(owner));
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        foreach (var entry in Entries)
            if (entry.Owner == owner) throw new InvalidOperationException("Duplicate task owner: " + owner);
        var lease = new Lease(owner, callback);
        Entries.Add(lease);
        _snapshot = Entries.ToArray();
        return lease;
    }

    internal static void Run(float now)
    {
        // Mutations rebuild the array; the hot path allocates nothing.
        foreach (var entry in _snapshot)
            if (entry.Active) entry.Task.Run(now);
    }

    private sealed class Lease : IDisposable
    {
        internal readonly string Owner;
        internal RuntimeTask Task;
        internal bool Active = true;
        internal Lease(string owner, Action callback)
        {
            Owner = owner;
            Task = new RuntimeTask(owner, callback, (name, error, suppressed) =>
                ModLog.Warn("模块任务 " + name + " 出错：" + error.GetType().FullName + "；期间另有 " + suppressed + " 次"));
        }
        public void Dispose()
        {
            if (!Active) return;
            Active = false;
            Task = null;
            Entries.Remove(this);
            _snapshot = Entries.ToArray();
        }
    }
}
