using UnityEngine;

namespace CorpPresence.UI;

internal static class Pointer
{
	public static Vector2 Local { get; private set; }

	public static Vector2 Logical { get; private set; }

	public static bool OverWindow { get; private set; }

	public static Vector2? PendingClick { get; set; }

	public static void Update(Rect window, float scale, bool visible)
	{
		Vector3 mousePosition = Input.mousePosition;
		Logical = new Vector2(mousePosition.x / scale, ((float)Screen.height - mousePosition.y) / scale);
		Local = Logical - window.position;
		OverWindow = visible && window.Contains(Logical);
		if (visible && OverWindow && Input.GetMouseButtonDown(0))
		{
			PendingClick = Local;
		}
	}
}
