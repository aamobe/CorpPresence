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

	// 高清立绘（旧版）：只分两档，4K 用 <key>_3x（864×633），其余都用 <key>_2x（576×422）；不用多级纹理，双线性过滤
	private const int Hd = 30;

	private static PixelPerfectCamera[] _cams = Array.Empty<PixelPerfectCamera>();

	private static float _camSearchAt = -100f;

	public static void ResetSceneReferences() { _cams = Array.Empty<PixelPerfectCamera>(); _camSearchAt = -100f; _nextCheck = 0f; }

	public static Texture2D Texture(string key, string referenceKey)
	{
		string text;
		if (Settings.PortraitSet == 3 && PortraitResources.Exists(key + "_3x"))
		{
			string hd = key + "_3x";
			if (Textures.TryGetValue(hd, out var cached) && cached != null) return cached;
			byte[] bytes = Read(hd);
			if (bytes != null) return Textures[hd] = Load(key, bytes, mipmaps: false, smooth: true);
		}
		if (Settings.PortraitSet == 2 && PortraitResources.Exists(key + "_n1x"))
		{
			text = key + "_n1x";
		}
		else if (PortraitResources.Exists(key + "_s3x"))
		{
			text = key + "_s3x";
		}
		else
		{
			text = ((PortraitResources.Exists(key + "_n1x")) ? (key + "_n1x") : null);
		}
		if (text == null)
		{
			return null;
		}
		if (Textures.TryGetValue(text, out var value) && value != null)
		{
			return value;
		}
		byte[] array = Read(text);
		if (array == null)
		{
			return null;
		}
		return Textures[text] = Load(key, array, !text.EndsWith("_n1x"));
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
		ModLog.Info(string.Format("分辨率 {0}x{1}，立绘换成{2}{3} 档（画面上换了 {4} 张）", Screen.width, Screen.height, (num > Hd) ? "高清 " : (num > Pixel) ? "像素化新版 " : "平滑新版 ", num % 10, num2));
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
		if (tier > Hd)
		{
			string hdFile = num == 3 ? key + "_3x" : key + "_2x";
			byte[] hdBytes = Read(hdFile);
			if (hdBytes == null) return SpriteFor(dict, key, tier - Hd + Smooth);   // 这个人没有高清立绘（撒该）：用平滑新版
			Texture2D hdTex = Load(text, hdBytes, mipmaps: false, smooth: true);
			Sprite hdSprite = Create(text, hdTex, sprite);
			ModLog.Info($"立绘 {text} 已生成：{hdTex.width}x{hdTex.height}，来自 {hdFile}.png，高清立绘、不带多级纹理");
			return Tiers[text] = hdSprite;
		}
		string text2 = ((num == 3) ? (key + "_n3x") : (key + "_n2x"));
		string text3 = $"{key}_s{num}x";
		bool flag = tier <= 20 ? !PortraitResources.Exists(text3) : PortraitResources.Exists(text2) || !PortraitResources.Exists(text3);
		string text4 = (flag ? text2 : text3);
		byte[] array = Read(text4);
		if (array == null)
		{
			ModLog.Warn("立绘 " + key + " 没有内置图，用原版立绘");
			return sprite;
		}
		Texture2D texture2D = Load(text, array, !flag && num > 1);
		string text5 = text4 + ".png";
		Sprite sprite2 = Create(text, texture2D, sprite);
		ModLog.Info($"立绘 {text} 已生成：{texture2D.width}x{texture2D.height}，来自 {text5}，多级纹理 {texture2D.mipmapCount} 级");
		return Tiers[text] = sprite2;
	}

	private static int CurrentTier()
	{
		int num = Settings.PortraitSet == 3 ? Hd : Settings.PortraitSet == 2 ? Pixel : Smooth;
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

	// smooth：不带多级纹理时用双线性过滤（高清立绘），否则点采样（像素画）
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
			texture2D.filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point;
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

	// 把不透明像素的颜色向透明区扩 8 圈（透明度不变），免得多级纹理缩小时边缘发黑。
	// 只处理正在扩的那一圈，不再每圈扫整张图；每圈结果和逐遍全图扫描相同。
	private static void BleedEdges(Color32[] px, int w, int h)
	{
		bool[] filled = new bool[px.Length];
		for (int i = 0; i < px.Length; i++)
		{
			filled[i] = px[i].a > 0;
		}
		bool[] queued = new bool[px.Length];
		List<int> ring = new List<int>();
		for (int i = 0; i < px.Length; i++)
		{
			if (filled[i])
			{
				Enqueue(i, w, h, filled, queued, ring);
			}
		}
		List<int> next = new List<int>();
		Color32[] colors = new Color32[ring.Count];
		for (int pass = 0; pass < 8 && ring.Count > 0; pass++)
		{
			if (colors.Length < ring.Count)
			{
				colors = new Color32[ring.Count];
			}
			// 先按上一圈的状态算出这一圈的颜色，再统一写回
			for (int j = 0; j < ring.Count; j++)
			{
				int p = ring[j];
				int x = p % w;
				int r = 0, g = 0, b = 0, n = 0;
				if (x > 0 && filled[p - 1]) { r += px[p - 1].r; g += px[p - 1].g; b += px[p - 1].b; n++; }
				if (x < w - 1 && filled[p + 1]) { r += px[p + 1].r; g += px[p + 1].g; b += px[p + 1].b; n++; }
				if (p >= w && filled[p - w]) { r += px[p - w].r; g += px[p - w].g; b += px[p - w].b; n++; }
				if (p < px.Length - w && filled[p + w]) { r += px[p + w].r; g += px[p + w].g; b += px[p + w].b; n++; }
				colors[j] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 0);
			}
			for (int j = 0; j < ring.Count; j++)
			{
				px[ring[j]] = colors[j];
				filled[ring[j]] = true;
			}
			next.Clear();
			for (int j = 0; j < ring.Count; j++)
			{
				Enqueue(ring[j], w, h, filled, queued, next);
			}
			List<int> swap = ring;
			ring = next;
			next = swap;
		}
	}

	// 把 p 四周还透明、没排过队的像素排进下一圈
	private static void Enqueue(int p, int w, int h, bool[] filled, bool[] queued, List<int> ring)
	{
		int x = p % w;
		if (x > 0) Add(p - 1);
		if (x < w - 1) Add(p + 1);
		if (p >= w) Add(p - w);
		if (p < w * (h - 1)) Add(p + w);
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




