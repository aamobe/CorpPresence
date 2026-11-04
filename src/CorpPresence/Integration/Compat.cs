using UnityEngine;

namespace CorpPresence;

internal static class Compat
{
	public static T Get<T>(GameObject go) where T : Component
	{
		return go.GetComponent<T>();
	}

	public static T Get<T>(Component c) where T : Component
	{
		return c.GetComponent<T>();
	}

	public static T As<T>(object o) where T : class
	{
		return o as T;
	}

	public static T Cast<T>(object o) where T : class
	{
		return (T)o;
	}

	public static GameObject NewObject<A, B, C>(string name) where A : Component where B : Component where C : Component
	{
		return new GameObject(name, typeof(A), typeof(B), typeof(C));
	}

	public static bool Alive(Object o)
	{
		return o != null;
	}

	public static GameItem ChoiceIcon()
	{
		return ItemDirectory.CreateEmptyItem("$");
	}
}
