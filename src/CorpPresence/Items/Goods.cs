using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.Malice;
using CorpPresence.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CorpPresence;

internal static partial class Goods
{
	internal enum ArtKind { Embedded, NativeSprite, TintedNativeSprite }
	internal sealed class Def
	{
		// Explicitly describes existing fallback-only art; Embedded remains the default.
		internal ArtKind ArtSource;
		internal GoodsText.Keys TextKeys;
		public string Name;

		public string Flavor;

		public string Desc;

		public string Company;

		public string Art;

		public string Manufacturer;

		public string NameEn;

		public string FlavorEn;

		public string DescEn;

		public string[] Types;

		public string Shape;

		public long Value;

		public int W;

		public int H;

		public bool Usable;

		public int[] Gun;

		public int[] Power;

		public int Durability;

		public bool VanillaUse;

		public string Fallback;

		public bool Crate;

		public Func<GameItem> Shell;

		public Action<GameItem> Setup;

		public Action<GameItem> OnNew;

		public (string Zh, string En) Manual;

		public (string Zh, string En) ModuleEffects;

		public string LName => L.K(TextKeys?.Name, Name, NameEn ?? Name);

		public string LFlavor => L.K(TextKeys?.Flavor, Flavor, FlavorEn ?? Flavor);

		public string LDesc
		{
			get
			{
				if (Desc != null)
				{
					string text = L.K(TextKeys?.Description, Desc, DescEn ?? Desc);
					return ModuleEffects.Zh == null ? text : text + "\n\n" + LocHelper.BulletLines(L.K(TextKeys?.Effects, ModuleEffects.Zh, ModuleEffects.En));
				}
				return LFlavor;
			}
		}
	}

	public const string Prefix = "corp_med_";

	private const string RefSprite = "injector_pink_alt";

	private static readonly IReadOnlyDictionary<string, Def> Defs;

	private const string CandleLit = "candle_lit";

	private static readonly Dictionary<string, (string Company, string Art)> ExtraArt;

	private static readonly Dictionary<string, Sprite> Sprites;

	private static readonly Dictionary<string, int> NativeWidth;

	private static readonly Dictionary<string, Sprite> VanillaCache;

	private static readonly Dictionary<string, float> VanillaMissAt;

	private static readonly HashSet<StoreClient> Boosted;

	private static int _boostDay;

	private static int Day => StoreStation.GetDayCounter();

	private static MedState M => CorpState.Get()?.Meds;

	public static int ExtraScavAttempts => Math.Min(M?.StaminaShots ?? 0, Settings.VanillaServices ? 2 : int.MaxValue);

	public static bool PainBlockedToday
	{
		get
		{
			if (M != null)
			{
				return M.PainBlockDay == Day;
			}
			return false;
		}
	}

	public static bool PatchActive
	{
		get
		{
			if (M != null)
			{
				return Day <= M.PatchUntil;
			}
			return false;
		}
	}

	public static bool ElixirActive
	{
		get
		{
			if (M != null)
			{
				return Day <= M.ElixirUntil;
			}
			return false;
		}
	}

	public static bool CandleActive
	{
		get
		{
			if (M != null)
			{
				return Day <= M.CandleUntil;
			}
			return false;
		}
	}

	static Goods()
	{
		Defs = Catalog.Defs;
		ExtraArt = Catalog.ExtraArt;
		Sprites = new Dictionary<string, Sprite>();
		NativeWidth = new Dictionary<string, int>
		{
			["sb_stage_small"] = 48,
			["sb_stage"] = 64,
			["sb_stage_large"] = 80
		};
		VanillaCache = new Dictionary<string, Sprite>();
		VanillaMissAt = new Dictionary<string, float>();
		Boosted = new HashSet<StoreClient>();
		_boostDay = -1;
	}

	public static bool Has(string id)
	{
		return Defs.ContainsKey(id);
	}

	private static void ApplyText(GameItem item, Def d)
	{
		try
		{
			item.SetName(d.LName);
		}
		catch
		{
		}
		try
		{
			item.flavorText = d.Desc == null ? "" : d.LFlavor ?? "";
		}
		catch
		{
		}
		try
		{
			item.shortDescription = d.LDesc + MachineUpgrade.Status(item);
		}
		catch
		{
		}
	}

