using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CorpPresence.UI;

internal static class Tex
{
	private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

	private static readonly Dictionary<(int, int, int, int), Texture2D> SolidCache = new Dictionary<(int, int, int, int), Texture2D>();

	private static readonly Dictionary<Sprite, Texture2D> Icons = new Dictionary<Sprite, Texture2D>();
	private static HashSet<Texture2D> _ownedIcons;

	public static Texture2D Solid(Color c)
	{
		(int, int, int, int) key = ((int)(c.r * 255f), (int)(c.g * 255f), (int)(c.b * 255f), (int)(c.a * 255f));
		if (SolidCache.TryGetValue(key, out var value) && value != null)
		{
			return value;
		}
		value = New(1, 1);
		value.SetPixel(0, 0, c);
		value.Apply();
		return SolidCache[key] = value;
	}

	public static Texture2D Art(string company, string name)
	{
		string key = "art:" + company + "/" + name;
		if (Cache.TryGetValue(key, out var value))
		{
			return value;
		}
		value = null;
		try
		{
			byte[] array = null;
			using (Stream stream = typeof(Tex).Assembly.GetManifestResourceStream("CorpPresence.art." + company + "." + name + ".png"))
			{
				if (stream != null)
				{
					using MemoryStream memoryStream = new MemoryStream();
					stream.CopyTo(memoryStream);
					array = memoryStream.ToArray();
				}
			}
			if (array != null)
			{
				value = New(2, 2);
				if (!value.LoadImage(array))
				{
					DestroyUiTexture(value);
					value = null;
				}
				else
				{
					value.filterMode = FilterMode.Point;
				}
			}
		}
		catch (Exception ex)
		{
			DestroyUiTexture(value);
			try { ModLog.Warn("读取美术 " + company + "/" + name + " 失败：" + ex.Message); } catch { }
			value = null;
		}
		return Cache[key] = value;
	}

	public static Texture2D FromSprite(Sprite sp)
	{
		if (sp == null)
		{
			return null;
		}
		if (Icons.TryGetValue(sp, out var value) && value != null)
		{
			return value;
		}
		Texture2D texture = sp.texture;
		if (texture == null)
		{
			return null;
		}
		Rect textureRect = sp.textureRect;
		if (textureRect.width >= (float)texture.width - 0.5f && textureRect.height >= (float)texture.height - 0.5f)
		{
			return Icons[sp] = texture;
		}
		int num = Mathf.RoundToInt(textureRect.width);
		int num2 = Mathf.RoundToInt(textureRect.height);
		RenderTexture active = RenderTexture.active;
		RenderTexture temporary = RenderTexture.GetTemporary(num, num2, 0, RenderTextureFormat.ARGB32);
		try
		{
			Graphics.Blit(texture, temporary, new Vector2(textureRect.width / (float)texture.width, textureRect.height / (float)texture.height), new Vector2(textureRect.x / (float)texture.width, textureRect.y / (float)texture.height));
			RenderTexture.active = temporary;
			value = New(num, num2);
			(_ownedIcons ?? (_ownedIcons = new HashSet<Texture2D>())).Add(value);
			value.ReadPixels(new Rect(0f, 0f, num, num2), 0, 0);
			value.Apply();
			value.filterMode = FilterMode.Point;
		}
		catch (Exception ex)
		{
			if (value != null) { _ownedIcons?.Remove(value); DestroyUiTexture(value); }
			try { ModLog.Warn("裁物品图标 " + sp.name + " 失败：" + ex.Message); } catch { }
			value = null;
		}
		finally
		{
			RenderTexture.active = active;
			RenderTexture.ReleaseTemporary(temporary);
		}
		return Icons[sp] = value;
	}

	public static Texture2D Bevel(Color face, Color hi, Color lo, Color edge, int edgeW = 1)
	{
		string key = $"bevel:{face}{hi}{lo}{edge}{edgeW}";
		if (Cache.TryGetValue(key, out var value) && value != null)
		{
			return value;
		}
		int num = 8 + edgeW * 2;
		value = New(num, num);
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Color color = face;
				if (j < edgeW || i < edgeW || j >= num - edgeW || i >= num - edgeW)
				{
					color = edge;
				}
				else if (i == num - edgeW - 1 || j == edgeW)
				{
					color = hi;
				}
				else if (i == edgeW || j == num - edgeW - 1)
				{
					color = lo;
				}
				value.SetPixel(j, i, color);
			}
		}
		value.Apply();
		return Cache[key] = value;
	}

	public static Texture2D DoubleLine(Color face, Color line)
	{
		string key = $"dline:{face}{line}";
		if (Cache.TryGetValue(key, out var value) && value != null)
		{
			return value;
		}
		value = New(12, 12);
		for (int i = 0; i < 12; i++)
		{
			for (int j = 0; j < 12; j++)
			{
				bool flag = j == 0 || i == 0 || j == 11 || i == 11;
				bool flag2 = (j == 3 || i == 3 || j == 8 || i == 8) && j >= 3 && i >= 3 && j <= 8 && i <= 8;
				value.SetPixel(j, i, (flag | flag2) ? line : face);
			}
		}
		value.Apply();
		return Cache[key] = value;
	}

	public static Texture2D Stripes(Color a, Color b, int width = 6)
	{
		string key = $"stripe:{a}{b}{width}";
		if (Cache.TryGetValue(key, out var value) && value != null)
		{
			return value;
		}
		int num = width * 4;
		value = New(num, num);
		value.wrapMode = TextureWrapMode.Repeat;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				value.SetPixel(j, i, ((j + i) / width % 2 == 0) ? a : b);
			}
		}
		value.Apply();
		return Cache[key] = value;
	}

	private static Texture2D New(int w, int h)
	{
		return new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false)
		{
			hideFlags = HideFlags.HideAndDontSave,
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp
		};
	}

	public static void ReleaseUi()
	{
		// Art textures may still be referenced by live item/portrait sprites.
		var remove = new List<string>();
		foreach (var entry in Cache)
		{
			if (entry.Key.StartsWith("art:", StringComparison.Ordinal)) continue;
			DestroyUiTexture(entry.Value);
			remove.Add(entry.Key);
		}
		foreach (string key in remove) Cache.Remove(key);
		foreach (Texture2D texture in SolidCache.Values) DestroyUiTexture(texture);
		SolidCache.Clear();
		if (_ownedIcons != null)
		{
			foreach (Texture2D texture in _ownedIcons) DestroyUiTexture(texture);
			_ownedIcons.Clear();
		}
		Icons.Clear(); // Full-atlas entries borrow native textures: never Destroy them.
	}

	private static void DestroyUiTexture(Texture2D texture)
	{
		if (texture == null) return;
		try { UnityEngine.Object.Destroy(texture); }
		catch (Exception error) { try { ModLog.Warn("释放自有UI纹理失败：" + error.Message); } catch { } }
	}
	public static Color Hex(int rgb, float a = 1f)
	{
		return new Color((float)((rgb >> 16) & 0xFF) / 255f, (float)((rgb >> 8) & 0xFF) / 255f, (float)(rgb & 0xFF) / 255f, a);
	}
}



