using System;

namespace CorpPresence.Malice;

internal static class MaliceGuards
{
	// Retained dialogue and placement callbacks may outlive the save that created them.
	internal static Func<bool> CaptureSession()
	{
		StoreSession session = StoreSession.Capture();
		MaliceState state = MaliceState.Get();
		return () => session.IsCurrent && state != null && ReferenceEquals(state, MaliceState.Get())
			&& SaveSlot.CanWrite("CorpPresence") && SaveSlot.CanWrite("CorpPresence.Malice");
	}

	internal static bool CanPauseBombDeadline(string chapter, int day, int deadline, int lastPauseDay, bool blocked)
	{
		if (blocked && chapter == "bomb" && deadline >= 0 && day <= deadline && day > lastPauseDay)
		{
			return deadline < int.MaxValue;
		}
		return false;
	}

	internal static bool CanDeliverBomb(string chapter, int day, int deadline, bool blocked, string buyerId)
	{
		if (chapter == "bomb" && deadline >= 0 && day <= deadline && !blocked)
		{
			return buyerId == "rebel_odin_envoy_bomb";
		}
		return false;
	}
}
