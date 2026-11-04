using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace CorpPresence;

internal static class Portraits
{
	private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

	private static readonly HashSet<string> Missing = new HashSet<string>();

	private static readonly Dictionary<string, string> Known = new Dictionary<string, string>();

	private static readonly Dictionary<string, Sprite> Tiers = new Dictionary<string, Sprite>();

	private static int _tier;

	private static float _nextCheck;

	private const int FourK = 2000;

	private const int FullHd = 1000;

	private const int Smooth = 10;

	private const int Pixel = 20;

	private const int Hd = 30;

	private static PixelPerfectCamera[] _cams = Array.Empty<PixelPerfectCamera>();

	private static float _camSearchAt = -100f;

	public static void ResetSceneReferences()
	{
		_cams = Array.Empty<PixelPerfectCamera>();
		_camSearchAt = -100f;
		_nextCheck = 0f;
	}

	public static Texture2D Texture(string key, string referenceKey)
	{
		if (Settings.PortraitSet == 3 && PortraitResources.Exists(key + "_3x"))
		{
			string text = key + "_3x";
			if (Textures.TryGetValue(text, out var value) && value != null)
			{
				return value;
			}
			byte[] array = Read(text);
			if (array != null)
			{
				return Textures[text] = Load(key, array, mipmaps: false, smooth: true);
			}
		}
		string text2;
		if (Settings.PortraitSet == 2 && PortraitResources.Exists(key + "_n1x"))
		{
			text2 = key + "_n1x";
		}
		else if (PortraitResources.Exists(key + "_s3x"))
		{
			text2 = key + "_s3x";
		}
		else
		{
			text2 = (PortraitResources.Exists(key + "_n1x") ? (key + "_n1x") : null);
		}
		if (text2 == null)
		{
			return null;
		}
		if (Textures.TryGetValue(text2, out var value2) && value2 != null)
		{
			return value2;
		}
		byte[] array2 = Read(text2);
		if (array2 == null)
		{
			return null;
		}
		return Textures[text2] = Load(key, array2, !text2.EndsWith("_n1x"));
	}

	public static string Ensure(string key, string referenceKey)
	{
		SpriteDict spriteDict = SpriteDict.Instance ?? throw new InvalidOperationException("SpriteDict 尚未初始化");
		if (!Known.ContainsKey(key) && !PortraitResources.Exists(key + "_s1x") && !PortraitResources.Exists(key + "_n1x") && spriteDict.spriteDictionary.ContainsKey(key))
		{
			return key;
		}
		if (_tier == 0)
		{
			_tier = CurrentTier();
		}
		Known[key] = referenceKey;
		spriteDict.spriteDictionary[key] = SpriteFor(spriteDict, key, _tier);
		return key;
	}

	public static void Tick()
	{
		if (Time.unscaledTime < _nextCheck)
		{
			return;
		}
		_nextCheck = Time.unscaledTime + 0.5f;
		int num = CurrentTier();
		SpriteDict instance = SpriteDict.Instance;
		if (instance == null || Known.Count == 0 || num == _tier)
		{
			return;
		}
		_tier = num;
		foreach (string key in Known.Keys)
		{
			instance.spriteDictionary[key] = SpriteFor(instance, key, num);
		}
		int num2 = 0;
		SpriteRenderer[] array = UnityEngine.Object.FindObjectsOfType<SpriteRenderer>();
		foreach (SpriteRenderer spriteRenderer in array)
		{
			Sprite sprite = Replacement(instance, spriteRenderer.sprite);
			if ((object)sprite != null)
			{
				spriteRenderer.sprite = sprite;
				num2++;
			}
		}
		Image[] array2 = UnityEngine.Object.FindObjectsOfType<Image>();
		foreach (Image image in array2)
		{
			Sprite sprite2 = Replacement(instance, image.sprite);
			if ((object)sprite2 != null)
			{
				image.sprite = sprite2;
				num2++;
			}
		}
		ModLog.Info(string.Format("分辨率 {0}x{1}，立绘换成{2}{3} 档（画面上换了 {4} 张）", Screen.width, Screen.height, (num > 30) ? "高清 " : ((num > 20) ? "像素化新版 " : "平滑新版 "), num % 10, num2));
	}