	public static void Relabel(GameItem item, string goodsId)
	{
		if (item != null && goodsId != null && Defs.TryGetValue(goodsId, out var value))
		{
			ApplyText(item, value);
			if (value.Manual.Zh != null) Tobacco.RefreshManual(item, value);
		}
	}

	internal static string NameOf(string id) => Defs[id].LName;

	public static long ValueOf(string id)
	{
		if (id == null || !Defs.TryGetValue(id, out var value))
		{
			return 0L;
		}
		return value.Value;
	}

	public static string[] TypesOf(string id)
	{
		if (id == null || !Defs.TryGetValue(id, out var value))
		{
			return null;
		}
		return value.Types;
	}

	internal static bool HasIndependentShell(string id) => id != null && Defs.TryGetValue(id, out var definition)
		&& (definition.Shell != null || definition.Crate);

	public static bool VanillaUse(GameItem item)
	{
		string text = IdOf(item);
		if (text != null && Defs.TryGetValue(text, out var value))
		{
			return value.VanillaUse;
		}
		return false;
	}

	public static GameItem MakeSpecial(string id)
	{
		if (id == null || !id.StartsWith("@") || !Has(id.Substring(1)))
		{
			return null;
		}
		return Make(id.Substring(1));
	}

	public static GameItem Make(string id)
	{
		ItemIdentity.Entry entry = ItemIdentity.ForGoods(id);
		GameItem gameItem = (((object)entry != null && entry.Goods != null) ? ItemRegistry.Create(entry.Id) : null);
		if (gameItem != null && Defs.TryGetValue(id, out var value))
		{
			try
			{
				value.OnNew?.Invoke(gameItem);
				if (value.Manual.Zh != null) Tobacco.AddManual(gameItem, value.Manual);
			}
			catch (Exception ex)
			{
				ModLog.Warn(value.Name + "出厂设置失败：" + ex.Message);
			}
		}
		return gameItem;
	}

	internal static GameItem Build(ItemIdentity.Entry entry)
	{
		if ((object)entry == null || entry.Goods == null || !Defs.TryGetValue(entry.Goods, out var value))
		{
			return null;
		}
		GameItem gameItem;
		if (value.Crate)
		{
			gameItem = Crates.CreateShell();
		}
		else
		{
			gameItem = ((value.Shell != null) ? value.Shell() : DirectoryMaster.Item(entry.LegacyId, isOwned: false));
		}
		if (gameItem == null)
		{
			return null;
		}
		gameItem.identifier = entry.Id;
		ApplyText(gameItem, value);
		try
		{
			gameItem.SetValue(value.Value);
		}
		catch (Exception ex)
		{
			ModLog.Warn(value.Name + "定价失败：" + ex.Message);
		}
		ApplyAppearance(gameItem, entry, value);
		try
		{
			ItemTags.Apply(gameItem, value.Types, Array.Empty<string>());
		}
		catch
		{
		}
		if (value.Manufacturer != null)
		{
			try
			{
				gameItem.EnableManufacturer(value.Manufacturer);
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				gameItem.DisableTag("manufacturer");
			}
			catch
			{
			}
		}
		if (value.Power != null)
		{
			try
			{
				bool flag = gameItem.IsTag("power_source_item");
				Func<GameItem, GameItem, bool> mayThisTargetItemFunc = gameItem.mayThisTargetItemFunc;
				Func<GameItem, GameItem, bool> canThisTargetItemFunc = gameItem.canThisTargetItemFunc;
				Action<GameItem, GameItem> onThisTargetItemFunc = gameItem.onThisTargetItemFunc;
				PowerHelper.InitPowerSourceItem(gameItem, value.Power[0], value.Power[1]);
				if (flag)
				{
					gameItem.mayThisTargetItemFunc = mayThisTargetItemFunc;
					gameItem.canThisTargetItemFunc = canThisTargetItemFunc;
					gameItem.onThisTargetItemFunc = onThisTargetItemFunc;
				}
				if (value.Power.Length > 2)
				{
					PowerHelper.SetPowerSourceAt(gameItem, value.Power[2]);
				}
				gameItem.SetValue(value.Value);
				long num = gameItem.GetCurrentValue(useRetailMarkup: false, withChild: true, includeEvents: false, forceMarkup: false, 0L) - value.Value;
				if (num > 0)
				{
					gameItem.SetValue(Math.Max(1L, value.Value - num));
				}
				RefreshBattery(gameItem);
			}
			catch (Exception ex4)
			{
				ModLog.Warn(value.Name + "设置电量失败：" + ex4.Message);
			}
		}
		if (value.Durability > 0)
		{
			try
			{
				DurabilityHelper.SetDurability(gameItem, value.Durability, value.Durability, value.Durability);
			}
			catch (Exception ex5)
			{
				ModLog.Warn(value.Name + "设置耐久失败：" + ex5.Message);
			}
		}
		if (value.Gun != null)
		{
			try
			{
				int[] gun = value.Gun;
				GunHelper.InitGun(gameItem, gun[0], gun[1], gun[2], gun[3], gun[4], gun[5], gun[6], 70, Array.IndexOf(value.Types, "MODDABLE") >= 0);
				if (entry.Goods == "rosen_rifle")
				{
					RifleMods.Attach(gameItem);
				}
				GunHelper.SetEquipmentCondition(gameItem, ItemFeatureList.EquipmentCondition.New);
				gameItem.SetValue(value.Value);
			}
			catch (Exception ex6)
			{
				ModLog.Warn(value.Name + "设置枪械属性失败：" + ex6.Message);
			}
		}
		if (value.Setup != null)
		{
			try
			{
				value.Setup(gameItem);
			}
			catch (Exception ex7)
			{
				ModLog.Warn(value.Name + "初始化失败：" + ex7.Message);
			}
		}
		return gameItem;
	}

