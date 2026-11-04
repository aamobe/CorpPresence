using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorpPresence.UI;

internal static class Ui
{
	private static readonly Dictionary<Texture2D, GUIStyle> BoxStyles = new Dictionary<Texture2D, GUIStyle>();

	private static readonly Dictionary<GUIStyle, GUIStyle> Hovers = new Dictionary<GUIStyle, GUIStyle>();

	public static Vector2 Origin { get; set; }

	public static Rect? Clip { get; set; }

	public static bool Button(Rect r, string text, GUIStyle style)
	{
		Rect rect = new Rect(r.x + Origin.x, r.y + Origin.y, r.width, r.height);
		bool flag = rect.Contains(Pointer.Local) && (!Clip.HasValue || Clip.Value.Contains(Pointer.Local));
		GUI.Box(r, text, (flag && GUI.enabled) ? HoverOf(style) : style);
		Event current = Event.current;
		if (current == null || current.type != EventType.Repaint || !GUI.enabled || !Pointer.PendingClick.HasValue)
		{
			return false;
		}
		Vector2 value = Pointer.PendingClick.Value;
		if (!rect.Contains(value) || (Clip.HasValue && !Clip.Value.Contains(value)))
		{
			return false;
		}
		Pointer.PendingClick = null;
		return true;
	}

	public static int Qty(Rect r, int q, int max, GUIStyle button, GUIStyle label)
	{
		float num = 24f;
		float y = r.y + (r.height - num) / 2f;
		if (Button(new Rect(r.x, y, 22f, num), "«", button))
		{
			q = 0;
		}
		if (Button(new Rect(r.x + 24f, y, 24f, num), "－", button) && q > 0)
		{
			q--;
		}
		GUI.Label(new Rect(r.x + 48f, r.y, r.width - 96f, r.height), q.ToString(), label);
		if (Button(new Rect(r.xMax - 48f, y, 24f, num), "＋", button) && q < max)
		{
			q++;
		}
		if (Button(new Rect(r.xMax - 22f, y, 22f, num), "»", button))
		{
			q = max;
		}
		return Math.Max(0, Math.Min(q, max));
	}

	public static void ResetCaches()
	{
		Hovers.Clear();
		BoxStyles.Clear();
		Origin = default;
		Clip = null;
	}

	public static void ClearHoverCache()
	{
		Hovers.Clear();
	}

	private static GUIStyle HoverOf(GUIStyle style)
	{
		if (Hovers.TryGetValue(style, out var value) && value != null)
		{
			return value;
		}
		value = new GUIStyle(style);
		if (style.hover.background != null)
		{
			value.normal.background = style.hover.background;
		}
		value.normal.textColor = ((style.hover.textColor.a > 0f) ? style.hover.textColor : style.normal.textColor);
		Hovers[style] = value;
		return value;
	}

	public static void Label(Rect r, string text, GUIStyle style, Color? color = null)
	{
		if (color.HasValue)
		{
			Color contentColor = GUI.contentColor;
			GUI.contentColor = color.Value;
			GUI.Label(r, text, style);
			GUI.contentColor = contentColor;
		}
		else
		{
			GUI.Label(r, text, style);
		}
	}

	public static void Image(Rect r, Texture2D tex)
	{
		if (!(tex == null))
		{
			if (!BoxStyles.TryGetValue(tex, out var value) || value == null)
			{
				value = new GUIStyle
				{
					border = new RectOffset(0, 0, 0, 0),
					padding = new RectOffset(0, 0, 0, 0)
				};
				value.normal.background = tex;
				BoxStyles[tex] = value;
			}
			GUI.Box(r, GUIContent.none, value);
		}
	}

	public static void Fill(Rect r, Color c)
	{
		Image(r, Tex.Solid(c));
	}
}
