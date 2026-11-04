using System;

namespace CorpPresence.Runtime;

internal sealed class RuntimeTask
{
	private readonly string _name;

	private readonly Action _execute;

	private readonly Action<string, Exception, int> _report;

	private float _reportAt = float.NegativeInfinity;

	private int _suppressed;

	public RuntimeTask(string name, Action execute, Action<string, Exception, int> report)
	{
		_name = name;
		_execute = execute;
		_report = report;
	}

	public void Run(float now)
	{
		try
		{
			_execute();
		}
		catch (Exception arg)
		{
			if (now < _reportAt)
			{
				if (_suppressed < int.MaxValue)
				{
					_suppressed++;
				}
				return;
			}
			int suppressed = _suppressed;
			_suppressed = 0;
			_reportAt = now + 60f;
			try
			{
				_report?.Invoke(_name, arg, suppressed);
			}
			catch
			{
			}
		}
	}
}
