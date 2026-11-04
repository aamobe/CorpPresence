using System;
using System.Collections.Generic;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static class Cards
{
	private const string BaseItem = "card_pharma";

	public const string Prefix = "corp_card_";

	private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

	private static Sprite _vanilla;

	private static bool _warned;

	public static string KeyOf(Company c)
	{
		return "corp_card_" + c.Id;
	}

	public static GameItem Make(Company c)
	{
		if (c is SpacerCo)
		{
			return SpacerCo.MakeNote();
		}
		ItemIdentity.Entry entry = ItemIdentity.ForCard(c?.Id);
		if (!(entry != null))
		{
			return null;
		}
		return ItemRegistry.Create(entry.Id);
	}

	private static void ApplyText(GameItem card, Company c)
	{
		try
		{
			card.SetName(L.T("名片（" + c.Name + "）", "Business Card (" + c.Name + ")"));
		}
		catch
		{
		}
		try
		{
			card.flavorText = c.CardFlavor;
		}
		catch
		{
		}
	}

	public static void Relabel(GameItem card, ItemIdentity.Entry entry)
	{
		Company company = (((object)entry == null || entry.Goods == null) ? Companies.Find(entry?.Company) : null);
		if (card != null && company != null && !(company is SpacerCo))
		{
			ApplyText(card, company);
		}
	}

	internal static GameItem Build(ItemIdentity.Entry entry)
	{
		Company company = Companies.Find(entry?.Company);
		if (company == null)
		{
			return null;
		}
		GameItem gameItem = DirectoryMaster.Item(entry.LegacyId, isOwned: false);
		if (gameItem == null)
		{
			return null;
		}
		gameItem.identifier = entry.Id;
		ApplyText(gameItem, company);
		try
		{
			gameItem.SetSprite("", KeyOf(company));
		}
		catch (Exception ex)
		{
			ModLog.Warn("名片换图失败：" + ex.Message);
		}
		return gameItem;
	}

	public static string IdOf(GameItem item)
	{
		try
		{
			return (item == null) ? null : ItemIdentity.CardKey(item.identifier, item.spritePath);
		}
		catch
		{
			return null;
		}
	}

	public static Sprite Find(string name)
	{
		if (string.IsNullOrEmpty(name) || !name.StartsWith("corp_card_", StringComparison.Ordinal))
		{
			return null;
		}
		if (Sprites.TryGetValue(name, out var value) && value != null)
		{
			return value;
		}
		Texture2D texture2D = Tex.Art(name.Substring("corp_card_".Length), "card");
		if (texture2D == null)
		{
			return VanillaCard();
		}
		Sprite sprite = VanillaCard();
		float num = ((sprite != null && (float)texture2D.width > sprite.rect.width * 1.01f) ? ((float)texture2D.width / sprite.rect.width) : 1f);
		if (num > 1.01f)
		{
			texture2D.filterMode = FilterMode.Bilinear;
		}
		float pixelsPerUnit = ((sprite != null) ? sprite.pixelsPerUnit : 100f) * num;
		Vector2 pivot = ((sprite != null) ? new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height) : new Vector2(0.5f, 0.5f));
		value = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), pivot, pixelsPerUnit);
		value.name = name;
		value.hideFlags = HideFlags.HideAndDontSave;
		Sprites[name] = value;
		return value;
	}

	private static Sprite VanillaCard()
	{
		if (_vanilla != null)
		{
			return _vanilla;
		}
		_vanilla = Goods.VanillaByName("card_pharma");
		if (_vanilla == null && !_warned)
		{
			_warned = true;
			ModLog.Warn("没找到原版名片图，名片按默认轴心显示");
		}
		return _vanilla;
	}
}