	private static Sprite Replacement(SpriteDict dict, Sprite current)
	{
		if (current == null)
		{
			return null;
		}
		string name = current.name;
		int num = name.LastIndexOf("_t", StringComparison.Ordinal);
		if (num < 0 || !Known.ContainsKey(name.Substring(0, num)))
		{
			return null;
		}
		Sprite sprite = dict.spriteDictionary[name.Substring(0, num)];
		if (!(sprite == current))
		{
			return sprite;
		}
		return null;
	}

	private static Sprite SpriteFor(SpriteDict dict, string key, int tier)
	{
		string text = $"{key}_t{tier}";
		if (Tiers.TryGetValue(text, out var value) && value != null)
		{
			return value;
		}
		Sprite sprite = dict.GetSprite(Known[key]);
		int num = tier % 10;
		if (tier > 30)
		{
			string text2 = ((num == 3) ? (key + "_3x") : (key + "_2x"));
			byte[] array = Read(text2);
			if (array == null)
			{
				return SpriteFor(dict, key, tier - 30 + 10);
			}
			Texture2D texture2D = Load(text, array, mipmaps: false, smooth: true);
			Sprite sprite2 = Create(text, texture2D, sprite);
			ModLog.Info($"立绘 {text} 已生成：{texture2D.width}x{texture2D.height}，来自 {text2}.png，高清立绘、不带多级纹理");
			return Tiers[text] = sprite2;
		}
		string text3 = ((num == 3) ? (key + "_n3x") : (key + "_n2x"));
		string text4 = $"{key}_s{num}x";
		bool flag = ((tier <= 20) ? (!PortraitResources.Exists(text4)) : (PortraitResources.Exists(text3) || !PortraitResources.Exists(text4)));
		string text5 = (flag ? text3 : text4);
		byte[] array2 = Read(text5);
		if (array2 == null)
		{
			ModLog.Warn("立绘 " + key + " 没有内置图，用原版立绘");
			return sprite;
		}
		Texture2D texture2D2 = Load(text, array2, !flag && num > 1);
		string text6 = text5 + ".png";
		Sprite sprite4 = Create(text, texture2D2, sprite);
		ModLog.Info($"立绘 {text} 已生成：{texture2D2.width}x{texture2D2.height}，来自 {text6}，多级纹理 {texture2D2.mipmapCount} 级");
		return Tiers[text] = sprite4;
	}

	private static int CurrentTier()
	{
		int num;
		if (Settings.PortraitSet == 3)
		{
			num = 30;
		}
		else
		{
			num = ((Settings.PortraitSet == 2) ? 20 : 10);
		}
		try
		{
			bool flag = Screen.height >= 1000;
			bool flag2 = false;
			PixelPerfectCamera[] array = Cameras();
			foreach (PixelPerfectCamera pixelPerfectCamera in array)
			{
				if (!(pixelPerfectCamera == null))
				{
					if (pixelPerfectCamera.upscaleRT == flag)
					{
						pixelPerfectCamera.upscaleRT = !flag;
					}
					if (pixelPerfectCamera.isActiveAndEnabled && pixelPerfectCamera.upscaleRT)
					{
						flag2 = true;
					}
				}
			}
			return num + ((Screen.height >= 2000) ? 3 : (flag2 ? 1 : 2));
		}
		catch (Exception ex)
		{
			ModLog.Warn("判断立绘档位失败：" + ex.Message);
			return num + 2;
		}
	}

	private static PixelPerfectCamera[] Cameras()
	{
		if ((_cams.Length == 0 || Array.Exists(_cams, (PixelPerfectCamera c) => c == null)) && Time.unscaledTime - _camSearchAt >= 5f)
		{
			_camSearchAt = Time.unscaledTime;
			_cams = UnityEngine.Object.FindObjectsOfType<PixelPerfectCamera>();
		}
		return _cams;
	}

	private static float RefHeight(Sprite reference)
	{
		if (!(reference != null))
		{
			return 211f;
		}
		return reference.rect.height;
	}

