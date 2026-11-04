using UnityEngine;

namespace CorpPresence.UI;

internal sealed class Lay
{
	public const float Gap = 6f;

	public const float LineH = 22f;

	private readonly float _x0;

	private float _rowH;

	public float X { get; private set; }

	public float Y { get; private set; }

	public float Width { get; }

	public float Right => _x0 + Width;

	public float Remaining => Right - X;

	public Lay(float x, float y, float width)
	{
		_x0 = (X = x);
		Y = y;
		Width = width;
	}

	public Rect Take(float w, float h = 22f)
	{
		Rect result = new Rect(X, Y, w, h);
		X += w + 6f;
		if (h > _rowH)
		{
			_rowH = h;
		}
		return result;
	}

	public Rect Fill(float h = 22f)
	{
		return Take(Mathf.Max(10f, Remaining), h);
	}

	public void Space(float w)
	{
		X += w;
	}

	public void NewLine(float extra = 0f)
	{
		Y += Mathf.Max(_rowH, 22f) + extra;
		X = _x0;
		_rowH = 0f;
	}

	public void Down(float h)
	{
		if (X > _x0)
		{
			NewLine();
		}
		Y += h;
	}

	public void Label(string text, GUIStyle style, float w, Color? color = null, float h = 22f)
	{
		Ui.Label(Take(w, h), text, style, color);
	}

	public bool Button(string text, float w, GUIStyle style = null, float h = 22f)
	{
		return Ui.Button(Take(w, h), text, style);
	}
}
