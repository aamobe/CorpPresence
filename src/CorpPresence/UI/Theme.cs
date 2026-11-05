using System;
using UnityEngine;

namespace CorpPresence.UI;

internal sealed class Theme
{
	public string Company;

	public Color Paper;

	public Color Ink;

	public Color Accent;

	public Color Accent2;

	public Color Weak;

	public Color Line;

	public Color Hot;

	public Texture2D Window;

	public Texture2D Panel;

	public Texture2D Cell;

	public Texture2D CellOn;

	public Texture2D Btn;

	public Texture2D BtnHover;

	public Texture2D BtnHot;

	public Texture2D BtnHotHover;

	public Texture2D Banner;

	public GUIStyle Title;

	public GUIStyle Body;

	public GUIStyle Small;

	public GUIStyle Wrap;

	public GUIStyle Tight;

	public GUIStyle Price;

	public GUIStyle Center;

	public GUIStyle BtnStyle;

	public GUIStyle BtnHotStyle;

	public GUIStyle NavStyle;

	public GUIStyle NavOnStyle;

	public GUIStyle Box;

	public GUIStyle CellBox;

	public GUIStyle CellOnBox;

	public GUIStyle BannerTitle;

	public GUIStyle Desk;

	public GUIStyle InvTitle;

	public GUIStyle InvBody;

	public GUIStyle InvCenter;

	public GUIStyle HotTitle;

	public int BannerTile;

	private static Font _font;

	private static float _nextFontSearch;

	public static void ResetCachedFont() { _font = null; _nextFontSearch = 0f; } // Borrowed font: never Destroy.

	private Theme()
	{
	}

	public static Theme Pastel()
	{
		return Build(new Theme
		{
			Company = "pastel",
			Paper = Tex.Hex(14013112),
			Ink = Tex.Hex(4861752),
			Accent = Tex.Hex(7033449),
			Accent2 = Tex.Hex(8950922),
			Weak = Tex.Hex(8220522),
			Line = Tex.Hex(8950922),
			Hot = Tex.Hex(11692878)
		}, pastel: true);
	}

	public static Theme Solana()
	{
		return Build(new Theme
		{
			Company = "solana",
			Paper = Tex.Hex(2955573),
			Ink = Tex.Hex(14013112),
			Accent = Tex.Hex(13416839),
			Accent2 = Tex.Hex(7689839),
			Weak = Tex.Hex(10324631),
			Line = Tex.Hex(12224851),
			Hot = Tex.Hex(13416839)
		}, pastel: false);
	}

	public static Theme Carmichael()
	{
		return Build(new Theme
		{
			Company = "carmicheal",
			Paper = Tex.Hex(15002083),
			Ink = Tex.Hex(2046778),
			Accent = Tex.Hex(3112824),
			Accent2 = Tex.Hex(10273466),
			Weak = Tex.Hex(6255986),
			Line = Tex.Hex(8368038),
			Hot = Tex.Hex(11027515)
		}, pastel: true);
	}

	public static Theme Meridian()
	{
		return Build(new Theme
		{
			Company = "meridian",
			Paper = Tex.Hex(13815492),
			Ink = Tex.Hex(1776415),
			Accent = Tex.Hex(9379615),
			Accent2 = Tex.Hex(4539724),
			Weak = Tex.Hex(6052438),
			Line = Tex.Hex(9209984),
			Hot = Tex.Hex(9379615)
		}, pastel: true);
	}

	public static Theme Spacer()
	{
		return Build(new Theme
		{
			Company = "spacer",
			Paper = Tex.Hex(13223353),
			Ink = Tex.Hex(1973280),
			Accent = Tex.Hex(10168868),
			Accent2 = Tex.Hex(5660782),
			Weak = Tex.Hex(6183508),
			Line = Tex.Hex(9078139),
			Hot = Tex.Hex(10168868)
		}, pastel: true);
	}

	public static Theme Rebel()
	{
		return Build(new Theme
		{
			Company = "rebel",
			Paper = Tex.Hex(12895656),
			Ink = Tex.Hex(1909270),
			Accent = Tex.Hex(5135658),
			Accent2 = Tex.Hex(7041872),
			Weak = Tex.Hex(5791046),
			Line = Tex.Hex(8422762),
			Hot = Tex.Hex(5135658)
		}, pastel: true);
	}

