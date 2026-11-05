using System;
using System.Collections.Generic;
namespace CorpPresence.Runtime;

// Leases for extension fields; acquire/release happens only at enable/disable.
internal sealed class OwnedCallbacks
{
    private readonly List<Action> _releases = new List<Action>();
    private readonly Action<Exception> _report;
    public OwnedCallbacks(Action<Exception> report) { _report = report; }

    public void Add(Func<Action> read, Action<Action> write, Action owned)
    {
        _releases.Add(() => write((Action)Delegate.Remove(read(), owned)));
        write((Action)Delegate.Combine(read(), owned));
    }

    public void Replace<T>(Func<T> read, Action<T> write, T owned) where T : Delegate
    {
        T previous = read();
        _releases.Add(() =>
        {
            T current = read();
            if (Equals(current, owned)) write(previous);
            else
            {
                T remaining = (T)Delegate.Remove(current, owned);
                if (!ReferenceEquals(current, remaining)) write(remaining);
            }
        });
        write(owned);
    }

    public void Release()
    {
        for (int i = _releases.Count - 1; i >= 0; i--)
            try { _releases[i](); }
            catch (Exception error) { try { _report?.Invoke(error); } catch { } }
        _releases.Clear();
    }
}