	private static GameItem CreateMedicineShell()
	{
		var item = ItemDirectory.CreateEmptyItem();
		item.EnableTag("injector");
		item.EnableTag("ITEM_MEDICAL_TAG");
		item.EnableProficiency("MEDICAL_PROFICIENCY");
		AudioHelper.InitInjectorAudio(item);
		return item;
	}

	private static void ApplyAppearance(GameItem gameItem, ItemIdentity.Entry entry, Def value)
	{
		// InitFishItem and the native load/growth callbacks own fish sprites and shapes.
		if (entry.Id == SilverBlood.CreatureId) return;
		try
		{
			gameItem.SetSprite("", entry.LegacySprite);
		}
		catch (Exception ex2)
		{
			ModLog.Warn(value.Name + "换图失败：" + ex2.Message);
		}
		try
		{
			gameItem.shape = ShapeOf(value);
			gameItem.modifiedShape = gameItem.shape.Clone();
		}
		catch (Exception ex3)
		{
			ModLog.Warn(value.Name + "改形状失败：" + ex3.Message);
		}
	}

	internal static void RestoreLegacyFootprints()
	{
		var store = EmporiumEntry.Instance;
		if (store == null) return;
		foreach (GameItem item in store.GetAllItems())
		{
			string key = IdOf(item);
			if (key is "stamina" or "forge_furnace") RestoreFixedFootprint(item, Defs[key]);
			if (key == CargoWarehouse.GoodsKey && item.shape.width == 3 && item.shape.height == 3)
				RestoreFixedFootprint(item, Defs[key]);
			if (key == ProductionWorkBay.GoodsKey && item.spritePath == "machine_bay_ext" &&
				RestoreFixedFootprint(item, Defs[key]))
				item.SetSprite("", ProductionWorkBay.SpriteKey);
		}
	}

	private static bool RestoreFixedFootprint(GameItem item, Def value)
	{
		GridShape old = item.shape;
		if (old.width == value.W && old.height == value.H &&
			Enumerable.Range(0, value.W).All(x => Enumerable.Range(0, value.H).All(y => old.GetLocal(x, y) != 0)))
			return true;
		if (item.parentInventory is not GameGridInventory inventory) return false;

		GridShape placed = item.modifiedShape;
		var replacement = new GridShapeBuilder(value.W, value.H).SetDataFill(1);
		GridShape candidate = replacement.SetTransform(placed).Build();
		if (!inventory.CheckShapePlacement(item, candidate, new List<GameItem>())) return false;
		item.SetSprite(item.spriteAtlasPath, item.spritePath);
		item.SetShape(replacement.SetTransform(old).Build());
		return true;
	}

