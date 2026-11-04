using System;
using System.Collections.Generic;

namespace CorpPresence;

internal sealed class SaveBinding
{
	private readonly WeakReference<PlayerStore> _store;

	private WeakReference<Dictionary<string, string>> _data;

	private readonly string _key;

	private string _raw;

	public SaveBinding(PlayerStore store, string key)
	{
		_store = new WeakReference<PlayerStore>(store);
		_key = key;
		Refresh(store);
	}

	private string Read(Dictionary<string, string> data)
	{
		if (data == null || !data.TryGetValue(_key, out var value))
		{
			return null;
		}
		return value;
	}

	public bool Matches(PlayerStore store)
	{
		if (store == null || !_store.TryGetTarget(out var target) || target != store)
		{
			return false;
		}
		Dictionary<string, string> modData = store.modData;
		if (_data == null)
		{
			if (_raw != null || Read(modData) != null)
			{
				return false;
			}
			if (modData != null)
			{
				_data = new WeakReference<Dictionary<string, string>>(modData);
			}
			return true;
		}
		if (_data.TryGetTarget(out var target2) && target2 == modData)
		{
			return (object)_raw == Read(modData);
		}
		return false;
	}

	public void Refresh(PlayerStore store)
	{
		Dictionary<string, string> modData = store.modData;
		if (modData == null)
		{
			_data = null;
		}
		else if (_data == null)
		{
			_data = new WeakReference<Dictionary<string, string>>(modData);
		}
		else
		{
			_data.SetTarget(modData);
		}
		_raw = Read(modData);
	}
}
