using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CorpPresence;

internal static class SaveSlot
{
	// Failure records live only as long as their actual native modData dictionary.
	private static readonly ConditionalWeakTable<Dictionary<string, string>, HashSet<string>> Unreadable = new();
	// Instance is lazy and constructs a player. Do not touch it before readiness.
	public static PlayerStore CurrentStore => PlayerStore.IsInstanceExist() ? PlayerStore.Instance : null;

	public static string Read(string key)
	{
		Dictionary<string, string> dictionary = CurrentStore?.modData;
		if (dictionary == null) return null;
		if (!dictionary.TryGetValue(key, out var value))
		{
			AcceptRead(key);
			return null;
		}
		return value;
	}

	public static void Write(string key, string value)
	{
		PlayerStore instance = CurrentStore;
		if (instance != null && CanWrite(key))
		{
			PlayerStore playerStore = instance;
			if (playerStore.modData == null)
			{
				playerStore.modData = new Dictionary<string, string>();
			}
			instance.modData[key] = value;
		}
	}

	public static void ProtectUnreadable(string key)
	{
		var data = CurrentStore?.modData;
		if (data != null && data.ContainsKey(key)) Unreadable.GetOrCreateValue(data).Add(key);
	}

	public static void AcceptRead(string key)
	{
		var data = CurrentStore?.modData;
		if (data != null && Unreadable.TryGetValue(data, out var keys)) keys.Remove(key);
	}

	public static bool CanWrite(string key)
	{
		var store = CurrentStore;
		if (store == null) return false;
		var data = store.modData;
		return data == null || !Unreadable.TryGetValue(data, out var keys) || !keys.Contains(key);
	}
}
