using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CorpPresence;

internal static class SaveSlot
{
	private static readonly ConditionalWeakTable<Dictionary<string, string>, HashSet<string>> Unreadable = new ConditionalWeakTable<Dictionary<string, string>, HashSet<string>>();

	public static PlayerStore CurrentStore
	{
		get
		{
			if (!PlayerStore.IsInstanceExist())
			{
				return null;
			}
			return PlayerStore.Instance;
		}
	}

	public static string Read(string key)
	{
		Dictionary<string, string> dictionary = CurrentStore?.modData;
		if (dictionary == null)
		{
			return null;
		}
		if (!dictionary.TryGetValue(key, out var value))
		{
			AcceptRead(key);
			return null;
		}
		return value;
	}

	public static void Write(string key, string value)
	{
		PlayerStore currentStore = CurrentStore;
		if (currentStore != null && CanWrite(key))
		{
			PlayerStore playerStore = currentStore;
			if (playerStore.modData == null)
			{
				playerStore.modData = new Dictionary<string, string>();
			}
			currentStore.modData[key] = value;
		}
	}

	public static void ProtectUnreadable(string key)
	{
		Dictionary<string, string> dictionary = CurrentStore?.modData;
		if (dictionary != null && dictionary.ContainsKey(key))
		{
			Unreadable.GetOrCreateValue(dictionary).Add(key);
		}
	}

	public static void AcceptRead(string key)
	{
		Dictionary<string, string> dictionary = CurrentStore?.modData;
		if (dictionary != null && Unreadable.TryGetValue(dictionary, out var value))
		{
			value.Remove(key);
		}
	}

	public static bool CanWrite(string key)
	{
		PlayerStore currentStore = CurrentStore;
		if (currentStore == null)
		{
			return false;
		}
		Dictionary<string, string> modData = currentStore.modData;
		if (modData != null && Unreadable.TryGetValue(modData, out var value))
		{
			return !value.Contains(key);
		}
		return true;
	}
}
