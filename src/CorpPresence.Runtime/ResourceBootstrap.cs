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
		_directory = (ModItemDirectory _) =>
		{
			Tick();
		};
		ModHook.OnModItemDirectoryInit += _directory;
	}

	public void Detach()
	{
		if (_directory != null)
		{
			ModHook.OnModItemDirectoryInit -= _directory;
		}
		_directory = null;
		_complete = null;
		_abort = null;
	}

	private void Update()
	{
		Tick();
	}

	public void Tick()
	{
		if (_complete == null)
		{
			return;
		}
		try
		{
			if (GameResources.Poll())
			{
				Action complete = _complete;
				_complete = null;
				complete();
			}
		}
		catch
		{
			Action abort = _abort;
			Detach();
			abort?.Invoke();
			throw;
		}
	}
}
