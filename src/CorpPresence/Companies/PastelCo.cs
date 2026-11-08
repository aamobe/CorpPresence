using System;
using System.Collections.Generic;
using CorpPresence.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CorpPresence;

internal sealed class PastelCo : Company
{
	public const string LimitedTag = "CORP_HEMINGWAY_LIMITED";

	public const long LimitedValue = 200L;

	public const string LimitedSprite = "corp_med_hemingway_limited";

	private const string OverlayName = "CorpLimitedOverlay";

	private static Sprite _overlay;

	public const double RebateRate = 0.1;

	public static readonly CatalogEntry[] Entries = new CatalogEntry[19]
	{
		new CatalogEntry
		{
			Id = "nutrifruit",
			Page = "日用"
		},
		new CatalogEntry
		{
			Id = "galaxy_blend",
			Page = "日用"
		},
		new CatalogEntry
		{
			Id = "shampoo",
			Page = "日用"
		},
		new CatalogEntry
		{
			Id = "toothpaste",
			Page = "日用"
		},
		new CatalogEntry
		{
			Id = "pack_condom",
			Page = "日用"
		},
		new CatalogEntry
		{
			Id = "kotton_fabric",
			Page = "日用",
			MinTier = 2
		},
		new CatalogEntry
		{
			Id = "box_tampon",
			Page = "日用",
			MinTier = 2
		},
		new CatalogEntry
		{
			Id = "black_injector",
			Page = "日用",
			MinTier = 3
		},
		new CatalogEntry
		{
			Id = "@gloves",
			Page = "日用",
			MinTier = 2,
			Label = "韧棉工装手套"
		},
		new CatalogEntry
		{
			Id = "@sign",
			Page = "日用",
			MinTier = 3,
			Label = "帕斯泰尔特约店灯牌"
		},
		new CatalogEntry
		{
			Id = "@hemingway_genuine",
			Page = "海明威",
			Label = "海明威香烟（正品）",
			LabelKey = "companies.catalog.hemingwayCigarettesGenuine.name",
			LabelEn = "Hemingway Cigarettes (Genuine)"
		},
		new CatalogEntry
		{
			Id = "cigarette_color",
			Page = "海明威"
		},
		new CatalogEntry
		{
			Id = "cigarette_guide",
			Page = "海明威"
		},
		new CatalogEntry
		{
			Id = "@patch",
			Page = "海明威",
			MinTier = 2,
			Label = "海明威戒烟贴"
		},
		new CatalogEntry
		{
			Id = "@hemingway_limited",
			Page = "海明威",
			MinTier = 3,
			WeeklyLimit = 2,
			Label = "海明威限量包装",
			LabelKey = "companies.catalog.hemingwayLimitedEdition.name",
			LabelEn = "Hemingway Limited Edition"
		},
		new CatalogEntry
		{
			Id = "@tobacco_seed",
			Page = "工艺"
		},
		new CatalogEntry
		{
			Id = "@rolling_machine",
			Page = "工艺"
		},
		new CatalogEntry
		{
			Id = "@packaging_machine",
			Page = "工艺"
		},
		new CatalogEntry
		{
			Id = "@hemingway_stamp",
			Page = "工艺",
			WeeklyLimit = 2
		}
	};

	public const int LooseNeeded = 10;

	public const int PacksNeeded = 6;

	public const string RollerSet = "@craft_roller_set";

	public const string PackerSet = "@craft_packer_set";

	public override string Id => "pastel";

	public override string Name => L.K("companies.pastel.name.pastelInc", "帕斯泰尔公司", "pastel Inc.");

	public override string CharacterId => "milo";

	public override long PhoneNumber => 72783L;

	public override string[] TierNames => new string[3] { L.K("companies.pastel.tierNames.signed", "签约", "Signed"), L.K("companies.pastel.tierNames.regularClient", "长期客户", "Regular Client"), L.K("companies.pastel.tierNames.partner", "合作伙伴", "Partner") };

	public override string CardFlavor => L.K("companies.pastel.cardFlavor.orderHotlineNutrifruitAdBack", "订货热线 72783。背面印着营养果的广告。", "Order hotline 72783. A Nutrifruit ad on the back.");

	public override string DeskName => L.K("companies.pastel.deskName.pastelFrontDesk", "帕斯泰尔前台", "pastel Front Desk");