	private static Sprite Create(string name, Texture2D tex, Sprite reference)
	{
		float num = ((reference != null) ? reference.pixelsPerUnit : 100f);
		Vector2 pivot = ((reference != null) ? new Vector2(reference.pivot.x / reference.rect.width, reference.pivot.y / reference.rect.height) : new Vector2(0.5f, 0.5f));
		Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), pivot, num * (float)tex.height / RefHeight(reference));
		sprite.name = name;
		sprite.hideFlags = HideFlags.HideAndDontSave;
		return sprite;
	}

	private static Texture2D Load(string name, byte[] bytes, bool mipmaps, bool smooth = false)
	{
		Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false)
		{
			hideFlags = HideFlags.HideAndDontSave
		};
		if (!texture2D.LoadImage(bytes))
		{
			throw new InvalidDataException(name);
		}
		if (!mipmaps)
		{
			texture2D.filterMode = (smooth ? FilterMode.Bilinear : FilterMode.Point);
			texture2D.wrapMode = TextureWrapMode.Clamp;
			return texture2D;
		}
		int width = texture2D.width;
		int height = texture2D.height;
		Color32[] pixels = texture2D.GetPixels32();
		BleedEdges(pixels, width, height);
		Texture2D texture2D2 = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: true);
		texture2D2.hideFlags = HideFlags.HideAndDontSave;
		texture2D2.SetPixels32(pixels);
		texture2D2.Apply(updateMipmaps: true, makeNoLongerReadable: false);
		texture2D2.filterMode = FilterMode.Trilinear;
		texture2D2.wrapMode = TextureWrapMode.Clamp;
		UnityEngine.Object.Destroy(texture2D);
		return texture2D2;
	}

	private static void BleedEdges(Color32[] px, int w, int h)
	{
		bool[] array = new bool[px.Length];
		for (int i = 0; i < px.Length; i++)
		{
			array[i] = px[i].a > 0;
		}
		bool[] queued = new bool[px.Length];
		List<int> list = new List<int>();
		for (int j = 0; j < px.Length; j++)
		{
			if (array[j])
			{
				Enqueue(j, w, h, array, queued, list);
			}
		}
		List<int> list2 = new List<int>();
		Color32[] array2 = new Color32[list.Count];
		for (int k = 0; k < 8; k++)
		{
			if (list.Count <= 0)
			{
				break;
			}
			if (array2.Length < list.Count)
			{
				array2 = new Color32[list.Count];
			}
			for (int l = 0; l < list.Count; l++)
			{
				int num = list[l];
				int num2 = num % w;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				if (num2 > 0 && array[num - 1])
				{
					num3 += px[num - 1].r;
					num4 += px[num - 1].g;
					num5 += px[num - 1].b;
					num6++;
				}
				if (num2 < w - 1 && array[num + 1])
				{
					num3 += px[num + 1].r;
					num4 += px[num + 1].g;
					num5 += px[num + 1].b;
					num6++;
				}
				if (num >= w && array[num - w])
				{
					num3 += px[num - w].r;
					num4 += px[num - w].g;
					num5 += px[num - w].b;
					num6++;
				}
				if (num < px.Length - w && array[num + w])
				{
					num3 += px[num + w].r;
					num4 += px[num + w].g;
					num5 += px[num + w].b;
					num6++;
				}
				array2[l] = new Color32((byte)(num3 / num6), (byte)(num4 / num6), (byte)(num5 / num6), 0);
			}
			for (int m = 0; m < list.Count; m++)
			{
				px[list[m]] = array2[m];
				array[list[m]] = true;
			}
			list2.Clear();
			for (int n = 0; n < list.Count; n++)
			{
				Enqueue(list[n], w, h, array, queued, list2);
			}
			List<int> list3 = list;
			list = list2;
			list2 = list3;
		}
	}

	private static void Enqueue(int p, int w, int h, bool[] filled, bool[] queued, List<int> ring)
	{
		int num = p % w;
		if (num > 0)
		{
			Add(p - 1);
		}
		if (num < w - 1)
		{
			Add(p + 1);
		}
		if (p >= w)
		{
			Add(p - w);
		}
		if (p < w * (h - 1))
		{
			Add(p + w);
		}
		void Add(int q)
		{
			if (!filled[q] && !queued[q])
			{
				queued[q] = true;
				ring.Add(q);
			}
		}
	}

	private static byte[] Read(string name)
	{
		if (Missing.Contains(name))
		{
			return null;
		}
		using Stream stream = typeof(Portraits).Assembly.GetManifestResourceStream("CorpPresence.portraits." + name + ".png");
		if (stream == null)
		{
			Missing.Add(name);
			return null;
		}
		using MemoryStream memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}
}
