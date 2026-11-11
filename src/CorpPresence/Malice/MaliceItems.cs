using System;
using System.Collections.Generic;
using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence.Malice;

internal static class MaliceItems
{
	public const string Medal = "friend_medal";

	public const string AgentCert = "agent_cert";

	public const string RebelToken = "rebel_token";

	public const string RepCert = "lower_rep_cert";

	public static readonly string[] CoinCompanies = new string[5] { "pastel", "solana", "carmicheal", "meridian", "davre" };

	internal static readonly DefinitionSet<Goods.Def> Defs = BuildDefs();
	private static Sprite _platinumCoin;

	public static string Coin(string companyId)
	{
		return "fate_coin_" + companyId;
	}

	internal static Sprite PlatinumCoin(Sprite original)
	{
		if (_platinumCoin != null) return _platinumCoin;
		if (original == null) return null;
		Texture2D crop = Tex.FromSprite(original);
		if (crop == null || !crop.isReadable) return original;

		Texture2D texture = null;
		try
		{
			Color32[] pixels = crop.GetPixels32();
			for (int i = 0; i < pixels.Length; i++) pixels[i] = PlatinumShade(pixels[i]);
			texture = new Texture2D(crop.width, crop.height, TextureFormat.RGBA32, mipChain: false)
			{
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp,
				hideFlags = HideFlags.HideAndDontSave
			};
			texture.SetPixels32(pixels);
			texture.Apply(updateMipmaps: false);
			Vector2 pivot = new Vector2(original.pivot.x / original.rect.width, original.pivot.y / original.rect.height);
			Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, crop.width, crop.height), pivot, original.pixelsPerUnit);
			sprite.name = "corp_med_fate_coin_platinum";
			sprite.hideFlags = HideFlags.HideAndDontSave;
			return _platinumCoin = sprite;
		}
		catch (Exception error)
		{
			if (texture != null) UnityEngine.Object.Destroy(texture);
			ModLog.Warn("命运硬币换色失败：" + error.Message);
			return original;
		}
	}

	private static Color32 PlatinumShade(Color32 pixel)
	{
		if (pixel.a < 128) return pixel;
		int lightness = (54 * pixel.r + 183 * pixel.g + 19 * pixel.b) >> 8;
		return lightness switch
		{
			< 55 => new Color32(0x29, 0x22, 0x34, pixel.a),
			< 118 => new Color32(0x6D, 0x6E, 0x7D, pixel.a),
			< 170 => new Color32(0xB7, 0xB9, 0xC0, pixel.a),
			_ => new Color32(0xEF, 0xEA, 0xDF, pixel.a)
		};
	}

	private static DefinitionSet<Goods.Def> BuildDefs()
	{
		DefinitionSet<Goods.Def> dictionary = new DefinitionSet<Goods.Def>("MaliceItems");
		dictionary["friend_medal"] = new Goods.Def
		{
			Name = "巨企之友",
			NameEn = "Friend of the Corporations",
			Value = 100L,
			Company = "malice",
			Art = "friend_medal",
			Fallback = "scav_medal",
			Types = Array.Empty<string>(),
			W = 1,
			H = 2,
			Desc = "联合站区管理处颁发。放进商品展示窗：上层区顾客带的钱 +10%。",
			DescEn = "Issued by the Joint District Administration. Put it in the display case: Upper Levels customers carry 10% more money.",
			Flavor = "金色的“ılı”徽记，绶带是深紫色的。背面刻着：感谢您对站区繁荣的贡献。",
			FlavorEn = "A gold “ılı” emblem on a deep purple ribbon. Engraved on the back: Thank you for your contribution to the district's prosperity."
		};
		dictionary["agent_cert"] = new Goods.Def
		{
			Name = "巨企代理人证书",
			NameEn = "Corporate Agent Certificate",
			Value = 50L,
			Company = "malice",
			Art = "agent_cert",
			Fallback = "cert4",
			Types = new string[1] { "DOCUMENT" },
			W = 3,
			H = 2,
			Desc = "下层区巨企代理人。到手即生效：所有公司服务免费，订货一律八折（仍按每周补货、按档位限量）。",
			DescEn = "Lower Levels corporate agent. Takes effect on receipt: all company services are free and phone orders are 20% off (weekly restock and tier limits still apply).",
			Flavor = "金框证书，盖着五家巨企的联合钢印。落款处原本该签名的地方空着。",
			FlavorEn = "A gold-framed certificate stamped with the joint seal of five corporations. The line where a signature should be is blank."
		};
		DefinitionSet<Goods.Def> dictionary2 = dictionary;
		dictionary2["rebel_token"] = new Goods.Def
		{
			Name = "反抗站点信物",
			NameEn = "Resistance Waypoint Token",
			Value = 20L,
			Company = "malice",
			Art = "rebel_token",
			Fallback = "scav_medal",
			Types = Array.Empty<string>(),
			W = 1,
			H = 1,
			Desc = "你是反抗军在这个站点的人。到手即生效：地下买家带的钱 +50%。",
			DescEn = "You're the Resistance's person at this station. Takes effect on receipt: underground buyers carry 50% more money.",
			Flavor = "一枚磨旧的金属牌，穿着一截橄榄绿布条。牌面刻着一道斜杠。",
			FlavorEn = "A worn metal tag on a strip of olive cloth. A single slash is scratched into its face."
		};
		dictionary2["lower_rep_cert"] = new Goods.Def
		{
			Name = "下层区代表证明",
			NameEn = "Lower Levels Representative",
			Value = 50L,
			Company = "malice",
			Art = "lower_rep_cert",
			Fallback = "cert2",
			Types = new string[1] { "DOCUMENT" },
			W = 3,
			H = 2,
			Desc = "下层区居民联署推举你做代表。到手即生效：所有顾客带的钱 +50%。",
			DescEn = "Lower Levels residents co-signed to name you their representative. Takes effect on receipt: all customers carry 50% more money.",
			Flavor = "一张旧纸，盖满了手印和歪歪扭扭的签名。",
			FlavorEn = "An old sheet covered in thumbprints and crooked signatures."
		};
		Dictionary<string, (string, string)> dictionary3 = new Dictionary<string, (string, string)>
		{
			["pastel"] = ("帕斯泰尔", "Pastel"),
			["solana"] = ("索拉纳", "Solana"),
			["carmicheal"] = ("卡迈克尔", "Carmicheal"),
			["meridian"] = ("子午线", "Meridian"),
			["davre"] = ("达夫雷", "Davre")
		};
		string[] coinCompanies = CoinCompanies;
		foreach (string text in coinCompanies)
		{
			var (text2, text3) = dictionary3[text];
			dictionary2[Coin(text)] = new Goods.Def
			{
				ArtSource = Goods.ArtKind.TintedNativeSprite,
				Name = text2 + "的命运硬币",
				NameEn = text3 + "'s Coin of Fate",
				Value = 50L,
				Company = "malice",
				Art = Coin(text),
				Fallback = "the_coin",
				Types = Array.Empty<string>(),
				W = 1,
				H = 1,
				Desc = "被巨企选中的人。带在身上：幸运 +5（每枚各算，可以叠加）。",
				DescEn = "One chosen by the corporations. Carried: +5 luck (each coin counts; they stack).",
				Flavor = text2 + "的代表亲手交给你的硬币。正面是公司的标志，背面什么都没有。",
				FlavorEn = "A coin " + text3 + "'s representative handed you in person. The company mark on one side; nothing on the other."
			};
		}
		return dictionary2;
	}
}
