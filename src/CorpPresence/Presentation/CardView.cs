using CorpPresence.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CorpPresence;

internal static class CardView
{
	private static GameObject _panel;
	private static Sprite _sprite;
	private static string _companyId;

	public static bool IsOurs(GameItem item)
	{
		return Cards.IdOf(item) != null;
	}

	public static void Open(GameItem item)
	{
		ItemRegistry.Migrate(item);
		string text = Cards.IdOf(item);
		if (text == null)
		{
			return;
		}
		Company company = Companies.Find(text);
		PaperUIManager instance = PaperUIManager.Instance;
		if (company == null || instance == null || instance.cardPharma == null)
		{
			return;
		}
		CloseAll();
		_companyId = company.Id;
		try
		{
			_panel = Object.Instantiate(instance.cardPharma, instance.cardPharma.transform.parent, worldPositionStays: false);
			_panel.name = "CorpPresence_Card_" + text;
			Setup(_panel, company);
			_panel.SetActive(value: true);
			_panel.transform.SetAsLastSibling();
		}
		catch
		{
			Release();
			throw;
		}
		try
		{
			instance.audioSourcePickup?.Play();
		}
		catch
		{
		}
	}

	public static void CloseAll()
	{
		bool wasOpen = _panel != null && _panel.activeSelf;
		Release();
		if (wasOpen)
		{
			try { PaperUIManager.Instance?.audioSourceDrop?.Play(); }
			catch { }
		}
	}

	internal static void Release()
	{
		if (_panel != null)
		{
			_panel.SetActive(false);
			Object.Destroy(_panel);
		}
		if (_sprite != null) Object.Destroy(_sprite);
		if (_companyId != null) Tex.ReleaseCardArt(_companyId);
		_panel = null;
		_sprite = null;
		_companyId = null;
	}

	private static void Setup(GameObject panel, Company c)
	{
		TextMeshProUGUI[] componentsInChildren = panel.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
		Texture2D texture2D = Tex.Art(c.Id, "card_big");
		if (texture2D != null)
		{
			Image image = Compat.Get<Image>(panel);
			if (image != null)
			{
				_sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				image.sprite = _sprite;
			}
			TextMeshProUGUI[] array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(value: false);
			}
		}
		else
		{
			if (componentsInChildren.Length != 0)
			{
				componentsInChildren[0].text = c.Name;
			}
			if (componentsInChildren.Length > 1)
			{
				componentsInChildren[1].text = c.PhoneNumber.ToString();
			}
		}
		Transform[] componentsInChildren2 = panel.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			Button button = Compat.Get<Button>(componentsInChildren2[i]);
			if (!(button == null))
			{
				button.onClick = new Button.ButtonClickedEvent();
				button.onClick.AddListener(() =>
				{
					CloseAll();
				});
			}
		}
	}
}
