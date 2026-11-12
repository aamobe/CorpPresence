using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class ItemIdentity
{
	internal sealed record Entry(string Id, string LegacyId, string LegacySprite, string Goods, string Company);

	public static IReadOnlyList<Entry> All { get; } = Array.AsReadOnly(new Entry[]
	{
		Good("stamina", "pink_alt_injector"),
		Good("painblock", "pink_alt_injector"),
		Good("gloves", "toothpaste"),
		Good("patch", "toothpaste"),
		Good("sign", "toothpaste"),
		Good("elixir", "skincare_cream"),
		Good("mask", "skincare_cream"),
		Good("candle", "skincare_cream"),
		Good("rosen_revolver", "revolver"),
		Good("rosen_rifle", "shotgun"),
		Good("zhuoyue_battery", "energy_credit"),
		Good("mutual_medkit", "common_medical"),
		Good("freedom_walkman", "skincare_cream"),
		Good("anchor_cell", "energy_credit"),
		Good("meridian_cert", "cert1"),
		Good("leviathan", "portable_water_purifier"),
		Good("crate_pastel", "ammo_box"),
		Good("crate_davre", "ammo_box"),
		Good("crate_solana", "ammo_box"),
		Good("crate_carmicheal", "ammo_box"),
		Good("crate_meridian", "ammo_box"),
		Good("crate_spacer", "ammo_box"),
		Good("crate_jackson_private", "ammo_box"),
		Good("tobacco_seed", "kotton_seed"),
		Good("oilflower_seed", "ammo_box"),
		Good("oilflower", "ammo_box"),
		Good(Perfume.OilKey, "ammo_box"),
		Good(Perfume.LegacyOilBottleKey, "ammo_box"),
		Good(Perfume.RedKey, "ammo_box"),
		Good(Perfume.BlueKey, "ammo_box"),
		Good(Perfume.ExtractorKey, "ammo_box"),
		Good(Perfume.BloodKey, "ammo_box"),
		Good(Perfume.MixerKey, "ammo_box"),
		Good(Perfume.CollectorKey, "ammo_box"),
		Good("tobacco_leaf", "ammo_box"),
		Good("loose_cigarette", "ammo_box"),
		Good("rolling_machine", "ammo_box"),
		Good("packaging_machine", "ammo_box"),
		Good("hemingway_stamp", "ammo_box"),
		Good("fine_mince", "ammo_box"),
		Good("coarse_mince", "ammo_box"),
		Good("fruit_paste", "ammo_box"),
		Good("fresh_meat_can", "ammo_box"),
		Good("meat_grinder", "ammo_box"),
		Good("canning_machine", "ammo_box"),
		Good("fish_mince", "ammo_box"),
		Good("crab_stick", "ammo_box"),
		Good("fish_can", "ammo_box"),
		Good("seafood_sealer", "ammo_box"),
		Good(RatWheelGenerator.GoodsKey, "ammo_box"),
		new Entry(ProductionWorkBay.ItemId, null, ProductionWorkBay.SpriteKey, ProductionWorkBay.GoodsKey, null),
		new Entry(CargoWarehouse.ItemId, null, CargoWarehouse.SpriteKey, CargoWarehouse.GoodsKey, null),
		new Entry(CargoWarehouse.MangoId, null, "corp_med_" + CargoWarehouse.MangoKey, CargoWarehouse.MangoKey, null),
		new Entry("corp_presence_mango_backpack", null, "corp_med_mango_backpack", "mango_backpack", null),
		new Entry(WasteBattery.ItemId, null, "corp_med_waste_battery", WasteBattery.Key, null),
		new Entry(CargoWarehouse.ModuleId, null, CargoWarehouse.ModuleSpriteKey, CargoWarehouse.ModuleKey, null),
		new Entry(CargoWarehouse.ControllerId, null, CargoWarehouse.ControllerSpriteKey, CargoWarehouse.ControllerKey, null),
		Good("forged_billet", "ammo_box"),
		Good("custom_dagger", "ammo_box"),
		Good("custom_saber", "ammo_box"),
		Good("custom_machete", "ammo_box"),
		Good("forge_furnace", "ammo_box"),
		Good("forge_anvil", "ammo_box"),
		Good("silverblood", "ammo_box"),
		Good("blood_drawer", "ammo_box"),
		Good("silver_blood", "ammo_box"),
		Good("isomer_vial", "ammo_box"),
		Good("carapace_small", "ammo_box"),
		Good("carapace_medium", "ammo_box"),
		Good("carapace_full", "ammo_box"),
		Good("rejuvenation", "ammo_box"),
		Good("implant_compat", "ammo_box"),
		Good("isomer_gel", "ammo_box"),
		Good("isomer_reactor", "ammo_box"),
		Good("anon_parcel", "ammo_box"),
		Good("sb_handbook", "ammo_box"),
		Good("friend_medal", "cert1"),
		Good("agent_cert", "cert4"),
		Good("rebel_token", "cert1"),
		Good("lower_rep_cert", "cert2"),
		Good("fate_coin_pastel", "gamblers_coin"),
		Good("fate_coin_solana", "gamblers_coin"),
		Good("fate_coin_carmicheal", "gamblers_coin"),
		Good("fate_coin_meridian", "gamblers_coin"),
		Good("fate_coin_davre", "gamblers_coin"),
		new Entry("corp_presence_candle_lit", "skincare_cream", "corp_med_candle_lit", "candle", null),
		new Entry("corp_presence_card_pastel", "card_pharma", "corp_card_pastel", null, "pastel"),
		new Entry("corp_presence_card_solana", "card_pharma", "corp_card_solana", null, "solana"),
		new Entry("corp_presence_card_carmicheal", "card_pharma", "corp_card_carmicheal", null, "carmicheal"),
		new Entry("corp_presence_card_meridian", "card_pharma", "corp_card_meridian", null, "meridian"),
		new Entry("corp_presence_card_davre", "card_pharma", "corp_card_davre", null, "davre")
	});

	// Preserve the original IL order: the table must exist before indexing it.
	private static readonly Dictionary<string, Entry> ById = BuildIndex();

	private static Entry Good(string key, string legacy)
	{
		return new Entry("corp_presence_" + key, legacy, "corp_med_" + key, key, null);
	}

	private static Dictionary<string, Entry> BuildIndex()
	{
		Dictionary<string, Entry> dictionary = new Dictionary<string, Entry>(StringComparer.Ordinal);
		foreach (Entry item in All)
		{
			dictionary.Add(item.Id, item);
		}
		return dictionary;
	}

	public static Entry Find(string identifier, string spritePath)
	{
		if (identifier == null)
		{
			return null;
		}
		if (ById.TryGetValue(identifier, out var value))
		{
			return value;
		}
		if (spritePath == null)
		{
			return null;
		}
		return ByLegacy.TryGetValue((identifier, spritePath), out value) ? value : null;
	}

	// 旧版物品按（旧编号, 旧贴图）查表；原版物品每次存档都会查一次，不再逐条比对
	private static readonly Dictionary<(string, string), Entry> ByLegacy = BuildLegacyIndex();

	private static Dictionary<(string, string), Entry> BuildLegacyIndex()
	{
		var dictionary = new Dictionary<(string, string), Entry>();
		foreach (Entry item in All)
		{
			// 和原来的逐条比对一样，同一组旧编号和贴图以表里第一条为准
			if (item.LegacyId != null && item.LegacySprite != null && !dictionary.ContainsKey((item.LegacyId, item.LegacySprite)))
			{
				dictionary.Add((item.LegacyId, item.LegacySprite), item);
			}
		}
		return dictionary;
	}

	public static string ResolveId(string identifier, string spritePath)
	{
		return Find(identifier, spritePath)?.Id;
	}

	public static string GoodsKey(string identifier, string spritePath)
	{
		return Find(identifier, spritePath)?.Goods;
	}

	public static string CardKey(string identifier, string spritePath)
	{
		return Find(identifier, spritePath)?.Company;
	}

	public static Entry ForGoods(string key)
	{
		if (key != null)
		{
			return Find("corp_presence_" + key, null);
		}
		return null;
	}

	public static Entry ForCard(string company)
	{
		if (company != null)
		{
			return Find("corp_presence_card_" + company, null);
		}
		return null;
	}
}
