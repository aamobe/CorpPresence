using System.Collections.Generic;
using CorpPresence.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CorpPresence;

internal static class CardView
{
	private static readonly Dictionary<string, GameObject> Panels = new Dictionary<string, GameObject>();

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
		if (!Panels.TryGetValue(text, out var value) || value == null)
		{
			value = Object.Instantiate(instance.cardPharma, instance.cardPharma.transform.parent, worldPositionStays: false);
			value.name = "CorpPresence_Card_" + text;
			Setup(value, company);
			Panels[text] = value;
		}
		value.SetActive(value: true);
		value.transform.SetAsLastSibling();
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
		foreach (GameObject value in Panels.Values)
		{
			if (value != null && value.activeSelf)
			{
				value.SetActive(value: false);
				try
				{
					PaperUIManager.Instance?.audioSourceDrop?.Play();
				}
				catch
				{
				}
			}
		}
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
				image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
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
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			Button button = Compat.Get<Button>(componentsInChildren2[j]);
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