	public static Theme Davre()
	{
		return Build(new Theme
		{
			Company = "davre",
			Paper = Tex.Hex(15129780),
			Ink = Tex.Hex(3023400),
			Accent = Tex.Hex(10237752),
			Accent2 = Tex.Hex(12224851),
			Weak = Tex.Hex(7232072),
			Line = Tex.Hex(12166524),
			Hot = Tex.Hex(10237752)
		}, pastel: true);
	}

	private static Theme Build(Theme t, bool pastel)
	{
		if (_font == null && Time.unscaledTime >= _nextFontSearch)
		{
			_nextFontSearch = Time.unscaledTime + 5f;
			try
			{
				Font[] array = Resources.FindObjectsOfTypeAll<Font>();
				foreach (Font font in array)
				{
					if (font != null && font.name == "WDXLLubrifontSC-Regular")
					{
						_font = font;
						break;
					}
				}
			}
			catch
			{
			}
		}
		Func<string, Texture2D, Texture2D> func = (string name, Texture2D fallback) => Tex.Art(t.Company, name) ?? fallback;
		bool flag = t.Company == "carmicheal";
		bool flag2 = t.Company == "spacer" || t.Company == "meridian";
		bool flag3 = t.Company == "davre";
		bool flag4 = t.Company == "rebel";
		if (flag4)
		{
			t.Window = Tex.Bevel(t.Paper, Tex.Hex(13882553), Tex.Hex(10922122), t.Ink, 2);
			t.Panel = Tex.Bevel(Tex.Hex(11711638), Tex.Hex(12698533), Tex.Hex(9935484), t.Ink);
			t.Cell = Tex.Bevel(Tex.Hex(13488050), Tex.Hex(14211774), Tex.Hex(11382930), t.Line);
			t.CellOn = Tex.Bevel(Tex.Hex(12371354), Tex.Hex(13292202), Tex.Hex(10134650), t.Accent);
			t.Btn = Tex.Bevel(Tex.Hex(10462084), Tex.Hex(11646358), Tex.Hex(8619882), t.Ink);
			t.BtnHover = Tex.Bevel(Tex.Hex(11383185), Tex.Hex(12501666), Tex.Hex(9343604), t.Ink);
			t.BtnHot = Tex.Bevel(t.Accent, Tex.Hex(6715958), Tex.Hex(3292186), t.Ink);
			t.BtnHotHover = Tex.Bevel(Tex.Hex(5925680), Tex.Hex(7637566), Tex.Hex(3819038), t.Ink);
			t.Banner = null;
		}
		else if (flag3)
		{
			t.Window = func("frame", Tex.Bevel(t.Paper, Tex.Hex(15788232), Tex.Hex(12891274), t.Ink, 2));
			t.Panel = Tex.Bevel(Tex.Hex(14537384), Tex.Hex(15327161), Tex.Hex(12759690), t.Line);
			t.Cell = func("cell", Tex.Bevel(Tex.Hex(15524800), Tex.Hex(16051408), Tex.Hex(13483928), t.Line));
			t.CellOn = Tex.Bevel(Tex.Hex(15257536), Tex.Hex(15850960), Tex.Hex(13214869), t.Accent);
			t.Btn = Tex.Bevel(t.Accent2, Tex.Hex(13409900), Tex.Hex(9333312), t.Ink);
			t.BtnHover = Tex.Bevel(Tex.Hex(13014879), Tex.Hex(14069116), Tex.Hex(10122312), t.Ink);
			t.BtnHot = Tex.Bevel(t.Accent, Tex.Hex(11880778), Tex.Hex(7218470), t.Ink);
			t.BtnHotHover = Tex.Bevel(Tex.Hex(11223362), Tex.Hex(12737114), Tex.Hex(8006187), t.Ink);
			t.Banner = func("banner", null);
		}
		else if (flag2)
		{
			t.Window = func("frame", Tex.Bevel(t.Paper, Tex.Hex(14078662), Tex.Hex(11183512), t.Ink, 2));
			t.Panel = Tex.Bevel(Tex.Hex(12433837), Tex.Hex(13354939), Tex.Hex(10657426), t.Ink);
			t.Cell = func("cell", Tex.Bevel(Tex.Hex(13815490), Tex.Hex(14473421), Tex.Hex(11775907), t.Line));
			t.CellOn = Tex.Bevel(Tex.Hex(14271677), Tex.Hex(14996172), Tex.Hex(12099479), t.Accent);
			t.Btn = Tex.Bevel(Tex.Hex(11117978), Tex.Hex(12302252), Tex.Hex(9144189), t.Ink);
			t.BtnHover = Tex.Bevel(Tex.Hex(11973287), Tex.Hex(13091768), Tex.Hex(9867914), t.Ink);
			t.BtnHot = Tex.Bevel(t.Accent, Tex.Hex(11745843), Tex.Hex(7216409), t.Ink);
			t.BtnHotHover = Tex.Bevel(Tex.Hex(11219756), Tex.Hex(12864066), Tex.Hex(8004125), t.Ink);
			t.Banner = func("banner", null);
		}
		else if (flag)
		{
			t.Window = func("frame", Tex.Bevel(t.Paper, Tex.Hex(15988466), Tex.Hex(12832198), t.Accent, 2));
			t.Panel = Tex.Bevel(Tex.Hex(14213594), Tex.Hex(15331305), Tex.Hex(12240831), t.Line);
			t.Cell = func("cell", Tex.Bevel(Tex.Hex(15856880), Tex.Hex(16448761), Tex.Hex(13687250), t.Line));
			t.CellOn = Tex.Bevel(Tex.Hex(14085094), Tex.Hex(15135728), Tex.Hex(11653320), t.Accent);
			t.Btn = Tex.Bevel(t.Accent2, Tex.Hex(12113615), Tex.Hex(8037788), t.Ink);
			t.BtnHover = Tex.Bevel(Tex.Hex(11325383), Tex.Hex(13033689), Tex.Hex(8827047), t.Ink);
			t.BtnHot = Tex.Bevel(t.Accent, Tex.Hex(4889234), Tex.Hex(2055254), t.Ink);
			t.BtnHotHover = Tex.Bevel(Tex.Hex(3837574), Tex.Hex(5942178), Tex.Hex(2583139), t.Ink);
			t.Banner = func("banner", null);
		}
		else if (pastel)
		{
			t.Window = func("frame", Tex.Bevel(t.Paper, Tex.Hex(15131596), Tex.Hex(12170907), t.Line, 2));
			t.Panel = Tex.Bevel(Tex.Hex(13289387), Tex.Hex(14473664), Tex.Hex(11578770), t.Line);
			t.Cell = func("cell", Tex.Bevel(Tex.Hex(14868422), Tex.Hex(15723734), Tex.Hex(12763044), t.Line));
			t.CellOn = Tex.Bevel(Tex.Hex(15324100), Tex.Hex(15983318), Tex.Hex(13216152), t.Hot);
			t.Btn = Tex.Bevel(t.Accent2, Tex.Hex(10792869), Tex.Hex(7042669), t.Ink);
			t.BtnHover = Tex.Bevel(Tex.Hex(9937817), Tex.Hex(11845301), Tex.Hex(7766394), t.Ink);
			t.BtnHot = Tex.Bevel(t.Accent, Tex.Hex(9137288), Tex.Hex(5192525), t.Ink);
			t.BtnHotHover = Tex.Bevel(Tex.Hex(8216697), Tex.Hex(10255769), Tex.Hex(5915736), t.Ink);
			t.Banner = func("banner", null);
			if (t.Banner == null)
			{
				t.Banner = Tex.Stripes(Tex.Hex(8950922), Tex.Hex(13223594));
				t.BannerTile = 1;
			}
		}
		else
		{
			t.Window = func("frame", Tex.DoubleLine(t.Paper, t.Line));
			t.Panel = Tex.DoubleLine(Tex.Hex(3810371), Tex.Hex(8018490));
			t.Cell = func("showcase", Tex.DoubleLine(Tex.Hex(3810371), t.Line));
			t.CellOn = Tex.DoubleLine(Tex.Hex(4862547), t.Accent);
			t.Btn = Tex.DoubleLine(Tex.Hex(3810371), t.Line);
			t.BtnHover = Tex.DoubleLine(Tex.Hex(4862547), t.Accent);
			t.BtnHot = Tex.DoubleLine(Tex.Hex(5914154), t.Accent);
			t.BtnHotHover = Tex.DoubleLine(Tex.Hex(7031346), Tex.Hex(15128230));
			t.Banner = func("banner", null);
		}
		t.Title = Text(pastel ? 22 : 20, pastel ? t.Ink : t.Accent, TextAnchor.MiddleLeft);
		t.Body = Text(15, t.Ink, TextAnchor.MiddleLeft);
		t.Small = Text(13, t.Weak, TextAnchor.MiddleLeft);
		t.Wrap = Text(13, t.Ink, TextAnchor.UpperLeft);
		t.Wrap.wordWrap = true;
		t.Tight = Text(11, t.Ink, TextAnchor.UpperLeft);
		t.Tight.wordWrap = true;
		t.Price = Text(15, pastel ? t.Hot : t.Accent, TextAnchor.MiddleCenter);
		t.Center = Text(14, t.Ink, TextAnchor.MiddleCenter);
		t.BannerTitle = Text(18, pastel ? t.Ink : t.Accent, TextAnchor.MiddleCenter);
		t.Desk = Text(13, t.Weak, TextAnchor.MiddleCenter);
		t.InvTitle = Text(24, t.Paper, TextAnchor.MiddleLeft);
		t.InvBody = Text(15, t.Paper, TextAnchor.MiddleLeft);
		t.InvCenter = Text(14, t.Paper, TextAnchor.MiddleCenter);
		t.HotTitle = Text(pastel ? 22 : 20, t.Hot, TextAnchor.MiddleLeft);
		t.BtnStyle = MakeBtn(t.Btn, t.BtnHover, pastel ? Tex.Hex(2761254) : t.Ink);
		t.BtnHotStyle = MakeBtn(t.BtnHot, t.BtnHotHover, pastel ? Tex.Hex(16052444) : Tex.Hex(16181968));
		Color c;
		if (flag4)
		{
			c = Tex.Hex(11975066);
		}
		else if (flag3)
		{
			c = Tex.Hex(14536610);
		}
		else if (flag2)
		{
			c = Tex.Hex(12170666);
		}
		else if (flag)
		{
			c = Tex.Hex(13886431);
		}
		else
		{
			c = (pastel ? Tex.Hex(13223594) : Tex.Hex(3810371));
		}
		Color c2;
		if (flag4)
		{
			c2 = Tex.Hex(10725766);
		}
		else if (flag3)
		{
			c2 = Tex.Hex(14071968);
		}
		else if (flag2)
		{
			c2 = Tex.Hex(11117978);
		}
		else if (flag)
		{
			c2 = Tex.Hex(12376273);
		}
		else
		{
			c2 = (pastel ? Tex.Hex(12568253) : Tex.Hex(4862547));
		}
		t.NavStyle = MakeBtn(Tex.Solid(new Color(0f, 0f, 0f, 0f)), Tex.Solid(c), t.Ink, TextAnchor.MiddleLeft);
		t.NavOnStyle = MakeBtn(Tex.Solid(c2), Tex.Solid(c2), pastel ? t.Ink : t.Accent, TextAnchor.MiddleLeft);
		t.Box = MakeBox(t.Window, pastel ? 4 : 5);
		t.CellBox = MakeBox(t.Cell, 4);
		t.CellOnBox = MakeBox(t.CellOn, 4);
		return t;
	}

	private static GUIStyle Text(int size, Color c, TextAnchor anchor)
	{
		return new GUIStyle
		{
			fontSize = size,
			font = _font,
			alignment = anchor,
			clipping = TextClipping.Clip,
			normal = 
			{
				textColor = c
			}
		};
	}

	private static GUIStyle MakeBtn(Texture2D normal, Texture2D hover, Color text, TextAnchor anchor = TextAnchor.MiddleCenter)
	{
		return new GUIStyle
		{
			border = new RectOffset(4, 4, 4, 4),
			alignment = anchor,
			fontSize = 15,
			font = _font,
			clipping = TextClipping.Clip,
			padding = new RectOffset(8, 8, 0, 0),
			normal = 
			{
				background = normal,
				textColor = text
			},
			hover = 
			{
				background = hover,
				textColor = text
			}
		};
	}

	private static GUIStyle MakeBox(Texture2D tex, int border)
	{
		return new GUIStyle
		{
			border = new RectOffset(border, border, border, border),
			normal = 
			{
				background = tex
			}
		};
	}
}

