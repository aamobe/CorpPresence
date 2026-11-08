using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence;

internal static class Brand
{
	internal sealed record Entry(string Name, string Desc, Texture2D Icon);

	private static readonly Dictionary<string, string[]> Lists = new Dictionary<string, string[]>
	{
		["pastel"] = new string[16]
		{
			"nutrifruit", "galaxy_blend", "nutrifruit_seed", "bloomberry_seed", "kotton_seed", "kotton_fiber", "kotton_fabric", "shampoo", "toothpaste", "box_tampon",
			"pack_condom", "packet_red_cigarette", "black_injector", "@gloves", "@patch", "@sign"
		},
		["solana"] = new string[] { "skincare_cream", "neuroactive_perfume", "self_refilling_neuroactive_perfume", "pheromone_perfume", "@elixir", "@mask", "@candle", "@oilflower_seed", "@oilflower", "@perfume_base_oil_material", "@perfume_extract", "@perfume_blue_extract", "@perfume_oil_extractor", "@perfume_blood_extractor", "@perfume_mixer", "@perfume_blood_collector" },
		["carmicheal"] = new string[9] { "large_purple_injector", "pink_injector", "pure_white_injector", "phagimycin_pill", "smelling_salt", "zerochew", "chem_scanner", "@stamina", "@painblock" },
		["meridian"] = new string[8] { "glock_receiver", "smg", "shotgun", "mag_10mm_smg", "mag_10mm_smg_high", "mag_10mm_smg_box", "stock", "short_stock" },
		["spacer"] = new string[20]
		{
			"printed_gun", "handmade_pistol", "heavy_handmade_pistol", "handmade_pistol_frame", "heavy_handmade_pistol_frame", "small_pistol_ammo", "small_pistol_ammo_p", "small_pistol_ammo_nl", "heavy_pistol_ammo", "heavy_pistol_ammo_p",
			"heavy_pistol_ammo_nl", "fanta_pill", "unlicensed_phagimycin_pill", "caffeine_pill", "salve", "red_beer", "@zhuoyue_battery", "@anchor_cell", "@mutual_medkit", "@freedom_walkman"
		},
		["davre"] = new string[]
		{
			"processed_meat", "cup_noodle", "water_tablet", "water_tablet_container", "water_test_strip", "water_test_container", "aquascan", "uv_filter", "ribwich", "@leviathan",
			"@fresh_meat_can", "@meat_grinder", "@canning_machine",
			"@fish_mince", "@crab_stick", "@fish_can", "@seafood_sealer"
		}
	};

	private static readonly Dictionary<string, string> Owner = Lists.SelectMany((KeyValuePair<string, string[]> kv) => kv.Value.Select((string id) => (id: id, Key: kv.Key))).ToDictionary(((string id, string Key) p) => p.id, ((string id, string Key) p) => p.Key);

	private static readonly Dictionary<string, Entry> Cache = new Dictionary<string, Entry>();

	public static string[] Of(Company c)
	{
		if (c == null || !Lists.TryGetValue(c.Id, out var value))
		{
			return Array.Empty<string>();
		}
		return value;
	}

	private static string KeyOf(GameItem item)
	{
		try
		{
			string text = ItemIdentity.GoodsKey(item.identifier, item.spritePath);
			return (text != null) ? ("@" + text) : item.identifier;
		}
		catch
		{
			return null;
		}
	}

	public static void Record(GameItem item)
	{
		string text = ((item == null) ? null : KeyOf(item));
		if (text != null && Owner.TryGetValue(text, out var value))
		{
			Company company = Companies.Find(value);
			CompanyState companyState = Companies.StateOf(company);
			if (companyState != null && !companyState.Brand.Contains(text))
			{
				companyState.Brand.Add(text);
				ModLog.Info("品牌目录收录：" + company.Name + " · " + Info(text).Name);
			}
		}
	}

	public static void ScanOwned()
	{
		try
		{
			List<GameItem> list = EmporiumEntry.Instance?.GetAllOwnedItems();
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					Record(list[i]);
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("品牌目录扫描失败：" + ex.Message);
		}
	}

	private static int _translationVersion = -1;

	public static Entry Info(string key)
	{
		int version = OfficialTranslations.Version;
		if (_translationVersion != version) { Cache.Clear(); _translationVersion = version; }
		string key2 = L.Locale + ":" + key;
		if (Cache.TryGetValue(key2, out var value))
		{
			return value;
		}
		string name = key;
		string desc = "";
		Texture2D icon = null;
		bool flag = false;
		try
		{
			GameItem gameItem = (key.StartsWith("@") ? Goods.Make(key.Substring(1)) : DirectoryMaster.Item(key, isOwned: false));
			if (gameItem != null)
			{
				name = Clean(gameItem.GetDisplayName())?.TrimEnd('。') ?? key;
				desc = Clean(string.IsNullOrEmpty(gameItem.flavorText) ? gameItem.shortDescription : gameItem.flavorText) ?? "";
				icon = Tex.FromSprite(Goods.FindSprite(gameItem.spritePath) ?? AtlasSprite(gameItem) ?? Goods.VanillaByName(gameItem.spritePath));
				flag = true;
				try
				{
					gameItem.Destroy();
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("品牌目录读取 " + key + " 失败：" + ex.Message);
		}
		Entry entry = new Entry(name, desc, icon);
		if (flag)
		{
			Cache[key2] = entry;
		}
		return entry;
	}

	private static Sprite AtlasSprite(GameItem item)
	{
		try
		{
			return string.IsNullOrEmpty(item.spriteAtlasPath) ? null : RenderHandler.LoadFromAtlas(item.spriteAtlasPath, item.spritePath);
		}
		catch
		{
			return null;
		}
	}

	private static string Clean(string s)
	{
		if (s != null)
		{
			return Regex.Replace(s, "<[^>]*>", "").Trim();
		}
		return null;
	}
}