	public override string DeskAsk => L.K("companies.pastel.deskAsk.wantSeeTodaySList", "看看今天的单子？", "Want to see today's list?");

	public override CatalogEntry[] Catalog => Entries;

	private static (string Zh, string En) LimitedFlavor => (Zh: "海明威分部的限量包装", En: "Hemingway division Limited Edition.");

	public override string[] FrontDesk => new string[3]
	{
		L.K("companies.pastel.frontDesk.pastelOrderHotlineWhatLl", "帕斯泰尔订货热线！今天要点什么？", "pastel order hotline! What'll it be today?"),
		L.K("companies.pastel.frontDesk.helloPastelNutrifruitSStock", "您好，帕斯泰尔。营养果今天有货。", "Hello, pastel. Nutrifruit's in stock today."),
		L.K("companies.pastel.frontDesk.pastelWeVeGotEverything", "帕斯泰尔，什么都有。您说。", "pastel. We've got everything. Go ahead.")
	};

	public override string FrontDeskDone => L.K("companies.pastel.frontDeskDone.gotArrivingTomorrow", "好嘞，明天就到！", "You got it. Arriving tomorrow!");

	public static int DealerFee => Services.Fee("pastel", "dealer", 300);

	public static int TrafficFee => Services.Fee("pastel", "traffic", 40);

	public static int RebatePeriod => Settings.Period("rebate", 7);

	public static int Bribe => Services.Fee("pastel", "bribe", 1000);

	private static CharacterState Milo => CorpState.Get()?.Of("milo");

	private static CompanyState State => CorpState.Get()?.CompanyOf("pastel");

	public static bool Bribed => State?.Bribed ?? false;

	public static bool BribePending
	{
		get
		{
			if (CraftDone(2))
			{
				return !Bribed;
			}
			return false;
		}
	}

	public static GameItem GenuineHemingway(bool limited = false)
	{
		GameItem gameItem = DirectoryMaster.Item("packet_red_cigarette", isOwned: false);
		if (gameItem == null)
		{
			return null;
		}
		InsCigaretteHelper.SetGenuine(gameItem);
		try
		{
			gameItem.AddItemFeature(ItemFeatureList.CigaretteAuthenticity());
		}
		catch (Exception ex)
		{
			ModLog.Warn("海明威正品特征失败：" + ex.Message);
		}
		if (limited)
		{
			try
			{
				gameItem.EnableTag("CORP_HEMINGWAY_LIMITED");
			}
			catch
			{
			}
			ApplyLimited(gameItem);
		}
		return gameItem;
	}

	public static void ApplyLimited(GameItem item)
	{
		if (item == null)
		{
			return;
		}
		try
		{
			item.SetName(L.K("companies.pastel.applyLimited.hemingwayLimitedEdition", "海明威限量包装", "Hemingway Limited Edition"));
		}
		catch
		{
		}
		try
		{
			item.SetValue(200L);
		}
		catch
		{
		}
		try
		{
			item.flavorText = L.K("companies.pastel.limitedFlavor", LimitedFlavor.Zh, LimitedFlavor.En);
		}
		catch
		{
		}
		try
		{
			if (item.spritePath != "corp_med_hemingway_limited")
			{
				item.SetSprite("", "corp_med_hemingway_limited");
				Goods.Redraw(item);
			}
		}
		catch
		{
		}
	}

