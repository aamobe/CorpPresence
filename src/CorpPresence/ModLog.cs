using UnityEngine;

namespace CorpPresence;

internal static class ModLog
{
	private const string Prefix = "[公司增强]: ";

	public static void Info(string message)
	{
		Debug.Log("[公司增强]: " + message);
	}

	public static void Warn(string message)
	{
		Debug.LogWarning("[公司增强]: " + message);
	}

	public static void Error(string message)
	{
		Debug.LogError("[公司增强]: " + message);
	}
}