	public static string IdOf(GameItem item)
	{
		try
		{
			return (item == null) ? null : ItemIdentity.GoodsKey(item.identifier, item.spritePath);
		}
		catch
		{
			return null;
		}
	}

	public static bool IsLitCandle(GameItem item)
	{
		try
		{
			return item != null && ItemIdentity.ResolveId(item.identifier, item.spritePath) == "corp_presence_candle_lit";
		}
		catch
		{
			return false;
		}
	}

	public static bool BeforeBuildShape(string spritePath, ref GridShapeBuilder __result)
	{
		try
		{
			if (spritePath == null || !spritePath.StartsWith("corp_med_", StringComparison.Ordinal))
			{
				return true;
			}
			string text = spritePath.Substring("corp_med_".Length);
			// State variants must keep the explicit machine footprint, not infer it from alpha.
			if (text == RatWheelGenerator.GoodsKey + "_occupied") text = RatWheelGenerator.GoodsKey;
			if (Defs.TryGetValue(text, out var value) || BatteryState(text, out value, out var _))
			{
				__result = ShapeBuilderOf(value);
				return false;
			}
			if (!ExtraArt.TryGetValue(text, out (string, string) value2))
			{
				return true;
			}
			Texture2D texture2D = Tex.Art(value2.Item1, value2.Item2);
			if (texture2D == null)
			{
				return true;
			}
			int num;
			if (NativeWidth.TryGetValue(text, out var value3))
			{
				num = value3;
			}
			else
			{
				num = ((text == "candle_lit" && Defs.TryGetValue("candle", out var value4)) ? (value4.W * 16) : texture2D.width);
			}
			int num2 = Math.Max(1, num / 16);
			int num3 = Math.Max(1, texture2D.width / num2);
			int h = Math.Max(1, texture2D.height / num3);
			__result = new GridShapeBuilder().SetData(Coverage(texture2D, num2, h), num2);
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static void AfterRaycastValid(Image __instance, Vector2 screenPoint, Camera eventCamera, ref bool __result)
	{
		try
		{
			// Most UI graphics already accept their full rectangle; avoid owner lookups.
			if (__instance.alphaHitTestMinimumThreshold <= 0f) return;
			// Only the actual inventory image of one of our items uses occupied cells.
			// Local-space conversion already undoes the parent rotation/flip.
			TreeNodeRender node = __instance.GetComponent<TreeNodeRender>();
			if (node != null && node.FindParentElement() is GameItemElement item
				&& item.background != null && item.background.image == __instance
				&& ItemIdentity.Find(item.identifier, item.spritePath) != null)
			{
				if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(__instance.rectTransform, screenPoint, eventCamera, out var point))
				{
					__result = false;
					return;
				}
				Rect rect = __instance.GetPixelAdjustedRect();
				GridShape shape = item.shape;
				__result = rect.width > 0f && rect.height > 0f && shape != null
					&& ItemHitArea.Cell((point.x - rect.x) / rect.width, (point.y - rect.y) / rect.height,
						shape.width, shape.height, out int x, out int y) && shape.GetLocal(x, y) != 0;
				return;
			}
			Sprite sprite = __instance.sprite;
			if (sprite == null || __instance.alphaHitTestMinimumThreshold <= 0f || sprite.name == null || !sprite.name.StartsWith("corp_med_", StringComparison.Ordinal))
			{
				return;
			}
			Canvas canvas = __instance.canvas;
			float num = ((canvas != null) ? canvas.referencePixelsPerUnit : 100f);
			if (sprite.pixelsPerUnit <= num * 1.01f)
			{
				return;
			}
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(__instance.rectTransform, screenPoint, eventCamera, out var localPoint))
			{
				__result = false;
				return;
			}
			Rect pixelAdjustedRect = __instance.GetPixelAdjustedRect();
			float num2 = (localPoint.x - pixelAdjustedRect.x) / pixelAdjustedRect.width;
			float num3 = (localPoint.y - pixelAdjustedRect.y) / pixelAdjustedRect.height;
			if (num2 < 0f || num2 > 1f || num3 < 0f || num3 > 1f)
			{
				__result = false;
				return;
			}
			Rect textureRect = sprite.textureRect;
			__result = sprite.texture.GetPixelBilinear((textureRect.x + num2 * textureRect.width) / (float)sprite.texture.width, (textureRect.y + num3 * textureRect.height) / (float)sprite.texture.height).a >= __instance.alphaHitTestMinimumThreshold;
		}
		catch
		{
		}
	}

