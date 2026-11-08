using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CorpPresence.UI;

internal static class Notebook
{
	private const long Exchange = 78625L;

	private const int PerPage = 4;

	private const float W = 252f;

	private const float Top = 50f;

	private const float RowH = 34f;

	private const float Foot = 34f;

	private const float H = 220f;

	private const float Pad = 18f;

	private const float Ear = 26f;

	private const int Px = 2;

	private static readonly Color Dim = new Color(0.6f, 0.6f, 0.6f, 1f);

	private static readonly Color Ink = Tex.Hex(3812906);

	private static readonly Color InkSoft = Tex.Hex(6179904);

	private static readonly Color Marker = new Color(0.95f, 0.75f, 0.3f, 0.45f);

	private static GameObject _root;

	private static RectTransform _rows;

	private static Image _paper;

	private static TextMeshProUGUI _pageLabel;

	private static GameObject _prev;

	private static GameObject _next;

	private static readonly Dictionary<string, Sprite> Papers = new Dictionary<string, Sprite>();

	private static readonly List<(RectTransform rt, Image bg)> Hover = new List<(RectTransform, Image)>();

	private static List<long> _numbers = new List<long>();

	private static int _page;

	private static float _flipStart = -1f;

	private static bool _bypass;

	private static int Pages => Math.Max(1, (_numbers.Count + 4 - 1) / 4);

	private static bool Ours(long n)
	{
		if (n != 78625)
		{
			return Companies.ByNumber(n) != null;
		}
		return true;
	}

	public static void AfterShownInPhoneBook(StorePhoneClient client, ref bool shown)
	{
		if (!_bypass && shown && client != null)
		{
			long num = NumberOf(client);
			if (num != 0L && Ours(num))
			{
				shown = false;
			}
		}
	}

	private static long NumberOf(StorePhoneClient client)
	{
		Dictionary<long, StorePhoneClient> dictionary = PlayerStore.Instance?.PhoneClientDict;
		if (dictionary == null)
		{
			return 0L;
		}
		foreach (KeyValuePair<long, StorePhoneClient> item in dictionary)
		{
			if (item.Value != null && item.Value == client)
			{
				return item.Key;
			}
		}
		return 0L;
	}

	private static List<long> Collect()
	{
		List<long> list = (from c in Companies.All
			where Phone.Available(c) && Phone.IsOurs(c.PhoneNumber, c)
			select c.PhoneNumber).ToList();
		Dictionary<long, StorePhoneClient> dictionary = PlayerStore.Instance?.PhoneClientDict;
		if (dictionary == null || !dictionary.ContainsKey(78625L) || dictionary[78625L] == null)
		{
			return list;
		}
		_bypass = true;
		try
		{
			if (dictionary[78625L].ShownInPhoneBook())
			{
				list.Add(78625L);
			}
		}
		catch
		{
		}
		finally
		{
			_bypass = false;
		}
		return list;
	}

	public static void Refresh()
	{
		PhoneUIManager instance = PhoneUIManager.Instance;
		if (instance == null || instance.phonePanel == null)
		{
			return;
		}
		_numbers = Collect();
		if (_root == null)
		{
			Build(instance);
		}
		if (!(_root == null))
		{
			_root.SetActive(_numbers.Count > 0);
			if (_numbers.Count != 0)
			{
				_page = Mathf.Clamp(_page, 0, Pages - 1);
				ShowPage();
			}
		}
	}

	private static void Turn(int delta)
	{
		int num = Mathf.Clamp(_page + delta, 0, Pages - 1);
		if (num == _page)
		{
			return;
		}
		_page = num;
		ShowPage();
		_flipStart = Time.unscaledTime;
		try
		{
			PhoneUIManager.Instance?.phonePickup?.Play();
		}
		catch
		{
		}
	}

