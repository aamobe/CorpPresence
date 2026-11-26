using UnityEngine;

namespace CorpPresence.UI;

internal static class Pointer
{
	public static Vector3 ScreenPosition
	{
		get
		{
#if LOCAL_PLAYTEST
			if (PanelPlaytest.Frame is { } sample) return sample.Item2;
#endif
			return Input.mousePosition;
		}
	}

	public static bool Down
	{
		get
		{
#if LOCAL_PLAYTEST
			if (PanelPlaytest.Frame is { } sample) return sample.Item3;
#endif
			return Input.GetMouseButtonDown(0);
		}
	}

	public static bool Held
	{
		get
		{
#if LOCAL_PLAYTEST
			if (PanelPlaytest.Frame is { } sample) return sample.Item4;
#endif
			return Input.GetMouseButton(0);
		}
	}

	public static bool Released
	{
		get
		{
#if LOCAL_PLAYTEST
			if (PanelPlaytest.Frame is { } sample) return sample.Item5;
#endif
			return Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1);
		}
	}

	public static float WheelNotches
	{
		get
		{
#if LOCAL_PLAYTEST
			if (PanelPlaytest.Frame is { } sample) return sample.Item6;
#endif
			return Input.mouseScrollDelta.y;
		}
	}

	public static Vector2 Local { get; private set; }

	public static Vector2 Logical { get; private set; }

	public static bool OverWindow { get; private set; }

#if LOCAL_PLAYTEST
	private static Vector2? pendingClick;
	private static System.Tuple<int, Vector2, bool, bool, bool, float> clickSample;

	public static Vector2? PendingClick
	{
		get => clickSample == null || ReferenceEquals(clickSample, PanelPlaytest.Frame) ? pendingClick : null;
		set
		{
			pendingClick = value;
			clickSample = value.HasValue ? PanelPlaytest.Frame : null;
		}
	}
#else
	public static Vector2? PendingClick { get; set; }
#endif

	internal static Vector2 ToLogical(Vector2 screenPoint, float scale) =>
		new Vector2(screenPoint.x, Screen.height - screenPoint.y) / scale;

	public static void Update(Rect window, float scale, bool visible)
	{
		Logical = ToLogical(ScreenPosition, scale);
		Local = Logical - window.position;
		OverWindow = visible && window.Contains(Logical);
		if (visible && OverWindow && Down)
		{
			PendingClick = Local;
		}
	}
}