	private static byte[] Coverage(Texture2D tex, int w, int h)
	{
		int num = tex.width / w;
		Color32[] pixels = tex.GetPixels32();
		byte[] array = new byte[w * h];
		for (int i = 0; i < h; i++)
		{
			for (int j = 0; j < w; j++)
			{
				int num2 = 0;
				int num3 = (h - 1 - i) * num;
				int num4 = j * num;
				for (int k = 0; k < num; k++)
				{
					for (int l = 0; l < num; l++)
					{
						if (pixels[(num3 + k) * tex.width + num4 + l].a > 128)
						{
							num2++;
						}
					}
				}
				array[i * w + j] = ((num2 * 50 >= num * num) ? ((byte)1) : ((byte)0));
			}
		}
		return array;
	}

	private static GridShape ShapeOf(Def d)
	{
		return ShapeBuilderOf(d).Build();
	}

	private static GridShapeBuilder ShapeBuilderOf(Def d)
	{
		// Machine housing can have openings, but the entire base needs clear space.
		if (d.Types != null && Array.IndexOf(d.Types, "MACHINE") >= 0)
		{
			return new GridShapeBuilder(d.W, d.H).SetDataFill(d.W, d.H, 1);
		}
		if (d.Shape != null)
		{
			string[] array = d.Shape.Split('|');
			byte[] array2 = new byte[d.W * d.H];
			for (int i = 0; i < d.H; i++)
			{
				for (int j = 0; j < d.W; j++)
				{
					array2[i * d.W + j] = (byte)((i < array.Length && j < array[i].Length && array[i][j] == '#') ? 1u : 0u);
				}
			}
			return new GridShapeBuilder().SetData(array2, d.W);
		}
		Texture2D texture2D = ((d.Art != null) ? Tex.Art(d.Company, d.Art) : null);
		if (texture2D == null || texture2D.width % d.W != 0 || texture2D.height % d.H != 0 || texture2D.width / d.W != texture2D.height / d.H || texture2D.width / d.W < 16)
		{
			return new GridShapeBuilder(d.W, d.H).SetDataFill(d.W, d.H, 1);
		}
		int num = texture2D.width / d.W;
		Color32[] pixels = texture2D.GetPixels32();
		byte[] array3 = new byte[d.W * d.H];
		for (int k = 0; k < d.H; k++)
		{
			for (int l = 0; l < d.W; l++)
			{
				int num2 = 0;
				int num3 = (d.H - 1 - k) * num;
				int num4 = l * num;
				for (int m = 0; m < num; m++)
				{
					for (int n = 0; n < num; n++)
					{
						if (pixels[(num3 + m) * texture2D.width + num4 + n].a > 128)
						{
							num2++;
						}
					}
				}
				array3[k * d.W + l] = ((num2 * 4 >= num * num) ? ((byte)1) : ((byte)0));
			}
		}
		return new GridShapeBuilder().SetData(array3, d.W);
	}

	private static float HdScale(Def d, Texture2D tex)
	{
		if (d == null || d.W <= 0 || tex.width <= d.W * 16)
		{
			return 1f;
		}
		return (float)tex.width / ((float)d.W * 16f);
	}

