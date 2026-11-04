using System;
using System.Collections.Generic;

namespace CorpPresence.Runtime;

internal sealed class OwnedCallbacks
{
	private readonly List<Action> _releases = new List<Action>();

	private readonly Action<Exception> _report;

	public OwnedCallbacks(Action<Exception> report)
	{
		_report = report;
	}

	public void Add(Func<Action> read, Action<Action> write, Action owned)
	{
		_releases.Add(() =>
		{
			write((Action)Delegate.Remove(read(), owned));
		});
		write((Action)Delegate.Combine(read(), owned));
	}

	public void Replace<T>(Func<T> read, Action<T> write, T owned) where T : Delegate
	{
		T previous = read();
		_releases.Add(() =>
		{
			T val = read();
			if (object.Equals(val, owned))
			{
				write(previous);
			}
			else
			{
				T val2 = (T)Delegate.Remove(val, owned);
				if ((object)val != val2)
				{
					write(val2);
				}
			}
		});
		write(owned);
	}

	public void Release()
	{
		for (int num = _releases.Count - 1; num >= 0; num--)
		{
			try
			{
				_releases[num]();
			}
			catch (Exception obj)
			{
				try
				{
					_report?.Invoke(obj);
				}
				catch
				{
				}
			}
		}
		_releases.Clear();
	}
}
