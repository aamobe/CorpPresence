using System;

namespace CorpPresence;

internal static class CrateFillGate
{
	[ThreadStatic]
	private static object _current;

	public static object Enter(object inventory)
	{
		if (inventory == null)
		{
			throw new ArgumentNullException("inventory");
		}
		object current = _current;
		_current = inventory;
		return current;
	}

	public static void Restore(object previous)
	{
		_current = previous;
	}

	public static bool Allows(object inventory)
	{
		if (inventory != null)
		{
			return _current == inventory;
		}
		return false;
	}
}