	public static Sprite FindSprite(string name)
	{
		if (name == Perfume.FlowerId + "_p" || name == Perfume.FlowerId + "_p2") return Perfume.FlowerSprite();
		if (name == "corp_presence_tobacco_leaf_p" || name == "corp_presence_tobacco_leaf_p2")
		{
			return Tobacco.PlantOverlay();
		}
		if (string.IsNullOrEmpty(name) || !name.StartsWith("corp_med_", StringComparison.Ordinal))
		{
			return null;
		}
		if (Sprites.TryGetValue(name, out var value) && value != null)
		{
			return value;
		}
		string text = name.Substring("corp_med_".Length);
		if (!Catalog.TryGetArt(text, out var value2, out var company, out var name2)) return null;
		Texture2D texture2D = Tex.Art(company, name2);
		if (texture2D == null)
		{
			if (value2 == null || value2.Fallback == null)
			{
				return null;
			}
			Sprite fallback = VanillaByName(value2.Fallback);
			return text.StartsWith("fate_coin_", StringComparison.Ordinal) ? MaliceItems.PlatinumCoin(fallback) : fallback;
		}
		if (value2 == null && NativeWidth.TryGetValue(text, out var value4) && texture2D.width > value4)
		{
			value2 = new Def
			{
				W = value4 / 16
			};
		}
		Sprite sprite = VanillaSprite();
		float num = HdScale(value2, texture2D);
		float pixelsPerUnit = ((sprite != null) ? sprite.pixelsPerUnit : 100f) * num;
		Vector2 pivot = ((sprite != null) ? new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height) : new Vector2(0.5f, 0.5f));
		value = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), pivot, pixelsPerUnit);
		value.name = name;
		value.hideFlags = HideFlags.HideAndDontSave;
		return Sprites[name] = value;
	}

	internal static Sprite FindLegacyFurnaceSprite(string name)
	{
		string cacheKey = name + "@2x2";
		if (Sprites.TryGetValue(cacheKey, out var cached) && cached != null) return cached;
		Sprite original = FindSprite(name);
		if (original == null) return null;
		Vector2 pivot = new Vector2(original.pivot.x / original.rect.width, original.pivot.y / original.rect.height);
		Sprite scaled = Sprite.Create(original.texture, original.rect, pivot, original.pixelsPerUnit * 1.5f);
		scaled.name = name;
		scaled.hideFlags = HideFlags.HideAndDontSave;
		return Sprites[cacheKey] = scaled;
	}

	private static bool BatteryState(string key, out Def d, out string suffix) => Catalog.TryBatteryState(key, out d, out suffix);

	internal static void Redraw(GameItem item)
	{
		try
		{
			Compat.As<GameItemElement>(item)?.Validate();
		}
		catch (Exception ex)
		{
			ModLog.Warn("物品重画失败：" + ex.Message);
		}
	}

	public static void RefreshBattery(GameItem item)
	{
		try
		{
			string text = IdOf(item);
			if (text != null && Defs.TryGetValue(text, out var value) && value.Power != null)
			{
				int num = item.GetTagReadonly("power_source_item_energy")?.GetInt() ?? 0;
				int num2 = item.GetTagReadonly("power_source_item_max_energy")?.GetInt() ?? value.Power[0];
				string text2;
				if (num <= 0)
				{
					text2 = "_empty";
				}
				else
				{
					text2 = ((num * 3 < num2 * 2) ? "_half" : "");
				}
				string text3 = "corp_med_" + text + text2;
				if (item.spritePath != text3)
				{
					item.SetSprite("", text3);
					Redraw(item);
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("电池换图失败：" + ex.Message);
		}
	}

	internal static Sprite VanillaByName(string name)
	{
		if (name == null)
		{
			return null;
		}
		if (VanillaCache.TryGetValue(name, out var value) && value != null)
		{
			return value;
		}
		if (VanillaMissAt.TryGetValue(name, out var value2) && Time.unscaledTime - value2 < 10f)
		{
			return null;
		}
		try
		{
			Sprite[] array = Resources.FindObjectsOfTypeAll<Sprite>();
			foreach (Sprite sprite in array)
			{
				if (sprite != null && sprite.name == name)
				{
					value = sprite;
					break;
				}
			}
		}
		catch
		{
		}
		if (value == null)
		{
			VanillaMissAt[name] = Time.unscaledTime;
		}
		return VanillaCache[name] = value;
	}

	private static Sprite VanillaSprite()
	{
		return VanillaByName("injector_pink_alt");
	}

	public static bool CanUse(GameItem item)
	{
		string text = IdOf(item);
		if (text == null || !Defs[text].Usable || IsLitCandle(item))
		{
			return false;
		}
		if (text == "rejuvenation" && !SilverBlood.CanInject())
		{
			return false;
		}
		try
		{
			return item.IsTag("IS_OWNED_TAG");
		}
		catch
		{
			return false;
		}
	}

	public static void Use(GameItem item, string id)
	{
		ItemRegistry.Migrate(item);
		CorpState corpState = CorpState.Get();
		if (corpState == null)
		{
			return;
		}
		MedState meds = corpState.Meds;
		bool flag = true;
		if (id == null)
		{
			return;
		}
		string text;
		switch (id.Length)
		{
		default:
			return;
		case 6:
			switch (id[0])
			{
			default:
				return;
			case 'e':
				if (!(id == "elixir"))
				{
					return;
				}
				meds.ElixirUntil = Math.Max(meds.ElixirUntil, Day + 2);
				text = L.K("items.goods.use.putSocialiteEssenceReputationGained", "你抹上了名流精华。\n3 天内交易得到的声望 +50%。", "You put on the Socialite Essence.\nReputation gained from trades +50% for 3 days.");
				break;
			case 'c':
			{
				if (!(id == "candle"))
				{
					return;
				}
				string id2 = ItemIdentity.ForGoods("candle_lit").Id;
				if (!ItemRegistry.IsAvailable(id2))
				{
					return;
				}
				item.identifier = id2;
				flag = false;
				meds.CandleUntil = Math.Max(meds.CandleUntil, Day + 2);
				try
				{
					item.SetSprite("", "corp_med_candle_lit");
					Redraw(item);
				}
				catch
				{
				}
				ApplyCandle(PlayerStore.Instance?.storeClientManager);
				text = L.K("items.goods.use.lightSolanaScentedCandleUpper", "你点燃了索拉纳香薰蜡烛。\n3 天内上层区顾客的预算 +15%。", "You light the Solana Scented Candle.\nUpper Levels customers' budget +15% for 3 days.");
				break;
			}
			}
			break;
		case 7:
			if (!(id == "stamina"))
			{
				return;
			}
			meds.StaminaShots++;
			text = ((Settings.VanillaServices && meds.StaminaShots > 2) ? L.K("items.goods.use.injectStaminaBoosterButBody", $"你注射了体力增强剂，但身体已经适应了。\n拾荒次数上限最多 +{2}，这一针没有再增加（设置“服务强度”为原版）。", (System.FormattableString)$"You inject the Stamina Booster, but your body has adapted.\nScavenging attempts are capped at +{2}; this shot adds nothing more (Service strength is set to Vanilla).") : L.K("items.goods.use.injectStaminaBoosterDailyScavenging", $"你注射了体力增强剂。\n每天在垃圾场的拾荒次数上限 +1（已注射 {meds.StaminaShots} 针）。", (System.FormattableString)$"You inject the Stamina Booster.\nDaily scavenging attempts in the Dumping Grounds +1 ({meds.StaminaShots} injected so far)."));
			break;
		case 9:
			if (!(id == "painblock"))
			{
				return;
			}
			meds.PainBlockDay = Day;
			text = L.K("items.goods.use.injectPainBlockerChanceGetting", "你注射了痛觉屏蔽剂。\n今天拾荒受伤的几率减半。", "You inject the Pain Blocker.\nYour chance of getting hurt while scavenging is halved today.");
			break;
		case 5:
			if (!(id == "patch"))
			{
				return;
			}
			meds.PatchUntil = Math.Max(meds.PatchUntil, Day + 2);
			text = L.K("items.goods.use.applyHemingwayQuitPatchNo", "你贴上了海明威戒烟贴。\n3 天内不受尼古丁戒断的交易惩罚。", "You apply the Hemingway Quit Patch.\nNo nicotine withdrawal trading penalty for 3 days.");
			break;
		case 4:
			if (!(id == "mask"))
			{
				return;
			}
			meds.MaskReady = true;
			text = L.K("items.goods.use.putNightRepairMaskAfter", "你敷上了夜间修护面膜。\n下一次夜间外出，第二天不会开店迟到。", "You put on the Night Repair Mask.\nAfter your next night out, you won't open late the next day.");
			break;
		case 12:
			if (!(id == "rejuvenation") || !SilverBlood.CanInject())
			{
				return;
			}
			text = SilverBlood.Inject();
			break;
		case 10:
			if (!(id == "isomer_gel"))
			{
				return;
			}
			if (SilverBlood.SplitGel(item) == 0)
			{
				try
				{
					StoreUIManager.Instance?.Notify(L.K("items.goods.use.noRoomShopMakeSome", "店里放不下了，先腾点地方再拆。", "No room in the shop. Make some space first."));
					return;
				}
				catch
				{
					return;
				}
			}
			text = L.K("items.goods.use.splitIsomerRepairGelInto", "你拆开了异构修复凝胶，得到 3 小管异构蛋白溶液。纯度各不相同，可以用原版的化学检测仪看。", "You split the isomer repair gel into 3 small tubes of isomer solution. Their purity varies; check them with a chemical scanner.");
			break;
		case 8:
		case 11:
			return;
		}
		try
		{
			if (!string.IsNullOrEmpty(item.soundUse))
			{
				AudioManager.Instance?.Play(item.soundUse);
			}
		}
		catch
		{
		}
		if (flag)
		{
			try
			{
				item.Destroy();
			}
			catch (Exception ex)
			{
				ModLog.Warn("消耗物品失败：" + ex.Message);
			}
		}
		ModLog.Info(text.Replace("\n", " "));
		try
		{
			GenericConfirmationUIManager.Instance?.OpenPanelSingleButton(text, L.K("items.goods.use.oK", "确定", "OK"));
		}
		catch
		{
		}
	}

	public static bool GlovesCarried()
	{
		try
		{
			List<GameItem> list = EmporiumEntry.Instance?.GetAllAfterhourOwnedItems();
			if (list == null)
			{
				return false;
			}
			foreach (GameItem item in list)
			{
				if (IdOf(item) == "gloves")
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	public static bool SignInShowcase()
	{
		try
		{
			GameGridInventory gameGridInventory = EmporiumEntry.Instance?.showcaseElement;
			if (gameGridInventory?.childItems == null)
			{
				return false;
			}
			foreach (GameItem childItem in gameGridInventory.childItems)
			{
				if (IdOf(childItem) == "sign")
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	public static bool TryUseMask(PlayerStore store)
	{
		MedState m = M;
		HealthData healthData = store?.healthData;
		if (m == null || !m.MaskReady || healthData == null || !healthData.wentToAfterhoursLastNight)
		{
			return false;
		}
		healthData.wentToAfterhoursLastNight = false;
		m.MaskReady = false;
		ModLog.Info("夜间修护面膜生效：今天不会开店迟到");
		return true;
	}

	public static void ApplyCandle(StoreClientManager manager)
	{
		if (!CandleActive || manager?.clientStack == null)
		{
			return;
		}
		if (_boostDay != Day)
		{
			Boosted.Clear();
			_boostDay = Day;
		}
		foreach (StoreClient item in manager.clientStack)
		{
			try
			{
				if (item != null && !(item.clientFaction != "FACTION_UPPER_LEVEL") && (item.identifier == null || !item.identifier.StartsWith("corp_")) && Boosted.Add(item))
				{
					int budget = item.GetBudget();
					if (budget > 0)
					{
						item.OverrideBudget((int)Math.Round((double)budget * 1.15));
					}
				}
			}
			catch
			{
			}
		}
	}

	public static void OnDayStart()
	{
		MedState m = M;
		if (m == null || m.CandleUntil < 0 || Day <= m.CandleUntil)
		{
			return;
		}
		try
		{
			List<GameItem> list = new List<GameItem>();
			foreach (GameItem allRepoEligibleItem in PlayerStore.Instance.GetAllRepoEligibleItems())
			{
				if (IsLitCandle(allRepoEligibleItem))
				{
					list.Add(allRepoEligibleItem);
				}
			}
			foreach (GameItem item in list)
			{
				try
				{
					item.Destroy();
				}
				catch
				{
				}
			}
			if (list.Count > 0)
			{
				ModLog.Info($"香薰蜡烛烧完了（{list.Count} 支）");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("清理蜡烛失败：" + ex.Message);
		}
		m.CandleUntil = -1;
	}
}