	public static void AfterCigaretteUI(CigaretteUI __instance, GameItem gameItem)
	{
		try
		{
			if (__instance == null || __instance.boxes == null || __instance.boxes.Count == 0)
			{
				return;
			}
			GameObject gameObject = __instance.boxes[0];
			Transform transform = gameObject.transform.parent.Find("CorpLimitedOverlay");
			if (gameItem == null || !gameItem.IsTag("CORP_HEMINGWAY_LIMITED"))
			{
				if (transform != null)
				{
					transform.gameObject.SetActive(value: false);
				}
				return;
			}
			GameObject gameObject2 = ((transform != null) ? transform.gameObject : CreateOverlay(gameObject));
			if (!(gameObject2 == null))
			{
				gameObject2.transform.SetAsLastSibling();
				gameObject2.SetActive(value: true);
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("限量包装鉴定图失败：" + ex.Message);
		}
	}

	private static GameObject CreateOverlay(GameObject box)
	{
		Texture2D texture2D = Tex.Art("pastel", "hemingway_limited_overlay");
		if (texture2D == null)
		{
			return null;
		}
		GameObject gameObject = new GameObject("CorpLimitedOverlay");
		gameObject.transform.SetParent(box.transform.parent, worldPositionStays: false);
		Image component = box.GetComponent<Image>();
		if (component != null)
		{
			if ((object)_overlay == null)
			{
				_overlay = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), (component.sprite != null) ? component.sprite.pixelsPerUnit : 100f);
			}
			RectTransform rectTransform = (RectTransform)box.transform;
			RectTransform rectTransform2 = gameObject.AddComponent<RectTransform>();
			rectTransform2.anchorMin = rectTransform.anchorMin;
			rectTransform2.anchorMax = rectTransform.anchorMax;
			rectTransform2.pivot = rectTransform.pivot;
			rectTransform2.anchoredPosition = rectTransform.anchoredPosition;
			rectTransform2.sizeDelta = rectTransform.sizeDelta;
			rectTransform2.localScale = rectTransform.localScale;
			rectTransform2.localRotation = rectTransform.localRotation;
			Image image = gameObject.AddComponent<Image>();
			image.sprite = _overlay;
			image.raycastTarget = false;
			image.preserveAspect = component.preserveAspect;
			image.material = component.material;
		}
		else
		{
			SpriteRenderer component2 = box.GetComponent<SpriteRenderer>();
			if (component2 == null || component2.sprite == null)
			{
				UnityEngine.Object.Destroy(gameObject);
				return null;
			}
			Sprite sprite = component2.sprite;
			if ((object)_overlay == null)
			{
				_overlay = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height), sprite.pixelsPerUnit);
			}
			gameObject.transform.localPosition = box.transform.localPosition;
			gameObject.transform.localRotation = box.transform.localRotation;
			gameObject.transform.localScale = box.transform.localScale;
			SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
			spriteRenderer.sprite = _overlay;
			spriteRenderer.sortingLayerID = component2.sortingLayerID;
			spriteRenderer.sortingOrder = component2.sortingOrder + 10;
			spriteRenderer.sharedMaterial = component2.sharedMaterial;
		}
		_overlay.name = "CorpLimitedOverlay";
		_overlay.hideFlags = HideFlags.HideAndDontSave;
		return gameObject;
	}

	public static void AfterGenuineAction(GameItem item)
	{
		try
		{
			if (item != null && item.IsTag("CORP_HEMINGWAY_LIMITED"))
			{
				ApplyLimited(item);
			}
		}
		catch
		{
		}
	}

	public static bool CraftDone(int segment)
	{
		return Milo?.SideDone.Contains(5 + segment) ?? false;
	}

	public static bool CraftUnlocked(CatalogEntry e)
	{
		switch (e.Id)
		{
		case "@tobacco_seed":
		case "@rolling_machine":
			return CraftDone(1);
		case "@packaging_machine":
			return Bribed;
		case "@hemingway_stamp":
			return CraftDone(3);
		default:
			return true;
		}
	}

	public static bool PayBribe(bool deliver)
	{
		CompanyState state = State;
		if (state == null || state.Bribed || Services.Pay(Companies.Find("pastel"), Bribe, "bribe") != null)
		{
			return false;
		}
		state.Bribed = true;
		if (deliver)
		{
			state.Orders.Add(new Order
			{
				Day = StoreStation.GetDayCounter() + 1,
				Items = new List<string> { "@packaging_machine" }
			});
		}
		ModLog.Info($"打点主管 {Bribe}₵ 已付" + (deliver ? "，分装机明天送到" : ""));
		return true;
	}

	public static void CountLoose(GameItem item)
	{
		CompanyState state = State;
		if (state != null && item?.identifier == "corp_presence_loose_cigarette")
		{
			state.LooseSold++;
		}
	}

	public static void CountPacks(int n)
	{
		CompanyState state = State;
		if (state != null && n > 0)
		{
			state.PacksMade += n;
		}
	}

	public static bool HemingwayOpen()
	{
		CharacterState characterState = CorpState.Get()?.Of("milo");
		if (characterState != null)
		{
			return characterState.Chapter >= 3;
		}
		return false;
	}
}