	private static void ShowPage()
	{
		for (int num = _rows.childCount - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(_rows.GetChild(num).gameObject);
		}
		Hover.Clear();
		List<long> list = _numbers.Skip(_page * 4).Take(4).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			AddRow(list[i], i);
		}
		bool flag = _page > 0;
		bool flag2 = _page < Pages - 1;
		_prev.SetActive(flag);
		_next.SetActive(flag2);
		if (_pageLabel != null)
		{
			_pageLabel.text = ((Pages > 1) ? $"{_page + 1} / {Pages}" : "");
		}
		_paper.sprite = PaperSprite(flag, flag2);
	}

	public static void Tick()
	{
		if (_root == null || !_root.activeInHierarchy)
		{
			return;
		}
		Canvas componentInParent = _root.GetComponentInParent<Canvas>();
		Camera cam = ((componentInParent != null && componentInParent.renderMode != RenderMode.ScreenSpaceOverlay) ? componentInParent.worldCamera : null);
		Vector2 screenPoint = Input.mousePosition;
		foreach (var (rectTransform, image) in Hover)
		{
			if (rectTransform != null && image != null)
			{
				image.color = (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, cam) ? Marker : Color.clear);
			}
		}
		if (_flipStart >= 0f)
		{
			float num = Mathf.Clamp01((Time.unscaledTime - _flipStart) / 0.18f);
			_rows.localScale = new Vector3(Mathf.Lerp(0.1f, 1f, 1f - (1f - num) * (1f - num)), 1f, 1f);
			if (num >= 1f)
			{
				_flipStart = -1f;
			}
		}
	}

	private static void Build(PhoneUIManager phone)
	{
		try
		{
			_root = new GameObject("CorpPresence_Notebook");
			_root.transform.SetParent(phone.phonePanel.transform, worldPositionStays: false);
			RectTransform rectTransform = _root.AddComponent<RectTransform>();
			Vector2 anchorMin = (rectTransform.anchorMax = new Vector2(1f, 1f));
			rectTransform.anchorMin = anchorMin;
			rectTransform.pivot = new Vector2(0f, 1f);
			rectTransform.anchoredPosition = new Vector2(10f, -24f);
			rectTransform.sizeDelta = new Vector2(252f, 220f);
			_paper = _root.AddComponent<Image>();
			_paper.color = Dim;
			_paper.raycastTarget = true;
			Label(_root.transform, L.K("ui.notebook.panel.notebook", "记事簿", "Notebook"), new Vector2(22f, 16f), 216f, Ink, TextAlignmentOptions.Left, bold: true);
			_pageLabel = Label(_root.transform, "", new Vector2(86f, 192f), 80f, InkSoft, TextAlignmentOptions.Center);
			GameObject gameObject = new GameObject("Rows");
			gameObject.transform.SetParent(_root.transform, worldPositionStays: false);
			_rows = gameObject.AddComponent<RectTransform>();
			RectTransform rows = _rows;
			anchorMin = (_rows.anchorMax = new Vector2(0f, 1f));
			rows.anchorMin = anchorMin;
			_rows.pivot = new Vector2(0f, 1f);
			_rows.anchoredPosition = new Vector2(18f, -50f);
			_rows.sizeDelta = new Vector2(216f, 136f);
			_prev = Corner(new Vector2(0f, -194f), () =>
			{
				Turn(-1);
			});
			_next = Corner(new Vector2(226f, -194f), () =>
			{
				Turn(1);
			});
		}
		catch (Exception ex)
		{
			ModLog.Warn("记事簿创建失败：" + ex.Message);
			if (_root != null)
			{
				UnityEngine.Object.Destroy(_root);
			}
			_root = null;
		}
	}

	public static void Destroy()
	{
		try
		{
			if (_root != null) { _root.SetActive(false); UnityEngine.Object.Destroy(_root); }
		}
		catch { }
		_root = null;
		_rows = null; _paper = null; _pageLabel = null; _prev = null; _next = null;
		Hover.Clear(); _numbers.Clear(); _page = 0; _flipStart = -1f; _bypass = false;
		foreach (Sprite paper in Papers.Values)
		{
			if (paper == null) continue;
			try { if (paper.texture != null) UnityEngine.Object.Destroy(paper.texture); UnityEngine.Object.Destroy(paper); }
			catch (Exception error) { try { ModLog.Warn("释放记事簿纸张失败：" + error.Message); } catch { } }
		}
		Papers.Clear();
	}

	private static GameObject Corner(Vector2 pos, Action onClick)
	{
		GameObject gameObject = new GameObject("Corner");
		gameObject.transform.SetParent(_root.transform, worldPositionStays: false);
		RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
		Vector2 anchorMin = (rectTransform.anchorMax = new Vector2(0f, 1f));
		rectTransform.anchorMin = anchorMin;
		rectTransform.pivot = new Vector2(0f, 1f);
		rectTransform.anchoredPosition = pos;
		rectTransform.sizeDelta = new Vector2(26f, 26f);
		gameObject.AddComponent<Image>().color = Color.clear;
		Button button = gameObject.AddComponent<Button>();
		button.transition = Selectable.Transition.None;
		button.onClick.AddListener(() =>
		{
			try
			{
				onClick();
			}
			catch (Exception ex)
			{
				ModLog.Warn("翻页出错：" + ex.Message);
			}
		});
		return gameObject;
	}

	private static GameObject NewElement(Transform parent)
	{
		PhoneBookUIManager phoneBookUIManager = PhoneUIManager.Instance?.phoneBookUIManager;
		if (phoneBookUIManager == null || phoneBookUIManager.contactElementPrefab == null)
		{
			return null;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(phoneBookUIManager.contactElementPrefab, parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		Vector2 anchorMin = (component.anchorMax = new Vector2(0f, 1f));
		component.anchorMin = anchorMin;
		component.pivot = new Vector2(0f, 1f);
		return gameObject;
	}

	private static IEnumerable<Image> Images(GameObject go)
	{
		Transform[] componentsInChildren = go.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Image image = Compat.Get<Image>(componentsInChildren[i]);
			if (image != null)
			{
				yield return image;
			}
		}
	}

	private static void AddRow(long number, int index)
	{
		GameObject gameObject = NewElement(_rows);
		if (gameObject == null)
		{
			return;
		}
		gameObject.name = "Row_" + number;
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchoredPosition = new Vector2(0f, (float)(-index) * 34f - 4f);
		component.sizeDelta = new Vector2(216f, 26f);
		foreach (Image item in Images(gameObject))
		{
			item.color = Color.clear;
			item.raycastTarget = false;
		}
		GameObject gameObject2 = new GameObject("Marker");
		gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		gameObject2.transform.SetAsFirstSibling();
		RectTransform rectTransform = gameObject2.AddComponent<RectTransform>();
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = new Vector2(0f, 1f);
		rectTransform.pivot = new Vector2(0f, 0.5f);
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.sizeDelta = new Vector2(212f, 0f);
		Image image = gameObject2.AddComponent<Image>();
		image.color = Color.clear;
		image.raycastTarget = true;
		ContactElement component2 = gameObject.GetComponent<ContactElement>();
		if (!(component2 == null))
		{
			component2.OnInit(number);
			float num = 216f;
			if (component2.titleTMP != null)
			{
				component2.titleTMP.color = Ink;
				component2.titleTMP.alignment = TextAlignmentOptions.Left;
				Place(component2.titleTMP.rectTransform, 4f, num - 80f);
			}
			if (component2.statusTMP != null)
			{
				component2.statusTMP.text = number.ToString();
				component2.statusTMP.color = InkSoft;
				component2.statusTMP.alignment = TextAlignmentOptions.Right;
				Place(component2.statusTMP.rectTransform, num - 76f, 68f);
			}
			Hover.Add((rectTransform, image));
		}
	}

	private static void Place(RectTransform rt, float x, float width)
	{
		rt.anchorMin = new Vector2(0f, 0f);
		rt.anchorMax = new Vector2(0f, 1f);
		rt.pivot = new Vector2(0f, 0.5f);
		rt.anchoredPosition = new Vector2(x, 0f);
		rt.sizeDelta = new Vector2(width, 0f);
	}

	private static TextMeshProUGUI Label(Transform parent, string text, Vector2 pos, float width, Color color, TextAlignmentOptions align, bool bold = false)
	{
		GameObject gameObject = NewElement(parent);
		if (gameObject == null)
		{
			return null;
		}
		gameObject.name = "Label";
		foreach (Image item in Images(gameObject))
		{
			item.enabled = false;
		}
		Transform[] componentsInChildren = gameObject.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Selectable selectable = Compat.Get<Selectable>(componentsInChildren[i]);
			if (selectable != null)
			{
				selectable.enabled = false;
			}
		}
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchoredPosition = new Vector2(pos.x, 0f - pos.y);
		component.sizeDelta = new Vector2(width, 24f);
		ContactElement component2 = gameObject.GetComponent<ContactElement>();
		if (component2 == null)
		{
			return null;
		}
		TextMeshProUGUI titleTMP = component2.titleTMP;
		if (component2.statusTMP != null)
		{
			component2.statusTMP.text = "";
		}
		UnityEngine.Object.Destroy(component2);
		if (titleTMP == null)
		{
			return null;
		}
		titleTMP.text = text;
		titleTMP.color = color;
		titleTMP.alignment = align;
		if (bold)
		{
			titleTMP.fontStyle = FontStyles.Bold;
		}
		return titleTMP;
	}

	private static Sprite PaperSprite(bool prev, bool next)
	{
		string key = $"{prev}{next}";
		if (Papers.TryGetValue(key, out var value) && value != null)
		{
			return value;
		}
		int num = 126;
		int num2 = 110;
		int num3 = 13;
		Texture2D texture2D = new Texture2D(num, num2, TextureFormat.RGBA32, mipChain: false)
		{
			hideFlags = HideFlags.HideAndDontSave,
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp
		};
		Color clear = Color.clear;
		Color color = Tex.Hex(15260860);
		Color color2 = Tex.Hex(13615516);
		Color color3 = Tex.Hex(4864816);
		Color color4 = Tex.Hex(11122630);
		Color color5 = Tex.Hex(13135964);
		Color color6 = Tex.Hex(5982782);
		Color color7 = Tex.Hex(2761245);
		Color color8 = Tex.Hex(13483412);
		Color color9 = Tex.Hex(9075294);
		int num4 = num - 3;
		int num5 = num2 - 3;
		int num6 = 6;
		int num7 = 25;
		int num8 = 17;
		int num9 = num5 - 17;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				int num10 = num2 - 1 - i;
				Color color10 = clear;
				for (int num11 = 3; num11 >= 1; num11 -= 2)
				{
					if (j >= num11 && num10 >= num11 && j < num4 + num11 && num10 < num5 + num11)
					{
						color10 = ((j == num4 + num11 - 1 || num10 == num5 + num11 - 1) ? color3 : color2);
					}
				}
				if (j < num4 && num10 < num5)
				{
					color10 = color;
					if (num10 >= num7 && num10 < num9 && (num10 - num7) % num8 == num8 - 1 && j > 2 && j < num4 - 3)
					{
						color10 = color4;
					}
					if (j == 6 && num10 >= num6)
					{
						color10 = color5;
					}
					if (num10 < num6)
					{
						color10 = color6;
					}
					if (num10 >= 2 && num10 <= 3 && j % 12 == 6)
					{
						color10 = color7;
					}
					if (j == 0 || j == num4 - 1 || num10 == 0 || num10 == num5 - 1)
					{
						color10 = color3;
					}
					int num12 = num4 - 1 - j;
					int num13 = num5 - 1 - num10;
					if (next && num12 + num13 < num3)
					{
						color10 = ((num12 + num13 == num3 - 1) ? color9 : color8);
					}
					if (prev && j + num13 < num3)
					{
						color10 = ((j + num13 == num3 - 1) ? color9 : color8);
					}
				}
				texture2D.SetPixel(j, i, color10);
			}
		}
		texture2D.Apply();
		return Papers[key] = Sprite.Create(texture2D, new Rect(0f, 0f, num, num2), new Vector2(0.5f, 0.5f), 50f);
	}
}

