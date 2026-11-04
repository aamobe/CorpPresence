using System;
using System.Runtime.CompilerServices;

namespace CorpPresence;

internal static class ServiceRetry
{
	private sealed class Attempt
	{
		public float Delay;

		public float Next;
	}

	private static readonly ConditionalWeakTable<object, Attempt> Attempts = new ConditionalWeakTable<object, Attempt>();

	public static bool Ready(object key, float now)
	{
		if (key != null)
		{
			if (Attempts.TryGetValue(key, out var value))
			{
				return now >= value.Next;
			}
			return true;
		}
		return false;
	}

	public static void Failed(object key, float now)
	{
		if (key != null)
		{
			Attempt value = Attempts.GetValue(key, (object _) => new Attempt());
			value.Delay = ((value.Delay <= 0f) ? 5f : Math.Min(60f, value.Delay * 2f));
			value.Next = now + value.Delay;
		}
	}

	public static void Succeeded(object key)
	{
		if (key != null)
		{
			Attempts.Remove(key);
		}
	}
}
